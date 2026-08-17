using System;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>Outcome of a completed solution export, for the UI's summary label.</summary>
    public class ExportResult
    {
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
