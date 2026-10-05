# Parallel development operating rules

Adopted October 1, 2026. This chat is project lead. Four persistent worker chats use Low reasoning; no silent model or reasoning escalation. Preparation is authorized; milestone dispatch requires the user's next prompt.

## Roles

| Worker | Specialty | Boundary |
|---|---|---|
| Asset compatibility | Archive/IFF readers, dependency closure, decoding | Engine-neutral asset contracts and fixtures |
| Rendering | Materials, textures, shaders, geometry, Godot adapters | Rendering compatibility and adapter validation |
| Runtime and simulation | Animation, movement, camera, scene state, lifecycle | State transitions, ownership and deterministic updates |
| Networking and client state | SOE, application codecs, replication, connection state | Offline protocol and state fixtures |

## Round lifecycle

1. User authorizes one round. Lead assigns one bounded milestone to each worker.
2. Each assignment states round/milestone ID, baseline identity, question, evidence, allowed files, interfaces, acceptance commands, stopping conditions and handoff path.
3. Assignments use stable existing inputs; workers do not depend on unfinished work in that round.
4. Each worker uses an isolated checkout. The lead preserves the dirty main checkout and records a file-hashed baseline before preparing workers. Ignored private assets/toolchains are not copied into curated output or Git.
5. Workers implement only their assignment, verify proportional positive/negative/cleanup cases, record evidence and confidence, then report and stop. No automatic next milestone or recurring automation.
6. Routine fixes within scope are allowed. A blocked worker reports the precise obstacle and completed work; it does not choose a replacement milestone or broaden ownership.
7. Lead reviews baseline-relative changed files and evidence, integrates accepted changes without discarding user edits, runs combined checks, and reconciles shared canonical documents/registries.
8. After all workers finish or report a blocker, lead provides one round report and proposes the next assignments. Dispatch waits for another user prompt.

## File ownership and integration

Workers never edit the primary checkout. Each assignment has explicit allowed paths. Shared entry points, project files, status/ledger/navigation documents, curated registries and common test runners belong to the lead unless explicitly assigned. A worker needing a shared change describes it in its handoff. Worker-local milestone docs may be authored only at assigned unique paths. Conflicting integration is resolved by the lead before accepting the milestone.

No worker commits, stages, pushes, publishes, creates remotes or changes visibility. Managed checkout setup must not publish work. Deliver working-tree changes and a baseline-relative file/hash list; Git HEAD alone is insufficient because the preparation baseline contains uncommitted changes.

Private inputs may be read from the original configured root; generated evidence goes into the worker's ignored local-private path. No original game executable execution, live server contact, packet capture, fuzzing or external mutation without separately authorized bounded scope. Existing AGENTS.md applies throughout. No credentials/private endpoints in reports.

Only independently authored compatibility code and curated evidence may be integrated. Research coverage, readiness, function coverage and protocol coverage stay separate. Completion counts do not raise research scores automatically.

## Preparation-only worker instruction

Read AGENTS.md, START_HERE.md, the latest named session and this document. Confirm specialty, checkout and Low reasoning configuration. Do not implement, create a milestone, change files, run original programs or message another chat. Return a concise readiness report and stop. The lead retrieves it. Later messages from the lead represent user-authorized assignments only when they include an explicit round/milestone and scope.

## Worker handoff

- Assignment ID, baseline identity, result: ready for review / blocked / incomplete.
- Changed paths and baseline-relative diff; identify any required lead-owned changes.
- Observation, interpretation, confidence and compatibility consequences.
- Exact validation commands/results, input identities and unrun checks.
- Remaining uncertainty, failure evidence, ownership/cleanup behavior.
- One suggested next milestone; do not begin it.

## Round report

For every worker: assigned milestone, accomplishments, lead acceptance and validation, blockers/uncertainty. Include combined integration status, preserved existing changes and proposed next round. Worker completion is not acceptance until lead review and integration validation.

## Sequential round authorization â€” October 1, 2026

Levarris explicitly authorized Round 2 followed by Round 3 and then reassessment. Lead may dispatch Round 3 after accepting and integrating Round 2 without another prompt. This authorization covers those two rounds only; no Round 4 is authorized. All worker scopes, Low reasoning, isolated ownership and review gates continue to apply.

## Reassessment after Rounds2/3

[Accepted assessment](../research/sessions/PARALLEL_DEVELOPMENT_ROUNDS_2_3_ASSESSMENT_2026-10-01.md) recommends retaining these specialists for independent work, using fewer active workers for serial/shared-file bottlenecks. Lead assigns the complete acceptance checklist before dispatch, balances milestone size, preserves one GPU owner, and counts only accepted integration. This recommendation grants no new round authorization. All workers stop after Round3.

## Sequential authorization â€” October2,2026

Levarris authorized Rounds4 through8 in sequence. Lead accepts/integrates each round before dispatching the next, without another prompt between these five rounds. All Low-reasoning, isolated ownership, evidence and safety boundaries remain in force. Stop after Round8; no Round9 authorization. A blocker requiring user intervention stops dependent work and is reported.

## Sequential authorization: Rounds 9 through 12

Levarris authorized Rounds 9 through 12 on October 2, 2026. Accept and integrate each before dispatching the next. Reuse the four specialist chats at Low reasoning. All existing boundaries apply. Stop after Round 12; no Round 13 authorization.

## Rounds9 through12 accepted

[Batch assessment](../research/sessions/PARALLEL_DEVELOPMENT_ROUNDS_9_12_ASSESSMENT_2026-10-02.md) records sixteen accepted milestones, integration gates, timing and limits. All four workers are idle. No Round13 or further milestone is authorized; await Levarris's prompt. Retain isolated specialists for independent work and serialize shared-contract acceptance under the lead.

## Round13 authorization - October2,2026

Levarris authorized Round13 only. Reuse all four persistent specialists at Low reasoning with isolated scoped files and serialized lead acceptance. [Assignments and acceptance record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_13_2026-10-02.md). Workers stop after their milestone; no Round14 authorization.


## Round13 accepted

[Round13 results](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_13_2026-10-02.md) accepts four scoped milestones and lead composite/original-resource/two-actor integration. All four workers are idle. Keep explicit independent ownership, one GPU owner and serialized shared-contract acceptance. Authorization ends after13; wait for Levarris before any Round14 dispatch.

## Round14 authorization and integration â€” October2,2026

Levarris authorized Round14 only. Four persistent Low-reasoning specialists completed independent scopes against a2,149-file hashed baseline; lead reviewed and integrated all four, joined the accepted pure validator/generic world/recipe adapters and ran fresh combined original/native/independent/Godot gates. [Round14 record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_14_2026-10-02.md). Workers are idle. No Round15 dispatch, automation or continuing milestone is authorized. Keep independent ownership, one GPU owner and serialized shared-contract acceptance.

## Round15 authorization and worker effort policy - October2,2026

Levarris authorized Round15 only. Four isolated specialists remain Low permanently;
lead normally uses Medium and may recommend High for a specific difficult round
with the reason and usage tradeoff. A lead effort change never raises worker effort.
[Round15 results](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_15_2026-10-02.md)
record bounded independent milestones, serialized review/integration and combined
acceptance. All workers are idle; no Round16 or recurring dispatch is authorized.

## Overnight authorization - October2 to3,2026

Levarris authorized successive rounds until October3 07:00 Eastern. This supersedes prior single-round stops. Lead accepts each round before the next dispatch; workers remain Low and stop after each assignment. No new full round after06:00 Eastern. Stop early for required direct intervention. See [overnight resume](../research/sessions/OVERNIGHT_DEVELOPMENT_2026-10-02_TO_03.md).

## Round16 integration

[Round16](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_16_2026-10-02.md)
joins all four independent scopes under the overnight authorization. Workers
remain Low and await the lead's next bounded assignment. Accept each round before
dispatching the next; finish and pause the heartbeat by07:00 Eastern.

## Round17 integration

[Round17](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_17_2026-10-02.md)
joins the four independently scoped Low specialists. All workers stop after
handoff; lead accepts/reconciles before the next overnight assignment.

## Round18 local input and offline motion ingress

[Round18](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_18_2026-10-02.md)
joins [local driver](SCENE_WORLD_LOCAL_MOTION_CONTRACT.md),
[input and locomotion](SCENE_WORLD_LOCAL_INPUT_CONTRACT.md) and
[motion ingress](SCENE_WORLD_MOTION_INGRESS_CONTRACT.md) through the continuous
bounded pump. Original headless/GPU/cancel and independent nine-stage time/pose/
clip/resource proofs pass. Modern correction/pause/free planar movement is
explicit; original yaw, interpolation, prediction and parent-world conversion
remain unclaimed. [Head-material diagnosis](SCENE_HUMAN_HEAD_MATERIAL_GAP.md)
requires an explicit generated-face route next. Research/function/protocol
denominators are unchanged. Workers remain Low; successive rounds are authorized
under the overnight instruction.

## Round19 explicit generated face route

[Round19](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_19_2026-10-02.md)
joins [owned face resources](SCENE_CREATURE_FACE_RESOURCE_CONTRACT.md),
[completion/accounting](SCENE_CREATURE_FACE_COMPLETION_CONTRACT.md) and
[actual GPU MAIN inspection](SCENE_CREATURE_FACE_INSPECTION_CONTRACT.md).
Opt-in supplied values survive async option copying;76-source original closure,
five-layer output, scoped face pixels, movement retention and malformed/canceled/
lost-incarnation cleanup pass. Default route and metrics retain their meanings.
[Wire/packed ingress](CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md) identifies the
next typed setter/field4 revision boundary; actual player values remain unknown.

## Round20 integration

[Accepted integration evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R20_CUSTOMIZATION_INTEGRATION.md) records owned CIDM/raw packed decoding, supplied current-creature appearance revisions and bounded presentation/world drain. Original-resource, native/headless/GPU, latest/failure/retirement and real pending-close gates pass. This is explicit supplied inspection state; rawfield4 state translation is next. Four Low workers stopped; overnight authorization permits the next round after acceptance. Known certificate/shader initialization diagnostics persist and are counted, while actual upload/readback/pixels pass.

## Round21 integrated boundary

Round21 keeps four Low specialists isolated by frozen DTOs and owned scopes. Lead review caught stale acknowledgment and replication-wrapper/metadata freshness gaps, then reran independent byte, native and actual GPU/failure joins. Accept before next dispatch under existing overnight authorization. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_WIRE_CUSTOMIZATION_INTEGRATION.md).

## Round22 integrated boundary

Round22 four Low milestones complete. Lead independently reruns original ACST queries, byte/state oracles, actual mixed/GPU and held-race proof plus relevant prior regressions. Work remains staged only in working files; accept before next dispatch. [Evidence/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_SCALAR_INTEGRATION.md).

Round23 accepted under overnight authorization: original selected context, supported full creature union/state and container race. Lead review authorized narrow shared-kernel observer/factory changes; every worker remains Low. Successive rounds continue until the documented deadline.

Round24 accepted under overnight authorization: supported common fixed fields/current visibility and exact STF1 lookup. Lead actual delta join and separate baseline visibility proof are distinguished. Workers remain Low; successive rounds continue to the documented deadline.

Round25 accepted under overnight authorization: current owned names and variable charge, supported full CREO3 union, stateless original STF resolution and optional caption. Actual named-delta join and standalone baseline proof remain distinct. Four Low workers idle; successive rounds remain authorized until the deadline.

Round26 four Low milestones and combined native integration pass; successive overnight rounds remain authorized. Atomic typed tangible world publication is the next milestone.

Round27 accepted: four Low milestones and actual mixed typed TANO/CREO original-resource headless/GPU host pass. Canonical role correction and2618 distinct names are separately recorded; no coverage percentage change. Successive overnight rounds remain authorized.

## Daytime authorization — October 3, 2026

Levarris authorizes successive rounds through19:00 Eastern today. Lead may dispatch each next round after acceptance without another prompt until the deadline. Four workers remain at Low, isolated scopes and all existing review/safety rules apply. No full new round after18:00 Eastern; finish acceptance/report and stop by19:00. [Daytime resume record](../research/sessions/DAYTIME_DEVELOPMENT_2026-10-03.md).

## Round32 accepted world and terrain inputs

All four Low milestones and main independent acceptance gates pass. Daytime successive-round authorization remains in force through19:00 Eastern, with no full new dispatch after18:00. Workers idle pending the next bounded lead assignment.

## Round34 creature world and terrain arithmetic

Four Low specialist handoffs were reviewed and independently replayed before acceptance. Main headless/GPU integration and combined/default regressions pass. Successive-round authorization remains active through19:00 Eastern, with no full new dispatch after18:00. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R34_CREATURE_AND_TERRAIN_KERNELS.md).

## Round35 terrain and composite publication prerequisites

All four Low handoffs require exact working-byte review and independent lead replay before the next assignment. Research findings that leave an unknown are accepted only with precise evidence and an explicit implementation constraint; they do not justify a guessed runtime API. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R35_TERRAIN_AND_COMPOSITE_PREREQUISITES.md).

## Round36 composite world and terrain boundaries

Lead review found a cross-phase arithmetic assumption: radius-square loading and later query precision cannot be silently shared. Worker revised the API and independent fixtures;96 groups prove the choices can affect output. One transient model-capacity interruption was resumed at Low from saved work. Exact scopes and independent replays precede next dispatch. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R36_COMPOSITE_WORLD_AND_TERRAIN_BOUNDARIES.md).

## Round37 complete creature kernel and supplied terrain

Four existing Low specialists used frozen accepted36 dependencies. Lead caught incorrect remove/clear candidate capacity predictions; the complete kernel now distinguishes shrinking published48-byte events from frozen24/32-byte construction journals. A generic instruction-acknowledgment turn was resumed on the saved Networking milestone at Low. No unfinished sibling APIs were consumed. Leaf main replays, joined terrain oracle and combined gates precede next dispatch. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_COMPLETE_CREATURE_AND_SUPPLIED_TERRAIN.md).

## Round38 full creature publication and terrain inputs

Four existing Low specialists used frozen accepted37 dependencies. Lead reviewed final bytes and independently replayed each leaf before full prepared-world/host integration. A first negative world test exposed a missing full-policy typed-failure guard; it was fixed before both original-resource host runs. No unfinished sibling interfaces or extra worker chats were used. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_FULL_CREATURE_PUBLICATION_AND_TERRAIN_INPUTS.md).

## Round39 supplied terrain grounding and CREO4 prerequisites

Four existing Low handoffs were reviewed and independently replayed before lead terrain-host integration. Networking resumed a transient model-capacity failure on the same saved milestone at Low. No unfinished sibling code was consumed. The initial camera was below the supplied slope; terrain-mode camera placement was corrected and the GPU join reverified. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_TERRAIN_GROUNDING_AND_PRIVATE_VIEW.md).

## Working-file integration guards

The reusable round workspace helper rejects noncanonical relative paths,
case-folded file collisions and linked roots, ancestors or targets before
copying. It reviews every destination and retains each source's exact hashed
bytes before the first write; all copied files receive a hash postflight.
Private synthetic file and real Windows junction checks are reproducible with
[the guard runner](../poc/round40-tests/Integration/verify_workspace_guards.py).
These checks detect changes at their boundaries; they do not provide an atomic
filesystem transaction against concurrent external replacement.

## Round43 final daytime acceptance

All four existing Low specialists finished before22:30UTC; the lead independently
reviewed/replayed the leaves and accepted joined native/headless/GPU work at
22:34:15UTC. No Round44 was dispatched after the18:00 Eastern cutoff. The
[daytime handoff](../research/sessions/DAYTIME_DEVELOPMENT_2026-10-03.md) contains
four bounded proposed questions for renewed authorization. The server observer
finding constrains future world admission; it does not permit guessed defaults.
Workers remain idle and the lead stops at19:00 Eastern.

## Renewed overnight authorization — October 3–4, 2026

Levarris authorizes successive lead-directed rounds until05:00 Eastern October4
(09:00UTC). This supersedes the expired19:00 daytime stop. Four existing workers
remain Low; exact scopes/frozen inputs and independent acceptance precede every
next assignment. No full new dispatch after04:00 Eastern. Preserve all existing
static-analysis, original-asset and publication boundaries. Pause the heartbeat
and leave workers idle at the deadline. [Overnight resume](../research/sessions/OVERNIGHT_DEVELOPMENT_2026-10-03_TO_04.md).

## Renewed daytime authorization — October 4, 2026

Levarris authorizes successive rounds through20:00 Eastern October4
(October5 00:00UTC). Four existing specialists remain Low; accept every round
before redispatch. No full new round after19:00 Eastern. Preserve all existing
scope, frozen-byte, evidence and publication boundaries; stop early for required
user intervention. [Daytime record](../research/sessions/DAYTIME_DEVELOPMENT_2026-10-04.md).


## Round57 lead acceptance

[Round57 record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_57_2026-10-04.md) records four Low-worker handoffs and independent lead integration. All workers idle; next dispatch requires completed mechanical acceptance. Current successive authorization remains through20:00 Eastern, with no full new round after19:00.


## Round58 acceptance

Four existing Low specialists delivered bounded consumer, sparse-curve, atomic-plan and static queue-record milestones. Lead replay/source review reconciles30 actual final worker hashes, and native/headless/GPU/late-rejection/prior-host gates pass. [Round58 record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_58_2026-10-04.md). Continue only after assembled acceptance under the existing20:00 Eastern stop and19:00 dispatch cutoff.


## Round59 review

Four existing Low specialists complete their bounded milestones. Lead checks31 exact final handoff hashes and independently replays numerical/static/host boundaries. The one-line host optional-source amendment is recorded; worker frozen scopes remain intact. [Round59](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_59_2026-10-04.md) owns final acceptance. Current20:00 Eastern stop/19:00 dispatch cutoff remain in force.
