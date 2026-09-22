# PEFT particle-template boundary

Date: 2026-09-13  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope and result

This block closes the particle appearance's top-level asset factory, supported PEFT versions, description ownership, effective-corpus profile and description-to-runtime-group bridge. It deliberately does not claim every `EMTR` emitter field.

Confidence: **Confirmed** for registration, versions, framing, group ownership/materialization, corpus counts and the runtime roles of all four PEFT/0002 scalars.

## Install and factory chain

```text
SetupClientParticle::install 0x007A1100
  -> ParticleEffectAppearanceTemplate install 0x007B6DB0
       -> register PEFT with AppearanceTemplate registry
       -> factory 0x007B6D50
            -> template ctor/load 0x007B6C20
                 -> ParticleEffectDescription ctor 0x007C2DA0
                 -> description load 0x007C3990
```

The legacy template is 0x24 bytes and separately owns a 0x38-byte description. Those sizes document the executable, not portable file structures.

## Top-level grammar

All versions are bounded IFF forms beneath `FORM PEFT / FORM version`.

| Version | Top-level contents |
|---|---|
| `0000` | count chunk `0000` containing S32/U32 group count; repeated `FORM EMGP` |
| `0001` | `FORM PTIM/0001`; count chunk; repeated `FORM EMGP` |
| `0002` | `FORM PTIM/0001`; count plus four F32 values in chunk `0000`; repeated `FORM EMGP` |

The version-2 float values are stored at description offsets `+0x28`, `+0x2C`, `+0x30`, `+0x34`. Constructor defaults are `1.0, 0.0, 1.0, 1.0`. Runtime `0x007A9060` proves the first three are initial time scale, transition time and sustaining time scale; runtime `0x007A90D0` proves the fourth is overall effect spatial scale. Nested particle effects recursively multiply parent and child values.

Each group becomes a 0x1C-byte legacy description value. `particle_emitter_group_description_load_VA_007C5460` recursively loads its versioned `EMTR` children.

## Runtime materialization

`ParticleEffectAppearance` installation is `0x007A8500`. Constructor `0x007A85A0`:

1. retains the source template;
2. initializes enabled/state/time-scale fields;
3. obtains the ordered emitter-group description vector;
4. allocates one runtime `ParticleEmitterGroup` for every description;
5. stores owned group pointers and invokes group initialization;
6. exposes aggregate liveness/count and alter/restart style operations by iterating the group vector.

The replacement boundary is therefore an immutable decoded template graph feeding simulation-owned runtime instances. Rendering consumes transient quad/mesh batches; it does not own particle lifetime policy.

## Effective corpus

A new configuration-aware scanner processed all 58 search trees named by the current `swgemu_live.cfg`, respecting highest-priority path winners.

| Effective result | Count |
|---|---:|
| `PEFT/0002` | 1,305 |
| `PEFT/0001` | 808 |
| `PEFT/0000` | 1 |
| zero-byte tombstones | 9 |
| total `.prt` effective paths | 2,123 |
| extraction errors | 0 |

All 2,114 live resources use a top-level version supported by Stardust.exe.

Representative fixtures:

- `appearance/unshaded_geyser_test.prt` — sole live 0000;
- `appearance/pt_bls_1_red_miss_terrain.prt` — 0001;
- `appearance/pt_trailing_space_explosion_md03.prt` — 0002 with multiple `EMTR/0013` quad emitters.

## Nested family evidence

The sample trees establish common nested tags:

```text
PEFT -> PTIM -> EMGP -> PTIM -> EMTR
EMTR -> WVFM... -> PTQD or particle description
PTQD -> PTCL + WVFM... + PTEX
```

`WVFM` is repeatedly used for time-varying scalar curves and `CLRR` for color ramps. Exact emitter-field ordinal semantics remain the next block; the reader must already preserve child order and version boundaries.

## 64-bit and engine implications

- Never map IFF bytes onto the legacy 0x24/0x38/0x1C C++ objects.
- Decode counts with bounds and allocate native containers.
- Store asset references as stable logical paths/handles, not 32-bit pointers.
- Separate immutable descriptions, mutable simulation instances, and renderer batches.
- Preserve overlay tombstones and version-specific defaults.
- Keep quad/mesh particle limits configurable; original memory-pool caps are performance policy, not file-format semantics.

## Remaining particle work

- recover `EMGP` timing/control fields;
- enumerate `EMTR` versions (the deployed sample reaches `0013`);
- map emitter shapes, generation rate, lifetime, velocity, orientation and local/world-space flags;
- close `PTCL`, `PTQD`, mesh-particle and attachment descriptions;
- trace per-frame spawn/update/removal and sorter-phase submission;
- generate golden decoded fixtures for versions 0000-0002.

Overall weighted client-research estimate remains **74%**. This is one meaningful asset-system closure, not enough by itself to change the whole-client percentage.
