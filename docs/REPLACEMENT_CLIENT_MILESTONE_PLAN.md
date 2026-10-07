# Replacement client milestone plan

Current review: accepted Round93. The original Round82 oversight assessment below is preserved as historical context. The oversight findings are the findings in Levarris's prompt; there is no separate report. No live interoperability has been proved.

## Findings and corrections

The integration concern is supported. Existing proofs have separate entry points and the Godot world preview requires many authored fixture inputs. Many later smoke scenarios are opt-in branches. Current summary tables lag later accepted material and rendering work.

Calling this only an isolated rendering playground would understate the implementation: `LoginClientCoordinator` and injected `LoginDatagramHost` already complete synthetic login and enumerate characters; `ZoneSceneTransition` and `SceneWorldCoordinator` enforce generation and readiness boundaries; asset-backed Godot presentation and local movement have accepted offline proofs. These component proofs do not demonstrate one interoperating client. Original resources already load through configured TREE overlays; the problem is reproducible composition, not universal substitution of original assets.

Evidence: [accepted implementation](CLIENT_IMPLEMENTATION_ACCEPTANCE.md), [login state machine](LOGIN_AND_ZONE_STATE_MACHINE.md), [zone transcript](OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md), and [Round82 acceptance](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_82_2026-10-06.md). Source symbols above distinguish inspection from execution. Confidence: Confirmed for these component boundaries; live compatibility Unknown.

## Ordered gates

| Boundary | Existing implementation | Missing gate and acceptance |
|---|---|---|
| Explicit installation → launch preparation | Round83 inputs; Round84 derives inherited human appearance/animation pins and loads original closure | Executed without historical vectors for the bounded human policy. General selected-avatar/customization and complete terrain preparation remain open; require dependency identities and deterministic missing-resource rejection. |
| Login → character list | Session and Round84 pinned injected two-port driver | Executed synthetic enumeration and late I/O retirement. Controlled server must later accept explicit version/account; real socket and endpoint admission remain unexecuted. |
| Character selection → zone authentication | Round83 protected handoff plus Round91 explicit advertised endpoint correspondence before acquisition | Offline positive/adversarial cases pass. Round92 bootstrap requires approved mappings and exactly one approved character; explicit transport composition is source-reviewed but unexecuted; real server must accept the explicit version/token/permissions and selection. |
| Zone entry → scene ready | Round84 bridge routes session ingress; delayed original PTAT provider, ordered staging, one independently verified ready | Complete required scene/terrain readiness and server-shaped lifecycle coverage. PTAT decode alone does not establish full procedural readiness. Test missing work, cancellation/stale traffic and timeout without premature ready. |
| Scene ready → visible selected player | Round85 joins complete typed CREO3/4/6 and explicitly returns unsupported/nearby messages; original human visible | CREO1/PLAY gameplay, arbitrary avatar/customization and nearby presentation remain open. Deferral is safe routing, not complete owner/zone state. Require scenario-specific supported state and resources before controlled validation. |
| Visible player → movement | Round86 joins modern planar input, protected root submission and authoritative corrections on the same original actor | Offline exact wires/rows/actor positions, duplicate/older/wrap counters and malformed cleanup pass. Original sender cadence/units, collision/prediction and live server authority remain gates. |
| Active session → clean disconnect | Round87 prepares exact protected zone disconnect, waits behind in-flight send, observes final send and retires/drains ports; original actor/world/session close is joined | Offline success/error/timeout and async cleanup ownership pass. Real server receipt/release and permitted subsequent session remain controlled validation gates; local send/drain is insufficient. |

## Focused Round83

The smallest next slice is successful login → validated character/zone handoff → fresh SOE authentication → permission-gated SelectCharacter submission. It ends before CmdStartScene integration. This tackles the earliest missing implementation boundary.

Four independent assignments are justified: Networking supplies the recovered authentication codecs and independent vectors; Runtime supplies the reusable client lifecycle consuming those sealed codecs; Assets supplies explicit installation preflight; Rendering supplies the offline regression entry point instead of additional UI work. Lead reviews and joins sealed dependencies, replays all changed behavior, and updates current summaries. No reverse-engineering probe is assigned: the required authentication layout is already documented. Workers remain at Low.

Next implementation slice after acceptance: connect zone application delivery to the existing world coordinator and presentation host using preflight installation inputs. Add a reusable injected-port driver with phase-specific pinned peers and retirement of login completions; keep actual sockets disabled until authorized validation. Controlled-server validation follows offline completion of that path; see the [bounded validation plan](CONTROLLED_SERVER_VALIDATION_PLAN.md).

## Focused Round84

Authorized as one next round on October 6. Runtime routes selected session applications into the existing world coordinator and emits ready only after its minimum-resource provider succeeds. Networking adds an owner-thread driver over injected ports, exact pinned peers, and retirement of login I/O before zone traffic. Assets derives creature catalog and animation pins from explicit installation/template/logical choices. Rendering supplies the selected-player adapter over the existing Godot factory, pump and host. Lead joins only reviewed, sealed components and verifies the full offline path headlessly and on GPU.

Acceptance: an independently encoded synthetic login/zone stream reaches one selected original-resource actor through the reusable components; delayed minimum resources prevent early ready, one ready is checked on wire, foreign and retired traffic cannot mutate the new session, and cancellation drains ports/jobs/actors. Original creature resources remain original installation inputs, with authored peer and modern animation/camera choices explicitly labeled. Full terrain setup, arbitrary avatar customization, live credentials/version acceptance, movement authority and server-observed disconnect remain distinct gates. No new reverse-engineering probe or playground polish is assigned.

Round84 accepted: [joined evidence and retained corrections](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R84_SELECTED_PLAYER_SESSION.md). The visual factory requires typed state; the joined proof uses the accepted independent CREO3 author, while the standalone pump proof retains opaque-envelope limits. Native removal exposed and repaired a host teardown ordering bug.

**Next milestone:** complete server-shaped selected-player baselines/lifecycle plus nearby-object ingress through this session. Acceptance must exercise the complete required baseline set, selected-avatar resource identity and explicit handling of unrelated objects, with bounded readiness failure and cleanup. This resolves the next unproved zone-entry boundary before joining authoritative movement and protocol disconnect. No reverse-engineering probe is assigned until an unsupported layout or state transition names an actual blocker. Live validation remains gated by the offline sequence and separate authorization.

## Focused Round85

Source review corrects an incomplete reading of the next gate: the owner receives
CREO1/3/4/6, while the separate slotted player object receives PLAY3/6/8/9.
Complete CREO4/6 codecs and stores already exist; CREO1 and PLAY gameplay state
are not joined. Server emitters are correlation evidence, not proof of the
original client's dispatch or current deployed-server behavior.

The bounded slice joins typed selected CREO3/4/6 and explicitly returns unsupported
owner state and unrelated lifecycle/replication messages to the caller. It must
require the supported selected baselines before end-baselines, preserve caller
ownership of deferred messages, and prevent unknown nearby templates from
entering the one-player resource catalog. This is a modern selected-player
policy, not full populated-zone admission or complete owner-state support.

Three Low workers have independent work: Runtime owns the reusable ingress
policy and optional bridge seam; Networking owns independent protected ingress
fixtures and dated server correlation; Rendering owns default regression
execution of the new gate. Assets remains idle because no asset parser or
preparation change is needed. Lead independently reviews, replays and joins the
sealed components through original-resource Godot publication. No new
reverse-engineering probe or presentation polish is assigned.

Acceptance requires exact selected CREO4/6 models and deferred-message bytes
against independent expectations, one protected ready after delayed minimum
resources, original actor identity/transform, and drained success/cancellation/
failure. Missing CREO4, missing CREO6 and truncated CREO6 must produce no ready
or actor. Deferred CREO1/PLAY remain named implementation blockers; returning
their bytes does not satisfy their gameplay semantics.

Round85 accepted: [selected ingress evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R85_SELECTED_INGRESS.md). Full selected4/6 model equality and twelve exact deferred applications pass through the actual joined headless/GPU path; missing4/missing6/truncated6 reject without ready/actor and drain. Thirteen default suites pass. Unsupported owner/private state and nearby presentation remain deferred. The native actor is still a bounded uncustomized human with modern minimum terrain policy.

**Next milestone recorded at Round85:** join local outbound movement and authoritative inbound corrections through this replacement session and the same selected actor. Reuse existing engine-neutral stores/builders/codecs; prove exact wire/current incarnation/stale handling and local cleanup. Protocol disconnect follows. CREO1/PLAY gameplay and general avatar/nearby/terrain readiness remain explicit work and controlled-test prerequisites as applicable. No further round is authorized by Round85, and no live validation is authorized.

## Focused Round86

Successive overnight authorization follows Round85. Three Low workers have independent boundaries: Runtime joins the existing local builder and authoritative router to session/bridge ownership; Rendering optionally wraps the selected actor with the existing motion visual and adds the required default regression pipeline; Networking independently authors protected movement/correction fixtures and primitive comparisons. Assets stays idle because the original preparation/resource formats do not change. No new reverse-engineering probe or playground feature is assigned.

[Accepted evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R86_PLAYER_MOVEMENT.md) executes actual local input, three protected root submissions and newer/duplicate/older/wrap-newer corrections on the same original human actor headlessly and on GPU. Malformed known correction closes cleanly. This is an authored modern timing/planar policy and synthetic authority test, not recovered sender cadence or live authority.

**Next milestone recorded at Round86:** implement bounded outbound SOE disconnect submission/drain through the same reusable session/port owner, distinguish submission from actual send completion, and preserve deterministic abort paths. Independently verify protected bytes/connection identity, one send, pending-I/O cleanup and original actor teardown. Server-observed release, live compatibility, owner/private state and approved-scenario avatar/terrain readiness remain separate. No live validation is authorized; request specific authorization only when offline prerequisites make it actionable.

## Focused Round87

Three Low workers implement selected-zone packet/session/pump ownership, an independent protected disconnect oracle and the required default regression pipeline. Assets idle; no new asset, Godot production or reverse-engineering work is needed. [Accepted evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R87_CLIENT_DISCONNECT.md) independently verifies the full original-player movement prefix followed by actual final send and local drain, including error/timeout. Runtime separately tests asynchronous I/O faults, late/completed receives, pending prior send, unsent buffer cleanup and post-send disposal failure. Fifteen required default suites pass.

**Next milestone recorded at Round87 acceptance:** make the accepted reusable components a reproducible application session path, with explicit original-installation launch inputs and consolidated native validation. Prepare the normal launch/selection/movement/disconnect adapter after stable core/input contracts. Limit further source investigation to the named controlled-movement decision: determine server boundary validation of sender timing/counter/speed before choosing the bounded test policy; stop after exact parser/handler constraints are documented. Do not treat server correlation as original-client proof. Keep general owner/terrain/avatar work scoped to the approved scenario. Once offline application preparation and scenario prerequisites pass, request exact operator/account/endpoint authorization; no live contact is implied by these rounds.

## Focused Round88

[Accepted Round88](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_88_2026-10-06.md) adds explicit installation profiles, reusable client run and one consolidated validation command. Independent original-resource headless/GPU, failure and cleanup verification pass. This remains an offline proof; synthetic peers do not establish interoperability.

Four Low specialists provided independent launch preparation, session composition, regression orchestration and the named server movement decision. The latter reveals teleport acknowledgement and known timestamp/validated-state origin as concrete remaining gates. Its conditional 400 ms policy is current local server correlation, not original cadence or deployed acceptance.

Next action recorded at Round88 acceptance: normal application ownership/launch/selection/input/disconnect using the accepted contracts, plus the named TeleportAck and timestamp/validated-state origin gate before controlled movement. Current server source supports a conditional 400 ms scenario; original/deployed/live behavior remains unproved. CREO1/PLAY gameplay, nearby presentation, arbitrary avatars/customization, full procedural terrain and original movement timing/collision/prediction remain open. Local send/drain does not prove server release. Live validation remains separately authorized after offline scenario prerequisites. No research, function or test denominator measures replacement-client completion. Use fewer workers for the next milestone when application assembly and presentation depend on the same new contract.

## Accepted Round89 and next milestone

Round89 adds the engine-neutral `ReplacementClientApplication`, owning original preparation, session, pump, world, motions and run. Its injected pinned ports and minimum provider make startup reproducible without constructing sockets or DNS. Presentation remains caller-owned. Nonblocking retirement retains actual resource/provider/port borrowers; owner-thread finalization waits for their completion and the exact presentation task, with faults separately observed. Independent sealed-component replay and the original-resource headless/GPU application join pass. All eighteen required defaults pass through the consolidated command. Synthetic peers remain test inputs, not interoperability evidence.

Next: normal Godot login/character selection, user movement and close using the accepted application contract. Fresh current-server SelectCharacter resets the stamp to zero and CmdSceneReady clears teleporting; a blanket TeleportAck at readiness is unnecessary for that gate. The conditional400 ms modern policy still requires matching current/validated state and an approved passable floor. Later full-transform ACK final packing, validated-position task ordering, original timing, general CREO1/PLAY/avatar/terrain and live authority/server release remain unproved. Request specific live authorization only after offline scenario preparation is actionable. Use fewer workers where useful independent work is insufficient; no further playground polish or unnamed probes.

## Accepted Round90 and next boundary

Round90 joins normal Godot login, credential entry, received character selection, controlled input and close through the accepted application owner. The engine-neutral pacer requires received server-ready and explicit fresh-state assertions, queues at most three roots at least400 ms apart on the caller clock, and stops on corrections, changed selection, unsupported placement or close. Independent original-resource native controls/input/close and GPU replay pass; all nineteen required defaults pass through the consolidated command. Signals and injected keys are offline test inputs, not physical OS input or live interoperability proof.

Three existing Low specialists supplied controlled movement, the Godot host and the named baseline comparison; Assets idle. Lead independently reviewed frozen components, replayed exact protected roots, inspected fourteen server inputs including inherited tangible/envelope serializers, and ran the original host and consolidated gate. Source correlation establishes no additional field-order blocker for a bounded outdoor human within limits. The prepared1,024-byte CREO3/4/6 cap remains a conditional rejection boundary; no chosen real character was measured. Deferred CREO1/PLAY supply no minimum local readiness mask, while gameplay/appearance parity remains unproved.

Next: validate the selected advertised galaxy/zone endpoint against an explicit operator-approved pinned mapping before acquiring the zone port. Current application fixes the peer but does not establish its relationship to the received hostname/zone port. Chosen-character baseline sizes, resource compatibility, validated position/floor and deployed movement admission still require scenario evidence. General CREO1/PLAY, avatars/customization, nearby actors, full terrain and live authority/server release remain open. Request specific controlled-server authorization only when offline preparation and operator inputs make it actionable.

## Accepted Round91 and next boundary

Round91 binds received selected galaxy and advertised zone hostname/zone port to an explicit operator-approved pinned IPv4 mapping before acquiring the zone port. Login mapping is checked before installation preparation or factory acquisition. Rejected selection preflight preserves login credentials, character-list state, clocks and submission counts. Independent protected-peer adversarial cases and the policy-enabled original-resource Godot host pass; the consolidated command runs all twenty-one defaults and nine native host cases including GPU. Mappings and peers are authored offline inputs; no DNS, socket or live interoperability proof is claimed.

Next: provide a standalone explicit-input bootstrap for the reusable application and Godot host, require approved endpoint and selected-character inputs, and enforce one bounded attempt with redacted phase diagnostics and observed cleanup. The main Godot project still opens the feasibility scene; the reusable host currently needs a caller to configure it. Chosen real-character baseline sizes/resources, validated position/floor, deployed movement admission and operator server/account prerequisites remain gates. General avatar/customization, nearby actors, full terrain and live server authority/release remain unproved. Request specific live authorization only when offline preparation and operator inputs make a controlled attempt actionable.

## Accepted Round92 and next boundary

Round93 adds an explicit controlled-transport entry over the reusable installation/application/Godot host and a bounded redacted attempt receipt written after actual local drain or startup rollback. Default startup still refuses contact. Offline native headless/GPU replay verifies the same strict admission/composition with mandatory synthetic spy ports, protected exchanges, one approved original human, bounded movement, deadlines and disconnect. Run, cleanup, rollback and diagnostic failures remain distinct; standalone quit follows ownership completion. The real UDP constructor branch is compiled and source-reviewed, never executed.

Next: obtain operator-approved deployment/account access, explicit advertised hostname-to-IPv4 mappings/ports and client version, one existing supported outdoor human with measured CREO3/4/6 limits and original resource identity, and a bounded minimum-readiness scenario. Keep movement disabled unless fresh stamp/ready state, matching validated position and passable floor are established. Then request specific authorization for one bounded controlled attempt. No original executable, real socket, DNS/server contact or live interoperability has been verified. General avatar/customization, nearby actors, full terrain and server-observed authority/release remain open.
