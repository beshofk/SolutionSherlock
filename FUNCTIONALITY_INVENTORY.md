# SolutionSherlock — Functionality Inventory

**Purpose:** Baseline of every feature and behavior discovered in the
codebase, produced *before* any refactoring, so that regressions can be
detected by re-checking against this list.

**Method:** Read-only audit of every source file under `src/`, including
services, view models, models, helpers, interfaces, the WinForms control,
and the plugin entry point.

**Scope note:** The inventory records what the code actually does today.
It does not include features documented elsewhere (README/roadmap) that
are not present in code.

---

## 1. Plugin entry and composition

### Feature: MEF plugin registration

- **Purpose:** Exposes the plugin to XrmToolBox through MEF metadata.
- **Entry point:** `SolutionSherlockPlugin.GetControl()`.
- **Files/classes:** `SolutionSherlockPlugin.cs` (`SolutionSherlockPlugin : PluginBase`).
- **Inputs:** XrmToolBox plugin discovery.
- **Outputs:** A new `SolutionSherlockControl` instance.
- **Dependencies:** `IXrmToolBoxPlugin`, `PluginBase`, MEF `Export` / `ExportMetadata`.
- **Flow:** Assembly scanned → MEF metadata read (name, description, colors, images) → `GetControl()` constructs the control.
- **Assumptions:** Parameterless construction, in-process host, service composition happens inside the control.

### Public plugin metadata

- Name: `SolutionSherlock`
- Repository: `SolutionSherlock`
- User: `beshofk`
- Help URL: `https://github.com/beshofk/SolutionSherlock`
- Assembly version: `1.0.0.0`
- Assembly title/product: `SolutionSherlock`
- Assembly GUID: `6f2f2e3f-2a6b-4a34-8a9e-9b0a2e3d7c11`

---

## 2. Connection lifecycle and service composition

### Feature: Connection initialization / reset

- **Purpose:** Rebuild all connection-scoped services and reset UI state whenever XrmToolBox changes connections.
- **Entry point:** `SolutionSherlockControl.UpdateConnection(...)`.
- **Inputs:** `IOrganizationService newService`, `ConnectionDetail detail`, `actionName`, `parameter`.
- **Outputs:** Recreated service graph, cleared grids/details/filters/collapse state, updated status label (Online/On-Premises + version).
- **Dependencies:** `SolutionCache`, `MetadataResolver`, `SearchEngine`, `SolutionExplorerService`, `SolutionExportService`, `BrowseSolutionsViewModel`.
- **Assumptions:** Metadata/solution caches are per-connection; recreating the view model is required to reset paging/sorting/selection; `detail` may be null.

---

## 3. Search Components UI

### Feature: Component search criteria selection

- **Entry points:** `InitializeSearchControls()`, `RefreshSubTypeOptions()`, `CboSubType_SelectedIndexChanged(...)`.
- **Controls:** `cboMainType`, `cboSubType`, `txtMainSearch`, `txtSubSearch`, `chkManagedOnly`, `chkUnmanagedOnly`.
- **Behavior:**
  - Main types from `ComponentTypeMap.MainTypes`.
  - Entity exposes Field, Form, View, Chart, Key, Relationship, Business Rule as sub-types.
  - Process replaces sub-type controls with a category selector.
  - Selecting no entity sub-component disables the sub-search field.
  - Changing main type clears sub-search state.
- **Edge cases:** Blank text allowed if process category selected; entirely blank criteria are rejected.

### Feature: Execute component search

- **Entry point:** `BtnSearch_Click(...)`.
- **Inputs:** Selected main/sub types, main/sub text, process category, managed/unmanaged filters, active `IOrganizationService`.
- **Outputs:** `List<SearchResult>` → `SearchResultRow` bound to `dgvResults`, summary counts, activity-log entries.
- **Flow:** Validate connection → build criteria → reject blank searches → clear existing rows → begin async op → `WorkAsync` → load solution cache (if empty) → `SearchEngine.Search` → render results / handle cancellation / show errors.
- **Error handling:** `OperationCanceledException` → cancelled; broad `catch` in callback → error dialog + log stack.
- **Threading:** CRM work inside `WorkAsync`; log messages via `BackgroundWorker.ReportProgress`; final render in `PostWorkCallBack`.

### Feature: Search result rendering and sorting

- **Entry points:** `RenderResults(...)`, `OrderResults(...)`.
- **Ordering:** By solution friendly name, then component display name.
- **Columns:** Solution, Managed/Unmanaged, Publisher, Component Type, Component Name, Logical Name, Parent Entity, Version, Solution URL, Component GUID.
- **Edge cases:** Null/empty result set → instructional or "no results" text. URL may be null when connection details are insufficient.

### Feature: Search result context actions

- **Entry points:** `DgvResults_CellMouseDown`, `CopySelectedValue(...)`, `OpenSelectedSolution()`.
- **Actions:** Copy solution friendly name; Copy component logical name; Open solution.
- **State:** `_rightClickedRowIndex`, `_lastResults`.
- **Fragility:** Row index tracked against a freshly ordered copy of `_lastResults` — dependency between `OrderResults` and the context menu.

### Feature: Open solution links

- **Entry points:** `DgvResults_CellContentClick`, `OpenSelectedSolution`, `OpenUrl`, `BuildSolutionUrl`.
- **URL rules:**
  - Online: `https://make.powerapps.com/environments/{EnvironmentId}/solutions/{solutionId}`.
  - On-premises: `{WebApplicationUrl}/tools/solution/edit.aspx?id={solutionId}`.
- **Edge cases:** Missing URL → informational message; `Process.Start` throwing `Win32Exception` is caught in the grid click path only.
- **Assumptions:** Online requires `EnvironmentId`; on-premises requires `WebApplicationUrl`; Maker Portal navigation uses solution GUID (not unique name).

---

## 4. Browse Solutions

### Feature: Load all visible solutions

- **Entry point:** `BtnLoadAllSolutions_Click(...)`.
- **Inputs:** Active Dataverse connection; cancellation token.
- **Outputs:** Cached `SolutionInfo`, paged grid, page indicator, activity log.
- **Flow:** Validate connection → begin async op → `WorkAsync` → `BrowseSolutionsViewModel.LoadSolutionsAsync` → `SolutionCache.LoadSolutionsAsync` (synchronous paging in `Task.Run`) → copy cache to `_allSolutions` → render first page.
- **Notes:** Method is `async void`; the `Progress<string>` and `SearchProgress` created locally are unused by the active `WorkAsync` path (dead code inside method).

### Feature: Solution cache

- **Purpose:** Cache solution metadata for the connection lifetime.
- **CRM call:** `RetrieveMultiple` on `solution`.
- **Columns:** `friendlyname`, `uniquename`, `ismanaged`, `version`, `publisherid`, `modifiedon`.
- **Filters:** `isvisible = true`, `uniquename != "Default"`.
- **Paging:** 500 per page.
- **Outputs:** `Dictionary<Guid, SolutionInfo>`, `LastRefreshedUtc`.
- **Cancellation:** Checked before each page; previous cache preserved on cancel.
- **Assumptions:** `isvisible` and unique-name != "Default" identify user solutions; publisher reference name is populated by SDK.

### Feature: Filtering, sorting, paging (in-memory)

- **Entry points:** `BrowseSolutionsViewModel.SetSearchText`, `SetSortColumn`, `NextPage`, `PreviousPage`, `GetCurrentPage`.
- **Behavior:** Page size 20; contains-match on friendly + unique name; default sort modified date descending; same-column click toggles direction; column change → ascending; search/sort reset to page 0; `GetCurrentPage` clamps out-of-range indices.
- **Edge cases:** Empty result reports 1 logical page; null names are tolerated.

### Feature: Solution grid rendering

- **Entry point:** `RenderSolutionsPage()`.
- **Fields:** Friendly name, Unique name, Managed/Unmanaged, Publisher, Version, localized Modified date, Solution GUID, Actions button.
- **Invariants:** Every render clears `_lastComponentDetails`; component export becomes disabled after paging/filtering/sorting until components reload.

### Feature: Solution action menu

- **Entry point:** `DgvSolutions_CellContentClick(...)`.
- **Actions:** View Component, Open in Browser, Export Solution.
- **Behavior:** Clicking the Actions button sets that cell current, shows `cmsSolutionActions` below the cell.

### Feature: View solution components

- **Entry point:** `BtnViewComponents_Click(...)`.
- **Outputs:** `_lastComponentDetails` (flat), grouped rows bound to `dgvSolutionComponents`, component header/count, cleared details panel, enabled Export Components when data exists.
- **Flow:** Validate selection → begin async op → `SolutionExplorerService.ListComponents` → store flat details → clear collapse state → build grouped rows → apply visibility → update counts → expand Components / auto-collapse Solutions unless pinned.

---

## 5. Component roster resolution

### Feature: Retrieve solution component rows

- **Entry point:** `SolutionExplorerService.RetrieveSolutionComponentRows(...)`.
- **CRM call:** `RetrieveMultiple` on `solutioncomponent`; columns `objectid`, `componenttype`, `rootcomponentbehavior`; page size 5000 via `QueryPaging.RetrieveAllPages`.
- **Outputs:** Tuples of (component type code, object id, root component behavior).
- **Edge cases:** Missing component type → `-1`, filtered out; missing root behavior → null.

### Feature: Entity component resolution

- **Entry point:** `ResolveEntityComponents(...)`.
- **Dependency:** `MetadataResolver.ResolveEntities(null)` → uses cached entity metadata.
- **Outputs:** Entity detail rows, `IsCustomizable`, localized description, parent-entity fields = self.
- **Extra:** Detects self-referencing hierarchical relationships and adds a synthetic "Hierarchy Settings" row.

### Feature: Global option-set resolution

- **Entry point:** `ResolveOptionSetComponents(...)`.
- **Outputs:** Option Set display name/name, localized description; Customizable = `—`.

### Feature: Generic data-record resolution

- **Entry point:** `ResolveDataRecordComponents(...)`.
- **Supported types:** Web Resource, Process, Security Role, Plugin Assembly, Plugin Step, Service Endpoint, Report, Templates, Duplicate Rule, Connection Role, App Module, Routing Rule, SLA, Convert Rule, Mobile Offline Profile, Dashboard.
- **Query:** `{table}id IN (...)`; batch 500; page 5000; columns include primary name, `createdon`, `modifiedon`, optional description/parent entity.
- **Assumptions:** Primary key naming follows `{logicalname}id`; `createdon`/`modifiedon` exist on all mapped record tables.

### Feature: System form / dashboard resolution

- **Entry point:** `ResolveSystemFormComponents(...)`.
- **CRM calls:** `RetrieveEntityRequest` for `systemform` (metadata) + batched `RetrieveMultiple`.
- **Distinction:** `type = 0` → Dashboard, else Form.
- **Error handling:** Broad catch on metadata retrieval; fallback to `formid` primary key.

### Feature: Workflow / process / business-rule resolution

- **Entry point:** `ResolveWorkflowComponents(...)`.
- **Columns:** `name`, `category`, `description`, `primaryentity`, `createdon`, `modifiedon`.
- **Categories:** 0=Workflow, 1=Dialog, 2=Business Rule, 3=Action, 4=BPF, 5=Modern Flow, 6=Desktop Flow.
- **Grouping:** Business Rule uses `primaryentity` as parent; others stay top-level.

### Feature: Standalone field resolution

- **Entry point:** `ResolveFieldComponents(...)` → `MetadataResolver.ResolveAttributesByMetadataId(...)` (RetrieveMetadataChangesRequest, batches of 50, OR of `MetadataId == id`).
- **Outputs:** Field display/logical name, parent entity, customizable status.
- **Limitations:** Attribute metadata does not expose `createdon`/`modifiedon`; unresolved IDs shown as placeholder rows.

### Feature: Standalone relationship resolution

- **Entry point:** `ResolveRelationshipComponents(...)`.
- **Outputs:** 1:N / N:1 / N:N labels; owning entity grouping; schema name and GUID.
- **Behavior:** N:N may produce two rows (one per participating entity).
- **Limitations:** No audit dates; unresolved IDs → placeholder rows.

### Feature: Standalone key resolution

- **Entry point:** `ResolveKeyComponents(...)`.
- **Outputs:** Key display/logical name; parent entity grouping.
- **Limitations:** No audit dates; unresolved keys → placeholder rows.

### Feature: Implicit entity sub-component expansion

- **Entry point:** `ResolveImplicitEntitySubComponents(...)`.
- **Trigger:** Entity row with `rootcomponentbehavior == 0` (include subcomponents).
- **CRM calls:** Full `RetrieveEntityRequest` per included entity + table queries for forms, views, charts, business rules.
- **Behavior:** Adds fields, relationships (1:N/N:1/N:N), keys, forms (excluding `type == 0`), views, charts, category-2 workflows; de-duplicates via `alreadyResolvedIds`.
- **Assumption:** Dashboards deliberately excluded from implicit expansion.

### Feature: Unknown component fallback

- **Entry point:** `ResolveUnknownComponents(...)`.
- **Outputs:** Placeholder with type label `"Type {code}"`, numeric object id, managed state.
- **Rationale:** Keep unknown component codes visible instead of dropping them silently.

---

## 6. Component grouping and details UI

### Feature: Hierarchical component grouping

- **Entry point:** `BuildGroupedComponentRows(...)`.
- **Hierarchy:** Top-level `"Entity"` super-group → per-entity groups → nested type groups (Forms, Views, Charts, Fields, Keys, 1:N, N:1, N:N, Business Rules, Hierarchy Settings, Dashboards); non-entity components grouped by type at top level.
- **Invariant:** Grouping does not mutate the flat `_lastComponentDetails` list.

### Feature: Expand/collapse component groups

- **Entry points:** `DgvSolutionComponents_CellClick`, `ApplyGroupVisibility`, `DgvSolutionComponents_CellFormatting`.
- **State:** `_collapsedGroupKeys`.
- **Behavior:** Any header click toggles the group; top-level header rows always visible; descendants hidden by top/entity/type key; glyph flips between expanded/collapsed indicator.

### Feature: Component details panel

- **Entry point:** `DgvSolutionComponents_SelectionChanged`.
- **Outputs:** Name, Logical Name, Created On, Modified On.
- **Behavior:** Group headers clear the details panel; missing dates → `—`; dates local time, format `yyyy-MM-dd HH:mm`.

---

## 7. Component CSV export

### Feature: Export component roster to CSV

- **Entry point:** `BtnExportComponents_Click(...)`.
- **Helpers:** `ExportComponentsToCsv(...)`, `CsvEscape(...)`, `SanitizeFileName(...)`.
- **Columns:** Display Name, Name, Type, State, Customizable, Description.
- **Output:** UTF-8 BOM CSV; `SaveFileDialog` suggests `{solution}_components.csv`.
- **Edge cases:** Empty/null → empty field; escapes commas, quotes, CR, LF; invalid filename chars stripped; empty sanitized name → `"solution"`.
- **Error handling:** Broad `catch (Exception)` → dialog + log.

---

## 8. Full solution ZIP export

### Feature: Export solution package

- **Entry points:** `DgvSolutions_CellDoubleClick(...)`, `ExportSelectedSolutionAsync(...)`, `BrowseSolutionsViewModel.ExportSelectedSolutionAsync(...)`.
- **Inputs:** Selected solution, managed checkbox, destination path, last export folder.
- **Outputs:** ZIP file, `ExportResult` (size + elapsed), progress status.
- **CRM request:** `Microsoft.Crm.Sdk.Messages.ExportSolutionRequest`; all boolean options mapped from `ExportSolutionOptions`.
- **Stages:** Preparing → Exporting → Saving → Completed.
- **Threading:** SDK export in `Task.Run` (blocking `Execute`); file write in `Task.Run`; `Progress<T>` marshals updates back to UI context.
- **Cancellation limits:** Checked before `Execute`; **cannot** interrupt an in-flight `ExportSolutionRequest`.
- **Error handling:** Specific catches for `InvalidOperationException`, `FaultException<OrganizationServiceFault>`, `TimeoutException`, `IOException`, plus broad `Exception`.
- **Assumptions:** Response contains `ExportSolutionFile`; target directory exists; ZIP bytes fit in memory.

### Feature: Suggested export filename

- **Entry point:** `BrowseSolutionsViewModel.BuildSuggestedFileName(...)`.
- **Behavior:** `{UniqueName}_{Version}.zip`; strips invalid chars; fallback `"solution"`/`"0.0.0.0"`.

---

## 9. UI docking, pinning, theming

### Feature: Solutions/components panel docking

- **Entry points:** `ToggleSolutionsPinned`, `ToggleComponentsPinned`, `OnSolutionsArrowClicked`, `OnComponentsArrowClicked`, `ShowBothPanelsSideBySide`, `AutoCollapseSolutions`, `AutoCollapseComponents`.
- **State:** `_solutionsPinned`, `_componentsPinned`.
- **Invariant:** Panel collapse is layout-only; must not affect data or selection.

### Feature: Shared UI theme

- **Entry point:** `ApplyModernTheme()` in `Helpers/UiTheme.cs`.
- **Effect:** Cosmetic only (colors, fonts, borders, button styles, grid styles, toolstrip rendering).
- **Static state:** Static color fields; static `Font SectionHeaderFont` (never disposed — process-lifetime).

---

## 10. Helpers

### `ComponentTypeMap`

- Central mapping `ComponentTypeKind` → display name, numeric type code, entity-scoped flag, IsDataRecord, table logical name, primary name attribute, optional description attribute, optional parent entity attribute, optional static filter.
- Public members: `All`, `SubTypesByMain`, `Get(...)`, `MainTypes`.
- Limitation: Several Dataverse component categories intentionally omitted (unverified type codes).

### `MetadataExtensions`

- Null-safe display-label helpers for `EntityMetadata`, `AttributeMetadata`, `EntityKeyMetadata`.
- Fallback = logical/name when localized label absent.

### `ProcessCategories`

- Static list: Workflow, Dialog, Action, Business Process Flow.
- Business Rule / Modern Flow / Desktop Flow deliberately not selectable via UI.

### `QueryPaging`

- `RetrieveAllPages(...)`: pages `RetrieveMultiple` until `MoreRecords == false`, checks cancellation before every request, logs per-page timing when multi-page.
- `Chunk(...)`: splits GUID lists into batches of 500.

### `UiTheme`

- Static palette + styling helpers; no CRM or data behavior.

---

## 11. Public API surface

### Plugin / control

- `SolutionSherlockPlugin.GetControl()`
- `SolutionSherlockControl` constructor
- `SolutionSherlockControl.UpdateConnection(...)`
- `SolutionSherlockControl.RepositoryName`, `UserName`, `HelpUrl`
- Protected overrides: `OnLoad`, `Dispose`

### Services (public methods)

- `DialogService`: `ShowSaveFileDialog`, `ShowError`, `ShowInfo`
- `FileSystemService`: `WriteAllBytesAsync`, `DirectoryExists`, `FileExists`, `GetFileSize`
- `SolutionCache`: `Refresh`, `LoadSolutionsAsync`, `GetById`, `GetAll`, `GetAllCached`, `LastRefreshedUtc`
- `MetadataResolver`: `InvalidateCache`, `GetEntityDisplayLabel`, `ResolveEntities`, `ResolveAttributes`, `ResolveAttributesByMetadataId`, `ResolveKeys`, `ResolveKeysByMetadataId`, `ResolveRelationships`, `ResolveRelationshipsByMetadataId`, `ResolveHierarchyRelationship`, `ResolveGlobalOptionSets`
- `SearchEngine`: `Search`
- `SolutionExplorerService`: `ListComponents`
- `SolutionExportService`: `ExportSolutionAsync`
- `BrowseSolutionsViewModel`: `LoadSolutionsAsync`, `SetSortColumn`, `NextPage`, `PreviousPage`, `SetSearchText`, `GetCurrentPage`, `ExportSelectedSolutionAsync`, `BuildSuggestedFileName`, `SelectedSolution`, `PageIndex`, `SortColumn`, `SortDescending`, `SearchText`, `LastExportFolder`

### Interfaces

- `IDialogService`, `IFileSystemService`, `ISolutionExportService`, `ISolutionRepository`

### Models

- DTOs: `ComponentTypeInfo`, `ExportResult`, `ExportSolutionOptions`, `ExportStageProgress`, `ProcessCategoryOption`, `RelationshipInfo`, `SearchCriteria`, `SearchProgress`, `SearchResult`, `SearchResultRow`, `SolutionComponentDetail`, `SolutionComponentDisplayRow`, `SolutionComponentRow`, `SolutionInfo`, `SolutionListRow`.
- Enums: `ComponentTypeKind`, `ExportStage`, `SolutionSortColumn`.
- No custom public events.

---

## 12. Threading model

### XrmToolBox `WorkAsync`

- Used for: search, load all solutions, view components.
- Pattern: `BeginAsyncOperation` → `WorkAsyncInfo.Work` → `SearchProgress.Log` → `worker.ReportProgress` → `ProgressChanged` (UI) → `PostWorkCallBack` (UI).

### `Task.Run`

- `SolutionCache.LoadSolutionsAsync` wraps synchronous `Refresh`.
- `SolutionExportService.ExportSolutionAsync` wraps blocking `IOrganizationService.Execute`.
- `FileSystemService.WriteAllBytesAsync` wraps `File.WriteAllBytes`.

### `Progress<T>`

- Export status updates.
- Local `Progress<string>` in `BtnLoadAllSolutions_Click` is created but never wired (dead code within method).

### `CancellationToken`

- Threaded through search, solution load, component load, export preflight, paging checkpoints, metadata resolution loops.
- Cancellation is cooperative and cannot interrupt an already-executing synchronous CRM call.

### `Invoke` / `BeginInvoke` / `BackgroundWorker`

- No direct `Invoke`/`BeginInvoke` calls.
- XrmToolBox supplies the `BackgroundWorker` under `WorkAsync`.

---

## 13. CRM SDK requests and calls

- `RetrieveAllEntitiesRequest` — entity-metadata cache.
- `RetrieveAllOptionSetsRequest` — global option-set cache.
- `RetrieveMetadataChangesRequest` — bulk relationship/key/attribute metadata by MetadataId.
- `RetrieveEntityRequest` — attributes, full entity metadata, relationships, `systemform` metadata.
- `ExportSolutionRequest` — solution ZIP export.
- `RetrieveMultiple` targets: `solution`, `solutioncomponent`, `systemform`, `workflow`, `savedquery`, `savedqueryvisualization`, plus each record-backed table in `ComponentTypeMap`.

---

## 14. Main control state fields

- **Connection-scoped services:** `_solutionCache`, `_metadataResolver`, `_searchEngine`, `_solutionExplorerService`, `_solutionExportService`, `_browseViewModel`.
- **Stable UI services:** `_fileSystemService`, `_dialogService`.
- **Search state:** `_lastResults`, `_rightClickedRowIndex`.
- **Component state:** `_lastComponentDetails`, `_componentsSolutionFriendlyName`, `_collapsedGroupKeys`.
- **Docking state:** `_solutionsPinned`, `_componentsPinned`.
- **Async lifecycle state:** `_operationCancellation`, `_operationStopwatch`.

### Invariants

- `_lastComponentDetails` matches the selected solution.
- Solution selection changes clear component data and disable component export.
- `_collapsedGroupKeys` corresponds to the currently bound grouped rows.
- `_browseViewModel` is recreated on connection changes.
- `_solutionCache` and metadata caches are never shared across connections.

---

## 15. Global mutable state and statics

- `ComponentTypeMap.All` and `SubTypesByMain` — public mutable dictionaries (defensive copies not made).
- `ProcessCategories.All` — public mutable array.
- `SearchProgress.None` — shared no-op progress.
- `UiTheme` static colors and `SectionHeaderFont`.
- `MetadataResolver.CacheLifetime` — static readonly TimeSpan.
- `SolutionSherlockControl.EntitySubComponentTypeOrder`, `EntitiesTopGroupKey` — static readonly.
- No static service instances / CRM clients.

---

## 16. Broad exception catches

- `SolutionExplorerService.ResolveImplicitEntitySubComponents` — broad catch skips a failed entity and preserves its top-level entity row.
- `SolutionExplorerService.ResolveSystemFormComponents` — broad catch on metadata retrieval → fallback `formid`.
- `SolutionSherlockControl.BtnLoadAllSolutions_Click` — outer broad catch.
- `SolutionSherlockControl.BtnExportComponents_Click` — broad catch around CSV creation.
- `SolutionSherlockControl.ExportSelectedSolutionAsync` — final broad catch after specific ones.
- Narrow `Win32Exception` catch for opening search-result solution URLs.

**Implication:** Broad catches preserve best-effort resolution but can hide the precise CRM cause unless the logger records the full exception (details already recorded in log — dialogs show only `.Message`).

---

## 17. Observed unused / dead members (documented; not scheduled for deletion)

- `SolutionComponentRow` — DTO defined but no active call sites.
- `IFileSystemService.FileExists`, `GetFileSize` — interface members without production callers.
- `MetadataResolver.InvalidateCache` — public but no active caller (connection change recreates the resolver instead).
- `UiTheme.Secondary` — palette value with no active usage.
- `BtnLoadAllSolutions_Click`: local `Progress<string>` + `SearchProgress`, and a commented-out direct-await implementation.
- `SolutionComponentRow.RootComponentBehavior` — reserved for a generic export flow not currently wired.

**Policy:** These are documented, not deleted. Removing them without a broader design decision could break API surface used externally.

---

## 18. Model inventory and flow

| Model | Represents | Flow |
|---|---|---|
| `ComponentTypeInfo` | Component mapping/configuration | `ComponentTypeMap` → search/explorer |
| `ExportResult` | Completed ZIP export summary | View model → control |
| `ExportSolutionOptions` | `ExportSolutionRequest` flags | Control → view model → export service |
| `ExportStageProgress` | Export stage + message | Export service → view model → UI |
| `ProcessCategoryOption` | Workflow category item | `ProcessCategories` → search criteria |
| `RelationshipInfo` | Unified relationship projection | Metadata resolver → search/explorer |
| `SearchCriteria` | User search payload | Control → `SearchEngine` |
| `SearchProgress` | Cancellation + logging channel | Control → services |
| `SearchResult` | Component/solution match | `SearchEngine` → control |
| `SearchResultRow` | Search grid projection | Control → `dgvResults` |
| `SolutionComponentDetail` | Resolved component roster row | Explorer → control/CSV |
| `SolutionComponentDisplayRow` | Group/header-aware grid row | Control → components grid |
| `SolutionComponentRow` | Raw component projection | Unused (documented) |
| `SolutionInfo` | Cached solution projection | Cache → view model/search/explorer |
| `SolutionListRow` | Browse grid projection | View model/control → solutions grid |

---

## 19. Known limitations and documented assumptions

- Attribute metadata does not expose `createdon`/`modifiedon` — standalone Field details show `—` for dates by design.
- Standalone Relationship/Key resolution relies on bulk metadata scans; no audit dates available.
- Dashboard/Form distinction relies on `systemform.type == 0`.
- Template primary name columns use `title`, not `name`.
- Entity "include all subcomponents" is inferred from `rootcomponentbehavior == 0`.
- Implicit entity expansion can issue many CRM calls (one full metadata request per entity + table queries).
- `ExportSolutionRequest` returns the entire ZIP synchronously; progress is indeterminate; in-flight requests cannot be cancelled.
- Solution ZIP bytes are held in memory before being written.
- `ComponentTypeMap.All` is manually maintained; some Dataverse types are intentionally omitted.
- Metadata cache TTL is 15 minutes; cache invalidation is not triggered by connection changes because a fresh resolver is instantiated on each connection.
- Browse solution filter/sort/paging is client-side after full list load.
- Component CSV export omits GUID, parent entity, and audit dates by design.

---

## Refactoring risk map

### High risk (behavioral gates — do not silently alter)

- `SolutionSherlockControl.UpdateConnection` — rebuilds all connection-scoped services and resets many UI invariants.
- `SolutionSherlockControl.BeginAsyncOperation` / `EndAsyncOperation` — coordinates cancellation, timing, button state, logging.
- `SolutionSherlockControl.BtnSearch_Click` / `BtnLoadAllSolutions_Click` / `BtnViewComponents_Click` — cross-boundary state machines.
- `SolutionCache.Refresh` — cache commit timing and cancellation invariants.
- `SearchEngine.Search` and lookup methods — component-code mappings, batching, implicit membership, managed filtering.
- `MetadataResolver` — Dataverse metadata request shape and SDK version sensitivity.
- `SolutionExplorerService.ListComponents` — per-component-type CRM peculiarities.
- `SolutionExportService.ExportSolutionAsync` — synchronous SDK behavior + cancellation limits.
- `BrowseSolutionsViewModel.GetCurrentPage` — filtering/sorting/paging state used directly by UI.

### Medium risk

- `BuildGroupedComponentRows` / `ApplyGroupVisibility` — coupled group-key contract.
- `ResolveImplicitEntitySubComponents` — dedupe + root-component semantics.
- `ResolveSystemFormComponents` — metadata fallback behavior.
- `ResolveWorkflowComponents` — category conventions.
- `ExportComponentsToCsv` — CSV format stability.

### Lower risk

- `MetadataExtensions`, `QueryPaging.Chunk`, `CsvEscape`, `SanitizeFileName`, `FormatFileSize`.
- DTO/model classes, `ProcessCategories`, most `UiTheme` styling methods.
- `DialogService`, `FileSystemService` wrappers (provided contracts stay stable).
