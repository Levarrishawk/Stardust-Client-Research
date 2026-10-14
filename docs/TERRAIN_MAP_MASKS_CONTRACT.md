# Terrain map mask inputs

[TerrainMapMasks](../../SWG-Godot-Client/poc/Stardust.Core/Assets/TerrainMapMasks.cs) is an owned engine-neutral decoder for the conceptual MAPS object. It consumes its **direct version FORM**, not a PTAT/TGEN document or literal `FORM MAPS` wrapper.

```csharp
TerrainMapMasks Decode(ReadOnlySpan<byte> versionForm, TerrainMapMaskLimits? limits = null);
byte[] CopyMask(TerrainMapMaskKind kind);
byte[] CopyDataRecord();
bool GetBit(TerrainMapMaskKind kind, int x, int y);
bool AnyBit(TerrainMapMaskKind kind, int x0, int y0, int x1, int y1, int maximumCells = 65536);
```

## Observed framing and schema

The [prior MAPS checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_PTAT_WMAP_SMAP.md) supplies field/bit evidence. Its literal `FORM MAPS` diagram is corrected by the effective Tatooine bytes: PTAT/0014 contains TGEN and then a direct `FORM 0001` map object. MAPS remains the conceptual object name. This decoder deliberately rejects a literal MAPS wrapper without evidence. Historical/global reconciliation is lead-owned.

| Accepted direct form | Required children |
|---|---|
| `FORM 0000` | None; empty optional maps |
| `FORM 0001` | Exactly `DATA`, `WMAP`, `SMAP`, in that order |

IFF extents are unsigned32 big-endian; records advance without RIFF padding. DATA is exactly16 bytes: little-endian F32 `worldSize`, F32 `metersPerBit`, U32 `bytesPerRow`, U32 `bitDimension`. Each mask is exactly checked `bytesPerRow * bitDimension` bytes. Square grid axes have `bitDimension` entries; byte index is `y * bytesPerRow + (x >> 3)` and bit mask `1 << (x & 7)`. Padded rows and unused high bits are preserved losslessly. Width is not inferred from encoded row length.

Version0000 exposes `IsEmpty=true`, zero scalar sentinels and empty masks/Data-copy; it contains no DATA words. Version0001 retains raw float bit patterns and U32 values through get-only properties, with typed float getters. No metadata ratio constraint is invented: `worldSize / metersPerBit` need not equal dimension for parser eligibility. Neither float metadata nor row padding changes raw grid query coordinates.

## Bounded modern policy

| Limit | Default | Allowed range |
|---|---:|---:|
| MaximumInputBytes | 16,777,216 | 12..67,108,864 |
| MaximumBitDimension | 8,192 | 1..32,768 |
| MaximumBytesPerRow | 1,048,576 | 1..1,048,576 |
| MaximumMaskBytes | 8,388,608 | 0..33,554,432 |

Settings are validated before IFF copying; invalid settings throw `ArgumentOutOfRangeException`. IFF parsing is bounded to eight nodes/depth3. Metadata must be finite positive, row/dimension positive and within limits, with checked row capacity `bytesPerRow*8 >= bitDimension`. Multiplication and both exact lengths are validated before retaining mask copies. Tags/forms/versions/order/duplicates/extra/truncated/trailing data reject with `InvalidDataException`. High-bit U32 values retain unsigned interpretation and reject against bounds rather than being reinterpreted as signed dimensions.

Finite positivity, dimension/byte ceilings and row-capacity rejection are explicit stricter replacement policies; they are not asserted as recovered original rejection checks. Limits are independent and include FORM/chunk overhead in input bytes. They bound logical input/retained mask storage, not peak CLR allocations. The accepted IFF parser snapshots input; retained private mask arrays are copied from that tree and cannot be mutated by caller source changes or inspection clones. No caller memory, stream, callback or mutable IFF node is retained.

## Integer-grid queries

`GetBit` validates the enum and returns false outside the raw grid, including empty maps. It performs no allocation or coordinate conversion. `AnyBit` validates enum/work setting (1..1,048,576 cells), normalizes inclusive integer corners, clips to0..dimension-1 and returns false for an empty intersection. It preflights the **entire clipped area** before scanning, even when the first cell is set. Oversized work throws `InvalidDataException`; invalid enum/work setting throws `ArgumentOutOfRangeException`. Int32 extreme corners are safe because arithmetic follows clipping. Budget measures cells, not elapsed time or early-hit work.

These are modern grid inspection helpers. Centered/negative world-coordinate conversion and terrain-owner wrappers remain outside this API. No mask mutation, PTAT metadata loader, TGEN generation, water rendering or active SMAP behavior is implemented.

## One original proof

Read-only root `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Effective `terrain/tatooine.trn`: priority42,1,188,446 bytes, SHA `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. Direct map FORM begins at139,818, is1,048,628 bytes and SHA `D3F7B55C110D6454E328507743FA0CF4A126CD6D8A802DC040752543CC18AD9D`.

DATA:16384.0 (`0x46800000`),8.0 (`0x41000000`),256,2048. Each mask is524,288 bytes. WMAP SHA `B893659B5BCE74ACFC851BF4613F6B25A8DEB9EC1DEE27D7103910376743F3FF`; SMAP SHA `D14368C40FDAB9F37C197F9BA0F6C92E7AE236579AC6B3BF6D3FA0E14CB114D9`. Native receipt prints input identities, raw/float metadata, hashes and sampled grid bits after archives close and source buffers clear. The independent Python oracle reconstructs the direct form from raw extents and computes both hashes and sampled bits.

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainMapMasksScenarios.cs) and [fixture producer/oracle](../../SWG-Godot-Client/poc/round31-tests/Assets/verify_masks.py) pass143 synthetic fixtures (five accepted/138 rejected),18 point/14 rectangle values, five query rejections and four ownership checks. Exact/one-less caps, empty forms, positive subnormals, padded/non-byte-aligned grids, malformed metadata/records/extents, all92 truncated prefixes and clone isolation are covered. All proprietary detailed receipts remain private; no corpus sweep or original executable run occurs. [Checkpoint and commands](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R31_TERRAIN_MAP_MASKS.md).
