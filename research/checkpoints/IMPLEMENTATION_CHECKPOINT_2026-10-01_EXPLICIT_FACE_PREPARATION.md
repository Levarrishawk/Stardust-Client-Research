# Explicit original face preparation — 2026-10-01

## Bounded question

What variable identity and failure/selection rules prepare the five original face shader slots? Close at independently verified texture/factor bindings, without rendering. Original object/configuration/SAT/BTRT pins,50-source closure and shader identities are in the [resource checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_RESOURCE_CLOSURE.md) and [inline checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_INLINE_FACE_EFFECTS.md). Executable SHA unchanged and verified this run. Existing static disassembler/raw TRE/IFF tools used; no original binary execution.

## Observation, interpretation and confidence

**Confirmed:** SST1 loader0086D490 reads target/tag/first-source/name/count. At0086D57B..58E it passes name,zero,count and Boolean1 to0086E370.0086E38F..3B2 forwards fourth argument to exact name/Boolean lookup00870510; absent factory0086E3E6..3F5 passes it into0086C680. SST1 therefore uses factory Boolean true despite having no serialized Boolean. The historical semantic domain label remains unknown; do not invent a private/public meaning.

**Confirmed:** SST1 runtime0086D5E0..637 treats requested index as signed, clamps negative to0/high tocount−1 and adds first-source. STFP0086DF70..95 sends negative or index≥palette-count to palette-count−1; it does NOT use PaletteDecoder.Select's unrelated out-of-range-to-zero helper. Empty palette behavior in original get-entry remains unsuitable for this bounded slice; replacement preparation rejects empty palettes.

**Confirmed:** four original STFP operations retain serialized factory Boolean; same skin variable appears with false and true separately. Two hair STFP layers share one exact true key. Three SST1 style variables use true. Six distinct supplied keys suffice. The five original shader slots remain distinct despite slots1–4 sharing one shader path. TFNS0000 tag/value pairs supply retained authored factors; original MAIN defaultsFFFFFFFF (freckles slot3 remains white).

**Confirmed shader output evidence, separate from preparation:** original texren_copy_c1a1.psh is PSHP0000 with source plus executable shader-model1.1 bytecode. Its instruction records sample texture0, multiply RGB by constant3, and copy sampled alpha. Palette alpha therefore does not alter this program's output alpha. Original program priority40/158 bytes/SHA256 `9162152E5DB23B15F06AC8DE9E4DAF223614D8226BEF6C6183266D7B9CB796EB`. Vertex program priority40/836 bytes/SHA256 `9E7368941FD8D8A83C0E45A7D8610AD2E8ECD41A08F7A4B4B68B99EF62BFBB54` forwards MAIN UV and transforms position. Backend62A0E9D0 resolves PASS+40 MAIN factor into normalized ARGB channels at compiled+5C;62A0EF30 passes that block to pixel constant register3 via62A0E290. Effective render-target alpha mode and complete camera/raster/sampler semantics remain open; no generated-face fidelity claim.

## Implementation consequences

BlueprintFacePreparer requires explicit BlueprintCustomizationKey(exact variable name,FactoryFlag) values, copies input values and publishes owned per-slot texture/factor dictionaries plus ordered selected-operation evidence. No implicit index0/default or callbacks/external mutation. SSTC binds fixed source; SST1 clamps range; STFP invalid indices choose last palette entry and retains opaque palette RGB semantics. Missing values/resources, empty palettes, unsupported operations and invalid targets/ranges reject. Unbound shader textures reject. Preparation budget4096 values/operations and256 slots.

## Validation

Full portable suite passes distinct factory keys, negative/high texture/palette rules, omitted/wrong-domain values, missing dependency/target and cleared-value ownership. Original probe prepares requested0/INT_MIN/INT_MAX for six keys; all27 selected operations and15 slot binding sets independently match raw blueprint operands, TFNS defaults and RIFF palette bytes. Omission of each key rejects. Prior50-source/251-mip/palette audits remain green. Prototype host build succeeds zero warnings/errors; workspace/link/diff gates run for checkpoint. Proprietary program/source/evidence stays ignored. Counts and research scores unchanged.

## Next probe

Trace BTRT camera/render-target setup and triangle-fan vertices, correlate stage sampler overrides, then implement a bounded CPU layer compositor using the verified programmable RGB/alpha output. Verify independent synthetic raster/blend fixtures before original generated texture/GPU MAIN binding.
