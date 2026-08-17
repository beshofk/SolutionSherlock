namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Models
{
    /// <summary>
    /// The set of component "kinds" the search UI knows about.
    ///
    /// MAIN types (IsEntityScoped == false in ComponentTypeMap) populate the
    /// "Component type" dropdown: Entity, OptionSet, Role, Workflow, WebResource,
    /// PluginAssembly, SdkMessageProcessingStep, Report, EmailTemplate,
    /// ContractTemplate, ArticleTemplate, MailMergeTemplate, DuplicateRule,
    /// ConnectionRole, CustomControl, FieldSecurityProfile, AppModule,
    /// ServiceEndpoint, RoutingRule, Sla, ConvertRule, MobileOfflineProfile, Dashboard.
    ///
    /// SUB types (IsEntityScoped == true) only ever appear under Entity: Attribute
    /// (Field), SystemForm (Form), SavedQuery (View), Chart, EntityKey (Key),
    /// Relationship, BusinessRule.
    /// </summary>
    public enum ComponentTypeKind
    {
        Entity,
        Attribute,
        OptionSet,
        Relationship,
        SystemForm,
        SavedQuery,
        WebResource,
        Workflow,
        Role,
        PluginAssembly,
        SdkMessageProcessingStep,
        Chart,
        EntityKey,
        BusinessRule,
        Report,
        EmailTemplate,
        ContractTemplate,
        ArticleTemplate,
        MailMergeTemplate,
        DuplicateRule,
        ConnectionRole,
        CustomControl,
        FieldSecurityProfile,
        AppModule,
        ServiceEndpoint,
        RoutingRule,
        Sla,
        ConvertRule,
        MobileOfflineProfile,
        Dashboard
    }

    /// <summary>
    /// Pairs a ComponentTypeKind with its display label, its numeric
    /// solutioncomponent.componenttype option set value, and - for component kinds
    /// that are ordinary Dataverse table rows rather than metadata - the table/column
    /// names SearchEngine needs to query them generically.
    /// </summary>
    public class ComponentTypeInfo
    {
        public ComponentTypeKind Kind { get; set; }
        public string DisplayName { get; set; }

        /// <summary>
        /// The numeric value of the solutioncomponent.componenttype choice column.
        /// See: https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/solutioncomponent
        /// </summary>
        public int SolutionComponentTypeCode { get; set; }

        /// <summary>
        /// True for component kinds that only make sense underneath a parent entity
        /// (Field, Form, View, Chart, Key, Relationship, Business Rule). Used to build
        /// the main-type dropdown (which only ever shows IsEntityScoped == false kinds)
        /// and to decide whether a query needs to be scoped to an entity's logical name.
        /// </summary>
        public bool IsEntityScoped { get; set; }

        /// <summary>
        /// True for kinds that are ordinary Dataverse table rows (Web Resource,
        /// Process, Security Role, Report, templates, etc.) rather than metadata
        /// (Entity, Attribute, global Option Set, Key). Drives which SearchEngine
        /// strategy is used: a generic QueryExpression-based search for record-backed
        /// kinds vs. a metadata-service call for the others.
        /// </summary>
        public bool IsDataRecord { get; set; }

        /// <summary>Dataverse table logical name, only set when IsDataRecord is true.</summary>
        public string EntityLogicalName { get; set; }

        /// <summary>Primary name column to filter/display on, only set when IsDataRecord is true.</summary>
        public string PrimaryNameAttribute { get; set; }

        /// <summary>
        /// Description column, only set for the handful of tables this tool has
        /// confirmed actually have one (currently: Web Resource, Process). Left null
        /// for every other record-backed type rather than guessed - an incorrect
        /// column name here would fail the whole query, not just the one field, so
        /// this is opt-in only where verified.
        /// </summary>
        public string DescriptionAttribute { get; set; }

        /// <summary>
        /// Optional always-on equality filter for tables shared by more than one
        /// component kind - e.g. Dashboards and Forms both live in "systemform",
        /// distinguished only by the "type" column. Null when not needed.
        /// </summary>
        public string StaticFilterAttribute { get; set; }
        public object StaticFilterValue { get; set; }

        /// <summary>
        /// Column holding the parent entity's logical name, only set for the
        /// entity-scoped record types where SolutionExplorerService's generic
        /// resolver can populate it directly (View: "returnedtypecode", Chart:
        /// "primaryentitytypecode"). Form and Business Rule are entity-scoped too
        /// but go through their own special-cased resolvers (they share a table with
        /// Dashboard/Process respectively) rather than this generic path, so they
        /// don't use this field even though they also end up grouped by entity.
        /// </summary>
        public string ParentEntityAttribute { get; set; }

        public override string ToString() => DisplayName;
    }
}
