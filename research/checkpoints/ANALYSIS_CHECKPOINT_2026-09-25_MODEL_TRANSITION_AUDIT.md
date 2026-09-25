# Model-transition audit and research course correction

Date: 2026-09-25. This review covers the GPT-6 Sol work added after the prior 2026-09-24 analysis checkpoint. It preserves useful evidence, corrects the accounting method, and restores the project’s primary objective: an implementation-grade behavioral, protocol, lifecycle, and original-asset model of the original `Stardust.exe`. It does not run the executable or begin replacement-client construction.

## What remains useful

- The arithmetic warning is valid: the dated 2026-09-21 weighted table in `COMPREHENSIVE_ANALYSIS_LEDGER.md` evaluates to 55.59% from its printed rows, not the adjacent 82%. That table is stale and cannot independently prove the later 90% estimate.
- Separating replacement-relevant behavioral coverage from raw function-address coverage is correct. The 2,528 curated addresses and 93,287 non-`Unwind@` Ghidra entries measure different things.
- The function stratification correctly shows that the analyzer inventory contains a large uncurated population and cannot be equated with understood application behavior.
- Byte-checking the historical vtable exports exposed real defects: file slices cross class boundaries, many rows are not verified table slots for this PE, and RTTI-validated tables contain executable targets omitted by the Ghidra export. Those results remain useful for targeted virtual-dispatch work.

## What went off course

1. **The denominator became the objective.** Work shifted from observable client contracts to normalizing analyzer entries. That is supporting work, but it does not directly specify login, replication, assets, rendering, animation, UI, or lifecycle behavior.
2. **One audit was reported as ten analysis blocks.** The “ten vtable blocks” are ten findings from one common script and evidence set. They must not be treated as ten independent behavioral advances.
3. **The planning percentage was conflated with function coverage.** The old 90% figure was a heuristic estimate of replacement-relevant research, never a percentage of disassembled functions. Its reproducibility weakness is real, but raw-function incompleteness does not mathematically reduce it to 55.59% or zero.
4. **Discovery proxies were promoted too far.** Raw `E8` matches, aligned pointer matches, filenames containing `vtable`, and exact-name Markdown mentions are candidate generators only. The UI mention audit, for example, reports `SwgCuiCharacterSheet` as unmentioned even though `ANALYSIS_CHECKPOINT_2026-09-22_CHARACTER_SHEET_RUNTIME.md` is a dedicated end-to-end checkpoint.
5. **Priority drifted away from material unknowns.** Earlier checkpoints already name the remaining behavioral boundaries: CKAT completion/event behavior, reachable forward asset variants, UI workflow breadth, light upload semantics, APT fallback reachability, and protocol failure/timing edges.

## Restored accounting model

| Measure | Current status | Meaning |
|---|---|---|
| Replacement-relevant comprehensive research | Last historical estimate: **90%**, pending final evidence-backed rescore | Architecture and observable behavioral coverage; not function percentage |
| First-build/engine-decision readiness | Last historical estimate: **96%**, pending final gate audit | Whether the evidence constrains architecture and initial milestones |
| Function/address accounting | **Uncalibrated** | Reachable application routines with stable roles; not every compiler/library fragment |

No percentage changes merely because a function, RTTI name, asset, packet, or block was counted. A substantive block must close or materially narrow an observable contract. Address-level work earns behavioral credit only when it identifies a reachable class/callback path or resolves behavior needed by such a contract.

## Research order restored

1. Finish named Class-C behavioral unknowns where static evidence can still decide them: animation completion/loop/replacement paths, reachable original-asset variants, and final renderer consumers.
2. Alternate those with genuinely untraced, high-impact UI/gameplay scenarios selected by constructor, activation, event, and server evidence—not prose mention counts.
3. Continue server producer → wire → parser → handler → state correlation for concrete mismatches or failure paths, not blanket packet recounting.
4. Use corrected vtable/function tooling only inside those targeted blocks. Defer exhaustive whole-PE normalization until it serves a named behavior or the final audit.
5. After the material-question queue, rebuild the weighted table from current evidence and publish behavioral and function-accounting results separately.

## Immediate next block

Resume **A02 / W08** at the unresolved end-frame animation-message boundary. Prior work proves the ordinary physical-track query is half-open and occurs before replacement, while 1,517 installed events sit exactly at clip end. The next evidence is the loop/reset/replacement path and any alternate dispatch that could deliver those events. If static analysis cannot decide them, stop at a bounded runtime-fixture specification rather than repeating corpus counts.

Outcome: **course corrected**. The GPT-6 work is retained as one useful methodology audit; it is not discarded, but it no longer controls the primary queue or block count.
