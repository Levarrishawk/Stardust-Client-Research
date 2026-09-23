# Effective playback action compatibility audit (overnight block 18)

The configured 48-TRE overlay contains 32 effective `PBSC/0002` playback scripts, 68 threads and 124 action records. This audit compares the **authored action versions**, the direct tag registrations, and the observed `Stardust.exe` parser branches; it is not a live execution test.

| Action family | Effective count | Authored versions | This-executable status |
| --- | ---: | --- | --- |
| `PSKA` | 42 | `0002` ×37, `0003` ×5 | `0002` parser/runtime traced; constructor has no `0003` branch. |
| `FISU` | 8 | `0002` | Registered and parsed; projectile runtime traced. |
| `WFBO` | 7 | `0002` | Registered and parsed; distance-based timer traced. |
| `PPRT` | 6 | `0000` | Registered and parsed; particle/sound runtime traced. |
| `CPSA`, `STOP` | 5 each | `0000` | Registered and parsed; posture and targeted stop behavior traced. |
| `TOAT`, `TSTP`, `TSTR` | 4 each | `0000`, `0000`, `0001` | Registered and parsed; temporary object and trail lifecycle traced. |
| `ATAT`, `CWAT`, `FACE`, `HPTA`, `ULAT` | 3 each | `0001`, `0000`, `0001`, `0000`, `0002` | Registered and parsed; respective visual/attachment/spatial runtimes traced. |
| `GRLB` | 3 | `0001` ×1, `0003` ×2 | `0001` parser/runtime traced; constructor has no `0003` branch. |
| `FTAT`, `SCAL` | 2 each | `0000` | Registered and parsed; turret and scale behavior traced. |
| `ALHP`, `AMSG`, `HTAT`, `THSW`, `TIMR`, `WATR` | 1, 10, 1, 1, 1, 1 | `0000` | Registered and parsed; respective hardpoint, message, homing, sword, timer, watcher behavior traced. |
| `FIPL`, `PPRL` | 1 each | As authored in `fire_projectiles_location.pst` | No direct tag registration or literal tag found in the executable; runtime semantics unresolved. |

The table totals **124** actions. Exactly **nine** action records have a demonstrated direct-registration/version gap: five `PSKA/0003`, two `GRLB/0003`, one `FIPL`, one `PPRL`. The other **115** have a direct registered parser for the observed version, but this does **not** prove they execute successfully with every variable binding. The nine occur only in four effective scripts from `mtg_patch_017.tre`: `hoth_snow_ball_throw.pst`, `creature_throw.pst`, `use_particle_system_defender.pst`, and `fire_projectiles_location.pst`. The previous literal-reference audit found none of those four named by another effective payload, the executable, or current server source; common combat manager references point to the other 28 scripts. This is strong evidence of dormant/forward-version content in this installation, not proof of impossibility through generated or externally supplied names.

The new `GRLB/0003` discrepancy is particularly important because `throw_grenade.pst` uses supported `GRLB/0001`, whereas `creature_throw` and the Hoth snowball variant use `0003`. It would be incorrect to generalize the working `0001` parser to all grenade/throw assets. An original-asset-compatible new client should preserve all four scripts and expose unsupported-version diagnostics; future code must not silently truncate `PSKA/0003` or `GRLB/0003` payloads to older layouts.

Confidence: **confirmed by bytes/code** for counts, observed version branches, direct registration gap and missing `0003` branches; **strongly inferred** for dormancy from reference census; **unknown** for live reachability and the precise behavior of this executable's IFF reader when encountering those unsupported nodes.
