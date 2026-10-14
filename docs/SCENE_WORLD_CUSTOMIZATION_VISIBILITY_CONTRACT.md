# Current visibility and hidden appearance lifecycle

[SceneWorldObjectVisualFactory](../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldObjectVisualFactory.cs) now reads `snapshot.Replication.TangibleScalars.Visibility` at creature creation and refresh instead of historical `Baseline.Visibility`. [SceneCreatureTangibleStatePresentation.IsVisible](../../SWG-Godot-Client/poc/Stardust.Godot/SceneCreatureTangibleStatePresentation.cs) maps zero to hidden and every nonzero byte to visible. It neither changes the raw byte nor interprets complexity, volume, options, use count, or condition. Height remains in the separate creature scalar/transform presentation boundary. All existing publication/create/envelope/current-state/generation/incarnation guards remain unchanged.

## Accepted test boundary

The separate `--customization-visibility-race` mode in the [standalone preview](../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldCreatureCustomizationPreview.cs) is mutually exclusive with Height/container race modes. It uses **complete synthetic baseline reseeds**, which the accepted world/store permits. It does not use unfinished extended codec/runtime APIs or claim visibility-delta application. Reseeding replaces the complete historical baseline/current typed state, retaining the incarnation and resource publication while advancing replication revision. This would reset existing overlays; these fixtures deliberately keep all other fields identical. Actual common-field delta preservation is the later runtime/lead join.

The [producer](../../SWG-Godot-Client/poc/round24-tests/Rendering/Generate-Peer.py) extends Rendering23 fixtures. Hidden/shown packets differ from the accepted106-byte complete baseline only at visibility byte71, with values0/255. Field order/envelope and other raw fields remain exact. The original baseline uses visibility1. No original/player/server state is inferred from these synthetic packets.

## Controlled lifecycle

The actor is created hidden from a complete visibility0 baseline. Its initial supplied appearance revision1 commits while hidden. Reseeding visibility1 shows it without reloading. Native motion selects a paused walk pose at frame3, origin(-1.5,0.2,-0.1), identity basis/Height1; a visible-zero capture establishes the fixed comparison pose.

Visibility0 hides the actor again before noncooperative CPU revision2 is submitted. Pending revisions3/4 coalesce while hidden, with peak active composers1. Held and committed receipts keep raw visibility0 and root visibilityfalse; revision4 commits the varied generated MAIN texture while still hidden. Scalars, exact transform, pose/normals/topology/UVs, frame/clip, eight-key source revision4 and mesh/material/shader/original-texture identities are retained. GPU texture readback remains available while the actor is hidden.

A visibility255 baseline shows the already committed revision4. Source, pose, clock, resource publication, identities, composition count and single archive closure remain unchanged; no resource job is started. Raw255 is retained in the model. Later accepted failure/recovery and movement/clock checks still execute. Before the final held revision23, visibility0 is reapplied. Generation or destroy retires that hidden actor without committing stale work: accepted revision22 remains, and disposal drain stays incomplete until the controlled noncooperative worker is released. Visual allocations/disposals1/1 and archive close1 balance.

## Executed verification

The [native scenario](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldCustomizationVisibilityScenarios.cs) passes36 assertions for baseline seeding/round trips, current forwarded visibility, reseed incarnation/revision/freshness, immutable overlay versus historical baseline, unchanged unrelated common fields, null rejection and actual helper mapping0/1/2/255. The own project compiles the pure presentation helper directly, avoiding a Godot runtime dependency. Prior container226/scalar134/source97 assertions and CPU queue regression pass.

Generation headless and destroy GPU visibility hosts pass. The [Python audit](../../SWG-Godot-Client/poc/round24-tests/Rendering/Verify.py) independently checks complete baseline bytes and all four receipts, then validates original-byte face preparation and five-layer composition. Original closure76 reads/5,940,390 bytes. GPU MAIN readback matches zero SHA-256 `A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808` and varied `60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5`, including the hidden commit.

Hidden-held/hidden-committed PNGs are identical uniform background. Before/show foreground silhouettes and backgrounds match;14,497 pixels change to the restored latest appearance. Both hidden and shown captures were visually inspected. Accepted R23 container and R22 scalar headless hosts/independent audits pass; normal R20 headless host passes. Godot build has zero warnings/errors. This establishes bounded replacement visibility/appearance behavior, not extended-wire integration, original renderer parity, gameplay effects of other common fields, or a policy to pause hidden animation automatically.

## Reproduction and resume

Use existing bundled Python/.NET8/Godot4.7.2Mono with private APPDATA and populated caches; no downloads/original EXE execution. Native project uses its own no-sources NuGet config and links the actual helper:

```powershell
python poc/round24-tests/Rendering/Generate-Peer.py local-private/round24/peer.json
dotnet restore poc/round24-tests/Rendering/Rendering.csproj --configfile poc/round24-tests/Rendering/NuGet.Config -p:NuGetAudit=false
dotnet run --project poc/round24-tests/Rendering/Rendering.csproj --no-restore -p:UseSharedCompilation=false -- local-private/round24/peer.json poc/preview-profiles/scene-object-catalog.json
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false
```

[Launch.ps1](../../SWG-Godot-Client/poc/round24-tests/Rendering/Launch.ps1) takes GodotPath, ArchiveRoot, Configuration, FixturePath, Retirement generation|destroy, and optional fresh private CapturePath. Run generation without capture, then destroy with capture. The four extra PNG siblings are `-visible-before`, `-hidden-held`, `-hidden-committed`, `-shown`, alongside normal base/varied captures. Audit `Verify.py EXPORT PEER LOG [CAPTURE_STEM]`. Omit the stem for receipt/original-face checking without pixel claims. All generated exports and captures remain ignored.

The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_CUSTOMIZATION_VISIBILITY.md) records identities and exact scope. Next lead action: join actual extended visibility updates, verify current TangibleScalars survive older specialized ingress, and prove presentation uses that overlay while historical baseline remains unchanged. The appearance wrapper needs no fix in this milestone.
