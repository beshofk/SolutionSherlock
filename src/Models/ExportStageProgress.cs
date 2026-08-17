namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// The four stages of a solution export, reported via IProgress&lt;ExportStageProgress&gt;.
    /// There's no real percentage available from the SDK (ExportSolutionRequest
    /// returns the complete file in one shot, with no incremental progress signal),
    /// so the UI shows these as a marquee/indeterminate progress bar plus a status
    /// message rather than a fabricated percentage.
    /// </summary>
    public enum ExportStage
    {
        Preparing,
        Exporting,
        Saving,
        Completed
    }

    public class ExportStageProgress
    {
        public ExportStage Stage { get; set; }
        public string Message { get; set; }
    }
}
