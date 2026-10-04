# Owned raw fractal family set

Date: 2026-10-04. `TerrainFractalFamilySet.Decode` is an engine-neutral, immutable snapshot of one MGRP/0000 family group. It preserves signed IDs, MFAM occurrence order, raw terminated name bytes and each [frozen MFRC/0001 input](TERRAIN_FRACTAL_RAW_INPUT_CONTRACT.md). It performs no source I/O, callbacks, original pointer retention or sampler evaluation. [The checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R50_FRACTAL_FAMILY_SET.md) records validation and reproducible inputs.

## Exact representation

The accepted root is one FORM MGRP containing exactly one FORM 0000. Its children are FORM MFAM records, each containing exactly DATA followed by FORM MFRC. IFF sizes are big-endian unsigned 32-bit extents and advance without padding, as in the existing `IffDocument` helper. DATA starts with a little-endian signed 32-bit ID, followed by name bytes ending in exactly one final NUL. No character encoding is inferred. Empty names retain their one terminator byte; high-byte synthetic names are accepted as raw bytes. Each MFRC is exactly the frozen 84-byte FORM MFRC/FORM 0001/DATA envelope, with thirteen little-endian words.

`Entries` is a read-only occurrence-order list. An entry exposes `Id`, group-relative FORM `Offset`, total MFAM `EncodedBytes`, terminated `NameBytes`, and immutable `Raw`. `CopyNameBytes`, `Raw.CopyEncodedBytes` and `Raw.CopyDataWordBits` return independent arrays. The set exposes the SHA-256 and byte length of its input, aggregate name/embedded/snapshot bytes and logical work charge. It retains no input span or original name pointer.

`Find(int id)` returns the retained entry or `null` for explicit missing. All signed IDs, including `-1` and both extrema, are accepted structurally. The [original consumer sentinel collision](TERRAIN_FRACTAL_FAMILY_LOOKUP_CONTRACT.md) bypasses lookup for a fresh desired `-1`; this snapshot does not emulate that cache or the original missing-pointer arithmetic. Original map reload/quiescence and same-key pointer validity remain Unknown.

## Observation and modern policy

Confirmed original observations are signed IDs, terminated name storage, MFAM DATA followed by MFRC, MGRP/0000 loading, and the selected installed occurrence below. This decoder deliberately requires the exact bounded grammar: duplicate IDs, extra chunks, trailing DATA fields, missing terminators, malformed child extents, alternative versions and trailing group bytes reject atomically. Duplicate rejection and exact grammar enforcement are explicit modern policies; the original duplicate allocation/failure and malformed-input behavior are not claimed to match.

Raw acceptance remains separate from operational eligibility. Arbitrary flags, modes, signed seed/octave words, infinities and NaN payloads survive the frozen raw decoder unchanged. A caller must separately select the explicit controls, curve/mode support, cache policy and budgets required by the [conditional sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md).

## Budgets, ownership and cancellation

| Limit | Default | Hard maximum |
| --- | ---: | ---: |
| Input bytes | 1,048,576 | 16,777,216 |
| Entries | 4,096 | 65,536 |
| Bytes per terminated name | 4,096 | 65,536 |
| Aggregate terminated names | 1,048,576 | 16,777,216 |
| Embedded encoded bytes | 344,064 | 5,505,024 |
| Logical owned snapshot bytes | 2,097,152 | 33,554,432 |
| Logical work charge | 2,097,152 | 33,554,432 |

Zero is allowed for every limit except the per-name limit, whose minimum is one. Invalid policies reject before parsing. Snapshot charge is terminated-name bytes plus 136 per entry: the frozen decoder's 84-byte encoded snapshot and 52-byte word array. Embedded charge is 84 per entry. Work charge is input bytes plus embedded bytes; this is deterministic logical accounting, not a measured instruction count. Object, dictionary, list and record overhead is excluded from snapshot charge; hard input and entry bounds constrain those allocations.

A complete structural pass checks every extent, terminator, embedded envelope, count and aggregate cap before any name or raw snapshot copying or raw decoder invocation. A second pass rejects duplicate IDs before publication allocations. SHA computation and owned construction follow; the frozen raw decoder revalidates its own input. The fixed extent scan uses the existing IFF byte-order/no-padding rules without invoking the tree parser, which copies its whole input before tree validation. No whole-group owned copy is retained here.

Cancellation is checked before parsing, in each entry pass and immediately before final publication. Rejections return no partial set and leave any earlier result unchanged. Callers must keep the borrowed input stable during the synchronous call; no concurrent mutation or original thread-safety guarantee is inferred. After return, mutating source buffers and exported arrays cannot change the set.

## Selected occurrence and reproduction

One effective priority-42 Tatooine occurrence is tested: MGRP at TGEN offset 6602, 5,633 bytes, SHA-256 `A221167452D8D67719BD72A3DB30CC10B7B652881B935D3571B9D55328B444A1`. It contains 43 entries, 965 terminated-name bytes, 3,612 embedded bytes, 6,813 logical snapshot bytes and 9,245 work units. This extends inspection within the same selected group, without a second terrain survey. Original names and raw words remain dynamically generated private evidence; only independently authored schemas and aggregates are public.

[Invoke-FamilySet.ps1](../poc/round50-tests/Assets/Invoke-FamilySet.ps1) requires explicit executable/static-packages/installed-assets/configuration/cached-NuGet/Python inputs. It SHA-gates the original baseline through the frozen static verifier, independently parses the selected chunk and fields, builds the actual Core offline, checks synthetic/native outputs, and invokes frozen sampler/raw/U64/default regressions. Output requires a fresh ignored private directory with lexical reparse-ancestor checks before creation.

Next: construct a selected owned sampler from `Find` under named operational profiles, cache and limits. This set does not establish original map quiescence, generator reload behavior or generation-request semantics.
