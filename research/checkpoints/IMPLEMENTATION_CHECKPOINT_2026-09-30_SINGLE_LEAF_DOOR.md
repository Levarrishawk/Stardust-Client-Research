# 2026-09-30 checkpoint: one original normal door leaf

## Question and stopping condition

Can a clean engine-neutral implementation decode the original style table, move one named normal leaf using the recovered curve and determine conservative actor clearance, with independent byte/math checks and a working Godot adapter? Stop after one named style passes; defer full interior wiring, paired leaves, force fields and original narrow-phase ordering.

## Inputs and identity

The executable SHA-256 was rechecked before address-based analysis: `C:\Stardust-DEV\Stardust.exe`, `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static analysis only. Installed archives use `C:\Stardust-DEV` with `swgemu_live.cfg`, 48 configured TREE mounts, configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` and established overlay priority/tombstone handling.

| Effective member | SHA-256 |
|---|---|
| datatables/appearance/door_style.iff | C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7 |
| appearance/cantina_door.apt | 86B5E5DB2B96C77FE8A262F07C8D26F538525B537217CE24B6FCE2CEF9AD3A57 |
| appearance/mesh/cantina_door.msh | 8EE34404E8A7517BB51265970A6495A2E6A3AE0A6695619FD37D2F10674A7148 |

The 24,635-byte table has 124 rows. APT/0000 NAME resolves the selected mesh; MESH's APPR/0003 primary EXBX/0001 has EXSP plus maximum-first/minimum-second BOX, followed by NULL secondary extent. The leaf has five authored hardpoints; no floor payload is consumed by this slice. Original bytes, detailed row output, static disassembly and captures stay ignored/private.

## Evidence and result

The [durable contract](../../docs/SINGLE_LEAF_DOOR_CONTRACT.md) records observations, confidence, schema, precise curve and implementation consequences. Exact static ranges: `00B52A50–00B52B30` for table install, path at `018B77EC`; `00B535ED–00B538F0` for column reads; `0128FF40–012900A0` for constructor/radius; `012901E0–01290660` for displacement, primary/secondary construction and trigger input; `01290B60` with `0131E2B0–0131E33A` for the power/cosine curve; `013EA170–013EA208` for trigger shape input copying. Existing prior evidence at `01290760`, `01290BB0`, `01290D50`, `01290EC0` supplies alter/contact/cap/prune semantics.

Confirmed refinements: retention radius is 1.5 times authored radius; new-contact center is local `(0,1,0)` with authored radius; movement uses a power/cosine blend, not raw linear fraction. Exact legacy lookup case behavior, denied-contact secondary behavior, general actor collision dispatch and narrow-phase leaf extent choice remain Unknown. Server `MMOCoreORB/sql/datatables.sql:67` is vocabulary correlation only.

Implementation is independently authored in [decoder](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/DoorStyleTableDecoder.cs), [asset reader](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/SingleLeafDoorAsset.cs), [runtime](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/SingleLeafDoorRuntime.cs), [synthetic scenarios](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/DoorLeafScenarios.cs) and [Godot adapter](../../../SWG-Godot-Client/poc/Stardust.Godot/Main.Door.cs). The existing collider gains an unpadded explicit-box constructor. Original curve precision is interpreted with modern double intermediates and F32 pi; no x87 bit-equivalence claim. Frame/contact policy remains an explicit modern seam.

## Reproduction and validation

With .NET 8 and Python available, from repository root:

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
dotnet build --no-restore poc/Stardust.Godot
python tools/python/verify_single_leaf_door.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg local-private/generated-evidence/door-style-milestone poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

The [independent verifier](../../../SWG-Godot-Client/tools/python/verify_single_leaf_door.py) reads effective archives, pins the inputs, decodes all rows and the primary box independently, then checks native output against its controller/curve and axial box oracle. Result: **124 rows, 100 frames, 60 blocked and 40 clear, three source identities**, zero discrepancies. Detailed summary and native output remain private. The synthetic suite includes malformed inputs, contact cap/pruning, hysteresis, revoked permission, callback failure, transformed owner and deterministic cleanup. Full core smoke and Godot build pass.

Bounded headless/GPU inspector command:

```powershell
& tools/powershell/Invoke-StardustAssetPreview.ps1 -GodotPath local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe -ArchiveRoot C:\Stardust-DEV -Configuration C:\Stardust-DEV\swgemu_live.cfg -Member appearance/cantina_door.apt -ExpectedSha256 86B5E5DB2B96C77FE8A262F07C8D26F538525B537217CE24B6FCE2CEF9AD3A57 -DoorStyle cantina_door -DoorTableSha256 C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7 -Headless
```

Replace `-Headless` with `-CapturePath` and a fresh absolute PNG destination under ignored `local-private` for GPU capture. Both modes pass contact/permission/reset controls, displaced adapter pose and box clearance checks; GPU capture was visually inspected. The known Windows root-certificate-store startup error is nonfatal; no network is used. Interactive mode uses `-Interactive` with the same pins. The adapter renders one neutral mesh; it does not play effects or load optional frames/textures.

## Handoff

Two newly reconciled semantic addresses bring the registry to 2,599. Research scores remain 89% / 94%; function percentage remains uncalibrated and packet matrix remains 345. No original executable execution, live-server contact or publication occurred. Next: locate one effective named POB door reference, compose cell/door transforms, and wire current single-leaf clearance into the bounded one-portal interior walk. Maintain conservative-box and ordering limitations until original solver evidence closes them.

Workspace validation: 774 Markdown files, zero broken links or links into private material, 2,599 unique provisional addresses and 345 packet rows. `git diff --check` passes; index remains empty. Existing unrelated dirty files remain preserved.
