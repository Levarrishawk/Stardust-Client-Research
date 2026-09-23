# WFBO WaitForBolt playback action (overnight block 4)

Original `Stardust.exe` executable plus configured effective `.pst` corpus. Seven `WFBO/0002` actions appear across `fire_projectiles_reflect.pst`, `fire_projectiles_reflect_target.pst`, and `fire_projectiles_ricochet.pst`; every observed `INFO` is four bytes `00000100`.

`0x005FE110` registers `WaitForBoltActionTemplate`. Factory `0x005FE220` allocates a 0x14-byte template; `0x005FE2F0` accepts only version `0002`, and `0x005FE3F0` reads two little-endian 16-bit actor indices into `+8` and `+0xC`. Thus the effective INFO selects actor 0 and actor 1. The template also initializes byte `+0x10` to true, but no INFO byte sets it in this version.

The template creates a 0x10-byte runtime object through `0x005FE190` → `0x006FB1D0`; runtime vtable `0x015E1F3C` points to update `0x006FB240`. On first update, this function requires both selected actors, computes their world-space translation distance, optionally subtracts 10.0 (default template flag true, clamped at zero), fetches playback float variable `LEVP` with default 55.0, and initializes a countdown to distance/speed. Each subsequent update subtracts frame delta and continues while the countdown is positive. If either actor cannot be resolved it terminates. This is **time-of-flight approximation**, not a wait on a live projectile object's collision event.

This behavior materially explains the reflected/ricochet scripts' synchronization: a replacement can reproduce the conceptual timing with logical actor positions and typed `LEVP` lookup while keeping actual projectile presentation decoupled. Whether `LEVP` can be zero/negative and how the original handles that edge has not been established. The decompiler's floating-point condition was checked against the code shape but not exhaustively proven for NaN/infinity behavior.

Provisional names: `0x005FE2F0` `parse_WFBO_template`; `0x005FE3F0` `parse_WFBO_0002_actor_indices`; `0x006FB1D0` `construct_WFBO_runtime`; `0x006FB240` `update_WFBO_time_of_flight_wait`.
