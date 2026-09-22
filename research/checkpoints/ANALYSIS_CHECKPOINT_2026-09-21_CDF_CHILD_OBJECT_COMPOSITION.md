# CDF Child Object Composition (`CHLD` / `CHL2`)

Date: 2026-09-21

## Result

This block resolves the two common child-composition records and their shared runtime representation. Across the effective configured corpus, `CHLD` appears 5,985 times in 615 files and `CHL2` appears 1,477 times in 407 files.

## Shared serialized layout

Both chunks consist of one NUL-terminated logical resource path followed immediately by the same fixed 68-byte numeric payload:

1. `Vector3` local translation;
2. three `float32` local rotation angles, converted by the loader into the engine's angular units;
3. `Vector3` continuous angular velocity (RotationDynamics);
4. `Vector3` seesaw axis/frequency vector;
5. `float32` seesaw angular amplitude;
6. `Vector3` spring direction/amplitude vector;
7. `float32` spring oscillation frequency.

Corpus chunk lengths independently confirm this boundary. For example, garden `CHLD` records are 116 bytes with a 48-byte NUL-terminated appearance path plus 68 numeric bytes. A `CHL2` record in the same file is 144 bytes because its object-template path occupies 76 bytes, again leaving exactly 68 numeric bytes.

The exact historic labels for vectors A/B/C and their paired scalars are not embedded in the executable. Their runtime effects are confirmed below; invented names would be misleading.

## Resource-type distinction

- `CHLD` resolves its path through the appearance-template loader and retains the resulting appearance template.
- `CHL2` resolves its path through the object-template registry/factory and retains a client object template.
- A `CHL2` record that cannot resolve its object template is discarded after diagnostic/cleanup; `CHLD` retains its appearance-oriented path.

Both variants are normalized into the same child-record vector at `ClientDataFile +0x20`. `WING` also uses this generalized child structure, adding fields not present in the basic 68-byte child payload.

## Runtime construction

The first phase of `ClientDataFile_applyToObject_VA_00604420` iterates the shared child vector:

1. If an object template is present (`CHL2`), invoke its factory to create the full client object and run its initialization virtual.
2. Otherwise create a generic client object and install the referenced appearance template (`CHLD`).
3. Apply the local translation and three rotations when the record is a direct-transform child.
4. For hardpoint-mode extended records, resolve the named hardpoint against the target appearance and copy that transform instead.
5. Add the child to the live object system.
6. Select at most one motion controller in priority order:
   - nonzero vector A -> `RotationDynamics`, applying each component times frame delta as continuous local angular velocity;
   - otherwise nonzero vector B -> `SeesawDynamics`; vector direction selects the principal rotation axis, vector magnitude controls sinusoidal frequency, and scalar B is symmetric angular amplitude;
   - otherwise nonzero vector C -> `SpringDynamics`; normalized vector selects translation direction, vector magnitude is displacement amplitude, scalar C is sinusoidal frequency, and the child translation is the equilibrium point;
   - otherwise an extended-record scalar branch used by `WING`.
7. Attach/release through the engine's normal object ownership path.

The mutually exclusive controller chain is important: these vectors are not three simultaneous transforms.

## Representative assets

`clientdata/building/shared_corl_garden_lrg_02.cdf` contains 30 `CHLD` records and one `CHL2` record:

- `CHLD` appearance children include `appearance/thm_corl_imprv_bridge_insert_s01.apt`;
- the `CHL2` child is `object/static/terrain/corellia/shared_rock_crystal_geyser_agrilat_small.iff`.

This confirms the architectural distinction: lightweight visual inserts can be appearance-only children, while a child that needs object-template behavior is instantiated as a complete client object.

## Replacement-client consequence

- The scene graph must support both appearance-only child nodes and full object-template child instances.
- Preserve the complete 68-byte numeric contract as explicit typed fields even before every motion scalar has a historical name.
- Implement the controller selection as a validated tagged mode in the modern representation; do not carry three ambiguous nonzero vectors into runtime.
- Loading a child object template may recursively create properties and client behavior, so it cannot be reduced to mesh instancing.
- Use handles/owned scene nodes in x64 code; the original shared 100-byte runtime record and raw cached-template pointers are not file or wire formats.

## Confidence

- String plus fixed 68-byte layout: confirmed by loader and corpus arithmetic.
- `CHLD` appearance-template versus `CHL2` object-template semantics: confirmed.
- Common vector and runtime apply path: confirmed.
- Controller class identities and behavioral field roles: confirmed by RTTI and update equations. Exact designer-facing field spelling and angular unit labels remain unknown.
