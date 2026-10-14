# Detail-door instance readiness and verified vertical leaf

## Screening boundary

[Instance screening](../../SWG-Godot-Client/tools/python/probe_door_instance_capabilities.py) consumes the ignored capability inventory's 782 directed DTLA candidates. It pins the exact candidate JSON, independently decodes floor-edge pairing and checks 5,150 native source identities. The [native fixture](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/DoorInstanceCapabilityScenarios.cs) waits 20 × 0.05 seconds, moves 20 × 0.08 metres forward, then 20 reverse steps. Passing requires one transfer each way, source ownership restored and return position within 1e-4. This is bounded fixture screening, not general locomotion or whole-instance coverage.

| Screening result at this checkpoint | Directed candidates |
|---|---:|
| Authored candidates | 782 |
| Independently uniquely paired floor candidates | 647 |
| Native actor instantiated | 533 |
| Exact short round trip passed | 131 |

Passing fixtures span 28 layouts and 19 styles. The 533 actors execute 31,980 native fixture ticks. Before simulation, 135 references lack a uniquely paired floor edge, 82 lack the source inspection floor hit, 31 lack the target hit, and one lacks a source leaf floor link. Among instantiated nonpasses, 283 finish Accepted without satisfying the complete round-trip criterion, 113 retain explicit unsupported-nonlevel stops, and six finish InvalidStart. These categories must not be combined with the 91 asset/controller rows or 1,497 authored style occurrences.

Observation: a matching reverse door name/transform and declared floors do not establish traversable floor geometry. Yacht candidates include missing pairs and elevator/slope cases; strict level-only movement correctly holds there. One independently matched source edge has two matching target edges and is therefore ambiguous; the native loader rejects it. Confidence: Confirmed byte/fixture results. Consequence: retain exact pairing and explicit unsupported behavior instead of accepting a named candidate alone.

## Verified Mustafar pair

Selected member `appearance/thm_must_mining_outpost.pob`, SHA-256 `79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818`: cell 5 `entrance_room_01`, reference 1 ↔ cell 6 `connecting_hall`, reference 0. Style `thm_must_mining_door` uses primary `appearance/thm_must_mining_door.apt`, SHA-256 `99C8ED2EEE49BF44DB9CCDFBA4436A2A56949389D3AB79306A1196493481A3D3`. Its two-band DTLA root hash is `C016369D967F4C4A7846025BCD71F52F1E48A0D0552C924E1CAAB8DE49E3DCF7`.

The style moves (0,3.75,0), opens/closes in 0.25 seconds, uses springiness 1 and smoothness 0.5, and trigger radius 3. This verifies an upward-moving primary with a different displacement blend and timing from the earlier lateral cantina door. Source and target floor hashes are `DD1ACA860D6F04A3A5D07C9D96FDDD640117EF968DB78714E5B06AAB204B0F54` and `42D9ADF10068796F6113D9371BBCDCF6DEF17B4027D96BF5063F61C39814EFEA`. The existing configuration/table pins remain in the [capability matrix](DOOR_STYLE_CAPABILITY_MATRIX.md).

[Generic independent instance selection](../../SWG-Godot-Client/tools/python/doorway_reference.py) decodes the root extent, bands, reverse reference, paired floor edge, approach and horizontal portal plane. Initial inspection uses the nearest downward segment hit, avoiding a false lowest-triangle choice in stacked floors. Actor projection in this oracle is explicitly bounded to the selected level lane; it does not independently implement arbitrary adjacency, slopes or obstacle walking.

The [persistent verifier](../../SWG-Godot-Client/tools/python/verify_persistent_door_actor.py) now accepts all five explicit generic instance options. Each starting direction matches 948 ticks over two owner poses, 44 transfers and 28 blocked ticks. Combined: **1,896 ticks, 88 transfers, 56 blocked ticks**. Pin checks cover eight effective sources. Headless/GPU persistent controls, timed input, normalized diagonal round trips and reflected placement pass; private capture inspected. Visual LOD changes retain the same authored physical envelope.

## Resolved contact risk and next work

The six InvalidStart fixtures exposed a modern rounding problem, now resolved by the [bounded contact policy](FLOOR_CONTACT_STABILITY_CONTRACT.md). The dated screening counts above preserve the initial result; current screening has zero InvalidStart cases and 289 Accepted nonpasses. Next: piecewise floor paths for slope-aware collision. World actor bounds and moving-leaf collision remain separate.

## Precise adapter follow-up, September 30 evening

The [precise actor screen](PRECISE_DOOR_ACTOR_CONTRACT.md) uses the same 782 authored candidates and 533 instantiated simulations. It completes 495 exact short round trips; 37 end Accepted without exact return and one ends LandingFailed. Zero InvalidStart or UnsupportedNonlevel outcomes remain after bounded returned-height repair. Historical level-only counts above retain their original scope. Short round-trip success is not full doorway or locomotion acceptance.

## Target bridge follow-up

The [thin target landing correction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LANDING_NORMAL.md) raises the same precise screen to 496 exact short round trips; 37 remain Accepted without exact return. LandingFailed and InvalidStart are zero. Historical screen counts above remain dated evidence.

## First forward boundary audit

[Independent remaining-boundary audit](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DOOR_BOUNDARY_AUDIT.md) categorizes all 37 non-round-trips: 19 solid near-vertical neighbors, six unloaded other target floors, four physical leaf contacts, one precision hold, six blocked edges and one accepted approach without transfer. Named other floors are present in layout references; MissingTargetFloor here describes two-cell fixture scope. Vertical triangles remain explicitly unsupported by the height-field walker pending a bounded step policy.

## Explicit step-enabled screen

[World step/portal gate](PORTAL_FLOOR_STEP_CONTRACT.md) yields 504 exact short round trips on the same 782 authored / 533 instantiated selections. Eight small-house cases now pass; 29 Accepted non-round-trips remain. Level, precise and precise-step modes have separate denominators and declared policies.

## Explicit local-step screen

[Local doorway gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_ACTOR.md): 509 exact short round trips on the unchanged 782 authored / 533 instantiated cohort. Remaining 24 first boundaries: seven unloaded other floor targets, four leaf contacts, five vertical neighbors, one precision hold, six blocked edges and one no-stop/no-transfer approach. Newly passing black-sun original trace independently verified.

## 2026-09-30 connected approach

Connected fixture screen: 782 authored, 647 paired, 528 constructed, 524 exact round trips, four leaf holds. 119 paired approaches reject before simulation. Seventeen gained and two lost keys relative to vertical inspection; no single coverage denominator. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_APPROACH.md).
