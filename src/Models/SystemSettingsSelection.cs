using System;
using System.Collections.Generic;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// Working set of the nine solution system-settings the "Include System Settings
    /// (Advanced)" flow lets a user toggle. This is the single source of truth for
    /// the list; both <see cref="SystemSettingsSelection.Definitions"/> (used by the
    /// dialog) and <see cref="ExportSolutionOptions.ApplySystemSettings"/> (used by
    /// the export mapping) enumerate the same set here so a new setting only ever
    /// has to be added in one place.
    ///
    /// The user's checkbox <c>Include System Settings</c> gates whether this
    /// selection is used at all. When that top-level checkbox is unchecked, callers
    /// must treat this selection as if every value were <c>false</c> regardless of
    /// what the user previously configured - see
    /// <see cref="BrowseSolutionsViewModel"/> and the export mapping in
    /// <see cref="ExportSolutionOptions.ApplySystemSettings"/>.
    /// </summary>
    public class SystemSettingsSelection
    {
        public bool Calendar { get; set; }
        public bool Customization { get; set; }
        public bool EmailTracking { get; set; }
        public bool General { get; set; }
        public bool Marketing { get; set; }
        public bool OutlookSynchronization { get; set; }
        public bool RelationshipRoles { get; set; }
        public bool IsvConfig { get; set; }
        public bool Sales { get; set; }

        /// <summary>True when at least one of the nine flags is set. Used for validation.</summary>
        public bool IsAnySelected =>
            Calendar || Customization || EmailTracking || General || Marketing ||
            OutlookSynchronization || RelationshipRoles || IsvConfig || Sales;

        public void SelectAll() => Apply(true);
        public void ClearAll() => Apply(false);

        private void Apply(bool value)
        {
            Calendar = value;
            Customization = value;
            EmailTracking = value;
            General = value;
            Marketing = value;
            OutlookSynchronization = value;
            RelationshipRoles = value;
            IsvConfig = value;
            Sales = value;
        }

        /// <summary>Deep copy - the dialog operates on a working copy so Cancel can discard changes.</summary>
        public SystemSettingsSelection Clone() => new SystemSettingsSelection
        {
            Calendar = Calendar,
            Customization = Customization,
            EmailTracking = EmailTracking,
            General = General,
            Marketing = Marketing,
            OutlookSynchronization = OutlookSynchronization,
            RelationshipRoles = RelationshipRoles,
            IsvConfig = IsvConfig,
            Sales = Sales
        };

        /// <summary>
        /// UI-facing description of each supported setting: display label + a
        /// setter/getter pair the dialog uses to bind a WinForms CheckBox to the
        /// underlying property without reflection. Order here is the order the
        /// checkboxes appear in the dialog.
        /// </summary>
        public static IReadOnlyList<SystemSettingDefinition> Definitions { get; } = new[]
        {
            new SystemSettingDefinition("Calendar",                s => s.Calendar,               (s, v) => s.Calendar = v),
            new SystemSettingDefinition("Customization",           s => s.Customization,          (s, v) => s.Customization = v),
            new SystemSettingDefinition("Email tracking",          s => s.EmailTracking,          (s, v) => s.EmailTracking = v),
            new SystemSettingDefinition("General",                 s => s.General,                (s, v) => s.General = v),
            new SystemSettingDefinition("Marketing",               s => s.Marketing,              (s, v) => s.Marketing = v),
            new SystemSettingDefinition("Outlook Synchronization", s => s.OutlookSynchronization, (s, v) => s.OutlookSynchronization = v),
            new SystemSettingDefinition("Relationship Roles",      s => s.RelationshipRoles,      (s, v) => s.RelationshipRoles = v),
            new SystemSettingDefinition("ISV Config",              s => s.IsvConfig,              (s, v) => s.IsvConfig = v),
            new SystemSettingDefinition("Sales",                   s => s.Sales,                  (s, v) => s.Sales = v),
        };
    }

    /// <summary>
    /// One entry in <see cref="SystemSettingsSelection.Definitions"/>. Pairs a
    /// display label with a strongly-typed getter/setter so the dialog can render a
    /// CheckBox per definition and bind it two-way against the selection instance -
    /// no reflection, and adding a new setting is a one-line change to the
    /// Definitions array.
    /// </summary>
    public class SystemSettingDefinition
    {
        public SystemSettingDefinition(string label, Func<SystemSettingsSelection, bool> getter, Action<SystemSettingsSelection, bool> setter)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Getter = getter ?? throw new ArgumentNullException(nameof(getter));
            Setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        public string Label { get; }
        public Func<SystemSettingsSelection, bool> Getter { get; }
        public Action<SystemSettingsSelection, bool> Setter { get; }
    }
}
