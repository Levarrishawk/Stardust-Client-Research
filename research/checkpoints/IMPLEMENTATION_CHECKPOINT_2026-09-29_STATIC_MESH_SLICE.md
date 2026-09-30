# Original static mesh CPU geometry slice

Date: 2026-09-29. Question: can a bounded engine-neutral decoder reproduce one
original MESH's positions, normals, UVs and triangle indices independently of
Godot? Stop at exact geometry agreement and malformed-input tests, before rendering.

## Inputs and evidence

- Executable baseline SHA-256, rechecked before using VAs:
  `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
- Archive `mtg_patch_004_appearance_04.tre`, SHA-256
  `A5E77EBE4FB96714E51FD6189AE9D769493F91DB9E142C1FE2C9A8140D18B784`.
- Member `appearance/mesh/thm_tato_imprv_building_s09_r0_mesh_l4.msh`:
  9,634 bytes, SHA-256
  `CA4F3070B8BBC9CD50DEC617AC2C4C249F3E888E15EB10E6F832113F7C6D126B`.
- Existing executable loaders `0x00770220` / `0x007704E0` select primitive
  versions 0000 / 0001 and index readers `0x007700A0` / `0x00770160`.
  The former reads U32 entries and truncates to U16; the latter reads U16.
  Index count is U32 LE in both. This corrects the durable document's previous
  statement that SPS/0001 always used U16 file entries.
  Direct disassembly recheck identifies calls at `0x007702FD` / `0x007703B5`
  to `0x007700A0`, and `0x007705BD` / `0x00770675` to `0x00770160`.
- VTXA/0003 declaration `0x1105` selects position F32×3, normal F32×3,
  and one F32×2 UV set, in that order: 32-byte vertex stride. Loader
  `0x00784D90` and original member byte lengths agree.

See the [durable asset specification](../../docs/ASSET_PIPELINE.md) and
[prior schema checkpoint](ANALYSIS_CHECKPOINT_2026-09-11_SPS_VTXA.md).

## Findings and implementation consequence

**Confirmed:** the original member is MESH/0004, SPS/0001, VTXA/0003,
with nine shader groups/primitive-0000 batches, 190 vertices, 477 indices and
159 triangles. Its index entries occupy four bytes in the file. All batches
use topology 9, indexed=true, sorted=false, declaration `0x1105`.

`IffDocument` owns a copy of its input and traverses declared ends without
classic even-byte padding. It bounds bytes (16 MiB), nodes (100,000) and depth
(32 by default; configurable maximum 128). Children cannot exceed parents;
there must be one FORM root. Opaque APPR metadata remains owned and preserved.

`StaticMeshDecoder` accepts MESH/0004 or /0005, SPS/0001, external shader NAMEs,
primitive versions 0000/0001, topology 9 with an index buffer and no SIDX,
and exactly VTXA/0003 declaration `0x1105`. It caps shader groups at 1,024,
vertices at one million per primitive and indices at three million; requires
finite components, triangle cardinality and indices within the vertex range.
These rejection limits are replacement policy, not recovered legacy constants.

Raw legacy positions, normals, UVs and winding are preserved. APPR hardpoints,
bounds, renderer axes, materials, tangent conventions and all other mesh
layouts remain outside this decoder. No claim of broad mesh compatibility or
visible original rendering follows.

## Independent agreement and reproduction

Both C# and independent Python decode the original bytes into this geometry
fingerprint:
`39B94C0F1BAC9FD33633CFDB25CD88C44AC7DBD625017A52677315E325F40800`.
For each batch in serialized order, the hash contains LE32 vertex/index counts,
eight LE F32 values per vertex, then converted LE U16 indices. Shader names and
materials are excluded. Agreement establishes this CPU geometry slice only.

```powershell
python tools/python/verify_static_mesh_slice.py C:\Stardust-DEV\mtg_patch_004_appearance_04.tre appearance/mesh/thm_tato_imprv_building_s09_r0_mesh_l4.msh CA4F3070B8BBC9CD50DEC617AC2C4C249F3E888E15EB10E6F832113F7C6D126B
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -- --mesh-tre C:\Stardust-DEV\mtg_patch_004_appearance_04.tre --member appearance/mesh/thm_tato_imprv_building_s09_r0_mesh_l4.msh --expected-sha256 CA4F3070B8BBC9CD50DEC617AC2C4C249F3E888E15EB10E6F832113F7C6D126B
```

Synthetic tests cover both primitive widths; U32 `0x10001` becoming U16 `1`;
all truncated prefixes; extra bytes; bad parent sizes, declaration/count,
topology, sorted flag, versions and out-of-range indices; NaN/Infinity;
ownership after clearing input; odd chunk traversal; byte/node/depth limits.
Full core suite passes. Original inputs were read only; proprietary extracts
remain ignored, and no new function names or registry rows were added.

Next: resolve the nine external shader dependencies through explicit configured
mounts, then establish one texture decode/material/axis conversion with fixtures
before adapting this geometry to Godot.
