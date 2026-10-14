# Deterministic creature template collision profile

## Boundary and evidence

This engine-neutral slice starts at original object IFF, resolves inherited shared-template fields and returns a collision profile plus appearance path. It does not instantiate the appearance, simulate an original controller or replace the existing preview actor volume.

Executable baseline SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. [Fallback evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_CREATURE_COLLISION_FALLBACK.md) confirms collision-field getter assignments and local sphere construction. Float parameter constructor `013FA000` initializes absent value and space modifier; vtable `01609AB8` dispatches loader `013FA200`. The loader reads U8 mode, U8 modifier and, for mode1, four bytes of scalar storage. Mode0 clears value presence; getter inheritance is demonstrated by `011AF1B0` and its sibling getters. Random list/range modes also exist.

String parameter constructor `013FA9B0` installs vtable `01609B48`, whose loader `013FADA0` reads U8 mode; mode0 clears value and mode1 reads a terminated string. Those are confirmed original codec observations. Original random/relative behavior, default policy and all template families are outside this implementation.

## Owned structural decoder

`SharedObjectTemplateDecoder` accepts a bounded nested hierarchy: SCOT/0012 or0013 → STOT/0007 or0009 → SHOT/0007,0008,0009 or0010. SHOT/0008 is the bounded original terminal observation added in [Round7](SCENE_STATIC_RESOURCE_CONTRACT.md). STOT and SHOT may also be roots. These are the observed input versions accepted by this replacement slice, not the original executable's complete version set.

Each layer has optional DERV containing one XXXX terminated ASCII parent path, then its version form, then its required nested base family. The version contains PCNT U32LE and exactly that many XXXX parameter chunks. Each parameter begins with a terminated ASCII name and retains its separately encoded payload. Names are case-sensitive and unique within their layer. Nested derivation paths must agree in this slice; differing paths reject explicitly.

Limits: 1 MiB per decoded IFF, 4,096 nodes, depth16, at most512 parameters per layer, names at most128 bytes and parent paths at most1,024 bytes. The decoded tree owns its bytes. Unknown parameter types remain opaque; they are not interpreted as collision fields.

## Deterministic profile resolution

`SharedCreatureTemplateProfileReader.Load` requires a pinned SCOT root and an effective overlay lookup callback. It follows one normalized parent chain, with cycle/missing/masked failure and at most16 sources, total1 MiB and caller depth1–16. It resolves seven SCOT floats and SHOT `appearanceFilename` from nearest authored values. Every resolved field reports source path, family and version; every read source reports effective priority, bytes and SHA-256.

- Float mode0 requires exactly `{U8 0, U8 space}` and continues inheritance. Mode1 requires `{U8 1, U8 space, F32LE value}`. Nonfinite scalars, random modes, relative modifiers and invalid lengths reject.
- String mode0 is exactly one zero byte and continues inheritance. Mode1 is byte1 followed by one terminated ASCII string. A resolved empty appearance, unsupported modes and trailing bytes reject.
- Missing/unresolved required fields reject. No original default value is fabricated. Negative step height, collision height, radius, length or warp tolerance are outside the valid profile slice.

`CreateFallbackSphere(scale, specialDimensions)` constructs only the recovered local geometry. Ordinary center is `(offsetX * scale, (stepHeight + radius) * scale, offsetZ * scale)`; radius is `radius * scale`. The special flag substitutes step height1 and radius0.5. Positive finite scale at most1,000 is a replacement guard. Nonfinite results reject. This method does not select source precedence, world orientation, active participation or the floor/body solver.

## Original human result and validation

Pinned `object/creature/player/shared_human_male.iff` resolves through five original sources totaling8,290 bytes. Configuration has48 descending mounts. Original root supplies `appearance/hum_m.sat`; base player supplies step height0.5; base creature supplies collision height1.8 (authored F32), radius0.5, offsets0, length1.5 and warp tolerance17. At scale1, ordinary fallback center is `(0,1,0)`, radius0.5 and lowest point0.5 above local origin. Those values are confirmed independently from original bytes; active runtime participation remains Unknown.

Native gates exercise inheritance override/provenance, ownership, scale/special geometry, cycles, root identity, masked parents, depth limit, truncation, duplicate fields and unsupported/nonfinite parameter encodings. `verify_creature_template_profile.py` independently parses the configured original chain and compares values, provenance, appearance and source identities. Prototype host builds with zero warnings/errors. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_CREATURE_TEMPLATE_PROFILE.md).

## Next action

Use the resolved profile to build an explicitly modern actor volume query that preserves the recovered sphere offset, with floor/door contacts and owner transforms. Keep original controller-wide ordering and physical participation separate. Appearance instantiation from the object root requires the existing skeletal appearance/animation boundary as its own vertical slice.
