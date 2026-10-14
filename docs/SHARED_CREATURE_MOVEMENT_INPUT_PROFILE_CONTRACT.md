# Owned creature movement-input profile

`SharedCreatureMovementInputProfile.Load` is an engine-neutral, bounded reader of authored template inputs. It follows a pinned SCOT root and its necessary parents, retains the five selected serialized records and provenance, and exposes a numeric input only for the nearest mode-1, space-modified, finite literal. It does not convert angles, sample RNG, simulate movement, seed CREO4, select runtime defaults or establish absence of later writers.

The authoritative serialized boundary is the [Round45 input contract](CREATURE_TEMPLATE_MOVEMENT_INPUT_CONTRACT.md); conditional runtime getters remain in the [Round44 contract](CREATURE_VIEW4_INITIALIZATION_CONTRACT.md). The implementation (external historical reference), scenarios (external historical reference) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R46_CREATURE_MOVEMENT_INPUT_PROFILE.md) keep this increment separate from collision/name profiles and recipient storage.

## API and tagged results

Load accepts a borrowed `Func<string,TreLookupResult>`, logical root path, expected SHA-256, optional limits and cancellation token. It reuses `LegacyAssetPath.Normalize`, `OwnedAssetSourceCache` and `SharedObjectTemplateDecoder`. Root identity mismatch, malformed hierarchy, missing/masked parent or cycle aborts the whole load. The result reads the complete necessary chain; finding five root literals does not bypass parent integrity checks.

The immutable profile exposes root, visits, source identities, accounted source/decoded bytes and work, plus these slots:

| Slot | Operational policy |
|---|---|
| `speed[0]` | nearest loaded mode-1 space literal; separate from speed[1] |
| `speed[1]` | same policy, retaining its own record/provenance |
| `slopeModAngle` | raw authored literal; no degrees/radians conversion |
| `slopeModPercent` | raw authored literal; no percentage arithmetic |
| `waterModPercent` | raw authored literal; no physical or simulation constraint |

Every input has `Kind`, nullable `Value`, optional source field and provenance. `Literal` means a supported authored input; authored positive/negative/zero values are not replaced with defaults. `Absent` has null value and no winning field. `Unsupported` has null value and the winning field/raw record: sampled modes 2/3, relative modifiers and other non-space modifier bytes do not fall back to a farther literal. `Operational` is true only when all five inputs are `Literal`. There is no conversion from this profile to received or full baseline state.

Visits retain source path, SCOT version when present, parent and five optional raw parameters. A present mode-0 record remains distinct from a missing parameter; both continue inheritance. Parameters expose mode, relative byte, byte length, optional raw literal bits and `CopyEncoding()`. They do not expose a public numeric operand for unresolved relative structure. Copied encodings cannot mutate retained bytes. Read-only visits/dictionaries and copied immutable source identities remain stable if resolver buffers change later or another load runs.

## Encodings and unsupported inputs

The decoder uses the accepted mode-0/1/2/3 widths and order from Round45. It structurally consumes nested weighted entries and range endpoints, preserving the outer selected encoding without evaluating weights, RNG, ranges or relative expressions. Mode 0 consumes two bytes; mode 1 consumes two header bytes plus F32LE; mode 2 consumes headers/count/weight-and-nested records; mode 3 consumes headers/two F32LE endpoints. Unsupported serialized modes, truncation, trailing bytes and nonfinite literal operands reject in this modern slice. Range endpoint bits remain uninterpreted under an unsupported tag.

Speed has a signed little-endian count. The reader assigns only the first two records, consumes and validates bounded surplus records and never creates speed[2]. Zero count and a short array leave absent slots; negative counts reject. Original nonpositive-loop and unsupported-mode behavior differ from these explicit modern rejection policies; no original malformed-input safety claim is made.

## Limits and ownership

Defaults: depth/resources 16, per-template/source aggregate 1 MiB, selected parameter stream 64 KiB, nesting below 16, array/list count at most 64, decoded-output/work accounting 8 MiB each. Caller ceilings are validated before resolution; sources remain bounded before cache clone and accepted decoder allocation. Node/version/count/name limits of the existing shared decoder also apply.

Decoded accounting reserves twice each source byte length for decoder copies, fixed metadata units for visits/inputs/retained parameters, raw retained encoding bytes, complete visit/provenance reference capacities and source snapshot entries. Reservations occur before the corresponding owned allocations. Work counts source bytes, parsed header/scalar/count bytes and slot/provenance scans. These are deterministic accounting budgets, not an exact managed-heap measurement. Resolver-owned allocations remain the resolver's responsibility; this reader bounds what it imports and retains.

Each load has a fresh owned cache and local budget. Cancellation is checked before resolution, after callbacks, during parameter/provenance loops and before publication. A same-thread nested load marks the active frame violated; even a caught nested exception makes the outer load fail. Finally cleanup clears the frame, so a later independent load can succeed. Cancellation does not claim interruption inside the bounded existing synchronous decoder. No partial profile is published on failure.

## Evidence and validation

**Observation, Confirmed:** one configured original human chain produces five source identities totaling 8,290 bytes and exactly agrees with a fresh independent Round45 oracle for every retained selected encoding, mode, relative byte, source identity and winning provenance. Original-derived numeric constants/payloads live only in private receipts. **Interpretation:** a determinate authored-input profile can be built from these five source records. **Confidence:** Confirmed for this pinned chain and implemented modern policy; final runtime values remain Unknown.

Native checks cover 352 synthetic fixtures (16 positive, 336 negative), 16 snapshot-isolation checks, eight lifecycle/recovery checks and three aggregate boundary checks. The independent peer (external historical reference) agrees on 573 checks. The actual Core runner and default native smoke build without warnings/errors. Root/configuration pins and the original five-source result agree with the existing 50-anchor loader audit. Sources/fixtures and detailed receipts remain private; no original executable execution or live Stardust-server contact occurred.

Reproduce with the own runner project (external historical reference), cached locked offline restore, then `Networking.dll ASSET_ROOT CONFIG SYNTHETIC_FIXTURES PRIVATE_NATIVE_RECEIPT`. Generate/verify fixtures with `peer.py`; compare against a fresh `verify_creature_template_movement.py` receipt. This opt-in runner does not modify existing smoke dispatch or production consumers.

Next action: decide an explicit consumer of authored inputs without turning them into replicated defaults. Original installation/use, x87 angle conversion and Round44 customization/event transitive writers remain separate probes; this profile closes none of those runtime paths.

## Round47 compatibility increment

An [immutable authored consumer](SHARED_CREATURE_AUTHORED_MOVEMENT_INPUT_CONTRACT.md) now provides explicit atomic named-slot selection and a guarded planar adapter. The [72-case source join](EXPLICIT_AUTHORED_RECEIVED_MOVEMENT_JOIN_CONTRACT.md) preserves missing received speed even when authored values are determinate. Effective original arbitration/defaults/angle/relative/RNG evaluation remain open.

## Round49 compatibility increment

[Original-resource sourced preview](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md) passes authored/received headless and authored GPU joins through the actual original recipe closure with zero extra movement reads. [Current raw inputs](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md) retain explicit constructor provenance and exact received overrides without store seeding; [explicit stop and sourced input](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md) own the modern direction/clock lifecycle. Original full arbitration, modifier/vehicle semantics and units remain separate.
