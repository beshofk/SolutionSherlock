namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// Flat, grid-friendly projection of a SearchResult plus a connection-aware
    /// SolutionUrl. Kept separate from SearchResult because the URL depends on the
    /// active ConnectionDetail (Online vs On-Premises), which is a UI/connection
    /// concern - SearchEngine has no reason to know about it.
    /// </summary>
    public class SearchResultRow
    {
        public string Solution { get; set; }
        public string Type { get; set; }
        public string Publisher { get; set; }
        public string Component { get; set; }
        public string Name { get; set; }
        public string Parent { get; set; }
        public string Version { get; set; }

        /// <summary>
        /// Deep link to the solution (Maker Portal for Online, classic solution
        /// record URL for On-Premises). Null when no usable connection info is
        /// available, in which case the grid's link column is left inert for that row.
        /// </summary>
        public string SolutionUrl { get; set; }

        /// <summary>The component's real Dataverse GUID (SearchResult.ComponentObjectId).</summary>
        public System.Guid ComponentGuid { get; set; }
    }
}
