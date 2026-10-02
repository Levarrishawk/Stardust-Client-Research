# Blueprint face description

`BlueprintTextureRendererDecoder` owns a bounded BTRT/0001 live face description, not a generated texture. It retains template scalar, destination width/height/format IDs, ordered shader and texture name tables, owned VTXA nodes, camera scalar, ordered SRSC/RTFC render sets and unconditional PCMD/TOPS prepare lists. SSTC/SST1/STFP operands preserve target index, texture tag, source index/range, exact variable name, palette path and raw factory flag. No implicit variable defaults, palette shading, effect execution, clear or draw occurs.

The supported slice requires VTXA0003 declaration0x1101, finite position/UV data at20 bytes per vertex, zero index buffers, one finite positive PCAM scalar and the deployed live command families. Unsupported0002, alternate declarations/index buffers, conditional/false lists and dormant operations reject rather than silently execute. Destination format IDs remain raw; friendly pixel formats and the old internal remap are not guessed. Source/node/depth/table/command budgets, exact extents and referenced table indices validate before publication.

`CreatureAppearanceDescriptorBundleReader.Load(...,loadFaceBlueprints:true)` requires owned geometry, follows original MGN TRTS paths and retains each selected original blueprint once in the same source cache. It does not load all blueprint shader/texture/palette dependencies. Native `--skeletal-face-blueprints` uses existing pinned object/SAT/config arguments. The selected human closure has13 sources/332,507 bytes; blueprint priority55/3,211 bytes/SHA256 `275C45C62CBF25982F20AD45C6986C69F335BF0C3385E3036900E7A78756DA2F`.

For this exact original asset, DEST is256x256 with count4 and IDs `[4,3,5,6]`; TXTS contains32 names. There are five shader slots, five four-vertex arrays, five SRSC sets and nine prepare lists (two SSTC, three SST1, four STFP). The older envelope fixture summary incorrectly reported five texture descriptions and included the destination count as a fifth format ID. This sample correction does not rescan the other seven historical templates.

Independent raw parsing checks every emitted table, vertex-byte hash, destination, camera, render set and prepare operand. Original every-truncated-prefix, bad version/count and missing/masked dependency gates pass. Portable synthetic owned positive and truncation/reference/camera/unsupported-operation tests pass. No generated face/GPU execution is claimed. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_BLUEPRINT_FACE_DESCRIPTION.md).

Next: load the selected blueprint's two unique shader paths and their effect dependencies; verify copy versus alpha-blend pass semantics. Then prepare explicit supplied customization values and independently test the five triangle-fan layers before publishing any generated MAIN override.

## Inline face shader boundary

The two selected original639-byte shaders use inline EFCT0001/IMPL0003/PASS0007 and runtime MAIN. Explicit `StaticShaderReferences.DecodeIncludingInlineEffect` retains the owned inline effect with empty EffectPath; default Decode remains external-only. `StaticEffectDecoder.DecodeInlineEffect` retains both candidates, absent newer fields and16-byte PTXM0000 metadata. Independent original audits and negative/ownership gates pass. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INLINE_FACE_EFFECTS.md). Blend-factor numeric states are traced; effective target-alpha mode and program/combiner output remain open before generated-face execution.

## 2026-10-01 face dependency closure

The optional object-root face resource closure owns two shaders,32 DDS choices/251 declared mips and three palettes:50 sources/2,283,397 bytes including preceding geometry. Independent pixel/palette/source-union agreement passes. Seven original64×32 BC2 eyebrow files retain one16-byte terminal block via explicit `AllowLegacyTerminalBlockTail`; default DDS trailing-data rejection remains. The extra block is owned metadata, not an added mip. Preparation/rendering remains pending. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_RESOURCE_CLOSURE.md).

## Explicit preparation

`BlueprintFacePreparer` prepares separate shader slots from supplied exact name/FactoryFlag keys. SST1's nonserialized flag is true; its requested texture index clamps to endpoints. STFP preserves its serialized flag and maps any invalid palette index to the last entry. Missing values/empty palettes reject. Original0/INT_MIN/INT_MAX preparations independently agree on27 operation selections and15 slot maps. MAIN factor defaults remain authored, including white freckles. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_EXPLICIT_FACE_PREPARATION.md). Pixel program independently specifies texture×factor RGB with copied texture alpha; target-alpha/raster semantics remain open.

## Camera, sampler and execution boundary

The selected PCAM scalar1 maps X to2*x−1 and Y to1−2*y: top-left origin, Y downward. Full base/freckle/mask quads surround smaller eyebrow and beard rectangles. Copy samples wrap; alpha layers clamp; both use point mip selection with linear min/mag after PTXM inheritance. Original PSHP0000 bytecode confirms sampled texture RGB×MAIN factor with copied texture alpha. Optional owned pixel-program closure validates these semantics from referenced bytes, not the path name; original51 sources/2,283,555 bytes independently agree.

The original ordered destination file IDs4,3,5,6 map to RGB888,ARGB4444,XRGB8888,ARGB8888. Creation requests a one-mip managed destination; the backend draws via an ARGB8 scratch target and converts afterward. Pixel-center/quantization parity, capability-dependent final format and inherited global opacity remain open. A CPU preview must state these policies. [Execution checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_EXECUTION_BOUNDARY.md).

## CPU execution

[Bounded face compositor](BLUEPRINT_FACE_COMPOSITION_CONTRACT.md) now executes explicit prepared layers under a documented modern raster/normal-opacity policy. Original three profiles independently agree on15 cumulative layer digests and final256×256 RGBA images. GPU TRTS binding remains next; exact legacy raster/format/opacity parity remains separate.


Runtime domain mapping is now confirmed: factorytrue→`/private/`, false→`/shared_owner/`, matching CSHD. [Shared customization contract](SKELETAL_CUSTOMIZATION_SELECTION_CONTRACT.md) preserves separate identities while projecting one explicit selection into both consumers.
