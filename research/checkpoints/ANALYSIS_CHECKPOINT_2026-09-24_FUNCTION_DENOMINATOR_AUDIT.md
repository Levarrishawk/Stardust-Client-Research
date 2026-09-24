# Function-denominator audit: planning progress is not function coverage

Date: 2026-09-24. Scope W01/A10, methodological correction. Inputs are the read-only Ghidra export for the original `C:\Stardust-DEV\Stardust.exe` and `data/curated/provisional_names.csv`. Reproduce with `tools/python/audit_function_inventory.py local-private/generated-evidence/stardust_function_inventory.tsv data/curated/provisional_names.csv`. The TSV is an analyzer inventory, not a canonical list of source-language functions.

| Measure | Current count | Interpretation |
|---|---:|---|
| Ghidra entries | 243,277 | Includes compiler-generated exception/unwind fragments |
| `Unwind@` entries | 149,990 | SEH/unwind fragments, not independent application behaviors |
| Non-`Unwind@` entries | 93,287 | Still includes thunks, wrappers, CRT/library routines, possible analyzer splits and unproven reachability |
| Non-`Unwind@` size >16 bytes | 72,447 | Crude triage, **not** a semantic-function denominator |
| Non-`Unwind@` size >64 bytes | 47,082 | Another crude triage, **not** a completeness denominator |
| Curated stable-name addresses | 2,481, all unique | Addresses for which a bounded provisional semantic identifier was recorded |
| Curated addresses present in Ghidra export | 2,382 | The other 99 include raw-entry recoveries or analyzer omissions; they are not automatically invalid |
| Curated addresses mapped to `Unwind@` | 0 | Registry is not inflated by that dominant compiler-artifact category |

Among the 99 curated addresses absent as Ghidra **entries**, 22 fall within some exported function interval (e.g. named inline dispatch branches), and 77 are outside exported intervals. The latter need individual raw-byte/entry evidence rather than automatic rejection or automatic promotion to functions.

The registry/export intersection is about 2.55% of the non-unwind Ghidra entries. **That is not an estimate of how much behavior is understood**, because many already modeled protocols, data formats and subsystem rules cover multiple unnamed helpers, while the denominator includes uninteresting or unreachable routines. Conversely, it plainly rules out interpreting the 90% whole-client planning estimate as 90% of individual functions reverse-engineered. The current 96%/90% numbers are subjective milestones toward replacement architecture/implementation-gate understanding; they are not exhaustive coverage measurements and cannot establish the user's stronger “every function” criterion.

Future progress reports should publish at least these two axes separately: (1) scoped implementation/engine-decision gate status and (2) mechanically countable address/function-identification status, with the latter's imperfect denominator explicit. A meaningful exhaustive audit would need reachable-callgraph classification, compiler/runtime/library exclusion, vtable/callback/data-driven reachability and a per-function confidence/effect ledger. Neither this count nor the 345-row server packet-source matrix substitutes for that work.

Confidence: **Confirmed** for analyzer/registry row counts and prefix categorization; **Strongly inferred** that most `Unwind@` rows are compiler exception fragments; **Unknown** exact number of behaviorally distinct, reachable original functions. No client functional closure, no percentage rescore.
