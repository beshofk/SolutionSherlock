using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Services
{
    /// <summary>
    /// Orchestrates a search: resolve free-text into metadata IDs or record IDs,
    /// batch-query solutioncomponent for those IDs, then join the results back to
    /// solution info. This is what keeps the search fast regardless of how many
    /// solutions exist in the environment - solutions are never iterated one at a time.
    ///
    /// Every method here takes a SearchProgress so a caller (the plugin UI) can both
    /// abort cleanly between server round-trips and show a live, step-by-step account
    /// of what's happening - see SearchProgress for the cancellation/logging contract.
    /// </summary>
    public class SearchEngine
    {
        // BatchSize (the "In" clause ceiling) now lives in QueryPaging, shared with
        // SolutionExplorerService. PageSize stays local since RetrieveMultiple's page
        // count is just a plain int, not worth sharing.
        private const int PageSize = 5000;

        private readonly IOrganizationService _service;
        private readonly ISolutionRepository _solutionCache;
        private readonly MetadataResolver _metadataResolver;

        public SearchEngine(IOrganizationService service, ISolutionRepository solutionCache, MetadataResolver metadataResolver)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _solutionCache = solutionCache ?? throw new ArgumentNullException(nameof(solutionCache));
            _metadataResolver = metadataResolver ?? throw new ArgumentNullException(nameof(metadataResolver));
        }

        /// <summary>
        /// Runs a full search and returns one row per (component, solution) pair.
        /// Safe to call from a background thread (XrmToolBox WorkAsync) - this method
        /// does not touch any UI state directly; all UI feedback flows through
        /// <paramref name="progress"/>. Throws OperationCanceledException if the
        /// caller cancels progress.CancellationToken mid-search.
        /// </summary>
        public List<SearchResult> Search(SearchCriteria criteria, SearchProgress progress = null)
        {
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));
            progress = progress ?? SearchProgress.None;

            var sw = Stopwatch.StartNew();
            progress.Log($"Starting {ComponentTypeMap.Get(criteria.MainType).DisplayName} search...");

            List<SearchResult> results;

            if (criteria.MainType == ComponentTypeKind.Entity)
            {
                results = SearchEntityOrSubComponent(criteria, progress);
            }
            else if (criteria.MainType == ComponentTypeKind.OptionSet)
            {
                results = SearchGlobalOptionSets(criteria, progress);
            }
            else
            {
                var typeInfo = ComponentTypeMap.Get(criteria.MainType);
                if (!typeInfo.IsDataRecord)
                    throw new NotSupportedException($"Searching by main type '{criteria.MainType}' is not implemented yet.");
                results = SearchDataRecordComponents(criteria, typeInfo, progress);
            }

            progress.Log($"Search finished: {results.Count} result row(s) in {sw.ElapsedMilliseconds}ms total.");
            return results;
        }

        /// <summary>
        /// Dispatches Entity searches: no sub-type selected means "search entities
        /// themselves"; a sub-type selected means "search that sub-component kind,
        /// scoped to entities matching the main search text".
        /// </summary>
        private List<SearchResult> SearchEntityOrSubComponent(SearchCriteria criteria, SearchProgress progress)
        {
            if (!criteria.SubType.HasValue)
                return SearchEntitiesOnly(criteria, progress);

            switch (criteria.SubType.Value)
            {
                case ComponentTypeKind.Attribute:
                    return SearchEntityFields(criteria, progress);
                case ComponentTypeKind.SystemForm:
                    return SearchEntityScopedRecords(criteria, progress, "systemform", "name", "objecttypecode", 60, "Form");
                case ComponentTypeKind.SavedQuery:
                    return SearchEntityScopedRecords(criteria, progress, "savedquery", "name", "returnedtypecode", 26, "View");
                case ComponentTypeKind.Chart:
                    return SearchEntityScopedRecords(criteria, progress, "savedqueryvisualization", "name", "primaryentitytypecode", 59, "Chart");
                case ComponentTypeKind.BusinessRule:
                    return SearchEntityScopedRecords(criteria, progress, "workflow", "name", "primaryentity", 29, "Business Rule", "category", 2);
                case ComponentTypeKind.EntityKey:
                    return SearchEntityKeys(criteria, progress);
                case ComponentTypeKind.Relationship:
                    return SearchEntityRelationships(criteria, progress);
                default:
                    throw new NotSupportedException(
                        $"Searching by sub-component type '{criteria.SubType.Value}' is not implemented yet.");
            }
        }

        private List<SearchResult> SearchEntitiesOnly(SearchCriteria criteria, SearchProgress progress)
        {
            var results = new List<SearchResult>();

            var matchedEntities = ResolveEntitiesLogged(criteria.MainSearchText, progress);
            if (matchedEntities.Count == 0) return results;

            var entityIds = matchedEntities.Select(e => e.MetadataId.Value).ToArray();
            var entityComponentCode = ComponentTypeMap.Get(ComponentTypeKind.Entity).SolutionComponentTypeCode;

            progress.Log($"Checking solution membership for {entityIds.Length} entit{(entityIds.Length == 1 ? "y" : "ies")}...");
            var sw = Stopwatch.StartNew();
            var solutionsByEntityId = LookupSolutions(entityIds, entityComponentCode, progress);
            progress.Log($"Solution membership lookup complete in {sw.ElapsedMilliseconds}ms.");

            foreach (var entity in matchedEntities)
            {
                progress.ThrowIfCancelled();

                if (!solutionsByEntityId.TryGetValue(entity.MetadataId.Value, out var solutionIds))
                    continue;

                foreach (var solutionId in solutionIds)
                {
                    var row = BuildResultRow(solutionId, criteria, out var included);
                    if (!included) continue;

                    row.ComponentTypeDisplay = "Entity";
                    row.ComponentDisplayName = entity.GetDisplayLabel();
                    row.ComponentLogicalName = entity.LogicalName;
                    row.ParentEntityLogicalName = null;
                    row.ComponentObjectId = entity.MetadataId.Value;
                    results.Add(row);
                }
            }

            return results;
        }

        private List<SearchResult> SearchEntityFields(SearchCriteria criteria, SearchProgress progress)
        {
            var results = new List<SearchResult>();

            var matchedEntities = ResolveEntitiesLogged(criteria.MainSearchText, progress);
            if (matchedEntities.Count == 0) return results;

            var attributeComponentCode = ComponentTypeMap.Get(ComponentTypeKind.Attribute).SolutionComponentTypeCode;
            progress.Log($"Checking fields on {matchedEntities.Count} matched entit{(matchedEntities.Count == 1 ? "y" : "ies")}...");

            // A field only gets its own solutioncomponent row when it was added
            // individually. When the parent entity was added with "include all"
            // (rootcomponentbehavior = IncludeSubcomponents), every field is
            // implicitly part of the solution with no row of its own, so that has
            // to be checked separately or such fields are invisible to search.
            var entityIds = matchedEntities.Where(e => e.MetadataId.HasValue).Select(e => e.MetadataId.Value).ToArray();
            var entityIncludeAllSolutions = LookupEntityIncludeAllSolutions(entityIds, progress);

            foreach (var entity in matchedEntities)
            {
                progress.ThrowIfCancelled();

                var matchedAttributes = _metadataResolver.ResolveAttributes(entity.LogicalName, criteria.SubSearchText);
                if (matchedAttributes.Count == 0) continue;

                progress.Log($"  {entity.LogicalName}: {matchedAttributes.Count} matching field(s).");

                // Results are grouped by the FIELD's own solutioncomponent membership,
                // not the parent entity's, since a custom field can ship in a different
                // solution than the entity it lives on.
                var attributeIds = matchedAttributes.Select(a => a.MetadataId.Value).ToArray();
                var solutionsByAttributeId = LookupSolutions(attributeIds, attributeComponentCode, progress);
                entityIncludeAllSolutions.TryGetValue(entity.MetadataId.Value, out var includeAllSolutionIds);

                foreach (var attribute in matchedAttributes)
                {
                    solutionsByAttributeId.TryGetValue(attribute.MetadataId.Value, out var explicitSolutionIds);
                    var solutionIds = MergeSolutionIds(explicitSolutionIds, includeAllSolutionIds);
                    if (solutionIds.Count == 0) continue;

                    foreach (var solutionId in solutionIds)
                    {
                        var row = BuildResultRow(solutionId, criteria, out var included);
                        if (!included) continue;

                        row.ComponentTypeDisplay = "Field";
                        row.ComponentDisplayName = attribute.GetDisplayLabel();
                        row.ComponentLogicalName = attribute.LogicalName;
                        row.ParentEntityLogicalName = entity.LogicalName;
                        row.ComponentObjectId = attribute.MetadataId.Value;
                        results.Add(row);
                    }
                }
            }

            return results;
        }

        private List<SearchResult> SearchEntityKeys(SearchCriteria criteria, SearchProgress progress)
        {
            var results = new List<SearchResult>();

            var matchedEntities = ResolveEntitiesLogged(criteria.MainSearchText, progress);
            if (matchedEntities.Count == 0) return results;

            var code = ComponentTypeMap.Get(ComponentTypeKind.EntityKey).SolutionComponentTypeCode;
            progress.Log($"Checking keys on {matchedEntities.Count} matched entit{(matchedEntities.Count == 1 ? "y" : "ies")}...");

            // Keys added implicitly via the parent entity's "include all" setting have
            // no solutioncomponent row of their own - see LookupEntityIncludeAllSolutions.
            var entityIds = matchedEntities.Where(e => e.MetadataId.HasValue).Select(e => e.MetadataId.Value).ToArray();
            var entityIncludeAllSolutions = LookupEntityIncludeAllSolutions(entityIds, progress);

            foreach (var entity in matchedEntities)
            {
                progress.ThrowIfCancelled();

                var keys = _metadataResolver.ResolveKeys(entity.LogicalName, criteria.SubSearchText);
                if (keys.Count == 0) continue;

                progress.Log($"  {entity.LogicalName}: {keys.Count} matching key(s).");

                var ids = keys.Where(k => k.MetadataId.HasValue).Select(k => k.MetadataId.Value).ToArray();
                var solutionsById = LookupSolutions(ids, code, progress);
                entityIncludeAllSolutions.TryGetValue(entity.MetadataId.Value, out var includeAllSolutionIds);

                foreach (var key in keys)
                {
                    if (!key.MetadataId.HasValue) continue;
                    solutionsById.TryGetValue(key.MetadataId.Value, out var explicitSolutionIds);
                    var solutionIds = MergeSolutionIds(explicitSolutionIds, includeAllSolutionIds);
                    if (solutionIds.Count == 0) continue;

                    foreach (var solutionId in solutionIds)
                    {
                        var row = BuildResultRow(solutionId, criteria, out var included);
                        if (!included) continue;

                        row.ComponentTypeDisplay = "Key";
                        row.ComponentDisplayName = key.GetDisplayLabel();
                        row.ComponentLogicalName = key.LogicalName;
                        row.ParentEntityLogicalName = entity.LogicalName;
                        row.ComponentObjectId = key.MetadataId.Value;
                        results.Add(row);
                    }
                }
            }

            return results;
        }

        private List<SearchResult> SearchEntityRelationships(SearchCriteria criteria, SearchProgress progress)
        {
            var results = new List<SearchResult>();

            var matchedEntities = ResolveEntitiesLogged(criteria.MainSearchText, progress);
            if (matchedEntities.Count == 0) return results;

            var code = ComponentTypeMap.Get(ComponentTypeKind.Relationship).SolutionComponentTypeCode;
            progress.Log($"Checking relationships on {matchedEntities.Count} matched entit{(matchedEntities.Count == 1 ? "y" : "ies")}...");

            // Relationships added implicitly via the parent entity's "include all"
            // setting have no solutioncomponent row of their own - see
            // LookupEntityIncludeAllSolutions.
            var entityIds = matchedEntities.Where(e => e.MetadataId.HasValue).Select(e => e.MetadataId.Value).ToArray();
            var entityIncludeAllSolutions = LookupEntityIncludeAllSolutions(entityIds, progress);

            foreach (var entity in matchedEntities)
            {
                progress.ThrowIfCancelled();

                var relationships = _metadataResolver.ResolveRelationships(entity.LogicalName, criteria.SubSearchText);
                if (relationships.Count == 0) continue;

                progress.Log($"  {entity.LogicalName}: {relationships.Count} matching relationship(s).");

                var ids = relationships.Where(r => r.MetadataId.HasValue).Select(r => r.MetadataId.Value).ToArray();
                var solutionsById = LookupSolutions(ids, code, progress);
                entityIncludeAllSolutions.TryGetValue(entity.MetadataId.Value, out var includeAllSolutionIds);

                foreach (var rel in relationships)
                {
                    if (!rel.MetadataId.HasValue) continue;
                    solutionsById.TryGetValue(rel.MetadataId.Value, out var explicitSolutionIds);
                    var solutionIds = MergeSolutionIds(explicitSolutionIds, includeAllSolutionIds);
                    if (solutionIds.Count == 0) continue;

                    foreach (var solutionId in solutionIds)
                    {
                        var row = BuildResultRow(solutionId, criteria, out var included);
                        if (!included) continue;

                        row.ComponentTypeDisplay = $"Relationship ({rel.RelationshipTypeLabel})";
                        row.ComponentDisplayName = rel.SchemaName;
                        row.ComponentLogicalName = rel.SchemaName;
                        row.ParentEntityLogicalName = entity.LogicalName;
                        row.ComponentObjectId = rel.MetadataId.Value;
                        results.Add(row);
                    }
                }
            }

            return results;
        }

        private List<SearchResult> SearchGlobalOptionSets(SearchCriteria criteria, SearchProgress progress)
        {
            var results = new List<SearchResult>();

            progress.Log($"Resolving global option sets matching '{criteria.MainSearchText}'...");
            var sw = Stopwatch.StartNew();
            var matched = _metadataResolver.ResolveGlobalOptionSets(criteria.MainSearchText);
            progress.Log($"Found {matched.Count} matching option set(s) in {sw.ElapsedMilliseconds}ms.");
            if (matched.Count == 0) return results;

            progress.ThrowIfCancelled();

            var ids = matched.Where(o => o.MetadataId.HasValue).Select(o => o.MetadataId.Value).ToArray();
            var componentCode = ComponentTypeMap.Get(ComponentTypeKind.OptionSet).SolutionComponentTypeCode;
            var solutionsById = LookupSolutions(ids, componentCode, progress);

            foreach (var optionSet in matched)
            {
                if (!optionSet.MetadataId.HasValue) continue;
                if (!solutionsById.TryGetValue(optionSet.MetadataId.Value, out var solutionIds)) continue;

                foreach (var solutionId in solutionIds)
                {
                    var row = BuildResultRow(solutionId, criteria, out var included);
                    if (!included) continue;

                    row.ComponentTypeDisplay = "Option Set";
                    row.ComponentDisplayName = optionSet.DisplayName?.UserLocalizedLabel?.Label ?? optionSet.Name;
                    row.ComponentLogicalName = optionSet.Name;
                    row.ParentEntityLogicalName = null;
                    row.ComponentObjectId = optionSet.MetadataId.Value;
                    results.Add(row);
                }
            }

            return results;
        }

        /// <summary>
        /// Generic search for MAIN component kinds that are ordinary Dataverse table
        /// rows (Web Resource, Process, Security Role, Report, templates, etc.).
        /// Covers all of them because they only differ by table name, name column,
        /// componenttype code, and an optional static filter (e.g. Dashboard's
        /// systemform.type = 0) - all of which live on ComponentTypeInfo.
        /// </summary>
        private List<SearchResult> SearchDataRecordComponents(SearchCriteria criteria, ComponentTypeInfo typeInfo, SearchProgress progress)
        {
            var results = new List<SearchResult>();

            var query = new QueryExpression(typeInfo.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(typeInfo.PrimaryNameAttribute),
                PageInfo = new PagingInfo { PageNumber = 1, Count = PageSize }
            };

            if (!string.IsNullOrWhiteSpace(criteria.MainSearchText))
            {
                query.Criteria.AddCondition(typeInfo.PrimaryNameAttribute, ConditionOperator.Like, $"%{criteria.MainSearchText.Trim()}%");
            }

            if (!string.IsNullOrEmpty(typeInfo.StaticFilterAttribute))
            {
                query.Criteria.AddCondition(typeInfo.StaticFilterAttribute, ConditionOperator.Equal, typeInfo.StaticFilterValue);
            }

            // Process category is a user-selected refinement (the "Category" dropdown
            // that replaces the sub-component controls for Process), not a name search.
            if (criteria.MainType == ComponentTypeKind.Workflow && criteria.ProcessCategory.HasValue)
            {
                query.Criteria.AddCondition("category", ConditionOperator.Equal, criteria.ProcessCategory.Value);
            }

            progress.Log($"Querying '{typeInfo.EntityLogicalName}' for matching records...");
            var sw = Stopwatch.StartNew();
            var matchedRecords = RetrieveAllPages(query, progress);
            progress.Log($"Found {matchedRecords.Count} matching record(s) in {sw.ElapsedMilliseconds}ms.");
            if (matchedRecords.Count == 0) return results;

            progress.ThrowIfCancelled();

            var objectIds = matchedRecords.Select(r => r.Id).ToArray();
            progress.Log($"Checking solution membership for {objectIds.Length} record(s)...");
            sw.Restart();
            var solutionsByObjectId = LookupSolutions(objectIds, typeInfo.SolutionComponentTypeCode, progress);
            progress.Log($"Solution membership lookup complete in {sw.ElapsedMilliseconds}ms.");

            foreach (var record in matchedRecords)
            {
                if (!solutionsByObjectId.TryGetValue(record.Id, out var solutionIds)) continue;

                var name = record.GetAttributeValue<string>(typeInfo.PrimaryNameAttribute);

                foreach (var solutionId in solutionIds)
                {
                    var row = BuildResultRow(solutionId, criteria, out var included);
                    if (!included) continue;

                    row.ComponentTypeDisplay = typeInfo.DisplayName;
                    row.ComponentDisplayName = name;
                    row.ComponentLogicalName = name;
                    row.ParentEntityLogicalName = null;
                    row.ComponentObjectId = record.Id;
                    results.Add(row);
                }
            }

            return results;
        }

        /// <summary>
        /// Generic search for ENTITY-SCOPED sub-component kinds that are ordinary
        /// table rows (Form, View, Chart, Business Rule). Scoped per matched entity,
        /// with an optional extra static filter (e.g. Business Rule's category = 2).
        /// </summary>
        private List<SearchResult> SearchEntityScopedRecords(
            SearchCriteria criteria,
            SearchProgress progress,
            string tableLogicalName,
            string nameAttribute,
            string entityScopeAttribute,
            int componentTypeCode,
            string componentDisplayName,
            string staticFilterAttribute = null,
            object staticFilterValue = null)
        {
            var results = new List<SearchResult>();

            var matchedEntities = ResolveEntitiesLogged(criteria.MainSearchText, progress);
            if (matchedEntities.Count == 0) return results;

            progress.Log($"Checking {componentDisplayName.ToLowerInvariant()}s on {matchedEntities.Count} matched entit{(matchedEntities.Count == 1 ? "y" : "ies")}...");

            // Forms/Views/Charts/Business Rules added implicitly via the parent
            // entity's "include all" setting have no solutioncomponent row of their
            // own either - same root cause as Fields/Relationships/Keys, see
            // LookupEntityIncludeAllSolutions.
            var entityIds = matchedEntities.Where(e => e.MetadataId.HasValue).Select(e => e.MetadataId.Value).ToArray();
            var entityIncludeAllSolutions = LookupEntityIncludeAllSolutions(entityIds, progress);

            foreach (var entity in matchedEntities)
            {
                progress.ThrowIfCancelled();

                var query = new QueryExpression(tableLogicalName)
                {
                    ColumnSet = new ColumnSet(nameAttribute),
                    PageInfo = new PagingInfo { PageNumber = 1, Count = PageSize }
                };
                query.Criteria.AddCondition(entityScopeAttribute, ConditionOperator.Equal, entity.LogicalName);

                if (!string.IsNullOrWhiteSpace(criteria.SubSearchText))
                    query.Criteria.AddCondition(nameAttribute, ConditionOperator.Like, $"%{criteria.SubSearchText.Trim()}%");

                if (!string.IsNullOrEmpty(staticFilterAttribute))
                    query.Criteria.AddCondition(staticFilterAttribute, ConditionOperator.Equal, staticFilterValue);

                var matchedRecords = RetrieveAllPages(query, progress);
                if (matchedRecords.Count == 0) continue;

                progress.Log($"  {entity.LogicalName}: {matchedRecords.Count} matching {componentDisplayName.ToLowerInvariant()}(s).");

                var objectIds = matchedRecords.Select(r => r.Id).ToArray();
                var solutionsByObjectId = LookupSolutions(objectIds, componentTypeCode, progress);
                List<Guid> includeAllSolutionIds = null;
                if (entity.MetadataId.HasValue)
                    entityIncludeAllSolutions.TryGetValue(entity.MetadataId.Value, out includeAllSolutionIds);

                foreach (var record in matchedRecords)
                {
                    solutionsByObjectId.TryGetValue(record.Id, out var explicitSolutionIds);
                    var solutionIds = MergeSolutionIds(explicitSolutionIds, includeAllSolutionIds);
                    if (solutionIds.Count == 0) continue;

                    var name = record.GetAttributeValue<string>(nameAttribute);

                    foreach (var solutionId in solutionIds)
                    {
                        var row = BuildResultRow(solutionId, criteria, out var included);
                        if (!included) continue;

                        row.ComponentTypeDisplay = componentDisplayName;
                        row.ComponentDisplayName = name;
                        row.ComponentLogicalName = name;
                        row.ParentEntityLogicalName = entity.LogicalName;
                        row.ComponentObjectId = record.Id;
                        results.Add(row);
                    }
                }
            }

            return results;
        }

        private List<Microsoft.Xrm.Sdk.Metadata.EntityMetadata> ResolveEntitiesLogged(string searchText, SearchProgress progress)
        {
            progress.ThrowIfCancelled();
            progress.Log($"Resolving entities matching '{searchText}'...");
            var sw = Stopwatch.StartNew();
            var matched = _metadataResolver.ResolveEntities(searchText);
            progress.Log($"Found {matched.Count} matching entit{(matched.Count == 1 ? "y" : "ies")} in {sw.ElapsedMilliseconds}ms.");
            return matched;
        }

        /// <summary>
        /// Builds the solution-side fields of a result row and applies the
        /// managed/unmanaged filter. Returns included = false when the row should be
        /// skipped (unknown solution, or filtered out by the managed/unmanaged toggle).
        /// </summary>
        private SearchResult BuildResultRow(Guid solutionId, SearchCriteria criteria, out bool included)
        {
            var solution = _solutionCache.GetById(solutionId);
            if (solution == null)
            {
                included = false;
                return null;
            }

            if (criteria.ManagedOnly && !solution.IsManaged)
            {
                included = false;
                return null;
            }

            if (criteria.UnmanagedOnly && solution.IsManaged)
            {
                included = false;
                return null;
            }

            included = true;
            return new SearchResult
            {
                SolutionId = solutionId,
                SolutionFriendlyName = solution.FriendlyName,
                SolutionUniqueName = solution.UniqueName,
                IsManaged = solution.IsManaged,
                Publisher = solution.PublisherName,
                Version = solution.Version
            };
        }

        /// <summary>
        /// Batched lookup of solutioncomponent rows for a set of metadata/record object
        /// ids. Returns a map of objectid -> distinct list of solutionids that contain
        /// it. This is the one query that replaces "loop over every solution and ask if
        /// it contains X" with a single (batched) round trip per component type.
        /// </summary>
        private Dictionary<Guid, List<Guid>> LookupSolutions(IReadOnlyList<Guid> objectIds, int componentTypeCode, SearchProgress progress)
        {
            var map = new Dictionary<Guid, List<Guid>>();
            if (objectIds == null || objectIds.Count == 0) return map;

            var batches = QueryPaging.Chunk(objectIds, QueryPaging.BatchSize).ToList();
            var batchNum = 0;

            foreach (var batch in batches)
            {
                progress.ThrowIfCancelled();
                batchNum++;

                if (batches.Count > 1)
                    progress.Log($"  solutioncomponent batch {batchNum}/{batches.Count} ({batch.Count} id(s))...");

                var query = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("objectid", "solutionid"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("componenttype", ConditionOperator.Equal, componentTypeCode),
                            new ConditionExpression("objectid", ConditionOperator.In, batch.Cast<object>().ToArray())
                        }
                    },
                    PageInfo = new PagingInfo { PageNumber = 1, Count = PageSize }
                };

                foreach (var row in RetrieveAllPages(query, progress))
                {
                    var objectId = row.GetAttributeValue<Guid>("objectid");
                    var solutionRef = row.GetAttributeValue<EntityReference>("solutionid");
                    if (solutionRef == null) continue;

                    if (!map.TryGetValue(objectId, out var list))
                    {
                        list = new List<Guid>();
                        map[objectId] = list;
                    }

                    if (!list.Contains(solutionRef.Id))
                        list.Add(solutionRef.Id);
                }
            }

            return map;
        }

        /// <summary>
        /// Batched lookup of solutions that include a given entity with
        /// rootcomponentbehavior = 0 ("Include Subcomponents", i.e. the entity was
        /// added with "all objects"/"all assets"). When that's the case, Dataverse
        /// never writes individual solutioncomponent rows for the entity's fields,
        /// relationships or keys - they're implied, not listed - so LookupSolutions
        /// alone would report zero solutions for a field/relationship/key that
        /// genuinely ships in that solution. Callers merge this map's result with
        /// LookupSolutions' via <see cref="MergeSolutionIds"/>.
        /// </summary>
        private Dictionary<Guid, List<Guid>> LookupEntityIncludeAllSolutions(IReadOnlyList<Guid> entityIds, SearchProgress progress)
        {
            var map = new Dictionary<Guid, List<Guid>>();
            if (entityIds == null || entityIds.Count == 0) return map;

            var entityComponentCode = ComponentTypeMap.Get(ComponentTypeKind.Entity).SolutionComponentTypeCode;
            var batches = QueryPaging.Chunk(entityIds, QueryPaging.BatchSize).ToList();

            foreach (var batch in batches)
            {
                progress.ThrowIfCancelled();

                var query = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("objectid", "solutionid", "rootcomponentbehavior"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("componenttype", ConditionOperator.Equal, entityComponentCode),
                            new ConditionExpression("objectid", ConditionOperator.In, batch.Cast<object>().ToArray())
                        }
                    },
                    PageInfo = new PagingInfo { PageNumber = 1, Count = PageSize }
                };

                foreach (var row in RetrieveAllPages(query, progress))
                {
                    var behavior = row.GetAttributeValue<OptionSetValue>("rootcomponentbehavior")?.Value;
                    if (behavior != 0) continue; // only "Include Subcomponents" implies fields/relationships/keys

                    var objectId = row.GetAttributeValue<Guid>("objectid");
                    var solutionRef = row.GetAttributeValue<EntityReference>("solutionid");
                    if (solutionRef == null) continue;

                    if (!map.TryGetValue(objectId, out var list))
                    {
                        list = new List<Guid>();
                        map[objectId] = list;
                    }

                    if (!list.Contains(solutionRef.Id))
                        list.Add(solutionRef.Id);
                }
            }

            return map;
        }

        private static List<Guid> MergeSolutionIds(List<Guid> a, List<Guid> b)
        {
            if (a == null && b == null) return new List<Guid>();
            if (b == null) return new List<Guid>(a);
            if (a == null) return new List<Guid>(b);

            var merged = new List<Guid>(a);
            foreach (var id in b)
                if (!merged.Contains(id)) merged.Add(id);
            return merged;
        }

        private List<Entity> RetrieveAllPages(QueryExpression query, SearchProgress progress) =>
            QueryPaging.RetrieveAllPages(_service, query, progress);
    }
}
