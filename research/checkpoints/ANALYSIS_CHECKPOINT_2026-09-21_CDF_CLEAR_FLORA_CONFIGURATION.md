# ClientDataFile `CNCF`: clear-flora exclusion circles

Scope: `CNCF` parsing and runtime application in `Stardust.exe`, correlated against the effective client-data corpus. This closes the semantic unknown previously left at `ClientDataFile +0x44`.

## Finding

`CNCF` is a **clear-flora configuration** record. Each record defines one object-local circular exclusion region used by the terrain/flora system:

```text
float32 local_x
float32 local_y
float32 local_z
float32 radius
```

The executable stores an ordered vector of these 16-byte records at `ClientDataFile +0x44`. The vector is not a camera, collision-response, or generic notification configuration.

## Evidence chain

### Parser

The `CLDF/0000` loader branch in `0x00606250` enters chunk `CNCF`, reads a three-float vector, reads one additional `float32`, and appends the resulting 16-byte value to the vector at `ClientDataFile +0x44`.

### Object application and deferred terrain registration

`ClientDataFile_applyToObject_VA_00604420` checks whether the `+0x44` vector is non-empty. If so, it connects a singleton receiver at `DAT_0191D630` to the engine's object notification source. This operation is registration infrastructure; it does not itself consume the four CNCF values.

The receiver callback at `0x006FF5F0`:

1. accepts a newly observed object;
2. verifies through RTTI that its template is a `SharedObjectTemplate`;
3. obtains `SharedObjectTemplateClientData` and downcasts it to `ClientDataFile`;
4. reads `ClientDataFile +0x44` through accessor `0x00605E80`;
5. obtains the singleton at `DAT_01947194`;
6. forwards the object and CNCF vector through `0x00B5CC40` to virtual slot `+0x14C` of the singleton's terrain-facing member at `+0x28`.

Earlier rendering analysis independently tied this singleton to the terrain/world environment path. The complementary receiver method at `0x006FF660` calls the corresponding removal path (`0x00B5CC60`), showing that exclusion regions are registered and unregistered with object lifetime.

### Asset corpus

A precedence-correct scan of the configured TRE set found exactly **147 CNCF records in 95 effective CDF files**. The strongest self-describing examples are:

| CDF | Vector | Radius |
|---|---:|---:|
| `shared_generic_flora_clearing_05m.cdf` | `(0,0,0)` | `5` |
| `shared_generic_flora_clearing_10m.cdf` | `(0,0,0)` | `10` |
| `shared_generic_flora_clearing_15m.cdf` | `(0,0,0)` | `15` |
| `shared_bunker_clear_flora.cdf` | `(0,0,12)` | `12` |
| `shared_shuttleport.cdf` | `(0,0,0)` | `29` |

Multi-record building CDFs place several circles at distinct local X/Z offsets. Across the effective corpus, the middle coordinate is consistently zero while X/Z offsets and radius vary, matching ground-plane flora masks rather than arbitrary 3D collision volumes.

## Replacement-client contract

- Preserve the on-disk four-`float32` layout and record order.
- Transform each local center by the owning object's world transform before registering it with terrain vegetation suppression.
- Register after the object becomes visible/known to the terrain system and unregister when it leaves or is destroyed.
- Permit multiple circles per object.
- Treat a zero radius as legal legacy data; do not widen, normalize, or silently discard it in the parser.
- The renderer/terrain implementation may use a different internal spatial index, but the resulting suppression behavior must follow these asset-authored local circles.

## Stable provisional names

| Address | Provisional name | Confidence |
|---:|---|---|
| `0x00605E80` | `ClientDataFile_getClearFloraConfigurations_VA` | Confirmed |
| `0x006FF5F0` | `ClearFloraObjectNotification_onObjectAdded_VA` | Strongly inferred |
| `0x006FF660` | `ClearFloraObjectNotification_onObjectRemoved_VA` | Strongly inferred |
| `0x00B5CC40` | `TerrainWorld_registerObjectClearFloraRegions_VA` | Strongly inferred |

The exact original C++ class names for the notification receiver and terrain member are not present in the recovered symbols. The behavior and data contract are nevertheless established.

## Confidence

- Byte grammar and storage offset: **confirmed by code**.
- Deferred object/template/CDF lookup and terrain-world forwarding: **confirmed by code**.
- Clear-flora purpose, local-center interpretation, and radius field: **confirmed by code plus self-describing corpus evidence**.
- Exact historic class/method names: **unknown**; provisional names above are behavioral labels.
