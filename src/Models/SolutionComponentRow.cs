using System;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// One row of the "solutioncomponent" table for a single solution, projected for
    /// the "Export Components" feature on the Loaded Solutions view. Unlike
    /// SearchResult (which is built by resolving specific component kinds down to
    /// friendly names via metadata/record lookups), this is a generic dump of every
    /// component the solution references, keyed by componenttype code + object id -
    /// it intentionally does not attempt to resolve every possible componenttype
    /// down to a display name, since that would require a metadata/record lookup
    /// strategy per type (see SearchEngine and ComponentTypeMap for the ones that
    /// already have one).
    /// </summary>
    public class SolutionComponentRow
    {
        public Guid SolutionId { get; set; }
        public string SolutionFriendlyName { get; set; }
        public string SolutionUniqueName { get; set; }
        public int ComponentTypeCode { get; set; }
        public string ComponentTypeDisplay { get; set; }
        public Guid ObjectId { get; set; }

        /// <summary>
        /// Human-readable form of solutioncomponent.rootcomponentbehavior (Include
        /// Subcomponents / Do Not Include Subcomponents / Include As Shell Only).
        /// Empty when the row has no value for it (not every component type sets it).
        /// </summary>
        public string RootComponentBehavior { get; set; }
    }
}
