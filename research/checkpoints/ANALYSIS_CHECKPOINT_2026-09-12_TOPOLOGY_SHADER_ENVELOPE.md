# Analysis Checkpoint — Topology, SIDX, and Shader/Effect Envelope

Date: 2026-09-12  
Primary binary: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

This block closes the complete static-mesh topology enum, establishes SIDX as view-dependent index ordering, traces external SHT references through EFCT/IMPL/PASS selection, and resolves every one of the 171 mesh-corpus anomalies as an archive tombstone rather than live geometry.

## Confirmed topology

IDs 0-5 are non-indexed point list, line list, line strip, triangle list, triangle strip and triangle fan. IDs 6-11 are the same ordered topologies using an index buffer. Evidence is the twelve-entry initializer at `0x0076F890`, graphics-interface slots `0x120`-`0x150`, loader index conditions, and metrics calculations.

## Confirmed SIDX behavior

Each SIDX element is `(Vector3 localDirection, IndexBuffer indices)`. `static_shader_primitive_prepare_VA_007707D0` transforms camera displacement into primitive-local space, selects the element with the minimum direction dot product, and binds its indices. Ordinary INDX is used when SIDX is absent.

## Confirmed shader/effect hierarchy

```text
SPS shader group
  NAME external SHT | inline shader-template form
    shader-template outer-tag registry
    effect reference
      EFCT 0000/0001 | NAME external EFT
        ordered implementation candidates
          IMPL 0000..0007 | NAME external implementation
            capability/option filtering
            counted PASS 0000..0009 records
```

Missing external resources use explicit `shader/defaultshader.sht` and `effect/defaulteffect.eft` fallbacks. The first supported implementation wins. Detailed PASS state fields are the next boundary.

## Corpus anomaly closure

All 171 prior anomalies have zero payload size and are SearchTree masks: 88 in `data_sku1_00.tre`, 11 in `data_sku1_06.tre`, seven in `data_sku1_07.tre`, and 65 in `mtg_patch_004_appearance_04.tre`. Some retain nonzero uncompressed-size metadata. No alternate live MESH/SPS/VTXA tuple remains.

## Progress

- Strict whole-client analysis: **47%**.
- Replacement-client-relevant understanding: **63%**.
- VFS/TRE/templates/assets: **60%**.

These percentages measure documented, evidence-backed behavior, not the percentage of machine functions renamed.

## Exact resume point

Decode PASS 0009 backward through its shared fields: alpha/depth/cull/fog/stencil/material state, texture-stage bindings, fixed-function fallback, and vertex/pixel program references. Then mechanically inventory installed SHT/EFCT/IMPL/PASS versions and tags to prioritize only the schemas required by the actual Stardust asset corpus.
