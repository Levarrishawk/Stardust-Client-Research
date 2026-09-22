# Static-collidable flora RNG closure

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

Static-collidable flora does **not** consume an unbounded global RNG stream. `terrain_update_static_collidable_flora_cell_VA_00B5E780` constructs a private RNG state on its stack for each 16-metre cell and seeds it from that cell's linear key. This restores a strict deterministic-placement path and corrects the later provisional global-state interpretation.

## Exact seed and state ownership

After computing:

```text
cellX = floor(worldX / 16)
cellZ = floor(worldZ / 16)
linearKey = (cellX + halfMapCells) + (cellZ + halfMapCells) * mapCellCount
```

the raw instructions at `0x00B5E867..0x00B5E8AC`:

1. store `-linearKey` as the first word of a zeroed stack-local RNG state;
2. zero the state table;
3. pass the address of that state in `ECX` to `0x00B59EF0` for both initial samples.

`0x00B59EF0` is a stateful shuffled Park–Miller generator. A negative seed forces initialization. It advances with modulus `0x7FFFFFFF` and multiplier `0x41A7` (16807), maintains the shuffled state table, and returns a positive integer normalized elsewhere by `4.65661287308e-10` (`1 / 2147483648`). A zero cell key becomes seed 1 through the generator's minimum-seed branch.

## Per-cell sample order

All generation samples use the same stack-local state:

1. X cell jitter;
2. Z cell jitter;
3. yaw in `[0, 2π)` after a flora family/template is accepted;
4. optional uniform-scale sample when the selected family enables a scale range.

Early rejection ends consumption only for that disposable cell-local state. It cannot perturb another cell. Rebuilding a cell therefore produces the same candidate position, family query coordinates, yaw and scale given the same terrain assets and bounds.

## Adjacent dynamic-flora correction

`0x00AB8E10` is a transform yaw rotation, not an RNG seeder. In `0x00858BC0`, `x + z` is supplied as a yaw angle to the new node's transform. The node's `+0x48` phase then comes from the shared RNG object at `0x01947188`. Thus:

- static-collidable placement is explicitly per-cell deterministic;
- dynamic-node orientation is position-derived;
- dynamic-node phase is a shared-stream sample and is not independently seeded by position.

## Replacement contract

For byte/placement parity, provide a small fixed-width legacy RNG implementation and create a fresh state for each static-collidable cell using `-linearKey`. Consume samples in the exact order above. Do not use the engine's global random service, native-width integers, or a hash-based substitute in strict compatibility mode.

A modern deterministic hash stream remains acceptable only as an explicitly non-parity mode.

## Confidence

- Stack-local ownership and negative linear-key seed: **Confirmed by raw x86 register flow**.
- Park–Miller constants and shuffled-state behavior: **Confirmed by `0x00B59EF0`**.
- Four-sample conditional order: **Confirmed by raw call sites and family-scale branch**.
- Dynamic yaw/global-phase correction: **Confirmed by `ECX` ownership at both calls**.

