# Explicit localized string-table preload contract

[LocalizedStringTableSet](../../SWG-Godot-Client/poc/Stardust.Core/Assets/LocalizedStringTableSet.cs) preloads a finite explicit list of safe STF VFS paths into immutable owned [variant 1 tables](LOCALIZED_STRING_TABLE_CONTRACT.md). It provides a pure provider for the [object-name resolver](LOCALIZED_OBJECT_NAME_CONTRACT.md). This is a modern preload policy; automatic locale selection, lazy reads, cache expiry and variant 0 remain deferred.

```csharp
var set = LocalizedStringTableSet.Build(paths, resourceResolver,
    limits: new LocalizedStringTableSetLimits(), cancellationToken: token);
// After preload, this method performs no resource reads:
var table = set.Find("string/en/obj_n.stf");
```

`resourceResolver` has type `Func<string, ReadOnlyMemory<byte>?>`. An actual nullable `null` means absence. A present zero-length/default memory value is malformed data, not absence. Callback exceptions propagate unchanged.

## Request preflight and exact identity

Build snapshots the entire request enumeration, validates every path/count and rejects ordinal duplicates **before the first resource callback**. It traverses directly without trusting collection Count or calling ToArray on caller collections. An overlong/infinite sequence is stopped after at most MaximumPaths + 1 yielded elements; a blocking enumerator/callback cannot be interrupted by this synchronous API. Request strings are immutable; later caller-list changes do not change the snapshot. Each accepted path receives exactly one callback unless cancellation or failure stops the build earlier.

Paths have literal lowercase root `string/`, at least one directory component, and a final identifier followed by literal `.stf`. Directory components and identifiers use only ASCII letters, digits, underscore and hyphen. The complete directory including `string/` is at most 256 characters; identifier is 1..128 characters; complete path is at most 389. Empty components, traversal, dots in identifiers, absolute paths, colon, backslash, NUL, high characters and alternate suffix spelling reject. No trimming, case folding, separator conversion or normalization is performed. Case-distinct valid paths are distinct requests. Paths are callback VFS identities, not direct native filesystem access.

Malformed/duplicate/over-budget request input throws `InvalidDataException`; invalid options throw `ArgumentOutOfRangeException` before enumeration/callbacks. Null request/callback arguments throw `ArgumentNullException`. Parser options are validated even for an empty request. An empty request is valid, including with zero set budgets.

## Budgets and counters

| Set limit | Default | Hard ceiling |
|---|---:|---:|
| MaximumPaths | 64 | 1,024 |
| MaximumSourceBytes | 67,108,864 | 268,435,456 |
| MaximumRecords | 262,144 | 1,000,000 |
| MaximumValueUnits | 8,388,608 | 33,554,432 |
| MaximumKeyBytes | 8,388,608 | 33,554,432 |

All set limits may be zero. `TableLimits` supplies the accepted individual parser's limits, defaults and hard ceilings; null uses its defaults. Each present resource must have at least 13 bytes and fit both individual and remaining aggregate source budgets **before copying**. Effective record/aggregate unit/key parser limits are the minimum of individual limits and the corresponding remaining set budget. Per-value/per-key limits remain the supplied individual limits. Thus later tables cannot allocate fields beyond the remaining aggregate allowance. Malformed tables, including variant 0, reject; they are never converted to absence.

Immutable `RequestedPaths` preserves request order, `Tables` is an ordinal path-to-table map, and `MissingPaths` preserves relative request order of null responses. Exact counters `SourceBytes`, `TotalRecords`, `TotalValueUnits`, `TotalKeyBytes` sum successfully parsed present tables only. Missing responses contribute zero; a valid empty table contributes its 13 source bytes. Table-local IDs/keys remain scoped to each table. Counters describe source/decoded quantities, not CLR memory usage, duplicate temporary copies or hash-map overhead.

## Ownership, atomicity and cancellation

Each callback memory is synchronously copied into a private bounded snapshot and parsed into owned immutable records before the next callback. The private snapshot is cleared in finally; caller memory is never cleared, retained or disposed. A callback can reuse its buffer on its next invocation. The caller must not concurrently mutate the memory while it is being copied; such races are not detected. The parser also owns its internal data according to its existing contract.

Every table/missing path/counter remains private until the whole build succeeds. A bad later resource/budget, exception or cancellation publishes no partial set; previously returned sets remain valid. Resource callback side effects are outside rollback. Cancellation is checked before/during request traversal, after traversal/disposal, before/after each callback, before/after parsing, and before final return. The existing bounded synchronous parser/copy does not poll internally, so cancellation is observed at these boundaries.

Same-thread recursive Build poisons the active build even if the callback or enumerator catches the nested exception; outer Build throws `InvalidOperationException`. The guard resets in finally for success/failure/cancellation. Independent threads can build sets. This guard is synchronous/thread-local and does not prevent cross-thread dependency cycles.

`Find(path)` validates the same safe path grammar, throws `ArgumentException` for unsafe/null paths, and returns the canonical table or null for a valid missing/unrequested path. It performs no I/O, allocation of tables, callback or reentry guard. Reads of an earlier immutable set are allowed inside Build callbacks; `MissingPaths` distinguishes requested absence from an unrequested miss.

## Verification and resume boundary

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/LocalizedStringTableSetScenarios.cs) and the [independent byte oracle](../../SWG-Godot-Client/poc/round26-tests/Assets/verify_preload.py) cover 110 fixtures (seven positive, 103 rejected), all five aggregate and six individual one-less budgets, late malformed tables, full prefix truncation, 21 controlled ownership/cancellation/reentry/enumeration checks and 32 concurrent builds. Exactly two pinned original tables agree for all 97 raw records, IDs, CRCs, UTF-16 units, keys, pass order and aggregate counters. Original text remains private. Prior resolver and STF regressions/oracles pass; [checkpoint and commands](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R26_LOCALIZED_TABLE_PRELOAD.md).

Next supply an explicit preloaded set to optional name presentation, outside frame callbacks. Locale/config policy, reload/expiry/shared cache ownership and variant 0 need separate specifications.
