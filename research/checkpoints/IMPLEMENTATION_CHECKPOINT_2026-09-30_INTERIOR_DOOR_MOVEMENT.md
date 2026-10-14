# 2026-09-30 checkpoint: original interior doorway movement

## Question and stopping condition

Can one named original POB door reference drive physical actor clearance and current entry permission before a bounded floor owner transfer? Stop once the selected doorway is integrated, both directions and transformed owners match an independent oracle, Godot controls/rendering pass, and unsupported source contacts preserve ownership explicitly.

## Input identity and tools

Executable rechecked: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Existing static evidence is reused; no new VA semantic claims are added. Effective archive root is `C:\Stardust-DEV`, 48 TREE mounts via `swgemu_live.cfg`, configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Used existing overlay/IFF/DTII/floor inspectors, .NET 8 native smoke tests, Python independent geometry oracle and Godot 4.7.2 .NET x64.

| Effective input | SHA-256 |
|---|---|
| appearance/thm_tato_cantina.pob | 3DAA136761AB8073F93250098CB1D38807E5279337C29F196D71C207B6D08984 |
| datatables/appearance/door_style.iff | C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7 |
| appearance/cantina_door.apt | 86B5E5DB2B96C77FE8A262F07C8D26F538525B537217CE24B6FCE2CEF9AD3A57 |
| appearance/mesh/cantina_door.msh | 8EE34404E8A7517BB51265970A6495A2E6A3AE0A6695619FD37D2F10674A7148 |
| appearance/collision/thm_tato_cantina_r3_cantina_collision_floor0.flr | DC61558BC59587861D0AAD94724783A0400662F302885656C96BE75A4E8DBAD5 |
| appearance/collision/thm_tato_cantina_r12_private_room_collision_floor0.flr | 5A28497651ADAD4FE477BF0DE3245749A2CBC314C242E4B469E7C79F705F9A2C |

## Evidence, interpretation and compatibility consequences

The [durable contract](../../docs/INTERIOR_DOOR_MOVEMENT_CONTRACT.md) consolidates exact reference/edge identities, transform composition, borrowed-runtime ownership, query snapshots, source-floor contact rules and confidence boundaries. Original selected references are cell 3 / portal 10 and cell 12 / portal 0, both geometry 13 and `cantina_door`. Source boundary triangle 97 / edge 1 pairs with target triangle 20 / edge 1. Identical authored transforms and matching floor edges are directly observed in original bytes (**Confirmed**). Using the primary leaf box and checking it against a speculative floor result before adopting ownership are independently authored modern policies; original narrow-phase shape and global actor ordering remain **Unknown**.

The loader requires explicit named/eligible/transformed references, exact supported style rather than fallback, two floors and a paired boundary. The extended walker preserves local/world owner frames and separates PermissionBarrier from DoorLeaf contacts. It snapshots boxes before external callbacks and selects attainable source contact without mutating runtime state. Nonlevel/unsupported source landing preserves the original locator. The host adapter displays original leaf/floors and explicit replayed crossing controls.

## Validation and reproduction

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
dotnet build --no-restore poc/Stardust.Godot
python tools/python/verify_interior_door.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg local-private/generated-evidence/interior-door-milestone poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

The [verifier](../../../SWG-Godot-Client/tools/python/verify_interior_door.py) matches two original references × two owner poses × six states = **24 independent cases**, including paired edges, six source identities, approach surface positions, curve, contact parameter/normal/center, local/world output and resulting source/target ownership. The transformed owner is a 90-degree yaw plus `(30,4,-11)` translation, supplied identically to both cells. Native detailed output and summaries remain ignored/private. The [synthetic scenarios](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/InteriorDoorScenarios.cs) pass owner/contact snapshots, malformed input/pins, missing floor, partial owner maps, initial overlap, permission failure, nonlevel and post-boundary-contact cases.

Bounded Godot proof:

```powershell
& tools/powershell/Invoke-StardustAssetPreview.ps1 -GodotPath local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe -ArchiveRoot C:\Stardust-DEV -Configuration C:\Stardust-DEV\swgemu_live.cfg -Member appearance/thm_tato_cantina.pob -ExpectedSha256 3DAA136761AB8073F93250098CB1D38807E5279337C29F196D71C207B6D08984 -DoorStyle cantina_door -DoorTableSha256 C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7 -DoorCell 3 -DoorPortal 10 -DoorLeafSha256 86B5E5DB2B96C77FE8A262F07C8D26F538525B537217CE24B6FCE2CEF9AD3A57 -Headless
```

Use `-DoorCell 12 -DoorPortal 0` for reverse inspection. Replace `-Headless` with `-CapturePath` and a fresh absolute private PNG path for GPU validation, or `-Interactive` to inspect controls. Both tested headless and GPU hosts pass 12 control cases, owner/reflection checks, reset and permission-induced closure. Capture was visually inspected. Known Windows root-certificate-store startup error remains nonfatal; no network contact occurs. Existing YT-1300 headless floor/path/transfer/barrier gates also pass: 14 transfer links / 28 allowed-denied probes, 12 level barrier links / 24 probes, two explicit nonlevel stops and existing two-pick controls.

## Uncertainty and exact next probe

This proof binds only the selected source reference per query context. Reverse cases instantiate the opposite binding separately; it does not recover legacy paired-controller synchronization. Destination leaf response, moving-leaf collision during a timestep, arbitrary slopes, sliding, effect playback and continuous input remain open. Original executable execution, server contact, publication and commits were not used.

Next: add a persistent actor locator and repeated input/tick loop in the same two-cell scene. Consume source/target ownership from movement results, use current actor position for trigger retention, and reverse through the opposite reference. Verify cleanup/reset and permission changes over multiple transfers. Keep the conservative box and modern scheduling limits explicit. Research metrics remain 89% / 94%; packet matrix stays 345 and provisional registry stays 2,599.

Final workspace gate: 777 Markdown files, zero broken links or links into private material, 2,599 distinct canonical addresses and 345 packet rows. `git diff --check` passes; index remains empty. Final reverse-direction Godot headless controls also pass. Unrelated dirty work was preserved.
