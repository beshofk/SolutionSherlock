# SolutionSherlock — Changelog

All notable changes to this repository. Format is loosely based on
[Keep a Changelog](https://keepachangelog.com/). Only *actually applied*
changes are listed — findings recorded in
[CODE_AUDIT_REPORT.md](CODE_AUDIT_REPORT.md) but *not* implemented are
called out under "Not implemented (recorded only)".

---

## [Unreleased] — Audit pass 2026-08-25

Codebase audit + safe refactoring pass. No public API removed, no public
signature changed, no CRM SDK request shape changed, no CSV column
changed, no UI layout changed, no threading model changed.

### Fixed

- **`BtnLoadAllSolutions_Click` no longer re-enables action buttons
  before the load completes.** The handler previously wrapped
  `WorkAsync(...)` in an outer `try/catch/finally` and called
  `EndAsyncOperation()` in the `finally`. Because `WorkAsync` dispatches
  to a `BackgroundWorker` and returns immediately, the `finally` block
  ran synchronously right after the call was queued — restoring the
  action-button enabled state while the load was still in flight and
  allowing the user to launch a second command against the same
  `IOrganizationService`. The outer wrapper is now removed; the handler
  matches the exact `BeginAsyncOperation → WorkAsync →
  PostWorkCallBack → EndAsyncOperation` pattern already used by
  `BtnSearch_Click` and `BtnViewComponents_Click`, both of which handled
  the lifecycle correctly. Cancellation and error messages are still
  produced from `PostWorkCallBack` (unchanged), including the
  `_operationStopwatch.ElapsedMilliseconds` line that previously lived in
  the outer catch blocks. See
  [CODE_AUDIT_REPORT.md#1-high](CODE_AUDIT_REPORT.md#1-high--btnloadallsolutions_click-re-enables-buttons-before-the-async-work-finishes)
  for the full analysis.
- **`BtnLoadAllSolutions_Click` signature normalized.** The method was
  `async void` but no longer contains any `await` after the fix above;
  the `async` modifier is removed. Behavior on the WinForms message
  loop is identical (a `void` event handler runs synchronously to
  completion — `WorkAsync` still dispatches its own background work).
- **Malformed XML doc comment in
  `SolutionSherlockControl.cs` collapsed.** The block above
  `EntitiesTopGroupKey` had two `<summary>` opening tags (a stale
  block-level comment that used to sit above `BuildGroupedComponentRows`
  before it moved), producing invalid IntelliSense. The single-line
  summary that actually describes `EntitiesTopGroupKey` is retained;
  the descriptive comment for `BuildGroupedComponentRows` remains in
  place directly above that method.
- **Malformed XML doc comment on `MetadataResolver.ResolveKeys`
  restored.** The opening `<summary>` tag and lead sentence were
  missing; the closing `</summary>` and body were orphaned. Restored a
  single valid summary describing why `EntityFilters.All` is used
  (`Keys` alone is not reliably filterable across SDK versions).

### Cleaned up

- **Dead code removed from `BtnLoadAllSolutions_Click`.** A local
  `System.IProgress<string>` and an outer `SearchProgress` were
  declared and immediately shadowed by a second `SearchProgress`
  constructed inside the `Work` delegate — neither of the outer
  instances were ever referenced. A commented-out direct-`await`
  implementation was preserved from a prior refactor. Both removed;
  the surviving `SearchProgress` inside `Work` is the only channel to
  `worker.ReportProgress`.
- **Stray `// after` marker removed** from
  `SolutionSherlockControl.cs` inside `ExportSelectedSolutionAsync`
  (immediately above the `Progress<ExportStageProgress>`
  declaration). No behavior change; the marker had been left from a
  prior edit.
- **Duplicated `<summary>` opening tag removed** from
  `SolutionExplorerService.ResolveFieldComponents`.

### Added — Documentation

- **[FUNCTIONALITY_INVENTORY.md](FUNCTIONALITY_INVENTORY.md)** — full
  baseline inventory produced during Phase 2 of the audit.
- **[CODE_AUDIT_REPORT.md](CODE_AUDIT_REPORT.md)** — Phase 5 findings
  report with severity, evidence, impact, scenario, recommendation, risk,
  and action per finding.
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — component map, layering
  rules, data flows for Search / Browse / Export, external integrations,
  extension points, threading model, and design decisions.
- **[EDGE_CASES.md](EDGE_CASES.md)** — every scenario checked against
  the code (connection lifecycle, search, browse, roster, CSV export, ZIP
  export, threading, configuration, security surface) with current
  handling and recommended future improvements.
- **[CODE_DOCUMENTATION.md](CODE_DOCUMENTATION.md)** — narrative
  documentation of every major class / service / algorithm / integration.
- **[REGRESSION_VALIDATION.md](REGRESSION_VALIDATION.md)** — Phase 11
  regression matrix.
- **This file (`CHANGELOG.md`).**

### Not implemented (recorded only)

The following findings are documented in
[CODE_AUDIT_REPORT.md](CODE_AUDIT_REPORT.md) but were intentionally not
implemented in this pass to preserve existing behavior:

- URL resolution simplification in
  [`DgvResults_CellContentClick`](src/SolutionSherlockControl.cs)
  (finding #4 — the reflection-based fallback is defensive and works
  today; simplifying is out of scope for a "no behavior change" pass).
- Logging (instead of swallowing) inside broad `catch` blocks in
  [`SolutionExplorerService.ResolveSystemFormComponents`](src/Services/SolutionExplorerService.cs)
  and
  [`ResolveImplicitEntitySubComponents`](src/Services/SolutionExplorerService.cs)
  (finding #5).
- Standardizing on `ProcessStartInfo { UseShellExecute = true }` in
  [`OpenUrl`](src/SolutionSherlockControl.cs) (finding #6).
- Bulk `RetrieveMetadataChangesRequest` replacement for the per-entity
  `RetrieveEntityRequest` calls in `SearchEngine.SearchEntityFields` /
  `SearchEntityKeys` / `SearchEntityRelationships` /
  `SearchEntityScopedRecords` (finding #10 — inherited from
  [PERFORMANCE_OPTIMIZATION_REPORT.md](PERFORMANCE_OPTIMIZATION_REPORT.md)
  "Rank 2").
- Streaming solution ZIP export (finding #14 — SDK limitation).
- Reconciling [README.md](README.md) with the current file layout
  (finding #11 — README's Section 7 explicitly defers this).
- Adding a test project (finding #12).
- Removing unreferenced public members
  (`SolutionComponentRow`, `IFileSystemService.FileExists` / `GetFileSize`,
  `MetadataResolver.InvalidateCache`, `UiTheme.Secondary`) (finding
  #13 — these are on the public surface and either documented as
  intentional or reserved for future work).

### Build validation

- `dotnet build SolutionSherlock.sln -v:minimal` after every code
  change — **0 errors**, 1 pre-existing warning (`MSB3277`
  `Microsoft.IdentityModel.Clients.ActiveDirectory` version conflict —
  present before this pass, tracked in
  [.github/copilot-instructions memory](README.md) as harmless).
- No new NuGet or file references introduced.
- No target framework change (`net481`).
- No assembly version change (`1.0.0.0`).

### Files changed

- `src/SolutionSherlockControl.cs` — three narrow edits (see "Fixed"
  and "Cleaned up").
- `src/Services/SolutionExplorerService.cs` — one narrow XML doc-comment
  edit.
- `src/Services/MetadataResolver.cs` — one narrow XML doc-comment edit.
- `.gitignore` — professional Visual Studio / .NET template applied
  earlier in the session.
- New: `FUNCTIONALITY_INVENTORY.md`, `CODE_AUDIT_REPORT.md`,
  `ARCHITECTURE.md`, `EDGE_CASES.md`, `CODE_DOCUMENTATION.md`,
  `REGRESSION_VALIDATION.md`, `CHANGELOG.md`.

---

## Prior to this pass

See [README.md](README.md) sections 7–9 for the sequence of feature
releases that led up to the current codebase (Component Details panel,
grouped component list, "Entity" super-group, standalone Field
resolution, collapse/expand, dock-panel pin/collapse, etc.).

See [PERFORMANCE_OPTIMIZATION_REPORT.md](PERFORMANCE_OPTIMIZATION_REPORT.md)
for the 2026-08-17 performance pass that introduced
`MetadataResolver.GetEntityDisplayLabel` and the O(1) entity-lookup
dictionary.
