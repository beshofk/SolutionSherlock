using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.ViewModels
{
    public enum SolutionSortColumn { Name, Date }

    /// <summary>
    /// Holds and orchestrates all the non-UI state for the Browse Solutions tab:
    /// the loaded solution list, sorting, paging, the currently selected solution,
    /// and solution export. The UserControl's job is limited to wiring button clicks
    /// to these methods and rendering whatever they return - no Dataverse calls,
    /// sorting/paging math, or file I/O happen in the code-behind for this tab.
    ///
    /// Constructed with its three collaborators via plain constructor injection
    /// (no DI container - there's no host-provided container for a MEF-instantiated,
    /// parameterless-constructor plugin control to plug into, so composition happens
    /// by hand in SolutionSherlockControl.UpdateConnection instead). Each
    /// collaborator is an interface, so a unit test can substitute fakes for all
    /// three without touching a real Dataverse connection or the file system.
    /// </summary>
    public class BrowseSolutionsViewModel
    {
        public const int PageSize = 20;

        private readonly ISolutionRepository _solutionRepository;
        private readonly ISolutionExportService _exportService;
        private readonly IFileSystemService _fileSystemService;

        private List<SolutionInfo> _allSolutions = new List<SolutionInfo>();

        public BrowseSolutionsViewModel(
            ISolutionRepository solutionRepository,
            ISolutionExportService exportService,
            IFileSystemService fileSystemService)
        {
            _solutionRepository = solutionRepository ?? throw new ArgumentNullException(nameof(solutionRepository));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        }

        public int PageIndex { get; private set; }
        public SolutionSortColumn SortColumn { get; private set; } = SolutionSortColumn.Date;
        public bool SortDescending { get; private set; } = true;
        public SolutionInfo SelectedSolution { get; set; }

        /// <summary>Current solution name/display name filter, set via SetSearchText. Empty means no filtering.</summary>
        public string SearchText { get; private set; } = string.Empty;

        /// <summary>Remembered for the lifetime of this view model instance (i.e. this connection) - not persisted across XrmToolBox restarts. See README for how to extend this to a persisted setting.</summary>
        public string LastExportFolder { get; private set; }

        public async Task LoadSolutionsAsync(SearchProgress progress, CancellationToken cancellationToken)
        {
            await _solutionRepository.LoadSolutionsAsync(progress, cancellationToken).ConfigureAwait(false);
            _allSolutions = _solutionRepository.GetAllCached().ToList();
            PageIndex = 0;
        }

        /// <summary>
        /// Toggles sort direction if the same column is clicked again (matching
        /// standard grid header-click UX), otherwise switches to the new column
        /// ascending. Resets to the first page since the row-to-page mapping changes.
        /// </summary>
        public void SetSortColumn(SolutionSortColumn column)
        {
            if (SortColumn == column)
                SortDescending = !SortDescending;
            else
            {
                SortColumn = column;
                SortDescending = false;
            }
            PageIndex = 0;
        }

        public void NextPage() => PageIndex++;
        public void PreviousPage() => PageIndex--;

        /// <summary>Filters the solution list by friendly name or unique name (case-insensitive, contains match). Resets to the first page since the row-to-page mapping changes.</summary>
        public void SetSearchText(string searchText)
        {
            SearchText = searchText ?? string.Empty;
            PageIndex = 0;
        }

        /// <summary>
        /// Returns the current page's rows (already filtered per SearchText and sorted
        /// per SortColumn/SortDescending) plus paging metadata for the UI's page
        /// indicator/button states.
        /// </summary>
        public (IReadOnlyList<SolutionInfo> PageItems, int TotalPages, int TotalCount) GetCurrentPage()
        {
            IEnumerable<SolutionInfo> filtered = _allSolutions;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var text = SearchText.Trim();
                filtered = filtered.Where(s =>
                    (s.FriendlyName?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (s.UniqueName?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0));
            }
            var filteredList = filtered.ToList();

            IOrderedEnumerable<SolutionInfo> sorted;
            if (SortColumn == SolutionSortColumn.Date)
            {
                sorted = SortDescending
                    ? filteredList.OrderByDescending(s => s.ModifiedOn)
                    : filteredList.OrderBy(s => s.ModifiedOn);
            }
            else
            {
                sorted = SortDescending
                    ? filteredList.OrderByDescending(s => s.FriendlyName, StringComparer.OrdinalIgnoreCase)
                    : filteredList.OrderBy(s => s.FriendlyName, StringComparer.OrdinalIgnoreCase);
            }

            var totalPages = Math.Max(1, (int)Math.Ceiling(filteredList.Count / (double)PageSize));
            PageIndex = Math.Max(0, Math.Min(PageIndex, totalPages - 1));

            var pageItems = sorted.Skip(PageIndex * PageSize).Take(PageSize).ToList();
            return (pageItems, totalPages, filteredList.Count);
        }

        /// <summary>
        /// Validates, exports, and saves SelectedSolution to destinationPath.
        /// Throws InvalidOperationException for validation failures (no solution
        /// selected, destination folder missing) so the caller's single catch block
        /// can present a readable message the same way it does for SDK/IO failures.
        /// </summary>
        public async Task<ExportResult> ExportSelectedSolutionAsync(
            string destinationPath,
            bool exportManaged,
            IProgress<ExportStageProgress> progress,
            CancellationToken cancellationToken)
        {
            if (SelectedSolution == null)
                throw new InvalidOperationException("No solution is selected.");

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new InvalidOperationException("No destination file was chosen.");

            var directory = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrEmpty(directory) || !_fileSystemService.DirectoryExists(directory))
                throw new InvalidOperationException($"The destination folder does not exist: {directory}");

            var sw = Stopwatch.StartNew();
            var options = new ExportSolutionOptions { Managed = exportManaged };

            var fileBytes = await _exportService
                .ExportSolutionAsync(SelectedSolution.UniqueName, options, progress, cancellationToken)
                .ConfigureAwait(false);

            progress?.Report(new ExportStageProgress { Stage = ExportStage.Saving, Message = "Saving file..." });
            await _fileSystemService.WriteAllBytesAsync(destinationPath, fileBytes).ConfigureAwait(false);

            sw.Stop();
            LastExportFolder = directory;

            progress?.Report(new ExportStageProgress { Stage = ExportStage.Completed, Message = "Completed." });

            return new ExportResult
            {
                FilePath = destinationPath,
                FileSizeBytes = fileBytes.LongLength,
                Duration = sw.Elapsed
            };
        }

        /// <summary>Suggested export filename: SolutionUniqueName_Version.zip.</summary>
        public static string BuildSuggestedFileName(SolutionInfo solution)
        {
            if (solution == null) return "solution.zip";

            var invalid = Path.GetInvalidFileNameChars();
            var safeUniqueName = new string((solution.UniqueName ?? "solution").Where(c => !invalid.Contains(c)).ToArray());
            var safeVersion = new string((solution.Version ?? "0.0.0.0").Where(c => !invalid.Contains(c)).ToArray());

            return $"{safeUniqueName}_{safeVersion}.zip";
        }
    }
}
