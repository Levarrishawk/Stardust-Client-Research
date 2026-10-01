# Precise persistent door movement

## Bounded behavior

[PortalDoorBoxFloorWalker](../poc/Stardust.Core/Assets/PortalDoorBoxFloorWalker.cs) adapts the [precise portal box query](PORTAL_FLOOR_BOX_CONTRACT.md) to directional permission barriers and borrowed physical leaf controllers. `PortalDoorActorSimulation(usePreciseMovement: true)` selects it; the existing level adapter remains the default.

Observation: source and target triangle-plane paths determine actor height before collision. Interpretation: conservative actor boxes can now traverse a connected sloped doorway without replacing the path by its endpoint chord. Confidence: Confirmed deterministic fixtures and original geometry comparison. Consequence: retain physical collision and floor ownership on both sides of a seam. This remains one transfer per request, with no sliding, gravity or general world obstacles.

All relevant physical leaves are captured before external permission callbacks. Both incident references reuse one snapshot of a shared controller. Each directional floor link has one cached permission decision per request. The target reverse barrier is excluded when it governs entry back into the source; it cannot deny entry into the target. Invalid starts and collider-budget overflow are resolved before callbacks. Controllers remain borrowed and are never advanced by the adapter.

## Pinned original corridor

The selected YT-2400 floor corridor is cell 1/portal 0 ↔ cell 2/portal 0. Inputs are effective descending-priority archives from `C:\Stardust-DEV\swgemu_live.cfg`, configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

| Input | SHA-256 |
|---|---|
| `appearance/yt2400_ground.pob` | `53C48649DE618C2EB2C240C229E8CD860BAD684BF7A5938E3B258E3329EC5B16` |
| `appearance/yt2400_door.apt` | `1AA816E4162BC5C39B5D929794BAE30290441171CD09499C485A14FA10F7B84C` |
| Door table | `C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7` |

The forward 1.6 m lane gains approximately 0.124684 m in height. [Independent verification](../tools/python/verify_persistent_door_actor.py) with `--precise` uses [original triangle-edge/plane geometry](../tools/python/precise_door_actor_reference.py), float ABI portal handoff, rotated box intervals and bounded contact output checks. It consumes neither native traces nor native parsers. The selected continuous floor sheet is a prerequisite; nearest-height selection is not proof of navigation across arbitrary stacked floors.

Two owner poses per starting direction yield **1,896 ticks, 88 transfers and 53 blocked ticks**: 948/44/27 starting in cell 1 and 948/44/26 starting in cell 2. Exact status, contact kind, owner and transfer counts match; positions and parameters retain the prior small numerical comparison budgets. Denied/unknown entry, retreat, recovery, repeated round trips and reset are included. The unchanged level cantina reference passes 948/44/34.

## Installed screen and limitations

The same 782 authored candidates yield 647 paired selections and 533 instantiated simulations. With precise movement, **495** complete the bounded exact short round trip, compared with the historical level adapter's **131**. Remaining instantiated outcomes are 37 Accepted without exact return and one LandingFailed. Construction exclusions remain 135 unpaired, 82 missing source approach hits, 31 missing target approach hits and one missing leaf source floor link. These denominators describe different gates and must remain separate.

The 31,980-tick screen has zero InvalidStart and zero UnsupportedNonlevel outcomes after the [returned-height correction](FLOOR_CONTACT_STABILITY_CONTRACT.md). Original paired-controller ordering, moving-leaf collision, narrow-phase shape, disconnected stacked sheets and full locomotion remain unresolved. No Godot precise-path acceptance is implied by the native evidence.

## Godot host acceptance

[Explicit host option](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PRECISE_DOOR_GODOT.md): launcher -DoorWalk -PreciseDoorWalk selects the verified engine-neutral path. Pinned YT-2400 forward-start headless and reverse-start GPU controls pass repeated ownership, retreat/reset, reflected placement, fixed-step/input and diagonal owner round trips. Private GPU capture was inspected. Level mode remains the default.

## Thin target landing correction

[September 30 follow-up](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LANDING_NORMAL.md) closes the Yavin reverse LandingFailed within existing geometric budgets. Its independent persistent trace passes 948/44/64. The current same-input screen now completes 496 exact short round trips, leaving 37 Accepted non-round-trips and zero LandingFailed. Earlier 495/one-failure counts above describe the preceding gate.
