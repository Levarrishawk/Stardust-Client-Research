# Portal floor transfer milestone — 2026-09-30

## Question and stopping condition

Can one original paired interior floor portal transfer owner and continue remaining motion, with explicit current permission and deterministic failure? Stop after one verified transfer; defer full actor collision. Completed.

## Identity, evidence and findings

Input executable, configuration and original scene SHA pins, exact VAs, confidence and compatibility consequences are consolidated in the [durable contract](../../docs/PORTAL_FLOOR_TRANSFER_CONTRACT.md). Executable identity was rechecked before address analysis. Static tools: `tools/python/disasm_range.py`, `find_call_xrefs.py`, `find_immediate_xrefs.py`, pefile/capstone and the existing function inventory. Original executable execution was not used.

Principal inspected ranges: `012792B0–01279567`, `01275FE0–012761E2`, `01279B90–01279E41`, `01266960–01267380`, `012670E0–01267380`, `00B2B240–00B2B520`, `00B53160–00B53272`, `01266410–01266842`. Registration and callback reads at `00423533`, `00422CB0`, `00B2A8A0`, `00B2BDB0` establish entry-byte use. Caller evidence separates same-floor contact recursion from actual cell transfer.

Corrections: `0126AA80` retains the same wrapper; transfer ownership belongs to `012670E0` / `00B2B3F0`. A trial raw SolidByte barrier gate rejected all 14 original interior links and was removed. Raw template bytes and current barrier state are distinct. Exact original actor collision ordering, actor state bit 27 semantics and door lifecycle remain Unknown.

Three canonical names were added for `00422CB0`, `00B2B3F0`, `00B2BDB0`: **2,593 distinct addresses**. Behavioral/readiness scores remain **89% / 94%**; packet matrix remains **345**. No cross-denominator completion claim is made.

## Implementation and reproduction

Core: `poc/Stardust.Core/Assets/PortalFloorTransferQuery.cs`; synthetic tests: `poc/Stardust.Core.SmokeTests/PortalTransferScenarios.cs`; independent verifier: `tools/python/verify_portal_transfers.py`. Godot adapter and controls live in `FloorMeshPreview.cs` and `Main.cs`. The query owns snapshots and supports one portal with finite rigid owner frames, exact local-ID mapping, bounded pairing/crossings, explicit permission failures and bounded landing correction.

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
dotnet build --no-restore poc/Stardust.Godot
python tools/python/verify_portal_transfers.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg appearance/yt1300.pob 8B4B59335CF34BA91DAA5CCB092AF60C9A996849DB29E77EB154FD4DF202DF4A local-private/generated-evidence/portal-transfer-milestone poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

Run `tools/powershell/Invoke-StardustAssetPreview.ps1` with the pinned scene, `-Interior -InteriorFloors -InteriorPaths -FloorOnly -Cell 1`, private Godot path and `-Headless` for the bounded host gate; omit headless and supply a fresh private capture path for GPU verification. The configured runtime Python may be needed. APPDATA can point to a task-specific temporary directory.

Results: full smoke tests pass; build has zero warnings/errors; **14 links / 56 independent probes** agree. Godot **28 adapter probes**, current-policy two-pick controls, existing surface/route selectors and coordinate fixtures pass headless and GPU. The startup certificate-store diagnostic is nonfatal. Private logs, source manifests and captures remain ignored. The native verifier and headless gate were rerun after final API refinement to pass the complete link to the permission provider.

## Precise resume point

Recover portal barrier creation and enable/disable ownership at `012906F0`, predicate use at `01290CFA`, and actor-contact ordering around `012670E0` / `01266410`. Then define the smallest fixture-backed body/door contact slice. Do not infer current barrier state from SolidByte or expand into exterior terrain continuation without evidence.

Final workspace validation: 768 Markdown files, zero broken links, zero links into private evidence, 2,593 canonical address rows with zero duplicates, and 345 packet rows. `git diff --check` passes. Final GPU gate also passed after the permission API refinement; its capture was visually inspected. Git status was reviewed and the staging area remains empty.
