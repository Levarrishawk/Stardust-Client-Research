# Ordered customization local-value projection

CustomizationVariableProjection (external historical reference) joins the accepted owned [CIDM dictionary](CUSTOMIZATION_ID_DICTIONARY_CONTRACT.md) and ordered raw [packed codec](PACKED_CUSTOMIZATION_CONTRACT.md) to an explicitly supplied integer range context and complete current seed. It implements only basic ranged/palette **local stored integer** behavior. Mounts, linked variable forwarding, notifications, ACST construction, original player values and other variable classes remain outside this API.

```csharp
public sealed record CustomizationVariableRange(
    int MinimumInclusive, int MaximumExclusive);

CustomizationVariableProjectionResult result = CustomizationVariableProjection.Apply(
    dictionary,
    IReadOnlyDictionary<ShaderCustomizationKey, CustomizationVariableRange> ranges,
    IReadOnlyDictionary<ShaderCustomizationKey, int> currentValues,
    PackedCustomizationSnapshot snapshot);
```

The convenience overload copies ranges into `CustomizationVariableRangeContext`. Callers can construct and reuse that immutable context directly through the second `Apply` overload. It requires 1..4096 unique exact keys, nonnull ranges with minimum less than maximum, and variable names 1..256 printable nonspace ASCII characters. The Boolean namespace is part of each key. Current values must contain exactly the full range key set; every current integer must already lie in its declared range. There are no implicit zeros, defaults or missing-key repairs. Invalid contexts/seeds fail with `InvalidDataException`; null arguments use argument exceptions.

Ranges and seed/result maps are owned read-only snapshots. The supplied range records, CIDM entries and packed records are immutable. `result.Values` contains exactly the complete context, including untouched variables. `result.Diagnostics` is a read-only ordered list, at most 255 entries, one per packed record. Each diagnostic preserves record index, installed ID, exact full name, projected nullable key, attempted integer, nullable previous/result integers and an explicit decision. There is no retained mutable byte array, external stream, callback, worker or disposer in this pure API. Inputs must not be mutated while their snapshot is being copied; independent calls can share immutable contexts safely.

## Ordered local behavior and failures

Each width-one raw value becomes unsigned U8 (0..255). Width two becomes little-endian signed S16 (-32768..32767), promoted to Int32. Width is separate from ID and namespace. All installed IDs are preflighted before processing; an unknown ID fails atomically with `InvalidDataException` identifying its ID and record index. No partial result or diagnostic list is returned, and neither seed nor an earlier published result changes.

Known CIDM entries use exact prefix projection. Unsupported names produce `SkippedUnsupportedNamespace`; known names absent from the declared consumer context produce `SkippedUndeclaredConsumer`. Both preserve the exact full name and leave all stored values unchanged. Original ID 118 `private/alternate_shader_blade` is explicitly skipped without adding a slash or guessing its namespace.

Declared keys apply in **wire order**, retaining duplicates. If `minimum <= attempted < maximumExclusive`, update the working value and report `Applied`. Otherwise retain that key's current working value and report `RetainedOutOfRange`. A valid write followed by an invalid write retains the valid write; later valid duplicates replace earlier ones. Neither compositor fallback nor texture/palette clamping substitutes for local setter behavior. This reproduces the two proven local integer setters; their linked forwarding remains unknown here.

Outer empty and version1/count0 both preserve all prior values and yield no diagnostics. `result.IsOuterEmpty` preserves their distinction. Strict malformed-byte rejection belongs to the accepted codec; atomic codec/projection publication and unknown-ID failure are explicit modern policy. Original sequential setters/tail permissiveness and linked callbacks are not claimed equivalent.

For an ordered raw journal, call `Apply` for every decoded snapshot, feeding the last successful `result.Values` into the next call. A later malformed/unknown snapshot must retain the preceding successful seed according to the caller's explicit failure policy. Collapsing a journal to its final raw bytes can discard earlier valid writes. Current-incarnation checking, journal acknowledgment, generation/reset and stale revision rejection remain caller responsibilities; this pure projector has no world identity.

## Verification and supplied original fixture

Native scenario (external historical reference) and independent Python oracle (external historical reference) agree on 817 independently authored packed wire/value fixtures: every U8 at both synthetic keys, all 256 S16 high bytes, signed/range edges, duplicate ordering, valid/invalid chains, explicit skips, unknown IDs, maximum 255 records and both empty forms. Two unknown-ID snapshots fail atomically; 13 context/seed failures reject. Ownership/read-only maps and diagnostics, context capacity 4096, full Int32 seed edges, exact key length 256 and 32 concurrent independent calls pass. There are no callbacks to reenter, queues to drain or stale/generation state in this component.

The readonly original human reader supplies a fixture-only face/material union from `SkeletalAppearanceCustomization.DescribeControls`: minimum 0, maximum-exclusive `MaximumValue+1`, plus the explicit eight-key full-zero profile. These are **supplied consumer bounds**, not recovered ACST/live variable ranges or defaults. The independent oracle maps all eight profile keys through the pinned 121-row dictionary. Sixteen ordered writes (valid1 then invalid S16 -1 per key) retain 1; a seventeenth record explicitly skips original ID 118. Original closure remains 76 sources/5,940,390 bytes. CIDM resource SHA-256 is `FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2`; archives close and borrowed buffers clear before control/projection verification.

Fresh dictionary/default-albedo/packed-codec regressions pass. **Confidence:** Confirmed tested modern behavior and prior evidence-backed U8/S16/local range rules. Live range contexts, linked/mounted graphs and packet-to-world-to-GPU integration are separate. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_CUSTOMIZATION_PROJECTION.md) records exact commands and handoff.
