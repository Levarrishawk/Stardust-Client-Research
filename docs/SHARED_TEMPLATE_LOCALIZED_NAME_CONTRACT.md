# Shared-template localized name profile

SharedTemplateLocalizedNameProfile (external historical reference) extracts owned raw `objectName` and `detailedDescription` StringId pairs from SHOT parameters. It reuses the existing shared-template decoder, source cache and provenance type. It performs no STF reads, display conversion, object creation or replicated-name writes.

```csharp
var profile = SharedTemplateLocalizedNameProfile.Load(resolve, rootPath, rootSha256,
    limits: new SharedTemplateLocalizedNameLimits(), followParents: true,
    cancellationToken: token);
```

The callback is `Func<string, TreLookupResult>` and the root requires a SHA-256 pin. `ObjectName` and `DetailedDescription` are independently nullable. A resolved pair owns exact narrow `Table`/`Key` bytes through `LocalizedStringKey`, has `ResolvedTemplateField` provenance, and exposes `IsEmptyPair`. Null means unresolved, not an empty StringId. `Visits`, `Sources`, exact `SourceBytes`, `Complete` and `Stop` describe only the bounded profile traversal.

## Byte observations versus policy

The [original-byte checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R27_TEMPLATE_LOCALIZED_NAMES.md) reproduces two pinned catalog chains, five human files and four terminal files sharing seven sources. Across their 18 selected parameter occurrences, four have exactly one zero byte and 14 use this explicit layout:

| Order | Encoding |
|---|---|
| Outer selector | U8 `1` |
| Table selector | U8 `1` |
| Table | Exact bytes through first NUL terminator |
| Key selector | U8 `1` |
| Key | Exact bytes through first NUL terminator |
| End | No remaining bytes |

The one-byte outer-zero encoding and these explicit payload boundaries are **Confirmed observations for the selected files**. Their original StringId getter/inheritance semantics are **Unverified**; no executable VAs or getter claims are added. The implementation deliberately supports only this observed modern profile: outer zero must be exactly one byte and means continue inheritance; outer one requires both nested modes to be one and exact exhaustion. Other outer/nested modes reject. No random/range/list/default encoding is guessed.

Narrow strings retain spaces, high bytes and all non-NUL content without encoding conversion. Terminators are delimiters and are not part of retained identities. An explicit pair with both strings empty is a resolved value that blocks inheritance in this modern policy. A partial-empty pair is also explicit. No key/table normalization or validity-for-STF-path claim is made by the profile.

## Nearest explicit traversal

At each visited template, the accepted structural decoder selects SHOT, and both present name/description payloads are validated. The nearest explicit pair wins independently for each field; absent and outer-zero fields continue. Shadowed name/description payloads are still validated when their source is visited to resolve the other field. Unrelated parameters remain opaque.

Traversal stops immediately when both fields resolve, even if one or both pairs are empty. Such a profile does not read or validate deeper parent resources. If a field remains unresolved, `followParents=true` follows the single parent chain. `followParents=false` stops before the next read. End of chain with unresolved fields returns a partial profile; it does not invent defaults. `Stop` is `Resolved`, `ParentTraversalDisabled` or `EndOfChain`. A missing/masked required parent rejects rather than returning an apparently complete profile.

Cycles are rejected only when traversed; a resolved root with a self-parent is a valid bounded modern profile. This is not a full template inheritance audit. Each visited source reports effective priority, byte size and SHA-256; only the root is required to match a supplied pin. The profile retains metadata and selected owned pairs, not full parameter arrays or callbacks.

## Structure and bounds

The existing [structural contract](OBJECT_TEMPLATE_COLLISION_PROFILE_CONTRACT.md) is unchanged: SCOT/0012 or0013, STOT/0007 or0009 and SHOT/0007,0008,0009 or0010; nested derivations agree, PCNT matches unique parameter chunks, and IFF extents are strict. Unsupported versions/hierarchies/duplicates/truncation/trailing bytes reject.

| Profile limit | Default | Allowed range |
|---|---:|---:|
| MaximumDepth | 16 sources | 1..16 |
| MaximumTemplateBytes | 1,048,576 | 1..1,048,576 |
| MaximumSourceBytes | 1,048,576 | 1..16,777,216 |
| MaximumStringBytes | 1,024 per table/key | 0..65,536 |

Per-source size is checked in the callback wrapper before cache copying; the existing cache enforces aggregate source budget. String scans examine at most MaximumStringBytes + 1 bytes before copying. General IFF limits remain 4,096 nodes/depth16. Counters measure source bytes, not CLR allocation overhead.

Root and nonempty DERV paths must already be normalized safe lowercase relative identities such as `object/name.iff` or a deeper path beneath `object/`, at most1,024 characters. Components use lowercase letters, digits, underscore or hyphen, separated by single slashes; suffix is literal `.iff`. Absolute, traversal, backslash, colon, NUL, spaces, high/uppercase characters, empty components and other dots reject. Raw DERV strings are checked before the existing decoder's legacy normalization. This applies even when the fields resolve without parent reads.

Invalid profile options throw `ArgumentOutOfRangeException`; malformed/unsafe/cycle/depth/identity/missing input throws `InvalidDataException`. Aggregate source-budget failure preserves the cache's `AppearanceGraphBudgetException` (an IOException). Invalid pin syntax preserves the existing catalog argument validation. Arbitrary callback exceptions propagate unchanged. No failure publishes a partial result.

## Lifetime and verification

The existing cache snapshots borrowed callback arrays. Returned selected raw pairs and read-only metadata own their state; source or returned-copy mutation cannot change the profile. Cancellation is polled before/after callbacks, between visits and before return; the bounded synchronous IFF decoder/string scan does not poll internally. Same-thread recursive Load poisons the outer call even if swallowed; finally clears the guard. Independent threads are permitted. Callback effects/concurrent buffer races are outside rollback.

Native scenarios (external historical reference) and the independent oracle (external historical reference) pass 176 fixtures (18 positive/158 rejected), 12 controlled checks and 32 concurrent loads. Exact limits, explicit-empty/partial/unresolved results, unsupported modes, safe paths, cycles, identity/missing/masked sources and all109 truncated prefixes are covered. Both original profiles resolve at one root source; the oracle separately verifies their full chains and two referenced STF key matches. Detailed raw names/text stay private. Preload/name and existing structural-template regressions pass.

Next join authored name references to an explicitly preloaded table provider as a separate optional presentation input. Keep authored metadata separate from authoritative live replicated names. Original getter/fallback policy, unobserved nested modes, locale/cache and STF variant0 remain open.
