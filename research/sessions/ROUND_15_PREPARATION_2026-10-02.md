# Round 15 preparation

October 2, 2026. Preparation only; no worker dispatch or implementation.

## Objective and evidence

Turn the accepted mixed creature/static preview into a reusable acknowledged
presentation consumer. Success means one bounded consumer can maintain actors
from generic world publications and change batches, without hardcoded object IDs.

Observation: the [Round 14 report](PARALLEL_DEVELOPMENT_ROUND_14_2026-10-02.md)
and [mixed preview](../../../SWG-Godot-Client/poc/Stardust.Godot/SceneMixedWorldPreview.cs) show
explicit publication, state application, retirement and recreation for two actors.
The existing [creature consumer](../../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldCreatureStatePresentation.cs)
remains creature-specific. Confidence: Confirmed by source inspection and the
accepted Round 14 tests. Consequence: reuse the established coordinator and visual
adapters; do not introduce another registry or resource scheduler.

Canonical inputs are the [mixed world contract](../../docs/SCENE_MIXED_WORLD_CONTRACT.md),
[preview contract](../../docs/MIXED_WORLD_PREVIEW_CONTRACT.md), existing catalog,
and independent Round 14 peers. This milestone remains offline, with original
assets read-only and synthetic static replication explicitly opaque.

## Proposed independent worker scopes

These are planning boundaries, not dispatch-ready assignments. Before dispatch,
the lead must capture a fresh hashed dirty-tree baseline and specify exact allowed
paths, frozen interfaces and acceptance commands under the
[operating rules](../../docs/PARALLEL_DEVELOPMENT_RULES.md).

| Specialist | Bounded assignment | Independent input and handoff |
| --- | --- | --- |
| Runtime | Define and implement bounded engine-neutral presentation bookkeeping, if inspection confirms it belongs in Core; preserve separate resource-attempt and state-incarnation freshness | Existing generic publications and acknowledged changes; deterministic fake-adapter tests; proposed interface frozen by lead before dispatch |
| Rendering | Build recipe adapters over the accepted creature/static visuals, with owned retirement and explicit terminal inspection policy | Existing visual APIs; local adapter tests independent of unfinished runtime implementation |
| Networking | Author an independent mixed lifecycle sequence and expected observations for acknowledgment retry, delayed completion and recreation | Existing accepted wire/world APIs; does not change transport or add packet-schema claims |
| Assets | Verify that existing owned completions remain reusable across state-only changes and isolated recreation; provide focused fixture expectations for both recipes | Existing router and pure validator; no new formats, corpus survey or additional resource loader |

Lead owns shared entry points, integration harness, consumer-to-adapter wiring,
canonical reconciliation and combined acceptance. Assets may finish early; do not
invent extra work to occupy every worker. Keep one GPU owner.

## Acceptance cases

1. Creature and static actors attach from generic publications without fixed IDs.
2. State-only creature updates preserve resource attempts and GPU identities.
3. Opaque static state is acknowledged without invented rendering semantics.
4. Destroy/recreate changes only the affected incarnation and resources.
5. Stale publication and stale acknowledgment cannot discard newer pending work.
6. Capacity exhaustion has a documented deterministic policy and no leaked roots.
7. Current failure retires the world; stale late faults are observed and retired.
8. Close/cancel releases visual roots, closes archives and drains outstanding jobs.
9. Existing creature-only APIs and Round 14 acceptance remain valid.

Use deterministic native/fake-adapter cases first. Run the combined original
human/terminal GPU, headless and real cancellation proof after integration.
The existing pixel/log oracle takes six arguments: GPU log, headless log,
cancellation log, and initial/updated/recreated PNGs. This is established by
[its argument unpacking](../../../SWG-Godot-Client/poc/round14-tests/Integration/verify_preview.py);
the earlier session's phrase "seven-input" is an editorial error.

## Resume procedure

On Levarris's next round authorization: inspect the current tree and this brief;
resolve the consumer interface and bookkeeping placement; capture baseline;
prepare the four isolated scopes; dispatch at Low reasoning; review and integrate;
run proportional native and combined visual gates; report and stop.

No builds, code changes, new executable claims, research-score changes, worker
messages, live contact or Git publication are part of this preparation.
