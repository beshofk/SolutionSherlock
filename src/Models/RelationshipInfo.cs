using System;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// Lightweight, direction-agnostic projection over the SDK's three separate
    /// relationship metadata shapes (OneToManyRelationshipMetadata used for both
    /// 1:N and N:1, ManyToManyRelationshipMetadata for N:N), so SearchEngine can
    /// treat all three uniformly when searching an entity's relationships.
    /// </summary>
    public class RelationshipInfo
    {
        public Guid? MetadataId { get; set; }
        public string SchemaName { get; set; }

        /// <summary>"1:N", "N:1", or "N:N".</summary>
        public string RelationshipTypeLabel { get; set; }

        /// <summary>Populated for 1:N/N:1 shapes only - the "one" side entity.</summary>
        public string ReferencedEntityLogicalName { get; set; }

        /// <summary>Populated for 1:N/N:1 shapes only - the "many" side entity.</summary>
        public string ReferencingEntityLogicalName { get; set; }

        /// <summary>Populated for N:N shapes only - the two participating entities.</summary>
        public string Entity1LogicalName { get; set; }
        public string Entity2LogicalName { get; set; }

        /// <summary>True when this is a self-referencing 1:N relationship used for entity hierarchy (parent/child records).</summary>
        public bool IsHierarchical { get; set; }
    }
}
