# SolutionSherlock — Regression Validation

Every feature listed in [FUNCTIONALITY_INVENTORY.md](FUNCTIONALITY_INVENTORY.md)
was re-verified against the current source after the refactoring pass.
The pass touched exactly four files in code
(`SolutionSherlockControl.cs`, `SolutionExplorerService.cs`,
`MetadataResolver.cs`) and produced no behavior change other than the
single documented bug fix in `BtnLoadAllSolutions_Click`.

Verification method: **static regression review**. No test project
exists in the repo (see [CODE_AUDIT_REPORT.md](CODE_AUDIT_REPORT.md#12-informational--no-test-project));
a manual smoke test in XrmToolBox against a live Dataverse environment
is a recommended follow-up before shipping.

Status legend: `PASS` · `PASS WITH NOTES` · `REQUIRES REVIEW`.

---

## Plugin entry / MEF registration

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| MEF registration ([SolutionSherlockPlugin.cs](src/SolutionSherlockPlugin.cs)) | `Export(typeof(IXrmToolBoxPlugin))` + `ExportMetadata` produces the plugin tile | Unchanged | **PASS** | No touch. |
| `GetControl()` returns new `SolutionSherlockControl` | Same | Unchanged | **PASS** | |
| Plugin metadata (name, description, colours, images, version, GUID) | Same | Unchanged | **PASS** | |

---

## Connection lifecycle

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| `UpdateConnection` rebuilds all connection-scoped services | Recreates `SolutionCache`, `MetadataResolver`, `SearchEngine`, `SolutionExplorerService`, `SolutionExportService`, `BrowseSolutionsViewModel` | Unchanged | **PASS** | |
| Connection status label | Shows friendly name + Online/On-Premises + org version | Unchanged | **PASS** | |
| Grids and detail panels cleared | `dgvSolutions`, `dgvSolutionComponents`, `_lastComponentDetails`, `_collapsedGroupKeys`, details labels reset | Unchanged | **PASS** | |
| Pin state reset | `_solutionsPinned = _componentsPinned = false` | Unchanged | **PASS** | |
| Dock layout reset | Solutions expanded, Components collapsed | Unchanged | **PASS** | |

---

## Search Components tab

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| Initialize main / sub combos ([`InitializeSearchControls`](src/SolutionSherlockControl.cs)) | Populates `cboMainType` from `MainTypes`; wires all events | Unchanged | **PASS** | |
| `RefreshSubTypeOptions` — Workflow shows Category dropdown, others show sub-type dropdown | Same | Unchanged | **PASS** | |
| Rejection of entirely blank criteria | Info dialog "Refine your search" | Unchanged | **PASS** | |
| `SearchCriteria` construction | Same shape (MainType, MainSearchText, SubType, SubSearchText, ProcessCategory, ManagedOnly, UnmanagedOnly) | Unchanged | **PASS** | |
| Cancellation begin / end via `BeginAsyncOperation` / `EndAsyncOperation` | Same | Unchanged | **PASS** | |
| `WorkAsync` dispatch of `SearchEngine.Search` | Same, cold-cache warmup via `SolutionCache.Refresh` on first search | Unchanged | **PASS** | |
| Cancellation via `OperationCanceledException` → `args.Cancel = true` | Same | Unchanged | **PASS** | |
| PostWorkCallBack rendering + error dialog + activity-log lines | Same | Unchanged | **PASS** | |
| Result ordering (`OrderResults` — by solution then component) | Same | Unchanged | **PASS** | |
| `RenderResults` summary + row projection to `SearchResultRow` | Same columns (Solution, Managed/Unmanaged, Publisher, Component Type, Component, Parent Entity, Version, Solution URL, Component GUID) | Unchanged | **PASS** | |
| Right-click context menu (Copy solution name / Copy component name / Open solution) | Same wiring | Unchanged | **PASS** | |
| `DgvResults_CellContentClick` — reflection-based URL fallback | Retained per preservation principle | Unchanged | **PASS** | See [CODE_AUDIT_REPORT.md #4](CODE_AUDIT_REPORT.md#4-medium--fragile-url-resolution-in-dgvresults_cellcontentclick). |
| `OpenUrl(BuildSolutionUrl(id))` for right-click "Open in Maker Portal" | Same URL format (Online vs On-Prem) | Unchanged | **PASS** | |

---

## Browse Solutions tab

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| **`BtnLoadAllSolutions_Click` — action buttons stay disabled during the load** | ❌ Buttons briefly re-enabled after `WorkAsync` returned synchronously (outer `finally { EndAsyncOperation(); }`) | ✅ Buttons remain disabled until `PostWorkCallBack` runs | **PASS WITH NOTES** | **Intentional behavior fix** — see [CHANGELOG.md#unreleased](CHANGELOG.md#unreleased--audit-pass-2026-08-25). This is the only user-visible behavior change in the pass. |
| `BtnLoadAllSolutions_Click` progress log lines | `SearchProgress` inside `WorkAsyncInfo.Work` → `worker.ReportProgress` → `AppendLog` | Unchanged | **PASS** | The outer dead `IProgress<string>`/`SearchProgress` locals were removed; the surviving progress path is the same one the code always used. |
| Cancellation of the load | `OperationCanceledException` → `args.Cancel = true` → PostWorkCallBack "Solution load cancelled." | Unchanged | **PASS** | Cancellation message + `_operationStopwatch.ElapsedMilliseconds` line now live in PostWorkCallBack (moved from the outer catch, which was unreachable). |
| Error handling | Server / SDK failure → `args.Error` → PostWorkCallBack error dialog + `LogError(args.Error.ToString())` | Unchanged | **PASS** | Outer `catch (Exception ex)` block was unreachable and is removed. |
| Solution grid rendering (`RenderSolutionsPage`) | Same columns, sort arrows, page indicator, "No solutions found." / "No solutions match '{term}'." text, "Page X of Y (Z solutions, 20 per page)" | Unchanged | **PASS** | |
| Filter text change → reset to page 0 | Same | Unchanged | **PASS** | |
| Sort column click → toggle direction / switch column ascending | Same | Unchanged | **PASS** | |
| Prev/Next page buttons + enabled-state logic | Same | Unchanged | **PASS** | |
| Selection-change resets component roster state | Same (`_lastComponentDetails = null`, collapse cleared, details reset, export disabled) | Unchanged | **PASS** | |
| Solution "Actions" per-row menu | Same wiring (View Components, Open in Browser, Export Solution) | Unchanged | **PASS** | |
| Double-click on a solution row → export | Same handler (`DgvSolutions_CellDoubleClick`) | Unchanged | **PASS** | |
| Pin / collapse / restore dock panel behavior | Same state machine, `_solutionsPinned` / `_componentsPinned` invariants preserved | Unchanged | **PASS** | |

---

## Component roster / `SolutionExplorerService.ListComponents`

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| Retrieve `solutioncomponent` rows | Same paged query (columns objectid / componenttype / rootcomponentbehavior; page 5000) | Unchanged | **PASS** | |
| Missing `componenttype` code → `-1` filtered | Same | Unchanged | **PASS** | |
| Entity component resolution (`ResolveEntityComponents`) | Same fields (DisplayName, LogicalName, State, Customizable from `IsCustomizable`, Description, ParentEntity = self) | Unchanged | **PASS** | |
| Synthetic "Hierarchy Settings" row on self-referencing IsHierarchical 1:N | Same | Unchanged | **PASS** | |
| Global Option Set resolution | Same | Unchanged | **PASS** | |
| Data-record resolution (Web Resource, Process, Role, Report, templates, App Module, Duplicate Rule, Connection Role, Routing Rule, SLA, Convert Rule, Mobile Offline Profile, Dashboard) | Same (batch 500, page 5000, columns: primary name, createdon, modifiedon, optional description, optional parent entity) | Unchanged | **PASS** | |
| Systemform resolution (Form / Dashboard distinguished by `type == 0`) | Same (metadata retrieve for primary key + attribute list, fallback to `formid` on failure) | Unchanged | **PASS** | Broad `catch` retained per preservation principle. |
| Workflow resolution (categories 0..6 → labels) | Same | Unchanged | **PASS** | |
| Standalone Field resolution (`ResolveFieldComponents`) | Same via `ResolveAttributesByMetadataId`; unresolved IDs → `BuildUnresolvedPlaceholders` | Unchanged | **PASS** | Duplicated `<summary>` XML tag collapsed — no code path affected. |
| Standalone Relationship resolution | Same; N:N produces two rows (one per participating entity); unresolved IDs → placeholders | Unchanged | **PASS** | |
| Standalone Key resolution | Same; unresolved IDs → placeholders | Unchanged | **PASS** | |
| Implicit entity subcomponent expansion (`rootcomponentbehavior == 0`) | Same set (Fields, 1:N/N:1/N:N Relationships, Keys, Forms with `type != 0`, Views, Charts, Business Rules); dedupe against `alreadyResolvedIds`; dashboards deliberately excluded | Unchanged | **PASS** | Broad `catch` retained. |
| Unknown componenttype → placeholder row | Same | Unchanged | **PASS** | |
| `ResolveEntityDisplayName` uses `MetadataResolver.GetEntityDisplayLabel` (O(1) dict lookup) | Unchanged | **PASS** | Preserved from the 2026-08-17 perf pass. |

---

## Component grouping / details UI

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| `BuildGroupedComponentRows` two-level hierarchy | Same (Entity super-group → per-entity → per-type; non-entity components as top-level type groups) | Unchanged | **PASS** | |
| Entity-scoped rows anchor Entity's own row first | Same | Unchanged | **PASS** | |
| Canonical sub-type order (`EntitySubComponentTypeOrder`) | Same | Unchanged | **PASS** | |
| Alphabetical fallback for unknown sub-type names | Same | Unchanged | **PASS** | |
| Collapse/expand via `CellClick` on header rows | Same | Unchanged | **PASS** | |
| Top-level headers always visible | Same | Unchanged | **PASS** | |
| Header cell formatting (bold + shaded + ▼/▶ glyph) | Same | Unchanged | **PASS** | |
| Nested row indent | Same | Unchanged | **PASS** | |
| Details panel populated by `SelectionChanged` | Same fields (Name, Logical Name, Created On, Modified On) | Unchanged | **PASS** | |
| Group headers clear selection instead of populating details | Same | Unchanged | **PASS** | |
| Date format `yyyy-MM-dd HH:mm` (local time) | Same | Unchanged | **PASS** | |
| Missing values → `—` placeholder | Same | Unchanged | **PASS** | |

---

## CSV export (components)

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| Suggested filename `{solution}_components.csv` | Same via `SanitizeFileName` | Unchanged | **PASS** | |
| Save dialog + user cancellation handling | Same | Unchanged | **PASS** | |
| UTF-8 BOM output | Same (`new UTF8Encoding(true)`) | Unchanged | **PASS** | |
| Columns: Display Name, Name, Type, State, Customizable, Description | Same | Unchanged | **PASS** | |
| CSV escaping via `CsvEscape` | Same (RFC 4180 style) | Unchanged | **PASS** | |
| Confirmation dialog + log line on success | Same | Unchanged | **PASS** | |
| Error dialog + `LogError` on failure | Same | Unchanged | **PASS** | |

---

## Solution ZIP export

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| Connection / selection guards | Same | Unchanged | **PASS** | |
| Suggested filename `{UniqueName}_{Version}.zip` | Same | Unchanged | **PASS** | |
| `IDialogService.ShowSaveFileDialog` used | Same | Unchanged | **PASS** | |
| Action buttons disabled during export | Same (`SetAllActionButtonsEnabled(false)`) | Unchanged | **PASS** | |
| Marquee progress bar visibility | Same | Unchanged | **PASS** | |
| `LogInfo` "Export started" entry | Same | Unchanged | **PASS** | |
| `Progress<ExportStageProgress>` updates `lblExportStatus.Text` | Same | Unchanged | **PASS** | Stray `// after` marker removed above this line — no behavior change. |
| `ExportSolutionRequest` options mapping | Same (Managed, AutoNumbering, Calendar, Customization, EmailTracking, General, Marketing, OutlookSync, RelationshipRoles, Sales, IsvConfig) | Unchanged | **PASS** | |
| Task.Run wrapper around `_service.Execute` | Same | Unchanged | **PASS** | |
| `IFileSystemService.WriteAllBytesAsync` called with returned bytes | Same | Unchanged | **PASS** | |
| Progress stages (Preparing → Exporting → Saving → Completed) | Same | Unchanged | **PASS** | |
| `LastExportFolder` remembered per view-model instance | Same | Unchanged | **PASS** | |
| Success dialog + log line | Same | Unchanged | **PASS** | |
| Specific catches (InvalidOperationException / FaultException / TimeoutException / IOException / Exception) | Same messages, same `LogError` / `LogWarning` calls | Unchanged | **PASS** | |
| Cancellation limitation (only before Execute) | Same, documented on `ISolutionExportService` | Unchanged | **PASS** | |

---

## Docking / pinning / theming

| Feature | Original behavior | Final behavior | Status | Notes |
|---|---|---|---|---|
| `ApplyModernTheme` cosmetic pass | Same | Unchanged | **PASS** | |
| Pin toggle → styled chip + prevents auto-collapse | Same | Unchanged | **PASS** | |
| Arrow click on pinned panel reveals both panels 50/50 | Same via `ShowBothPanelsSideBySide` | Unchanged | **PASS** | |
| Auto-collapse honors pin state | Same | Unchanged | **PASS** | |
| Collapsed panel shows compact tab strip | Same | Unchanged | **PASS** | |

---

## Cross-cutting invariants

| Invariant | Before | After | Status |
|---|---|---|---|
| `_lastComponentDetails` matches the currently selected solution | Held | Held | **PASS** |
| Solution-selection change clears roster + disables export | Held | Held | **PASS** |
| `_collapsedGroupKeys` corresponds to the currently bound grouped rows | Held | Held | **PASS** |
| `_browseViewModel` recreated per connection | Held | Held | **PASS** |
| Solution cache / metadata cache never shared across connections | Held | Held | **PASS** |
| Only one operation can be in flight at a time | ❌ *Violated* by `BtnLoadAllSolutions_Click` outer finally | ✅ Held | **PASS WITH NOTES** — see the `BtnLoadAllSolutions_Click` row above; this is the intentional fix. |

---

## Build validation

| Check | Result |
|---|---|
| `dotnet build SolutionSherlock.sln -v:minimal` | **Succeeded**, 0 errors, 1 pre-existing warning (`MSB3277` — Microsoft.IdentityModel.Clients.ActiveDirectory version conflict, unchanged since before the audit). |
| Target framework | `net481` (unchanged). |
| Assembly version | `1.0.0.0` (unchanged). |
| NuGet package references | Unchanged (`Microsoft.CrmSdk.CoreAssemblies` 9.0.2.60, `Microsoft.CrmSdk.XrmTooling.CoreAssembly` 9.1.1.65, `Microsoft.Toolkit.Uwp.Notifications` 7.1.0). |
| Local `lib/` references | Unchanged (`XrmToolBox.Extensibility`, `McTools.Xrm.Connection`, `McTools.Xrm.Connection.WinForms`, `Microsoft.Toolkit.Uwp.Notifications`). |
| Post-build copy step | Unchanged (`copy /Y "$(TargetPath)" "C:\Users\Beshoy.Fanous\Downloads\XrmToolboxNewTool\Plugins\"`). |

---

## Summary

- **Features preserved**: 100%.
- **Public API changes**: none.
- **CRM SDK request shape changes**: none.
- **CSV column changes**: none.
- **UI layout changes**: none.
- **Threading model changes**: none — but a bug that violated the
  single-operation-in-flight invariant is fixed by removing an
  unreachable outer `try/catch/finally`.
- **Documentation added**: 6 new Markdown files, all cross-linked.

Recommended next step before shipping: a manual smoke test in
XrmToolBox against an Online *and* an On-Premises Dataverse
environment, exercising Search Components, Load All Solutions, View
Components (including one solution added with "Include Subcomponents"),
component CSV export, and solution ZIP export in both managed and
unmanaged form.
