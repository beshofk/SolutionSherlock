using System;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// One row in a solution's full component roster - mirrors the columns of the
    /// classic Solution Explorer "Components" grid (Display Name, Name, Type, State,
    /// Customizable, Description). Produced by SolutionExplorerService.ListComponents.
    /// </summary>
    public class SolutionComponentDetail
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string TypeDisplay { get; set; }

        /// <summary>"Managed" / "Unmanaged" - taken from the containing solution's own
        /// state. Individual components don't track a separately queryable managed
        /// flag the way the solution itself does, so this is the practical
        /// approximation Solution Explorer's own grid effectively shows too.</summary>
        public string State { get; set; }

        /// <summary>
        /// "True"/"False" where a real, verified source exists (currently: Entity,
        /// via EntityMetadata.IsCustomizable); "—" everywhere else rather than a
        /// guessed value - see README "Customizable and Description columns".
        /// </summary>
        public string Customizable { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Populated only for component kinds backed by an actual Dataverse table row
        /// (every "IsDataRecord" type in ComponentTypeMap, plus the systemform/workflow
        /// special cases) - every such table has standard createdon/modifiedon columns.
        /// Null for metadata-only kinds (Entity, Option Set) and for the unresolved
        /// standalone Field/Relationship/Key placeholders, where no such audit date
        /// is available - the UI shows a "—" placeholder in that case rather than
        /// guessing or showing a misleading blank.
        /// </summary>
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        /// <summary>
        /// Populated for entity-scoped kinds where the parent entity is knowable:
        /// the Entity component itself (its own logical/display name), Form, Business
        /// Rule, View, and Chart. Drives grouping-by-entity in the UI - when this is
        /// set, the component's group heading is its parent entity's display name
        /// instead of its own component-type name. Null for non-entity-scoped kinds
        /// (Web Resource, Process, Role, Report, ...) and for the standalone
        /// Relationship/Key placeholders, which still group by type.
        /// </summary>
        public string ParentEntityLogicalName { get; set; }
        public string ParentEntityDisplayName { get; set; }

        public int ComponentTypeCode { get; set; }
        public Guid ObjectId { get; set; }
    }
}
