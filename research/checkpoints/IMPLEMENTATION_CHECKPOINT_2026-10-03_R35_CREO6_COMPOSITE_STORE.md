# Round35 CREO6 composite store implementation checkpoint

Date: 2026-10-03. Question: can the accepted composite CREO6 kernel publish full immutable state and ordered scalar/HAM events through an owner-thread store, with global budgets and exact prepared/batch ownership, without changing the kernel? Stop condition: assigned store/scenario/contract/checkpoint plus own runner pass synthetic native/oracle tests and isolated checkout scope validation.

## Inputs and boundaries

The frozen Round35 public overlay contains 2,905 files. Manifest SHA-256: `edd3e8aac46561b3dc1ff3349d590e32149fe0ceb4b3aab1639d7d5df0b91e6a`; archive SHA-256: `b60f83b6d137925d800ff4f3af8beabd8ed16112edf5641110e97a3e912e5ee0`. Main snapshot HEAD: `5493338d9cd4255cf58f25de6c4cd15f6507b818`; worker HEAD: `65de8c88141071744dc75594be126b3bab3696a5`.

Preflight checked exact archive membership, unique/canonical paths, size/hash, containment, and symlink/junction/reparse rejection before writing. Postflight verified every listed public file. Private/unlisted material was preserved. Orientation used START_HERE, the accepted Round34 session, current status/ledger/unknown-function terminology, prior checkpoints, and accepted composite/store contracts. No executable VA was used, so no new executable identity claim is made. No original execution, server contact, asset extraction, download, publication, commit, or staging occurred.

## Observation, interpretation, consequence

Observation: the new store delegates seed/apply to unchanged `CreatureView6CompositeState`; its candidate construction builds immutable snapshot, map, metadata-only queue, totals, and checked revision/epoch before assignments. Exact expected snapshot, registry handle/incarnation, active prepared candidate, and issued batch identities gate publication/acknowledgement.

Interpretation: Confirmed by source and synthetic tests, failed writes retain the preceding publication and journal. Same-incarnation handle changes carry state; recreation resets composite revision without resetting global store revision. Historical event ownership is distinct from live event delivery authorization.

Consequence: preserve the [durable contract](../../docs/SCENE_CREATURE_VIEW6_COMPOSITE_STORE_CONTRACT.md) at future world integration. Check incarnation before delivery. Trusted prepared callers must check immediately before commit while the gate is held and prohibit intervening callbacks/registry mutation.

Observation: effective limits subtract other actors' logical state and all pending 24-byte event charges before invoking the frozen kernel. Native cases reject state/events/queue/output/work/revision overflow, global multi-actor overflow, intermediate growth, invalid policy, stale/cancelled/consumed prepared tokens, foreign snapshots/batches, cross-thread use, and unsupported vector deltas.

Interpretation: Confirmed bounded logical-resource policy, not a measured CLR heap or total CPU budget. Message work and kernel work remain separately enforced. Defender/wearable delta support is absent; baseline retention is supported.

Consequence: keep logical accounting semantics explicit and do not reinterpret this prototype as complete CREO6/world delivery coverage.

## Reproduction and results

Use .NET 8 with the existing offline package cache and the bundled Python runtime. Restore the own runner with its clear-source NuGet.Config and `-p:NuGetAudit=false`; build with `--no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false`.

```powershell
dotnet restore poc/round35-tests/Runtime/Runtime.csproj --configfile poc/round35-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round35-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round35-tests/Runtime/bin/Debug/net8.0/Runtime.dll poc/round35-tests/Runtime/ordered-fixtures.json <ignored-receipt-path>
python poc/round35-tests/Runtime/peer.py verify <ignored-receipt-path>
python tools/python/validate_organized_workspace.py .
git diff --check
```

Native result: `R35_CREO6_COMPOSITE_STORE_PASS checks=521 budgets=373 sequences=34 cases=12`. Build: zero warnings/errors. Independent oracle: 34 exact fixture sequences, 106 ordered events, full initial/final models, revisions, sizes, and work agree. Fixtures are synthetic and mechanically regenerated using the accepted independent Round34 oracle.

Final scope gate passed: exactly nine additions (four assigned public files and five own-runner files), zero changed baseline files, all 2,905 baseline files byte-identical, and HEAD/index preserved. Evidence, receipt, and build output are ignored. Workspace validation passed with 1,340 Markdown files, zero broken links, zero links into private material, 2,626 distinct provisional rows, and 345 packet rows. `git diff --check` and explicit UTF-8/LF/no-BOM/trailing-whitespace checks on all nine new files passed. The focused diff and Git status were inspected; the overlay-related pre-existing status was preserved. No shared living ledger/status edit is authorized in this worker scope.

Synthetic fixture SHA-256: `6f2706377dc3301b8f54fbedfce19622e7a0e942a868089308290505717cbcb9`. Local execution used bundled Python at `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`, the pre-existing package cache at `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages`, and a round-specific temporary APPDATA. No packages were downloaded.

## Remaining uncertainty and next probe

No world transaction, dispatch route, networking acceptance, or live callback behavior is claimed. Next bounded probe: world-owner integration of this prepared candidate with other publication participants, including failure before commit, destruction/recreation before delivery, stale batch acknowledgement, and deterministic cleanup. The accepted kernel's work accounting bounds remain distinct from total runtime costs.
