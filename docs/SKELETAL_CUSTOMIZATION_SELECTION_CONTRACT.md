# Explicit skeletal color/style customization

## Identity evidence

The face factory Boolean has a confirmed runtime namespace mapping. BTRT renderer registration00872C90 queries template variable name/flag via vtable+10/+14. At00872CE2..CEB, true selects string object01924C80 and false01924C70. Initializers00872A00/00872B10 construct those objects from0188F570 `/private/` and0186B880 `/shared_owner/`. Update00873190 repeats the same branch at008731E2..EB, concatenates the name and calls customization lookup00B32850, then obtains the integer through variable vtable+1C and updates the renderer value vector. Registration skips already-existing names and creates missing variables via template+18 before00B327A0.

CSHD instance lookup007859B0 independently chooses those same namespaces from its normalized Boolean. [CSHD contract](CUSTOMIZABLE_SHADER_DECODER_CONTRACT.md). Thus `(VariableName,FactoryFlag)` and `(VariableName,Private)` share runtime identity: true/private, false/shared_owner. Exact case and both domains remain distinct; historical editor labels are unnecessary. This closes the previously unknown runtime domain relationship. Missing face-variable update preserves the prior slot and can diagnose; complete registration/conflict/update policy remains outside this explicit snapshot slice.

## Core boundary

`SkeletalAppearanceCustomization.Apply` consumes an owned creature bundle and an explicit integer snapshot keyed by `ShaderCustomizationKey`. It requires geometry, face resources/programs and material palette/albedo descriptions. The required key set is exactly the union of face operations and material palette controls. Missing/extra/wrong-domain keys reject; values are owned. Texture-choice controls and skeletal shape morphs remain separate and are not accepted by this color/style API.

Selected CSHD palette factors and diagnostics are recomputed from owned palettes. Their existing `AuthoredFactors` storage field now carries the selected query result, whose `UsedAuthoredDefault=false` distinguishes explicit values; authored defaults remain unchanged in the immutable shader description. Validated program/pass identities are preserved while albedo colors update. Face-required keys are projected to their name/factory pairs and composed over the same owned assets. The input bundle, source identities and original texture pixels remain unchanged. Result values, selected factors, albedo descriptions and generated outputs are owned.

Fallbacks remain separate: invalid CSHD palette index→first entry; invalid BTRT palette index→last; style textures clamp to endpoints. Missing palettes retain the existing explicit diagnostic fallback. Cancellation applies before selection, each palette and composition. No filesystem/archive/engine dependency is introduced.

## Preview profiles and host

The strict existing version1 profile schema is reused: `variableName`, `factoryFlag`, int32 `value`. The confirmed mapping makes factoryFlag the normalized private/shared-owner discriminator for both consumers. [Zero profile](../../SWG-Godot-Client/poc/preview-profiles/human-customization-zero.json) and [varied profile](../../SWG-Godot-Client/poc/preview-profiles/human-customization-varied.json) are independently authored inspection selections, not recovered character-creation defaults.

The human chain requires eight exact keys. Shared-owner skin updates body/head and base face; private skin remains a distinct face highlight. Private eye/index_color_2 and white/index_color_3 feed mouth/body factor controls. Three styles and facial-hair color feed the generated face. Age/normal-map texture selection, shape morphs, lighting and specular remain unexecuted.

`-CreatureCustomizationProfile` and `-SceneCharacterCustomizationProfile` require albedo factors and replace the face-only profile option. Profile parsing and pure selection/composition happen in the owned background worker; GPU publication occurs on the main thread. [Live revisions](LIVE_SKELETAL_CUSTOMIZATION_CONTRACT.md) now permit interactive edits with bounded worker ordering and retained main-thread publication.

## Verification

Native `--skeletal-customization` appends `--customization-profile <path>` to the prior pinned object/SAT arguments. Independent descriptor audit validates exact eight-key union, selected CSHD/RIFF colors, face operations/pixels/TRTS binding, original program semantics and unchanged74-source/5,927,038-byte union. Zero, varied andINT_MAX profiles agree. Zero output preservesA41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808; varied output60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5.

Interior `--original-interior-customized-character` similarly appends a profile. Independent audit preserves139 sources/15,541,123 bytes,42,986 guard/2,810 character triangles and2,880 route ticks/24 transfers/182 contacts. Portable shared-color/ownership/fallback/missing/extra/domain/cancellation gates pass. Standalone/interior GPU selected-color/face byte/material identity and animation/control proofs pass; captures inspected. Both first-lookup worker cancellation modes close archives with zero publication. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SHARED_CUSTOMIZATION.md).
