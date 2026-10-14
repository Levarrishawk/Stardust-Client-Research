# Explicit scene object resource routing

R13-A,2026-10-02. [SceneObjectResourceRouter](../../SWG-Godot-Client/poc/Stardust.Core/Assets/SceneObjectResourceRouter.cs) is an engine-neutral synchronous composition of the two accepted catalog recipes. It adds no world publication, initializer adapter, render abstraction, runtime switcher or implicit fallback/global catalog.

## API and discriminator

```csharp
SceneObjectResources SceneObjectResourceRouter.Load(
    SceneCreateObjectByCrcMessage create, ObjectTemplateCatalog catalog,
    SceneObjectResourceOptions options, Func<string, TreLookupResult> resolve,
    CancellationToken token = default);
```

The caller supplies an accepted [catalog](SCENE_CREATURE_RESOURCE_CONTRACT.md), decoded raw create, one explicit sealed option type and resolver. Catalog membership and exact option/recipe agreement reject before any source read. There is no unknown-CRC human substitution. Invalid/null inputs, nonfinite create floats, invalid option bounds/selections and pre-cancellation also reject before reads. Pins remain supplied by the catalog and selections; a syntactically valid but incorrect content pin rejects when that source is fetched.

`SceneObjectResources` is a sealed class with getter-only properties and internal typed constructors. `Kind` is the existing `SceneObjectResourceRecipe`; exactly one of `Creature` or `Static` is nonnull. Common `Create`, `Recipe`, `Sources`, `ExternalReads` and `SourceBytes` are the same accepted payload metadata, not separately recomputed versions. No public constructor or writable discriminator/payload allows callers to build a contradictory result. Source records contain normalized path, priority, encoded byte count and SHA256. The source map is the accepted readonly owned map.

The result does not retain the resolver, token, options or callback. Structural immutability and source ownership follow the existing reader contracts. Pixel/metadata `ReadOnlyMemory` remains an owned read-only view; this is not a new cryptographic/deep-immutability promise. Caller input mutation cannot change owned parsed descriptors or decoded pixels. Creating a changed record with `with` does not mutate the stored record.

## Explicit recipe options

The base constructor prevents external subclasses; callers construct the two sealed option classes. Their constructors copy mutable inputs and expose getter-only fields. Option creation must occur under caller control, without concurrent mutation of input collections.

| Option | Required policy | Retained bounds |
| --- | --- | --- |
| `SceneCreatureResourceOptions` | idle clip, exact walk/run bank, explicit `loadAlbedoFactors` boolean | resources default64/range1..4096; encoded bytes default16MiB/range1..128MiB |
| `SceneStaticResourceOptions` | explicit `SceneStaticMaterialPolicy`, explicit nullable SWTS frame selection | graph defaults64resources/16MiB, depth32/edges4096; accepted resource/bytes ranges above, depth1..32/edges1..4096; decoded bytes default128MiB/range4..128MiB |

Creature selections use the accepted normalized `appearance/lat/*.lat` path policy, SHA256 pin/name validation and at-most-eight explicit child indices0..4095. Idle, walk, run and their routes are snapshotted into an ordinal readonly dictionary/list; mutating caller banks/routes later has no effect. Existing logical-name policy is preserved (bounded printable ASCII; no new empty-name restriction). There is no automatic motion/event/animation-graph choice.

Static frame selection uses the accepted canonical `shader/*.sht` path, SHA256 and index0..255 validation. A validated immutable selection/graph limits snapshot is retained. Null frame deliberately means no selected-frame extension; it never selects frame0 automatically. `RequireComplete` and `AllowNeutral` remain explicit modern reader policies. An index below256 can still exceed a particular shader's actual frame count and reject during loading. Choosing a frame does not recover DTST timing or runtime switching.

Dispatch calls the unchanged [creature reader](SCENE_CREATURE_RESOURCE_CONTRACT.md) or [static reader](SCENE_STATIC_RESOURCE_CONTRACT.md), including the [albedo closure](SCENE_CREATURE_ALBEDO_RESOURCE_CONTRACT.md) or [selected SWTS frame extension](SWITCH_TEXTURE_STATIC_RESOURCE_CONTRACT.md). The creature reader retains its fixed32MiB decoded-image check; the router does not invent a configurable replacement. All geometry/material/fallback semantics remain recipe-specific. The selected-frame terminal has full CPU material bundles; this does not establish GPU effect completeness or original render fidelity.

## Cancellation, errors and worker ownership

Loading is synchronous; the router creates no threads, shared cache, tasks or scheduler. A caller can construct immutable options on the owning thread and invoke `Load` on a dedicated worker using a resolver created/owned there. The caller owns archive lifetime and must dispose it after load or failure. Neither archive objects nor delegates are transferred in the result.

The token is checked before catalog routing, by existing reader callbacks around source lookup, throughout accepted reader progress and immediately before router return. Cancellation during a blocking resolver waits for that call to return; this is cooperative cancellation, not forced interruption. Exceptions propagate from the accepted readers/resolver. No successful result is returned on failure, and there is no world/registry/initializer publication callback. Previously accepted results remain caller-owned. This contract makes no concurrent archive-reader safety claim.

## Fresh original proof and limits

The supplied [two-entry profile](../../SWG-Godot-Client/poc/preview-profiles/scene-object-catalog.json) and pinned installed configuration select:

| Route | CRC | Source closure | Textures/mips |
| --- | --- | --- | --- |
| CreatureDetail0 human male, albedo enabled |2937962913|39 /4,686,371bytes|10 /80|
| StaticAppearance mission terminal, strict materials, frame0 |2907007746|27 /1,401,794bytes|7 /62|

Both preserve the entire47-byte synthetic real-layout create: U64 ID`FEDCBA9876543210`, seven raw float words`80000000/3E800000/BF000000/40000000/42F70000/C2350000/00000001`, selected CRC and raw flag255. Quaternion/position values are neither repaired nor normalized/reordered. Five inherited creature templates and terminal object/inheritance/APT/LOD/mesh dependencies are checked by the accepted independent original audits. Every routed source priority/length/SHA and texture mip hash matches those independently checked closures. This is original asset/modern router evidence, not a global recipe catalog or recovered original routing/world readiness behavior.

[Native scenario](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneObjectResourceRouterScenarios.cs) passes65 rejection checks: unknown/incorrect recipes/options, null/nonfinite/canceled inputs, invalid option bounds/pins/routes/policies, missing/masked sources, wrong root/appearance/frame pins, unsupported complete-material policy, actual frame range, source/decode budgets and resolver exceptions. Preflight reads0; canceled resolvers make exactly one call per route. Exact source count/byte bounds succeed; one below rejects. All66 borrowed original buffers are cleared without changing whole-payload/pixel fingerprints; output remains valid after worker-owned archive scopes close. Dedicated workers differ from the caller thread. Source/option maps reject mutation. These are native observations; Python checks their receipts without claiming to re-execute ownership/cancellation.

The own [compact oracle](../../SWG-Godot-Client/poc/round13-tests/Assets/verify_router.py) verifies raw wire/catalog/routing/source totals and mip identity, composing accepted source/pin/inheritance/geometry/pixel auditors. Existing albedo and selected-frame regression scenarios pass16 and168 rejection cases. No whole corpus rescan, original executable/GPU/network execution or world publication is performed. [Checkpoint and commands](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R13_RESOURCE_ROUTER.md).

The shared --round13-resource-router entry registers the explicit four-input original proof. Round14's separately accepted [generic initializer/world](SCENE_MIXED_WORLD_CONTRACT.md) now adapts this discriminator with the [pure completion gate](SCENE_OBJECT_RESOURCE_VALIDATION_CONTRACT.md); [mixed original rendering](MIXED_WORLD_PREVIEW_CONTRACT.md) uses recipe-specific guarded adapters. The router itself remains synchronous and owns no world readiness/publication policy.
