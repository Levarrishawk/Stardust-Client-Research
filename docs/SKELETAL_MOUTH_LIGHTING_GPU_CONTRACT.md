# Mouth multi-light vertex-color GPU contract

R2-RENDER-01 verifies the existing replacement mouth shader against [the source-shaped modern CPU reference](SKELETAL_VERTEX_LIT_SOURCE_CONTRACT.md). Original generated vertex bytecode, LIT/power300 range and device precision remain **Unknown**. This adds a bounded diagnostic, not a production equation change.

## Observed adapter and proof boundary

`SkeletalVertexLitSpecularPreview.Code` already calculates cell lighting and adds emissive at vertices. `CellLightingGpuPreview.Apply(...,includePrimary:true)` publishes the selected primary directional plus additional recovered slots. There was no missing multi-light feature to implement. The new `SkeletalMouthLightingGpuVerification.VerifyAsync` derives a diagnostic shader from that existing code; it only adds fragment readback modes for the two raw color varyings. The vertex code and production pixel/hue/SPEC equations remain unchanged.

A one-line hook in `SkeletalSpecularPreview.VerifyGpuBoundary` runs this diagnostic after the ordinary mouth fixtures when vertexLit is true and cellLighting/legacyHdr/staticDoor are false. Existing standalone and interior specular verification routes reach it. Headless/interactive routes retain their existing execution policy. No shared Main/material/project/runner files were edited.

**Observation/confidence:** Confirmed existing owned code path and bounded GPU agreement. **Interpretation:** the replacement produces source-shaped mouth color staging under the explicitly modern literal-power policy. **Compatibility consequence:** do not claim this reproduces original compiled HLSL LIT or a hardware instruction sequence.

## Fixture and independent reference

The independently authored fixture uses one triangle with three unequal depths and differing normalized normals, a constant MAIN texel RGBA(160,112,64,192), SPEC alpha96 and two nonwhite hue factors. The MAIN material has nonwhite specular RGB(0.45,0.25,0.1), emissive RGB(0.025,0.035,0.02) and deliberately unused nonwhite ambient/diffuse material values. Scene ambient is RGB(0.045,0.065,0.055).

Three directional and five positive-distance point inputs exercise the recovered1+2 directional/1+4 point slots. All supplied lights have nonzero authored specular, but recovered diffuse-only slots publish zero specular. Point coefficients are(0.6,0.2,0.05), with different positions and uncapped positive coefficient attenuation. The primary direction is normalize(0.15,0.1,1). Its synthetic transform stores minus that vector in RowX.Z/RowY.Z/RowZ.Z: `TransformDirection(UnitZ)` reads the third column. An explicit resolved-direction/slot-count gate catches the former row/column fixture error.

Each vertex's world position/normal/viewer is supplied to `SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower`. The CPU reference is the independently audited R1 helper; the GPU test does not copy the shader loop into another oracle. Screen barycentric weights come from independently projected vertex positions at pixel centers. Perspective interpolation divides each barycentric weight by its camera depth/clip-W and normalizes the three results, then interpolates colors. A hand triangle has screen weights(1/2,1/4,1/4), clip-W(1,2,4), and expected normalized perspective weights(8/11,2/11,1/11). Outside samples, zero/nonfinite clip-W and degenerate triangles reject.

The cases cover both normal signs, identity and rotated/translated owners, orthographic/perspective cameras, powers2/20/300 and three interior pixel locations. All geometry/light/view inputs cross the existing Godot Z reflection. The transformed core owner is rigid and right-handed; arbitrary mirrored/nonrigid core owners are not accepted or claimed. Three output modes compare raw COLOR0 diffuse, raw COLOR1 specular and the combined sampled MAIN/hue/SPEC result. The raw fixture outputs stay in displayable range; combined pixels apply the existing later COLOR saturation. Power300 combined cases also enable the selected two-color family's bloom option, preserving its unscaled RGB specular behavior.

## Results and discriminators

216 comparisons pass:72 diffuse,72 specular and72 combined pixels. Maximum display-channel error is0.003438, below the unchanged2/255 tolerance. The existing compatibility-renderer display transfer/quantization oracle is applied consistently. These are display readbacks, not raw floating-point framebuffer identities.

| Incorrect policy | Maximum display-channel difference |
|---|---:|
| Omit the primary directional | 0.294286 |
| Omit diffuse-only additional slots | 0.179544 |
| Let diffuse-only slots contribute specular | 0.036496 |
| Omit material emissive | 0.040608 |
| Recompute lighting after color interpolation | 0.208822 |

Each discriminator must exceed4/255, giving margin beyond the2/255 acceptance threshold. Back-facing cases preserve ambient/emissive diffuse with zero specular. The combined oracle reuses the independently verified mouth pixel evaluator rather than translating the fragment shader mechanically.

`CellLightingGpuPreview.Apply` verifies uploaded array identities/counts and source-attenuation flags. Each temporary mesh is detached in finally; the viewport is freed in finally even when a comparison rejects, and its invalid Godot object state is checked afterward. Textures/images/shader/material/meshes use owned disposal scopes. The original scene cancellation fixture separately proves pending first-lookup archive cleanup and zero scene/GPU publication.

## Reproduction and retained original validation

Baseline is HEAD65de8c88141071744dc75594be126b3bab3696a5 plus the accepted1735-file Round2 manifest SHA256 E2AFB787D6CDB9F8C1971183D5A63CDC2B4736E24EC41E039F94FEEAF058D097; ZIP SHA256 CEBF4BD8017249ECC1FC9A9647900BD4062C134CA10040D5B2420C04A3CC4981. Every ZIP path/byte count/hash was checked before overlay, and every resulting file hash was checked afterward. Ignored/unlisted files were preserved.

Build the worker checkout offline using a task-specific APPDATA, the accepted local NuGet cache and a task-local NuGet.Config with cleared sources:

```powershell
dotnet restore poc/Stardust.Godot/Stardust.Godot.csproj --configfile <offline-config> -p:NuGetAudit=false
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
```

The accepted package cache is required: the separately bundled engine NUPKGs have different content hashes from the pinned project lock. Restore from the accepted cache passes without changing locks. The worker used its own ignored copy of that cache. Both Core and Godot builds pass with zero warnings/errors. No SDK/project/lock files changed.

Native source/math proof was rerun with the unchanged R1 Rendering console, explicit `C:/Stardust-DEV` and `C:/Stardust-DEV/swgemu_live.cfg`, then `tools/python/verify_skeletal_vertex_lit_sources.py <root> <configuration> <native-log>`. All original four-node source/binding and malformed/ownership gates plus64 independent modern CPU cases pass at maximum3.4835412372480334e-6, tolerance1e-5. Configuration SHA256 is22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524.

The existing `tools/powershell/Invoke-StardustAssetPreview.ps1` runs only the replacement engine. Use the pinned original full interior configuration summarized in the [Round1 integration record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_1_2026-10-01.md): mining outpost layout79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818, scene specification12BF5B21EC0D7BDA15CB322BB14FF5FC4C7C640B81E7C1DB38CA11F66D9349CA, original human object/SAT and idle pins, room geometry/textures/triangle guard, portal visibility, held closing doors, character materials/palette/albedo/normal/specular, varied customization/age, zero shapes, locomotion and camera profile01A19A5B18E81C9366B86B5F313C31D8F0C1961F8230DB4535AA5C50E95C543D, cell/room/door lighting. Choose a fresh ignored CapturePath, then repeat with Headless and with Headless+CancelSceneLoad. Exact worker invocations and logs remain ignored evidence.

Fresh standalone and full interior GPU runs pass existing mouth/normal/tangent/room/door/cell/HDR fixtures and retained route/material/pose/control checks. The final interior remains160 sources/16643480 bytes. Headless original route/animation/camera checks and first-lookup cancellation pass. Fresh capture inspected: retained character, original room/door and chase controls render. Engine startup reports the pre-existing root-certificate-store error; offline assertions continue and pass. No certificate/network setup changed.

The bounded renderer tested was Godot4.7.2 .NET/OpenGL compatibility on NVIDIA RTX4070 SUPER. Vulkan/other GPUs and raw float outputs were not run. No original executable, socket/DNS/server or packet capture was used. Hardware precision, original capability branch, live visibility/registration order and original normalize-zero behavior remain Unknown. Research/function/protocol scores are unchanged.

## Lead handoff

Only the new GPU class, the one-line verification hook, this contract and the unique checkpoint differ from the Round2 manifest. The production mouth shader file is unchanged. Lead should integrate these four files, retain the existing hook or register `VerifyAsync` once in a GPU verification route, reconcile acceptance/status/navigation and rerun the combined review gates. The worker stops here; any next GPU scope requires the next assigned milestone.
