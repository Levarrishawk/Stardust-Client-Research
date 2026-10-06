# Replacement client milestone plan

Reviewed October 6, 2026 against accepted Round82 and source before assigning Round83. The oversight findings are the findings in Levarris's prompt; there is no separate report. No live interoperability has been proved.

## Findings and corrections

The integration concern is supported. Existing proofs have separate entry points and the Godot world preview requires many authored fixture inputs. Many later smoke scenarios are opt-in branches. Current summary tables lag later accepted material and rendering work.

Calling this only an isolated rendering playground would understate the implementation: `LoginClientCoordinator` and injected `LoginDatagramHost` already complete synthetic login and enumerate characters; `ZoneSceneTransition` and `SceneWorldCoordinator` enforce generation and readiness boundaries; asset-backed Godot presentation and local movement have accepted offline proofs. These component proofs do not demonstrate one interoperating client. Original resources already load through configured TREE overlays; the problem is reproducible composition, not universal substitution of original assets.

Evidence: [accepted implementation](CLIENT_IMPLEMENTATION_ACCEPTANCE.md), [login state machine](LOGIN_AND_ZONE_STATE_MACHINE.md), [zone transcript](OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md), and [Round82 acceptance](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_82_2026-10-06.md). Source symbols above distinguish inspection from execution. Confidence: Confirmed for these component boundaries; live compatibility Unknown.

## Ordered gates

| Boundary | Existing implementation | Missing gate and acceptance |
|---|---|---|
| Explicit installation → launch preparation | ConfiguredTreOverlay, resource loaders, original-install proofs | Reusable inputs root/config/resource list; preflight hashes and dependencies without historical vectors. Round83 provides this component; complete terrain/player dependency closure remains later. |
| Login → character list | Login codecs, SOE, injected host, redacted disposable result | Combined client lifecycle and pinned two-port I/O driver. Round83 adds the socket-independent lifecycle; caller-tagged datagrams alone do not prove peer pinning or retirement of old receive completions. Synthetic transcript must drive real login into stable enumeration; later authorized controlled server must accept exact client version and account. |
| Character selection → zone authentication | Enumeration DTOs, SelectCharacter codec; credential handoff documented | Round83 joins unique selected character/cluster, zone port, fresh SOE, exact opaque ClientId credential, accepted permissions, one Select. Negative transitions, wrong identity, denied permissions and cleanup must fail deterministically. |
| Zone entry → scene ready | CmdStartScene, SceneWorldCoordinator, minimum-resource barrier, staging and ready codec | Route live-shaped session application frames through reusable world owner; one ready only after actual minimum resources, preserve ordered staging and matching selected object. Test delayed resources, malformed/stale traffic and failure before server validation. |
| Scene ready → visible selected player | Replication, original resource loading, Godot presentation pump/host | Reusable Godot session entry consumes the selected player's authoritative creates/baselines and original template chain; assert identity, coordinate transform, appearance visibility and no fixture-specific launcher arguments. |
| Visible player → movement | Inbound transforms, local input and outbound motion proofs | Route input through session transport and apply authoritative corrections on same actor; verify independently encoded wire and stale/out-of-order handling. Controlled server must observe movement and return consistent updates. |
| Active session → clean disconnect | Local transport close, credential zeroing, generation cancellation, resource/presentation cleanup independently proved | One owner closes ports/session/world/Godot; outbound peer disconnect submission/receipt remains an explicit gate. Prove cancellation in every phase and no remaining jobs/actors/credentials/retries. Controlled server releases the test character and permits a new session. Local disposal alone does not prove server release. |

## Focused Round83

The smallest next slice is successful login → validated character/zone handoff → fresh SOE authentication → permission-gated SelectCharacter submission. It ends before CmdStartScene integration. This tackles the earliest missing implementation boundary.

Four independent assignments are justified: Networking supplies the recovered authentication codecs and independent vectors; Runtime supplies the reusable client lifecycle consuming those sealed codecs; Assets supplies explicit installation preflight; Rendering supplies the offline regression entry point instead of additional UI work. Lead reviews and joins sealed dependencies, replays all changed behavior, and updates current summaries. No reverse-engineering probe is assigned: the required authentication layout is already documented. Workers remain at Low.

Next implementation slice after acceptance: connect zone application delivery to the existing world coordinator and presentation host using preflight installation inputs. Add a reusable injected-port driver with phase-specific pinned peers and retirement of login completions; keep actual sockets disabled until authorized validation. Controlled-server validation follows offline completion of that path; see the [bounded validation plan](CONTROLLED_SERVER_VALIDATION_PLAN.md).
