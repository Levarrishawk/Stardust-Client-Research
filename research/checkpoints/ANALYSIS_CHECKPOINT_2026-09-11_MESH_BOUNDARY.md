# Analysis Checkpoint — MESH Boundary

Date: 2026-09-11  
Primary binary: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

This continuation followed the newly recovered appearance registry into the concrete MESH loader and stopped at the coherent shader-primitive-set version boundary.

Strict whole-client progress advances from **44% to 45%**. The combined VFS/TRE/templates/assets workstream reaches **50%**.

## Confirmed structure

```text
MESH
  0002 or 0003
    legacy APPR fragments
    CNTR: Vector3
    RADI: float
    SPS : shader primitive set

  0004 or 0005
    APPR: versioned shared appearance metadata
      extent
      optional HPTS
        repeated HPNT: Transform + name
    derived center/radius
    SPS : shader primitive set
```

SPS requires `FORM SPS ` and dispatches versions `0000` and `0001`. This is the next safe boundary: the block does not yet claim exact vertex formats, topology, index widths or shader-reference encoding.

## Architectural consequences

- Hardpoint name/transform pairs belong in the engine-neutral appearance description.
- Bounds and extents must be decoded independently of the renderer.
- Legacy CNTR/RADI and newer APPR extents should normalize into one modern bounds model.
- DPVS test-model creation is legacy runtime culling behavior, not serialized asset data.
- SPS decoding should produce primitive batches referencing engine-neutral materials, vertex streams and index streams.

## Exact resume point

Decompile SPS version loaders `0x00770D60` and `0x00770F70`, enumerate per-primitive forms/chunks, then follow shader-template references into ShaderTemplateList. Validate the resulting schema against representative installed MSH files before claiming field layouts.
