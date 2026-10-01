# Static-mesh segment collision selection

Baseline SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The [October 1 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_MESH_COLLISION_SELECTION.md) records the bounded trace and original agreement. These are original **segment-query** rules. Actor volume collision, sliding and movement-controller selector choice remain separate.

## Confirmed executable boundary

`MeshAppearance` RTTI identifies runtime vtable `0x015E472C`. Template factory `0x007964E0` constructs the runtime at `0x007931C0`; ready primitive data creates its instance set at member `+0x54` through `0x00793360`/`0x007711E0`. The collision virtual at vtable `+0x0C` is `0x00793640`.

- Query member `+0x00`, returned by `0x01269000`, selects the coarse mode. Value2 intersects the appearance extent and returns interpolated point, normal and segment parameter.
- Other modes require the instance set and nonnull appearance extent, perform an extent segment test, then call `0x0077EA10`. Extent getter `0x00B2C290` reads appearance member `+0x08`; this is distinct from an authored collision-bound NULL record.
- `0x0077EA10` traverses primitive instances and shortens the desired endpoint after hits; `0x0077E4C0` delegates to the primitive template at `0x00770BA0`.
- Primitive template `+0x14` owns a collision receiver. An absent receiver rejects the query. Otherwise, with global bypass `0x01923373` false, shader virtuals `+0x10/+0x14` supply two booleans to `0x01269040`. Query selector member `+0x04` accepts booleanA for value0 or booleanB for value1. Other selector values reject. Coarse mode and shader selector are separate fields.
- Global bypass setter `0x00770C10` is wrapped by `0x00793180`; bypass skips eligibility filtering but cannot create a missing receiver. Its higher-level enabling policy remains open.

For `StaticShaderTemplate`, constructor `0x0077FB10` installs vtable `0x015E3A34`. Getter A `0x0077FF60` resolves the selected effect implementation via `0x00783700`, then calls `0x0075E4E0`. It is true only when the implementation has a first pass whose members `+0x20/+0x30` are both false. Getter B `0x0077FF90` returns selected implementation byte `+0x15`; no selected implementation makes either getter false. The [rendering specification](RENDERING_ARCHITECTURE.md) independently identifies PASS alpha-test state at `+0x30`.

IMPL/0005–0007 DATA byte5 loads member `+0x14`, byte6 loads `+0x15` (`0x0075DA60/0x0075DDC0/0x0075E120`). Only byte6 contributes to getter B. IMPL/0004 loader `0x0075D730` derives both bytes from the first-pass predicate after loading passes. A zero-pass implementation has false A and may have true B. Later passes do not change A. The explicit byte's broader authoring label remains unasserted; implementation calls it `SelectorOne`.

## Receiver construction and ownership

After each primitive version loads, `0x0076FBA0` invokes `0x0076FC20`. Nonzero indexed geometry is required. Unless configuration `ClientGraphics/ShaderPrimitiveSetTemplate` / `createCollisionDataForNonCollidablePrimitives` is true, at least one shader predicate must be true. Initializer `0x0076F910` supplies default false. Construction reads vertex positions through indices, deduplicates positions and constructs triangle data. Up to64 triangles use allocation0x44/constructor `0x0076E250`; larger sets use allocation0x25C/constructor `0x0076EB10`. The receiver is stored at `+0x14`, and primitive destruction invokes its deleting virtual when nonnull (`0x0076FA02–0x0076FA0F`). Detailed receiver intersection tolerances, topology variants and acceleration structure semantics remain open.

## Replacement boundary

`MeshPrimitiveCollisionPolicy.FromSelectedImplementation` accepts an explicitly selected decoded IMPL/0004–0007 or null. It returns `OpaqueFirstPass` and raw `SelectorOne`, plus construction eligibility and selector acceptance. It does **not** choose capabilities or union candidates. Native tests cover the two-predicate truth table, zero-pass eligibility, missing selection, first-pass-only behavior, older derivation and invalid fields/versions/selectors.

The existing Mustafar route's three original meshes have44 primitives/42,986 triangles. All candidates agree per primitive: selector1 accepts all44/42,986; selector0 accepts42/42,890. The96-triangle difference is precisely the two hidden helpers. Hall6 has6 primitives/1,984 triangles accepted under both selectors. No GPU implementation selection ambiguity affects these predicates in this sample. This closes the helper-only source ambiguity, without establishing original actor collision policy.

Independent `tools/python/verify_selected_mesh_collision_predicates.py` compares original bytes with the native `--portal-materials-defaults` probe. It checks configuration and every read source against the existing source manifest, uses48 descending mounts, reads26 pinned sources and agrees on164 candidate records. Detailed proprietary-derived output stays ignored. Both rooms exceed the bounded triangle collider's2,048-triangle limit; the hall fits and is the first viable original movement slice. Full-room integration needs conservative acceleration and explicit work budgets.

## Next probe

Use hall6's original eligible geometry in a bounded movement query, recording contact and valid supported rollback. In parallel with implementation progress, trace the original movement caller's selector assignment before describing selector1 as original actor authority. Preserve shader/appearance transforms, floor support and owner changes independently of rendering visibility.

## Original hall extraction follow-up

[Original hall checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_HALL_COLLISION.md) confirms owned extraction, stable source IDs and explicit degeneracy accounting. `FromInvariantCandidates` rejects differing candidate predicates; `StaticMeshCollisionGeometryReader` consumes supplied shader flags and an explicit selector. Hall1,984 nondegenerate triangles match independent raw bytes and a canonical hash. Original movement probes validate supported stops with explicit modern feet clearance; they do not establish original actor selector authority.

## Complete original sets and actor participation risk

[Full-room checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPLETE_ROOM_TRIANGLES.md) independently hashes all42,986 eligible/nondegenerate triangles, validates eight original portal requests and exposes0.1 m render-floor ramps above flat FLOR. The conservative box guard stops at those rises; original actor participation/contact-height policy remains Unknown. Eligibility for original segment queries is insufficient to assert original actor volume behavior.


## Actor extent follow-up

The [actor participation trace](GROUNDED_ACTOR_COLLISION_PARTICIPATION.md) establishes transformed collision extents in the inspected body paths. Mesh segment eligibility does not establish grounded actor volume participation. Extent source selection and controller ordering are the next bounded probes.
