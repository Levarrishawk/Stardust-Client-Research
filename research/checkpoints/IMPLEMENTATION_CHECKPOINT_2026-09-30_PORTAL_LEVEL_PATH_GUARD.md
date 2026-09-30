# 2026-09-30 — Complete source/target level-path guard

## Bounded question

Can the level-only portal adapter accept a rise-and-fall floor path whose initial/final heights match? Stop after source and target counterexamples are rejected before adoption, flat transfer and original actor regressions pass, and affected documentation is reconciled.

Observation: terminal-only comparisons accept synthetic ridges with equal-height endpoints. Interpretation: the bounded level-only collision path must inspect intermediate floor planes. Confidence: Confirmed native counterexamples. This is a replacement defect, not recovered original solver behavior. Executable identity remains the rechecked SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; no new VA labels.

## Implementation boundary

[PortalFloorTransferQuery](../../poc/Stardust.Core/Assets/PortalFloorTransferQuery.cs) now offers `Trace` alongside unchanged `Walk`, returning read-only source and optional target floor traces. The same pairing, permission callback, edge bridge, landing and crossing budgets are used; target traces start at the actual validated bridge point. At most one transfer and two floor traces are supported.

[PortalBarrierFloorWalker](../../poc/Stardust.Core/Assets/PortalBarrierFloorWalker.cs) checks every source/target segment endpoint in world height, using validated yaw-owner translation. Any variation above the existing 1e-4 level tolerance returns UnsupportedNonlevelBarrierSweep at the validated source, zero progress and no adopted transfer. This enforces the declared level-only scope; it does not enable slope collision.

## Validation and resume

[Native tests](../../poc/Stardust.Core.SmokeTests/PortalLevelPathScenarios.cs) prove source and target ridges with equal-height endpoints, trace/result equality, source hold and flat-target transfer. Full core smoke passes. Independent cantina 948 ticks / 44 transfers / 34 blocked and Mustafar 948 / 44 / 28 pass over two poses. Godot build and bounded headless Mustafar controls pass. Installed candidate screening is rerun to reconcile the existing denominator.

Installed screening repeats 782 candidates / 647 independently paired / 533 actors / 31,980 ticks: 131 exact round trips, 289 other Accepted and 113 UnsupportedNonlevelBarrierSweep results. Counts remain unchanged. Final workspace validation: 800 Markdown files, zero broken/private links, 2,599 names with no duplicates and 345 packet rows. Diff check passes; index remains empty and generated evidence is ignored.

Next: integrate precise source/target box sweeps and validated contact landing with portal transfer, beginning with an original sloped doorway. Preserve explicit unsupported status until this gate passes. Research scores 89% / 94%, names 2,599 and packet rows 345 remain unchanged. No original execution, server contact, staging or publication.
