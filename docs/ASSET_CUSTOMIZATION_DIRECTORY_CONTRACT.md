# Owned ACST asset customization directory

[AssetCustomizationDirectory](../../SWG-Godot-Client/poc/Stardust.Core/Assets/AssetCustomizationDirectory.cs) implements the confirmed twelve-table `ACST/0000` boundary from the [September12 manager evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_MANAGER_TABLES.md). It uses the existing owned IFF reader: big-endian extents and back-to-back chunks without RIFF padding. Required exact chunk order is `NAME PNOF VNOF DEFV IRNG RTYP UCMP ULST UIDX LLST LIDX CIDX`.

```csharp
var directory = AssetCustomizationDirectory.Load(bytes, limits: null);
var query = directory.Query(filenameCrc, suppressSharedOwner: false,
    normalizedAssetPath: null);
```

`Query` takes an explicit unsigned filename CRC. If provided, the path must already equal `LegacyAssetPath.Normalize(path)` and its existing Stardust checksum must match that CRC; mismatches reject with `ArgumentException`. Unknown CRC returns `Found=false`, null `RootKey` and empty `Variables`. Known results return an immutable ordered variable list and root key. The directory's CRC index is order-independent; it accepts the shipped appended CIDX transition while retaining its exact row/previous/current CRC in `Metadata.CrcOrderingInversions`. No binary-search ordering assumption survives in lookup.

## Schema and admission

| Table | Record |
| --- | --- |
| NAME | NUL ASCII string pool; offsets point to complete string starts |
| PNOF / VNOF | U16LE NAME byte offsets |
| DEFV | signed I32LE authored defaults |
| IRNG | signed I32LE minimum / exclusive maximum |
| RTYP | U16LE: bit15 palette; low15 one-based PNOF or IRNG index |
| UCMP | three U8 one-based VNOF / RTYP / DEFV indices |
| ULST / LLST | U16LE one-based UCMP index / inherited key |
| UIDX / LIDX | U16LE nonzero key, U16LE zero-based list start, U8 count |
| CIDX | U32LE CRC, U16LE known root key |

Before publication, the reader validates every width, name boundary, index, list slice, type target and integer range, including unused table/list records. It rejects duplicate keys within UIDX or LIDX, duplicate CRCs, missing roots and inheritance targets, cycles including unreachable cycles, and bounded graph depth/visits. The same key may legally occur in both direct/link tables. Repeated values, inherited references and names across component definitions retain first-name semantics. Redundant exact NAME pool spellings remain legal at distinct offsets and are reported in `DuplicateNameStrings`; duplicate pool text does not create an ambiguous lookup. PNOF/VNOF/default/type/component aliases are not invented as errors. Empty tables may form an empty directory; queries then explicitly miss.

Names and palette paths retain exact ASCII spelling/case/slashes. Printable bytes 32..126, nonempty terminated names and U16-addressable string starts are explicit bounded modern admission policy. All IRNG minimums must be less than exclusive maxima. Signed authored defaults are preserved even outside an integer range; this is metadata, not a valid runtime current value. Palette defaults remain signed indices with no guessed entry count or range. Invalid data/budgets throw `InvalidDataException`; invalid limit options throw `ArgumentOutOfRangeException`. Atomic publication is modern policy, not asserted legacy diagnostic equivalence.

Default `AssetCustomizationDirectoryLimits`: 2MiB source, 131,072 records per non-NAME table/string count, 1,024 bytes per name, graph depth 128, 1,000,000 validation visits, 65,536 query visits and 4,096 expanded values. Configurable hard ceilings: 16MiB source, 1,000,000 table/string records, 4,096 name bytes, depth 256,4,000,000 validation visits, 1,000,000 query visits and 4,096 expanded values. IFF itself is capped at 14 nodes/depth 2. Every key's graph height is validated independently of traversal/memo order. Query traversal also charges repeated edges and rejects before returning an over-budget result. These are logical bounds, not heap/peak-memory accounting.

Internal tables, roots and immutable typed component templates are owned and private; raw original tables are not publicly exposed. Metadata owns read-only counts and inversion records. Input bytes can clear and archives close before queries. Results remain valid across later failures and concurrent independent queries. There are no retained external streams, callbacks, workers or disposable resources. No full expansion of all 26,080 original roots is needed for load validation or this fixture.

## Expansion and typed values

For each root, direct ULST values precede recursively inherited LLST keys in stored order. The first **exact full name** wins globally, including duplicate component records and converging inheritance. A visited-key set avoids redundant re-expansion without changing first-name precedence. Optional suppression drops any name containing ordinal `/shared_owner/`, matching the established manager option; it does not repair or project namespaces.

Each result is an `AssetCustomizationVariable` with `FullName` and `AuthoredDefault`, discriminated as:

- `AssetPaletteCustomizationVariable`: exact `PalettePath`; palette entry count unresolved.
- `AssetRangedCustomizationVariable`: `MinimumInclusive` and `MaximumExclusive`.

This directory does not load palettes, build a full seed, invoke local setters, recover a player snapshot, create mounted/linked variable objects or apply notifications. Converting these descriptions into a runtime context requires a separate owned palette/resource boundary and explicit current-value policy.

## Bounded verification

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/AssetCustomizationDirectoryScenarios.cs) and [independent Python generator/oracle](../../SWG-Godot-Client/poc/round22-tests/Assets/verify_directory.py) pass 50 synthetic cases: five positive schema/precedence/empty/alias/signed-default variants and 45 malformed cases. Seven budget failures, two path/CRC failures, exact source/output bounds, immutable metadata/results, failure preservation and 32 concurrent synthetic queries pass. Cycle fixtures include an unreachable key. Signed Int32 extrema and an out-of-range authored default remain metadata verbatim.

Readonly configured overlay selects `customization/asset_customization_manager.iff` from priority 53 `stardust_3_01.tre`: 458,340 bytes, SHA-256 `5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20`. Schema aggregates match prior evidence and Python. Human `appearance/hum_m.sat` CRC `D700D576`, root 11821, returns 36 variables /8 suppressed. Appended `appearance/togruta_f.sat` CRC `E201EE84`, root 52048, returns 6 /1 suppressed. All typed rows/defaults/ranges/palette paths/order agree independently. The one CIDX inversion remains row 26079, `FFFD72EB -> E201EE84`. Detailed original results stay ignored/private; no bulk query corpus is published or expanded.

Fresh CIDM/default-albedo regressions pass. **Confidence:** Confirmed original bytes, schema aggregates, targeted expansions and modern fixtures; inherited manager behavior derives from the prior confirmed evidence. Palette entry resolution, full live seed, actual linked/mounted runtime variables and notifications remain separate. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_ACST_DIRECTORY.md) contains exact reproduction and next action.
