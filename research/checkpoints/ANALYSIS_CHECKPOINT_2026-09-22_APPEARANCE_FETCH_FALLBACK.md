# Appearance fetch: missing path, redirect target, and loose-file boundary — 2026-09-22

## Scope and evidence

Static analysis uses `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`), the existing read-only Ghidra project, 32-bit instruction disassembly, and the installed `swgemu.cfg`/`swgemu_live.cfg` asset overlay. Reconstructed C is pseudocode, not original source. The address list and private decompilation outputs are retained in ignored `local-private/decompiler/appearance_fetch_missing/`. This checkpoint corrects the narrower DTLA failure speculation in the preceding checkpoint; no client execution or server packet interception was used.

## Confirmed call paths

```text
DTLA child path [0x00799810]
  -> fetch-or-default [0x00B260F0]
       -> VFS exists [0x00A93030 -> 0x00A92FD0 -> SearchTree exists 0x00A996A0]
       -> if null/empty/not found: default path [0x00B262D0]
       -> strict appearance fetch [0x00B263C0]
       -> retain template [0x00B26130]
  -> create runtime child [0x00799880]

strict fetch of live .apt [0x00B263C0]
  -> parse APT/0000/NAME and reject .apt-to-.apt
  -> recurse directly into strict fetch for NAME target
  -> IFF/TreeFile open [0x00A94210 -> 0x00A931E0]
  -> if ordinary open fails: fatal diagnostic "Could not open appearance file %s"
```

The executable disassembly of `0x00B260F0` confirms the order: check input pointer and first byte, call `0x00A93030`, select `0x00B262D0` on false, call `0x00B263C0`, then call `0x00B26130` on its result. `0x00B262D0` returns `appearance/defaultappearance.apt`; the effective APT inventory shows it redirects to a present `appearance/defaultappearance.msh`. The mesh has independently been classified as `MESH/0003` with valid SPS geometry in the asset research.

`0x00A93030` normalizes the logical path then asks `0x00A92FD0` to traverse search nodes in priority order. SearchTree's existence virtual (`0x00A996A0` from vtable `0x015EA454` +8) uses `0x00A99550`: a matching record with zero uncompressed-size field reports absence and sets the shadow/stop flag. Therefore a zero-payload APT **name** is treated as missing by the fetch-or-default wrapper, while the three live APTs with unavailable *targets* are not converted to default at the APT-name check.

## Distinct failure/substitution cases

| Requested path state | `fetch-or-default` behavior | Strict fetch consequence | Confidence |
|---|---|---|---|
| Direct DTLA `.msh` child path absent from mounted VFS | Selects `appearance/defaultappearance.apt`; then obtains that live template | Original child geometry is not loaded; final visual/availability policy not dynamically tested | Confirmed path-level substitution; runtime presentation unknown |
| Direct requested APT name is zero-payload/masked | Existence test fails and selects default APT | No attempt to parse the masked APT through this wrapper | Confirmed for this wrapper |
| Live APT with absent `.lod` target (`speeder_usv5_s02`) | Original APT passes existence check | Recursive strict target fetch reaches ordinary open failure/fatal diagnostic if no loose override | Strongly inferred from code and installed path state |
| Live APT with masked `.ssa` target (`wp_rifle_dlt20a`) | Original APT passes existence check | Recursive strict target fetch reaches ordinary open failure/fatal diagnostic if no loose override | Strongly inferred |
| Live APT with absent `.msh` target (`eft_cloud_cumulus_01`) | Original APT passes existence check | Same ordinary failure branch, but an `AsynchronousLoader`-state MESH factory path may construct an initially unloaded template before the ordinary open | Conditional/unknown eventual load |

The direct-child substitution is not the neighboring-detail fallback: it happens when the child template is fetched, before `0x00799880` invokes the template's runtime-appearance constructor. Selector `0x00787710` reaches `0x00787650` through `0x00787700`, then queries the created child's virtual availability method. In 60 DTLA parents every authored child path is absent from the archive overlay; for those parents, the default-template substitution is more relevant than searching adjacent authored levels. Do not infer that the child was displayed in a particular frame or that a missing target is harmless.

The conditional `.msh` path is narrower than a general fallback: `0x00A96430` tests the active `AsynchronousLoader` state and nesting depth, and `0x00B263C0` recognizes `.msh`/`.MSH` before looking up registered MESH tag `0x4D455348`. It calls the MESH factory `0x007962D0` with the filename and a zero IFF pointer. Constructor `0x00796340` only invokes its MESH loader when that pointer is nonzero. Thus this branch can create an initially unloaded template; whether asynchronous completion later finds the absent target, marks it unavailable, or fails is not established by this trace.

## Loose-file search and installed configuration

`TreeFile::open` at `0x00A931E0` visits nodes in descending priority. Installer `0x00A92800` assigns automatic `SearchAbsolute` priority one greater than the current highest TREE priority; with `swgemu_live.cfg`'s maximum 55, this is 56. `SearchAbsolute` existence/open virtuals at `0x00A99190`/`0x00A991F0` reach `GetFileAttributesA`/`CreateFileA` through `0x00A9B650`/`0x00A9B6D0`. It can therefore satisfy a logical relative path if a corresponding loose file is accessible in the process working directory, and has higher priority than configured TREs. None of the installed CFGs specifies `searchPath` or `searchTOC`, and no file with any of the three broken-target basenames was found under `C:\Stardust-DEV`. A different working directory or user-created loose override is not ruled out. `SearchCache` is also mounted; its contents and invalidation are outside this block.

## Replacement-client implication and remaining work

Specify two APIs, not one blanket fallback: `fetch_or_default(path)` for call sites proven to use the wrapper, and `fetch_strict(path)` for recursive APT targets and other strict callers. Preserve VFS priority and masked-path behavior. Surface broken redirect targets as structured load errors in the new client rather than silently substituting an invented mesh; compatibility tests can decide whether to emulate the original fatal diagnostic. Next investigate asynchronous completion/error behavior for a missing `.msh`, the exact process working-directory policy, and whether selected DTLA default children report available to the distance selector. No network mapping or renderer decision changed here.

New stable names: `0x00B260F0`, `0x00B262D0`, `0x00A93030`, `0x00A92FD0`, `0x00A94210`, and `0x00A996A0`. Registry total: 2,204 unique addresses / zero duplicates. Planning estimates remain 96% implementation-grade and 90% comprehensive; packet matrix remains 345/0.

## Corrective raw-MSH audit in this session

While validating the SearchTree size test, a separate historical claim was rechecked: the 171 raw `.msh` anomalies had previously all been described as tombstones because their compressed-size fields were zero. Corrected kind-0 payload sizing shows **106 live assets** in the unmounted SKU archives (88/11/7 in `data_sku1_00/06/07.tre`). Every one parses as `MESH/0005 / SPS/0001 / VTXA/0003`. The other **65** (`mtg_patch_004_appearance_04.tre`) have `usize=0` and are actual masks. Corrected `tools/mesh_schema_inventory.py` reports 29,131 / 11,814 / six live tuple counts plus 65 masks across all 41,016 raw MSH records, with zero extraction errors; corrected `tools/mesh_anomaly_audit.py` reports 65 tombstones and no other anomalies. The 106 valid SKU meshes are installed but those SKU TREs are not among the 48 mounted by `swgemu_live.cfg`; this does not alter the current effective-overlay DTLA missing-child count. The historical topology/SIDX code findings remain intact; only the raw asset classification is corrected.

## Shader inventory spillover from the same size rule

The raw SHT/EFT inventory tools had the same compressed-size-only test. Corrected `tools/shader_corpus_inventory.py` reports 37,842 live SHT, 87 true SHT masks and 467 live EFT among installed TRE copies, with zero parse errors. SSHT outer versions are 35,293 v0000 and 2,395 v0001; PASS/0009 is 1,703. Corrected `tools/python/shader_overlay_inventory.py` reports 23,393 configured `.sht/.eft` winners: 23,306 live and 87 masks (20 more live SHT winners than before), zero parse errors, and the same 27 visible IMPL/0009–PASS/0010 outliers. Six effective `.stf` records also have `kind=0`, `csize=0`, `usize>0`; STF-specific inventory scripts remain on the follow-up audit list, not silently declared closed.

Tooling audit queue for the next block: `tools/tre_probe.py`, `tools/python/cpit_corpus_inventory.py`, `tools/python/iff_shader_structure.py`, `tools/python/scan_effective_asset_references.py`, and `tools/python/sound_overlay_inventory.py` still contain compressed-size-based payload slicing or empty-record checks. Determine each script's actual corpus exposure before changing published counts; do not infer that a code pattern necessarily altered its results. The six effective STF records are a confirmed exposure candidate.
