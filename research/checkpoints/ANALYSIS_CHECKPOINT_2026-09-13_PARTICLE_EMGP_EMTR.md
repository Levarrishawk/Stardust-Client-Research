# Particle EMGP/EMTR hierarchy and version boundary

Date: 2026-09-13  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope and conclusion

This continuation closes particle emitter-group framing, emitter version dispatch, deployed-version reachability and group-to-runtime-emitter cardinality. It leaves detailed per-field semantics inside the fifteen EMTR loaders for subsequent blocks.

Confidence: **Confirmed** for versions, order, counts, ownership and construction. Individual PTIM and EMTR field labels remain partly Unknown.

## EMGP grammar

`particle_emitter_group_description_load_VA_007C5460` enters `FORM EMGP`, reads the nested version and accepts only 0000 or 0001.

| Version | Contents |
|---|---|
| 0000 | `FORM 0000 / chunk 0000 {U32/S32 emitterCount}`; repeated `FORM EMTR` |
| 0001 | `FORM 0001 / FORM PTIM/0001`; count chunk; repeated `FORM EMTR` |

The description constructor at `0x007C4BD0` initializes common timing state and an owned vector. Each vector element is 0x24C bytes in this 32-bit executable. That size is not an on-disk record length because each emitter contains nested variable-length WVFM, CLRR and particle-description forms.

## EMTR dispatcher

`particle_emitter_description_load_VA_007B9B90` enters `FORM EMTR` and explicitly dispatches every version 0000 through 0014:

| Version | Loader |
|---|---:|
| 0000 | `0x007B9D70` |
| 0001 | `0x007BA010` |
| 0002 | `0x007BA2B0` |
| 0003 | `0x007BA560` |
| 0004 | `0x007BA860` |
| 0005 | `0x007BAB70` |
| 0006 | `0x007BAEF0` |
| 0007 | `0x007BB290` |
| 0008 | `0x007BB6F0` |
| 0009 | `0x007BBB70` |
| 0010 | `0x007BC030` |
| 0011 | `0x007BC5A0` |
| 0012 | `0x007BCB20` |
| 0013 | `0x007BD140` |
| 0014 | `0x007BD780` |

Unsupported tags are rendered as four printable characters in `ParticleEmitterDescription::load() - Unsupported data version: %s`; the loader does not heuristically consume a newer version.

## Effective nested-version census

The scanner traversed every live effective `.prt` selected by the current configured search-tree overlay.

### Emitter groups

| Version | Occurrences |
|---|---:|
| EMGP/0000 | 1 |
| EMGP/0001 | 2,113 |
| Total | 2,114 |

There is exactly one group per live top-level effect in the current corpus.

### Emitters

| Version | Occurrences |
|---|---:|
| EMTR/0000 | 1 |
| EMTR/0006 | 3 |
| EMTR/0009 | 50 |
| EMTR/0010 | 26 |
| EMTR/0011 | 419 |
| EMTR/0012 | 636 |
| EMTR/0013 | 340 |
| EMTR/0014 | 1,859 |
| Total | 3,334 |

Versions 0001-0005 and 0007-0008 are supported migration formats but absent from current effective winners. No live unsupported version exists.

## Runtime construction

`particle_emitter_group_ctor_materialize_emitters_VA_007B6030` receives the group description and parent `ParticleEffectAppearance`. It initializes group timing/state, retains parent and description relationships, allocates an owned emitter-pointer vector, then loops across the description vector. For every 0x24C-byte description it allocates one 0x364-byte runtime `ParticleEmitter`, initializes it with the description and parent context, and appends it in source order.

```text
PEFT description
  -> ordered EMGP descriptions
       -> ordered EMTR descriptions

ParticleEffectAppearance instance
  -> owned ParticleEmitterGroup instances
       -> owned ParticleEmitter instances
```

The replacement must preserve this hierarchy and order but may use ordinary 64-bit-safe handles and containers.

## Architectural implications

- PTIM belongs to simulation/lifecycle policy, not renderer state.
- EMTR is an immutable authored description; runtime emission counters, clocks and live particles belong to an instance.
- The renderer should accept generated quad/mesh batches after simulation.
- Version upgrade readers should populate a common current description with explicit defaults.
- Counts and nested IFF boundaries require defensive validation before allocating.
- Never use legacy 0x24C or 0x364 layouts as serialized or network-visible structures.

## Next particle slice

Field-difference versions 0009-0014, starting from dominant 0014, to map:

- emitter shape and local/world reference frame;
- generation rate and burst policy;
- emitter and particle lifetime;
- velocity/acceleration/orientation controls;
- looping and one-shot termination;
- particle-description selector and attachment behavior.

Overall weighted client-research estimate remains **74%**. This block reduces asset-format uncertainty but does not independently move the whole-client estimate.
