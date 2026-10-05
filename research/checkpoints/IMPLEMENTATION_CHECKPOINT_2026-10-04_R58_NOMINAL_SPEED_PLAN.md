# R58: atomic supplied nominal speed plan — 2026-10-04

Question: can immutable supplied steps compose the accepted target and transition graphs, preserve actual receipts and raw current stores, and reject an entire bounded plan without changing a prior result?

Stopping condition: standalone implementation, independent multi-step arithmetic agreement, ownership/failure/cap checks, clean baseline scope review and a reproducible handoff. This checkpoint adds no executable address claim and performs no original execution, live contact, capture or mutation of external systems.

## Input identity and tools

Frozen round-58 manifest: SHA-256 `1b7f1accb970175d436e43d0a2fbc381eaf8c460505692741a8f3eeddf94445c`; archive: `8651f06b9398f910ac8ea2bd1fc246bb448602278c69c5a58bdbb99c7612e2d5`. The overlay contains 3,832 public files. All eight reviewed prior round-57 own-source hashes were crosschecked against the worker bytes and lead integration record before the once-only frozen overlay. Main was read-only; worker HEAD and index were preserved.

Inputs are the accepted [target contract](../../docs/CREATURE_NOMINAL_TARGET_SPEED_CONTRACT.md), [transition contract](../../docs/CREATURE_NOMINAL_SPEED_TRANSITION_CONTRACT.md), their C# implementations and independent Python oracles. No new VA analysis relies on an unchecked executable. Cached .NET 8 and bundled Python run offline, with clear NuGet sources, a private APPDATA and cached packages. Generated synthetic fixture data and receipts stay ignored and are not publication material.

## Findings and confidence

Observation: `CreatureNominalSpeedPlan.Build` primitive-preflights all supplied values and conservative caps, copies and revalidates the source, then invokes actual frozen target and transition producers. Each receipt owns its value source and retains both immutable producer objects. The next step uses the actual transition `StoredBits`. Interpretation: this composes the recovered bounded supplied operation graphs without approximating their retained or F32-store boundaries. Confidence: Confirmed by source inspection and independent fixture agreement. Compatibility consequence: engine adapters can consume one accepted immutable plan and preserve its evidence trail.

Observation: 343 synthetic cases matched the independent Fraction/isqrt models: 322 positive plans, 2,312 positive steps, 20 input rejections and one cancellation. Count, logical copy and conservative work each have exact and one-below fixtures. Nine late failures include nonfinite primitive inputs, invalid factor/rate profiles, ordered missing rate and late arithmetic overflow. Native checks preserve the prior result, copy source values, refuse collection mutation and compare actual receipts; 100 repetitions and 64 independent parallel calls pass. Interpretation: deterministic acceptance/rejection and atomic publication hold for this bounded corpus. Confidence: Confirmed for executed scenarios; timed mid-arithmetic cancellation remains inspection-only.

Atomicity means publication of a complete result, not atomic reading of concurrently modified caller memory. Reservation equality detects some concurrent changes but cannot detect all same-budget mutations. Concurrent source mutation is unsupported. Logical copy/work accounting is explicit modern policy, not CLR allocation or original instruction counting.

## Reproduction

1. Restore `poc/round58-tests/Runtime/Runtime.csproj` using its clear-source `NuGet.Config`, private APPDATA and cached packages with `NuGetAudit=false` and `RestoreLockedMode=true`.
2. Build with `--no-restore`, `UseSharedCompilation=false` and `UseAppHost=false`.
3. Run `verify_plan.py generate --out FRESH_PRIVATE_CASES`.
4. Run the built `Runtime.dll FRESH_PRIVATE_CASES FRESH_PRIVATE_NATIVE`.
5. Run `verify_plan.py verify --cases FRESH_PRIVATE_CASES --native FRESH_PRIVATE_NATIVE --receipt FRESH_PRIVATE_RECEIPT`.
6. Run the organized workspace validator, the default smoke build/run, and baseline-relative source/hash/diff checks.

The [durable contract](../../docs/CREATURE_NOMINAL_SPEED_PLAN_CONTRACT.md) is canonical for the API and accounting rules. The standalone runner includes its scenario source without modifying the shared smoke entrypoint or project. Round scope permits only these four requested artifacts and four new runner files; frozen producers, shared host and canonical status/ledgers are untouched for lead consolidation.

## Remaining uncertainty and next probe

Physical getters, supplied factor provenance, SI units, original arithmetic controls, receiver lifetime, vector/root-motion effects and full locomotion remain Unknown. Supplied nominal planner agreement does not close these questions or increase executable function coverage. The next action is lead review of the frozen-58-relative source hashes, followed by an independently authorized host adapter consuming this complete plan and checking receipt/current-chain continuity. Any physical parity claim requires its separate evidence closure.
