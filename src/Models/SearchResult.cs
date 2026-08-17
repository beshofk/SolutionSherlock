using System;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// One row in the results grid: a single component matched inside a single
    /// solution. A component that ships in 3 solutions produces 3 SearchResult rows.
    /// </summary>
    public class SearchResult
    {
        public string SolutionFriendlyName { get; set; }
        public string SolutionUniqueName { get; set; }
        public bool IsManaged { get; set; }
        public string Publisher { get; set; }
        public string Version { get; set; }

        public string ComponentTypeDisplay { get; set; }
        public string ComponentDisplayName { get; set; }
        public string ComponentLogicalName { get; set; }

        /// <summary>Populated only for entity-scoped sub-components, e.g. a Field's parent entity.</summary>
        public string ParentEntityLogicalName { get; set; }

        public Guid ComponentObjectId { get; set; }
        public Guid SolutionId { get; set; }
    }
}
