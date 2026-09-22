# Analysis Checkpoint: Core Controller Effects

Date: 2026-09-11  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope completed

This block moved beyond controller-envelope and factory availability into client-side effects for representative high-frequency server emissions. It traced the player/creature/common dispatcher hierarchy and correlated combat, animation, spatial chat, posture, seating, weapon-range, flourish and buff behavior with current server constructors. No executable or service was run.

## Principal findings

- Confirmed fallback hierarchy: player dispatcher `0x0062AA10` -> creature dispatcher `0x006392E0` -> common dispatcher `0x006E5210`.
- `0xCC` CombatAction submits zero- or one-defender presentation, but explicitly discards the entire action for more than one defender.
- `0xF2`, `0xF4`, `0x131`, and `0x134` reach animation, spatial-chat, posture, and combat-spam presentation respectively.
- `0x140` directly updates five weapon combat properties after object lookup.
- `0x13B` resolves a seat/parent target and schedules attachment, with posture fallback.
- `0x229/0x22A` update/remove buff collection entries.

## Compatibility consequence

The server can construct multi-target `0xCC` actions with up to 20 defenders, while this executable intentionally ignores any decoded action with more than one. Preserve this fact in compatibility tests; it need not remain a limitation in the replacement.

## Progress

The strict whole-client estimate advances from 41% to **42%**. Named/controller-message coverage advances from 35% to **40%**. Next, continue the playable-scene controller set or pivot to the less-covered VFS/TRE pipeline.
