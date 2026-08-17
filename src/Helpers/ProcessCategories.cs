using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers
{
    /// <summary>
    /// The workflow.category choice values relevant to this tool's Process search.
    /// These are the well-established standard values used throughout the SDK's own
    /// sample OptionSets.cs (WorkflowCategory): 0=Workflow, 1=Dialog, 2=Business Rule,
    /// 3=Action, 4=Business Process Flow, 5=Modern Flow (Power Automate cloud flow),
    /// 6=Desktop Flow (RPA). Business Rule is deliberately NOT offered here since it's
    /// already reachable as the "Business Rule" entity sub-component (see
    /// ComponentTypeMap.SubTypesByMain) - offering it in both places would be a
    /// duplicate, confusing path to the same data. Modern/Desktop Flow are omitted
    /// too, since they're a different mental model (Power Automate) than the four
    /// classic process categories this tool's category filter targets.
    /// </summary>
    public static class ProcessCategories
    {
        public static readonly ProcessCategoryOption[] All =
        {
            new ProcessCategoryOption { DisplayName = "Workflow", Value = 0 },
            new ProcessCategoryOption { DisplayName = "Dialog", Value = 1 },
            new ProcessCategoryOption { DisplayName = "Action", Value = 3 },
            new ProcessCategoryOption { DisplayName = "Business Process Flow", Value = 4 },
        };
    }
}
