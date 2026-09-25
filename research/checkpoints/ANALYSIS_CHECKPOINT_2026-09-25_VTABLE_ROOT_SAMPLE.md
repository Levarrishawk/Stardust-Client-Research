# R05: known vtable-export candidate roots, not a reachable-function denominator

**Superseded as a class-attribution/counting method** by [the byte-verified RTTI-table audit](ANALYSIS_CHECKPOINT_2026-09-25_TEN_VTABLE_AUDIT_BLOCKS.md). The 68 large-function candidates below came from exact TSV/Ghidra address joins, but two are not byte-verified executable slots, and several source files cross into adjacent classes. The corrected recovered-table sample contains 69 unique large candidates, including targets beyond the TSV slices. Retain this checkpoint as a record of the preliminary method, not its final tally.

Date: 2026-09-25. Target remains the SHA-256-verified original `C:\Stardust-DEV\Stardust.exe`. No execution, server interaction, build, or engine decision. This block asks whether the existing virtual-dispatch evidence can give a defensible *subsystem-stratified sample* of the 45,036 uncurated generic functions over 64 bytes. It does **not** claim to enumerate all vtables or establish runtime reachability.

Reproduce with `tools/python/audit_vtable_function_roots.py local-private/generated-evidence/stardust_function_inventory.tsv data/curated/provisional_names.csv local-private/generated-evidence` using a Python 3 interpreter. The script reads 24 existing `*vtable*.tsv` exports. Only exact matches between an export's second-column hexadecimal address and a Ghidra function entry are counted. This avoids promoting nearby addresses or data pointers to functions, but does not prove that a matched row is a real virtual slot. The TSV files have heterogeneous layouts: some describe RTTI rather than slots, some include addresses from another module, and repeated slots appear across exports.

| Sample category | Exact unique entries |
|---|---:|
| All second-column Ghidra-entry candidates | 190 |
| Already in curated semantic registry | 27 |
| Uncurated generic `FUN_`, >64 bytes | **68** |
| Other non-unwind entries | 95 |

There were 1,837 raw export rows, 260 exact-match rows, and 1,577 rows without an exact original-exe Ghidra-entry match. The 190 unique candidates include 48 appearing in multiple export files. These figures are inventory joins, **not 190 proven live callbacks**. In particular, the large UI export has 1,104 rows but only two exact original-exe entry matches; it cannot be treated as evidence that UI has only two virtual methods. Camera and thread files yield zero exact matches because their current exports are RTTI descriptors rather than complete slot enumerations. These distinctions show why a universal “vtable root” inference from filename alone would be false.

The sample nevertheless identifies bounded, reviewable leads in original-exe code. The `client_procedural_terrain_vtable.tsv` export yields 22 distinct uncurated generic >64-byte exact entries; `flora_static_vtable.tsv` yields 16; `world_singleton_vtable.tsv` yields 10; `sound2d_vtable.tsv` yields 8; `client_chunk_vtable.tsv` and `client_global_water_manager_vtable.tsv` yield six each. Counts overlap across files and are not additive. Their file labels are candidate subsystem associations, **not confirmed method roles**. Verify table address, object construction and virtual call before assigning a semantic function name.

## Effect on the denominator

The previous 45,036 generic >64-byte stratum remains a search space, not a reachable application-function denominator. This pass adds a reproducible 68-entry candidate virtual-dispatch review queue but removes **zero** entries from that search space and closes **zero** function semantics. It also demonstrates that the existing vtable exports are incomplete and structurally mixed; expanding them requires PE/data-section-aware table recovery and code xrefs, not another filename count. Raw `E8` and aligned-pointer reference candidates remain heuristics. A decoded call graph, module-origin signatures, verified callback/vtable roots, and a missing-entry audit are still needed before a numerical function-completion score.

Outcome: **R05 narrowed; function denominator and whole-client percentage remain uncalibrated.** Confidence: confirmed for exact joins and counts; tentative for subsystem labels; unknown for runtime reachability and method semantics.
