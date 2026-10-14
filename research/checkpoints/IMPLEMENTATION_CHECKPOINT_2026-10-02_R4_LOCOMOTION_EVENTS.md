# R4-RUNTIME-01: accepted-distance event traversal — 2026-10-02

Question: can an engine-neutral event wrapper preserve every successful existing locomotion-clock state while emitting bounded ordered messages from accepted movement, including switches, blocked movement, and atomic failures? Stopping condition: native synthetic and pinned original-data probes pass, existing native suite passes, and the baseline-relative handoff contains only the nine authorized paths. Complete Round 4 only.

## Inputs and method

Workspace: C:/Users/ancie/.codex/worktrees/9175/Stardust-Client. HEAD: 65de8c88141071744dc75594be126b3bab3696a5. The accepted Round 4 snapshot contains 1792 files. ZIP SHA-256: d7364d604436d086b9103c2a76489ce51bb27b1bbe464b07fb918f4a586a5b38. Manifest SHA-256: e53b9decda458cb7ea9f335ea4eda9422ea915415f514316d7e07df2507609d7. Snapshot members and resulting overlay were hash-verified; unlisted/private material was preserved.

Orientation covered START_HERE, its session resume record, the accepted Round 3 session, status/ledger/unknown terminology, prior event cursor/playback/diagnostic and locomotion/bank contracts and checkpoints, existing scenarios, and the scope-review helper. No executable addresses are used; Stardust.exe was neither executed nor used as an evidence input.

Original assets were read statically through configured overlay and existing bundle readers at C:/Stardust-DEV. Configuration SHA-256: 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. Human object SHA-256: E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243; SAT: 951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C. Canonical idle logical name emt_standing_idle_ambient_1_ag resolves through appearance/lat/all_m.lat to appearance/animation/all_b_idl_ambient_01.ans, SHA-256 798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B, scale 1. Walk SHA-256: FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6. Run: 72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4. Additional selection uses poc/preview-profiles/human-locomotion.json.

Tools: installed .NET, bundled Python, existing asset readers, and parallel_round_workspace.py. Offline sources-cleared NuGet configuration and the worker's ignored package cache were used. Native synthetic fixtures are independently authored CKAT/MSGS/LOCT records. No original assets or extracted corpora are added to public files.

## Evidence and interpretation

Observation: the new wrapper forks LocomotionPreviewClock and AnimationEventCursor, seeds switched clips at previous normalized phase, computes unwrapped phase from the authoritative increment, and commits candidates only after bounded traversal succeeds. Existing public clock/cursor defaults remain unchanged; their two minimal internal extensions are fork/seed access and last-increment observation. Interpretation: this supplies modern accepted-distance scheduling without reimplementing gait classification. Confidence: Confirmed by source and exact state comparisons. Consequence: adapters must pass accepted displacement after physics and use matched clip descriptors/event sources.

Observation: synthetic tests exercise idle/walk/run switches, heading/Y behavior, blocked/inactive/zero ticks, duplicate and terminal/zero ordering, reset/proxy scale, foreign-owner and stale batches, invalid inputs, budget failures/retry, duration mismatch, and an extreme finite speed exceeding the phase ceiling. 512 independent-oracle and binary-exact partition cases pass. Interpretation: bounded traversal and rollback hold for these tested cases. Confidence: Confirmed within fixtures. Consequence: arbitrary floating partitions are not guaranteed bit-identical near boundaries; no rounding compensation is introduced.

Observation: the original resolved walk/run clips produce three wraps and six footsteps after 100/67 accepted-distance ticks, with exact existing-clock state equality each tick. Frames are walk 7/23 and run 6/17. Blocking movement selects idle and produces no movement footsteps. Interpretation: the wrapper traverses real decoded occurrences using modern distance progression. Confidence: Confirmed for pinned samples; original client scheduling remains Unknown. Consequence: preserve occurrence ordering and identity, while retaining the modern-policy label.

Initial probing guessed an idle filename and failed. The final probe uses the canonical object/SAT/LAT bank instead. A failed Windows apphost retained a worker executable; only that exact owned process was terminated. The console now disables apphost and catches failures for clean exits. No original executable or external system was involved.

## Validation and reproduction

Run from the workspace with APPDATA assigned to a worker-specific temporary directory. Restore uses poc/round4-tests/Runtime/NuGet.Config, local-private/round1/nuget-packages, and NuGetAudit=false. Build uses UseSharedCompilation=false.

1. dotnet restore poc/round4-tests/Runtime/Runtime.csproj --configfile poc/round4-tests/Runtime/NuGet.Config --packages local-private/round1/nuget-packages -p:NuGetAudit=false
2. dotnet build poc/round4-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false
3. dotnet poc/round4-tests/Runtime/bin/Debug/net8.0/Runtime.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
4. Restore and build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj with the same offline configuration/cache and build flag; execute its net8.0 DLL.
5. python tools/python/validate_organized_workspace.py .
6. git diff --check, focused new-file checks, and parallel_round_workspace.py review against the accepted manifest and exact nine-path allowlist.

Native console and existing shared suite passed; builds reported zero warnings/errors. The shared suite includes existing locomotion and R1–R3 event scenarios. Round 4 scenarios compile there but execute through the owned console; the shared runner remains lead-owned. Scope and documentation validation results accompany the final handoff.

## Handoff and next probe

Canonical conclusion: [locomotion event preview contract](../../docs/LOCOMOTION_EVENT_PREVIEW_CONTRACT.md). Seven new files plus two minimal existing-file edits comprise the assignment. Baseline-relative before/after SHA-256 records are generated by the existing scope-review tool in the worker's ignored handoff manifest; accepted dirty baseline changes are not worker changes. Lead-owned status, ledger, shared runner, and prototype host files are untouched by this assignment.

Generation exhaustion and long-duration phase precision have guard logic but are not exhaustively executed. No prototype host/GPU proof, root-motion application, audio/effects dispatch, original-client scheduler recovery, or external contact is claimed. Next exact action for the lead: review the nine-file handoff and register LocomotionEventPreviewScenarios.Run() in the shared runner; optionally run its Original(root, configuration) probe where pinned inputs exist. Subsequent renderer work requires its own round scope. Runtime stops after this handoff.
