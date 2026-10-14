# Interior generated character —2026-10-01

## Question and stopping condition

Can the owned original generated face and albedo factors enter the existing interior character scene while retaining single-read asset ownership, geometry, animation, room transfers and cleanup? Stop at independent original byte/field/pixel agreement and offline Godot route/GPU/cancellation proofs.

## Input identity and method

Inputs are the unchanged configured installed overlay: configuration SHA25622158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524; pinned mining-outpost POB79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818; authored scene specification12BF5B21EC0D7BDA15CB322BB14FF5FC4C7C640B81E7C1DB38CA11F66D9349CA; human objectE0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243 and SAT951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C. [Prior integration](IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_ANIMATION.md), [face](IMPLEMENTATION_CHECKPOINT_2026-10-01_GENERATED_FACE_PREVIEW.md) and [albedo](IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_ALBEDO_FACTORS.md) establish the component contracts. No executable address claims added; executable/DLL hashes remain the verified session baseline. Only the replacement proof was executed, offline.

## Changes and observations

`InteriorSceneContentBundleReader` accepts explicit face values and an albedo-factor option, validates required stages before lookup, loads all dependencies through the existing scene cache and composes the face before publishing the character. Cancellation checks guard source lookup, composition and final delivery. The character's nested source budget is128/32MiB when generating a face; the outer scene keeps its existing256/128MiB bound.

The Godot worker owns profile parsing, original loading and CPU composition. GPU publication remains on the main thread after the archive scope closes. Existing per-mesh generated MAIN bindings and sampled albedo shaders are reused; rebuilt animation surfaces retain the same materials. Launcher paths are resolved before process launch; the first attempt exposed and corrected a relative-profile path failure.

Confirmed original result:139 unique sources/15,541,123 bytes, one generated head MAIN binding, four albedo descriptions/two original programs,32 face DDS/251 declared mips/three face palettes. The face output agrees with the independent rectangular/Pillow/NumPy reference under the explicit modern composition policy. Both character meshes total2,810 triangles. The original three-room guard retains42,986 triangles.

Native route:2,880 ticks,24 transfers,182 blocked contacts; every recorded locator and reflected-owner position independently agrees with floor bytes. This does not independently reimplement the dynamic door controller. Godot proves guarded closing behavior, denied entry/retreat/reset, physical keyboard/mouse routes, transformed owners, timestamped transfers/stall/pause/focus, two idle cycles and retained byte/material identities. GPU filtered albedo fixtures agree within0.001545 normalized error; tolerance remains2/255. Capture visually inspected: colored animated character with generated face inside the original interior.

## Reproduction and validation

Native command: `dotnet run --no-restore --project poc/Stardust.Core.SmokeTests -- --original-interior-generated-character <archive-root> --configuration <cfg> --scene-spec <spec> --expected-sha256 <spec-pin>`. Run from repository root; explicit authored values come from [human face profile](../../../SWG-Godot-Client/poc/preview-profiles/human-face-zero.json).

Independent command: `python tools/python/verify_original_interior_sphere.py <archive-root> <cfg> <native-output> --configuration-sha256 <configuration-pin>`. It reuses the raw face resource/preparation/program/composition/binding and albedo auditors, checks all source identities/counts and single external reads, and retains the room/character/floor audits.

Godot launcher adds `-SceneCharacterMaterialDefaults -SceneCharacterPaletteDefaults -SceneCharacterFaceProfile <profile> -SceneCharacterAlbedoFactors` to the prior pinned animated interior command. Headless and GPU checks pass. Strict option/canceled-before-lookup gates are included in the portable suite. Worker cancellation at first lookup must close the archive and publish zero scene/GPU objects. Private native/reference/log/capture outputs remain ignored.

## Confidence and implementation consequence

**Confirmed** component/source ownership, original field/pixel agreement and observed offline host behavior. **Explicit prototype policy**: CPU raster/sample rounding, RGBA8 destination, normal-opacity albedo display and modern triangle guard. Exact original graphics-driver behavior, lighting/specular and capability negotiation remain unknown/unimplemented. Research89%/94%, names and protocol metrics unchanged.

Face profile values and body/eye authored defaults remain separate. Next probe: reconcile factory-flag variable identity and build one explicit customization selection driving both material colors and face preparation; verify nonzero skin/eye/style profiles without changing original bytes or scene state.
