# 2026-09-30 connected doorway approaches

## Question and inputs

Can a modern fixture approach follow the selected paired floor sheet without a highest vertical cast choosing a disconnected upper floor? Close after synthetic stacked-sheet/obstructed-route gates, independent original endpoint checks and repeated lower-floor doorway traces in both directions.

Inputs: documented executable SHA-256 baseline unchanged; configuration `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 descending TREE mounts; candidate JSON `D80CB2CA9034EB33729078150F1DB32A5ADD09179C803B5F32B81FE233FA3423`, 782 authored selections. Black-sun POB `6CFDFE347FC94F38BC03D8FD440D2F4E8C63FF46532F33A9D730DF371658EA1F`, primary APT `AB41F2335A3F0CD65FDF28818AFA187CDEECFF3E9728358E85E3374113ABB2B7`, selected 2/0 ↔ 3/0. Table pin is documented in the [precise actor contract](../../docs/PRECISE_DOOR_ACTOR_CONTRACT.md).

Tools: native core smoke, [screen producer](../../../SWG-Godot-Client/tools/python/probe_door_instance_capabilities.py), [independent approach reference](../../../SWG-Godot-Client/tools/python/connected_door_approach_reference.py), [persistent verifier](../../../SWG-Godot-Client/tools/python/verify_persistent_door_actor.py). Proprietary-derived records remain ignored/private.

## Observations and conclusions

Synthetic stacked floors show the old cast selecting Y=2 and the connected approach preserving selected Y=0; malformed distance and blocked route reject. A first corpus run exposed multiple paired edges on some selected portals; the explicit nearest authored-door midpoint/tie rule resolves selection deterministically.

Screen: 647 independently paired selections, 5,150 source identity checks, **1,056 independently checked endpoints**, **528 constructed approaches / 31,680 ticks / 524 exact short round trips**. Failures: 135 unpaired, 116 UnsupportedSurface approach, two UnrepresentableContact approach, one InvalidTopology approach; four constructed cases stop at leaf contact. The prior vertical local screen stays 533/509; connected selection gains 17 passing keys and loses two.

Black-sun 2/0 initial is on lower floor near Y=-3.05143, not the disconnected upper floor near Y=1.95. Both starting directions/two poses independently match **1,896 ticks / 88 transfers / 182 step events / 56 blocked ticks** (90/92 steps by direction). Independent geometry consumes original planes/edge cuts and reciprocal vertical chords, not native traces. Continuous-sheet oracle limits remain explicit.

Confidence: Confirmed modern fixture construction and bounded original actor trace. Original spawn/navigation policy remains Unknown. No new executable names or protocol rows; weighted research scores remain unchanged.

## Compatibility and next probe

See the [durable contract](../../docs/CONNECTED_DOOR_APPROACH_CONTRACT.md). Preserve option separation and reject obstructed construction. Add explicit host approach selection, then verify lower-floor controls and reflected placement. Investigate any host input failures rather than weakening ownership/collision checks.
