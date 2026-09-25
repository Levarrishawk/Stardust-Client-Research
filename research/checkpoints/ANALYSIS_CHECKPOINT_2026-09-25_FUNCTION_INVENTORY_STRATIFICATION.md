# Stardust.exe function-inventory stratification — denominator work, not coverage

Date: 2026-09-25. Question R05 / W01. Target: verified original `C:\Stardust-DEV\Stardust.exe`; inputs are its existing read-only Ghidra TSV export and the 2,528-address curated-name CSV. Reproduce with:

```text
python tools/python/stratify_function_inventory.py local-private/generated-evidence/stardust_function_inventory.tsv data/curated/provisional_names.csv
```

This is a **denominator audit** and does not reverse-engineer an additional client behavior.

| Inventory stratum | Ghidra entries | Curated exact entries | Not curated |
|---|---:|---:|---:|
| `Unwind@` prefixed | 149,990 | 0 | 149,990 |
| Non-unwind, Ghidra thunk flag | 370 | 0 | 370 |
| Non-unwind, generic `FUN_`, 1–4 bytes | 877 | 24 | 853 |
| Non-unwind, generic `FUN_`, 5–16 bytes | 19,542 | 55 | 19,487 |
| Non-unwind, generic `FUN_`, 17–64 bytes | 25,239 | 295 | 24,944 |
| Non-unwind, generic `FUN_`, 65+ bytes | 46,690 | 1,654 | **45,036** |
| Non-unwind, non-generic name, not thunk | 569 | 401 | 168 |
| **Total** | **243,277** | **2,429** | **240,848** |

The non-unwind subtotal is **93,287**. Of 2,528 curated addresses, 2,429 coincide with exact Ghidra entries and 99 do not; the previous audit classifies those 99 into entries inside exported intervals and raw-entry/analyzer-omission candidates. The 370 Ghidra-flagged thunks are only one explicit wrapper category. The 45,036 generic, >64-byte, uncurated entries show why an “every function” promise cannot be translated into a short number of future blocks. But this stratum is **not** 45,036 proven reachable gameplay functions: it includes unknown CRT/library code, analyzer splits, inaccessible paths and code whose behavior is captured at a higher-level contract.

A second reproducible *reference-candidate* pass uses `tools/python/audit_function_reference_candidates.py` on the original PE. It scans raw `E8 rel32` byte patterns in executable sections and aligned dwords in non-executable sections; these are **heuristic possible** direct calls/pointers, not decoded callgraph edges. Of the 45,036 generic, >64-byte, uncurated entries, **38,273** have at least one raw `E8` candidate, **2,834** have an aligned non-code pointer candidate, and **40,872** have one or both. This makes an automated origin/reachability classifier worthwhile but does **not** certify 40,872 reachable client functions. Raw-byte false matches, indirect calls and callback registration all prevent that inference. Reproduce with:

```text
python tools/python/audit_function_reference_candidates.py C:\Stardust-DEV\Stardust.exe local-private/generated-evidence/stardust_function_inventory.tsv data/curated/provisional_names.csv
```

## Exit criterion, not yet met

The next denominator step is a reproducible origin/reachability classification for *all* 93,287 non-unwind entries: identify library/CRT signatures, imports/exports, tiny wrappers, callback/vtable roots and direct-call graph; classify dynamic calls and table-driven dispatch separately; sample false positives and missing raw entries. Define an in-scope reachable application-function set **before** publishing a function-completion percentage. A semantic name alone is not a full handler analysis, and an unnamed routine can belong to a behaviorally mapped chain.

**Outcome: narrowed measurement problem, no behavioral closure and no whole-client percentage.** Confidence is confirmed for TSV/CSV counts and script categories, unknown for actual application-function count and reachability. No code execution of Stardust.exe occurred.
