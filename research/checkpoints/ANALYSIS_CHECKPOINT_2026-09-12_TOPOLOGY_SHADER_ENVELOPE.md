# Analysis Checkpoint — Topology, SIDX, and Shader/Effect Envelope

Date: 2026-09-12  
Primary binary: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

This block closes the complete static-mesh topology enum, establishes SIDX as view-dependent index ordering, and traces external SHT references through EFCT/IMPL/PASS selection. Its original claim that all 171 mesh-corpus anomalies were tombstones was later disproven by the corrected kind-0 TRE size rule; see the correction below.

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

Correction (2026-09-22): only the 65 records in `mtg_patch_004_appearance_04.tre` have zero uncompressed size and are SearchTree masks. The 88/11/7 SKU records are 106 **live**, uncompressed `MESH/0005 / SPS/0001 / VTXA/0003` assets with `csize=0` and `usize>0`. The corrected all-archive inventory returns 29,131 such MESH/0005 tuples, 11,814 MESH/0004 tuples, six legacy tuples, 65 true masks, and zero extraction errors. The topology and SIDX code findings of this historical checkpoint are unaffected. See `ANALYSIS_CHECKPOINT_2026-09-22_APPEARANCE_FETCH_FALLBACK.md`.

## Progress

- Strict whole-client analysis: **47%**.
- Replacement-client-relevant understanding: **63%**.
- VFS/TRE/templates/assets: **60%**.

These percentages measure documented, evidence-backed behavior, not the percentage of machine functions renamed.

## Exact resume point

Decode PASS 0009 backward through its shared fields: alpha/depth/cull/fog/stencil/material state, texture-stage bindings, fixed-function fallback, and vertex/pixel program references. Then mechanically inventory installed SHT/EFCT/IMPL/PASS versions and tags to prioritize only the schemas required by the actual Stardust asset corpus.
