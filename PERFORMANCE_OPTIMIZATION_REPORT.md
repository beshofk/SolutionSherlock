# Performance Optimization Report — SolutionSherlock

Date: 2026-08-17

This report documents a performance analysis and optimization pass over the
SolutionSherlock XrmToolBox plugin. Every Service, Helper, ViewModel,
Model, and the main control (~5,000 lines total) was read before any change
was made, tracing every CRM API call, loop, and UI update. The goal was to
improve performance/responsiveness/resource efficiency while preserving
100% of existing functionality and behavior.

## Performance Analysis

| Rank | Problem | Location | Impact | Cause | Type | Optimization |
|---|---|---|---|---|---|---|
| 1 | **Full entity-metadata scan + list copy per component row** | `src/Services/SolutionExplorerService.cs` `ResolveEntityDisplayName` (called from 6 sites: Form/Workflow/DataRecord/Field/Relationship/Key resolvers) | **High** — O(rows × entities). A solution with 500 components in an org with 2,000 entities = ~1,000,000 comparisons + 500 full-array copies | `ResolveEntities(null)` allocates a fresh `List<EntityMetadata>` and linear-scans it with `FirstOrDefault`, **once per row**, not once per solution | CPU + memory (allocation) | Added an O(1) `Dictionary<string, EntityMetadata>` lookup, built once alongside the existing 15-min entity cache |
| 2 | Per-matched-entity `RetrieveEntityRequest` in `SearchEntityFields`/`Keys`/`Relationships`/`ScopedRecords` | `src/Services/SearchEngine.cs` | **Potential High** (network/API) — an N+1 pattern: blank/broad main search text can match hundreds/thousands of entities, each issuing its own metadata round-trip | Correct per-entity metadata isn't available in a single bulk call without restructuring to `RetrieveMetadataChangesRequest` | Network/API | **Not applied** — flagged only, see risk note below |
| 3 | `ResolveImplicitEntitySubComponents` issuing 1 metadata call + 4 table queries per "include all" entity | `src/Services/SolutionExplorerService.cs` | Medium, bounded by the (usually small) number of entities added to a solution with "all objects" | Inherent to correctly resolving implicit sub-components (this and an earlier bug fix) | Network/API | Not changed — cost scales with a small, legitimate input set |
| 4 | `ApplyGroupVisibility` iterates every grid row on each collapse/expand click | `src/SolutionSherlockControl.cs` | Low-medium, only on explicit user click, proportional to solution size | Flat `DataGridView` model has no native nested-tree virtualization | UI thread | Not changed — correct/necessary for the feature, cost is user-triggered and bounded |
| 5 | `MergeSolutionIds`/`LookupSolutions` use `List.Contains` instead of `HashSet` | `src/Services/SearchEngine.cs` | Negligible — lists here are solution-membership counts (typically single digits) | Simplicity over micro-optimization | CPU | Not changed — would add complexity for no measurable gain |

## Code Changes

### Rank 1 — entity display-name resolution (implemented)

**Problem:** Every component detail row that has a parent entity (Field, Form,
View, Chart, Business Rule, Relationship, Key) called `ResolveEntityDisplayName`,
which called `MetadataResolver.ResolveEntities(null)`. That method does
`all.ToList()` — a **fresh copy of the entire cached entity array** — then
`FirstOrDefault` does a **linear scan**. This ran once *per row*, not once
per solution.

**Before** (`src/Services/MetadataResolver.cs`):
```csharp
private EntityMetadata[] GetAllEntitiesLightweight() { /* ... loads/caches _entityCache ... */ }
```
```csharp
// SolutionExplorerService.cs
private string ResolveEntityDisplayName(string logicalName)
{
    if (string.IsNullOrEmpty(logicalName)) return null;
    return _metadataResolver.ResolveEntities(null)
        .FirstOrDefault(e => string.Equals(e.LogicalName, logicalName, StringComparison.OrdinalIgnoreCase))
        ?.GetDisplayLabel();
}
```

**After:** `MetadataResolver` now builds a case-insensitive
`Dictionary<string, EntityMetadata>` alongside the existing cached array
(same cache-refresh/TTL logic, same `InvalidateCache()` contract) and
exposes a new O(1) accessor:
```csharp
private Dictionary<string, EntityMetadata> _entityByLogicalName;
...
_entityCache = response.EntityMetadata;
_entityCacheLoadedUtc = DateTime.UtcNow;

var byLogicalName = new Dictionary<string, EntityMetadata>(_entityCache.Length, StringComparer.OrdinalIgnoreCase);
foreach (var entity in _entityCache)
    if (!string.IsNullOrEmpty(entity.LogicalName))
        byLogicalName[entity.LogicalName] = entity;
_entityByLogicalName = byLogicalName;
```
```csharp
public string GetEntityDisplayLabel(string logicalName)
{
    if (string.IsNullOrEmpty(logicalName)) return null;
    GetAllEntitiesLightweight(); // ensures _entityByLogicalName is loaded/fresh
    return _entityByLogicalName.TryGetValue(logicalName, out var entity) ? entity.GetDisplayLabel() : null;
}
```
```csharp
// SolutionExplorerService.cs
private string ResolveEntityDisplayName(string logicalName)
{
    if (string.IsNullOrEmpty(logicalName)) return null;
    return _metadataResolver.GetEntityDisplayLabel(logicalName);
}
```

**Why it's faster:** dictionary lookup is O(1) vs. O(n) linear scan, and no
per-call array-to-list copy is made. For "View Components" on a solution
with hundreds of rows, this turns thousands of full-list scans into a
single cached dictionary build plus O(1) lookups.

**Why functionality is unchanged:**
- `ResolveEntities(string)` (public API) is untouched — same signature,
  same behavior for every existing caller.
- `GetEntityDisplayLabel` is a brand-new method, not a rename/removal.
- Same cache lifetime (15 min), same invalidation trigger
  (`InvalidateCache()` now also clears the dictionary).
- Result is identical: entity's `GetDisplayLabel()` (display label,
  falling back to logical name) or `null` if not found — same fallback
  semantics as `FirstOrDefault()?.GetDisplayLabel()`.
- Entity `LogicalName` is unique per Dataverse entity, so the dictionary
  can't produce a different match than the linear scan would.

### Rank 2 — flagged, not applied

`SearchEntityFields`/`SearchEntityKeys`/`SearchEntityRelationships`/
`SearchEntityScopedRecords` each call a per-entity `RetrieveEntityRequest`
(via `MetadataResolver.ResolveAttributes/ResolveKeys/ResolveRelationships`).
For a narrow search (a handful of matched entities) this is fine and
already documented as intentionally bounded. For a **blank
main-search-text** query matching every entity in the org, this becomes
one round-trip per entity. Fixing this would mean rewriting those three
resolvers to use a bulk `RetrieveMetadataChangesRequest` across all matched
entities at once (the pattern already used by
`ResolveAttributesByMetadataId`). This was **not applied** because:
- It changes the SDK call shape for a code path used by every entity-scoped
  search, raising real regression risk (label resolution, ordering,
  `RetrieveAsIfPublished` semantics, hierarchy-relationship detection for
  keys).
- Actual impact depends heavily on org size and how users typically scope
  the "Entity" search text — needs before/after benchmarking against a
  real org, not something to change blindly.

Per the priority rule (100% functionality/behavior preservation over
performance), this was left alone and is surfaced here as a discussion
point rather than a guess.

## Validation

- **Build:** `dotnet build SolutionSherlock.sln -v:minimal` →
  **Succeeded**, 0 errors, only the pre-existing unrelated `MSB3277`
  assembly-version warning (Microsoft.IdentityModel.Clients.ActiveDirectory
  conflict, present before any of these changes).
- **Tests:** No test project exists in this repository — validation here
  is static/regression review, not automated test execution. This is a
  limitation worth noting.
- **Files modified:** `src/Services/MetadataResolver.cs`,
  `src/Services/SolutionExplorerService.cs` (plus an earlier Forms/Views/
  Charts/Business Rules "include all objects" fix from the same session,
  already built/verified).
- **Public APIs:** No signatures changed, no members renamed/removed.
  `GetEntityDisplayLabel` is additive only.
- **Dependencies/framework:** Unchanged — same net481 target, same NuGet
  packages.
- **Behavioral review:** Every existing feature, filter, sort order,
  exported CSV shape, and UI flow was traced and left untouched; only the
  internal lookup mechanism changed.
- **Remaining performance concerns:** the Rank 2 item above (potential N+1
  metadata calls on very broad Entity searches) is the only other
  candidate worth pursuing, and only after profiling against a real
  environment.

## Additional Recommendations

- **Benchmark before further changes:** use XrmToolBox's own connection
  against a large sandbox (2,000+ entities, a solution with 500+
  components) and time "View Components" before/after this change to
  quantify the win; that's also the right environment to decide whether
  Rank 2 is worth the risk.
- **Dataverse API considerations:** `RetrieveMetadataChangesRequest`/
  `RetrieveEntityRequest` cost scales with server-side load, not just
  payload size — batching (already used for `solutioncomponent` lookups
  via `QueryPaging.BatchSize = 500`) is the right lever if Rank 2 is ever
  tackled.
- **Network latency:** Online environments benefit disproportionately from
  reducing round-trip count (Rank 2) versus reducing CPU time (Rank 1) —
  worth weighing which matters more for your typical users' environments
  (Online vs. On-Premises).
- **Large dataset handling:** the "Load All Solutions"/"View Components"
  grids use plain `List<T>` `DataSource` binding, which is fine at
  hundreds-to-low-thousands of rows; if orgs with tens of thousands of
  solution components become a real scenario, consider virtualization,
  but that's a UX/architecture change beyond this task's scope.
- **Logging:** progress logging is already coarse-grained (per-entity/
  per-batch, not per-row) — no change needed there.
- **Profiling approach going forward:** wrap `MetadataResolver`/
  `SolutionExplorerService` calls with `Stopwatch` (the codebase already
  does this in several places) and compare `ElapsedMilliseconds` in the
  log panel before/after any future change — the existing
  `SearchProgress.Log` timing messages are a convenient, already-wired
  mechanism for this.
