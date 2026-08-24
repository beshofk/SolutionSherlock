# SolutionSherlock — Code Audit Report

**Method:** Every source file under [src](src) was read end-to-end. Findings
are grouped by severity. Every finding lists concrete evidence (file + method),
the impact, a recommended fix, the risk of applying the fix, and an action.

**Preservation rule:** No public API is removed, renamed, or reshaped by this
audit. Findings that can only be resolved by changing behavior are recorded
under **Requires review** and left unimplemented.

Severities: `Critical` · `High` · `Medium` · `Low` · `Informational`.

Actions: `Safe to fix` · `Requires review` · `Do not change` · `Documentation only`.

---

## Summary

| # | Severity | Area | Finding | Action |
|---|---|---|---|---|
| 1 | **High** | Threading / UI state | `BtnLoadAllSolutions_Click` re-enables action buttons before the async work finishes | Safe to fix |
| 2 | Medium | Maintainability | Dead code inside `BtnLoadAllSolutions_Click` (unused `IProgress`, unused `SearchProgress`, commented-out block) | Safe to fix |
| 3 | Medium | Maintainability | Stray `// after` marker + duplicated `<summary>` block above `ResolveFieldComponents` | Safe to fix |
| 4 | Medium | Reliability | `DgvResults_CellContentClick` treats a bound-cell string as a filesystem path via `File.Exists` | Requires review |
| 5 | Medium | Reliability | `SolutionExplorerService.ResolveSystemFormComponents` and `ResolveImplicitEntitySubComponents` swallow all exceptions | Do not change |
| 6 | Low | Consistency | Two different `Process.Start` code paths for opening URLs | Requires review |
| 7 | Low | API surface | `ComponentTypeMap.All` / `SubTypesByMain`, `ProcessCategories.All`, `UiTheme.*` colors exposed as mutable | Do not change |
| 8 | Low | Concurrency | `SolutionCache.GetById` / `GetAll` lazily call `Refresh()` — could re-enter | Do not change |
| 9 | Low | Documentation | Several public service methods missing XML docs on parameters/exceptions | Safe to fix (docs only) |
| 10 | Low | Performance | Repeated `RetrieveEntityRequest` calls in `SearchEntityFields` / `Keys` / `Relationships` when the main search matches many entities (already flagged in `PERFORMANCE_OPTIMIZATION_REPORT.md` as "Rank 2") | Requires review |
| 11 | Informational | Documentation | Public [README.md](README.md) description of features predates the current code (as acknowledged in-file) | Documentation only |
| 12 | Informational | Testability | No test project exists; regression-safety of large refactors currently rests on manual inspection | Documentation only |
| 13 | Informational | Dead members | `SolutionComponentRow`, `IFileSystemService.FileExists` / `GetFileSize`, `MetadataResolver.InvalidateCache`, `UiTheme.Secondary` are unreferenced by production code | Do not change |
| 14 | Low | Reliability | `SolutionExportService` holds the entire ZIP in a `byte[]` before it is streamed to disk | Requires review |
| 15 | Low | UX | `_operationStopwatch.ElapsedMilliseconds` is read from `catch/finally` in `BtnLoadAllSolutions_Click` even though those blocks fire before the async work finishes | Safe to fix (follows from #1) |

---

## 1. High — `BtnLoadAllSolutions_Click` re-enables buttons before the async work finishes

### Finding

`BtnLoadAllSolutions_Click` wraps `WorkAsync(...)` in a synchronous
`try/catch/finally` and calls `EndAsyncOperation()` in `finally`.
`WorkAsync` dispatches to a `BackgroundWorker` and returns immediately, so
the `finally` block runs the moment the call is queued — not when the work
completes. `EndAsyncOperation()` calls
`SetAllActionButtonsEnabled(true)` and disables both Abort buttons, so
every action button becomes re-enabled while the load is still running in
the background. The `PostWorkCallBack` correctly calls `EndAsyncOperation()`
again later, but by then the user has already had the opportunity to
click other action buttons that also mutate shared state
(`_solutionCache`, `_metadataResolver`).

### Evidence

- File: [src/SolutionSherlockControl.cs](src/SolutionSherlockControl.cs#L1153-L1243)
- Compare with `BtnSearch_Click`
  ([src/SolutionSherlockControl.cs](src/SolutionSherlockControl.cs#L375-L430))
  and `BtnViewComponents_Click`
  ([src/SolutionSherlockControl.cs](src/SolutionSherlockControl.cs#L1346-L1414)),
  which follow the same pattern *without* a surrounding
  `try/catch/finally` — they rely on `PostWorkCallBack` for cleanup, and
  buttons stay disabled until the work is really finished.

### Impact

- Action buttons flicker enabled → disabled after clicking Load All Solutions.
- The user can trigger Search, another Load, or View Components while the
  load is still in flight. Two concurrent `WorkAsync` calls share the same
  `IOrganizationService` and can interleave `_operationCancellation` /
  `_operationStopwatch`.

### Scenario

Click **Load All Solutions**. Within the first ~50 ms the outer `finally`
has already re-enabled every button. Click Search. Both operations run in
parallel; the log/stopwatch reflect only one of them, and the second
click's `BeginAsyncOperation` disposes the token still owned by the first.

### Recommendation

Remove the surrounding `try/catch/finally`. This aligns the handler with
the exact pattern already used by `BtnSearch_Click` and
`BtnViewComponents_Click`. All error / cancellation handling for a
`WorkAsync` operation belongs in `PostWorkCallBack`; the outer `catch`
blocks cannot fire on `args.Error`, and the outer `finally` fires at the
wrong time.

### Risk of fix

Low. `WorkAsync` in XrmToolBox's `PluginControlBase` does not throw
synchronously in production use, so the outer catches are unreachable in
practice. Removing them changes an *incorrect* early-enable to the
*intended* late-enable that the `PostWorkCallBack` already implements.
This is not a public-API change; the method signature and side effects
(post-completion) are preserved.

### Action

**Safe to fix.**

---

## 2. Medium — Dead code inside `BtnLoadAllSolutions_Click`

### Finding

The handler declares two locals that are never used by the active code
path (a shadowed `progress` is created inside the `Work` delegate). It
also contains a commented-out direct-`await` implementation.

### Evidence

- File: [src/SolutionSherlockControl.cs](src/SolutionSherlockControl.cs#L1153-L1180)
- Lines with the unused `IProgress<string> uiProgress` and outer
  `SearchProgress progress` are never referenced after their declaration.
- Commented-out `try/await` block is preserved from an older
  implementation.

### Impact

- Readability. Two `progress` symbols in the same method (one dead, one
  shadowed inside the `Work` lambda) actively mislead the next reader.
- The dead `SearchProgress` construction subscribes to
  `cancellationToken`, which is harmless but is a subtle allocation and
  makes intent unclear.

### Recommendation

Delete the unused locals and the commented-out block. Retain the code
comments that explain why `Progress<T>` is created on the UI thread when
it is actually used.

### Risk of fix

None. No caller depends on the dead code.

### Action

**Safe to fix.**

---

## 3. Medium — Stray `// after` marker and duplicated `<summary>` block

### Finding

- A dangling `// after` comment sits between the end of an XML-doc
  paragraph and the following statement in `ExportSelectedSolutionAsync`.
- The XML-doc comment above
  `SolutionExplorerService.ResolveFieldComponents` opens with two
  `<summary>` tags in a row, which produces malformed IntelliSense.

### Evidence

- [src/SolutionSherlockControl.cs](src/SolutionSherlockControl.cs#L1538-L1541)
- [src/Services/SolutionExplorerService.cs](src/Services/SolutionExplorerService.cs#L491-L510)

### Recommendation

Remove the `// after` marker. Collapse the duplicated `<summary>` opener.

### Risk of fix

None. Comment-only edits, no code path affected.

### Action

**Safe to fix.**

---

## 4. Medium — Fragile URL resolution in `DgvResults_CellContentClick`

### Finding

The search-results grid link click handler uses reflection over multiple
candidate property names (`SolutionUrl`, `Url`, `Link`, `OpenSolutionUrl`)
to find a URL to open, and additionally treats the cell's string value as
a filesystem path via `System.IO.File.Exists(valueStr)` before deciding
whether to use it.

### Evidence

- File: [src/SolutionSherlockControl.cs](src/SolutionSherlockControl.cs#L497-L570)

### Impact

- The `File.Exists` check could open the user's filesystem to a probing
  side-effect if `SolutionUrl` were ever populated with a string derived
  from user input (currently it is not — it is always the output of
  `BuildSolutionUrl`, which only produces `https://…` or an on-prem
  `WebApplicationUrl` derivative). Today this is a code smell, not an
  active vulnerability.
- The reflection fallback masks changes to the bound row shape rather
  than catching them at compile time.

### Scenario

If a future contributor renames `SearchResultRow.SolutionUrl` to
`SearchResultRow.OpenInMakerUrl`, the compile succeeds and the link
column silently stops working, because the reflection block still finds
*something* to try.

### Recommendation

Rely on the strongly typed bound property (`row.DataBoundItem is
SearchResultRow r` → `r.SolutionUrl`) and drop both the reflection loop
and the `File.Exists` check.

### Risk of fix

Medium. The current implementation is deliberately defensive and works
today; simplifying it changes behavior on edge inputs (Tag-set URLs,
File.Exists matches) that no current caller produces but a future one
might. Left in place per the preservation principle.

### Action

**Requires review.**

---

## 5. Medium — Broad `catch` in metadata resolvers

### Finding

`SolutionExplorerService.ResolveSystemFormComponents` and
`ResolveImplicitEntitySubComponents` each contain empty broad `catch`
blocks. The former swallows the failure of a metadata retrieve and falls
back to `formid`; the latter skips the current entity's implicit
sub-components if the full metadata retrieve throws.

### Evidence

- [src/Services/SolutionExplorerService.cs](src/Services/SolutionExplorerService.cs#L406-L432)
- [src/Services/SolutionExplorerService.cs](src/Services/SolutionExplorerService.cs#L192-L207)

### Impact

- Actual server errors (permission failures, transient network faults)
  become silent partial results. The user sees fewer components than the
  solution actually contains, with no log entry explaining why.

### Scenario

An organization revokes read access on `systemform` metadata for a
specific role. A user in that role opens View Components and sees Forms
listed under their fallback `formid` primary key. Everything appears
normal.

### Recommendation

Log the caught exception via the existing `SearchProgress` channel before
falling back, so at minimum the activity log shows *something failed*.
Behavioral fallback stays intact.

### Risk of fix

The comments in-place say this is intentional: the fallback is deliberate
so that a metadata-retrieval hiccup does not break the whole roster.
Adding a log line is behaviorally equivalent but changes the log output.

### Action

**Do not change** (per preservation principle). Recorded in
[EDGE_CASES.md](EDGE_CASES.md) for future work.

---

## 6. Low — Two different `Process.Start` code paths for opening URLs

### Finding

`DgvResults_CellContentClick` uses `new ProcessStartInfo { FileName =
url, UseShellExecute = true }`. `OpenUrl` uses the bare
`Process.Start(url)` overload. On .NET Framework 4.8.1 both invoke shell
execute for `http(s)://` strings, so the observable behavior is
identical today; on .NET 5+ the second form would fail.

### Evidence

- [src/SolutionSherlockControl.cs](src/SolutionSherlockControl.cs#L562-L570) (explicit `ProcessStartInfo`).
- [src/SolutionSherlockControl.cs](src/SolutionSherlockControl.cs#L1717-L1725) (`OpenUrl`).

### Recommendation

Standardize on the `ProcessStartInfo { UseShellExecute = true }` form —
future-proofs against a target-framework upgrade with no observable
change on net481.

### Risk of fix

Low. Behaviorally equivalent on net481; would prevent a future
regression when the plugin retargets.

### Action

**Requires review.**

---

## 7. Low — Public mutable statics

### Finding

`ComponentTypeMap.All`, `ComponentTypeMap.SubTypesByMain`,
`ProcessCategories.All`, and every `UiTheme` colour field are declared as
mutable static instances (`Dictionary<,>`, `T[]`, `Color`) rather than
read-only projections.

### Evidence

- [src/Helpers/ComponentTypeMap.cs](src/Helpers/ComponentTypeMap.cs#L22-L237)
- [src/Helpers/ProcessCategories.cs](src/Helpers/ProcessCategories.cs#L18-L24)
- [src/Helpers/UiTheme.cs](src/Helpers/UiTheme.cs#L14-L34)

### Recommendation

Ideally expose read-only views (`IReadOnlyDictionary`, `IReadOnlyList`)
or seal the type behind static getters. However, all three are part of
the plugin's public surface; changing the exposed type would be a
compile-breaking API change for any consumer that indexes into them
(none exist today, but a future MEF composition could).

### Risk of fix

Compile-breaking API surface change.

### Action

**Do not change.**

---

## 8. Low — `SolutionCache.GetById` / `GetAll` lazy re-entry

### Finding

`SolutionCache.GetById` and `GetAll` both call `Refresh()` if `_byId` is
null. `Refresh()` is not thread-safe. A background `WorkAsync` operation
that has already begun and is waiting on a page could, in principle,
re-enter through a UI thread caller of `GetById` (there are none today
during that window).

### Evidence

- [src/Services/SolutionCache.cs](src/Services/SolutionCache.cs#L102-L133)

### Impact

- None observed today: all invocation paths marshal through XrmToolBox's
  serialized `WorkAsync`.

### Recommendation

If concurrent access becomes possible, add a lock around `Refresh()`.
For today's usage, the invariant that only one operation is in flight at
a time keeps this from ever manifesting.

### Risk of fix

Adding a `lock` would change threading semantics.

### Action

**Do not change.** Recorded in [EDGE_CASES.md](EDGE_CASES.md).

---

## 9. Low — Missing XML docs on a few public members

### Finding

Some public methods on `MetadataResolver`, `SolutionExplorerService`, and
`SearchEngine` lack `<param>` / `<returns>` / `<exception>` tags even
where their `<summary>` is present. Others (e.g. `SolutionInfo`,
`SolutionListRow`) have no summary at all because they are pure
domain-record DTOs.

### Recommendation

Add `<param>` and `<returns>` on public service methods that already have
a `<summary>`. Do **not** add trivial summaries to every DTO — DTO
properties document themselves and adding boilerplate would only add
noise.

### Risk of fix

None. Documentation-only additions.

### Action

**Safe to fix.**

---

## 10. Low — Repeated `RetrieveEntityRequest` calls in entity-scoped search

### Finding

`SearchEntityFields`, `SearchEntityKeys`, `SearchEntityRelationships`,
and `SearchEntityScopedRecords` all issue one `RetrieveEntityRequest`
per matched entity. When the main-type search text matches every entity
in the org, this is O(N) network round trips.

### Evidence

- [src/Services/SearchEngine.cs](src/Services/SearchEngine.cs#L141-L280)
- Already documented as "Rank 2" in
  [PERFORMANCE_OPTIMIZATION_REPORT.md](PERFORMANCE_OPTIMIZATION_REPORT.md).

### Impact

- Only felt when a user runs an entity sub-component search with no main
  search text against a large org. Documented and left as-is by the prior
  performance pass.

### Recommendation

Replace with a single `RetrieveMetadataChangesRequest` sweep bounded by
the matched-entity set — the same pattern used by
`ResolveAttributesByMetadataId`. This is a deliberate future change: it
would alter SDK request shape and label / ordering semantics.

### Risk of fix

High regression surface (label resolution, hierarchy detection,
`RetrieveAsIfPublished`).

### Action

**Requires review** — inherited from the prior performance pass.

---

## 11. Informational — README.md documentation drift

### Finding

Sections 4–7 of [README.md](README.md) reference files and layouts that
predate the current namespace/rename and no longer exist by those names
(e.g. `Models/SearchResult.cs` under a bare `Models/` heading rather than
the current `Models/` folder inside `BeshoyFanous.XrmToolBox.SolutionSherlock`).
Section 7's own disclaimer notes this drift and defers reconciliation.

### Recommendation

Update the "Project layout" table to match the current
[FUNCTIONALITY_INVENTORY.md](FUNCTIONALITY_INVENTORY.md) and remove
sections that describe pre-rename paths.

### Risk of fix

None — documentation only.

### Action

**Documentation only.** Deferred to keep this pass focused on code
audit output.

---

## 12. Informational — No test project

### Finding

The solution contains a single project (`src/SolutionSherlock.csproj`)
and no test project. Interfaces
(`IFileSystemService`, `IDialogService`, `ISolutionExportService`,
`ISolutionRepository`) exist explicitly to enable unit tests, per the
XML docs on those interfaces.

### Recommendation

Introduce an xUnit or MSTest project that exercises at minimum:

- `BrowseSolutionsViewModel` filtering, sorting, paging invariants.
- `BrowseSolutionsViewModel.ExportSelectedSolutionAsync` — validation
  paths, `IProgress` stage sequence.
- `SolutionCache.Refresh` cancellation invariant (cache retains previous
  values on cancel).
- `CsvEscape` / `SanitizeFileName` edge cases.

### Action

**Documentation only** for this pass — recorded here so a follow-up work
item is clear.

---

## 13. Informational — Unreferenced public members

### Finding

Several public members are declared but have no active call sites in
production code:

- `SolutionComponentRow`
- `IFileSystemService.FileExists`, `IFileSystemService.GetFileSize`
- `MetadataResolver.InvalidateCache`
- `UiTheme.Secondary`
- The local `IProgress<string>` and `SearchProgress` in
  `BtnLoadAllSolutions_Click` (removed by Finding #2 above).

### Recommendation

Do **not** delete. `SolutionComponentRow`, `FileExists`, `GetFileSize`,
and `InvalidateCache` sit on the public surface and could be referenced
by consumers or future features (`SolutionComponentRow` in particular is
documented as the intended shape for a generic solution-component
export). Deleting them is a versioning break with no benefit today.

### Action

**Do not change.** Documented here so the intent is visible.

---

## 14. Low — `SolutionExportService` loads the entire ZIP into memory

### Finding

`ExportSolutionAsync` awaits an `ExportSolutionResponse` and returns
`response.ExportSolutionFile` (a `byte[]`), which the view model then
passes to `IFileSystemService.WriteAllBytesAsync`.

### Evidence

- [src/Services/SolutionExportService.cs](src/Services/SolutionExportService.cs#L61-L88)
- [src/ViewModels/BrowseSolutionsViewModel.cs](src/ViewModels/BrowseSolutionsViewModel.cs#L146-L165)

### Impact

- For very large solutions (multiple GB is uncommon but possible in
  ISV-produced packages) the process must hold the entire byte array in
  memory before flushing to disk.

### Recommendation

The SDK offers no streaming variant for `ExportSolutionRequest`; the
response ships the whole ZIP in one shot. There is no path here that
does not first materialize the ZIP in memory. Recorded for awareness
only.

### Action

**Requires review** — architectural, blocked by SDK behaviour.

---

## 15. Low — Stopwatch reads in unreachable catch blocks

### Finding

`BtnLoadAllSolutions_Click`'s outer `catch (OperationCanceledException)`
and `catch (Exception ex)` blocks read
`_operationStopwatch.ElapsedMilliseconds`, which — because those catches
are unreachable (see Finding #1) — never fires.

### Recommendation

Removed as part of the Finding #1 fix.

### Action

**Safe to fix** (follows from #1).

---

## Missing-scenario coverage

Recorded here for reference; details migrate to
[EDGE_CASES.md](EDGE_CASES.md) in the same commit.

| Scenario | Coverage today |
|---|---|
| No connection | Handled: `Service == null` guard at every command entry point. |
| Cancelled mid-page during solution list refresh | Handled: `SolutionCache.Refresh` only commits `_byId` after all pages complete. |
| Cancelled mid-export | Partial: `cancellationToken.ThrowIfCancellationRequested()` runs before `_service.Execute`; once the SDK call is in flight, cancellation cannot interrupt it. |
| Corrupt / malformed `SolutionUrl` | Handled by `Uri.IsWellFormedUriString` + `Win32Exception` catch on the search grid path; `OpenUrl` (Browse tab) does not catch. Recorded in Finding #6. |
| Missing `EnvironmentId` / `WebApplicationUrl` | Handled: `BuildSolutionUrl` returns null; menu shows "Not available". |
| Zero results | Handled: dedicated summary text. |
| Thousands of results | Handled: paginated in Browse Solutions; in Search results, bound as a flat list (see #10). |
| Concurrent operations | Not handled: Finding #1 currently allows a re-entrant click; the fix restores serial execution. |
| Export directory missing | Handled: `ExportSelectedSolutionAsync` throws `InvalidOperationException`, rendered as a readable dialog. |
| CSV export with commas/quotes/CR/LF in field values | Handled by `CsvEscape`. |
| Filename with invalid characters | Handled by `SanitizeFileName`. |
| Repeated `View Components` on the same solution | Handled: state reset before each load. |
| Entity added with "Include Subcomponents" (`rootcomponentbehavior == 0`) | Handled: `ResolveImplicitEntitySubComponents` synthesizes the implicit rows. |
| Unresolved standalone Field / Relationship / Key | Handled: `BuildUnresolvedPlaceholders` keeps them visible with an honest label. |
| Metadata cache TTL expiry mid-search | Cache is per-connection; the 15-minute TTL only affects performance, not correctness. |
| Multiple connections in one XrmToolBox session | Handled: `UpdateConnection` reconstructs the entire connection-scoped service graph. |
