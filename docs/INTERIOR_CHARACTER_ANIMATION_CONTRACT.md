# Selected original animation in the persistent interior

`InteriorSceneContentBundleReader` optionally accepts an explicit character table/name/clip pin, requiring the existing pinned actor/character geometry. The same outer owned cache supplies object templates, SAT/MLOD/skeletons/MGN, both LATs and selected clip, alongside room/door/material sources. CPU preparation and archive scope closure precede main-thread delivery. Missing/masked/wrong identities reject; pending-first-lookup cancellation publishes neither character nor scene/GPU resources.

The prototype host interior character uses the same prepared sampler/local composition/hierarchy/full-weight upload as standalone playback. Its root node follows authoritative actor feet in world axes, as declared by the prior neutral character adapter. Sampled root-joint channels may change the mesh pose locally; no extracted root motion moves the actor or updates collision/doors. No locomotion-state choice is inferred: the selected idle clip can continue while the user moves the actor.

The preview clock starts looping; Play original idle pauses it independently. Scene pause/focus hold its frame, and actor/building reset seeks frame zero. First-person hides the visual; overview shows it. Host elapsed stalls beyond0.25 seconds are discarded by the modern presentation policy. Existing owner transforms, actor sphere, room triangles, door permissions, occupancy guards and first-person inputs stay authoritative.

## Verified sample

Original shared human ambient idle adds exactly three original sources to the previous77-source union: two LATs and one CKAT. Total80 sources/10,340,219 bytes, each externally read once. Independent source audit verifies every priority/length/SHA, body/face logical counts, exact selected proxy/clip relationship and unused `rifle`/missing `hold_l`. Existing original2,880 ticks,24 transfers,182 contacts and all42,986 room triangles remain passing. Character2,810 triangles unchanged.

Headless/GPU host fixtures apply one complete selected cycle in each of two building owner poses, periodically read back all surfaces and assert the scene state and character root transform do not change during animation. Play-pause/focus controls hold, bind/actor reset and existing route/closing/input gates pass. GPU capture inspected: original idle pose rendered inside the original textured interior, aligned attached head/body under neutral character materials. Cancellation at pending first lookup passes with zero scene/GPU publication.

## Reproduction

Add `-SceneCharacterAnimationTable appearance/lat/all_m.lat`, `-SceneCharacterAnimationName emt_standing_idle_ambient_1_ag` and `-SceneCharacterAnimationSha256 798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B` to the existing profile/room/character-pinned interior launcher. All three require SceneCharacter. Native `--original-interior-animated-character` uses the existing original sphere-scene specification arguments. `verify_original_interior_sphere.py` automatically audits animation rows and80-source union.

[Clock policy](ANIMATION_PREVIEW_PLAYBACK_CONTRACT.md), [prior interior adapter](NEUTRAL_CHARACTER_PREVIEW_CONTRACT.md), [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_ANIMATION.md). Full animation graphs, locomotion choice, facial animation, character materials/customization and gameplay root motion remain pending.


## Character material defaults

`-SceneCharacterMaterialDefaults` requires owned scene character geometry and uses the same original authored CSHD texture-only policy as the standalone preview. The selected idle plus material scene union is97 sources/14,282,453 bytes, each externally resolved once. Four materials/ten textures/80 mips agree independently. Headless/GPU/cancel and original route gates pass; capture inspected. Palette shading/generated face remain unexecuted. See the [material checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_MATERIALS.md).
