# SolutionSherlock — Edge Cases

Every scenario worth thinking about was cross-checked against the code
during the audit. This file records **what the code does today**, not
what it should do — future improvements are called out under
"Recommended future improvements".

Legend:

- ✅ Handled
- ⚠️ Partial / documented limitation
- ❌ Not handled today

---

## Connection lifecycle

| Scenario | Behavior today | Status |
|---|---|---|
| No connection when a command runs | Every action's entry point (`BtnSearch_Click`, `BtnLoadAllSolutions_Click`, `BtnViewComponents_Click`, `ExportSelectedSolutionAsync`) checks `Service == null` and shows an informational dialog. | ✅ |
| `ConnectionDetail == null` on `UpdateConnection` | Handled with a null-conditional access in the status label. | ✅ |
| Connection change mid-load | `UpdateConnection` recreates every connection-scoped service, so in-flight background work on the previous service continues but its results are ignored by the next render (the view model was replaced). This can leak logs from the previous operation. | ⚠️ |
| Environment not on `*.dynamics.com` | `ConnectionDetail.UseOnline == false` selects the on-prem URL path (`{orgUrl}/tools/solution/edit.aspx?id={id}`). | ✅ |
| Online connection without `EnvironmentId` | `BuildSolutionUrl` returns null; UI shows "Not available" instead of a broken link. | ✅ |
| On-prem connection without `WebApplicationUrl` | Same — `BuildSolutionUrl` returns null. | ✅ |

---

## Search

| Scenario | Behavior today | Status |
|---|---|---|
| Blank main + sub + no process category | Rejected with a "Refine your search" dialog. | ✅ |
| Blank main text with an entity sub-type | Runs an unbounded search across every entity. The user is warned in the rejection dialog only if *everything* is blank. | ⚠️ |
| Managed only + Unmanaged only both checked | Both filters run in `BuildResultRow`, so every row is filtered out — 0 results. UI shows "no matching components". | ⚠️ Documented, not enforced. |
| Zero matched entities for an entity sub-search | Returns empty list, UI shows "no matching components". | ✅ |
| Metadata sweep finds thousands of entities | The metadata call is cached (15-minute TTL); each subsequent search hits the cache. Per-entity `RetrieveEntityRequest` calls are O(matched entities). See CODE_AUDIT_REPORT.md finding #10. | ⚠️ |
| `MergeSolutionIds` with `List.Contains` on large solution counts | Solution counts per component are typically small (single digits); documented but not optimized. | ⚠️ |
| Search cancelled mid-flight | `SearchProgress.ThrowIfCancelled()` is checked before every page/round-trip; SDK calls already in flight cannot be aborted. | ⚠️ |
| Solution URL cell click when column has no link | `DgvResults_CellContentClick` falls through to a "Not available" message. | ✅ |
| Solution URL cell click when `Process.Start` throws `Win32Exception` | Caught and shown as an error dialog. | ✅ |

---

## Browse Solutions

| Scenario | Behavior today | Status |
|---|---|---|
| Load All Solutions clicked twice quickly | Historically re-enabled buttons before completion; **fixed** in this audit pass to keep buttons disabled until the WorkAsync PostWorkCallBack runs. | ✅ Fixed |
| Filter matches zero solutions | Page indicator shows "No solutions match '{term}'."; grid is empty; component grid clears. | ✅ |
| Page index out of range (e.g. after filter change) | `GetCurrentPage` clamps to `[0, totalPages - 1]`. | ✅ |
| Solution list refresh cancelled mid-page | `SolutionCache.Refresh` only commits `_byId` after all pages complete; the previous cache stays intact. | ✅ |
| Solution deleted between page load and View Components | `_solutionCache.GetById(id)` may return null; `BtnViewComponents_Click` guards with `if (solutionInfo == null) return;` (silently). | ⚠️ Silent — no dialog. |
| Publisher name null | `SolutionInfo.PublisherName = publisherRef?.Name` — null tolerated by the grid renderer. | ✅ |
| Column header click on a non-sortable column | `DgvSolutions_ColumnHeaderMouseClick` `return`s without side effect. | ✅ |
| Change page while a load is running | `Next/PreviousPage` update `PageIndex` on the view model; a subsequent `RenderSolutionsPage` uses the newest index. `_lastComponentDetails` is cleared per render, matching the invariant that the component export can never emit stale data. | ✅ |

---

## Component roster

| Scenario | Behavior today | Status |
|---|---|---|
| Solution with 0 components | `ListComponents` returns an empty list; UI shows "0 component(s) in '{name}'"; export button stays disabled. | ✅ |
| Component with unknown `componenttype` code | `ResolveUnknownComponents` creates a placeholder row (`Type {code}` + numeric ID). | ✅ |
| Solutioncomponent row missing `componenttype` | Coerced to `-1` and filtered out (never displayed). | ✅ |
| Systemform row without `type` column | Treated as Form (only `type == 0` becomes Dashboard). | ✅ |
| Entity with `rootcomponentbehavior == 0` but no additional metadata | Entity's own row is shown; broad catch inside `ResolveImplicitEntitySubComponents` skips the entity's implicit sub-components on metadata failure. | ⚠️ Silent — see CODE_AUDIT_REPORT.md finding #5. |
| Standalone Field / Relationship / Key that can't be resolved | `BuildUnresolvedPlaceholders` still lists it under an honest label with the raw GUID as the "Name". | ✅ |
| Attribute metadata has no `createdon` / `modifiedon` | UI shows `—` — data-availability limit of the platform, documented. | ⚠️ Data-availability limit. |
| Component roster contains only entity-scoped rows | Only the "Entity" super-group is shown; no top-level component-type siblings. | ✅ |
| Component roster contains only non-entity rows | No "Entity" super-group is rendered; top-level component-type groups only. | ✅ |
| User clicks a group header row | Header selection is cleared; details panel shows placeholders. | ✅ |
| User collapses a group and reloads the roster | Collapse state is cleared on each new load (`_collapsedGroupKeys.Clear()`); every group starts expanded. | ✅ |

---

## Component CSV export

| Scenario | Behavior today | Status |
|---|---|---|
| Empty roster | Export button disabled; if called anyway, a "Nothing to export" dialog is shown. | ✅ |
| Values contain commas / quotes / CR / LF | `CsvEscape` quotes and doubles internal quotes per RFC 4180. | ✅ |
| Solution friendly name contains invalid filename characters | `SanitizeFileName` strips them; empty result falls back to `"solution"`. | ✅ |
| Path chosen is read-only / disk full / access denied | Broad `catch (Exception)` shows the message via a dialog and logs the full exception. | ✅ |
| Export completes | Confirmation dialog + activity log line. | ✅ |

---

## Solution ZIP export

| Scenario | Behavior today | Status |
|---|---|---|
| No solution selected | Handled in the view model — `InvalidOperationException` → readable dialog. | ✅ |
| Destination path is empty / user cancels dialog | Handled — method returns early. | ✅ |
| Destination folder does not exist | `InvalidOperationException` → readable dialog. | ✅ |
| Server returns `FaultException<OrganizationServiceFault>` | Specific catch → error dialog + full exception logged. | ✅ |
| Server times out | Specific `TimeoutException` catch → dedicated message. | ✅ |
| Writing the file fails | Specific `IOException` catch → dedicated message. | ✅ |
| Cancel mid-export | Cancellation is only honored *before* the SDK call is issued. Once `Execute` has been sent, cancellation is not observable. Documented on `ISolutionExportService`. | ⚠️ Platform limitation. |
| Solution larger than free memory | The entire ZIP is materialized as `byte[]` before being written. No streaming path exists in the SDK. | ⚠️ Platform limitation. |
| `chkExportManaged` unchecked | `Managed = false` → unmanaged ZIP. | ✅ |

---

## Threading

| Scenario | Behavior today | Status |
|---|---|---|
| Two commands issued simultaneously | Prevented via `SetAllActionButtonsEnabled(false)` in `BeginAsyncOperation`, restored only in `PostWorkCallBack`. | ✅ Fixed in this pass. |
| Cross-thread UI access from a background thread | Never done directly; `WorkAsync.ReportProgress` and `Progress<T>` marshal back to the UI thread. | ✅ |
| Cancellation issued after the operation completes | `BtnAbort_Click` checks `_operationCancellation.IsCancellationRequested` and returns; PostWorkCallBack has already disabled the button. | ✅ |
| Cancellation issued twice | Second call is a no-op (`IsCancellationRequested` short-circuit). | ✅ |

---

## Configuration

| Scenario | Behavior today | Status |
|---|---|---|
| `App.config` missing | Not required — nothing in production code reads the config file. | ✅ |
| `SettingsManager` unavailable | Not used today. Recorded as an extension point in [ARCHITECTURE.md](ARCHITECTURE.md). | ⚠️ |
| Assembly version bumped without publishing | Handled by XrmToolBox's Tool Library update detection; documented in `Properties/AssemblyInfo.cs`. | ✅ |

---

## Security surface

| Scenario | Behavior today | Status |
|---|---|---|
| Malicious `SolutionUrl` from bound data | `BuildSolutionUrl` only produces `https://make.powerapps.com/…` or `{WebApplicationUrl}/tools/solution/edit.aspx?id={id}` — both constructed from `ConnectionDetail`, not from user text. Search grid click handler uses `Uri.IsWellFormedUriString` + `Process.Start(ProcessStartInfo)`. `OpenUrl` uses `Process.Start(url)` bare (works on net481 via shell execute; would fail on .NET 5+). | ⚠️ Consistency — see CODE_AUDIT_REPORT.md finding #6. |
| Malicious CSV cell value | `CsvEscape` quotes fields containing special chars, but does not defend against CSV-injection formulas (leading `=`, `+`, `-`, `@`). This is a stakeholder-review CSV, not opened as a spreadsheet, but worth noting. | ⚠️ |
| Filename with `..` traversal | `SanitizeFileName` strips only characters from `Path.GetInvalidFileNameChars()`; `..` is legal in a filename per Windows rules but `SaveFileDialog` prevents directory traversal at the OS level. | ✅ |
| Unauthorised metadata access | Broad catches in `SolutionExplorerService.ResolveSystemFormComponents` and `ResolveImplicitEntitySubComponents` swallow permission failures — see finding #5. | ⚠️ |

---

## Recommended future improvements

The following are edge cases where the current handling is safe but not
optimal. Each is deliberately **not** implemented in this pass because it
would either change observable behavior or require a design conversation.

1. **CSV injection guard.** Prefix any exported cell value that starts
   with `=`, `+`, `-`, or `@` with a single quote to defuse Excel/Sheets
   formula parsing. Would change file content — needs stakeholder buy-in.
2. **Broad `catch` visibility.** Convert the silent broad catches in
   `SolutionExplorerService.ResolveSystemFormComponents` and
   `ResolveImplicitEntitySubComponents` into `progress.Log($"...failed:
   {ex.Message}")` before falling back. Would change log output.
3. **Concurrent operations invariant assertion.** `BeginAsyncOperation`
   could throw if called while `_operationCancellation` is already
   non-null — currently it disposes the previous token. Would surface
   accidental re-entry loudly.
4. **`Process.Start` consistency.** Standardise on
   `ProcessStartInfo { UseShellExecute = true }` in both
   `DgvResults_CellContentClick` and `OpenUrl` so a future net-framework
   retarget cannot silently break URL opening.
5. **Streaming ZIP writes.** Requires an SDK path that does not exist
   today, or a lower-level HTTP path that would bypass
   `IOrganizationService`. Architectural change.
6. **Cache invalidation surface.** `MetadataResolver.InvalidateCache` is
   defined but never called; each `UpdateConnection` allocates a fresh
   resolver instead. Calling `InvalidateCache` when
   `RetrieveEntityRequest` throws a "metadata cache stale" fault would
   allow one retry without dropping the whole session.
7. **Missing solution silent guard.** In `BtnViewComponents_Click`,
   `_solutionCache.GetById` returning null returns silently; a "Solution
   no longer available" dialog would be clearer.
