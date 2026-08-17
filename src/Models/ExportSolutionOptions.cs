namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// Maps directly onto ExportSolutionRequest's settings flags. Defaults mirror
    /// what the classic "Export Solution" dialog in the application itself defaults
    /// to: everything included except the legacy ISV.Config setting.
    /// </summary>
    public class ExportSolutionOptions
    {
        /// <summary>Required by ExportSolutionRequest - export as Managed vs. Unmanaged.</summary>
        public bool Managed { get; set; }

        public bool ExportAutoNumberingSettings { get; set; } = true;
        public bool ExportCalendarSettings { get; set; } = true;
        public bool ExportCustomizationSettings { get; set; } = true;
        public bool ExportEmailTrackingSettings { get; set; } = true;
        public bool ExportGeneralSettings { get; set; } = true;
        public bool ExportMarketingSettings { get; set; } = true;
        public bool ExportOutlookSynchronizationSettings { get; set; } = true;
        public bool ExportRelationshipRoles { get; set; } = true;
        public bool ExportSales { get; set; } = true;

        /// <summary>Legacy ISV.Config export - off by default, rarely needed on modern Dataverse.</summary>
        public bool ExportIsvConfig { get; set; } = false;
    }
}
