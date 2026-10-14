# Unified world motion ingress

Round18 Networking, October2,2026. The [router](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldMotionApplicationRouter.cs) joins the accepted [compact codec](COMPACT_SCENE_TRANSFORM_CONTRACT.md), [motion store](SCENE_WORLD_OBJECT_MOTION_CONTRACT.md) and [presentation pump](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldPresentationPump.cs). This is a modern replacement policy backed by offline fixtures, not a recovered legacy dispatch or interpolation policy.

```csharp
new SceneWorldMotionApplicationRouter(world, motions, worldProjection);
bool accepted = router.Accept(generation, applicationSpan);
// Projector type:
Func<CompactSceneTransformMessage, SceneWorldObjectSnapshot,
    (AppearanceTransform Transform, float Speed)> worldProjection;
// Actual baseline pump constructor hook (named argument):
applicationReceiver: (generation, bytes) => router.Accept(generation, bytes.Span)
```

The router borrows world/store and requires the construction thread for Accept and Dispose. Constructor verifies world affinity. Calls are synchronous and retain no input bytes, snapshot or decoded packet after returning. Dispose releases borrowed references without closing or clearing either world or store; the caller disposes the router separately from the owning pump. Disposal is idempotent; Accept after disposal throws. There is no new local-driver dependency.

Accept first drops stale generations without parsing. It reads the opcode only when at least six bytes exist. Known compact root and parent opcodes always receive full exact structural decoding, even before the barrier or for a missing target. All other bytes pass unchanged to world.Accept, preserving its normal state transitions and malformed-input behavior. Decode errors and projection/store exceptions propagate to the pump, whose accepted cleanup closes the world, retires actors, clears queued owned bytes and observes resource jobs. Direct router users own their failure policy; the router itself does not close the world.

Compact motion is legal only in AwaitingServerReady or Ready for a current finalized target. Before-barrier, pending and unknown targets return false without calling the projector. Both parent branches return false after decoding, including parent ID zero. Their eighth-unit local positions are never supplied to the world projector or published as world coordinates.

For root motion the caller must explicitly interpret raw signed direction and speed bytes. No default yaw, units, interpolation, normalization or speed clamp is inferred. The fixture projector deliberately uses identity orientation, quarter-unit NativePosition and constant diagnostic speed2; this is synthetic policy only. Height stays in replication and raw create placement stays unchanged.

Before projection the router captures the world state, read-only change batch mutation stamp, fresh target snapshot and current target motion reference. After projection it rejects generation/state/world mutation changes or a changed target motion reference. The motion store then validates exact object/placement/envelope/typed-state freshness, finite proper rigid transform and nonnegative finite speed before authoritative publication. The batch is never acknowledged by the router. A callback which catches a nested router Accept/Dispose exception still latches reentry: the outer call throws before publication. Callback side effects are not rolled back; the guarantee is that the router adds no motion from that invalid result. Clear of an already empty store has no observable target-row change; changes to unrelated store rows are outside this target-row guard. Projectors should be pure.

Authoritative serial ordering and store-wide revision belong to the motion store: duplicate, older and half-range counters reject without consuming revisions, forward wrap accepts, and recreation starts counter history for the new incarnation. A rejected serial may still invoke the projector because the store owns that policy. Raw counter and signed packet fields reach it unchanged. No additional queue is created; the pump owns copies and invokes this borrowed-byte receiver before clearing each array. Use the accepted pump message/count/byte/publication budgets.

## Evidence and reproduction

Observation: independently handpacked root bytes encode high object ID, signed extrema/fractions, negative raw speed/direction, wrap and rejected serials. Native scenarios exercise the actual baseline pump hook with three-message step cap, ordinary scalar plus motion ingress, caller-array clearing, controlled resource completions and two fake presentation actors. Original sealed closures are loaded read-only once (human39 sources and terminal27), with archive scopes closed before controlled transfer. No original executable, live transport, socket, DNS or server is used.

Confidence: Confirmed implemented offline behavior; inherited raw grammar is described in the compact contract. Barrier/finalization/parent-drop/serial/freshness choices are explicit modern policy. Original yaw conversion, prediction, interpolation and parent-world resolution remain Unknown/outside this slice. Existing packet/name registries and research/readiness denominators are unchanged.

[Scenario entry point](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldMotionIngressScenarios.cs): Run(string[] originalInputs, string peerPath, string exportPath), with root/config/catalog/bank inputs. [Runner](../../SWG-Godot-Client/poc/round18-tests/Networking/Networking.csproj) and [independent byte/state oracle](../../SWG-Godot-Client/poc/round18-tests/Networking/peer.py) produce and verify synthetic receipts without importing production code. The byte producer reuses prior independent ordinary-packet fixtures.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r18'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round18 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round18/NuGet.Config
& $python poc/round18-tests/Networking/peer.py --generate local-private/round18/peer.json
dotnet restore poc/round18-tests/Networking/Networking.csproj --configfile local-private/round18/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round18-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round18/peer.json local-private/round18/export.json
& $python poc/round18-tests/Networking/peer.py --peer local-private/round18/peer.json --verify local-private/round18/export.json
```

The exact next action is lead-owned registration and offline original-preview joining via the named applicationReceiver hook with an explicitly selected world projection policy. Parent-space hosting and local-driver reconciliation remain separate bounded work.
