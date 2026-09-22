# Server time and environment clock checkpoint — 2026-09-12

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes the principal `ServerTimeMessage` path and the time-of-day clock it controls. Stardust is authoritative for an elapsed zone-time value, but the client does not wait for network packets to animate the sky. It converts the initial/periodic server value into a position within an asset-defined environmental cycle, advances that position by frame delta locally, wraps it at the cycle duration, and recomputes environmental interpolation state. Periodic server messages correct the local cycle position.

## Server evidence

`ServerTimeMessage.h` writes `LE16(2) | LE32(0x2EBC3BD9) | LE64(galacticTime)`. `CmdStartScene.h` carries the same LE64 value in its scene-start payload. `Zone::getGalacticTime()` returns `galacticTime.miliDifference() / 1000`; the default `Time` constructor captures current real time, and no later mutation of `Zone::galacticTime` was found. The value is therefore elapsed seconds since that Zone object's construction, not Unix time.

`PlayerObjectImplementation.cpp` sends `ServerTimeMessage` every 60,000 ms while the player is in a zone and the server is not loading. `CmdStartScene` supplies the initial value when constructing the scene.

## Client path

| Stage | Address | Confirmed behavior |
|---|---:|---|
| registered dispatch branch | `0x0051C41E` | recognizes `ServerTimeMessage` |
| parser | `0x00ADC0F0` | materializes the LE64 time at parser object `+0x40` |
| scene offset setter | `0x0051F8F0` | stores `low32(serverSeconds) - localWallClockSeconds` at GroundScene `+0xC0` |
| synchronized-seconds getter | `0x0051F900` | returns `localWallClockSeconds + sceneOffset`; no direct static caller was recovered |
| cycle-duration query | `0x00B5CB40` → `0x00B5E200` → `0x00B6E6C0` | returns PTAT terrain-template field `+0x40` |
| normalized-time setter | `0x00B5CBD0` → `0x0084B880` → `0x00846BF0` | converts normalized cycle position to elapsed cycle seconds and refreshes environment-entry phase |
| GroundEnvironment frame update | `0x008479B0` | unless time-of-day is disabled, adds frame delta to cycle seconds; wraps at duration; stores normalized time at `+0x7C`; updates environment state |
| normalized-time query | `0x00846BE0` | returns `cycleSeconds / cycleDuration` |

The handler preserves the full parsed U64 for its floating-point modulo calculation. It obtains the terrain cycle duration, computes `serverSeconds mod duration`, divides by duration, and calls the normalized-time setter. Separately, its compatibility clock-offset path truncates to the low 32 bits before subtracting the local wall clock.

## Deployed asset evidence

The effective `terrain/tatooine.trn` resolves from `mtg_patch_013_configurable_02.tre` at overlay priority 42 and is `PTAT/0014`. Its DATA payload is deliberately unaligned after the NUL-terminated source-path string. Following the executable's exact reader sequence, PTAT field `+0x40` is the four bytes `00 40 30 46`, little-endian float `11280.0` seconds: a 188-minute complete environment cycle.

The template constructor at `0x00B6E2F0` supplies a generic `1800.0`-second default, but PTAT/0013 and /0014 loaders overwrite that member. A replacement must use each terrain asset's value, not hard-code either number.

## Configuration

| Key | Executable default | Role |
|---|---:|---|
| `disableTimeOfDay` | false | prevents per-frame clock advancement when set |
| `environmentStartTime` | 300.0 | used by a separate local/debug scene path |
| `useNormalizedTime` | false | selects a configured normalized start instead of supplied galactic seconds in the shared scene helper |
| `environmentNormalizedStartTime` | approximately 0.525 | configured normalized start used when the preceding switch is true |

Production `CmdStartScene` passes its server galactic-time field through the shared scene constructor. Configuration can override that behavior, but the loose deployed files have not been shown to do so.

## Consumers and semantics

GroundEnvironment frame update derives normalized cycle position and drives interpolation across sky, fog, light/cloud and related environment records. The exposed predicates at `0x00846C70` and `0x00846C90` both compare normal finite values against the embedded `0.7f` boundary (they differ only in unordered/NaN treatment). The already traced music manager and game-light path consume the terrain/environment day-state service. Exact astronomical labels for every segment of the normalized cycle remain asset- and consumer-dependent; `0.7` should not be renamed to a guessed real-world hour.

## Replacement contract

1. Parse both `CmdStartScene` and `ServerTimeMessage` time fields as explicit little-endian `uint64_t` seconds.
2. Treat the value as server/zone elapsed time, not an epoch timestamp.
3. Read the full-cycle duration from the effective PTAT terrain asset.
4. Compute cycle position with full-width integer/double arithmetic, then maintain a monotonic local accumulator between server corrections.
5. Preserve the wire width, but do not reproduce the legacy low-32-bit offset truncation internally.
6. Publish one authoritative environment-cycle state to lighting, sky/fog/cloud, terrain environment and adaptive music consumers.
7. Define correction smoothing explicitly. The legacy message path appears to set the cycle position immediately; a modern client may smooth small drift while snapping on scene entry or large divergence, provided protocol-visible behavior is unchanged.

## Confidence and remaining boundary

- **Confirmed:** wire opcode/layout, initial and periodic server sources, seconds unit, elapsed-Zone lifetime semantics, full-U64 modulo path, local frame-delta advancement/wrap, PTAT-sourced duration, effective Tatooine duration, configuration keys/defaults.
- **Strong:** the periodic message is a clock correction for the same environment cycle initialized by `CmdStartScene`.
- **Unknown:** whether deployed launch arguments override the loose time settings; the intended friendly names of all normalized-cycle intervals; whether the unused-looking `0x0051F900` API is reached only through an unresolved indirect boundary.

