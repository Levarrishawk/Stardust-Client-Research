# Owned portal-layout decoder

## Supported structure

`PortalLayoutDecoder` accepts deployed PRTO/0001, /0003 and /0004. Required ordered children are DATA, PRTS, CELS, optional PGRF for v3/v4, then CRC. DATA is two LE U32 counts (geometry, cells); CRC is retained LE U32. PGRF, cell bounding extents and LGHT remain separately owned IFF metadata at this layout boundary. The optional [cell-light decoder](PORTAL_CELL_LIGHT_CONTRACT.md) now interprets LGHT without changing layout acceptance. Stored CRC interpretation/verification is not inferred.

Versions 1/3 PRTS contains PRTL chunks: LE U32 vertex count, then count three-F32 positions. The portable inspection representation derives `(0,i-1,i)` fan triangles, an explicit adapter choice rather than a promise of original triangulation/collision behavior. Version 4 contains IDTL/0000, with raw VERT (three F32 per position) and raw INDX (LE U32 triples). Finite positions and index ranges are checked. Original values/winding are retained; engine coordinate conversion is separate.

CELS contains CELL/0004 or /0005. DATA is LE U32 portal count, one raw flag byte, NUL cell name, NUL appearance path, floor-present byte and optional NUL floor path. Strings retain exact printable ASCII; logical resource lookup separately normalizes paths. Cell v5 can have a recognized extent form after DATA, but its absence is legal. Then come portal references and optional LGHT chunks. Raw flag semantics remain unknown.

Each FORM PRTL contains one **version chunk**, not a version form:

| Chunk | Payload order |
|---|---|
| 0002 | raw solid U8, geometry I32, raw winding U8, target-cell I32 |
| 0004 | same fields, NUL door name, transform flag U8, twelve F32 |
| 0005 | leading raw U8, then the 0004 fields |

The twelve transform floats exist even when the transform flag is zero. They use the established row-major 3x4 convention. Boolean consumers in the original normalize nonzero bytes; this representation retains raw values. Solid/winding/target labels correlate with server symbols; runtime interpretation and post-load flag changes are separate.

## Adjacency and limits

Cells use their CELS ordinal. Every observed serialized target matches the opposite side's zero-based ordinal. `Connections` groups `(cell index, reference index)` by geometry index. Unpaired or inconsistent connections remain explicit diagnostics; no fake neighbor is generated. Geometry/target indices outside declared tables reject. Cell names need not be unique structurally; runtime name resolution remains a later contract.

Policy bounds: 4,096 geometries/cells, 100,000 vertices/300,000 indices per geometry, one million vertices/three million indices overall, 4,096 references per cell and 32,768 overall, 1,024-byte strings, finite geometry/transforms and the existing 16-MiB bounded IFF reader. Adjacency construction is linear in retained references plus geometries. These are replacement limits, not legacy maxima. Results own source storage through IFF parsing and decoded arrays.

## Evidence and reproduction

Baseline static evidence: PRTO dispatcher 00B461B0, deployed v4 loader 00B47790, CELL v4/v5 loaders 00B44C80/00B451E0, PRTL v2/v4/v5 loaders 00B43A00/00B43B40/00B43C10. V4 portal geometry constructs 00ABC980, dispatches IDTL at 00ABCA70 and reads VERT/INDX at 00ABCAE0. Cell v5 calls optional extent factory 01270770, which returns NULL without consuming an unregistered next tag. The PRTL v5 reader consumes its target I32 but does not store it in the reference object along that loader path. The older claim that post-load helper 00B43E80 itself resolves neighbors is corrected: its instructions invert the second reference byte. Full original linking/flag semantics remain outside this decoder.

Server correlation: `MMOCoreORB/src/templates/appearance/PortalLayout.cpp`, `PortalLayout::parse` and geometry readers; `CellProperty.cpp`, `CellPortal::readObject` and `CellProperty::loadVersion4/5`. These label fields and support matching-geometry adjacency; original bytes independently establish widths and relationships.

`verify_portal_layout.py ROOT CONFIG PRIVATE_OUTPUT --core-dll CORE_DLL` independently discovers current effective winners and compares all source identities, geometry float bits/indices, cell strings/flags, portal fields/transforms and opaque metadata hashes. All 291 live layouts agree: 3,470 cells, 3,821 geometries, 7,642 references; every geometry has two sides and every target agrees. Versions are 3 v1/215 v3/73 v4; one masked POB remains excluded explicitly. There are 2,448 nonempty door names and 2,288 enabled transform flags. This proves parsing/topology, not render visibility, collision/navigation, door creation or a playable interior. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LAYOUT_READER.md).

## 2026-09-30 permission barrier follow-up

The [barrier contract](PORTAL_BARRIER_COLLISION_CONTRACT.md) adds normal-controller primitives and bounded source-cell actor-box contact before transfer. Runtime fields are LeadingByte exclusion, SolidByte support and separate WindingByte; B43E80 inversion is specific to PRTO/0002. Current permission, mutable portal state and physical door-leaf clearance remain distinct. Independent 291-layout / 21,693-probe agreement and 12-link prototype host actor contact gates pass; nonlevel requests stop explicitly. Physical leaf collision and global actor solver ordering remain next.
