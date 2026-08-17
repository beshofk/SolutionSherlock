using System;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// One row bound to dgvSolutionComponents. Three shapes flow through the same grid:
    /// - A top-level group header (IsGroupHeader = true, GroupLevel = 0) - either the
    ///   "Entity" super-group (when the roster contains any entity-scoped rows) or a
    ///   plain component-type heading ("Web Resource", "Process", ...).
    /// - A nested entity header (IsGroupHeader = true, GroupLevel = 1) - one per
    ///   distinct parent entity, only ever nested under the "Entity" super-group.
    /// - A real component row (mirrors SolutionComponentDetail's display columns).
    ///
    /// GroupKey/TopGroupKey/EntityGroupKey exist purely to support collapse/expand:
    /// SolutionSherlockControl tracks which GroupKey values are currently
    /// collapsed and hides any row whose TopGroupKey (and, if set, EntityGroupKey)
    /// is in that set - see ApplyGroupVisibility.
    ///
    /// Kept separate from SolutionComponentDetail - same pattern already used for
    /// SearchResultRow/SolutionListRow elsewhere in this project - so the domain
    /// model (SolutionComponentDetail) stays free of UI-only concerns like "is this
    /// a header row" or "is this currently collapsed".
    /// </summary>
    public class SolutionComponentDisplayRow
    {
        public bool IsGroupHeader { get; set; }
        public string GroupHeaderText { get; set; }

        /// <summary>0 for top-level headers, 1 for a nested entity header. Unused (0) for Member rows.</summary>
        public int GroupLevel { get; set; }

        /// <summary>Unique key identifying THIS header row for collapse tracking. Null for Member rows.</summary>
        public string GroupKey { get; set; }

        /// <summary>The top-level group (header or member) this row belongs under - its own GroupKey, for a top-level header.</summary>
        public string TopGroupKey { get; set; }

        /// <summary>For rows nested under the "Entity" super-group, the specific entity sub-group's key; null otherwise (including for the entity header row itself, which IS that key, not nested under it).</summary>
        public string EntityGroupKey { get; set; }

        /// <summary>For member rows nested under a component-type header within an entity (e.g. "Forms"), that header's key; null for the header row itself and for non-entity-scoped rows.</summary>
        public string TypeGroupKey { get; set; }

        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string TypeDisplay { get; set; }
        public string State { get; set; }
        public string Customizable { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        /// <summary>The real Dataverse GUID this component/record represents; null for group header rows.</summary>
        public Guid? ObjectId { get; set; }
    }
}
