# Original face dependency closure — 2026-10-01

## Question and input identity

Can the selected original object→SAT→LMG→MGN→TRTS→BTRT chain own its full shader/texture/palette dependency set without drawing or inventing customization values? Closed for the pinned human sample. Original pins and overlay configuration are in the [face description checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_BLUEPRINT_FACE_DESCRIPTION.md); two shader identities/layouts in the [inline effect checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_INLINE_FACE_EFFECTS.md). Inputs unchanged,48 archives descending priority; original binary execution not used.

## Observations and compatibility policy

**Confirmed original bytes:** BTRT references two unique shaders,32 texture paths and three palette paths. The object-root union is50 unique sources/2,283,397 bytes. All251 declared DDS mips independently match RGBA hashes; all three palette entry/flag/opaqueARGB canonical hashes agree. Original/source identity and single-read union agreement include the preceding mesh/skeleton/blueprint chain.

Seven priority55 eyebrow DXT3 members are64×32, declare six mips and contain one16-byte block after the declared chain. Representative hum_m_face_brow_thick.dds is2,896 bytes, SHA256 `85FB8014E73E5C797A08B562E37BD3E03DD62254FF08D1491183D3F86FEA0C0F`. The last declared mip is2×1; the remaining block could encode an omitted1×1 mip. That interpretation is **Strongly inferred**, not a recovered executable policy. Its exact bytes are retained without decoding or increasing mip count.

**Replacement policy, explicit:** DdsReadLimits.AllowLegacyTerminalBlockTail defaults false. Opt-in accepts exactly16 trailing bytes only on legacy BC2 with DDSD_MIPMAPCOUNT, count one below dimension-derived maximum and a last2×1/1×2 mip. Owned RetainedTerminalBlock preserves bytes. Default trailing-data rejection, DX10 behavior and declared mip output are unchanged. Face resource reader opts in for this observed boundary. Truncated/multiple tails, other formats/absent mip flag reject. No broad trailing-byte tolerance is introduced.

BlueprintFaceResourceReader owns shader/effect descriptions, all declared texture choices, named shader texture dependencies and palettes through existing OwnedAssetSourceCache. Shared creature load adds optional loadFaceResources requiring loadFaceBlueprints. Up to64 shader/texture/palette resources per category, default64 total source cache/16MiB encoded budget; face textures share128MiB decoded pixel budget. Required missing/masked palettes reject here; no silent missing-palette substitution or variable selection. EffectsByShader keys identify owning shader; program names remain metadata, original GPU programs are not executed.

## Validation and consequences

Native --skeletal-face-resources uses pinned object/configuration/SAT arguments. Portable suite covers synthetic dependency deduplication, missing/masked texture, resource budget and cleared source ownership. Original probe covers missing/masked shader/texture/palette families,13 preceding sources plus37 face dependencies, every original blueprint truncated prefix. Narrow DDS tail fixtures cover strict default rejection, opt-in ownership/declared count and malformed tail/format/flag gates.

Independent verify_face_resources.py checks raw effect candidate counts, every declared DDS mip/dimension/hash, retained tail bytes and every palette canonical record; integrated descriptor verifier also validates raw BTRT operands and derives the expected union from that independently checked description. All checks pass. Godot build zero warnings/errors. No generated face, GPU face upload or private corpus publication. Metrics/canonical counts unchanged.

## Next probe

Pixel program source/bytecode observation already shows RGB texture×factor and alpha copied from texture; confirm constant binding and render-target alpha policy, then prepare five layers from explicit integer values keyed by exact variable/factory identity. Preserve repeated shader slots as separate layer bindings. Determine camera/UV mapping and sampler policy before compositing original texture pixels.
