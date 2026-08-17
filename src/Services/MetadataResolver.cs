using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Services
{
    /// <summary>
    /// Wraps entity/attribute/key/relationship/global-option-set metadata retrieval
    /// and caches the lightweight lists for the lifetime of a connection, since a
    /// full metadata pull is the single most expensive call this tool makes.
    ///
    /// Deliberately uses RetrieveAllEntitiesRequest / RetrieveEntityRequest /
    /// RetrieveAllOptionSetsRequest rather than the newer RetrieveMetadataChangesRequest:
    /// all three have been supported since CRM 2011, which keeps this tool working
    /// unmodified against older On-Premises deployments as well as the current
    /// Online service.
    /// </summary>
    public class MetadataResolver
    {
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(15);

        private readonly IOrganizationService _service;

        private EntityMetadata[] _entityCache;
        private DateTime _entityCacheLoadedUtc = DateTime.MinValue;

        // Built alongside _entityCache (see GetAllEntitiesLightweight) so repeated
        // logical-name lookups (e.g. resolving a component row's parent entity
        // display name) are O(1) instead of a fresh linear scan/allocation per call.
        private Dictionary<string, EntityMetadata> _entityByLogicalName;

        private OptionSetMetadataBase[] _optionSetCache;
        private DateTime _optionSetCacheLoadedUtc = DateTime.MinValue;

        private EntityMetadata[] _relationshipCache;
        private DateTime _relationshipCacheLoadedUtc = DateTime.MinValue;

        private EntityMetadata[] _keyCache;
        private DateTime _keyCacheLoadedUtc = DateTime.MinValue;

        public MetadataResolver(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>Forces the next metadata read to hit the server again.</summary>
        public void InvalidateCache()
        {
            _entityCache = null;
            _entityByLogicalName = null;
            _optionSetCache = null;
            _relationshipCache = null;
            _keyCache = null;
        }

        private EntityMetadata[] GetAllEntitiesLightweight()
        {
            var stale = _entityCache == null || DateTime.UtcNow - _entityCacheLoadedUtc > CacheLifetime;
            if (!stale) return _entityCache;

            // EntityFilters.Entity returns entity-level metadata only (no attributes,
            // relationships, forms, etc.) - this keeps the call cheap even in large
            // orgs with thousands of custom entities.
            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = true
            };
            var response = (RetrieveAllEntitiesResponse)_service.Execute(request);
            _entityCache = response.EntityMetadata;
            _entityCacheLoadedUtc = DateTime.UtcNow;

            var byLogicalName = new Dictionary<string, EntityMetadata>(_entityCache.Length, StringComparer.OrdinalIgnoreCase);
            foreach (var entity in _entityCache)
            {
                if (!string.IsNullOrEmpty(entity.LogicalName))
                    byLogicalName[entity.LogicalName] = entity;
            }
            _entityByLogicalName = byLogicalName;

            return _entityCache;
        }

        /// <summary>
        /// O(1) lookup of a single entity's display label by logical name, backed by
        /// the same cached entity sweep as ResolveEntities/GetAllEntitiesLightweight -
        /// no extra server call, and no linear scan/allocation of the full entity list
        /// per call (unlike calling ResolveEntities(null).FirstOrDefault(...) would
        /// require). Returns null when the entity isn't found or logicalName is empty.
        /// </summary>
        public string GetEntityDisplayLabel(string logicalName)
        {
            if (string.IsNullOrEmpty(logicalName)) return null;
            GetAllEntitiesLightweight(); // ensures _entityByLogicalName is loaded/fresh
            return _entityByLogicalName.TryGetValue(logicalName, out var entity) ? entity.GetDisplayLabel() : null;
        }


        private OptionSetMetadataBase[] GetAllGlobalOptionSetsLightweight()
        {
            var stale = _optionSetCache == null || DateTime.UtcNow - _optionSetCacheLoadedUtc > CacheLifetime;
            if (!stale) return _optionSetCache;

            var request = new RetrieveAllOptionSetsRequest();
            var response = (RetrieveAllOptionSetsResponse)_service.Execute(request);
            _optionSetCache = response.OptionSetMetadata ?? new OptionSetMetadataBase[0];
            _optionSetCacheLoadedUtc = DateTime.UtcNow;
            return _optionSetCache;
        }

        /// <summary>
        /// One bulk sweep of every entity's relationship collections (used to reverse-
        /// resolve standalone Relationship solutioncomponent rows, whose objectid is a
        /// relationship MetadataId, back to schema name/type/owning entities - the same
        /// problem ResolveAttributesByMetadataId solves for Field). Cached like
        /// GetAllEntitiesLightweight since a full sweep is expensive and rarely changes
        /// mid-session.
        /// </summary>
        private EntityMetadata[] GetAllRelationshipsLightweight()
        {
            var stale = _relationshipCache == null || DateTime.UtcNow - _relationshipCacheLoadedUtc > CacheLifetime;
            if (!stale) return _relationshipCache;

            var query = new EntityQueryExpression
            {
                Properties = new MetadataPropertiesExpression(
                    "LogicalName", "OneToManyRelationships", "ManyToOneRelationships", "ManyToManyRelationships")
            };
            var request = new RetrieveMetadataChangesRequest { Query = query };
            var response = (RetrieveMetadataChangesResponse)_service.Execute(request);
            _relationshipCache = response.EntityMetadata?.ToArray() ?? new EntityMetadata[0];
            _relationshipCacheLoadedUtc = DateTime.UtcNow;
            return _relationshipCache;
        }

        /// <summary>Same bulk-sweep-and-cache pattern as relationships above, for entity keys.</summary>
        private EntityMetadata[] GetAllEntityKeysLightweight()
        {
            var stale = _keyCache == null || DateTime.UtcNow - _keyCacheLoadedUtc > CacheLifetime;
            if (!stale) return _keyCache;

            var query = new EntityQueryExpression
            {
                Properties = new MetadataPropertiesExpression("LogicalName", "Keys")
            };
            var request = new RetrieveMetadataChangesRequest { Query = query };
            var response = (RetrieveMetadataChangesResponse)_service.Execute(request);
            _keyCache = response.EntityMetadata?.ToArray() ?? new EntityMetadata[0];
            _keyCacheLoadedUtc = DateTime.UtcNow;
            return _keyCache;
        }

        /// <summary>
        /// Resolves standalone Relationship solutioncomponent rows (objectid = relationship
        /// MetadataId) back to schema name, 1:N/N:1/N:N type, participating entities, and
        /// whether the relationship is the entity's hierarchy (parent/child) relationship.
        /// Unlike ResolveRelationships(entityLogicalName, ...), this doesn't require knowing
        /// which entity owns the relationship up front.
        /// </summary>
        public List<RelationshipInfo> ResolveRelationshipsByMetadataId(IReadOnlyList<Guid> relationshipMetadataIds, SearchProgress progress = null)
        {
            var results = new List<RelationshipInfo>();
            if (relationshipMetadataIds == null || relationshipMetadataIds.Count == 0) return results;

            progress = progress ?? SearchProgress.None;
            var idSet = new HashSet<Guid>(relationshipMetadataIds);

            // Separate dedup sets per relationship "side": the same physical 1:N
            // relationship legitimately appears once as "1:N" (via the referenced/one-
            // side entity) and once as "N:1" (via the referencing/many-side entity) -
            // both are wanted. Only guards against the same side being listed twice.
            var seenOneToMany = new HashSet<Guid>();
            var seenManyToOne = new HashSet<Guid>();
            var seenManyToMany = new HashSet<Guid>();

            foreach (var entity in GetAllRelationshipsLightweight())
            {
                progress.ThrowIfCancelled();

                foreach (var r in entity.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
                {
                    if (!r.MetadataId.HasValue || !idSet.Contains(r.MetadataId.Value) || !seenOneToMany.Add(r.MetadataId.Value)) continue;
                    results.Add(new RelationshipInfo
                    {
                        MetadataId = r.MetadataId,
                        SchemaName = r.SchemaName,
                        RelationshipTypeLabel = "1:N",
                        ReferencedEntityLogicalName = r.ReferencedEntity,
                        ReferencingEntityLogicalName = r.ReferencingEntity,
                        IsHierarchical = r.IsHierarchical ?? false
                    });
                }

                foreach (var r in entity.ManyToOneRelationships ?? new OneToManyRelationshipMetadata[0])
                {
                    if (!r.MetadataId.HasValue || !idSet.Contains(r.MetadataId.Value) || !seenManyToOne.Add(r.MetadataId.Value)) continue;
                    results.Add(new RelationshipInfo
                    {
                        MetadataId = r.MetadataId,
                        SchemaName = r.SchemaName,
                        RelationshipTypeLabel = "N:1",
                        ReferencedEntityLogicalName = r.ReferencedEntity,
                        ReferencingEntityLogicalName = r.ReferencingEntity,
                        IsHierarchical = r.IsHierarchical ?? false
                    });
                }

                foreach (var r in entity.ManyToManyRelationships ?? new ManyToManyRelationshipMetadata[0])
                {
                    if (!r.MetadataId.HasValue || !idSet.Contains(r.MetadataId.Value) || !seenManyToMany.Add(r.MetadataId.Value)) continue;
                    results.Add(new RelationshipInfo
                    {
                        MetadataId = r.MetadataId,
                        SchemaName = r.SchemaName,
                        RelationshipTypeLabel = "N:N",
                        Entity1LogicalName = r.Entity1LogicalName,
                        Entity2LogicalName = r.Entity2LogicalName
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Resolves standalone Key solutioncomponent rows (objectid = key MetadataId) back
        /// to the key metadata and its single owning entity - the Key analogue of
        /// ResolveAttributesByMetadataId.
        /// </summary>
        public List<(EntityKeyMetadata Key, string ParentEntityLogicalName)> ResolveKeysByMetadataId(
            IReadOnlyList<Guid> keyMetadataIds, SearchProgress progress = null)
        {
            var results = new List<(EntityKeyMetadata, string)>();
            if (keyMetadataIds == null || keyMetadataIds.Count == 0) return results;

            progress = progress ?? SearchProgress.None;
            var idSet = new HashSet<Guid>(keyMetadataIds);

            foreach (var entity in GetAllEntityKeysLightweight())
            {
                progress.ThrowIfCancelled();

                foreach (var key in entity.Keys ?? new EntityKeyMetadata[0])
                {
                    if (key.MetadataId.HasValue && idSet.Contains(key.MetadataId.Value))
                        results.Add((key, entity.LogicalName));
                }
            }

            return results;
        }

        /// <summary>
        /// Looks up the self-referencing, IsHierarchical 1:N relationship for an entity (if
        /// any) - used to synthesize the "Hierarchy Settings" node in the View Components
        /// tree. Returns null when the entity has no hierarchy relationship configured.
        /// </summary>
        public RelationshipInfo ResolveHierarchyRelationship(string entityLogicalName)
        {
            if (string.IsNullOrEmpty(entityLogicalName)) return null;

            foreach (var entity in GetAllRelationshipsLightweight())
            {
                if (!string.Equals(entity.LogicalName, entityLogicalName, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var r in entity.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
                {
                    if (r.IsHierarchical == true &&
                        string.Equals(r.ReferencedEntity, entityLogicalName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(r.ReferencingEntity, entityLogicalName, StringComparison.OrdinalIgnoreCase))
                    {
                        return new RelationshipInfo
                        {
                            MetadataId = r.MetadataId,
                            SchemaName = r.SchemaName,
                            RelationshipTypeLabel = "1:N",
                            ReferencedEntityLogicalName = r.ReferencedEntity,
                            ReferencingEntityLogicalName = r.ReferencingEntity,
                            IsHierarchical = true
                        };
                    }
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Returns entities whose logical name or display label contains searchText
        /// (case-insensitive). An empty/blank searchText returns every entity - callers
        /// should combine this with a sub-component filter or warn the user before
        /// running an unbounded search in a large environment.
        /// </summary>
        public List<EntityMetadata> ResolveEntities(string searchText)
        {
            var all = GetAllEntitiesLightweight();
            if (string.IsNullOrWhiteSpace(searchText))
                return all.ToList();

            var text = searchText.Trim();
            return all.Where(e =>
                    (e.LogicalName?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (e.GetDisplayLabel()?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>
        /// Retrieves attributes for a single entity and filters by name/label. Only
        /// called for entities that already survived a main-type filter, which bounds
        /// the number of RetrieveEntityRequest calls to "matched entities", not "every
        /// entity in the org". Every other per-entity resolver below follows the same
        /// bounded-cost pattern.
        /// </summary>
        public List<AttributeMetadata> ResolveAttributes(string entityLogicalName, string searchText)
        {
            var request = new RetrieveEntityRequest
            {
                LogicalName = entityLogicalName,
                EntityFilters = EntityFilters.Attributes,
                RetrieveAsIfPublished = true
            };
            var response = (RetrieveEntityResponse)_service.Execute(request);
            var attributes = response.EntityMetadata.Attributes ?? new AttributeMetadata[0];

            if (string.IsNullOrWhiteSpace(searchText))
                return attributes.ToList();

            var text = searchText.Trim();
            return attributes.Where(a =>
                    (a.LogicalName?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (a.GetDisplayLabel()?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>
        /// Resolves a batch of Attribute MetadataIds back to their full
        /// AttributeMetadata AND parent entity logical name, without needing to know
        /// which entity each one belongs to ahead of time - unlike ResolveAttributes
        /// above, which requires the entity logical name as an input. Needed when a
        /// Field was added to a solution as a standalone component: solutioncomponent
        /// records the field's own MetadataId but not its parent entity.
        ///
        /// Uses RetrieveMetadataChangesRequest with an EntityQueryExpression whose
        /// AttributeQuery.Criteria filters by MetadataId - the officially documented
        /// way to search for specific attributes across every entity in the org in a
        /// bounded number of calls. The filter is built as an OR of per-id Equals
        /// conditions rather than a single MetadataConditionOperator.In condition:
        /// "In" does exist and is documented, but the expected value type for a
        /// multi-value list isn't clearly documented anywhere findable, whereas the
        /// OR/Equals form is a directly confirmed, working pattern from Microsoft's
        /// own sample code - correctness over conciseness here.
        /// </summary>
        public List<(AttributeMetadata Attribute, string ParentEntityLogicalName)> ResolveAttributesByMetadataId(
            IReadOnlyList<Guid> attributeMetadataIds, SearchProgress progress = null)
        {
            var results = new List<(AttributeMetadata, string)>();
            if (attributeMetadataIds == null || attributeMetadataIds.Count == 0) return results;

            progress = progress ?? SearchProgress.None;

            // Smaller batch size than the data-record "In" batching elsewhere in this
            // project (500) - each id here becomes its own OR'd condition rather than
            // a single compact "In" clause, so the filter expression grows faster per id.
            const int batchSize = 50;

            foreach (var batch in QueryPaging.Chunk(attributeMetadataIds, batchSize))
            {
                progress.ThrowIfCancelled();

                var attributeFilter = new MetadataFilterExpression(LogicalOperator.Or);
                foreach (var id in batch)
                    attributeFilter.Conditions.Add(new MetadataConditionExpression("MetadataId", MetadataConditionOperator.Equals, id));

                var query = new EntityQueryExpression
                {
                    // Request Attributes at the entity level when using AttributeQuery,
                    // otherwise the service throws: "Attributes must be part of the requested EntityMetadata properties when an AttributeQuery is specified."
                    Properties = new MetadataPropertiesExpression("LogicalName", "Attributes"),
                    AttributeQuery = new AttributeQueryExpression
                    {
                        Properties = new MetadataPropertiesExpression("LogicalName", "DisplayName", "MetadataId"),
                        Criteria = attributeFilter
                    }
                };

                var request = new RetrieveMetadataChangesRequest { Query = query };
                var response = (RetrieveMetadataChangesResponse)_service.Execute(request);

                foreach (var entity in response.EntityMetadata)
                {
                    foreach (var attribute in entity.Attributes ?? new AttributeMetadata[0])
                    {
                        results.Add((attribute, entity.LogicalName));
                    }
                }
            }

            return results;
        }


        /// name/label. Uses EntityFilters.All rather than a narrower filter because
        /// the SDK doesn't expose a dedicated "Keys only" filter bit reliably across
        /// versions - correctness over the small extra payload.
        /// </summary>
        public List<EntityKeyMetadata> ResolveKeys(string entityLogicalName, string searchText)
        {
            var request = new RetrieveEntityRequest
            {
                LogicalName = entityLogicalName,
                EntityFilters = EntityFilters.All,
                RetrieveAsIfPublished = true
            };
            var response = (RetrieveEntityResponse)_service.Execute(request);
            var keys = response.EntityMetadata.Keys ?? new EntityKeyMetadata[0];

            if (string.IsNullOrWhiteSpace(searchText))
                return keys.ToList();

            var text = searchText.Trim();
            return keys.Where(k =>
                    (k.LogicalName?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (k.GetDisplayLabel()?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>
        /// Retrieves every relationship (1:N, N:1, N:N) an entity participates in and
        /// filters by schema name. A relationship where the entity is on the "one"
        /// side of a 1:N is tagged differently from the same table's "many" side view
        /// of a different relationship - both are legitimate, separately relevant
        /// results, matching how Solution Explorer shows relationships per entity.
        /// </summary>
        public List<RelationshipInfo> ResolveRelationships(string entityLogicalName, string searchText)
        {
            var request = new RetrieveEntityRequest
            {
                LogicalName = entityLogicalName,
                EntityFilters = EntityFilters.Relationships,
                RetrieveAsIfPublished = true
            };
            var response = (RetrieveEntityResponse)_service.Execute(request);
            var metadata = response.EntityMetadata;

            var all = new List<RelationshipInfo>();

            foreach (var r in metadata.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
                all.Add(new RelationshipInfo { MetadataId = r.MetadataId, SchemaName = r.SchemaName, RelationshipTypeLabel = "1:N" });

            foreach (var r in metadata.ManyToOneRelationships ?? new OneToManyRelationshipMetadata[0])
                all.Add(new RelationshipInfo { MetadataId = r.MetadataId, SchemaName = r.SchemaName, RelationshipTypeLabel = "N:1" });

            foreach (var r in metadata.ManyToManyRelationships ?? new ManyToManyRelationshipMetadata[0])
                all.Add(new RelationshipInfo { MetadataId = r.MetadataId, SchemaName = r.SchemaName, RelationshipTypeLabel = "N:N" });

            if (string.IsNullOrWhiteSpace(searchText))
                return all;

            var text = searchText.Trim();
            return all.Where(r => r.SchemaName?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        /// <summary>
        /// Returns global option sets (a.k.a. global choices) whose name or display
        /// label contains searchText.
        /// </summary>
        public List<OptionSetMetadataBase> ResolveGlobalOptionSets(string searchText)
        {
            var all = GetAllGlobalOptionSetsLightweight();
            if (string.IsNullOrWhiteSpace(searchText))
                return all.ToList();

            var text = searchText.Trim();
            return all.Where(o =>
                    (o.Name?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (o.DisplayName?.UserLocalizedLabel?.Label?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }
    }
}
