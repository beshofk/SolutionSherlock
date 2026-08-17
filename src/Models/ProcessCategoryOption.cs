namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// A single Process (workflow.category) choice for the "Category" dropdown that
    /// replaces the sub-component controls when the main type is Process. Unlike
    /// entity sub-types, a category is a fixed-value filter, not a separately
    /// searchable component kind - so it deliberately doesn't reuse ComponentTypeInfo.
    /// </summary>
    public class ProcessCategoryOption
    {
        public string DisplayName { get; set; }

        /// <summary>The workflow.category numeric value this option filters to.</summary>
        public int Value { get; set; }

        public override string ToString() => DisplayName;
    }
}
