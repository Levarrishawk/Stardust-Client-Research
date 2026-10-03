# Round 28 — SWTS clock origin and frame ordering

Date: 2026-10-03. Question: what supplies the float reaching `0077E2C0`, in which unit, and what can static evidence establish about first update/draw order and x87 precision? Stop condition: close the ordinary GroundScene clock chain or identify its precise missing caller. The ordinary chain is closed; first visibility and alternate owners remain open.

## Input and method

Executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` was verified before interpreting VAs. The Round 28 canonical snapshot contained 2,666 files; snapshot manifest SHA-256 `c6945d7365f1c6d2b3585aebeec57d10a29fb9c0fcbd7b0e586a0d58125ab654`, archive SHA-256 `e0eb172e1b2429de49c55857bc14baa0cbe75f5aa825bfcba88762b8483ca011`. Worker HEAD remained `65de8c88141071744dc75594be126b3bab3696a5`.

Reused PE/Capstone static tools, existing frame/x87 checkpoints, curated names, and private decompiler leads; instruction decoding supplied the new proof. No original client execution, server contact, packet capture, asset mutation, or engine implementation. Bulk instruction and caller output remained ignored private evidence. Durable conclusions and exact VAs are in the [clock contract](../../docs/SWITCH_TEXTURE_CLOCK_CONTRACT.md).

## Findings, evidence, and confidence

| Observation | Interpretation/confidence | Compatibility consequence |
| --- | --- | --- |
| `00AA4BBE` calls QPF; `00AA4DEA` calls QPC; `00AA4E24` divides counter difference by cached frequency. Export `?frameTime@Clock@@SAMXZ` resolves to `00AA4EC0`. | Confirmed ordinary published time is seconds, subject to cap/retain branches. | Do not invent millisecond units for DTST intervals. |
| `00423979` calls IoWin update; type-4 event stores delta at `+10`; GroundScene reads it at `0051AF80` and calls `005616D0` at `0051B25D`. Both scheduler mode branches reach `00B410D0`. | Confirmed synchronous reachable GroundScene route. | Event propagation and scheduler gates matter. |
| `00B3FE60` multiplies by 1000; `0131E27C` truncates integer conversion and restores CW; unchanged tick forces +1 at `00B3FED4`. `00B40F2D` scales per-object tick difference by float32 inverse 1000. | Confirmed quantized, accumulated scheduler seconds. | Callback delta can span frames; zero frame input is not necessarily zero object delta. |
| Object `00B23144` → Mesh `007933D5` → set `0077E805` → primitive `0077E2CC` → SWTS `00785C10` → DTST `00B5AD20`; RTTI slots corroborate each virtual edge. | Confirmed examined object/mesh path preserves the float unit. | Engine-independent time boundary belongs above selector arithmetic. |
| DetailAppearance child loop `00787CD3–00787CFE` forwards input to every non-null loaded child, before later LOD work. | Confirmed local loop; whole-scene culling behavior Unknown. | Do not update only the selected loaded LOD based on this path. |
| IoWin update at `00423979` precedes render `00423AA6`; selector constructor initializes zero. | Confirmed ordinary phase order and initial query; first visible variant Unknown. | Keep initial query and first draw claims separate. |
| `00AA7B1D AND F0FF` clears PC/RC; frame pump calls restore at `00AA0729`. | Confirmed configured PC=00/RC=00; actual SWTS callback word unobserved. | Replace unconditional extended-intermediate wording with conditional precision policy. |

The contract cites Microsoft and Intel only for API units and architectural bit meanings. Those sources do not prove client behavior. The prior [frame schedule](ANALYSIS_CHECKPOINT_2026-09-12_FRAME_SCHEDULE.md) and [x87 mask investigation](ANALYSIS_CHECKPOINT_2026-09-24_CKAT_X87_EXCEPTION_MASK_POLICY.md) supply corroborating context.

## Verification and handoff

Run the [verifier](../../poc/round28-tests/Rendering/Verify-SwitchClock.py) with explicit `--exe`, cached `--packages`, and a new ignored `--receipt` path. Its positive audit covers 26 ranges, seven RTTI slots, export/import identity, scale constants, and ordered frame call sites. Wrong-identity and truncated synthetic executable inputs must fail before PE/VA analysis. These are static checks, not execution-based animation tests.

Executed results: positive audit passed (26 instruction ranges, seven RTTI slots); both synthetic negatives rejected input before VA use. Python syntax and scoped EOF/trailing-whitespace checks passed. The workspace validator reported zero broken links, zero links into private storage, and zero duplicate provisional addresses. `git diff --check` passed. All 2,666 supplied baseline hashes remained unchanged, exactly three scoped public files were added, HEAD was preserved, and the index stayed empty. The full dirty status was inspected against the snapshot rather than treating snapshot-overlay differences as this worker's changes.

This worker adds only the two allowed research documents and its scoped verifier. No registry, ledger, status, UNKNOWN_FUNCTIONS, or Round 27 file is changed. Lead reconciliation: add this finding to current status/ledger and replace Round 27's extended-intermediate sentence with the bounded correction quoted in the durable contract; preserve historical reports. No new canonical aliases are required to use this result.

Remaining uncertainty: actual first visible index; due/registration timing for a newly loaded SWTS object; complete pause/culling semantics; alternate appearance routes; intervening CW overrides and non-frame-thread owners; malformed/extreme time inputs. Next action is the constructor-to-first-enqueue probe specified in the contract. No combined coverage metric or replacement readiness percentage follows from this milestone.
