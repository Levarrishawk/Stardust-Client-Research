# Scene object logical resource accounting

## Contract

[SceneObjectResourceAccounting.Measure](../../SWG-Godot-Client/poc/Stardust.Core/Assets/SceneObjectResourceAccounting.cs) is a pure engine-neutral measurement of an existing `SceneObjectResources`. It returns a sealed, get-only `SceneObjectResourceAccountingReceipt` with `Sources`, `SourceBytes`, `DecodedTextures`, `Mips`, `DecodedTextureBytes` and checked `PendingWeight = SourceBytes + DecodedTextureBytes`.

This is a logical queued-resource budget. It excludes geometry, animation, descriptor/collection/CLR storage, allocator overhead, GPU storage and transient decoding buffers. The same encoded asset may be logically charged by more than one queued object. It is not a heap estimate, resident-memory measurement or allocation promise. The receipt retains only scalar values; it does not retain the measured result or decoded arrays.

No resolver, file, stream, decoder, engine, scheduler, cache or callback is called. Measurement reads passive owned descriptor metadata and memory lengths; it does not copy or scan pixel payloads. A temporary path/reference map supports deduplication. The caller must serialize access to mutable descriptors and separately perform [compatibility validation](SCENE_OBJECT_RESOURCE_VALIDATION_CONTRACT.md) and publication-currentness checks. This helper does not replace those gates.

## Counting and identity policy

Common source entries are counted once by their source dictionary key. Key/identity path must agree, byte sizes must be positive, and checked sums/counts must equal the completion's `SourceBytes`/`ExternalReads`. Source hashes and source origin are not attested here.

Creature textures come from its material bundle. Static textures come from every retained mesh's RGBA and existing HDR texture maps. A canonical reader texture path is the logical identity; distinct paths count separately even if they refer to one image object. Repeated paths count once and must refer to the exact same decoded image instance. Distinct instances for one path reject conservatively, including equal-looking metadata. This matches the accepted static validator/cache identity rule and rejects inconsistent duplicate metadata without scanning pixels. RGBA/HDR collisions at one path reject.

Every texture path must occur in the common source map. Decoded mip lists must be nonempty; each mip must exist with positive width/height and a length matching checked width×height×4 for RGBA or ×6 for existing RGB-half descriptors. Count and byte sums use checked arithmetic. HDR counting is only length accounting over the already exposed DTO; it establishes no new parser or compatibility acceptance. The current pure compatibility gate still rejects static HDR.

Null caller input throws `ArgumentNullException`; malformed represented accounting metadata throws `InvalidDataException`; checked arithmetic overflow throws `OverflowException`. No partial receipt escapes. Collection callbacks, concurrent mutation and adversarial custom enumerators are outside the passive-descriptor contract. Measurement does not mutate or dispose its argument.

## Evidence and consequences

[Native fixture](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneObjectResourceAccountingScenarios.cs), [independent header oracle](../../SWG-Godot-Client/poc/round16-tests/Assets/verify_accounting.py) and [dated checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R16_RESOURCE_ACCOUNTING.md) reproduce these **Confirmed modern observations**:

| Original closure | Sources | Source bytes | Unique textures | Mips | Decoded texture bytes | Pending weight |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Human detail0/albedo | 39 | 4,686,371 | 10 | 80 | 14,330,528 | 19,016,899 |
| Terminal frame0 | 27 | 1,401,794 | 7 | 62 | 2,140,836 | 3,542,630 |

Both queued together have logical weight22,559,529 bytes. The original closures/options are exactly those in the [retention fixture](SCENE_OBJECT_RESOURCE_RETENTION_CONTRACT.md). Native tests measure after original overlays close and borrowed arrays clear; repeated owner/worker measurements agree, cause zero new resolver calls and leave complete resource fingerprints unchanged. Synthetic positives cover repeated texture identity across mesh descriptors, existing HDR six-byte accounting, immutable receipts and unchanged length accounting after content mutation. Fifteen negatives cover null input/image/mip, empty mips, invalid sizes, dimension overflow, source total/count/path/size disagreement, distinct duplicate image identity/metadata, missing source and RGBA/HDR identity collision.

The Python oracle verifies all66 original source hashes/priority/length identities. From the same17 original DDS headers, it independently computes every mip dimension and RGBA byte length, verifies142 mip length receipts and mechanically derives both totals above. It does not decode pixels, introduce new asset formats or perform a broader corpus census. Pixel/hash correctness remains prior accepted reader evidence. Native reference/mutation/exception behavior is not independently executed by Python.

Receipt and completion constructors are sealed/internal as recorded in the existing gate fixture. Tests use reflection only to wrap copied typed payload records; no production construction/mutation hook is added. Valid accepted descriptors cannot practically reach long-total overflow under their bounded collections; a representable maximal mip width/height exercises checked dimensional overflow. PendingWeight overflow remains guarded in code but is not reached through a valid public reader-produced result in this fixture.

Lead integration may inject `r => SceneObjectResourceAccounting.Measure(r).PendingWeight` into the separately owned runtime pump's budget delegate. This assignment does not edit or implement the pump/interface, its lifecycle or queue policy. Modern logical scheduling policy makes no recovered legacy memory/timing claim.

## Round19 explicit face completion

[Opt-in resource route](SCENE_CREATURE_FACE_RESOURCE_CONTRACT.md) addresses the
default route's missing generated face with supplied inspection values.
[Requested completion and logical accounting](SCENE_CREATURE_FACE_COMPLETION_CONTRACT.md)
require exact values/binding/dependencies and account unique source-path inputs
plus generated image bytes. Equivalent separately decoded face cross-map inputs
must match metadata/all pixels; contradictory aliases reject. GeneratedFaces and
GeneratedFaceBytes receipt fields are zero for default/static resources.
[GPU inspection](SCENE_CREATURE_FACE_INSPECTION_CONTRACT.md) verifies actual head
MAIN binding and scoped pixel change. Original player customization/display parity
remain unknown; this does not infer full replicated appearance behavior.

Round29 explicit static SwitchFrames opt-in charges every retained bundle image/mip as well as selected appearance images, including duplicate frame0 representation; unique encoded sources stay charged once. [Contract](SCENE_STATIC_SWITCH_FRAME_RESOURCES_CONTRACT.md). Additive zero receipt fields are normalized by round29 Assets oracle before the unchanged round16 oracle.
