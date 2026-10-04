# Round38 complete CREO6 prepared store checkpoint

Date: 2026-10-03. Question: can the accepted complete18-field kernel publish incarnation-bound immutable full models and historical metadata-only48-byte journals through a real prepared store, preserving prior state/receipts after late failure, cancellation, staleness and exhaustion? Stop condition: assigned new store/scenario/contract/checkpoint and own runner pass independent all18 fixtures, public lifecycle/global budget checks, isolated source-linked prepared-slot checks, actual separate Core/SmokeTests builds and exact scope validation.

## Input pins, orientation and boundaries

Approved Round38 overlay:3030 public files. Manifest SHA-256 `d28af4bf1510c02eb3e6f91b1232fc9d7e3318021a294691c4366cd662a8ba76`; archive SHA-256 `c60ea7c6452d3cbf4920373f19517973afdf9c95b48dc6b8f28b9043f948ee36`. Main snapshot HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`; worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`.

Every member passed exact listed set/count, canonical/casefold uniqueness, size/hash, containment and target/ancestor symlink/junction/reparse preflight before the first overlay write. Postflight checked all3030 listed bytes and preserved private/unlisted material, actual HEAD/index. Orientation covered START_HERE/latest accepted37, repository instructions, current status/ledger/unknown-function terminology, accepted35 store/36 world patterns, accepted37 full kernel/construction policy and checkpoint, and existing curated/tools/runners. No executable VA was used or new original identity claim made. No sibling Networking38 interface, world/host/shared-file change, original execution, live contact, download, new extraction, staging, commit or push occurred.

## Observation, interpretation, confidence and consequence

Observation: the new [store](../../poc/Stardust.Core/Session/SceneCreatureView6FullReplicationStore.cs) wraps frozen CreatureView6FullState, validates/copied policies, and binds current registry handle/incarnation plus exact owner/expected snapshot. Candidates contain prebuilt complete state/map/journal/totals/checked revision/epoch, active slot identity and consumed marker. All store methods are owner-thread/gate protected. CheckPrepared precedes assignment-only CommitPrepared; finally release is required. No callbacks run.

Interpretation: Confirmed modern bounded prepared publication for these participants; stale/cancelled/foreign/consumed tokens cannot publish through the checked path. Consequence: future world integration must check immediately before commit, prohibit intervening registry mutation/callbacks, and release in finally. Commit is trusted internal assignment-only code, not a public validation API.

Observation: immutable full models preserve baseline metadata, raw strings/prefix spelling, complete vectors/entries/counters and current values. A valid whole plan advances one global store and one outer kernel revision. Same-incarnation handle moves carry state; reseed continues state revision; recreation starts state revision one. Read-side retirement removes stale map entries but keeps historical journal; failed writes do not prune stale other actors. Exact issued batch owner/reference/revision/epoch authorizes acknowledgement; IsCurrentEvent separately checks current incarnation and index. Close leaves the borrowed registry open.

Interpretation: Confirmed current publication identity and historical journal ownership are distinct from eventual recipient delivery. Consequence: host adds its world generation/finalized phase guard and immediate serialized delivery; historical acknowledgement is allowed without a live recipient. Caller-held old values are outside store retention.

Observation: effective caps reserve all other actors' retained logical bytes and48 per already pending full event. Independent exact/one-less store output peaks include frozen24/32 child construction reservations, including long wearable remove/clear; frozen kernels are unchanged. Full logical work includes whole-message consumption plus public adapter overhead and is not charged again by the store.

Interpretation: Confirmed logical aggregate policy and late/intermediate rejection without a prefix; not a heap or total CPU ceiling. Consequence: preserve full48 journal policy and narrow construction peaks separately, and keep bounded copy/scan overhead explicit.

Canonical API, lifecycle, prepared seam, accounting and delivery distinctions are in [the durable contract](../../docs/SCENE_CREATURE_VIEW6_FULL_STORE_CONTRACT.md).

## Reproduction and verification

Use .NET8 and bundled Python `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`. Set NUGET_PACKAGES to pre-existing `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages` and APPDATA to a round-specific temporary directory. The own NuGet.Config clears all package feeds; NuGetAudit=false and no downloads. Own Runtime uses a ProjectReference to the actual separate Core assembly and only the assigned public scenario. Runtime.Internal compiles Core sources into its own assembly; internal access/fault injection exists only in InternalProgram.cs. No production test accessors were introduced.

```powershell
python poc/round38-tests/Runtime/peer.py generate poc/round38-tests/Runtime/ordered-fixtures.json
dotnet restore poc/round38-tests/Runtime/Runtime.csproj --configfile poc/round38-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round38-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round38-tests/Runtime/bin/Debug/net8.0/Runtime.dll poc/round38-tests/Runtime/ordered-fixtures.json <ignored-store-receipt>
python poc/round38-tests/Runtime/peer.py verify <ignored-store-receipt>
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
```

For Runtime.Internal restore/build, use its same own clear-feed config and flags plus absolute private BaseIntermediateOutputPath and OutputPath (worker `local-private/round38/internal-obj/` and `internal-bin/`). The project has no ProjectReference, excludes Core bin/obj source, and reuses the existing cached BCnEncoder.Net2.3.0 package. Explicitly run its private `Runtime.Internal.dll <ignored-internal-receipt>`.

```powershell
python tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
git status --short
```

Final public native result: `R38_CREO6_FULL_STORE_PASS checks=2773 budgets=1453 sequences=97 cases=15`. Source-linked internal result: `R38_FULL_STORE_INTERNAL_PASS checks=26 cases=4`. Actual Core, separate SmokeTests, public runner and internal runner build with zero warnings/errors.

The [independent oracle](../../poc/round38-tests/Runtime/peer.py) regenerates compact all18 synthetic fixture bytes from the accepted37 independent Python producer, parses the union and independently derives initial/current raw models, all counters, exact ordered descriptions, full work and every capacity peak. It agrees on97 publications and494 ordered events, including all eighteen fields, vector skip/wrap/equality/defaults, cross-partition growth/shrink and narrow construction peaks. Native exported work is a separate frozen kernel replay from the seed; store work limits are exercised through actual exact/one-less preparation. Native output is never an input to expectation generation.

Public adverse cases cover state/events/queue/output/work/revision, foreign/owner/close, aliases, late failure, same-incarnation move, destroy/recreate/reset/generation, reseed, stale other-actor capacity, read retirement, global other-actor/history charges, no-op publication with a full queue, and exact/repeated/stale historical acknowledgement/delivery. Public scenario uses public Core APIs only. Internal cases cover stale handle, active gate/slot, cancellation, foreign tokens, cross-thread check/release, consumed/repeated release, recreated/reset recipient before check, and modeled epoch exhaustion for ack/build/retirement. Epoch fault injection is private reflection solely in the own source-linked runner.

Failed attempts: initial native/build passed, but first oracle adapter passed the parsed tagged union to the flattened independent apply routine, yielding KeyError Text. After flattening, the oracle detected two missing native export keys (OldId/NewId); the public scenario export was completed. Final independent replay passes with every full event field. These corrections changed only test adapter/export, not production semantics. Detailed failure notes/build/native/oracle receipts remain ignored.

Final exact scope passed: eleven additions (four assigned public files and seven own-runner files), zero changed existing files, all3030 baseline files byte-identical, worker HEAD/index preserved, and detailed evidence/build output ignored. Workspace validation passed:1372 Markdown files, zero broken/private links,2626 distinct provisional rows/zero duplicates,345 packet rows. `git diff --check`, explicit UTF-8/LF/no-BOM/final-newline/trailing-whitespace checks, Python AST and project/config XML parsing pass. The focused source/scenario/contract diff and final overlay-related status were inspected; private/unlisted material is preserved. Synthetic fixture SHA-256 `e7fca8d758d7dac792e132fff245a06995a52c2d3b6a1fde7cee29c28b82e2e3`. Shared living status/ledger reconciliation belongs to lead acceptance.

## Remaining uncertainty and next milestone

No full-union world ingress/delivery, original owner gameplay effects, target/resource resolver or live interoperability is established here. Next action: lead wraps this prepared full store in a separate optional world/router partition, preserving exact current generation/incarnation/expected publication and issued-batch identities,48-byte logical charges, frozen construction/work policy and callback-attempt stamps before prepare. Then validate original-resource host full-model/event publication with immediate current recipient guards. Research/readiness/function/protocol denominators remain separate and unchanged.
