# Space HUD window manager composition

Date: 2026-09-24. Scope A09/W10. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses are provisional; decompiler output is reconstructed pseudocode.

## Confirmed by code

`SwgCuiHudWindowManagerSpace` RTTI maps to primary vtable `0x015F392C`. Constructor `0x00E21B40` calls common HUD-window-manager construction and binds named UI pages `RadarPage`, `AllTargets`, `ShipReticle`, `FlyOut`, `GroupStatus`, and `MissileCount`. It creates six child mediators through `0x00EEF140`, `0x00EE7EB0`, `0x00EE74A0`, `0x00EE4A60`, `0x00EDFA00`, and `0x00EDE4E0`, respectively, and registers them into the HUD's mediator system. Four child pointers are retained at `+0x68..+0x74` and released in destructor `0x00E22030`; the other two use a different registration/ownership path, whose exact reference accounting remains open. Missing `ShipReticle`, `FlyOut`, `GroupStatus`, or `MissileCount` CodeData objects trigger `Fatal`; these names are part of the original UI-asset layout contract.

The constructor has a capability/setting gate `0x00911A70` that opens `WS_ShipSelfInfo` and `WS_ShipTargetInfo` via `0x008849C0`. The meaning of the gate is not established by this call alone.

Message callback `0x00E220E0` recognizes the already mapped `NewbieTutorialEnableHudElement` (`0xCA375124`), parses with `0x013F29E0`, compares its ASCII key against two retained global strings, and for either match applies blink amount and show/hide state to the first retained child. It then delegates to common HUD callback `0x00E40E40` for broader handling. The two key literals were not recovered from uninitialized globals in this block. This is an additional client consumer of an **existing** opcode, not a new message. No direct network send is visible in the inspected constructor, destructor or callback.

## Architectural reading and limits

Ground and space HUD managers share a common base and the `RadarPage`/`AllTargets` roles, but they assemble distinct ground status/group/attribute versus space reticle/flyout/group/missile children. A replacement can model these as two compositions over shared HUD services; it need not mirror the original inheritance or pointer layout. Original UI pages remain required input assets even if a future UI changes their layout.

- **Confirmed:** RTTI/vtable identity, child page names and constructor calls, fatal page checks, two-key tutorial branch, base delegation, lack of direct send here.
- **Strongly inferred:** scene-specific composition root for the space HUD.
- **Unknown:** two tutorial key literals, capability gate meaning, exact ownership of the non-retained child mediators, complete widget state behavior.

Evidence: read-only PE RTTI/vtable scan and Ghidra decompilation of `0x00E21B40`, `0x00E22030`, `0x00E220E0`; compare [ground counterpart](ANALYSIS_CHECKPOINT_2026-09-24_GROUND_HUD_WINDOW_MANAGER_COMPOSITION.md) and prior [space widget research](ANALYSIS_CHECKPOINT_2026-09-22_SPACE_HUD_WIDGETS_FORMATION.md). Transient decompilation is ignored under `local-private/scratch/hud_window_manager_space/`.
