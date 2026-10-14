# Explicit scene creature face inspection resources

## API and scope

SceneCreatureResourceReader.Load (external historical reference) adds final optional `SceneCreatureFaceInspectionPolicy? faceInspection=null`. SceneObjectResourceRouter.Load (external historical reference) forwards the frozen creature option's `FaceInspection` only in its creature branch. Frozen policy/options definitions remain unchanged.

Face inspection requires the complete albedo route, explicit supplied `BlueprintCustomizationKey/int` values and caller-selected source limits. No values are inferred from a player, packet or authored profile automatically. The default albedo-enabled route remains39 sources/4,686,371 bytes; the existing albedo-disabled route remains37 sources. Default source/decoded limits and existing call signatures remain source-compatible. This route addresses the [known head gap](SCENE_HUMAN_HEAD_MATERIAL_GAP.md) by owning generated face data, without changing any renderer.

When requested, the existing descriptor reader enables `loadFaceBlueprints`, `loadFaceResources` and `loadFacePixelPrograms`. The existing `BlueprintGeneratedFaceBuilder.Create` consumes the owned geometry/blueprints/resources, exact frozen policy values and cancellation token. Its result is assigned only to `GeneratedFaces`. Material palette/albedo selections, profile/geometry, selected/prepared idle and walk/run fields remain preserved. `CustomizationValues` stays null; global `SkeletalAppearanceCustomization.Apply` is not called. This is face-only inspection, not a recovered complete replicated appearance snapshot.

The builder enforces exact required key closure and namespace. Missing, extra, wrong-domain and invalid key syntax reject. Valid Int32 values retain existing [preparation fallback semantics](BLUEPRINT_FACE_COMPOSITION_CONTRACT.md): style texture indices clamp and out-of-range palette indices select the established fallback. Int32 minimum/maximum therefore succeed and remain recorded verbatim; no new range policy is invented.

## Budget, ownership and cancellation

Source count/encoded-byte limits come from the existing reader/router options and shared owned source cache. The preview selects128 sources/32MiB; the opted original result's union is counted mechanically. Every source is read once and remains owned after resolver arrays clear and readonly archives close.

`policy.MaximumDecodedBytes` bounds the logical sum of every mip in unique original paths across `MaterialBundle.Textures` and `FaceResources.Textures`, plus each generated renderer output once. Addition uses checked64-bit arithmetic. A private path set deduplicates the two existing maps; decoded pixels are not copied to measure. Before composition, declared generated dimensions are added to the owned-image sum and a known over-budget result rejects. Actual generated mip lengths are summed and checked afterward. Default no-face mode retains the32MiB decoded-material limit.

This is logical retained texture admission, not heap/peak-memory/GPU accounting. Different internal decodes can occupy separate storage for one source path. In this exact original closure the sole overlap is `texture/hum_m_face.dds`: distinct `DdsImage` references with identical format/color-space/alpha/DXGI metadata and all11 mip dimensions/pixel bytes. Count that logical path once. Runtime completion validation/accounting must allow equivalent cross-map aliases and reject contradictory metadata/pixels; reference inequality alone is not a contradiction. The reader obtains these two decodes from the same cached original bytes. No public descriptor accounting API is added here.

Cancellation checks apply before the first read, around resolver calls, after descriptor preparation, during image-budget traversal, before the builder, inside the existing compositor and before result construction. No result prefix is returned on an observed cancellation/failure; partial local descriptors become unreferenced. The reader owns no worker/scheduler/GPU/stream lifetime. The test resolver closes archives and clears borrowed bytes on both success and failure.

## Bounded original result

Native scenario (external historical reference), independent verifier (external historical reference) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R19_FACE_RESOURCES.md) establish **Confirmed modern fixture behavior**:

| Quantity | Opted original value |
| --- | ---: |
| Sources / encoded bytes | 76 /5,940,390 |
| Material / face-resource texture paths | 10 /32 |
| Unique original texture paths | 41 |
| Original-path decoded bytes | 17,203,152 |
| Generated output | one256×256 RGBA image /262,144 bytes |
| Total logical decoded budget | 17,465,296 bytes |

The binding is exactly `(appearance/mesh/hum_m_head_l0.mgn,shader index0,MAIN)` → `texturerenderer/hum_m_face.trt`, shader `shader/hum_m_face.sht`. Supplied zero-profile output SHA-256: `A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808`. This digest describes the existing explicit modern raster policy, not a legacy capture. Frozen face profile SHA-256: `C4F7CF06AB8C4083A2510556550C047DC0ADD5A5A0040829A81CBB047CCCBD9B`.

All non-face descriptor fields compare unchanged after substituting the expected common source union. Source/decoded exact limits succeed; one below each rejects. Twelve negatives cover key closure/domain/syntax, source count/bytes, decoded limits, direct incomplete-albedo request and pre-read/first-read/compositor cancellation. A bounded10ms cancellation scheduled after the known final read was observed throwing inside `BlueprintFaceCompositor`; the fixture checks the exception stack and no-result/closed-scope outcome. This is a current-toolchain stage probe, not a deterministic original timing claim. Original successful results remain unchanged after all failure/fallback tests; frozen input values survive mutation of the caller dictionary.

The verifier checks all76 source priority/length/SHA rows against the readonly effective overlay, all unique texture mip dimensions/lengths from original DDS headers and the complete logical sum. It reuses the established independent rectangular face-composition oracle for all five cumulative layers and raw MGN TRTS binding. Int32 extreme output hashes match prior accepted fallback fixtures. Fresh existing albedo native regression passes16 reader negatives and two initializer-accounting failures, with unchanged37/39-source modes. No new decoder, compositor, format or corpus sweep is introduced.

## Joined integration dependency

The snapshot's `SceneObjectResourceRequest.CopyOptions` in `SceneObjectResourceInitializer.cs` omits `FaceInspection` when reconstructing creature options. The lead acknowledged ownership of that shared fix; this assignment leaves it unchanged. Without that fix, world initialization drops the opt-in even though direct reader/router loads work. Requested-extension validation/logical accounting and GPU binding/capture belong to other reviewed integration work; these tests inspect returned resources directly and depend on neither same-round component.

The standalone producer prints compact `face_resource` JSON with source counts/bytes, sorted source identities, logical decoded bytes, aliases, projected binding rows, renderer output dimensions/bytes/mip0 hash and `profileSHA256`. Additional diagnostic rows support the existing independent oracle and remain private when captured. Exact commands are in the checkpoint. Lead's next action is to preserve the frozen policy through option copying, join requested-extension gates/accounting, compare this receipt, then verify the actual generated MAIN on the GPU. Original player complexion, replicated customization translation and legacy driver parity remain unknown.
