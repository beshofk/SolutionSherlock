using System;
using System.Threading;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// Bundles the two things every long-running step of a search needs: a way to
    /// check "should I stop now?" and a way to report "here's what I'm doing" back
    /// to the UI. Passed through SearchEngine's call chain instead of two separate
    /// parameters everywhere.
    ///
    /// The Log callback is expected to marshal onto the UI thread itself (in
    /// practice: SolutionSherlockControl wires it to
    /// BackgroundWorker.ReportProgress, which XrmToolBox's WorkAsync guarantees is
    /// delivered back on the UI thread via WorkAsyncInfo.ProgressChanged) - callers
    /// inside SearchEngine never need to think about threading.
    /// </summary>
    public class SearchProgress
    {
        /// <summary>No-op progress sink for callers that don't need logging/cancellation.</summary>
        public static readonly SearchProgress None = new SearchProgress(CancellationToken.None, null);

        private readonly Action<string> _log;

        public CancellationToken CancellationToken { get; }

        public SearchProgress(CancellationToken cancellationToken, Action<string> log)
        {
            CancellationToken = cancellationToken;
            _log = log ?? (_ => { });
        }

        public void Log(string message) => _log(message);

        /// <summary>
        /// Checkpoint to call between discrete server round-trips (never mid-call -
        /// IOrganizationService has no way to abort a call already in flight, so
        /// cancellation is cooperative: it takes effect as soon as the current
        /// operation finishes, not instantly).
        /// </summary>
        public void ThrowIfCancelled() => CancellationToken.ThrowIfCancellationRequested();
    }
}
