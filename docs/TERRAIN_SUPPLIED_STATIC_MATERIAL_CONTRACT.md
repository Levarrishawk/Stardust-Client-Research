# Explicit supplied static terrain material

TerrainSuppliedStaticMaterial (external historical reference) is an engine-neutral, two-source closure over a caller-supplied normalized SSHT path, exact shader SHA-256 and borrowed resolver. It reuses frozen `StaticShaderReferences`, `StaticShaderParameterDecoder`, `StaticShaderFactors`, `StaticShaderMaterialDecoder` and `DdsDecoder`. It performs no terrain shader-family lookup, original effect execution, lighting, engine allocation, or resolver/archive disposal.

## API and modern policy

`Load(resolve, shaderPath, shaderSha256, policy = TextureTimesVertexColor, limits = null, cancellation = default)` returns an owned material with actual selected `Binding` in its owned `Shader.Textures`, `Parameters`, integer `Factors`, `ShaderSource` and `TextureSource`. Each identity preserves logical path, actual resolver priority, encoded byte count and captured-byte SHA-256. The root pin is checked exactly, case-insensitively; texture identity is recorded rather than externally pinned. `ExternalReads` is two on success. Missing/masked sources do not fall back.

`TextureTimesVertexColor` computes binary32 MAIN texel RGBA * supplied vertex RGBA. The optional `TextureTimesVertexColorAndAuthoredMainDiffuse` additionally multiplies the directly authored MAIN-tag MATL diffuse RGBA, using the frozen material component decoder. **Missing MAIN MATL explicitly means white identity.** `AuthoredMainDiffuse` remains null when absent; `DiffuseMultiplier` reports the actual policy multiplier. An authored diffuse is retained even under the default policy. TFNS factors, other materials, coordinate maps and effect references remain metadata; TFNS factors are not silently mapped to diffuse or lighting.

`Modulate(mainTexel, vertexColor)` requires finite RGBA components in [0,1]. It applies no gamma conversion, physical lighting, alpha-mode conversion, sampler addressing or color clamping. DDS color-space/alpha metadata are retained, so engine adapters must explicitly decide how to sample/convert before using this numerical policy. All material color fields must also be in [0,1]; all 17 MATL scalars must be finite through the frozen decoder. HDR/negative color components reject, even in unselected material records. Specular power is retained without introducing a lighting interpretation.

## Selection, ownership and upload boundary

Exactly one authored MAIN binding (`0x4D41494E`) must have `SourceFlag == 0` and a named `texture/... .dds` resource. Duplicate tags, missing MAIN, dynamic MAIN and unnamed MAIN reject before texture resolution. CSHD, SWTS, unsupported SSHT/TXM versions, malformed IFF/maps and unsupported HDR DDS reject; no wrapper traversal or dynamic substitution occurs. Other texture references can remain present or dynamic as metadata; they are not resolved. External effects are not read. An inline EFCT is accepted only if the frozen parser validates it, without executing it.

The caller shader path must already be normalized printable ASCII `shader/... .sht`, with no empty, dot/dot-dot or colon components. The selected texture path undergoes the same strict canonical-component checks after frozen reference decoding/normalization. Root identity validation precedes parsing or dependency resolution.

`Width`, `Height`, `MipCount`, `Format`, `ColorSpace`, `AlphaMode` and `DxgiFormat` describe the actual retained DDS result. `CopyMipRgba(level)` (alias `CopyRgba`) and `CopyDecodedTexture()` return detached arrays/images; no retained RGBA memory is exposed. Shader descriptions own their parsed byte snapshot; resolver payload arrays are copied before parsing. Parameter/factor maps and shader binding lists are read-only views.

CPU admission permits valid partial mip chains accepted by the existing decoder. `IsCompleteMipChain` compares retained mip count with the dimension-derived complete chain. `IsSingleMipOrCompleteChain` is true for a single mip or complete chain. **The intended host adapter rejects false before any GPU allocation.** No GPU upload occurs here. Returned mip rows and RGBA channel order follow the existing DDS contract; a partial chain is not extended or synthesized.

## Bounds and failure

| Limit | Default | Hard maximum |
|---|---:|---:|
| Resolver reads | 2 | 2 |
| Shader encoded bytes | 1,048,576 | 16,777,216 |
| Texture encoded bytes | 67,108,864 | 67,108,864 |
| Combined source bytes | 68,157,440 | 134,217,728 |
| Dimension | 8,192 | 32,768 |
| Mips | 16 | 16 |
| Decoded RGBA bytes | 134,217,728 | 134,217,728 |

All options/path/pin syntax and the required two-read allowance precede resolution. Individual/checked combined source byte limits precede copying. Known DDS mip count is checked before decode; actual DDS structure, dimensions and complete checked decoded-byte reservations are validated by the frozen decoder before RGBA allocation. The loader reconciles actual retained mip lengths/totals before publication. Read/source limits describe this closure, not allocation performed inside a caller-owned resolver or measured peak CLR heap.

Cancellation is checked before each resolve, after resolver return, after copied bytes and hashing, after shader parsing, before/after synchronous DDS decode and immediately before result publication. Frozen parsers/decoders have no internal token hooks; cancellation during them is observed afterwards. No partial material is returned. Earlier results and external resolver ownership remain unchanged after failure. Caller mutation during byte capture is not an atomic snapshot; resolver owners must serialize their own mutable payload access.

## Evidence and replay

The native scenarios (external historical reference), the separate implementation record and offline replay runner (external historical reference) exercise actual Core APIs. Synthetic bytes and expected RGBA/scalar bits are authored independently before native execution. Original IFF/source extraction and Pillow mip hashes independently verify one pinned existing terminal-base shader diagnostic; it has **no established original terrain association**.

See the [round64 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R64_SUPPLIED_TERRAIN_MATERIAL.md) for identities, exact results, confidence, limits and next action. Original bytes and generated detailed fixtures remain private. `OriginalTerrainAssociation` and `OriginalShaderExecutionParity` stay false. No Assets64 sibling, prototype host file or shared ledger/status artifact is consumed or changed by this worker.
