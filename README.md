# SolutionSherlock — XrmToolBox Plugin

![SolutionSherlock](https://raw.githubusercontent.com/beshofk/SolutionSherlock/main/Assets/SolutionSherlock-logo-dark.png)

**SolutionSherlock** is an XrmToolBox plugin for investigating and exploring
Microsoft Dataverse / Dynamics 365 solutions and their components.

**Author:** Beshoy Fanous

**License:** [MIT](LICENSE.md)

Status: **Roadmap Phase 1 + 2 complete** (Entity search, Entity → Field search,
managed/unmanaged filter, grouping, copy/Maker-Portal context menu, plus the
full Solution Explorer tab described below). Phases 3–6 (more component
types, saved searches) are structurally supported but not yet wired — see
`Extending this tool` below. Additional investigation/exploration
capabilities may be added in future releases.

---

## Features

### 🔎 Solution Component Investigation

Search across every solution in a connected environment to find which ones
contain a given component, using:

- Entity (main component search)
- Field on a given entity (sub-component search)
- Managed / unmanaged filters
- Grouped, sortable results with a copy / "Open in Maker Portal" context menu

### 🕵️ Solution Explorer

Explore Dataverse solutions in depth:

- Loading all solutions in the connected environment
- Searching / filtering the solution list
- Browsing a solution's full component roster, grouped by component type
  (and, for Entity sub-components, by parent entity)
- Viewing component details (name, logical name, created/modified dates)
- Opening a solution (in the Maker Portal, where supported)
- Exporting a solution as a deployable `.zip`
- Exporting a solution's component list to CSV

---

## 1. Requirements

| Requirement | Version |
|---|---|
| Visual Studio | **2026** (v18), Community/Professional/Enterprise. VS2026 is fully backward compatible with VS2022-era project types, so this SDK-style `net48` project opens and builds with no conversion step. |
| .NET Framework Developer Pack | **4.8** (must be the *Developer Pack*, not just the runtime, or `net48` won't appear as a target) |
| XrmToolBox | Any recent build — the project references the `XrmToolBox` NuGet package, which supplies both `XrmToolBox.Extensibility` and a version-matched Dataverse SDK. Bump the package version in `SolutionSherlock.csproj` to match whatever XrmToolBox build you're targeting. |
| Dataverse SDK | Supplied transitively via the `XrmToolBox` package (currently backed by `Microsoft.CrmSdk.CoreAssemblies`). **Do not** add `Microsoft.PowerPlatform.Dataverse.Client` or a different `Microsoft.CrmSdk.CoreAssemblies` version directly to this project — see the comment block at the top of the `.csproj` for why that causes runtime `FileLoadException`s. |

> **Framework note:** this is a *client-side XrmToolBox tool* (a WinForms UserControl
> hosted in-process by XrmToolBox.exe), which is why it targets `net48` — matching
> the host. This is unrelated to the `.NET Framework 4.6.2` requirement for
> server-side Dataverse plugin/workflow-activity assemblies; nothing in this
> project is a Dataverse plugin.

---

## 2. Getting it building in VS2026

1. Open `SolutionSherlock.sln` in VS2026.
2. Let NuGet restore (`XrmToolBox` package). VS2026's restore is unchanged from
   VS2022 for this project shape.
3. Build. Output lands in `src/bin/Debug/SolutionSherlock.dll`.

### Debugging inside XrmToolBox

XrmToolBox itself isn't part of this repo. To debug interactively:

1. Download/install XrmToolBox (or copy an existing installed copy's folder).
2. In the project's **Debug** properties (VS2026: Project → Properties → Debug → General):
   - **Start external program**: path to `XrmToolBox.exe`
   - **Working directory**: that same folder
3. Copy `SolutionSherlock.dll` (and `.pdb`) into that XrmToolBox
   installation's `Plugins` folder after each build (or add a post-build
   `xcopy`/`Copy-Item` step targeting it).
4. F5. XrmToolBox launches, the tool appears in the plugin list as **SolutionSherlock**.

---

## 3. Testing against On-Premises vs Online

No code path in this project distinguishes deployment type for the core search —
`Service` is just an `IOrganizationService`, and the same `RetrieveAllEntitiesRequest`
/ `RetrieveEntityRequest` / `solutioncomponent` queries work identically against
both, because those requests have been supported since CRM 2011. What *does*
differ:

- **Connecting** — entirely handled by XrmToolBox's own connection manager
  before this plugin ever sees a `Service`. On-Premises supports Windows
  Integrated/Claims/ADFS; Online supports OAuth/Azure AD (including MFA). This
  project never builds a connection string itself.
- **Status label** — `UpdateConnection()` reads `ConnectionDetail.UseOnline` to
  show "Online" vs "On-Premises" plus the org version, so you can visually
  confirm which kind of environment you're connected to while testing.
- **"Open Solution in Maker Portal"** — only works for Online connections
  (`ConnectionDetail.EnvironmentId` is only populated there). On On-Premises
  connections the menu item shows an explanatory message instead of failing.

To test both paths: connect once via an Online (`*.dynamics.com`) environment
and once via an On-Premises/IFD environment (or a Dataverse-compatible
on-prem test org), and run the same search against each. Results, grouping,
and the managed/unmanaged filter should behave identically; only the status
label and Maker Portal link should differ.

---

## 4. Project layout

```
src/
 ├─ SolutionSherlock.csproj
 ├─ SolutionSherlockPlugin.cs              MEF entry point (IXrmToolBoxPlugin)
 ├─ SolutionSherlockControl.cs             Main UI logic
 ├─ SolutionSherlockControl.Designer.cs    Hand-authored WinForms layout
 ├─ Models/
 │   ├─ ComponentTypeInfo.cs               ComponentTypeKind enum + display info
 │   ├─ SearchCriteria.cs
 │   ├─ SearchResult.cs
 │   └─ SolutionInfo.cs
 ├─ Helpers/
 │   ├─ ComponentTypeMap.cs                componenttype numeric codes + sub-type rules
 │   └─ MetadataExtensions.cs              Label -> string helpers
 └─ Services/
     ├─ MetadataResolver.cs                Entity/attribute metadata + caching
     ├─ SolutionCache.cs                   Solution/publisher caching
     └─ SearchEngine.cs                    Orchestrates the search + solutioncomponent join
```

The `SolutionSherlockControl.Designer.cs` file is hand-written to the
same shape VS's Windows Forms Designer would generate. Opening the control in
Designer view in VS2026 loads it normally, and further visual edits are
written back into that file as usual.

---

## 5. Extending this tool (Roadmap Phases 3–6)

- **More main component types** (Option Set, Web Resource, Process, Plugin
  Assembly, Security Role, ...): add a metadata-resolution method to
  `MetadataResolver`, then a branch in `SearchEngine.Search()` alongside
  `SearchEntitiesOnly`/`SearchEntityFields`. The numeric `componenttype`
  codes for these are already registered in `ComponentTypeMap.All`.
- **More sub-component types under Entity** (Form, View, Relationship): these
  are already listed in `ComponentTypeMap.SubTypesByMain[ComponentTypeKind.Entity]`
  for the dropdown; you only need to add a `MetadataResolver.ResolveXxx(...)`
  method and a branch in `SearchEngine.Search()` similar to `SearchEntityFields`.
- **Performance at very large scale**: swap `RetrieveAllEntitiesRequest` in
  `MetadataResolver.GetAllEntitiesLightweight()` for `RetrieveMetadataChangesRequest`
  with a server-side `MetadataFilterExpression`, which reduces payload size
  further — at the cost of requiring a newer platform version (this is why the
  MVP deliberately avoids it; see the comment in `MetadataResolver.cs`).
- **Export to CSV/Excel**: `RenderResults()` already builds a flat anonymous
  projection per row — write that same shape out with a `StreamWriter` /
  CSV library instead of (or in addition to) binding it to `dgvResults`.
- **Saved searches**: use XrmToolBox's `SettingsManager.Instance` (see
  `PluginControlBase`) to persist a `SearchCriteria` list per plugin, same
  pattern used by most XrmToolBox tools.

---

## 6. Before distributing

- Bump `AssemblyVersion`/`AssemblyFileVersion` in `Properties/AssemblyInfo.cs`
  **and** `<version>` in `SolutionSherlock.nuspec` together on every release —
  they must match, and XrmToolBox's Tool Library uses the assembly version to
  detect available updates.
- Update `RepositoryName`, `UserName`, `HelpUrl` in `SolutionSherlockControl.cs`
  if the repository location changes, and keep `SolutionSherlock.nuspec`'s
  `projectUrl`/`iconUrl` in sync.
- Build the Tool Library NuGet package from the repository root:

  ```powershell
  dotnet build SolutionSherlock.sln -c Release
  dotnet pack src\SolutionSherlock.csproj -c Release -p:NuspecFile="..\SolutionSherlock.nuspec" -p:NuspecBasePath=".." -p:NoDefaultExcludes=true -o dist
  ```

  This produces `dist\BeshoyFanous.XrmToolBox.SolutionSherlock.<version>.nupkg`
  containing only `SolutionSherlock.dll`/`.pdb`/`.dll.config` plus the
  third-party assemblies XrmToolBox doesn't already ship (currently
  `Microsoft.Toolkit.Uwp.Notifications` and its dependencies), all under
  `lib\net481\Plugins` — no Dataverse SDK/XrmToolBox host assemblies are
  included, per XrmToolBox's packaging rules. If you add/remove NuGet
  dependencies, re-check `src\bin\Release` and update the `<files>` list in
  `SolutionSherlock.nuspec` accordingly.
- Publish the `.nupkg` to nuget.org, then register the package on the
  [Tool Library submission page](https://www.xrmtoolbox.com/plugins/new/).
  See the [Tool Library validation checklist](https://www.xrmtoolbox.com/documentation/for-developers/deploy-your-plugin-in-plugins-store/plugin-validation-check-list/)
  before submitting.

---

## 7. Component Details panel & grouped component list (latest update)

> **Note on this README:** the rest of this file predates several rounds of
> features that are actually present in the code (Abort/cancellation, the
> activity log, sorting, solution export, etc. - see the class-level comments
> in `SolutionSherlockControl.cs` and `BrowseSolutionsViewModel.cs` for
> those). This section only documents what changed in this latest update;
> reconciling the rest of the document with the current code is a separate
> cleanup task, not attempted here.

### Component Details panel

Selecting a (non-header) row in the Browse Solutions tab's component grid
populates a small panel below it with that component's **Name**, **Logical
Name**, **Created On**, and **Modified On** - `pnlComponentDetails` in the
Designer, wired via `dgvSolutionComponents.SelectionChanged`.

- Dates use the exact same format already used for solutions' Modified On
  column (`yyyy-MM-dd HH:mm`, converted to local time) - see
  `FormatComponentDate` in `SolutionSherlockControl.cs`.
- Missing values show `—`, matching the placeholder convention already used
  elsewhere (e.g. the Customizable column).
- Created On / Modified On are only available for component kinds backed by
  an actual Dataverse table row (Web Resource, Process, Form, View, Chart,
  templates, etc. - everything with `IsDataRecord = true` in
  `ComponentTypeMap`). Entity and Option Set are metadata, not table rows, and
  don't expose these as simple dates via the metadata API, so they show `—`
  too - this is a real data-availability limit, not a bug.

### Grouped component list

The same grid now groups rows under a shaded, bold heading per Component
Type (e.g. "Form (12)", "Web Resource (4)") instead of a flat list. This
reuses the existing `DataGridView` rather than introducing a new control:
header rows are just regular bound rows (`SolutionComponentDisplayRow.IsGroupHeader
= true`) rendered specially by a `CellFormatting` handler - the simplest
reliable way to get a group-heading look without custom drawing or a
different control type.

- **Ordering is unchanged, not re-sorted.** The list was already effectively
  grouped (`OrderBy(TypeDisplay).ThenBy(DisplayName)`, set when "View
  Components" loads); grouping now just adds visible headers on top of that
  same order. LINQ's `GroupBy` is documented to preserve first-seen-key and
  in-group order, so grouping an already-sorted list reproduces it exactly.
- **No empty groups are possible** - groups are derived only from components
  that exist, never from a fixed list of all known types.
- **CSV export is unaffected.** `_lastComponentDetails` (what "Export
  Components" writes) stays the original flat list; only the grid's
  `DataSource` gets the grouped/header-injected projection
  (`BuildGroupedComponentRows`). This was a deliberate separation, not an
  oversight - it guarantees the exported CSV can never accidentally contain a
  header-marker row.
- Header rows can't be selected (clicking one clears the grid selection
  instead) since they carry no component data for the details panel to show.

### A regression found and fixed along the way

Opening `SolutionSherlockControl.Designer.cs` in Visual Studio's
WinForms Designer (evident from the file's re-serialized, standard VS
format) silently dropped `AutoGenerateColumns = false` from all three grids
(`dgvResults`, `dgvSolutions`, `dgvSolutionComponents`). Since every grid's
columns are configured by hand in code (`Configure*GridColumns()`), this
would have caused `DataGridView`'s default `AutoGenerateColumns = true` to
add a second, duplicate set of columns generated from each bound row type's
public properties the next time a `DataSource` was set - not something this
update introduced, but directly relevant here since grouping required
touching `dgvSolutionComponents`'s binding anyway. Restored on all three
grids as part of this change.

---

## 8. Entity sub-components now group under their parent entity (latest update)

Fields, Forms, Views, Charts, and Business Rules now group under their parent
entity's display name (e.g. "Account", "Contact") instead of a flat
"Field"/"Form"/"View" type bucket - see `GetGroupKey` and
`BuildGroupedComponentRows` in `SolutionSherlockControl.cs`. Within an
entity's group, the Entity's own row (if it's also a component of the
solution) sorts first, followed by its sub-components alphabetically.
Relationship and Key remain grouped by type (see below - their parent entity
still isn't resolved).

### Standalone Field components now resolve properly

Previously, a Field added to a solution as its own standalone component
(rather than implicitly via its parent Entity's "all objects" setting) showed
a placeholder instead of its real name, its GUID instead of a logical name,
and no dates - documented at the time as a known limitation, since resolving
an attribute's parent entity from just its MetadataId isn't possible without
a bulk, cross-entity metadata query.

That query is now implemented: `MetadataResolver.ResolveAttributesByMetadataId`
uses `RetrieveMetadataChangesRequest` with an `EntityQueryExpression` whose
`AttributeQuery.Criteria` filters by `MetadataId` - the documented way to
search for specific attributes across every entity in the org without
already knowing which one they belong to. The filter is built as an OR of
per-id `Equals` conditions rather than a single `MetadataConditionOperator.In`
condition: `In` is real and documented, but its expected value type for a
multi-value list isn't clearly documented anywhere findable, whereas the
OR/Equals form is a directly confirmed, working pattern from Microsoft's own
sample code.

**What's fixed vs. what's still a known limit, for a standalone Field:**

| | Before | Now |
|---|---|---|
| Display name | `(Field — search by parent Entity to see details)` | Real display name |
| Logical name | The raw GUID | Real logical name |
| Parent entity / grouping | N/A (flat "Field" bucket) | Groups under the real parent entity |
| Created On / Modified On | Empty | **Still empty - not a bug.** Attribute metadata has no `createdon`/`modifiedon` exposed via the metadata API, unlike ordinary table rows (Forms, Web Resources, ...). This is a genuine data-availability limit of the platform, not something this fix could resolve. |

**Relationship and Key are unchanged** - still shown with a placeholder name
rather than a resolved one. `RelationshipQueryExpression` and
`EntityKeyQueryExpression` do exist (confirmed against Microsoft's docs), but
their exact property names and filtering shape weren't verified with the same
confidence as `AttributeQuery`, and shipping an unverified guess here risked
reproducing the exact bug this update fixes for Field. Extending this same
pattern to Relationship/Key is a scoped, well-understood follow-up once that
shape is confirmed - `ResolveFieldComponents` in `SolutionExplorerService.cs`
is the template to copy.

---

## 9. "Entity" super-group and collapse/expand (latest update)

The component grid's grouping is now two levels deep:

```
▼ Entity (2)
     ▼ Account (3)
          Account (Entity)
          industrycode (Field)
          Account Main Form (Form)
     ▼ Contact (2)
          Contact Quick Create Form (Form)
          ...
▼ Process (2)
     ...
▼ Web Resource (4)
     ...
```

Everything with a resolvable parent entity (Field, Form, View, Chart, Business
Rule, and the Entity component itself) nests under one "Entity" super-group
instead of being siblings of Web Resource/Process/etc. at the top level.
Relationship and Key - still unresolved to a parent, see section 8 - remain
their own top-level groups, since nesting them under "Entity" without knowing
*which* entity would be misleading.

### Collapse/expand

Click any header row (top-level or the nested per-entity ones) to
collapse/expand it - `▼`/`▶` shows current state. This is implemented as row
visibility toggling on the existing `DataGridView`
(`ApplyGroupVisibility`/`DgvSolutionComponents_CellClick` in
`SolutionSherlockControl.cs`), not a different control: the full row
list (headers + members, expanded or not) is always bound, and collapsing
just hides the affected rows and repaints the header's glyph - no rebind, no
flicker, no lost scroll position. Collapse state resets to "everything
expanded" each time a fresh roster loads (new solution, page change,
reconnect), tracked in `_collapsedGroupKeys`.

---

## 10. Export Solution — advanced system settings (latest update)

The Browse Solutions tab's Export Solution flow now exposes an
**Include System Settings (Advanced)** option in addition to the
existing **Export as Managed** checkbox. Both are independent and
default to **unchecked**.

### UI

```
Export Solution

☐ Export as Managed
☐ Include System Settings (Advanced)     [ Configure... ]
```

- **`☐ Export as Managed`** — unchanged. Unchecked = Unmanaged export
  (the default). Checked = Managed export.
- **`☐ Include System Settings (Advanced)`** — unchecked by default.
  When unchecked, **no system settings are included in the export**,
  regardless of what was previously configured in the dialog. This is
  the current out-of-the-box behavior of the tool.
- **`[ Configure... ]`** — disabled while Include System Settings is
  unchecked. Enabling the checkbox enables the button; clicking it
  opens the advanced dialog.

### Advanced dialog

```
Export System Settings (Advanced)

Select the system settings you want to include:

☐ Calendar
☐ Customization
☐ Email tracking
☐ General
☐ Marketing
☐ Outlook Synchronization
☐ Relationship Roles
☐ ISV Config
☐ Sales

[ Select All ]    [ Clear All ]      [ Cancel ]  [ Apply ]
```

Nine per-setting checkboxes, each mapped 1-1 to a flag on
`Microsoft.Crm.Sdk.Messages.ExportSolutionRequest`
(`ExportCalendarSettings`, `ExportCustomizationSettings`,
`ExportEmailTrackingSettings`, `ExportGeneralSettings`,
`ExportMarketingSettings`, `ExportOutlookSynchronizationSettings`,
`ExportRelationshipRoles`, `ExportIsvConfig`, `ExportSales`). The
dialog operates on a **working copy** of the current selection:

- **Apply** commits the working copy back onto the persistent
  selection.
- **Cancel** discards changes.
- **Select All** ticks every checkbox in the dialog (the working
  copy - persist only on Apply).
- **Clear All** unticks every checkbox in the dialog.

### Behavior

| Export as Managed | Include System Settings | Effective request |
|---|---|---|
| Unchecked | Unchecked | Unmanaged, no system settings (unchanged default). |
| Checked | Unchecked | Managed, no system settings. |
| Unchecked | Checked | Unmanaged + only the settings the user ticked in Configure... |
| Checked | Checked | Managed + only the settings the user ticked in Configure... |

- **When Include System Settings is unchecked, all nine individual
  settings are treated as unchecked**, regardless of prior
  configuration - the check happens in
  `BrowseSolutionsViewModel.BuildExportOptions`, which only invokes
  `ExportSolutionOptions.ApplySystemSettings` when
  `IncludeSystemSettings == true`. Stale UI selections cannot leak
  into the export request.
- Toggling Include System Settings off **does not clear the user's
  selections** in the dialog — they're retained as a UX convenience so
  briefly unchecking and re-checking doesn't wipe your picks. The
  effective export request is still gated exclusively by the
  Include System Settings checkbox.
- **Validation**: If Include System Settings is checked but no
  individual setting is picked, the export is refused with a
  "Please select at least one setting, or uncheck Include System
  Settings" message — no invalid request is sent to Dataverse.
- **Per-connection reset**: switching to a different connection
  resets Include System Settings to unchecked and clears the
  underlying selection, matching the overall "unchecked by default"
  rule.
- **`ExportAutoNumberingSettings`** exists on the SDK request but is
  not part of the nine user-visible checkboxes; it stays `false` on
  every export request.

The nine supported settings and their SDK mapping are defined once in
[`SystemSettingsSelection.Definitions`](src/Models/SystemSettingsSelection.cs)
(display label + strongly-typed getter/setter). Adding a new setting
is a one-line change to that array plus a matching new property on
`SystemSettingsSelection`.

