# R28 Runtime: bounded local delta-time cursor

Date 2026-10-03. Question: can the established DTST local selector shape be implemented with explicit modern numeric/work bounds and atomic receipts without claiming outer clock or original x87 parity? Stopping condition: engine-neutral cursor, independent bit/reference oracle, negative budget/counter/ownership cases, durable contract and exact scope audit.

## Inputs and identity

Accepted Round28 baseline contains 2,666 files. Manifest SHA-256 `c6945d7365f1c6d2b3585aebeec57d10a29fb9c0fcbd7b0e586a0d58125ab654`; archive `e0eb172e1b2429de49c55857bc14baa0cbe75f5aa825bfcba88762b8483ca011`. Before writing, full archive names/uniqueness/normalized containment, every size/hash and archive identity, archive symlinks and target/all ancestor symlinks/junctions were checked. Only listed bytes were overlaid and all 2,666 postflight hashes matched. Unlisted/private files and HEAD/index remained preserved. Checkout HEAD `65de8c88141071744dc75594be126b3bab3696a5` was not reset to supplied snapshot metadata.

Orientation covered START/latest accepted27, status/ledger/unknown functions and DTST curated rows, accepted [SWTS runtime contract](../../docs/SWITCH_TEXTURE_RUNTIME_CONTRACT.md)/checkpoint, decoder extent and existing numeric probe. No VAs or new original instructions were relied upon; the existing static report is correlation evidence for shape/count/interval. Tools: cached offline .NET/Core dependencies, own no-source NuGet config, task APPDATA, bundled Python with PYTHONUTF8=1. No new dependencies, downloads, original execution, assets/archive loading, live contact or main-checkout writes occurred.

## Observation, interpretation and confidence

Confirmed implementation: [cursor](../../../SWG-Godot-Client/poc/Stardust.Core/Runtime/DeltaTimeTextureCursor.cs) retains float32 residual and uses explicit double addition/repeated subtraction, frame wrap, bounded work, checked counters and prebuilt immutable candidate receipts. All failures preserve the exact prior reference and issued snapshots. Reset preserves monotonic cumulative advances; close preserves final values while invalidating freshness. These ownership/validation/receipt policies are modern, not recovered legacy memory layout.

Confirmed numerical fixture: original documented interval bits3DCCCCCD, initial delta32700000, then3E4CCCCC yield persistent residual equal to interval after one step. Zero delta advances again and clears residual. No clamp was used. Ten0.0125 inputs versus one0.125 input produce distinct float32 residuals (3CCCCCD0/3CCCCCCC); arbitrary partition bit identity is not claimed.

Confirmed bounds: work ceiling1,048,576, conservative constant-time excessive-total rejection and at most budget subtraction steps. Independent bounded reference does not reuse the admission estimate. True near-budget acceptance and rejection are covered, as are minimal interval/maximal delta cases where subtraction cannot make double progress. Original x87 precision/controlword, scheduler units/pause/culling/thread and first-visible-frame remain Unknown.

Compatibility consequence: engine-neutral local frame state is ready for a separately bounded adapter experiment. Frame-count versus variant-list validation belongs to the actor; clocks must use explicitly matching abstract units until additional evidence establishes units. No research/protocol/function coverage denominator changes.

## Validation and reproduction

Build own [runner](../../../SWG-Godot-Client/poc/round28-tests/Runtime/Runtime.csproj) with cached packages and `NuGetAudit=false`, `UseSharedCompilation=false`, `UseAppHost=false`. Native:

```text
dotnet <Runtime.dll> poc/round28-tests/Runtime/ordered-fixtures.json <cursor-receipt.json>
python poc/round28-tests/Runtime/verify.py <cursor-receipt.json>
```

Zero warnings/errors. Native 870 checks /36 cases pass. Independent oracle checks all180 operations, including3 budget/6 argument/5 overflow rejections and four full-budget successes; constructor malformed cases are separate. Positive/negative bits cover signed zero, interval neighbors, count1/8/large modulus, multiple wraps, subnormal/extreme floats, original interval, repeated/partition rounding, reset, exact immutable receipts, foreign owner and closed writes. No broad unchanged world/Godot regressions are warranted for this additive isolated class.

Workspace validator, both new document links, git diff --check, UTF-8/LF/trailing-whitespace and exact scope audits pass. Exactly nine authorized additions; all 2,666 baseline files byte-identical; HEAD/index preserved; private evidence and build products ignored. Focused snapshot diff and git status inspected. Only four allowed additions and five own runner files are authored; no snapshot file changes are permitted. Receipts/logs/build outputs remain ignored. Lead owns canonical session/status reconciliation outside this worker allowlist. No Git stage/commit/push/publication occurred.

## Precise resume

Lead reviews the cursor's numeric policy and wires it into a separately owned static wrapper only with explicit frame-count/list validation and supplied abstract units. Independently verify selected frame identity across boundaries/wrap/reset and resource lifetime. Investigate outer primitive/appearance scheduling before claiming seconds, pause/culling or first-draw behavior. Worker stops after this milestone.
