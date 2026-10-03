# Round31 tangible view 6 publication store

Date: 2026-10-03 UTC. Question: can accepted TANO6 state be published for an exact current scene recipient with global bounded retention and an ordered journal, while failed writes preserve earlier valid work? Stopping condition: isolated store, independent accepted30 replay, negative/lifecycle checks, durable contract and scope audit; no world/router integration.

## Inputs and identity

The accepted Round31 baseline manifest contains 2754 files. Manifest SHA-256: `093a193c288ed5889c62317c57028a26f0afffa9c710b104a9c5f9bd2163b0fb`. ZIP SHA-256: `d80bb36fd1d71a63ff31d2eaf28a5d9dc7c90de197b5e0abd7bd2bf29f86dc70`.

Overlay verification checked archive names, bytes, hashes, containment and symlink/reparse paths before writes and checked the resulting complete listed baseline afterward. Checkout HEAD `65de8c88141071744dc75594be126b3bab3696a5` and its preexisting index were preserved. No executable virtual addresses are used; original executable identity verification is therefore not applicable to this implementation task.

Evidence inputs: accepted [state contract](../../docs/TANGIBLE_VIEW6_STATE_CONTRACT.md), [state policy](../../docs/TANGIBLE_VIEW6_STATE_POLICY.md), accepted30 independent Python peer, scene registry and existing store patterns. Tools: .NET 8 isolated native runner, Python independent oracle, SHA-256 snapshot audit and workspace validator. Synthetic fixtures derive from the compact accepted30 peer producer; no proprietary corpus is promoted.

## Findings, confidence and consequences

The [canonical store contract](../../docs/SCENE_TANGIBLE_VIEW6_STORE_CONTRACT.md) defines the implemented boundary and links the source, tests and fixtures. Direct modern implementation plus tests confirm exact current handle and exact expected stored-snapshot publication, atomic checked replacement, global logical state/output/event caps, owner-thread rejection, historical journal retention and exactly-once batch ownership acknowledgment.

Same-incarnation moved handles can carry state forward using the exact stored historical snapshot, even while its old publication is not current. Recreated recipients cannot. Failed writes retain earlier journal work. Stale other entries deliberately remain charged until a retirement read; a capacity rejection followed by explicit retirement and successful seed is tested. Historical events remain visible after removal/recreation; delivery requires the eventual host to validate the recorded incarnation independently.

Interpretation/confidence: these are Confirmed modern implementation policies, not claims about legacy client control flow. Kernel behavior is preserved from accepted30 state requirements and independently replayed. Same-thread reentry protection is inspected, not dynamically exercised. Registry concurrency is excluded by serialized-host ownership. `MaximumWork` applies to kernel work only; bounded object-map and journal copying is separately constrained by collection caps, with no whole-store CPU-work claim.

Compatibility consequence: callers can compose accepted immutable state with scene lifecycle tokens without callbacks, world adapters or additional packet parsing. Event acknowledgment is journal ownership, not live delivery authorization. The new event record retains metadata, not historical state graphs.

## Verification and reproduction

Native final result: `R31_TANO6_STORE_PASS checks=143 sequences=21 cases=9`. Independent oracle: passed; 23 transitions and 38 ordered events. Nine case groups cover state, queue, per-apply event, output, kernel-work and revision limits; journal/handle/recreation/reset; capacity/foreign-owner/close; and global multi-object state capacity. The final build has zero warnings/errors.

From repository root, with .NET 8 and Python available:

```powershell
dotnet restore poc/round31-tests/Runtime/Runtime.csproj --configfile poc/round31-tests/Runtime/NuGet.Config --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages -p:NuGetAudit=false
dotnet build poc/round31-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round31-tests/Runtime/bin/Debug/net8.0/Runtime.dll poc/round31-tests/Runtime/ordered-fixtures.json local-private/round31/store-receipt.json
python poc/round31-tests/Runtime/verify.py local-private/round31/store-receipt.json
python tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

The local package cache is an offline execution dependency, not curated evidence. Private logs are named `build-final.txt`, `store-native.txt`, `oracle-final.json`, `scope-final.json`, `workspace-validation.txt` and `focused.diff` under the ignored Round31 evidence directory. No original execution, live contact, download, commit, staging or push occurred.

Scope: four assigned source/scenario/contract/checkpoint additions plus five isolated Runtime runner files; no existing baseline file changes. Final scope audit confirms exactly nine allowed additions, all 2754 baseline files byte-identical, preserved HEAD/index and ignored evidence/build outputs. Workspace validation exits zero with zero broken links, zero links into private evidence and zero provisional duplicates. `git diff --check` exits zero; the focused diff and status were inspected. Lead-owned global status/ledger reconciliation remains outside this worker's allowlist.

## Exact next probe

Define the eventual serialized scene host's recipient validation and journal acknowledgment policy for historical batches, including destroyed/recreated recipients. Then integrate this isolated store only against that explicit ownership contract. Do not treat drain or acknowledgment as callback dispatch.
