# SolutionSherlock — Code Documentation

Compact reference for every non-trivial class, service, algorithm, and
integration. Complements the auto-generated XML docs already present in
source; this file gives the reader a place to start when they open the
solution for the first time.

Cross-references use workspace-relative links: click any file name to
open it.

---

## Plugin entry

### [SolutionSherlockPlugin](src/SolutionSherlockPlugin.cs)

MEF entry point (`[Export(typeof(IXrmToolBoxPlugin))]`). The class does
zero work itself; XrmToolBox reads `ExportMetadata` values (name,
description, images, colours) at discovery time and calls `GetControl()`
once when the user opens the tool. `GetControl()` returns a new
[SolutionSherlockControl](src/SolutionSherlockControl.cs).

Image metadata (`SmallImageBase64`, `BigImageBase64`) is embedded
directly as Base64 strings.

---

## Main control

### [SolutionSherlockControl](src/SolutionSherlockControl.cs)

The full WinForms UI, split into two tabs (Search + Browse) sharing a
common activity-log panel and connection status label.

Key state fields:

- **Connection-scoped services**: `_solutionCache`,
  `_metadataResolver`, `_searchEngine`, `_solutionExplorerService`,
  `_solutionExportService`, `_browseViewModel`. Every one is rebuilt
  inside `UpdateConnection`.
- **Stable UI services**: `_fileSystemService`, `_dialogService`. Built
  once in the constructor since neither depends on the SDK connection.
- **Search state**: `_lastResults`, `_rightClickedRowIndex`.
- **Component roster state**: `_lastComponentDetails` (flat, source of
  truth for CSV export), `_componentsSolutionFriendlyName`,
  `_collapsedGroupKeys` (which grouping keys the user has currently
  collapsed).
- **Docking state**: `_solutionsPinned`, `_componentsPinned`.
- **Async lifecycle**: `_operationCancellation` (single
  `CancellationTokenSource` at a time), `_operationStopwatch` (per
  operation timing).

Critical methods:

- `UpdateConnection(...)` — rebuilds every connection-scoped service,
  clears grids/details/filters, resets pin state, expands Solutions
  panel + collapses Components panel.
- `BtnSearch_Click`, `BtnLoadAllSolutions_Click`,
  `BtnViewComponents_Click` — all follow the same
  `BeginAsyncOperation → WorkAsync → PostWorkCallBack → EndAsyncOperation`
  pattern. **`BtnLoadAllSolutions_Click` used to violate that
  contract**; see
  [CHANGELOG.md](CHANGELOG.md#unreleased) for the fix in this pass.
- `BuildGroupedComponentRows(...)` — pure function producing the
  UI-only grouped projection of the flat `_lastComponentDetails` list.
- `ApplyGroupVisibility()` — hides/shows rows based on
  `_collapsedGroupKeys` without rebinding.
- `BuildSolutionUrl(Guid)` — the single place URLs are constructed for
  either Online (Maker Portal) or On-Premises (classic solution editor).
- `ExportSelectedSolutionAsync()` — the only handler that uses genuine
  `async/await`; every other command uses `WorkAsync`. See the
  class-level XML doc for the rationale.
- `BeginAsyncOperation(...)` / `EndAsyncOperation()` — the single
  source of truth for disabling action buttons + enabling Abort during
  a background operation.

---

## Services

### [SolutionCache](src/Services/SolutionCache.cs)

Implements `ISolutionRepository`. Caches every visible non-Default
solution for the connection lifetime.

- **Filter**: `isvisible == true` + `uniquename != "Default"`.
- **Paging**: 500 per page.
- **Refresh atomicity**: `_byId` is only assigned after the last page
  succeeds. Cancellation mid-refresh leaves the previous cache intact.
- **`LoadSolutionsAsync`**: wraps the synchronous `Refresh` in
  `Task.Run` because `IOrganizationService` has no native async surface
  on this SDK version.

### [MetadataResolver](src/Services/MetadataResolver.cs)

Wraps every metadata call and caches the "lightweight" (`EntityFilters =
Entity` only) result for 15 minutes per connection.

- **Entity cache** — `RetrieveAllEntitiesRequest`, `EntityFilters.Entity`.
  Also builds a case-insensitive `Dictionary<string, EntityMetadata>` for
  O(1) `LogicalName → display label` lookups
  (see `GetEntityDisplayLabel`).
- **Relationship / Key caches** — bulk
  `RetrieveMetadataChangesRequest` sweeps of every entity's respective
  collection.
- **Per-entity metadata** — `RetrieveEntityRequest` with
  `RetrieveAsIfPublished = true`. Callers filter results client-side by
  logical/display name.
- **`ResolveAttributesByMetadataId`** — reverse-resolves standalone Field
  solutioncomponent rows to their `AttributeMetadata` + parent entity
  logical name. Uses `RetrieveMetadataChangesRequest` with an OR of
  per-id `Equals` conditions. Batch size 50 (each id widens the OR
  filter unlike a single `In`).
- **`ResolveHierarchyRelationship`** — looks up an entity's
  self-referencing `IsHierarchical` 1:N relationship (source of the
  synthetic "Hierarchy Settings" row).

### [SearchEngine](src/Services/SearchEngine.cs)

Dispatches by criteria kind:

| Main type | Sub type | Strategy |
|---|---|---|
| Entity | *(none)* | `SearchEntitiesOnly` — match entities, then batched `solutioncomponent` lookup for membership. |
| Entity | Attribute | `SearchEntityFields` — per-entity `RetrieveEntityRequest` + solutioncomponent membership + `include-all` merge. |
| Entity | Key | `SearchEntityKeys` (same pattern). |
| Entity | Relationship | `SearchEntityRelationships` (same pattern). |
| Entity | SystemForm / SavedQuery / Chart / BusinessRule | `SearchEntityScopedRecords` — one `QueryExpression` per entity scoped by `objecttypecode` / `returnedtypecode` / `primaryentitytypecode` / `primaryentity`. |
| OptionSet | — | `SearchGlobalOptionSets` — filter cached list, then membership lookup. |
| All other IsDataRecord | — | `SearchDataRecordComponents` — one `QueryExpression` on the table, filtered by name / category / static filter. |

Key building block: **`LookupSolutions(objectIds, componentTypeCode)`** —
one batched query per component type (batch 500) returning
`objectid → [solutionids]`. This is what keeps the search fast regardless
of solution count.

Related: **`LookupEntityIncludeAllSolutions`** — for entity-scoped
sub-components, computes which solutions include the parent entity with
`rootcomponentbehavior == 0` so those implicit memberships surface in
search results (they otherwise have no `solutioncomponent` row of their
own).

### [SolutionExplorerService](src/Services/SolutionExplorerService.cs)

The reverse of `SearchEngine`: given a solution, produce every
component's display detail.

- **`RetrieveSolutionComponentRows`** — page through
  `solutioncomponent` for the solution (columns: `objectid`,
  `componenttype`, `rootcomponentbehavior`; page 5000).
- **Dispatch table** — one resolver per componenttype code (see
  [ARCHITECTURE.md](ARCHITECTURE.md) map). Every resolver returns a
  `List<SolutionComponentDetail>`.
- **`ResolveImplicitEntitySubComponents`** — for entities with
  `rootcomponentbehavior == 0`, synthesises Field / Relationship / Key /
  Form / View / Chart / Business Rule rows from full entity metadata +
  per-entity queries. Dedupes against `alreadyResolvedIds` so an entity
  that also has explicit rows doesn't double-count.
- **`ResolveEntityDisplayName`** — one-liner over
  `MetadataResolver.GetEntityDisplayLabel` (O(1) dictionary lookup).
- **`BuildUnresolvedPlaceholders`** — keeps unresolved standalone
  Fields / Relationships / Keys visible with an honest label instead of
  silently dropping them.

### [SolutionExportService](src/Services/SolutionExportService.cs)

Implements `ISolutionExportService`. One method
(`ExportSolutionAsync`) wraps `ExportSolutionRequest`.

- Fires `ExportStage.Preparing` → `ExportStage.Exporting` → returns.
  `Saving` / `Completed` are emitted by the view model after the bytes
  arrive.
- Uses `Task.Run` to move the blocking `_service.Execute` off the UI
  thread. **Cancellation is only checked before `Execute`.** The SDK
  offers no way to abort a request in flight; the returned `Task` simply
  waits for the server response.

### [DialogService](src/Services/DialogService.cs) and [FileSystemService](src/Services/FileSystemService.cs)

Thin wrappers over WinForms `SaveFileDialog` / `MessageBox` and
`System.IO.File` respectively — introduced so
`BrowseSolutionsViewModel` can be unit-tested with mocks.

---

## View model

### [BrowseSolutionsViewModel](src/ViewModels/BrowseSolutionsViewModel.cs)

Holds all non-UI state for the Browse Solutions tab. Constructor takes
`ISolutionRepository`, `ISolutionExportService`, `IFileSystemService`.

- **`LoadSolutionsAsync`** — delegates to
  `ISolutionRepository.LoadSolutionsAsync`, copies the cache to
  `_allSolutions`, resets `PageIndex = 0`.
- **`SetSearchText` / `SetSortColumn` / `NextPage` / `PreviousPage`** —
  state mutation only; UI calls `GetCurrentPage` afterwards.
- **`GetCurrentPage`** — in-memory filter (contains-match on friendly +
  unique names) → sort (name asc/desc or date asc/desc) → clamp
  `PageIndex` → slice (page size 20). Returns
  `(PageItems, TotalPages, TotalCount)`.
- **`ExportSelectedSolutionAsync`** — validates destination folder,
  invokes the export service, reports `Saving` before file write,
  reports `Completed` after, returns `ExportResult`. Throws
  `InvalidOperationException` for validation failures so the UI's
  catch block can render a readable message.
- **`BuildSuggestedFileName`** — `{UniqueName}_{Version}.zip`,
  invalid filename chars stripped, fallback `"solution"` / `"0.0.0.0"`.

---

## Helpers

### [ComponentTypeMap](src/Helpers/ComponentTypeMap.cs)

Single source of truth mapping `ComponentTypeKind` to display name +
solutioncomponent code + optional table config. Used by both
`SearchEngine` and `SolutionExplorerService`. Documented per-entry with
Microsoft's own componenttype reference.

- **`All`** — the full map.
- **`SubTypesByMain[Entity]`** — the 7 sub-component kinds shown under
  Entity in the search UI (Field / Form / View / Chart / Key /
  Relationship / Business Rule).
- **`MainTypes`** — the filtered dropdown source ordered so Entity
  appears first, everything else alphabetical.

### [QueryPaging](src/Helpers/QueryPaging.cs)

Shared paging + ID batching helpers.

- `RetrieveAllPages(service, query, progress)` — pages until
  `MoreRecords == false`, checks cancellation before every page.
- `Chunk(guidList, batchSize = 500)` — splits into batches suitable for
  a `QueryExpression` `In` clause.

### [MetadataExtensions](src/Helpers/MetadataExtensions.cs)

`GetDisplayLabel(this EntityMetadata / AttributeMetadata /
EntityKeyMetadata)` — null-safe display label with logical-name
fallback.

### [ProcessCategories](src/Helpers/ProcessCategories.cs)

Four `workflow.category` choices offered in the Search UI: Workflow /
Dialog / Action / Business Process Flow. Business Rule is deliberately
not offered here (already reachable as an entity sub-component);
Modern Flow / Desktop Flow are deliberately not offered (a different
mental model — Power Automate).

### [UiTheme](src/Helpers/UiTheme.cs)

Cosmetic-only static palette + styling helpers. Never touches layout
(Dock/Anchor), data sources, or event wiring.

---

## Models (DTOs)

Every file under [src/Models](src/Models) is a plain data-carrier. The
non-obvious ones:

- **[ComponentTypeInfo](src/Models/ComponentTypeInfo.cs)** — per-kind
  configuration entry in `ComponentTypeMap.All`.
- **[SearchCriteria](src/Models/SearchCriteria.cs)** — payload from
  Search UI to `SearchEngine.Search`.
- **[SearchProgress](src/Models/SearchProgress.cs)** — bundles a
  `CancellationToken` and a `Log` callback (`SearchProgress.None` is a
  shared no-op used by services when the caller doesn't care).
- **[SolutionComponentDetail](src/Models/SolutionComponentDetail.cs)** —
  one row of the resolved component roster; feeds the CSV export.
- **[SolutionComponentDisplayRow](src/Models/SolutionComponentDisplayRow.cs)** —
  UI-only row for `dgvSolutionComponents`. Three shapes flow through:
  top-level headers, nested entity headers, and member rows.
- **[SolutionComponentRow](src/Models/SolutionComponentRow.cs)** —
  reserved DTO for a generic component export flow not currently
  wired. Retained deliberately (see [CODE_AUDIT_REPORT.md](CODE_AUDIT_REPORT.md#13-informational--unreferenced-public-members)).
- **[ExportStageProgress](src/Models/ExportStageProgress.cs)** — status
  update surface for solution ZIP export.

---

## Complex algorithms

### Grouped component rendering (`BuildGroupedComponentRows`)

Two-level hierarchy:

```
▼ Entity (N distinct entities)
    ▼ Account (M rows)
        Account (its own entity row, anchors the top)
        ▼ Forms (K)
            Account Main Form
            ...
        ▼ Fields (P)
            ...
    ▼ Contact (Q rows)
        ...
▼ Web Resource (S)  ← non-entity top-level type group
    ...
▼ Process (T)
    ...
```

- Groups with zero members never appear (they're derived from the
  rows that exist, not from a fixed list).
- Ordering: LINQ `GroupBy` preserves first-seen key order, then
  members are sorted alphabetically by `DisplayName`. The entity
  sub-type order is fixed
  (`EntitySubComponentTypeOrder = { Form, View, Chart, Field, Key, 1:N,
  N:1, N:N, Business Rule, Hierarchy Settings, Dashboard }`), with
  anything not on that list appended alphabetically.
- Collapse/expand toggles `_collapsedGroupKeys` and calls
  `ApplyGroupVisibility`; the bound list is never rebuilt.
- `_lastComponentDetails` — the flat, unsorted-by-grid list — stays
  untouched. That's what CSV export reads from, guaranteeing the export
  file never contains a header-marker row.

### Batched solution-membership lookup (`SearchEngine.LookupSolutions`)

Instead of iterating solutions and asking each one "do you contain X?",
the search issues **one batched query per componenttype** with a filter
of the form:

```sql
componenttype = {code} AND objectid IN (id1, id2, ..., id500)
```

Batch size 500 (`QueryPaging.BatchSize`) — the practical ceiling on a
Dataverse `In` clause. The output map is `objectid → distinct
solutionids`, which the search then joins to
`SolutionCache.GetById(solutionid)` to build the final rows.

### Implicit entity subcomponent expansion (`ResolveImplicitEntitySubComponents`)

For entities added with `rootcomponentbehavior == 0`, Dataverse writes
no per-field/relationship/key rows. This method reconstructs them:

1. One `RetrieveEntityRequest` per entity, `EntityFilters =
   Entity | Attributes | Relationships`, `RetrieveAsIfPublished = true`.
2. For each returned `AttributeMetadata` / `OneToManyRelationshipMetadata`
   (both 1:N and N:1) / `ManyToManyRelationshipMetadata` /
   `EntityKeyMetadata`, produce a `SolutionComponentDetail`.
3. Also issue table queries for `systemform` (excluding dashboards),
   `savedquery`, `savedqueryvisualization`, and `workflow` (category 2)
   scoped to the entity.
4. Dedupe against `alreadyResolvedIds` so nothing counted by an
   explicit `solutioncomponent` row gets double-listed.

---

## External integrations

### Maker Portal / classic editor URL construction (`BuildSolutionUrl`)

- **Online**: `https://make.powerapps.com/environments/{ConnectionDetail.EnvironmentId}/solutions/{solutionGuid}` — the Maker Portal uses the solution GUID, not the unique name.
- **On-Premises**: `{ConnectionDetail.WebApplicationUrl.TrimEnd('/')}/tools/solution/edit.aspx?id={{{solutionGuid}}}` — the classic solution editor takes the GUID wrapped in braces.
- Returns null when neither is available; the UI shows "Not available"
  rather than opening a broken link.

### XrmToolBox

- `PluginControlBase.WorkAsync(WorkAsyncInfo)` — background execution
  with automatic UI marshalling on completion / progress.
- `PluginControlBase.LogInfo` / `LogWarning` / `LogError` — writes to
  the XrmToolBox log window (persistent across plugin switches).
- `ConnectionDetail.UseOnline`, `EnvironmentId`, `WebApplicationUrl`,
  `OrganizationFriendlyName`, `OrganizationVersion` — read-only
  metadata about the connection.
- `IGitHubPlugin.RepositoryName` / `UserName` and `IHelpPlugin.HelpUrl`
  — surface the tool's GitHub location + help page in the tool tile.

---

## Where to look first

- **New to the codebase?** Start with
  [SolutionSherlockControl](src/SolutionSherlockControl.cs), the
  `BtnSearch_Click` handler, and follow the calls into
  [SearchEngine](src/Services/SearchEngine.cs).
- **Debugging component roster issues?** Start with
  [SolutionExplorerService.ListComponents](src/Services/SolutionExplorerService.cs)
  and cross-reference the code path against
  [ComponentTypeMap.All](src/Helpers/ComponentTypeMap.cs).
- **Working on export?** Start with
  [BrowseSolutionsViewModel.ExportSelectedSolutionAsync](src/ViewModels/BrowseSolutionsViewModel.cs),
  then
  [SolutionExportService](src/Services/SolutionExportService.cs).
- **Threading questions?** See
  [ARCHITECTURE.md](ARCHITECTURE.md#threading-model) and the class-level
  XML doc on [SolutionSherlockControl](src/SolutionSherlockControl.cs).
