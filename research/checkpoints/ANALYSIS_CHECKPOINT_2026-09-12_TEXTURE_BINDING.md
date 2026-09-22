# Analysis Checkpoint: StaticShader Texture Binding

Date: 2026-09-12

## Outcome

This block connected the SHT texture-map grammar to runtime `StaticShader` maps, the s207 sampler compiler, Direct3D texture binding and the mutable dynamic-texture registry. The original asset dependency is now specified far enough to design a renderer-neutral texture-parameter IR.

## Confirmed path

```text
SSHT -> TXMS -> TXM 0000/0001/0002 -> optional NAME
     -> tag-keyed 36-byte TextureData
     -> StaticShader::getTextureData(textureTag)
     -> per-stage sampler compiler
     -> cached texture/sampler record
     -> IDirect3DDevice9::SetTexture / SetSamplerState
```

The separate texture-coordinate tag reaches `StaticShader::getTextureCoordinateSet`, then the packed coordinate-selection key and generated vertex-shader variant.

## Version schemas

- TXM 0000: five bytes, flag then tag; all sampler fields default.
- TXM 0001: normally 11 bytes, tag + flag + address U/V/W + mip/min/mag filters.
- TXM 0002: 12 bytes, v0001 plus maximum anisotropy.
- Eleven effective v0001 records contain a tolerated trailing zero byte. A replacement decoder must accept this without shifting the defined fields.

## Corpus

- 41,741 effective TXM records.
- 39,679 v0001 occurrences, 2,060 v0002 occurrences, two v0000 occurrences.
- 45 distinct texture tags.
- Leading tags: MAIN 22,893; SPEC 4,975; NRML 2,936; CNRM 2,806; LKUP 2,657; ENVM 1,435; DETA 1,379; HUEB 669; MASK 552; EMIS 494.

## Dynamic resources

Named records use NAME -> TemporaryCrcString -> central texture cache. Special records can be replaced per `StaticShader`. The raster backend additionally treats ENVM and all underscore-prefixed tags as global dynamic resources, backed by a tag map with explicit reference-count ownership. `_SKY` occurs 23 times in the effective corpus.

## Modern-client consequence

Use a texture parameter record resembling:

```text
TextureParameter {
  tag: FourCC
  source: NamedAsset | PerMaterialSlot | GlobalDynamicSlot
  sampler: { addressU, addressV, addressW, mip, min, mag, maxAnisotropy }
  coordinateSetTag: FourCC
}
```

Do not preserve the original 36-byte `TextureData` or 0x14-byte backend record layouts: both contain 32-bit pointers and are runtime implementation details. Preserve the tags, filename, enum values, inheritance behavior and ownership semantics.

## Remaining unknowns

- Full texture resource creation, DDS format selection, mip upload and device-loss recreation path.
- Exact distinction among all non-named special texture flags at the engine level.
- Final STAG byte and fixed-function combiner translation tables.
- Whether the eleven extended v0001 DATA chunks reflect a documented exporter variant or merely tolerated padding.

## Progress

- Comprehensive weighted analysis: approximately 51.7%, reported as 52%.
- Replacement-architecture readiness: approximately 70%.
- Asset workstream: 70%.
- Renderer workstream: 39%.
