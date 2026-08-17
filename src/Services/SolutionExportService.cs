using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Services
{
    /// <summary>
    /// Wraps Microsoft.Crm.Sdk.Messages.ExportSolutionRequest - the official SDK
    /// request for exporting a solution to a downloadable .zip package. This is the
    /// same request the classic "Export Solution" dialog in the application itself
    /// issues under the hood.
    /// </summary>
    public class SolutionExportService : ISolutionExportService
    {
        private readonly IOrganizationService _service;

        public SolutionExportService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task<byte[]> ExportSolutionAsync(
            string solutionUniqueName,
            ExportSolutionOptions options,
            IProgress<ExportStageProgress> progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(solutionUniqueName))
                throw new ArgumentException("Solution unique name is required.", nameof(solutionUniqueName));

            options = options ?? new ExportSolutionOptions();
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ExportStageProgress
            {
                Stage = ExportStage.Preparing,
                Message = $"Preparing to export '{solutionUniqueName}'..."
            });

            var request = new ExportSolutionRequest
            {
                SolutionName = solutionUniqueName,
                Managed = options.Managed,
                ExportAutoNumberingSettings = options.ExportAutoNumberingSettings,
                ExportCalendarSettings = options.ExportCalendarSettings,
                ExportCustomizationSettings = options.ExportCustomizationSettings,
                ExportEmailTrackingSettings = options.ExportEmailTrackingSettings,
                ExportGeneralSettings = options.ExportGeneralSettings,
                ExportMarketingSettings = options.ExportMarketingSettings,
                ExportOutlookSynchronizationSettings = options.ExportOutlookSynchronizationSettings,
                ExportRelationshipRoles = options.ExportRelationshipRoles,
                ExportSales = options.ExportSales,
                ExportIsvConfig = options.ExportIsvConfig
            };

            progress?.Report(new ExportStageProgress
            {
                Stage = ExportStage.Exporting,
                Message = $"Exporting '{solutionUniqueName}' ({(options.Managed ? "Managed" : "Unmanaged")})..."
            });

            // IMPORTANT SDK NUANCE: ExportSolutionRequest is synchronous and blocking
            // on the wire - Dataverse builds the entire export package server-side and
            // returns the complete .zip in a single response. There is no server-side
            // async job to poll (unlike, say, ImportSolutionAsyncRequest), and no
            // incremental progress signal - the call simply doesn't return until the
            // whole package is ready, which can take anywhere from a couple of seconds
            // to several minutes for a large solution.
            //
            // Task.Run pushes that blocking call onto a thread-pool thread so the UI
            // thread stays responsive (the form keeps repainting, the marquee progress
            // bar keeps animating). It does NOT make the underlying request itself
            // cancellable mid-flight: cancellationToken is only checked before Execute()
            // is called, never during. Once the request has been sent, this Task simply
            // waits for the server's response - there is no API to abort an in-flight
            // ExportSolutionRequest.
            var response = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return (ExportSolutionResponse)_service.Execute(request);
            }, cancellationToken).ConfigureAwait(false);

            return response.ExportSolutionFile;
        }
    }
}
