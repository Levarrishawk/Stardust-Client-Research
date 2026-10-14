# Round36 optional composite CREO6 world checkpoint

Date: 2026-10-03. Question: can the frozen accepted composite store/kernel publish scalar/HAM state and metadata-only events together with a serialized world journal, while preserving scalar/default behavior and later-failure ownership? Stopping condition: assigned coordinator/router/scenario/contract/checkpoint and own runner pass independent synthetic full-model/event/work oracles, lifecycle/callback/budget negatives, accepted coexistence regressions, workspace validation and exact scope gates.

## Inputs and scope

Round36 overlay: 2,937 public files. Manifest SHA-256 `766d99456ebd6b3f781e14a2e07122d142ea26d085230349fe402e070aae756e`; archive SHA-256 `871bca6403228b05ac7a43fea2942ea4b20c24cc0b5053618a38aad72ea1f0c1`. Main snapshot HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`; isolated worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`.

Before any overlay writes, all members passed exact membership, canonical/casefold uniqueness, size/hash, containment, and target/ancestor symlink/junction/reparse checks. Postflight verified all listed bytes; private/unlisted material and HEAD/index were preserved. Orientation included AGENTS, START_HERE/latest accepted35, current status/ledger/unknown-function terminology, accepted scalar world/composite kernel/store contracts and their checkpoints, existing runners and curated/tool discovery. No original executable VA, original execution, live contact, new extraction, download, sibling Round36 code, staging, commit, push, or historical implementation path (omitted) edit was used.

Only existing SceneWorldCoordinator is modified. Four assigned public additions and five own-runner files are added; frozen codecs/states/stores/scalar router and shared production remain unchanged. The lead owns living ledger/status reconciliation after acceptance.

## Evidence and interpretation

Observation: both constructors append an independent composite policy and reject simultaneous scalar/composite CREO6. World snapshots and change batches carry the separate composite wrapper. A full baseline seeds the frozen store before the creature end barrier; exact generation/incarnation/publication plus finalized family gate deltas. Static TANO and prior CREO3 partitions remain separate. Confidence: Confirmed by build and native constructor/coexistence cases.

Observation: world journal allocation/capacity/revision checks precede frozen store preparation; store CheckPrepared precedes callback-free prebuilt store/world assignments; finally releases preparation. Native replay of frozen35 covers stale/released/consumed candidate identity. World failure cases cover overflow, exhausted journal, global state/output/event capacity, and late HAM growth following a scalar assignment. Interpretation: bounded serialized publication is atomic for these participants; callbacks cannot intervene during commit. Confidence: Confirmed for source and deterministic modern cases. Consequence: trusted prepared callers must preserve the immediate check/commit serialization rule; no external rollback is claimed.

Observation: whole-world callback stamps now include composite publication revisions. Event drain/ack and rejected mutation attempts advance callback-attempt state; returned plans after mutation/reentry/disposal/generation change reject. Pure reads pass. Router failure closes only its references. A held resource job remains owned and uncancelled after later invalid active composite ingress, then cancels/drains on world close. Confidence: Confirmed by scenarios. Consequence: preserve prior presentation/resource ownership on rejected exact active packets; do not claim callback side effects can be undone.

Observation: recreation retains historical events but delivery guard checks exact current issued batch, generation, incarnation and finalized recipient. Historical batch acknowledgement remains distinct from delivery permission. Logical retained charges include all mapped full models plus 24 per pending event; global other-actor and earlier-event capacity tests pass. Interpretation: exact journal ownership, modern logical accounting and current recipient authorization are separate. Confidence: Confirmed. Consequence: host must guard immediately before delivery, with no intervening mutation.

Canonical API, policies, failure behavior and limits are consolidated in [the durable contract](../../docs/SCENE_WORLD_CREATURE_VIEW6_COMPOSITE_CONTRACT.md).

## Reproduction

Use .NET 8, existing offline package cache, and bundled Python. Set NUGET_PACKAGES to the existing cache; use a round-specific temporary APPDATA. Local paths used: Python `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`; package cache `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages`. No packages were downloaded.

```powershell
python poc/round36-tests/Runtime/peer.py generate poc/round36-tests/Runtime/ordered-fixtures.json
dotnet restore poc/round36-tests/Runtime/Runtime.csproj --configfile poc/round36-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round36-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round36-tests/Runtime/bin/Debug/net8.0/Runtime.dll poc/round36-tests/Runtime/ordered-fixtures.json <ignored-world-receipt> poc/round34-tests/Runtime/ordered-fixtures.json <ignored-scalar-receipt> poc/round35-tests/Runtime/ordered-fixtures.json <ignored-store-receipt>
python poc/round36-tests/Runtime/peer.py verify <ignored-world-receipt>
python poc/round34-tests/Runtime/peer.py verify <ignored-scalar-receipt>
python poc/round35-tests/Runtime/peer.py verify <ignored-store-receipt>
python tools/python/validate_organized_workspace.py .
git diff --check
```

Native: `R36_CREO6_COMPOSITE_WORLD_PASS checks=816 sequences=34 cases=40`; unchanged scalar world `checks=836 sequences=7 cases=30`; unchanged composite store `checks=521 budgets=373 sequences=34 cases=12`. Build zero warnings/errors. Independent oracles match 34 mixed full models/106 composite events, accepted scalar 112 transitions/308 events, and frozen store full models/events/work/budgets. All fixture bytes are synthetic. Composite WorkUsed export comes from a separate frozen kernel replay of the exact pre-publication state; the store/world exposes no work receipt. World work ceilings are checked by reject/retention cases.

Final scope passed: exactly one existing modification (SceneWorldCoordinator) and nine additions (four assigned public additions and five own-runner files); all 2,936 other baseline files are byte-identical. Worker HEAD/index are preserved and receipts/build outputs are ignored. Workspace validation passed: 1,350 Markdown files, zero broken links, zero private links, 2,626 distinct provisional rows and 345 packet rows. `git diff --check` and explicit UTF-8/LF/no-BOM/trailing-whitespace checks on the ten scoped files passed. The focused coordinator/source/scenario diff and final status were inspected against the overlay baseline; unrelated overlay status was preserved.

Synthetic fixture SHA-256: `82d5ddc78c772085bce9a7c3d2713d033c3d532c04e3590de14ac9552a466928`. Independent fixture regeneration is exact. Detailed receipts/build outputs remain ignored; curated documents contain no private links.

## Remaining uncertainty and precise resume

This worker does not execute the original mixed host or deliver callbacks. Next action: lead registers the new router above existing ingress, chooses composite instead of scalar CREO6 policy, queues a complete baseline before end-baselines, and validates ordered scalar/HAM full-model/event delivery with the current-event guard against original resources. Defender/wearable deltas, original owner effects, complete gameplay and live server interoperability remain open. No coverage/readiness denominator changes are claimed.
