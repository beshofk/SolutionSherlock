using System.Collections.Generic;
using System.Linq;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers
{
    /// <summary>
    /// Single source of truth mapping the friendly ComponentTypeKind values used
    /// throughout the UI/search logic to the numeric solutioncomponent.componenttype
    /// option set values Dataverse actually stores, plus (for record-backed kinds)
    /// the table/column names SearchEngine needs.
    ///
    /// Every code below (except where flagged) was checked against Microsoft's
    /// documented componenttype reference and community-verified mappings rather
    /// than assumed - see README.md "Component code confidence notes" for the
    /// handful of entries that are best-effort rather than fully confirmed, and
    /// for a list of requested component types deliberately left out because no
    /// stable componenttype code could be confirmed for them (Custom API and its
    /// sub-tables, Virtual Entity Data Source/Provider, Client Extensions,
    /// Chatbot/Copilot subcomponents, Catalogs).
    /// </summary>
    public static class ComponentTypeMap
    {
        public static readonly Dictionary<ComponentTypeKind, ComponentTypeInfo> All =
            new Dictionary<ComponentTypeKind, ComponentTypeInfo>
            {
                // ---- Metadata-based kinds ----
                [ComponentTypeKind.Entity] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.Entity, DisplayName = "Entity",
                    SolutionComponentTypeCode = 1, IsEntityScoped = false, IsDataRecord = false
                },
                [ComponentTypeKind.Attribute] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.Attribute, DisplayName = "Field",
                    SolutionComponentTypeCode = 2, IsEntityScoped = true, IsDataRecord = false
                },
                [ComponentTypeKind.OptionSet] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.OptionSet, DisplayName = "Option Set (Global Choice)",
                    SolutionComponentTypeCode = 9, IsEntityScoped = false, IsDataRecord = false
                },
                [ComponentTypeKind.Relationship] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.Relationship, DisplayName = "Relationship",
                    SolutionComponentTypeCode = 10, IsEntityScoped = true, IsDataRecord = false
                },
                [ComponentTypeKind.EntityKey] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.EntityKey, DisplayName = "Key",
                    SolutionComponentTypeCode = 14, IsEntityScoped = true, IsDataRecord = false
                },

                // ---- Entity-scoped, table-backed sub-components ----
                [ComponentTypeKind.SystemForm] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.SystemForm, DisplayName = "Form",
                    SolutionComponentTypeCode = 60, IsEntityScoped = true, IsDataRecord = true,
                    EntityLogicalName = "systemform", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.SavedQuery] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.SavedQuery, DisplayName = "View",
                    SolutionComponentTypeCode = 26, IsEntityScoped = true, IsDataRecord = true,
                    EntityLogicalName = "savedquery", PrimaryNameAttribute = "name",
                    ParentEntityAttribute = "returnedtypecode"
                },
                [ComponentTypeKind.Chart] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.Chart, DisplayName = "Chart",
                    SolutionComponentTypeCode = 59, IsEntityScoped = true, IsDataRecord = true,
                    EntityLogicalName = "savedqueryvisualization", PrimaryNameAttribute = "name",
                    ParentEntityAttribute = "primaryentitytypecode"
                },
                [ComponentTypeKind.BusinessRule] = new ComponentTypeInfo
                {
                    // Business Rules are NOT a distinct solutioncomponent type - they're
                    // rows in the same "workflow" table as Process (componenttype 29),
                    // distinguished only by category = 2. See SearchEngine's entity
                    // sub-component dispatch for how the category filter is applied.
                    Kind = ComponentTypeKind.BusinessRule, DisplayName = "Business Rule",
                    SolutionComponentTypeCode = 29, IsEntityScoped = true, IsDataRecord = true,
                    EntityLogicalName = "workflow", PrimaryNameAttribute = "name", DescriptionAttribute = "description"
                },

                // ---- Main types: automation ----
                [ComponentTypeKind.Workflow] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.Workflow, DisplayName = "Process",
                    SolutionComponentTypeCode = 29, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "workflow", PrimaryNameAttribute = "name", DescriptionAttribute = "description"
                },
                [ComponentTypeKind.PluginAssembly] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.PluginAssembly, DisplayName = "Plugin Assembly",
                    SolutionComponentTypeCode = 91, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "pluginassembly", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.SdkMessageProcessingStep] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.SdkMessageProcessingStep, DisplayName = "Plugin Step",
                    SolutionComponentTypeCode = 92, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "sdkmessageprocessingstep", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.ServiceEndpoint] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.ServiceEndpoint, DisplayName = "Service Endpoint",
                    SolutionComponentTypeCode = 95, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "serviceendpoint", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.WebResource] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.WebResource, DisplayName = "Web Resource",
                    SolutionComponentTypeCode = 61, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "webresource", PrimaryNameAttribute = "name", DescriptionAttribute = "description"
                },
                [ComponentTypeKind.CustomControl] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.CustomControl, DisplayName = "Custom Control",
                    SolutionComponentTypeCode = 66, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "customcontrol", PrimaryNameAttribute = "name"
                },

                // ---- Main types: security & governance ----
                [ComponentTypeKind.Role] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.Role, DisplayName = "Security Role",
                    SolutionComponentTypeCode = 20, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "role", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.FieldSecurityProfile] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.FieldSecurityProfile, DisplayName = "Field Security Profile",
                    SolutionComponentTypeCode = 70, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "fieldsecurityprofile", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.DuplicateRule] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.DuplicateRule, DisplayName = "Duplicate Detection Rule",
                    SolutionComponentTypeCode = 44, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "duplicaterule", PrimaryNameAttribute = "name"
                },

                // ---- Main types: service/case management ----
                [ComponentTypeKind.RoutingRule] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.RoutingRule, DisplayName = "Routing Rule Set",
                    SolutionComponentTypeCode = 150, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "routingrule", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.Sla] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.Sla, DisplayName = "SLA",
                    SolutionComponentTypeCode = 152, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "sla", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.ConvertRule] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.ConvertRule, DisplayName = "Record Creation and Update Rule",
                    SolutionComponentTypeCode = 154, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "convertrule", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.ConnectionRole] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.ConnectionRole, DisplayName = "Connection Role",
                    SolutionComponentTypeCode = 63, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "connectionrole", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.MobileOfflineProfile] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.MobileOfflineProfile, DisplayName = "Mobile Offline Profile",
                    SolutionComponentTypeCode = 161, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "mobileofflineprofile", PrimaryNameAttribute = "name"
                },

                // ---- Main types: apps & reporting ----
                [ComponentTypeKind.AppModule] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.AppModule, DisplayName = "Model-driven App",
                    SolutionComponentTypeCode = 80, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "appmodule", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.Report] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.Report, DisplayName = "Report",
                    SolutionComponentTypeCode = 31, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "report", PrimaryNameAttribute = "name"
                },
                [ComponentTypeKind.Dashboard] = new ComponentTypeInfo
                {
                    // Dashboards are also systemform rows (same componenttype as Form,
                    // 60) distinguished by type = 0. Not entity-scoped in practice -
                    // most dashboards aren't tied to a single table - so this is a
                    // MAIN type, not an Entity sub-component, even though it shares a
                    // table with Form. Best-effort: Microsoft doesn't publish the
                    // systemform.type option set values in an easily citable page;
                    // type = 0 = Dashboard is a long-standing, widely-used convention -
                    // verify in your org if this returns nothing unexpected.
                    Kind = ComponentTypeKind.Dashboard, DisplayName = "Dashboard",
                    SolutionComponentTypeCode = 60, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "systemform", PrimaryNameAttribute = "name",
                    StaticFilterAttribute = "type", StaticFilterValue = 0
                },

                // ---- Main types: templates ----
                // NOTE on PrimaryNameAttribute = "title": the Template-entity family
                // (Email/Contract/Article/Mail Merge templates) uses "title" rather
                // than "name" for its primary text column, unlike most other tables
                // in this map. Verify against your org if a template search behaves
                // unexpectedly.
                [ComponentTypeKind.EmailTemplate] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.EmailTemplate, DisplayName = "Email Template",
                    SolutionComponentTypeCode = 36, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "template", PrimaryNameAttribute = "title"
                },
                [ComponentTypeKind.ContractTemplate] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.ContractTemplate, DisplayName = "Contract Template",
                    SolutionComponentTypeCode = 37, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "contracttemplate", PrimaryNameAttribute = "title"
                },
                [ComponentTypeKind.ArticleTemplate] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.ArticleTemplate, DisplayName = "Article Template (Knowledge Base)",
                    SolutionComponentTypeCode = 38, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "kbarticletemplate", PrimaryNameAttribute = "title"
                },
                [ComponentTypeKind.MailMergeTemplate] = new ComponentTypeInfo
                {
                    Kind = ComponentTypeKind.MailMergeTemplate, DisplayName = "Mail Merge Template",
                    SolutionComponentTypeCode = 39, IsEntityScoped = false, IsDataRecord = true,
                    EntityLogicalName = "mailmergetemplate", PrimaryNameAttribute = "title"
                },
            };

        /// <summary>
        /// Which sub-component kinds make sense underneath a given main type. Only
        /// Entity has entries. All seven are fully wired into SearchEngine (unlike
        /// the earlier build, which only wired Field) - see SearchEngine's entity
        /// sub-component dispatch.
        /// </summary>
        public static readonly Dictionary<ComponentTypeKind, ComponentTypeKind[]> SubTypesByMain =
            new Dictionary<ComponentTypeKind, ComponentTypeKind[]>
            {
                [ComponentTypeKind.Entity] = new[]
                {
                    ComponentTypeKind.Attribute,
                    ComponentTypeKind.SystemForm,
                    ComponentTypeKind.SavedQuery,
                    ComponentTypeKind.Chart,
                    ComponentTypeKind.EntityKey,
                    ComponentTypeKind.Relationship,
                    ComponentTypeKind.BusinessRule
                }
            };

        public static ComponentTypeInfo Get(ComponentTypeKind kind) => All[kind];

        /// <summary>
        /// The set of kinds that make sense as a MAIN search type - i.e. everything
        /// that isn't only meaningful underneath a parent entity. This is what
        /// populates the "Component type" dropdown.
        /// </summary>
        public static IEnumerable<ComponentTypeInfo> MainTypes =>
            All.Values.Where(t => !t.IsEntityScoped).OrderBy(t => t.DisplayName == "Entity" ? 0 : 1).ThenBy(t => t.DisplayName);
    }
}
