# Replacement session-to-world bridge

Round86 adds optional borrowed `ReplacementPlayerMotionSession? playerMotion = null` after selectedIngress. BindSelection binds both to the admitted ID and new generation. ObserveExchange routes motion after selected ingress and before WorldMessage; selected full-root corrections are consumed even on a router decline, while unsupported or unrelated frames are copied to caller-owned remainder. Known malformed root/parent frames fail the bridge and close session/world. Default construction preserves earlier behavior. Normal bridge disposal leaves motion seam and its borrowed store alive. See the [motion session contract](REPLACEMENT_PLAYER_MOTION_SESSION_CONTRACT.md).

## Question, scope and evidence

Can accepted login/zone application delivery bind one selected world generation, preserve staged lifecycle order, and emit one ready only after an explicit asynchronous minimum-resource provider succeeds? The bounded ReplacementSceneBridge (external historical reference) does that using the existing SceneWorldCoordinator. It borrows session/world and owns only its minimum-resource job/cancellation. No prototype host, terrain decoding, port or world implementation is added.

Confirmed on 2026-10-06: an independently authored in-memory login/zone transcript reaches a matching valid CmdStartScene, stages create/opaque CREO/end-baselines while resources are delayed, then replays those messages and produces one exact protected header-only CmdSceneReady at sequence2. The baseline fixture establishes an opaque envelope, not typed CREO field semantics or a complete player baseline. No visual resource loader is called by these tests. Original-resource publication and live compatibility remain unverified by this component task.

Input accepted83 has4752 public files; trusted synchronization preserves HEAD/index/private and prior seals. The milestone plan's Round84 section was read from main after the snapshot. Source evidence is the existing [world transcript contract](OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md), [replacement session contract](REPLACEMENT_CLIENT_SESSION_CONTRACT.md), SceneWorldCoordinator staging/barrier methods and reviewed frozen83 authentication codec. No new reverse-engineering or executable VA input is used; research/protocol/function denominators do not change.

## Frozen public API

Namespace `Stardust.Core.Session`:

```csharp
var bridge = new ReplacementSceneBridge(ReplacementClientSession session,
    SceneWorldCoordinator world,
    Func<CmdStartSceneMessage, CancellationToken, Task> minimumResources);
bridge.BindSelection();
ReplacementClientExchange routed = bridge.ObserveExchange(exchange);
ReplacementClientExchange output = bridge.Poll(nowMilliseconds);
long generation = bridge.Generation;
int readySubmissions = bridge.ReadySubmissions;
bridge.Dispose();
await bridge.DrainCompletion;
```

All methods that mutate state run on the bridge's creator thread without reentry. Provider task continuations observe completion/cancellation and dispose only owned cancellation state; they never call session/world methods. Poll observes the provider outcome on the caller thread. The provider is invoked once on a matching accepted CmdStartScene and must return a task; it must not mutate borrowed session/world from a background continuation.

## Binding and routing

Call BindSelection once after session.BeginZone has admitted a nonzero SelectedObjectId. ZoneHandshake, ZonePermissions or Selected phase is legal for binding. It calls world.BeginSelection and records its Generation; its generated Select message is not sent. The session is still the only Select sender. The borrowed world must remain assigned to this bridge until retirement. External generation replacement is a stale-owner error and triggers the documented terminal close boundary; old work cannot publish ready into the replacement.

ObserveExchange takes ownership of ZoneApplications and zeroes every supplied application before returning. World messages route in input order through world.Accept: start, create, destroy, containment, end-baselines, ready, and CREO/TANO baseline/delta families. Malformed required messages fail closed. Other replication families and nonworld known/unknown opcodes return explicit defensive copies in ZoneApplications for the caller's next handler. There is no silent drop. A truncated application header or replication family prefix is invalid. Local admission bounds are1024 applications/8MiB per exchange and the existing1.5MB transport message cap per application.

Datagrams pass through with their original ownership; the bridge neither sends nor clears them during ObserveExchange. FailureType and RejectedFraming are preserved, including exchanges from an injected driver with empty outgoing datagrams. On a routing error, output retains controls when provided, reports a safe exception type (or preexisting failure), zeroes unhandled temporary copies and closes the borrowed session/world. Caller must not treat a failed exchange as successful world mutation.

## Minimum barrier and ready

The caller supplies the modern minimum-resource policy. A successful task is an explicit assertion that that policy completed; the bridge does not infer full terrain, legacy barrier parity or visual publication from it. While the job is incomplete, Poll returns session reliability output but never completes the world barrier or queues ready. A faulted/canceled task, invalid generation, malformed staged replay or borrowed-session failure closes the terminal boundary with no ready.

After successful completion, Poll waits for any pending session message to enter the reliable send window. It calls CompleteMinimumSceneBarrier once with the bound generation. Existing world code replays staged messages in order before returning ready bytes. Those bytes queue via Selected-only QueueZoneApplication, then the temporary ready array is zeroed. ReadySubmissions counts one accepted queue submission, not physical retransmissions or full visual publication. A subsequent server ready routes back into the world. Each returned ready/reliability datagram remains caller-owned for driver submission and cleanup.

Normal Dispose is idempotent: cancel the owned job and expose DrainCompletion, without disposing the borrowed session/world or competing with the presentation pump's close owner. A provider that ignores cancellation keeps DrainCompletion pending until its actual completion is observed. On failure the bridge additionally closes session/world. DrainCompletion covers this bridge's minimum job; callers must also await the world's own DrainCompletion and the port/presentation owners where applicable. A failed Poll zeroes any internally received session output that was not returned.

## Independent tests and reproduction

The test project (external historical reference) references actual Core directly. Fixture author (external historical reference) reuses the accepted83 login author and independent Python SOE peer, then independently packs valid world lifecycle bytes. It writes fresh private JSON and a separate accepted83 base fixture. Verifier (external historical reference) compares the one ready wire against independent seeded CRC/selector framing and sequence2. No unpublished sibling component is imported.

```powershell
python poc/round84-tests/Runtime/fixtures.py --out <fresh-private-fixture.json>
dotnet build poc/round84-tests/Runtime/Runtime.csproj --artifacts-path <private-artifacts> --configfile poc/round84-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet <private-artifacts>/bin/Runtime/debug/Runtime.dll <private-fixture.json>
python poc/round84-tests/Runtime/verify.py --fixtures <private-fixture.json> --result <private-test-output.log> --out <fresh-private-receipt.json>
```

Set private APPDATA/NuGet.Config, DOTNET_CLI_HOME, existing exact offline NUGET_PACKAGES and DOTNET_GENERATE_ASPNET_CERTIFICATE=false. Capture Program stdout to the explicit result log. All artifacts/receipts remain ignored. No native/GPU command is required for this engine-neutral component.

Observed result: zero-warning/error Core and test build;70 checks/18 rejection cases. Tests cover delayed readiness, exact wire, selected identity, ordered opaque staging, unhandled ownership, synchronous/faulted/canceled/ignoring-cancel jobs, duplicate start, wrong ID, early server ready, malformed baseline, failed barrier replay, stale generation, bind/phase errors, foreign thread/reentry, input budget and normal borrowed-owner preservation. QueueZoneApplication also exercises fragmented submission and rejects invalid short headers. Confidence Confirmed for these tested transitions and bytes; actual installed resource minimum policy, prototype host actor publication and live server scene readiness remain separate.

Exact next action: lead joins reviewed injected ports, installed selected-creature resources and presentation adapter, supplies a delayed explicit minimum-resource policy, and independently verifies one ready and full cleanup on the native selected-player path. Keep full terrain/surface parity, arbitrary customization, authority/movement and server-observed disconnect as separate gates.

## Round85 optional selected-player ingress

The constructor appends `ReplacementSelectedPlayerIngress? selectedIngress = null`. The bridge borrows it and binds it once to the admitted selected ID and world generation. It calls Route before its existing world classification. False returns an owned unhandled copy; true continues the original routing. Existing default behavior is unchanged. Normal bridge disposal does not dispose the ingress; the caller does so after bridge retirement. [Policy contract](REPLACEMENT_SELECTED_PLAYER_INGRESS_CONTRACT.md).

With this bounded modern policy, unrelated lifecycle/envelopes never reach the selected catalog/world, and selected end-baselines requires observed CREO3/4/6 baseline envelopes. CREO1 and unsupported selected families/views are explicitly deferred. Observed presence is not typed completeness: existing world decoders remain authoritative, and a well-framed but invalid full4 body fails at staged barrier replay without ready. No complete CREO1/PLAY semantics or recovered dispatch rule is claimed.
