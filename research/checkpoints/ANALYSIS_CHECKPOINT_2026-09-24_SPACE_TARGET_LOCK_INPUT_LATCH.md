# Space target-lock input latch

Date: 2026-09-24. Scope W10/W09. Target is original `C:\Stardust-DEV\Stardust.exe`; Ghidra decompilation is reconstructed pseudocode.

`SwgCuiHudActionSpace` registers `targetLockDown` (`0x0196C7A8`) and `targetLockUp` (`0x0196C79C`). The `0x00E211D0` dispatcher handles both behind a timestamp gate and a `Camera`→`CockpitCamera` RTTI check. Down tests local flag getter `0x00582B90`, then sets it through `0x00582B40(1)` and advances the gate timestamp. Up passes `0` through the same setter and resets the gate to current time. The getter reads byte `+0x30C` in its state owner; the setter writes that byte and, on release from an active state, clears two related fields through `0x00582B00`/`0x00582B20`. The callback falls through to the base HUD action dispatcher afterward. The only direct call references found to these getter/setter functions are in this HUD action callback.

This is a local cockpit-camera-scoped input latch. No direct packet send or command enqueue occurs in either inspected action branch. The later reader that turns the latch into targeting/weapon behavior, the identities of the two cleared fields, and any server-visible consequence remain **unknown**. Do not equate the flag with an authoritative target lock or serialize its 32-bit object offset.

Confidence: **Confirmed by executable** for names, camera/timing gates, getter/setter and absence of a direct send in these branches; **Unknown** downstream simulation/visual/server meaning. No new opcode or whole-client percentage change.
