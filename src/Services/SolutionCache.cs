using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Services
{
    /// <summary>
    /// Caches solution + publisher lookups for the lifetime of a connection.
    /// Solutions rarely change mid-session; callers can force a refresh via Refresh().
    /// Implements ISolutionRepository so SearchEngine and the Browse Solutions view
    /// model depend on the interface, not this concrete class.
    /// </summary>
    public class SolutionCache : ISolutionRepository
    {
        private readonly IOrganizationService _service;
        private Dictionary<Guid, Models.SolutionInfo> _byId;

        public DateTime LastRefreshedUtc { get; private set; } = DateTime.MinValue;

        public SolutionCache(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Reloads every visible solution (both managed and unmanaged) into memory.
        /// "isvisible" excludes internal/system solutions, and the Default Solution is
        /// additionally excluded by unique name as a belt-and-suspenders measure since
        /// its own isvisible value isn't something this tool controls. Only ever
        /// assigns _byId once, after every page has been retrieved - if cancelled
        /// partway through, the previous (possibly empty) cache is left untouched
        /// rather than replaced with a partial one.
        /// </summary>
        public void Refresh(SearchProgress progress = null)
        {
            progress = progress ?? SearchProgress.None;

            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("friendlyname", "uniquename", "ismanaged", "version", "publisherid", "modifiedon"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                        // Belt-and-suspenders: the Default Solution and other
                        // system-owned solutions should already be isvisible = false
                        // in virtually every environment, but exclude by unique name
                        // explicitly too so "only show exportable solutions" holds
                        // even in an environment where that assumption doesn't.
                        new ConditionExpression("uniquename", ConditionOperator.NotEqual, "Default")
                    }
                },
                PageInfo = new PagingInfo { PageNumber = 1, Count = 500 }
            };

            var sw = Stopwatch.StartNew();
            var result = new Dictionary<Guid, Models.SolutionInfo>();
            EntityCollection page;

            do
            {
                progress.ThrowIfCancelled();

                page = _service.RetrieveMultiple(query);
                foreach (var row in page.Entities)
                {
                    var publisherRef = row.GetAttributeValue<EntityReference>("publisherid");
                    result[row.Id] = new Models.SolutionInfo
                    {
                        SolutionId = row.Id,
                        FriendlyName = row.GetAttributeValue<string>("friendlyname"),
                        UniqueName = row.GetAttributeValue<string>("uniquename"),
                        IsManaged = row.GetAttributeValue<bool>("ismanaged"),
                        Version = row.GetAttributeValue<string>("version"),
                        PublisherName = publisherRef?.Name,
                        ModifiedOn = row.GetAttributeValue<DateTime?>("modifiedon")
                    };
                }

                if (page.MoreRecords)
                {
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = page.PagingCookie;
                }
            } while (page.MoreRecords);

            _byId = result;
            LastRefreshedUtc = DateTime.UtcNow;
            progress.Log($"Loaded {result.Count} solution(s) in {sw.ElapsedMilliseconds}ms.");
        }

        /// <summary>
        /// ISolutionRepository's async surface. Wraps the synchronous Refresh() in
        /// Task.Run - IOrganizationService (as supplied by the XrmToolBox host) has no
        /// native async methods on this SDK, so genuine async/await at the service
        /// layer means "run the blocking call off the calling thread", not "the SDK
        /// call itself is non-blocking".
        /// </summary>
        public async Task LoadSolutionsAsync(SearchProgress progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() => Refresh(progress), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Loads on first use if Refresh() hasn't been called yet.</summary>
        public Models.SolutionInfo GetById(Guid solutionId)
        {
            if (_byId == null) Refresh();
            return _byId.TryGetValue(solutionId, out var info) ? info : null;
        }

        /// <summary>
        /// Every cached solution, sorted by friendly name - the source list for the
        /// paged "Browse Solutions" grid. Loads on first use if Refresh() hasn't been
        /// called yet, same as GetById.
        /// </summary>
        public IReadOnlyList<Models.SolutionInfo> GetAll()
        {
            if (_byId == null) Refresh();
            return _byId.Values.OrderBy(s => s.FriendlyName).ToList();
        }

        /// <summary>ISolutionRepository's synchronous accessor - identical to GetAll(), named to match the interface.</summary>
        public IReadOnlyList<Models.SolutionInfo> GetAllCached() => GetAll();
    }
}
