namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// Everything the user entered on the search panel, captured as one immutable-ish
    /// payload so SearchEngine.Search() has a single, testable input.
    /// </summary>
    public class SearchCriteria
    {
        public ComponentTypeKind MainType { get; set; }
        public string MainSearchText { get; set; }

        public ComponentTypeKind? SubType { get; set; }
        public string SubSearchText { get; set; }

        /// <summary>
        /// Only used when MainType == Workflow. A fixed-value filter (workflow.category)
        /// selected via the "Category" dropdown that replaces the sub-component
        /// controls for Process - see ProcessCategories. Null means "any category".
        /// </summary>
        public int? ProcessCategory { get; set; }

        public bool ManagedOnly { get; set; }
        public bool UnmanagedOnly { get; set; }
    }
}
