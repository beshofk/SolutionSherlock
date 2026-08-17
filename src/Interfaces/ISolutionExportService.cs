using System;
using System.Threading;
using System.Threading.Tasks;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces
{
    /// <summary>
    /// Abstracts the ExportSolutionRequest SDK call so the view model can be unit
    /// tested without a real Dataverse connection.
    /// </summary>
    public interface ISolutionExportService
    {
        /// <summary>
        /// Exports a solution and returns the raw .zip file bytes
        /// (ExportSolutionResponse.ExportSolutionFile). Does not write anything to
        /// disk itself - see IFileSystemService for that half of the flow.
        ///
        /// Cancellation note: ExportSolutionRequest is a single, atomic, blocking SDK
        /// call with no intermediate checkpoints (unlike the paginated search queries
        /// elsewhere in this tool). cancellationToken is honored only BEFORE the call
        /// is issued - once Execute() has been sent to the server, this cannot be
        /// interrupted; the Task simply waits for the server's response.
        /// </summary>
        Task<byte[]> ExportSolutionAsync(
            string solutionUniqueName,
            ExportSolutionOptions options,
            IProgress<ExportStageProgress> progress,
            CancellationToken cancellationToken);
    }
}
