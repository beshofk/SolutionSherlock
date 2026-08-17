using System;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>Lightweight projection of the "solution" table, cached per connection.</summary>
    public class SolutionInfo
    {
        public Guid SolutionId { get; set; }
        public string FriendlyName { get; set; }
        public string UniqueName { get; set; }
        public bool IsManaged { get; set; }
        public string Version { get; set; }
        public string PublisherName { get; set; }

        /// <summary>solution.modifiedon - the field "sort by date" sorts on.</summary>
        public DateTime? ModifiedOn { get; set; }
    }
}
