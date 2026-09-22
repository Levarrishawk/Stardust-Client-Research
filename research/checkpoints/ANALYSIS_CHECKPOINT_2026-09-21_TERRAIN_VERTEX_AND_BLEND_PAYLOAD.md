# Terrain Vertex and Blend Payload Closure

Target: `C:\Stardust-DEV\Stardust.exe`

This block traces the nine-vertex `ClientChunk` cell primitive through the `ClientTerrainSorter` batch object and its dynamic vertex-buffer fill routine. It resolves the prior ambiguity between the packed per-vertex DWORD and the `ALP1..3` terrain blend resources.

## Confirmed primitive CPU layout

`terrain_create_cell_primitive_VA_00865DA0` allocates a 0x148-byte primitive and copies:

| Offset | Count/type | Meaning |
|---:|---|---|
| `+0x0C` | 9 x float3 | positions |
| `+0x78` | 9 x float3 | normals |
| `+0xE4` | 9 x U32 | packed diffuse colors |
| `+0x108` | U32 | material-complexity/sorter category |
| `+0x10C` | float | cell texture-coordinate scale |
| `+0x110..0x11C` | 4 x U32 | indices selecting predefined auxiliary UV sets |
| `+0x120` | pointer | selected one-of-nine seam index list |
| `+0x124` | pointer | eight-triangle plane/collision receiver |

Bounds follow at `+0x128..+0x144`. These offsets are x86 implementation evidence, not a replacement ABI.

## Dynamic GPU vertex payload

`client_terrain_batch_prepare_draw_VA_0084E270` is the `ClientTerrainSorter::PrimitiveNode` preparation method. It locks/obtains the dynamic vertex and 16-bit index destinations, walks every retained cell primitive, and writes fields through signed offsets supplied by the shader-compatible vertex-buffer iterator.

The emitted semantic payload is:

1. `POSITION`: float3 copied from primitive `+0x0C`.
2. `NORMAL`: float3 copied from primitive `+0x78`.
3. `COLOR0`/diffuse: packed U32 copied from primitive `+0xE4`.
4. First texture coordinate: float2 local patch coordinate multiplied by primitive `+0x10C`.
5. Further texture coordinates: zero to three float2 pairs selected from the static table at `0x015E7640`, indexed by the corresponding selector in `+0x110..0x11C` and the local vertex number.

The shader's encoded texture-coordinate count is read from bits 8..11 of the batch format field. The routine emits the first UV plus `count - 1` auxiliary pairs. Consequently there is no single hard-coded byte stride or D3D declaration for all terrain shaders: semantic destinations and stride are obtained from the active shader/vertex format. The compatibility contract is the semantic payload above, not a particular D3D9 declaration.

The local nine-point first-UV pattern is confirmed as center plus perimeter ring:

```text
(0.5,0.5), (0,0.5), (0,0), (0.5,0), (1,0),
(1,0.5), (1,1), (0.5,1), (0,1)
```

Selected seam indices are copied to the dynamic 16-bit index buffer with a nine-vertex base offset for each cell.

## Packed DWORD is lighting color, not ALP blend weights

The primitive builder obtains a three-byte generated color sample, converts its RGB bytes to normalized floats using `1/255`, supplies alpha 1.0, and repacks it as an ARGB/D3D diffuse DWORD through `0x00AB87B0 -> 0x00ABA1C0`. Under an enabled terrain-color modification path, `0x00AB88B0` adjusts the RGB sample before it is repacked. The batch writer sends this DWORD to the diffuse/color semantic.

Therefore the DWORD at primitive `+0xE4` is vertex lighting/color modulation. It is **not** a packed set of terrain-layer weights.

## ALP1..3 contract

`terrain_fetch_blend_shader_VA_008654F0` binds resources through shader tags. For each additional layer it calls the same texture/resource setter with:

- `ALP1`, `ALP2`, or `ALP3`;
- `LVL1`, `LVL2`, or `LVL3`;
- optionally `NRM1`, `NRM2`, or `NRM3`.

The material descriptor stores the corresponding alpha-map resource handle separately from the level and normal texture handles. `ALP1..3` are therefore sampled alpha/blend **textures**, not names for components of the packed vertex diffuse color. Their sampling coordinates are supplied by the auxiliary float2 UV sets described above. The base layer uses `LVL0` (and optional `NRM0`) plus the `TERR` material setup; additional layers are composited through their alpha resources.

Exact authored channel semantics inside every ALP texture (for example whether a given shader implementation reads alpha versus luminance) remain shader-program-specific, but the client-side resource and coordinate contract is closed.

## Renderer-neutral replacement model

The replacement terrain vertex should expose explicit semantics rather than reproduce the iterator's signed offsets:

```text
TerrainVertex {
  float3 position;
  float3 normal;
  UNORM8x4 diffuse;
  float2 baseUv;
  float2 blendUv[up to 3];
}
```

A modern implementation may reduce or derive redundant UV sets after validating all installed terrain shaders, but the original asset path must initially preserve the shader-requested coordinate count, ALP resource identity, and per-layer coordinate selection. The D3D9 dynamic-buffer globals, 16-bit offset fields, and 0x148 primitive layout should not cross the new renderer boundary.

## Confidence

- Primitive offsets/counts, dynamic buffer writes, nine-point UV pattern, texture-coordinate count, selected-index copying, ARGB color conversion, and ALP/LVL/NRM resource setters: **Confirmed by code**.
- Naming the packed DWORD “diffuse lighting/color modulation”: **Strongly inferred** from the destination semantic and conversion path.
- Historical C++ member names and shader-specific ALP channel interpretation: **Unknown / deliberately not invented**.

