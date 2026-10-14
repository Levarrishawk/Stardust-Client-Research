# Explicit offline scenario authoring

scenario_author.py (external historical reference) exposes `create(base_argument_file, profile_file, runs, output) -> producer_receipt` and a CLI:

```powershell
python poc/round82-tests/Networking/scenario_author.py --base-arguments <base-arguments.json> --profile <configured-Version13.json> --runs 3 --output local-private/scenario-review
```

This authors modern synthetic configuration. It does not recover protocol, extract assets, launch a scene, create a socket, or change source inputs. It imports only the frozen Round81 reseeder (external historical reference), which reuses the independent SOE encoder and checks every inherited protected field. It does not import the new runtime scenario parser.

## Admission and path ownership

The base input is one JSON string array with exactly16 required value arguments, one `--terrain-point-target-profile` value, and zero to eight optional Boolean flags. Unknown/duplicate/incomplete arguments, nonstrings, flag values such as `false`, and missing inputs reject. Optional flag presence means true; absence means false. All eight options are emitted explicitly. Base JSON is bounded262144 bytes; source profile16384 bytes. JSON uses strict UTF8 (an existing UTF8 BOM is accepted), duplicate-property rejection at every object, no nonfinite constants and maximum four container levels checked before parsing.

Required input order: asset-root, asset-config, mixed-peer, catalog-profile, motion-peer, face-profile, customization-peer, customization-dictionary, customization-asset-context, customization-name-context, tangible-peer, tangible6-peer, creature6-full-peer, creature4-partial-peer, terrain-peer, terrain-material-profile.

Option order: local-input, customization-wire, customization-scalars, customization-full, customization-extended, customization-named, terminal-animation, terminal-clock.

Every base path resolves against the **base argument file's parent**, never process cwd or output directory. The CLI/API's explicit base/profile filenames themselves follow ordinary caller path resolution. All values require nonempty strings of at most4096 characters, no NUL or URI-like resource/network scheme. Rooted local Windows drive paths are accepted; drive-relative paths and UNC/network paths reject to preserve offline I/O. Asset-root must be a directory; every other input and the base point-profile template must be an existing file. The producer reads/hashes only explicitly supplied files; it does not enumerate the asset directory. Inputs may reference public configuration profiles and ignored/private asset fixtures; output always stays under this repository's ignored `local-private`.

The explicit `profile_file` is authoritative and replaces the base point-profile template. It may differ intentionally (for example, switching from replay to human inspection). Both source and template paths/hashes are recorded, and both remain unchanged. Frozen81 admission/reseeding validates Version13 policy, settings/adapter consistency and all protected source fixtures; stale seeds, malformed correction/ACK/outer-Multi fields and obsolete/unknown SOE fields reject. Full child behavior admission remains the existing configured host's responsibility; the producer does not claim to validate all gameplay schedules or asset contents.

## Output and reproducibility

RunCount requires an exact integer1..32, excluding Boolean/coerced values. Run i (one-based) uses `(sourceConnectionId+i) mod 2^32` and `(sourceSeed+i) mod 2^32`. These are distinct across the bounded run set and differ from the source, including wraparound. The reseeder preserves application/object/behavior fields and regenerates all protected seed/identity-dependent fixtures. Native outbound sequence0 remains a fresh-child responsibility, already verified in Round81; this author does not execute transport.

Outputs are `run-01.json` through `run-NN.json`, `scenario.json`, and `producer-receipt.json`. The scenario has exactly `{Schema: 1, RunCount, Inputs, Options, PointTargetProfiles}`. Inputs are absolute resolved strings in the order above; Options are strict Booleans in the order above; PointTargetProfiles are relative filenames in run order, resolved by consumers against the manifest directory. No raw argument overrides or transport implementation are embedded in the schema.

Receipt records base/source/template hashes, each explicit file input hash (asset-root records directory existence without corpus enumeration), expected per-run IDs/seeds, independently generated handshake hex, serialized profile hashes and scenario hash. It labels synthetic authoring, no socket and no source mutation. Profiles cap16384 bytes; manifest/receipt each cap262144 bytes. Hashes cover exact emitted UTF8 bytes including final newline.

Every argument/path/source fixture, every authored profile, all output bytes/size limits and receipt hashes are computed **before the first output write**. Only then does the reused fresh-private guard check lexical symlink/reparse ancestors, private containment and nonexistence, and create the output directory. Existing directories reject; files use exclusive creation. Validation failure leaves no output or new parent directory. Unexpected storage failure after admitted writing begins may leave partial ignored output; there is no destructive cleanup or crash-atomic transaction claim. Fix the cause and choose another fresh output directory.

## Verification

replay.py (external historical reference) passes412 checks/44 rejection cases with authored synthetic admission inputs, runs1/3/32 and U32 wraparound. It independently checks schema/order/options/resolution, identities/handshakes/hash bytes, preserved application fields, reverse regeneration and stale-source ACK CRC rejection. Malformed/duplicate/unknown/missing/type/depth/byte-cap/path/profile cases reject before output exists; an injected late manifest-cap rejection also proves the pre-write boundary. Old base/profile/other explicit fixture hashes remain unchanged.

```powershell
python poc/round82-tests/Networking/replay.py --profile <accepted-private-Version13-profile.json> --output local-private/scenario-tests
```

A real-input CLI authoring run also created three profiles using accepted local asset/actor/terrain input paths and a non-replay configured profile, without asset extraction or native execution. Runtime parser/launcher integration and native scene consumption are lead-owned. [Dated evidence and next action](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R82_SCENARIO_TRANSPORT.md).
