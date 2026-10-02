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

## Sequential round authorization — October 1, 2026

Levarris explicitly authorized Round 2 followed by Round 3 and then reassessment. Lead may dispatch Round 3 after accepting and integrating Round 2 without another prompt. This authorization covers those two rounds only; no Round 4 is authorized. All worker scopes, Low reasoning, isolated ownership and review gates continue to apply.

## Reassessment after Rounds2/3

[Accepted assessment](../research/sessions/PARALLEL_DEVELOPMENT_ROUNDS_2_3_ASSESSMENT_2026-10-01.md) recommends retaining these specialists for independent work, using fewer active workers for serial/shared-file bottlenecks. Lead assigns the complete acceptance checklist before dispatch, balances milestone size, preserves one GPU owner, and counts only accepted integration. This recommendation grants no new round authorization. All workers stop after Round3.

## Sequential authorization — October2,2026

Levarris authorized Rounds4 through8 in sequence. Lead accepts/integrates each round before dispatching the next, without another prompt between these five rounds. All Low-reasoning, isolated ownership, evidence and safety boundaries remain in force. Stop after Round8; no Round9 authorization. A blocker requiring user intervention stops dependent work and is reported.

## Sequential authorization: Rounds 9 through 12

Levarris authorized Rounds 9 through 12 on October 2, 2026. Accept and integrate each before dispatching the next. Reuse the four specialist chats at Low reasoning. All existing boundaries apply. Stop after Round 12; no Round 13 authorization.

## Rounds9 through12 accepted

[Batch assessment](../research/sessions/PARALLEL_DEVELOPMENT_ROUNDS_9_12_ASSESSMENT_2026-10-02.md) records sixteen accepted milestones, integration gates, timing and limits. All four workers are idle. No Round13 or further milestone is authorized; await Levarris's prompt. Retain isolated specialists for independent work and serialize shared-contract acceptance under the lead.

## Round13 authorization - October2,2026

Levarris authorized Round13 only. Reuse all four persistent specialists at Low reasoning with isolated scoped files and serialized lead acceptance. [Assignments and acceptance record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_13_2026-10-02.md). Workers stop after their milestone; no Round14 authorization.


## Round13 accepted

[Round13 results](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_13_2026-10-02.md) accepts four scoped milestones and lead composite/original-resource/two-actor integration. All four workers are idle. Keep explicit independent ownership, one GPU owner and serialized shared-contract acceptance. Authorization ends after13; wait for Levarris before any Round14 dispatch.

## Round14 authorization and integration — October2,2026

Levarris authorized Round14 only. Four persistent Low-reasoning specialists completed independent scopes against a2,149-file hashed baseline; lead reviewed and integrated all four, joined the accepted pure validator/generic world/recipe adapters and ran fresh combined original/native/independent/Godot gates. [Round14 record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_14_2026-10-02.md). Workers are idle. No Round15 dispatch, automation or continuing milestone is authorized. Keep independent ownership, one GPU owner and serialized shared-contract acceptance.
