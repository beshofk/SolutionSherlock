namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// Maps directly onto <c>ExportSolutionRequest</c>'s settings flags. Every flag
    /// defaults to <c>false</c> so a new instance represents "unmanaged, no system
    /// settings" - matching the unchecked state of the Export Solution UI, where
    /// both <c>Export as Managed</c> and <c>Include System Settings (Advanced)</c>
    /// are unchecked by default. Callers opt IN to system settings explicitly, via
    /// <see cref="ApplySystemSettings"/>, which the Browse Solutions view model
    /// only invokes when the user has ticked <c>Include System Settings</c> AND
    /// picked at least one entry in the advanced Configure... dialog. When that
    /// top-level checkbox is unchecked, the view model never calls
    /// <see cref="ApplySystemSettings"/>, so every system-setting flag stays
    /// <c>false</c> regardless of what the user previously configured.
    /// </summary>
    public class ExportSolutionOptions
    {
        /// <summary>Required by ExportSolutionRequest - export as Managed vs. Unmanaged.</summary>
        public bool Managed { get; set; }

        // Reserved for future use - not currently exposed in the Configure... dialog,
        // never set by ApplySystemSettings. Stays false unless a future caller sets it.
        public bool ExportAutoNumberingSettings { get; set; }

        public bool ExportCalendarSettings { get; set; }
        public bool ExportCustomizationSettings { get; set; }
        public bool ExportEmailTrackingSettings { get; set; }
        public bool ExportGeneralSettings { get; set; }
        public bool ExportMarketingSettings { get; set; }
        public bool ExportOutlookSynchronizationSettings { get; set; }
        public bool ExportRelationshipRoles { get; set; }
        public bool ExportSales { get; set; }
        public bool ExportIsvConfig { get; set; }

        /// <summary>
        /// Copies the nine user-configurable system-setting flags from
        /// <paramref name="selection"/> onto this options instance. Callers are
        /// expected to only invoke this when the user has ticked <c>Include System
        /// Settings (Advanced)</c>; when that checkbox is unchecked, do NOT call
        /// this method - leave every flag at its <c>false</c> default so a
        /// previously stored selection cannot leak into the export.
        /// <see cref="ExportAutoNumberingSettings"/> is not part of the dialog and
        /// is deliberately not touched here.
        /// </summary>
        public void ApplySystemSettings(SystemSettingsSelection selection)
        {
            if (selection == null) return;

            ExportCalendarSettings = selection.Calendar;
            ExportCustomizationSettings = selection.Customization;
            ExportEmailTrackingSettings = selection.EmailTracking;
            ExportGeneralSettings = selection.General;
            ExportMarketingSettings = selection.Marketing;
            ExportOutlookSynchronizationSettings = selection.OutlookSynchronization;
            ExportRelationshipRoles = selection.RelationshipRoles;
            ExportIsvConfig = selection.IsvConfig;
            ExportSales = selection.Sales;
        }
    }
}
