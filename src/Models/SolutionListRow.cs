using System;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>Grid-friendly projection of SolutionInfo for the paged Browse Solutions grid.</summary>
    public class SolutionListRow
    {
        public string FriendlyName { get; set; }
        public string UniqueName { get; set; }
        public string Type { get; set; }
        public string Publisher { get; set; }
        public string Version { get; set; }
        public string ModifiedOnDisplay { get; set; }
        public Guid SolutionId { get; set; }
    }
}
