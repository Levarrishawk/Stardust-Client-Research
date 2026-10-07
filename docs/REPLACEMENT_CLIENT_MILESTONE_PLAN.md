# Replacement client milestone plan

Reviewed October 6, 2026 against accepted Round82 and source before assigning Round83. The oversight findings are the findings in Levarris's prompt; there is no separate report. No live interoperability has been proved.

## Findings and corrections

The integration concern is supported. Existing proofs have separate entry points and the Godot world preview requires many authored fixture inputs. Many later smoke scenarios are opt-in branches. Current summary tables lag later accepted material and rendering work.

Calling this only an isolated rendering playground would understate the implementation: `LoginClientCoordinator` and injected `LoginDatagramHost` already complete synthetic login and enumerate characters; `ZoneSceneTransition` and `SceneWorldCoordinator` enforce generation and readiness boundaries; asset-backed Godot presentation and local movement have accepted offline proofs. These component proofs do not demonstrate one interoperating client. Original resources already load through configured TREE overlays; the problem is reproducible composition, not universal substitution of original assets.

Evidence: [accepted implementation](CLIENT_IMPLEMENTATION_ACCEPTANCE.md), [login state machine](LOGIN_AND_ZONE_STATE_MACHINE.md), [zone transcript](OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md), and [Round82 acceptance](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_82_2026-10-06.md). Source symbols above distinguish inspection from execution. Confidence: Confirmed for these component boundaries; live compatibility Unknown.

## Ordered gates

| Boundary | Existing implementation | Missing gate and acceptance |
|---|---|---|
| Explicit installation → launch preparation | Round83 inputs; Round84 derives inherited human appearance/animation pins and loads original closure | Executed without historical vectors for the bounded human policy. General selected-avatar/customization and complete terrain preparation remain open; require dependency identities and deterministic missing-resource rejection. |
| Login → character list | Session and Round84 pinned injected two-port driver | Executed synthetic enumeration and late I/O retirement. Controlled server must later accept explicit version/account; real socket and endpoint admission remain unexecuted. |
| Character selection → zone authentication | Enumeration DTOs, SelectCharacter codec; credential handoff documented | Round83 joins unique selected character/cluster, zone port, fresh SOE, exact opaque ClientId credential, accepted permissions, one Select. Negative transitions, wrong identity, denied permissions and cleanup must fail deterministically. |
| Zone entry → scene ready | Round84 bridge routes session ingress; delayed original PTAT provider, ordered staging, one independently verified ready | Complete required scene/terrain readiness and server-shaped lifecycle coverage. PTAT decode alone does not establish full procedural readiness. Test missing work, cancellation/stale traffic and timeout without premature ready. |
| Scene ready → visible selected player | Round84 typed authored CREO3 and original human publish headlessly/GPU with identity/transform/drain proof | Complete player baseline set, arbitrary selected avatar/customization and nearby-object admission/defer policy. Feed complete independently authored server-shaped ingress into the actual session and preserve selected publication without unrelated-object failure. |
| Visible player → movement | Inbound transforms, local input and outbound motion proofs | Route input through session transport and apply authoritative corrections on same actor; verify independently encoded wire and stale/out-of-order handling. Controlled server must observe movement and return consistent updates. |
| Active session → clean disconnect | Local transport close, credential zeroing, generation cancellation, resource/presentation cleanup independently proved | One owner closes ports/session/world/Godot; outbound peer disconnect submission/receipt remains an explicit gate. Prove cancellation in every phase and no remaining jobs/actors/credentials/retries. Controlled server releases the test character and permits a new session. Local disposal alone does not prove server release. |

## Focused Round83

The smallest next slice is successful login → validated character/zone handoff → fresh SOE authentication → permission-gated SelectCharacter submission. It ends before CmdStartScene integration. This tackles the earliest missing implementation boundary.

Four independent assignments are justified: Networking supplies the recovered authentication codecs and independent vectors; Runtime supplies the reusable client lifecycle consuming those sealed codecs; Assets supplies explicit installation preflight; Rendering supplies the offline regression entry point instead of additional UI work. Lead reviews and joins sealed dependencies, replays all changed behavior, and updates current summaries. No reverse-engineering probe is assigned: the required authentication layout is already documented. Workers remain at Low.

Next implementation slice after acceptance: connect zone application delivery to the existing world coordinator and presentation host using preflight installation inputs. Add a reusable injected-port driver with phase-specific pinned peers and retirement of login completions; keep actual sockets disabled until authorized validation. Controlled-server validation follows offline completion of that path; see the [bounded validation plan](CONTROLLED_SERVER_VALIDATION_PLAN.md).

## Focused Round84

Authorized as one next round on October 6. Runtime routes selected session applications into the existing world coordinator and emits ready only after its minimum-resource provider succeeds. Networking adds an owner-thread driver over injected ports, exact pinned peers, and retirement of login I/O before zone traffic. Assets derives creature catalog and animation pins from explicit installation/template/logical choices. Rendering supplies the selected-player adapter over the existing Godot factory, pump and host. Lead joins only reviewed, sealed components and verifies the full offline path headlessly and on GPU.

Acceptance: an independently encoded synthetic login/zone stream reaches one selected original-resource actor through the reusable components; delayed minimum resources prevent early ready, one ready is checked on wire, foreign and retired traffic cannot mutate the new session, and cancellation drains ports/jobs/actors. Original creature resources remain original installation inputs, with authored peer and modern animation/camera choices explicitly labeled. Full terrain setup, arbitrary avatar customization, live credentials/version acceptance, movement authority and server-observed disconnect remain distinct gates. No new reverse-engineering probe or playground polish is assigned.

Round84 accepted: [joined evidence and retained corrections](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R84_SELECTED_PLAYER_SESSION.md). The visual factory requires typed state; the joined proof uses the accepted independent CREO3 author, while the standalone pump proof retains opaque-envelope limits. Native removal exposed and repaired a host teardown ordering bug.

**Next milestone:** complete server-shaped selected-player baselines/lifecycle plus nearby-object ingress through this session. Acceptance must exercise the complete required baseline set, selected-avatar resource identity and explicit handling of unrelated objects, with bounded readiness failure and cleanup. This resolves the next unproved zone-entry boundary before joining authoritative movement and protocol disconnect. No reverse-engineering probe is assigned until an unsupported layout or state transition names an actual blocker. Live validation remains gated by the offline sequence and separate authorization.
