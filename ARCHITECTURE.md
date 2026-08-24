# SolutionSherlock — Architecture

## Overview

**SolutionSherlock** is a single-assembly XrmToolBox plugin (a Windows Forms
`UserControl` hosted in-process by `XrmToolBox.exe`) targeting
.NET Framework 4.8.1. It uses the Dataverse SDK (`Microsoft.Xrm.Sdk`) to
inspect solutions and their component roster in a connected Dataverse /
Dynamics 365 environment.

The tool has two tabs:

- **Search Components** — free-text search for a component across every
  solution; result rows link back to their containing solution.
- **Browse Solutions** — paged list of every user-visible solution, per-row
  actions for viewing the full component roster, opening the solution in a
  browser, or exporting the solution as a `.zip`.

---

## Component map

```
┌──────────────────────────────────────────────────────────────────┐
│  SolutionSherlockPlugin  (MEF entry, one class, no work)         │
│  → GetControl() returns SolutionSherlockControl                  │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│  SolutionSherlockControl   (WinForms UserControl, ~1700 LOC)     │
│                                                                  │
│   Owns:                                                          │
│    • two DataGridViews (search results + browse solutions)       │
│    • dgvSolutionComponents (component roster grid)               │
│    • the activity log listbox                                    │
│    • connection status label                                     │
│    • cancellation source + operation stopwatch                   │
│                                                                  │
│   Delegates domain work to:                                      │
│    • SolutionCache  (ISolutionRepository)                        │
│    • MetadataResolver                                            │
│    • SearchEngine                                                │
│    • SolutionExplorerService                                     │
│    • SolutionExportService  (ISolutionExportService)             │
│    • BrowseSolutionsViewModel                                    │
│    • FileSystemService (IFileSystemService)                      │
│    • DialogService (IDialogService)                              │
└──────────────────────────────────────────────────────────────────┘

    │
    │  Search Components tab flow
    ▼
┌──────────────────────────┐         ┌──────────────────────────┐
│  SearchCriteria (DTO)    │──────▶─│  SearchEngine.Search()   │
└──────────────────────────┘         │                          │
                                     │  Dispatches by kind:     │
                                     │   • Entity + subtypes    │
                                     │   • Global Option Sets   │
                                     │   • Data-record kinds    │
                                     │                          │
                                     │  Uses:                   │
                                     │   • MetadataResolver     │
                                     │   • SolutionCache        │
                                     │   • QueryPaging          │
                                     └───────────┬──────────────┘
                                                 ▼
                                     ┌──────────────────────────┐
                                     │  List<SearchResult> →    │
                                     │  SearchResultRow rows    │
                                     │  bound to dgvResults     │
                                     └──────────────────────────┘

    │
    │  Browse Solutions tab flow
    ▼
┌──────────────────────────────────────────────────────────────────┐
│  BrowseSolutionsViewModel                                        │
│                                                                  │
│   State:                                                         │
│    • _allSolutions, PageIndex, SortColumn, SortDescending,       │
│      SearchText, SelectedSolution, LastExportFolder              │
│                                                                  │
│   API:                                                           │
│    • LoadSolutionsAsync   → ISolutionRepository.LoadSolutionsAsync│
│    • GetCurrentPage       (in-memory filter/sort/page)           │
│    • ExportSelectedSolutionAsync                                 │
│      → ISolutionExportService.ExportSolutionAsync                │
│      → IFileSystemService.WriteAllBytesAsync                     │
│    • BuildSuggestedFileName                                      │
└──────────────────────────────────────────────────────────────────┘

    │  View Components action
    ▼
┌──────────────────────────────────────────────────────────────────┐
│  SolutionExplorerService.ListComponents(solution)                │
│                                                                  │
│   1. RetrieveSolutionComponentRows  (solutioncomponent, paged)   │
│   2. Group rows by componenttype code                            │
│   3. Per code, dispatch to a resolver:                           │
│        • code 1  → ResolveEntityComponents                       │
│        • code 9  → ResolveOptionSetComponents                    │
│        • code 60 → ResolveSystemFormComponents (Form/Dashboard)  │
│        • code 29 → ResolveWorkflowComponents (Process / BR)      │
│        • code 2  → ResolveFieldComponents                        │
│        • code 10 → ResolveRelationshipComponents                 │
│        • code 14 → ResolveKeyComponents                          │
│        • others → ResolveDataRecordComponents (generic)          │
│        • unknown → ResolveUnknownComponents (placeholder)        │
│   4. For entities added with rootcomponentbehavior == 0,         │
│      synthesize implicit sub-components via                      │
│      ResolveImplicitEntitySubComponents.                         │
│                                                                  │
│   Uses MetadataResolver's cached lookups for label resolution.   │
└──────────────────────────────────────────────────────────────────┘
```

---

## Layering rules

1. **UI (SolutionSherlockControl)** knows about `WinForms`, XrmToolBox
   `PluginControlBase`, `WorkAsync`, and every service. It never issues a
   Dataverse SDK call directly.
2. **View models (BrowseSolutionsViewModel)** know about model DTOs,
   `ISolutionRepository`, `ISolutionExportService`,
   `IFileSystemService`, and expose async surfaces. They never touch
   WinForms controls.
3. **Services (SolutionCache, MetadataResolver, SearchEngine,
   SolutionExplorerService, SolutionExportService)** know about
   `Microsoft.Xrm.Sdk` and model DTOs. They never reference WinForms or
   XrmToolBox.
4. **Helpers (ComponentTypeMap, QueryPaging, MetadataExtensions,
   UiTheme)** are pure static utilities. `UiTheme` is the only WinForms
   helper.
5. **Models (`Models/*.cs`)** are plain DTOs and enums with no
   dependencies beyond `System` and the SDK.

Circular dependencies: none. Each layer depends only on layers "below"
in the list above.

---

## Data flow — Search Components

1. User picks a main type / optional sub-type / text / filter and clicks
   **Search**.
2. `BtnSearch_Click` builds a `SearchCriteria` and calls
   `BeginAsyncOperation`, disabling every action button and setting up a
   cancellation token.
3. `WorkAsync.Work` runs on a `BackgroundWorker` thread:
   - If the solution cache is cold, `_solutionCache.Refresh` is called.
   - `_searchEngine.Search(criteria, progress)` returns
     `List<SearchResult>`.
4. `WorkAsync.PostWorkCallBack` marshals back to the UI thread:
   - `EndAsyncOperation` restores button state.
   - `RenderResults` projects to `SearchResultRow`, orders by (solution,
     component), and binds `dgvResults`.

---

## Data flow — Browse Solutions → Component roster

1. User clicks **Load All Solutions** →
   `BrowseSolutionsViewModel.LoadSolutionsAsync` invokes
   `SolutionCache.LoadSolutionsAsync` (which internally runs a paged
   `RetrieveMultiple` on `solution`).
2. `RenderSolutionsPage` binds the paged, filtered, sorted view.
3. User picks a solution and clicks **View Components** →
   `SolutionExplorerService.ListComponents`:
   - Retrieves every `solutioncomponent` row for the solution.
   - Groups by `componenttype` and dispatches per type (see the map
     above).
   - Handles the "Include Subcomponents" case by synthesizing the
     implicit rows from full entity metadata + per-entity `systemform`
     / `savedquery` / `savedqueryvisualization` / `workflow` queries.
4. UI binds `BuildGroupedComponentRows(...)` to `dgvSolutionComponents`.
   Group headers render via `DgvSolutionComponents_CellFormatting`.
   Expansion state lives in `_collapsedGroupKeys`; visibility is
   applied by `ApplyGroupVisibility`.

---

## Data flow — Solution ZIP export

1. User double-clicks a solution row or picks **Export Solution** in the
   Actions menu.
2. `ExportSelectedSolutionAsync` uses `DialogService.ShowSaveFileDialog`
   to obtain the destination path.
3. `BrowseSolutionsViewModel.ExportSelectedSolutionAsync`:
   - Validates the destination.
   - Calls `ISolutionExportService.ExportSolutionAsync` — which wraps
     `ExportSolutionRequest` inside `Task.Run` so the UI thread is not
     blocked.
   - Writes the returned bytes via
     `IFileSystemService.WriteAllBytesAsync`.
   - Returns an `ExportResult` (file path + size + elapsed).
4. UI reports success or exception-specific messages
   (`InvalidOperationException`, `FaultException<OrganizationServiceFault>`,
   `TimeoutException`, `IOException`, catch-all).

---

## External integrations

- **XrmToolBox** — MEF host, connection manager, `PluginControlBase`
  `WorkAsync` / `LogInfo` / `LogWarning` / `LogError`,
  `ConnectionDetail.UseOnline` / `EnvironmentId` / `WebApplicationUrl`,
  `SettingsManager` (unused, mentioned as extension point).
- **Microsoft.Xrm.Sdk** — `IOrganizationService`, `Entity`, `Query*`,
  `RetrieveAllEntitiesRequest`, `RetrieveMetadataChangesRequest`,
  `RetrieveEntityRequest`, `RetrieveAllOptionSetsRequest`.
- **Microsoft.Crm.Sdk.Messages** — `ExportSolutionRequest`.
- **Maker Portal** — `https://make.powerapps.com/environments/{envId}/solutions/{solutionId}`.
- **On-Premises classic solution editor** — `{orgUrl}/tools/solution/edit.aspx?id={solutionId}`.

---

## Extension points

1. **New main component type** — add a `ComponentTypeInfo` entry to
   `ComponentTypeMap.All`, then handle it in `SearchEngine.Search()`
   dispatch and `SolutionExplorerService.ListComponents` if it needs a
   custom resolver.
2. **New entity sub-component type** — add to
   `ComponentTypeMap.SubTypesByMain[Entity]` and to
   `SearchEngine.SearchEntityOrSubComponent` dispatch.
3. **Streaming solution export** — replace
   `IFileSystemService.WriteAllBytesAsync` with a streaming variant and
   provide a matching SDK path. The SDK itself does not currently offer
   a streaming `ExportSolutionRequest`.
4. **Persisted saved searches / last-export folder** — hook into
   `XrmToolBox.SettingsManager` and persist a `SearchCriteria` list and
   `LastExportFolder` between sessions.
5. **Unit tests** — the interface surface
   (`ISolutionRepository`, `ISolutionExportService`, `IFileSystemService`,
   `IDialogService`) exists precisely for this purpose. See
   `BrowseSolutionsViewModel` docs.

---

## Threading model

| Concern | Mechanism |
|---|---|
| Search / Load / View Components | XrmToolBox `WorkAsync` (BackgroundWorker) with `SearchProgress` for logging / cancellation |
| Solution ZIP export | Genuine async/await — `Task.Run` wrapping blocking SDK `Execute` |
| File writing | `Task.Run` wrapping `File.WriteAllBytes` (net481 has no async byte-write on `File`) |
| Progress updates back to UI | `IProgress<T>` captured on the UI thread; `WorkAsync` `ReportProgress` for the WorkAsync variant |
| Cancellation | Cooperative `CancellationToken`; only honored *before* the next SDK round-trip — Dataverse SDK synchronous requests cannot be aborted in flight |
| Concurrent operations | Not supported — every command entry point calls `BeginAsyncOperation`, which disables every action button; `PostWorkCallBack` calls `EndAsyncOperation` |

See [CODE_AUDIT_REPORT.md](CODE_AUDIT_REPORT.md#1-high--btnloadallsolutions_click-re-enables-buttons-before-the-async-work-finishes)
for a historical bug in `BtnLoadAllSolutions_Click` where the
disable / re-enable lifecycle was violated, and its fix in this audit pass.

---

## Design decisions

- **No DI container.** XrmToolBox instantiates plugin controls via MEF with
  a parameterless constructor. Composition happens manually in
  `SolutionSherlockControl.UpdateConnection` because every connection-scoped
  service must be replaceable when the user reconnects.
- **Interfaces exist on file-system / dialog / repository / export
  boundaries but not on `MetadataResolver`, `SearchEngine`,
  `SolutionExplorerService`, `SolutionCache`'s concrete methods.** The
  first four are the only ones a unit test would need to mock; the
  metadata / search / explorer types depend on the same
  `IOrganizationService` mocks used at that seam already.
- **`ComponentTypeMap` is code-driven, not data-driven.** Adding a new
  Dataverse component type requires a code change. This is deliberate —
  each new type still needs matching resolver logic, and a runtime
  configuration would give a false sense that the mapping alone is enough.
- **CSV export omits GUID / parent entity / audit dates by design.** The
  intent is a human-readable roster equivalent to what Solution Explorer
  shows; the GUID column is present in the grid for copy/paste but not
  in the CSV, to keep the exported file usable for stakeholder review.
- **Component grouping is UI-only.** The flat `_lastComponentDetails`
  list is the source of truth for CSV export; `BuildGroupedComponentRows`
  only produces the presentation projection.
- **Implicit-subcomponent synthesis is a workaround for Dataverse
  behavior.** When an entity is added with "Include Subcomponents",
  Dataverse writes no per-field/relationship/key
  `solutioncomponent` rows. `ResolveImplicitEntitySubComponents`
  reconstructs those from full entity metadata.
