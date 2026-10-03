# Completed scene resource validation

R14-A,2026-10-02. [SceneObjectResourceValidation.Validate](../poc/Stardust.Core/Assets/SceneObjectResourceValidation.cs) is a synchronous, engine-neutral consistency gate over an externally completed [two-recipe router result](SCENE_OBJECT_RESOURCE_ROUTER_CONTRACT.md). It reads no sources, publishes no result and has no registry/thread/scheduler dependency.

```csharp
void SceneObjectResourceValidation.Validate(
    SceneObjectResources result, SceneCreateObjectByCrcMessage expectedCreate,
    ObjectTemplateCatalog catalog, SceneObjectResourceOptions options);
```

Success returns normally; rejection throws `InvalidDataException`. Null caller arguments throw `ArgumentNullException`. Malformed descriptor argument/null/index/overflow failures are converted to `InvalidDataException` with the inner failure retained. The validator does not modify, clone, transfer or make an invalid result safe. It does not retain callbacks, caches, tokens or results. Input collections are assumed to be passive owned collections from accepted readers; hostile custom collection callbacks are outside this contract. The caller serializes ownership while validation runs and applies generation/incarnation/freshness guards separately before publication.

## Identity and source consistency

The expected transform must be finite. Exact encoded47-byte create equality checks U64 ID, all quaternion/position raw float bits, CRC and raw flag; negative zero is distinct from positive zero. Values are never normalized or repaired. Catalog lookup, selected recipe, option discriminator, exactly-one payload and payload/common metadata must agree. There is no fallback catalog or default recipe.

Common sources must be nonempty, within the requested count/encoded-byte bounds, and match ExternalReads and SourceBytes. Every key equals its canonical logical path/identity path; sizes are positive and hashes are64 hex digits. Sum uses checked64-bit arithmetic. Root, appearance, requested clip and selected SWTS pins are checked. Static SWTS content pin comparison preserves the accepted case-insensitive hex policy. Nested source maps require exact record equality, including priority/size/hash, against common metadata. Static profile+appearance source-key union must equal the common map.

These checks compare supplied metadata and available cross-references. They cannot regenerate every encoded source hash from decoded pixels, or discover omitted/unused original dependencies without source I/O. Hash syntax or matching records is not attestation that a worker fetched those bytes. An internally consistent forged source/pixel graph is not made trustworthy. Encoded origin and archive precedence are independently verified by the original-byte oracle in the fixture proof; callers still trust the accepted readers for production provenance.

## Creature gate

The accepted [creature initializer](../poc/Stardust.Core/Session/SceneCreatureResourceInitializer.cs)'s private `ValidatePrepared` checks are preserved in the new helper: source accounting and root/appearance pins; owned skeleton/detail0/skin/geometry/LAT/material/palette closure; selected idle identity and explicit child route; pinned prepared idle/walk/run clips, positive rates/end frames and movement locomotion. Existing initializer code is unchanged; there is no reflection or registry invocation in product validation.

Additional checks require exact albedo-presence agreement with `SceneCreatureResourceOptions.LoadAlbedoFactors`. Owned logical tables re-resolve the requested proxy/child route using the accepted lookup/decoder, and match prepared path/time scale. Palette shader metadata/factor/selection fields agree across independently parsed descriptors. The accepted palette query recomputes authored selections from owned palettes, and the accepted factor helper checks resulting colors. Program/material key closure, effect candidate/program/factor tags and source presence must agree. This does not execute lighting or animation scheduling.

DDS mip extents and exact RGBA byte lengths are checked without decoding. Aggregate creature decoded bytes remain32MiB. No unrequested face/blend/shape/custom-color/texture-choice/compressed-normal/vertex-lit/tangent extensions are accepted: the R13 options do not specify those operations. A later separately specified validation mode would be needed for such transformed results.

## Static gate

The result must retain the exact requested material policy and root/appearance pins, coherent inheritance/provenance and nested source identities. Geometry root matches the selected appearance. Traversal checks graph depth/edge/resource limits, unique child indices, cycles/contradictory shared nodes, exactly one geometry family, finite local transforms and nonempty mesh closure. Buffers require bounded primitives/vertices, triangle indices in range, finite positions/normals/UV0 and source-backed shaders. Independently decoded material/geometry copies use the accepted geometry fingerprint helper rather than reference identity.

Material keys and shader/effect/named-texture dependencies must agree. `RequireComplete` requires every geometry mesh and no material diagnostics; `AllowNeutral` retains its distinct accepted policy. Selected-frame metadata must equal the explicit selection, reference its pinned SWTS source/base source, stay within actual frame count and bind the selected named texture/tag. A supplied frame must occur; a null frame must not conceal a switched material. Missing, changed or contradictory frame metadata rejects. Runtime DTST behavior and GPU effect completeness remain separate.

Shared images must be the same canonical owned objects for a repeated path. Image dimensions, mips/byte lengths and deduplicated decoded totals are checked against caller bounds. The current static gate covers RGBA8 results of the accepted terminal recipe; it explicitly rejects nonempty HDR maps pending a separately specified validation extension. This does not remove HDR support from readers or change HDR decoding.

Validation adds conservative modern bounds: static primitive count≤1,024, vertex count≤65,536 per primitive,≤256 materials/texture bindings, source-bounded material image/effect maps;≤1,048,576 accumulated geometry/material work entries (both retained geometry copies count); logical lookup entries≤65,536; effect candidate tables≤64×64. Palette/controls keep the accepted bounds. These are completion-validation policy, not recovered original rejection behavior or a guarantee that every otherwise-readable corpus asset fits this gate.

## Evidence and reproduction

Fresh read-only installed input uses the existing two-entry pinned catalog, configured overlay and accepted router. Human albedo:39 sources/4,686,371bytes,10textures/80mips. Terminal frame0:27 sources/1,401,794bytes,7textures/62mips. Both pass validation after all66 borrowed buffers are cleared and archives close; valid payload fingerprints remain unchanged. Validation also runs on dedicated threads with no registry. Exact source limits pass; one below rejects.

[Native scenarios](../poc/Stardust.Core.SmokeTests/SceneObjectResourceValidationScenarios.cs) pass89 rejected cases and seven positives: altered every create field/raw-zero bit; nonfinite placement; wrong catalog/recipe/options; source accounting/path/hash/null/missing/extra cases; absent descriptor/clip/material data; albedo color/option and movement route faults; static policy/frame/nested-hash/cycle/placement/decoded-limit faults. Test-only reflection invokes existing internal typed router-result constructors around changed public payload records; no arbitrary result constructor or production hook was exposed.

[Compact oracle](../poc/round14-tests/Assets/verify_validation.py) composes accepted router/source/pin/inheritance/geometry/pixel auditors and checks both validated metadata sets and142 actual mip hashes. Router/albedo/SWTS regressions pass65/16/168 negatives. Python checks native fault/ownership/no-I/O/thread receipts without claiming to execute those C# behaviors independently. No corpus rescan, original executable/GPU/live-network execution occurs. [Checkpoint and exact commands](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R14_RESOURCE_VALIDATION.md).

Lead integration should register the explicit original validation scenario and call this gate at a separately accepted delivery/publication boundary. The current milestone changes no initializer/world delivery API and relies on no unfinished same-round implementation.

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

Round29 explicit static SwitchFrames opt-in validates immutable source/binding/selected-frame proof without copying bundle pixels. [Contract](SCENE_STATIC_SWITCH_FRAME_RESOURCES_CONTRACT.md). Resource initializer now preserves this optional policy through its owned option copy.
