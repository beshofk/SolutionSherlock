using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces
{
    /// <summary>
    /// Abstracts solution list loading/caching so callers (SearchEngine, the Browse
    /// Solutions view model) depend on an interface rather than the concrete
    /// SolutionCache - the seam a unit test would substitute a fake/mock through.
    /// </summary>
    public interface ISolutionRepository
    {
        /// <summary>
        /// Loads (or reloads) every visible, non-system solution. Wraps the
        /// underlying synchronous SDK call in Task.Run - see SolutionCache for why
        /// (IOrganizationService has no native async surface on this SDK).
        /// </summary>
        Task LoadSolutionsAsync(SearchProgress progress, CancellationToken cancellationToken);

        /// <summary>Every cached solution. Triggers a synchronous load on first use if nothing has been loaded yet.</summary>
        IReadOnlyList<SolutionInfo> GetAllCached();

        SolutionInfo GetById(Guid solutionId);

        DateTime LastRefreshedUtc { get; }
    }
}
