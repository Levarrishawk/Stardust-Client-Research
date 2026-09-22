# Block 5 — ParticleEffectAppearance lifecycle and hierarchy

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Runtime model

Constructor `particle_effect_appearance_ctor_materialize_groups_VA_007A85A0` retains its template, allocates one owned runtime group per ordered EMGP description, initializes elapsed/enabled/liveness state, and establishes aggregate bounds. Destruction walks those groups before releasing template/base appearance state.

Alter path `0x007A8AB0` validates enabled/liveness state, obtains recursive effective time scale from `0x007A9060`, multiplies frame delta, advances every group in bounded slices, accumulates effect elapsed time at runtime `+0x60`, rebuilds aggregate extent from live groups, and reports continued lifetime through the normal Appearance contract.

Nested effects retain a parent effect pointer at runtime `+0x5C`. Time scale and spatial scale are recursively composed, but child simulation and renderer batches remain independently owned.

## Replacement rule

Use immutable descriptions plus mutable effect/group/emitter instances. Keep hierarchy-derived scale in instance state, advance simulation in bounded steps, rebuild conservative bounds after updates, and expose renderer-neutral batches. Do not copy legacy vtables, vectors or pointer offsets.

Confidence: **Confirmed** for ownership and scale composition; **Strong** for individual alter-return labels where symbols are absent.
