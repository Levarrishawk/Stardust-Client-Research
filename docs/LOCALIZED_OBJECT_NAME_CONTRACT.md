# Localized object-name resolution contract

The engine-neutral [resolver](../poc/Stardust.Core/Assets/LocalizedObjectNameResolver.cs) converts frozen [current name state](../poc/Stardust.Core/Protocol/CreatureView3NameState.cs) into an immutable `LocalizedObjectNameResult`. It borrows already-owned [STF variant 1 tables](LOCALIZED_STRING_TABLE_CONTRACT.md) through a caller-supplied callback. It performs no archive I/O, caching, locale discovery, background work or callback retention.

```csharp
LocalizedObjectNameResolver.Resolve(names, parsedTableResolver,
    primaryDirectory: "string/en", fallbackDirectory: null, maximumDisplayUnits: 256);
```

The result contains `DisplayText`, `Origin`, `Truncated`, optional `TablePath` and optional U32 `MatchedId`. The directory default is an explicit API default; it does not recover the client's configured locale.

## Selection and outcomes

These are explicit modern presentation policies. Original object-name precedence and UI wording are not claimed recovered.

| Condition | Origin | Text before output limit | Resource calls |
|---|---|---|---|
| Nonempty custom raw UTF-16 units | Custom | Safe display of custom units | None |
| Empty custom, empty table and key | Empty | Empty | None |
| Unsafe/oversized table identifier or oversized key | InvalidStringId | `(invalid name)` | None |
| Primary table exists and exact key matches | PrimaryTable | Safe localized units | Primary once |
| Primary absent, explicit distinct fallback exists and key matches | FallbackTable | Safe localized units | Primary then fallback |
| All permitted table attempts absent | MissingTable | `(missing table)` | At most two |
| Loaded table lacks exact key | MissingKey | `(missing key)` | No fallback after key miss |

Nonempty includes custom NUL/control-only units. An empty localized value is a successful match with an empty display and a retained matched ID. A nonempty valid table with an empty key performs an exact empty-key lookup. Empty table plus nonempty key is invalid. `NameSpacer` is opaque and does not affect resolution.

A callback `null` means the table is absent. Only this outcome permits fallback; arbitrary callback exceptions propagate unchanged. If both directories are equal by ordinal string comparison, only one call occurs. A missing-table result reports the last attempted path; a missing-key result reports the loaded table path. Custom, empty and invalid results have no path or matched ID. A match retains path/ID even when display is truncated or empty.

The [prior runtime investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_LOCALIZATION_STF_RUNTIME.md) confirms failed-primary table-load retry and distinguishes bad table from bad key. This implementation preserves that boundary with an explicit absence callback; it does not implement legacy cache expiry, config, warning or load-error conversion.

## Exact identity and path bounds

Table identifiers contain 1..128 bytes, each ASCII letter, digit, underscore or hyphen. Dots, slashes, backslashes, colon, whitespace, NUL and high bytes reject. Directories contain 1..256 characters, using slash-separated nonempty components of the same allowed characters. Absolute/trailing/doubled separators and traversal components reject. Caller directory errors throw `ArgumentException`, even for custom/empty names; output limits outside 0..4096 throw `ArgumentOutOfRangeException`. Null model/callback throws `ArgumentNullException`.

Paths are exactly `directory + "/" + tableIdentifier + ".stf"`; spelling and case remain unchanged. No path normalization is performed. These are VFS paths handed to the callback, not native filesystem accesses. Key length must be at most 65,536 bytes before resource calls/copy. Key contents are otherwise arbitrary; exact case/high bytes/NUL remain significant. Raw state is never modified or normalized. Frozen baseline names can exceed update caps; the resolver checks raw lengths and borrows immutable spans before copying a bounded lookup key.

## Bounded display

`maximumDisplayUnits` measures **output UTF-16 code units**, including escape spelling. Its default is 256 and hard range is 0..4096. Valid surrogate pairs produce two units as one indivisible token; isolated surrogates become U+FFFD. Controls become literal `\uXXXX`, and backslashes double. Other units are retained, including noncharacters. This is not HTML escaping or a font/glyph policy.

The resolver reads only the prefix needed to fill output, plus a lookahead low-surrogate unit when needed. It never calls the table's unbounded full-display conversion or copies an entire custom name. If the next complete token cannot fit, output stops before that token; escapes and pairs are never split. `Truncated` means raw units remain unconsumed; no ellipsis is added. At limit zero, nonempty input is truncated and empty input is not. Diagnostic ASCII fallback wording is prefix-limited by the same output limit.

## Callback lifecycle

Same-thread recursive `Resolve` is rejected before nested argument validation, including custom-name recursion. A nested attempt marks the active outer frame; even if a callback catches the nested exception, the outer resolution fails with `InvalidOperationException`. `finally` clears the guard after success or any exception. Independent threads are permitted. This is a synchronous thread-local reentry guard, not a cross-thread dependency/cancellation mechanism. No partial result is published; earlier immutable results and borrowed tables remain valid. Callback side effects are not rolled back.

## Verification and next boundary

The [native scenarios](../poc/Stardust.Core.SmokeTests/LocalizedObjectNameScenarios.cs) and [independent Python oracle](../poc/round25-tests/Assets/verify_names.py) verify 89 policy vectors, 14 controlled failure/ownership/baseline-bound checks and 32 independent concurrent calls. One selected key from each of two pinned original tables agrees with independently decoded bytes and display. Detailed original names stay private. See [Round25 evidence and commands](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R25_OBJECT_NAME_RESOLUTION.md).

Next integrate current named replication with an optional presentation consumer and an explicitly supplied owned-table provider. Dynamic locale/config policy, cache ownership, original UI wording and variant 0 remain deferred.
