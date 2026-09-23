# Analysis checkpoint — XFMS authoring reachability

This is a bounded effective-overlay reference scan for the nine active XFMS files, not a proof of dynamic runtime frequency. Target remains the verified `Stardust.exe`; no server, executable, or asset was modified.

Only two effective `appearance/lat/*.lat` records contain full literal `appearance/mask/` paths: `all_f.lat` and `all_m.lat`. They contain 1,014 total literal occurrences across four mask paths:

| XFMS path | `all_f.lat` occurrences | `all_m.lat` occurrences | Total |
|---|---:|---:|---:|
| `mask_rider.iff` | 96 | 96 | 192 |
| `mask_rider_no_combat.iff` | 251 | 443 | 694 |
| `mask_spine3_arms.iff` | 10 | 10 | 20 |
| `mask_upper_body.iff` | 54 | 54 | 108 |

All four paths resolve to effective live XFMS/0000 assets. Five other installed XFMS files (`mask_head`, `mask_arm_right`, `mask_arm_left`, `spine`, `bantha_upper_spine`) have no full-path literal in the scanned effective `.lat`, `.ans`, `.ash`, `.skt`, `.lmg`, `.sat`, `.mgn` and `.iff` records under the 1 MiB scan cap. This is evidence of no direct reference in that bounded scan, not proof of dead resources: alternate path construction, other formats, and dynamically authored content were not ruled out.

The `all_f`/`all_m` repetitions are consistent with masks being referenced from many animation graph nodes, but the exact count of distinct logical states was not decoded here. The prior runtime trace establishes that a loaded XFMS maps each skeleton transform to in/out groups. These two facts make XFMS mandatory for original-asset consumption in the common human animation chains; the five without direct references can be lower-priority compatibility fixtures until a broader reachability trace finds callers.

The XFMS-backed runtime animation instance delegates most general playback methods to an underlying animation object (vtable at `0x015E62EC`, short disassembly `0x0083D4B0` onward). Its special method `0x0083D480` reads the per-transform group-reference array populated by `0x0083CA00`; `0x0083D4A0` returns the retained locomotion group. Thus the mask is used to answer per-transform priority/group queries while playback mechanics are delegated. The exact higher-level arbitration formula remains a separate animation-controller concern.

Evidence: ignored scratch `scan_xfms_references.py`, `xfms_runtime_disasm.tsv`, and previous XFMS loader/runtime checkpoints. The source scan respects configured TRE overlay priority and tombstones.
