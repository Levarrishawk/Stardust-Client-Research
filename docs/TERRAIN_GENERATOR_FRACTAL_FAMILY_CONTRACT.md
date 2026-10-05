# Generator-to-fractal-family publication

Current reservation behavior is superseded by the [Round52 identity/budget contract](TERRAIN_GENERATOR_IDENTITY_BUDGET_CONTRACT.md). The Round51 conservative reservation description and measurements below are historical; current reservations use actual immutable input length.

Date: 2026-10-04. `TerrainGeneratorFractalFamilies.Bind` publishes an immutable owned family set from an existing [TerrainGeneratorInput](TERRAIN_GENERATOR_INPUT_CONTRACT.md). It selects the sole `FractalMap` role, requires recognized exact MGRP/0000, and delegates raw family decoding to the [frozen family set](TERRAIN_FRACTAL_FAMILY_SET_CONTRACT.md). It performs no asset I/O, callbacks, sampler creation or runtime map operation. [The checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R51_GENERATOR_FRACTAL_FAMILIES.md) records the bounded evidence and validation.

## Selection and provenance

The existing generator parser requires five ordered groups; absence is rejected by that parser before a valid descriptor can exist. Bind additionally rejects an absent or multiple FractalMap role defensively. It rejects unsupported map versions and malformed family grammar, without an implicit empty or sentinel fallback. An explicitly encoded empty MGRP/0000 is accepted as an empty set. Optional Bitmap-role MGRP is distinct and is not treated as another FractalMap. These are modern policies, not claims about original failure or reload behavior.

The result exposes `Families`, exact complete `GeneratorSha256`/`GeneratorBytes`, `GroupSha256`/`GroupOffset`/`GroupBytes`/`GroupVersion`, actual `EncodedCopyBytes`, conservative `ReservedEncodedCopyBytes`, retained `SnapshotBytes`, actual logical `Work` and `ReservedWork`. Group offsets are relative to TGEN, not PTAT. The selected encoded bytes must equal the corresponding region of the owned generator copy; the frozen family decoder supplies the group hash. Root and group copies are temporary and never exposed or retained by the result. Owned family name/raw exports remain independent, and Find preserves stable entry identity.

## Copy reservations and budgets

The frozen descriptor exposes `CopyGeneratorForm`, but no zero-copy root length/hash. Its parser hard-caps root input at 67,108,864 bytes. The bridge therefore reserves that full 64 MiB bound before requesting a root copy. This is conservative: a small root still requires the reservation. The result reports actual copied lengths separately; it does not pretend the reserve was physically allocated. A future descriptor identity API could reduce this reservation in a separately scoped change.

Default outer limits are section bytes 1,048,576, encoded-copy allowance 83,886,080, snapshot allowance 33,554,432 and work allowance 117,440,512. Their hard maxima are respectively 16,777,216, 83,886,080, 33,554,432 and 117,440,512; zero is valid but rejects any corresponding required charge. Nested family limits retain all frozen hard bounds and are validated before copies.

Before root or section copying, the bridge checks the recognized section extent and record count, nested input/entry/embedded/name/snapshot/work aggregates, and outer copy/snapshot/work allowances. For exact grammar with n records and B group bytes, terminated-name bytes must equal B−24−108n. Embedded bytes are 84n; retained snapshot bytes are names+136n; family logical work is B+84n. These are necessary framing/accounting checks; the family decoder subsequently validates each actual name and complete grammar before its own payload snapshots. Per-name policy is validated before the section copy; actual per-name sizes are inspected by the frozen decoder within the already-authorized encoded copy.

Reserved copies are 64 MiB+B. Reserved work is 64 MiB+B+family work. Actual copies are generator bytes+B; actual work is generator bytes+B+family work. Snapshot charge covers the new family's logical owned buffers, not CLR object overhead, temporary root/group copies, or memory already owned by the caller's descriptor. The result does not retain the descriptor. Work is deterministic logical accounting, not an instruction or heap measurement.

Cancellation is checked before selection/copy, between root and section phases, within frozen decoding, and after result construction immediately before publication. Deterministic fixtures cover pre-cancellation; late checks are verified by inspection, without a scheduler-specific timing claim. Every failure leaves prior results unchanged. Since the descriptor is immutable, the bridge has no borrowed mutable input span to stabilize during binding.

## Selected original and next boundary

The single pinned Tatooine TGEN is 139,608 bytes, SHA-256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`. Its MGRP at offset 6602 is 5,633 bytes, SHA-256 `A221167452D8D67719BD72A3DB30CC10B7B652881B935D3571B9D55328B444A1`, with 43 families. Binding charges 145,241 actual copy bytes, 6,813 retained snapshot bytes and 154,486 logical work; reservations are 67,114,497 copy bytes and 67,123,742 work units. Independent raw parsing compares every family ID, terminated name byte, embedded word, offset and hash. Original names/words remain private; no second terrain or corpus survey is included.

[Invoke-GeneratorFamilies.ps1](../poc/round51-tests/Assets/Invoke-GeneratorFamilies.ps1) requires explicit offline original/Python/static-packages/cached-NuGet inputs and a fresh ignored private output root. It checks lexical reparse ancestors before creation, regenerates the static/raw oracle, builds actual Core, compares the bridge plus frozen family fixtures, runs a focused conditional sampler gate and the default Core smoke.

Next: select an owned entry and construct a sampler under named controls, cache and limits. Original map quiescence, reload safety and generation-request meaning remain Unknown.
