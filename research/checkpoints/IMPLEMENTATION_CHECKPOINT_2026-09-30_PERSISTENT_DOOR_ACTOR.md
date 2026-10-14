# 2026-09-30 checkpoint: persistent actor through the original doorway

## Bounded question

Can actual actor ownership/position persist over repeated forward and reverse steps through the selected original door, with one modern shared leaf, current contact/permission, blocked retreat and deterministic cleanup? Stop after independent native traces and Godot controls pass. Continuous render/input scheduling remains next.

## Inputs and evidence

Rechecked executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. No new original execution or VA semantic naming. Archive root/configuration and six effective POB/table/APT/MESH/floor pins are the same as the [instance checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md). Configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` across 48 TREE mounts. Exact original reverse reference pose/style equality is verified from POB bytes.

Observation: eligible references cell 3 / portal 10 and cell 12 / portal 0 describe the same authored pose/style and paired floor boundary. Interpretation: they can share one modern physical leaf in this bounded replacement. Confidence: original bytes Confirmed; legacy paired-controller synchronization Unknown. Implementation consequence: validate both incident references, preserve direction-specific permission ownership, and avoid claiming that modern shared-controller internals reproduce original memory/lifecycle.

The [durable contract](../../docs/PERSISTENT_DOOR_ACTOR_CONTRACT.md) records tick order, normalization, borrowed-versus-owned state, separating-face policy and deferred fidelity. The new [simulation](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/PortalDoorActorSimulation.cs) owns actor/leaf lifetime. The existing runtime accepts an optional declared incident cell; the walker can bind both directions and opt into outward retreat. Genuine overlap and unsupported floor height/landing retain explicit stops.

## Verification

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
dotnet build --no-restore poc/Stardust.Godot
python tools/python/verify_persistent_door_actor.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg local-private/generated-evidence/persistent-door-milestone poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

[Independent verifier](../../../SWG-Godot-Client/tools/python/verify_persistent_door_actor.py): 948 ticks over two owner poses, 44 transfers, 34 blocked ticks, zero discrepancies. It independently decodes all six effective inputs and derives actor trajectory, portal side, current contact, controller pulses/events, curve, conservative contacts and local/world ownership. Original-input trace detail stays private. [Synthetic scenarios](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/PersistentDoorActorScenarios.cs) pass 124 repeated transfers plus invalid input/pair/handle, reset/dispose, unknown/denied entry and outward retreat gates. Full core smoke passes; Godot builds with zero warnings/errors.

Use the [prior bounded Godot command](IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md) with **`-DoorWalk`**. Headless and GPU adapters pass 20 repeated control transfers across two owner poses and validate retained handle, actual source/target ownership, retreat, reset and reflected placement. GPU capture was visually inspected; captures and detailed output stay ignored/private. Known root-certificate-store startup error is nonfatal; no network is used.

## Next probe

Add a bounded fixed-step scheduler separating render delta from simulation time/input, with pause/focus behavior, finite/monotonic checks and explicit catch-up/drop accounting. Feed live Godot keyboard input through it. Compare equal simulation outcomes across different render partitioning and permission changes at tick boundaries. Original narrow-phase geometry, general actor ordering and moving-leaf overlap response remain open. Coverage scores remain 89% / 94%, packet rows 345, canonical names 2,599.
