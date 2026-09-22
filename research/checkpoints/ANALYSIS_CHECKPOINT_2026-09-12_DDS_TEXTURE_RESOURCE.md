# Analysis Checkpoint: DDS Texture Resource

Date: 2026-09-12

## Outcome

This block traced named texture acquisition from the CRC cache through DDS header parsing, mip policy, Direct3D resource creation, face/level upload and destruction. It also inventoried every effective `.dds` namespace winner.

## Confirmed runtime path

```text
logical texture name
  -> TextureList CRC cache (mutex protected)
  -> TreeFile DDS open
  -> DDS magic + 124-byte header
  -> format/cube/mip classification and discard policy
  -> Graphics API texture-data creation
  -> D3D9 managed 2D/cubemap resource
  -> per-face/per-mip lock, row copy or conversion, unlock
```

Static textures use D3D9's managed pool. The future explicit renderer must instead retain an asset key or CPU upload representation for GPU-resource reconstruction.

## Effective corpus

- 30,109 `.dds` path winners.
- 29,903 valid DDS resources.
- 203 tombstones.
- Three paths contain `FORM SSHT` rather than DDS despite their extension.
- Formats: DXT5 21,673; DXT1 4,874; BGRA32 2,325; DXT3 910; BGR24 113; A1R5G5B5 4; L8 4.
- 174 cubemaps.
- Eight non-power-of-two textures.
- 1,668 textures declare less than a complete dimension-derived mip chain.

## Compatibility requirements

- Validate DDS magic/header rather than trusting `.dds`.
- Honor declared mip counts and cubemap flags.
- Support BC1, BC2 and BC3 direct upload plus the small uncompressed tail.
- Preserve global and normal-map-specific highest-mip discard controls.
- Use 4x4 block minimums when discarding compressed mips.
- Provide deterministic fallback-texture behavior for absent or unsupported payloads.
- Keep resource identity and reference ownership independent of native pointer size.

## Unknowns carried forward

- Exact labels for internal formats unused by the effective corpus.
- Detailed procedural/render-target texture lifecycles.
- Whether the three extension anomalies are ever requested as textures in normal play.
- Modern policy for optional transcoding where a target GPU lacks a legacy format.

## Progress

- Comprehensive weighted analysis: approximately 52.4%, reported as 52%.
- Replacement-architecture readiness: approximately 72%.
- Asset workstream: 73%.
- Renderer workstream: 43%.
