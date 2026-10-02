# Textured animated interior character — October1,2026

## Question and stopping condition

Can the original character material closure share the existing interior content owner while preserving actor state, door state and cancellation? Stop at original source/mip/route audit and inspected GPU scene.

## Inputs and observations

The [animated interior checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_ANIMATION.md) supplies unchanged pinned configuration, scene specification, POB, door table, inherited actor template/SAT, MGN/LAT and idle clip. Four original CSHD shaders and their effect/DDS dependencies use the [skeletal texture-default contract](../../docs/SKELETAL_MATERIAL_DEFAULTS_CONTRACT.md). Native `--original-interior-textured-character`; Godot opt-in `-SceneCharacterMaterialDefaults` requires `-SceneCharacter`. Original bytes are read through the effective overlay with no asset mutation, original executable execution or server contact.

Shared content source union grows from80 to97 unique sources and10,340,219 to14,282,453 bytes. Every source resolves externally once; template/SAT/geometry dependencies are shared. The character's decoded pixels have their own bounded128MiB cache; scene pixels retain their existing separate budget. This is not one shared aggregate GPU/decode cache across room and character materials.

## Verification and confidence

Independent original-source SHA/priority/size audit passes97 sources, four material descriptors, ten textures and80 mip hashes. Original route remains2,880 ticks/24 transfers/182 contacts;42,986 room guard triangles and2,810 character triangles match independently. Native full suite and Godot build pass. Headless, GPU and cancellation fixtures pass. GPU retains original texture/material resources through two selected clip cycles and owner transforms; full vertex/direction/UV/index and texture/mip readbacks pass. Actor feet and doors remain unchanged during visual pose updates. Capture inspected: authored pale human texture detail appears in the original textured interior.

Confirmed prototype boundary and independent byte agreement. Original runtime effect selection, palette shading and generated face execution remain unclaimed. Cancellation closes worker archives and publishes no scene/GPU resources. Invalid character material request without owned geometry rejects before lookup.

## Compatibility consequence and next action

Visual texture/animation state now travels with the owned scene character rather than requiring detached extracted assets. Original root/bone animation affects visual mesh only; the scene's actor feet remain authoritative. Palette colors, secondary normal/specular shading, generated face compositing, equipment, morph/occlusion, animation graphs and root-motion integration remain open. Next: verify preview clock fractional loop boundaries across non120-frame clips, then select a bounded next material/animation risk from original evidence. Research scores and registries unchanged.
