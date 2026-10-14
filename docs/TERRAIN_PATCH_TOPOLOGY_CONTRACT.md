# Terrain patch topology contract

## Bounded behavior

`Stardust.Core.Geometry.TerrainPatchTopology` is an engine-neutral compatibility
prerequisite, not a terrain generator. It owns nine fixed active ushort index
vectors and exposes explicit pattern indices 0 through 8. It supplies no
positions, normals, materials, PTAT/TGEN evaluation, chunk scheduling, collision
planes, neighbor-mask selector, or GPU objects.

The baseline executable is `Stardust.exe`, SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Addresses here are VAs. Prior context:
[world terrain architecture](WORLD_TERRAIN_ARCHITECTURE.md) and
[ClientChunk mesh/blend checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_CLIENTCHUNK_MESH_BLEND.md).
This milestone verifies the compact compatibility vectors against original
bytes; it does not raise any research or implementation coverage percentage.

## Evidence and confidence

| Observation | Interpretation / confidence | Implementation consequence |
| --- | --- | --- |
| Nine little-endian U32 counts at `015E7468` are `24,21,18,21,18,21,18,21,18`. | Confirmed active index counts: 8, 7, or 6 triangles. | Preserve counts and order; unused padding is not geometry. |
| Nine 48-byte slots at `015E7490` each contain up to 24 little-endian ushort indices. Active indices are all 0..8; trailing unused words are zero. | Confirmed local topology vectors, 180 active indices total. | Keep compact active definitions; copy exact triangle order. |
| Installer `00865A80` points to both tables at `00865AC7/00865AD1`, copies words at `00865B23/00865B26`, increments count pointer by 4 and vector pointer by `30h`, and stops at `015E748C`. | Confirmed table association, stride, and nine iterations. | Do not concatenate padded slots as active triangle lists. |
| Batching `008668E0` copies nine positions (loop counter at `00866991`), reads the selected index list from primitive `+120h`, zero-extends a ushort at `00866A04`, adds destination vertex base at `00866A0E`, and writes a dword at `00866A1E`. | Confirmed local-index rebasing into 32-bit batch output. | Local storage stays ushort; output is not restricted to 16-bit range. |
| Existing selector `00865FF0` consumes seam flags and grid-boundary coordinates. | Its original branch structure is prior evidence; integration of mask directions and generated vertex coordinates is unclosed in this milestone. | Expose explicit pattern index only. No invented mask-to-pattern or coordinate mapping. |

The following ring description specifies the active vectors compactly. Each
adjacent directed pair `(a,b)` expands to triangle `(0,a,b)`, in row order.
These are exact compatibility vectors, not generated proprietary terrain data.

| Pattern | Directed perimeter sequence | Indices |
| ---: | --- | ---: |
| 0 | 1,2,3,4,5,6,7,8,1 | 24 |
| 1 | 1,2,4,5,6,7,8,1 | 21 |
| 2 | 2,4,5,6,7,8,2 | 18 |
| 3 | 2,3,4,5,6,7,8,2 | 21 |
| 4 | 2,3,4,5,6,8,2 | 18 |
| 5 | 1,2,3,4,5,6,8,1 | 21 |
| 6 | 1,2,3,4,6,8,1 | 18 |
| 7 | 1,2,3,4,6,7,8,1 | 21 |
| 8 | 1,2,4,6,7,8,1 | 18 |

Vertex 0 is the center and 1..8 constitute the perimeter numbering established
in prior research. This table does not assign those numbers to original X/Z
coordinates, a compass direction, coordinate handedness, or renderer front-face
winding.

## API and modern bounds

[Production helper](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainPatchTopology.cs):

- `GetIndexCount(patternIndex)` returns the active count.
- `CopyIndices(patternIndex)` returns a new owned ushort array.
- `RebaseIndices(patternIndex, vertexBase, destinationVertexCount)` returns a
  new owned int array, preserving index order and adding the same base to each
  index.

All three reject pattern indices outside 0..8. Rebasing rejects negative bases,
destination counts below nine, and a complete nine-vertex cell extending beyond
the declared destination range. `vertexBase + 9` is checked; signed 32-bit
overflow throws before allocation. For example, base 65536 and count 65545 are
valid; base 1 and count 9 reject. The largest valid base with count
`int.MaxValue` is `int.MaxValue - 9`.

The range is caller-declared metadata: the helper does not inspect or allocate
the destination vertex buffer. Each operation copies at most 24 indices
(48-byte ushort payload or 96-byte int payload, excluding array overhead).
Complete-cell validation is intentional even when a collapsed pattern omits a
perimeter vertex. Checked range and signed-int rejection are modern safety
policies; original overflow/rejection behavior is not asserted.

The internal arrays are private and never mutated after construction. No public
API exposes their backing storage. Returned copies may be mutated by the caller
without affecting later queries or other outputs. There is no mutable service
state, I/O, engine dependency, callback, or thread-affine lifetime. A failed
call cannot change topology or a caller's previous result.

## Verification and limits

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainPatchTopologyScenarios.cs)
and the [independent static/ring oracle](../../SWG-Godot-Client/poc/round31-tests/Rendering/verify_topology.py)
pass 362 native checks: 45 rebasing cases, 66 rejection cases, and 60 synthetic
triangles. The oracle compares every active original index, all counts and
padding, twelve instruction sites in two ranges, and all native output against
an independently authored ring-edge description. Wrong executable identity and
a truncated synthetic input reject before PE parsing or VA interpretation.

Synthetic points are explicitly `(X,Z)`:
`(0,0),(1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1),(0,-1),(1,-1)`.
Every triangle has positive signed X/Z area under that fixture. This checks
consistent order and nondegeneracy for the declared fixture only; it is not
proof of original coordinate order or Godot winding.

No original client execution, Godot/GPU operation, live server, packet capture,
download, or terrain corpus sweep was performed. Full generator/scene/render
integration remains deferred. Reproduction and exact next action are in the
[Round 31 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R31_TERRAIN_PATCH_TOPOLOGY.md).
