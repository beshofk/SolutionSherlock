using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Services
{
    /// <summary>
    /// The reverse of SearchEngine: given a specific solution, resolve every
    /// solutioncomponent row it contains back into human-readable details (Display
    /// Name, Name, Type, State, Customizable, Description) - the classic Solution
    /// Explorer "Components" grid, reproduced from the API.
    ///
    /// Resolution strategy per component type:
    /// - Entity, Option Set: filtered out of MetadataResolver's already-cached full
    ///   lists (ResolveEntities(null) / ResolveGlobalOptionSets(null)) - zero extra
    ///   server calls beyond whatever warmed those caches.
    /// - Every other "IsDataRecord" type in ComponentTypeMap (Web Resource, Process,
    ///   Security Role, Report, templates, Form, View, Chart, ...): a single batched
    ///   QueryExpression per type, filtering "{table}id IN (ids)" - the standard
    ///   Dataverse primary-key-column naming convention.
    /// - systemform (componenttype 60) and workflow (componenttype 29) each host TWO
    ///   different logical kinds (Form/Dashboard, and Process/Business Rule/Dialog/
    ///   Action/BPF/Flow respectively) distinguished by an internal column, not by
    ///   componenttype - handled as special cases so a Dashboard doesn't get
    ///   mislabeled "Form" or vice versa.
    /// - Field, Relationship, Key (componenttype 2, 10, 14) appearing as STANDALONE
    ///   solution components (rather than implicitly included via their parent
    ///   Entity's "all objects" setting) can't be resolved back to a name without
    ///   first knowing which entity they belong to, which solutioncomponent doesn't
    ///   record directly. These are listed with their type and ID but not a resolved
    ///   name - see README "Solution component roster: known limitations".
    /// </summary>
    public class SolutionExplorerService
    {
        private readonly IOrganizationService _service;
        private readonly MetadataResolver _metadataResolver;

        public SolutionExplorerService(IOrganizationService service, MetadataResolver metadataResolver)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _metadataResolver = metadataResolver ?? throw new ArgumentNullException(nameof(metadataResolver));
        }

        public List<SolutionComponentDetail> ListComponents(Models.SolutionInfo solution, SearchProgress progress = null)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            progress = progress ?? SearchProgress.None;

            var results = new List<SolutionComponentDetail>();

            progress.Log($"Loading components for solution '{solution.FriendlyName}'...");
            var sw = Stopwatch.StartNew();
            var rows = RetrieveSolutionComponentRows(solution.SolutionId, progress);
            progress.Log($"Found {rows.Count} component row(s) in {sw.ElapsedMilliseconds}ms.");

            var byCode = rows.GroupBy(r => r.Code).ToDictionary(g => g.Key, g => g.Select(r => r.ObjectId).ToList());

            foreach (var kvp in byCode)
            {
                progress.ThrowIfCancelled();
                var code = kvp.Key;
                var ids = kvp.Value;

                if (code == 1) { results.AddRange(ResolveEntityComponents(ids, solution)); continue; }
                if (code == 9) { results.AddRange(ResolveOptionSetComponents(ids, solution)); continue; }
                if (code == 60) { results.AddRange(ResolveSystemFormComponents(ids, solution, progress)); continue; }
                if (code == 29) { results.AddRange(ResolveWorkflowComponents(ids, solution, progress)); continue; }
                if (code == 2) { results.AddRange(ResolveFieldComponents(ids, solution, progress)); continue; }
                if (code == 10) { results.AddRange(ResolveRelationshipComponents(ids, solution, progress)); continue; }
                if (code == 14) { results.AddRange(ResolveKeyComponents(ids, solution, progress)); continue; }

                var typeInfo = ComponentTypeMap.All.Values.FirstOrDefault(t => t.SolutionComponentTypeCode == code && t.IsDataRecord);
                results.AddRange(typeInfo != null
                    ? ResolveDataRecordComponents(typeInfo, ids, solution, progress)
                    : ResolveUnknownComponents(code, ids, solution));
            }

            // Entities added with "Include Subcomponents" (all objects, rootcomponentbehavior
            // == 0) get no individual solutioncomponent rows for their Fields/Relationships/Keys
            // - Dataverse relies solely on the entity's own row to imply "everything" - so
            // without this they'd never appear under that entity's group. Resolve them
            // directly from full entity metadata instead (mirrors the equivalent Search fix).
            var includeAllEntityIds = rows.Where(r => r.Code == 1 && r.RootComponentBehavior == 0).Select(r => r.ObjectId).ToList();
            if (includeAllEntityIds.Count > 0)
            {
                var alreadyResolvedIds = new HashSet<Guid>(results.Select(r => r.ObjectId));
                results.AddRange(ResolveImplicitEntitySubComponents(includeAllEntityIds, alreadyResolvedIds, solution, progress));
            }

            progress.Log($"Resolved {results.Count} component detail row(s) total.");
            return results;
        }

        private List<(int Code, Guid ObjectId, int? RootComponentBehavior)> RetrieveSolutionComponentRows(Guid solutionId, SearchProgress progress)
        {
            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "componenttype", "rootcomponentbehavior"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId) }
                },
                PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
            };

            return QueryPaging.RetrieveAllPages(_service, query, progress)
                .Select(row => (
                    Code: row.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? -1,
                    ObjectId: row.GetAttributeValue<Guid>("objectid"),
                    RootComponentBehavior: row.GetAttributeValue<OptionSetValue>("rootcomponentbehavior")?.Value))
                .Where(r => r.Code >= 0)
                .ToList();
        }

        private List<SolutionComponentDetail> ResolveEntityComponents(List<Guid> ids, Models.SolutionInfo solution)
        {
            var idSet = new HashSet<Guid>(ids);
            var results = _metadataResolver.ResolveEntities(null)
                .Where(e => e.MetadataId.HasValue && idSet.Contains(e.MetadataId.Value))
                .Select(e => new SolutionComponentDetail
                {
                    DisplayName = e.GetDisplayLabel(),
                    Name = e.LogicalName,
                    TypeDisplay = "Entity",
                    State = solution.IsManaged ? "Managed" : "Unmanaged",
                    Customizable = (e.IsCustomizable?.Value ?? true) ? "True" : "False",
                    Description = e.Description?.UserLocalizedLabel?.Label,
                    // An Entity component is its own parent for grouping purposes -
                    // this is what makes its own row anchor the top of its entity's
                    // group alongside its sub-components (Fields, Forms, ...).
                    ParentEntityLogicalName = e.LogicalName,
                    ParentEntityDisplayName = e.GetDisplayLabel(),
                    ComponentTypeCode = 1,
                    ObjectId = e.MetadataId.Value
                }).ToList();

            // Hierarchy Settings isn't a real solutioncomponent type - it's derived from
            // a self-referencing 1:N relationship flagged IsHierarchical on the entity.
            // Only added when that relationship actually exists (never guessed).
            foreach (var entityRow in results.ToList())
            {
                var hierarchyRelationship = _metadataResolver.ResolveHierarchyRelationship(entityRow.Name);
                if (hierarchyRelationship == null) continue;

                results.Add(new SolutionComponentDetail
                {
                    DisplayName = "Hierarchy Settings",
                    Name = hierarchyRelationship.SchemaName,
                    TypeDisplay = "Hierarchy Settings",
                    State = solution.IsManaged ? "Managed" : "Unmanaged",
                    Customizable = "—",
                    Description = null,
                    ParentEntityLogicalName = entityRow.Name,
                    ParentEntityDisplayName = entityRow.DisplayName,
                    ComponentTypeCode = 10,
                    ObjectId = hierarchyRelationship.MetadataId ?? Guid.Empty
                });
            }

            return results;
        }

        /// <summary>
        /// Synthesizes Field/Relationship/Key/Form/View/Chart/Business Rule rows for
        /// entities that were added to the solution with "Include Subcomponents" ("all
        /// objects") - rootcomponentbehavior == 0 - since Dataverse never writes
        /// individual solutioncomponent rows for those in that case (only the entity's
        /// own row exists). Field/Relationship/Key come from a full RetrieveEntityRequest
        /// per entity; Form/View/Chart/Business Rule are ordinary table rows so they're
        /// queried directly, scoped to the entity, same as their explicit-row
        /// counterparts (ResolveSystemFormComponents/ResolveWorkflowComponents/
        /// ResolveDataRecordComponents). alreadyResolvedIds guards against duplicating
        /// anything that (rarely) also has its own explicit solutioncomponent row.
        /// Dashboards are NOT included here - they're a main type, not entity-scoped,
        /// so they're unaffected by an entity's rootcomponentbehavior.
        /// </summary>
        private List<SolutionComponentDetail> ResolveImplicitEntitySubComponents(
            List<Guid> entityMetadataIds, HashSet<Guid> alreadyResolvedIds, Models.SolutionInfo solution, SearchProgress progress)
        {
            var results = new List<SolutionComponentDetail>();
            var idSet = new HashSet<Guid>(entityMetadataIds);
            var logicalNames = _metadataResolver.ResolveEntities(null)
                .Where(e => e.MetadataId.HasValue && idSet.Contains(e.MetadataId.Value))
                .Select(e => e.LogicalName)
                .ToList();

            foreach (var logicalName in logicalNames)
            {
                progress.ThrowIfCancelled();

                EntityMetadata full;
                try
                {
                    var response = (RetrieveEntityResponse)_service.Execute(new RetrieveEntityRequest
                    {
                        LogicalName = logicalName,
                        EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                        RetrieveAsIfPublished = true
                    });
                    full = response.EntityMetadata;
                }
                catch
                {
                    continue; // best-effort; the entity's own row still shows even if this fails
                }
                if (full == null) continue;

                var entityDisplayName = full.GetDisplayLabel();

                foreach (var attr in full.Attributes ?? Array.Empty<AttributeMetadata>())
                {
                    if (attr.MetadataId.HasValue && alreadyResolvedIds.Contains(attr.MetadataId.Value)) continue;
                    results.Add(new SolutionComponentDetail
                    {
                        DisplayName = attr.GetDisplayLabel(),
                        Name = attr.LogicalName,
                        TypeDisplay = "Field",
                        State = solution.IsManaged ? "Managed" : "Unmanaged",
                        Customizable = (attr.IsCustomizable?.Value ?? true) ? "True" : "False",
                        Description = attr.Description?.UserLocalizedLabel?.Label,
                        ParentEntityLogicalName = logicalName,
                        ParentEntityDisplayName = entityDisplayName,
                        ComponentTypeCode = 2,
                        ObjectId = attr.MetadataId ?? Guid.Empty
                    });
                }

                foreach (var rel in full.OneToManyRelationships ?? Array.Empty<OneToManyRelationshipMetadata>())
                {
                    if (rel.MetadataId.HasValue && alreadyResolvedIds.Contains(rel.MetadataId.Value)) continue;
                    results.Add(BuildImplicitRelationshipDetail(rel.SchemaName, rel.MetadataId, "1:N Relationship", logicalName, entityDisplayName, solution));
                }

                foreach (var rel in full.ManyToOneRelationships ?? Array.Empty<OneToManyRelationshipMetadata>())
                {
                    if (rel.MetadataId.HasValue && alreadyResolvedIds.Contains(rel.MetadataId.Value)) continue;
                    results.Add(BuildImplicitRelationshipDetail(rel.SchemaName, rel.MetadataId, "N:1 Relationship", logicalName, entityDisplayName, solution));
                }

                foreach (var rel in full.ManyToManyRelationships ?? Array.Empty<ManyToManyRelationshipMetadata>())
                {
                    if (rel.MetadataId.HasValue && alreadyResolvedIds.Contains(rel.MetadataId.Value)) continue;
                    results.Add(BuildImplicitRelationshipDetail(rel.SchemaName, rel.MetadataId, "N:N Relationship", logicalName, entityDisplayName, solution));
                }

                foreach (var key in full.Keys ?? Array.Empty<EntityKeyMetadata>())
                {
                    if (key.MetadataId.HasValue && alreadyResolvedIds.Contains(key.MetadataId.Value)) continue;
                    results.Add(new SolutionComponentDetail
                    {
                        DisplayName = key.GetDisplayLabel(),
                        Name = key.LogicalName,
                        TypeDisplay = "Key",
                        State = solution.IsManaged ? "Managed" : "Unmanaged",
                        Customizable = "—",
                        Description = null,
                        ParentEntityLogicalName = logicalName,
                        ParentEntityDisplayName = entityDisplayName,
                        ComponentTypeCode = 14,
                        ObjectId = key.MetadataId ?? Guid.Empty
                    });
                }

                results.AddRange(ResolveImplicitFormsAndViews(logicalName, entityDisplayName, alreadyResolvedIds, solution, progress));
            }

            return results;
        }

        /// <summary>
        /// Forms, Views, Charts and Business Rules for an entity added with "all
        /// objects" are ordinary table rows, unlike Field/Relationship/Key - so
        /// they're queried directly rather than pulled from metadata. Dashboards are
        /// deliberately excluded (systemform type == 0) - see the caller's remarks.
        /// </summary>
        private List<SolutionComponentDetail> ResolveImplicitFormsAndViews(
            string logicalName, string entityDisplayName, HashSet<Guid> alreadyResolvedIds, Models.SolutionInfo solution, SearchProgress progress)
        {
            var results = new List<SolutionComponentDetail>();

            var formQuery = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("name", "type"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("objecttypecode", ConditionOperator.Equal, logicalName),
                        new ConditionExpression("type", ConditionOperator.NotEqual, 0)
                    }
                },
                PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
            };
            foreach (var form in QueryPaging.RetrieveAllPages(_service, formQuery, progress))
            {
                if (alreadyResolvedIds.Contains(form.Id)) continue;
                var name = form.GetAttributeValue<string>("name");
                results.Add(new SolutionComponentDetail
                {
                    DisplayName = name,
                    Name = name,
                    TypeDisplay = "Form",
                    State = solution.IsManaged ? "Managed" : "Unmanaged",
                    Customizable = "—",
                    Description = null,
                    ParentEntityLogicalName = logicalName,
                    ParentEntityDisplayName = entityDisplayName,
                    ComponentTypeCode = 60,
                    ObjectId = form.Id
                });
            }

            var viewQuery = new QueryExpression("savedquery")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("returnedtypecode", ConditionOperator.Equal, logicalName) }
                },
                PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
            };
            foreach (var view in QueryPaging.RetrieveAllPages(_service, viewQuery, progress))
            {
                if (alreadyResolvedIds.Contains(view.Id)) continue;
                var name = view.GetAttributeValue<string>("name");
                results.Add(new SolutionComponentDetail
                {
                    DisplayName = name,
                    Name = name,
                    TypeDisplay = "View",
                    State = solution.IsManaged ? "Managed" : "Unmanaged",
                    Customizable = "—",
                    Description = null,
                    ParentEntityLogicalName = logicalName,
                    ParentEntityDisplayName = entityDisplayName,
                    ComponentTypeCode = 26,
                    ObjectId = view.Id
                });
            }

            var chartQuery = new QueryExpression("savedqueryvisualization")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("primaryentitytypecode", ConditionOperator.Equal, logicalName) }
                },
                PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
            };
            foreach (var chart in QueryPaging.RetrieveAllPages(_service, chartQuery, progress))
            {
                if (alreadyResolvedIds.Contains(chart.Id)) continue;
                var name = chart.GetAttributeValue<string>("name");
                results.Add(new SolutionComponentDetail
                {
                    DisplayName = name,
                    Name = name,
                    TypeDisplay = "Chart",
                    State = solution.IsManaged ? "Managed" : "Unmanaged",
                    Customizable = "—",
                    Description = null,
                    ParentEntityLogicalName = logicalName,
                    ParentEntityDisplayName = entityDisplayName,
                    ComponentTypeCode = 59,
                    ObjectId = chart.Id
                });
            }

            var businessRuleQuery = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("name", "description"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("primaryentity", ConditionOperator.Equal, logicalName),
                        new ConditionExpression("category", ConditionOperator.Equal, 2)
                    }
                },
                PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
            };
            foreach (var rule in QueryPaging.RetrieveAllPages(_service, businessRuleQuery, progress))
            {
                if (alreadyResolvedIds.Contains(rule.Id)) continue;
                var name = rule.GetAttributeValue<string>("name");
                results.Add(new SolutionComponentDetail
                {
                    DisplayName = name,
                    Name = name,
                    TypeDisplay = "Business Rule",
                    State = solution.IsManaged ? "Managed" : "Unmanaged",
                    Customizable = "—",
                    Description = rule.GetAttributeValue<string>("description"),
                    ParentEntityLogicalName = logicalName,
                    ParentEntityDisplayName = entityDisplayName,
                    ComponentTypeCode = 29,
                    ObjectId = rule.Id
                });
            }

            return results;
        }

        private SolutionComponentDetail BuildImplicitRelationshipDetail(
            string schemaName, Guid? metadataId, string typeDisplay, string owningEntityLogicalName, string owningEntityDisplayName, Models.SolutionInfo solution)
        {
            return new SolutionComponentDetail
            {
                DisplayName = schemaName,
                Name = schemaName,
                TypeDisplay = typeDisplay,
                State = solution.IsManaged ? "Managed" : "Unmanaged",
                Customizable = "—",
                Description = null,
                ParentEntityLogicalName = owningEntityLogicalName,
                ParentEntityDisplayName = owningEntityDisplayName,
                ComponentTypeCode = 10,
                ObjectId = metadataId ?? Guid.Empty
            };
        }

        private List<SolutionComponentDetail> ResolveOptionSetComponents(List<Guid> ids, Models.SolutionInfo solution)
        {
            var idSet = new HashSet<Guid>(ids);
            return _metadataResolver.ResolveGlobalOptionSets(null)
                .Where(o => o.MetadataId.HasValue && idSet.Contains(o.MetadataId.Value))
                .Select(o => new SolutionComponentDetail
                {
                    DisplayName = o.DisplayName?.UserLocalizedLabel?.Label ?? o.Name,
                    Name = o.Name,
                    TypeDisplay = "Option Set",
                    State = solution.IsManaged ? "Managed" : "Unmanaged",
                    Customizable = "—",
                    Description = o.Description?.UserLocalizedLabel?.Label,
                    ComponentTypeCode = 9,
                    ObjectId = o.MetadataId.Value
                }).ToList();
        }

        /// <summary>
        /// systemform hosts both Forms and Dashboards, distinguished by the "type"
        /// column rather than componenttype - type == 0 is the long-standing
        /// convention for Dashboard (see README for the same caveat noted where
        /// Dashboard is registered as a main search type).
        /// </summary>
        private List<SolutionComponentDetail> ResolveSystemFormComponents(List<Guid> ids, Models.SolutionInfo solution, SearchProgress progress)
        {
            var results = new List<SolutionComponentDetail>();

            // Resolve the primary id attribute for systemform via metadata (do once per method)
            // Also retrieve attribute metadata so we only request columns that actually exist
            string idAttribute = "formid"; // fallback
            var availableAttrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var retrieveReq = new RetrieveEntityRequest
                {
                    LogicalName = "systemform",
                    EntityFilters = EntityFilters.Entity | EntityFilters.Attributes
                };
                var retrieveResp = (RetrieveEntityResponse)_service.Execute(retrieveReq);
                if (retrieveResp?.EntityMetadata != null)
                {
                    if (!string.IsNullOrEmpty(retrieveResp.EntityMetadata.PrimaryIdAttribute))
                        idAttribute = retrieveResp.EntityMetadata.PrimaryIdAttribute;

                    if (retrieveResp.EntityMetadata.Attributes != null)
                    {
                        foreach (var a in retrieveResp.EntityMetadata.Attributes)
                            availableAttrs.Add(a.LogicalName);
                    }
                }
            }
            catch
            {
                // Swallow metadata retrieval errors and fall back to the conventional name
                // availableAttrs will stay empty, and we'll only request a conservative set of columns below
            }

            foreach (var batch in QueryPaging.Chunk(ids))
            {
                progress.ThrowIfCancelled();

                // Build ColumnSet only with attributes that exist in this org/version
                var cols = new List<string> { "name", "type", "objecttypecode" };
                if (availableAttrs.Contains("createdon")) cols.Add("createdon");
                if (availableAttrs.Contains("modifiedon")) cols.Add("modifiedon");

                var query = new QueryExpression("systemform")
                {
                    ColumnSet = new ColumnSet(cols.ToArray()),
                    Criteria = new FilterExpression
                    {
                        Conditions = { new ConditionExpression(idAttribute, ConditionOperator.In, batch.Cast<object>().ToArray()) }
                    },
                    PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
                };

                foreach (var record in QueryPaging.RetrieveAllPages(_service, query, progress))
                {
                    var name = record.GetAttributeValue<string>("name");
                    var formType = record.GetAttributeValue<OptionSetValue>("type")?.Value;
                    // Only Forms are meaningfully entity-scoped for grouping purposes -
                    // most Dashboards won't have objecttypecode set at all, in which
                    // case this naturally stays null and the row falls back to
                    // grouping by TypeDisplay ("Dashboard"), same as before.
                    var parentEntityLogicalName = record.GetAttributeValue<string>("objecttypecode");

                    results.Add(new SolutionComponentDetail
                    {
                        DisplayName = name,
                        Name = name,
                        TypeDisplay = formType == 0 ? "Dashboard" : "Form",
                        State = solution.IsManaged ? "Managed" : "Unmanaged",
                        Customizable = "—",
                        Description = null,
                        CreatedOn = record.GetAttributeValue<DateTime?>("createdon"),
                        ModifiedOn = record.GetAttributeValue<DateTime?>("modifiedon"),
                        ParentEntityLogicalName = parentEntityLogicalName,
                        ParentEntityDisplayName = ResolveEntityDisplayName(parentEntityLogicalName),
                        ComponentTypeCode = 60,
                        ObjectId = record.Id
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// workflow hosts Process (Workflow/Dialog/Action/Business Process Flow/Modern
        /// Flow/Desktop Flow) and Business Rule, distinguished by the "category"
        /// column - same standard SDK OptionSets.WorkflowCategory values used by
        /// ProcessCategories.
        /// </summary>
        private List<SolutionComponentDetail> ResolveWorkflowComponents(List<Guid> ids, Models.SolutionInfo solution, SearchProgress progress)
        {
            var results = new List<SolutionComponentDetail>();

            foreach (var batch in QueryPaging.Chunk(ids))
            {
                progress.ThrowIfCancelled();

                var query = new QueryExpression("workflow")
                {
                    ColumnSet = new ColumnSet("name", "category", "description", "primaryentity", "createdon", "modifiedon"),
                    Criteria = new FilterExpression
                    {
                        Conditions = { new ConditionExpression("workflowid", ConditionOperator.In, batch.Cast<object>().ToArray()) }
                    },
                    PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
                };

                foreach (var record in QueryPaging.RetrieveAllPages(_service, query, progress))
                {
                    var name = record.GetAttributeValue<string>("name");
                    var category = record.GetAttributeValue<OptionSetValue>("category")?.Value;
                    var isBusinessRule = category == 2;

                    // Only Business Rule groups by parent entity - it's the one
                    // ComponentTypeMap.SubTypesByMain[Entity] member that lives in this
                    // table. Regular Process rows (category != 2) keep grouping by
                    // TypeDisplay ("Process (Workflow)", etc.) as before - Process is a
                    // main-level type, not an entity sub-component, even though most
                    // processes do have a primaryentity.
                    var parentEntityLogicalName = isBusinessRule ? record.GetAttributeValue<string>("primaryentity") : null;

                    results.Add(new SolutionComponentDetail
                    {
                        DisplayName = name,
                        Name = name,
                        TypeDisplay = LabelForWorkflowCategory(category),
                        State = solution.IsManaged ? "Managed" : "Unmanaged",
                        Customizable = "—",
                        Description = record.GetAttributeValue<string>("description"),
                        CreatedOn = record.GetAttributeValue<DateTime?>("createdon"),
                        ModifiedOn = record.GetAttributeValue<DateTime?>("modifiedon"),
                        ParentEntityLogicalName = parentEntityLogicalName,
                        ParentEntityDisplayName = ResolveEntityDisplayName(parentEntityLogicalName),
                        ComponentTypeCode = 29,
                        ObjectId = record.Id
                    });
                }
            }

            return results;
        }

        private static string LabelForWorkflowCategory(int? category)
        {
            switch (category)
            {
                case 0: return "Process (Workflow)";
                case 1: return "Process (Dialog)";
                case 2: return "Business Rule";
                case 3: return "Process (Action)";
                case 4: return "Process (Business Process Flow)";
                case 5: return "Process (Modern Flow)";
                case 6: return "Process (Desktop Flow)";
                default: return "Process";
            }
        }

        /// <summary>
        /// Generic path for every other IsDataRecord component type - one batched
        /// query per type, filtering on the standard "{table}id" primary key column.
        /// </summary>
        private List<SolutionComponentDetail> ResolveDataRecordComponents(
            ComponentTypeInfo typeInfo, List<Guid> ids, Models.SolutionInfo solution, SearchProgress progress)
        {
            var results = new List<SolutionComponentDetail>();
            var idAttribute = typeInfo.EntityLogicalName + "id";

            // createdon/modifiedon are standard columns on every Dataverse table
            // (out-of-box or custom), so unlike PrimaryNameAttribute/DescriptionAttribute
            // these don't need a per-type opt-in - safe to request unconditionally.
            var columns = new List<string> { typeInfo.PrimaryNameAttribute, "createdon", "modifiedon" };
            if (!string.IsNullOrEmpty(typeInfo.DescriptionAttribute))
                columns.Add(typeInfo.DescriptionAttribute);
            if (!string.IsNullOrEmpty(typeInfo.ParentEntityAttribute))
                columns.Add(typeInfo.ParentEntityAttribute);

            foreach (var batch in QueryPaging.Chunk(ids))
            {
                progress.ThrowIfCancelled();

                var query = new QueryExpression(typeInfo.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(columns.ToArray()),
                    Criteria = new FilterExpression
                    {
                        Conditions = { new ConditionExpression(idAttribute, ConditionOperator.In, batch.Cast<object>().ToArray()) }
                    },
                    PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
                };

                foreach (var record in QueryPaging.RetrieveAllPages(_service, query, progress))
                {
                    var name = record.GetAttributeValue<string>(typeInfo.PrimaryNameAttribute);
                    var description = !string.IsNullOrEmpty(typeInfo.DescriptionAttribute)
                        ? record.GetAttributeValue<string>(typeInfo.DescriptionAttribute)
                        : null;
                    var parentEntityLogicalName = !string.IsNullOrEmpty(typeInfo.ParentEntityAttribute)
                        ? record.GetAttributeValue<string>(typeInfo.ParentEntityAttribute)
                        : null;

                    results.Add(new SolutionComponentDetail
                    {
                        DisplayName = name,
                        Name = name,
                        TypeDisplay = typeInfo.DisplayName,
                        State = solution.IsManaged ? "Managed" : "Unmanaged",
                        Customizable = "—",
                        Description = description,
                        CreatedOn = record.GetAttributeValue<DateTime?>("createdon"),
                        ModifiedOn = record.GetAttributeValue<DateTime?>("modifiedon"),
                        ParentEntityLogicalName = parentEntityLogicalName,
                        ParentEntityDisplayName = ResolveEntityDisplayName(parentEntityLogicalName),
                        ComponentTypeCode = typeInfo.SolutionComponentTypeCode,
                        ObjectId = record.Id
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// <summary>
        /// Resolves standalone Field components (added to the solution individually,
        /// rather than implicitly via their parent Entity's "all objects" setting) to
        /// their real display name, logical name, and parent entity - via
        /// MetadataResolver.ResolveAttributesByMetadataId, which searches across every
        /// entity in the org for matching AttributeMetadata in a bounded number of
        /// calls. Once resolved, these group under their parent entity exactly like
        /// Forms/Views/Charts/Business Rules do.
        ///
        /// Created On / Modified On stay null here - unlike Forms/Views/Web Resources
        /// (ordinary table rows with standard audit columns), attribute metadata has
        /// no createdon/modifiedon exposed via the metadata API. This is a genuine
        /// data-availability limit, not an oversight - the UI shows "—" for these two
        /// fields on Field rows for that reason.
        /// </summary>
        private List<SolutionComponentDetail> ResolveFieldComponents(List<Guid> ids, Models.SolutionInfo solution, SearchProgress progress)
        {
            var attributeComponentCode = ComponentTypeMap.Get(ComponentTypeKind.Attribute).SolutionComponentTypeCode;
            var resolved = _metadataResolver.ResolveAttributesByMetadataId(ids, progress);

            var results = resolved.Select(r => new SolutionComponentDetail
            {
                DisplayName = r.Attribute.GetDisplayLabel(),
                Name = r.Attribute.LogicalName,
                TypeDisplay = "Field",
                State = solution.IsManaged ? "Managed" : "Unmanaged",
                Customizable = (r.Attribute.IsCustomizable?.Value ?? true) ? "True" : "False",
                Description = r.Attribute.Description?.UserLocalizedLabel?.Label,
                ParentEntityLogicalName = r.ParentEntityLogicalName,
                ParentEntityDisplayName = ResolveEntityDisplayName(r.ParentEntityLogicalName),
                ComponentTypeCode = attributeComponentCode,
                ObjectId = r.Attribute.MetadataId ?? Guid.Empty
            }).ToList();

            // Extremely rare, but guard against it anyway: an id RetrieveMetadataChangesRequest
            // didn't return anything for (e.g. the field was deleted after the solution
            // snapshot was taken) falls back to the same honest placeholder used for
            // Relationship/Key, rather than silently vanishing from the count.
            var resolvedIds = new HashSet<Guid>(resolved.Where(r => r.Attribute.MetadataId.HasValue).Select(r => r.Attribute.MetadataId.Value));
            var missing = ids.Where(id => !resolvedIds.Contains(id)).ToList();
            if (missing.Count > 0)
                results.AddRange(BuildUnresolvedPlaceholders(attributeComponentCode, "Field", missing, solution));

            return results;
        }

        /// <summary>
        /// Resolves standalone Relationship solutioncomponent rows (componenttype 10) to
        /// schema name, 1:N/N:1/N:N type, and owning entity/entities via
        /// MetadataResolver.ResolveRelationshipsByMetadataId. A relationship whose
        /// referenced and referencing entities both resolve produces two detail rows -
        /// one under each participating entity, mirroring Solution Explorer, sharing the
        /// same GUID (not a same-entity duplicate). Ids that can't be resolved fall back
        /// to the existing unresolved placeholder.
        /// </summary>
        private List<SolutionComponentDetail> ResolveRelationshipComponents(List<Guid> ids, Models.SolutionInfo solution, SearchProgress progress)
        {
            var results = new List<SolutionComponentDetail>();
            var resolved = _metadataResolver.ResolveRelationshipsByMetadataId(ids, progress);

            foreach (var r in resolved)
            {
                if (r.RelationshipTypeLabel == "N:N")
                {
                    foreach (var entityLogicalName in new[] { r.Entity1LogicalName, r.Entity2LogicalName }.Where(n => !string.IsNullOrEmpty(n)).Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        results.Add(BuildRelationshipDetail(r, "N:N Relationship", entityLogicalName, solution));
                    }
                    continue;
                }

                // "1:N" entries come from the referenced (one) side entity, "N:1" entries
                // from the referencing (many) side entity - each entry already carries the
                // correct owning entity for its own label.
                var owningEntity = r.RelationshipTypeLabel == "1:N" ? r.ReferencedEntityLogicalName : r.ReferencingEntityLogicalName;
                if (!string.IsNullOrEmpty(owningEntity))
                {
                    results.Add(BuildRelationshipDetail(r, $"{r.RelationshipTypeLabel} Relationship", owningEntity, solution));
                }
            }

            var resolvedIds = new HashSet<Guid>(resolved.Where(r => r.MetadataId.HasValue).Select(r => r.MetadataId.Value));
            var missing = ids.Where(id => !resolvedIds.Contains(id)).ToList();
            if (missing.Count > 0)
                results.AddRange(BuildUnresolvedPlaceholders(10, "Relationship", missing, solution));

            return results;
        }

        private SolutionComponentDetail BuildRelationshipDetail(RelationshipInfo r, string typeDisplay, string owningEntityLogicalName, Models.SolutionInfo solution)
        {
            return new SolutionComponentDetail
            {
                DisplayName = r.SchemaName,
                Name = r.SchemaName,
                TypeDisplay = typeDisplay,
                State = solution.IsManaged ? "Managed" : "Unmanaged",
                Customizable = "—",
                Description = null,
                ParentEntityLogicalName = owningEntityLogicalName,
                ParentEntityDisplayName = ResolveEntityDisplayName(owningEntityLogicalName),
                ComponentTypeCode = 10,
                ObjectId = r.MetadataId ?? Guid.Empty
            };
        }

        /// <summary>
        /// Resolves standalone Key solutioncomponent rows (componenttype 14) to their key
        /// metadata and single owning entity via MetadataResolver.ResolveKeysByMetadataId.
        /// Ids that can't be resolved fall back to the existing unresolved placeholder.
        /// </summary>
        private List<SolutionComponentDetail> ResolveKeyComponents(List<Guid> ids, Models.SolutionInfo solution, SearchProgress progress)
        {
            var results = new List<SolutionComponentDetail>();
            var resolved = _metadataResolver.ResolveKeysByMetadataId(ids, progress);

            foreach (var r in resolved)
            {
                results.Add(new SolutionComponentDetail
                {
                    DisplayName = r.Key.GetDisplayLabel(),
                    Name = r.Key.LogicalName,
                    TypeDisplay = "Key",
                    State = solution.IsManaged ? "Managed" : "Unmanaged",
                    Customizable = "—",
                    Description = null,
                    ParentEntityLogicalName = r.ParentEntityLogicalName,
                    ParentEntityDisplayName = ResolveEntityDisplayName(r.ParentEntityLogicalName),
                    ComponentTypeCode = 14,
                    ObjectId = r.Key.MetadataId ?? Guid.Empty
                });
            }

            var resolvedIds = new HashSet<Guid>(resolved.Where(r => r.Key.MetadataId.HasValue).Select(r => r.Key.MetadataId.Value));
            var missing = ids.Where(id => !resolvedIds.Contains(id)).ToList();
            if (missing.Count > 0)
                results.AddRange(BuildUnresolvedPlaceholders(14, "Key", missing, solution));

            return results;
        }

        private static List<SolutionComponentDetail> BuildUnresolvedPlaceholders(int code, string typeLabel, List<Guid> ids, Models.SolutionInfo solution)
        {
            return ids.Select(id => new SolutionComponentDetail
            {
                DisplayName = $"({typeLabel} — search by parent Entity to see details)",
                Name = id.ToString(),
                TypeDisplay = typeLabel,
                State = solution.IsManaged ? "Managed" : "Unmanaged",
                Customizable = "—",
                Description = null,
                ComponentTypeCode = code,
                ObjectId = id
            }).ToList();
        }

        private List<SolutionComponentDetail> ResolveUnknownComponents(int code, List<Guid> ids, Models.SolutionInfo solution)
        {
            return ids.Select(id => new SolutionComponentDetail
            {
                DisplayName = $"(Unrecognized component type {code})",
                Name = id.ToString(),
                TypeDisplay = $"Type {code}",
                State = solution.IsManaged ? "Managed" : "Unmanaged",
                Customizable = "—",
                Description = null,
                ComponentTypeCode = code,
                ObjectId = id
            }).ToList();
        }

        /// <summary>Looks up an entity's display label by logical name from MetadataResolver's already-cached entity list - no extra server call.</summary>
        private string ResolveEntityDisplayName(string logicalName)
        {
            if (string.IsNullOrEmpty(logicalName)) return null;

            return _metadataResolver.GetEntityDisplayLabel(logicalName);
        }
    }
}
