# EMTR modern field evolution and runtime consumers

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope and confidence

This block compares the deployed `EMTR/0009` through `/0014` loaders, inventories their effective-asset values, resolves the two serialized factories, and traces several late-version fields into runtime behavior.

- **Confirmed:** serialized order, member destinations, version-tail byte counts, shape/type discriminators, corpus counts, terrain-conformance behavior, and the generation-accumulator path.
- **Strongly inferred:** behavioral names for the capacity/burst fields from their arithmetic and container comparisons.
- **Unknown:** the final editorial names of many waveform and scalar controls. Raw order is retained rather than fabricated.

## Common modern grammar

Every version 0009-0014 loads, in order:

1. `PTIM/0001` timing data;
2. sixteen `WVFM` waveform objects into description members `+0x3C` through `+0x1B0` (0x18-byte stride, with the gap at `+0x114` occupied by scalar state);
3. `chunk 0000` containing an author label string, a fixed scalar prefix, an optional sound-template string, a 39-byte common scalar tail, and the version extension;
4. a nested particle description selected by the serialized particle-type discriminator.

The corrected effective-corpus scan parsed all 6,364 deployed modern emitters without error (the remaining nine deployed emitters use older 0000/0006 formats). The original count incorrectly applied classic external odd-byte padding to SWG IFF siblings.

| Version | Emitters | Bytes after sound string | Version-specific suffix |
|---|---:|---:|---:|
| 0009 | 78 | 39 | 0 |
| 0010 | 28 | 68 | 29 |
| 0011 | 696 | 72 | 33 |
| 0012 | 1,039 | 97 | 62 bytes after its 35-byte stable portion |
| 0013 | 841 | 98 | 63 bytes after its 35-byte stable portion |
| 0014 | 3,682 | 99 | 64 bytes after its 35-byte stable portion |

Versions 0009-0011 have a 39-byte stable portion after the sound string (three booleans and nine dwords). Version 0012 removes the ninth legacy dword, leaving a 35-byte stable portion, and begins its full-dword flocking mode there. It then resumes append-oriented evolution: 0013 adds one byte to 0012 and 0014 adds one further byte. Version conversion must be explicit; a host-structure overlay is invalid.

## Fixed prefix and factories

The fixed prefix maps to legacy description members as follows. Types are loader-observed, while semantic labels are deliberately limited.

| Raw position | Legacy member/use | Finding |
|---|---|---|
| prefix 0, 1 | `+0x1C`, `+0x20` | U32/mode fields; `+0x20` selects alternate generation-accumulator arithmetic |
| prefix 2 | factory input stored at `+0x24` | emitter-shape discriminator |
| prefix 3 | `+0x28` | boolean consulted when live particles already exist |
| prefix 4, 5 | `+0x23C`, `+0x240` | float32 controls, exact editorial names unknown |
| prefix 6 | `+0x2C` | float32 compared with live-particle vector cardinality; strongly inferred capacity ceiling |
| prefix 7 | `+0x30` | boolean selecting integer burst-count behavior |
| prefix 8, 9 | `+0x34`, `+0x38` | inclusive random integer lower/upper range when burst behavior is selected |
| prefix 10 | `+0x114` | boolean, editorial name unknown |
| prefix 11 | `+0x118` | mode value; legacy serialized value 3 is normalized to runtime value 2 |
| prefix 12 | `+0x11C` | boolean, editorial name unknown |
| prefix 13 | consumed scalar | invariant 1 throughout all 6,364 modern deployed emitters |
| prefix 14 | particle-description factory input | 0 = quad description, 1 = mesh description |

`particle_emitter_shape_factory_VA_007C5D30` has an exhaustive six-entry jump table. RTTI resolves the values conclusively:

| Value | Concrete shape |
|---:|---|
| 0 | `ParticleEmitterShapeCircle` |
| 1 | `ParticleEmitterShapeSphere` |
| 2 | `ParticleEmitterShapeRectangle` |
| 3 | `ParticleEmitterShapeCube` |
| 4 | `ParticleEmitterShapeLine` |
| 5 | `ParticleEmitterShapeX` |

`particle_description_factory_VA_007BE8C0` accepts only values 0 and 1. Constructor and RTTI/vtable correlation proves 0 creates `ParticleDescriptionQuad` (legacy size 0x128) and 1 creates `ParticleDescriptionMesh` (legacy size 0x120). Other values return null. The selected object's virtual load at slot `+0x10` consumes the following nested description.

## Strings and audio attachment

The first chunk string is an author-facing emitter label (`Default`, `flash`, `torch`, and similar values dominate). The second is an optional sound-template path. Within the modern set it is empty for 3,151 records (47/50 v0009, 26/26 v0010, 391/419 v0011, 602/636 v0012, 340/340 v0013, and 1,745/1,859 v0014), leaving 179 non-empty references. Non-empty values include `sound\\veh_airtaxi_run_lp.snd`, environment loops, explosions and distant-vehicle sounds. Absolute authoring-machine paths also survive in assets, so the replacement loader must normalize or resolve by logical asset suffix rather than assume every stored string is a portable path.

## Runtime-confirmed late fields

The 0012 extension introduces terrain/conformance controls:

- Boolean `+0x218` gates a collision/ground query during particle initialization, provided the common sound/attachment flag at `+0x1E4` is not set.
- Float `+0x21C` is added to the returned surface height: a confirmed vertical surface offset.
- Boolean `+0x219` additionally transforms the particle orientation/direction using the returned surface frame; this is terrain/surface-normal alignment behavior.
- Float `+0x220` is tested against epsilon and then used as a symmetric random angular range in runtime direction perturbation.
- Float `+0x224` scales the periodic randomization gate.

The byte introduced at `+0x228` in version 0013 is consumed by `particle_emitter_advance_generation_accumulator_VA_007A6980`. When enabled it changes negative generation-accumulator recovery and whether a partially exhausted interval may immediately spawn another particle. It is therefore a generation/backlog policy flag; a narrower editor label is not yet proven.

The byte introduced at `+0x229` in version 0014 is a particle-pool admission-priority flag. Spawn path `0x007A4350` passes it to `0x007B5990` before allocation. False and true select the normal and expanded/reserve global thresholds respectively when the pool is under pressure; reaching the selected threshold suppresses the spawn. It is false in 3,127 and true in 555 effective 0014 emitters.

## Replacement-client consequences

- Parse EMTR into a typed, engine-neutral description with explicit version conversion.
- Preserve all sixteen waveforms and every scalar, including currently dormant fields.
- Represent shapes as a closed enum and particle payloads as a `Quad | Mesh` tagged union.
- Keep audio-template references in the particle description but resolve them through the asset manager.
- Terrain conformance belongs in simulation/spawn initialization, not the renderer.
- Maintain generation accumulation separately from particle rendering; renderer APIs should receive already-simulated batches.
- Never copy the legacy 0x24C description memory image or its 32-bit pointers into the x64 implementation.

## Remaining bounded work

1. Assign semantics to the sixteen waveform channels by tracing their runtime evaluation sites.
2. Preserve the now-decoded 0010-0014 flocking/cage and environmental-color controls; editorial names for dormant `+0x20C/+0x210` remain unknown.
3. Close older deployed formats 0000 and 0006.
4. Decode nested `PTQD`, `PTCL`, mesh and attachment description schemas.
