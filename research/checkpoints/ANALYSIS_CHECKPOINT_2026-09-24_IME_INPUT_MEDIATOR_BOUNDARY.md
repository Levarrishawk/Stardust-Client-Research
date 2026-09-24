# IME input mediator boundary

Date: 2026-09-24. Scope A09/W10+W07. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Ghidra output is reconstructed pseudocode; these are provisional addresses.

## Confirmed by code

RTTI identifies `SwgCuiIMEInput` with primary mediator vtable `0x015F09A8` and callback-side vtable `0x015F0924`. Constructor `0x00CA8D00` invokes the mediator base, binds a UI list named `listCandidate`, and registers the screen name `IMEInput` with a UI manager through `0x013D01F0`. This is a dedicated candidate-list presentation surface, not just ordinary chat text input.

The activation override `0x00CA8EB0` calls the base activation, obtains the current UI/input context, computes candidate-overlay placement relative to the active editor and screen bounds, and updates the screen registration through `0x013D0160(..., 1)`. The deactivation override `0x00CA91B0` calls the base deactivation and the same registration helper with flag `0`, then interacts with the active input widget to restore/normalize focus. The exact interpretation of the UI geometry fields is not fully reconstructed. No packet parse or direct network send is present in these inspected methods.

The nearby vtable entry `0x00CA9770` belongs to a **different** table after an RTTI boundary and was deliberately excluded from the IMEInput claims; adjacency is not class membership.

## Implication and residual work

The replacement needs a platform text-composition service that can present candidate choices near the focused editor. It need not preserve this exact mediator/vtable layout. This block does **not** recover the Win32 IME message bridge, candidate enumeration or commit/cancel semantics; those require separate analysis before claiming full text-input compatibility.

- **Confirmed:** class identity, candidate list binding, activation/deactivation UI-registration calls and absence of direct send.
- **Strongly inferred:** overlay positioning near the focused editor with clipping to screen bounds.
- **Unknown:** OS IME bridge, candidate list contents, commit semantics and behavior with modern Windows text services.

Evidence: read-only PE RTTI/vtable scan; Ghidra decompilation of `0x00CA8D00`, `0x00CA8EB0`, `0x00CA91B0`, `0x00CA8E20`. Transient output is ignored under `local-private/scratch/ime_input/`.
