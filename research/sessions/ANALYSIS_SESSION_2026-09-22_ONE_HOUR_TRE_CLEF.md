# One-hour Stardust.exe analysis session: TRE correction and CLEF asset boundary

Correction recorded later on 2026-09-22: the 1,889 direct CLEF references split
as **1,876 live / 13 absent**, not 1,877/12. The earlier regex omitted an
authored trailing space from one `CPAP` path; TreeFile does not trim it. See
`../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CLEF_CPAP_RUNTIME_AND_PATH_CORRECTION.md`.

Date: 2026-09-22  
Session window: 20:04:59–21:04:59 UTC (16:04:59–17:04:59 EDT)  
Scope: read-only executable/asset analysis, corrected research tools and documentation; no replacement-client implementation

## Outcome

Eleven bounded blocks were completed. The main correction is to TRE kind-0 payload handling: `usize`, not `csize`, is the stored byte length when `kind=0`. Several older research scripts silently misclassified live files with `csize=0,usize>0`. After correction, the configured overlay and Stardust.exe code support a much clearer account of cockpit frames, client data, client effects, terrain ramps, and combat markers. Nothing in this session changes the server, original executable, or original assets.

## Blocks completed

1. **Targeted TRE-reader correction.** Fixed five queued extractors. Corrected cockpit profile results (36 valid profiles; 11 empty frame names, 25 live named APTs), confirmed six zero-entry STF files are valid, and reran sound/forward-shader reference scans unchanged. Earlier claim of 18 missing/masked cockpit frames was withdrawn.
2. **Cockpit frame-chain closure.** Traced all 25 named frames through APT targets: three direct MSH and 22 DTLA LOD; all 22 LOD children are live. Runtime display and deeper shader/texture closure remain unproven.
3. **Global legacy TRE-tool re-audit.** Fixed 15 additional old entry points. Across 203,617 effective paths, 1,576 live kind-0 records have zero compressed-size fields. Corrected CDF inventory: 6,288 paths, 6,284 valid CLDF/0000, four true masks; 84 previously omitted valid CDFs. Specialized CNCF, light, barrel, and contrail counts were unchanged after rerun.
4. **Recovered short CEF assets.** Classified 430 previously hidden live CLEF files and first-level references. Later expanded the reference extractor to include force-feedback `.ffe`; final short-file count is 457 recognized child references, 454 live and three absent from configured TREs.
5. **Recovered CDF dependencies.** The 84 added CDFs introduce no new first-level tags: 75 ASND, five CEFT, three HOBJ, one childless. Of 83 recognized direct references, 80 are live and three ASND sounds absent from configured TREs/loose paths.
6. **Terrain TGA ramps.** All 53 corrected `.tga` records are valid one-row uncompressed true-color terrain ramps with TGA 2.0 footers; 24- or 32-bit samples.
7. **CLEF 0001–0003 executable loader.** Decompiled dispatch and three version readers. Confirmed CPAP, PSND, CAMS, CLGT, FFBK leaves and version-dependent CPAP extensions.
8. **CLEF template cache/fetch.** Decompiled top-level CEF lookup: cache hit reuse; absent top-level CEF warns/returns null. Nested CPAP uses the distinct default-appearance fetch wrapper.
9. **PSND missing-sound boundary.** Sound-template lookup uses allow-missing IFF open and returns null for absent child sound; CLEF reader still appends the entry. Downstream instance handling of null remains unknown.
10. **MKAT marker appearance.** All 15 recovered markers parse as MKAT/0000/INFO and point to live shaders. Decompiled factory and INFO reader; first-level field order is now constrained.
11. **Full CEF overlay breadth check.** All 1,145 live effective CEFs are supported CLEF versions (650 v0001, 39 v0002, 456 v0003); one true mask. Across them, 1,889 recognized direct references: 999 SND, 815 PRT, 75 FFE. A later literal-path correction gives **1,876 live / 13 absent** in the configured overlay; the thirteenth path has an authored trailing space. No malformed first-level form or unsupported version was found.

## Cross-references and corrections

- [TRE targeted size audit](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_TRE_SIZE_TOOL_AUDIT.md) and [global legacy re-audit](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_TRE_LEGACY_TOOL_REAUDIT.md).
- [Cockpit-frame chain](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CPIT_FRAME_CHAIN.md); the prior 18-frame masking interpretation is withdrawn.
- [Recovered CDF dependencies](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_RECOVERED_CDF_DEPENDENCIES.md) and corrected [CDF core](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_CLIENT_DATA_FILE_CORE.md).
- [Full CLEF overlay](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_FULL_CLEF_OVERLAY.md), [CLEF loader](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CLEF_LOADER_VERSIONS.md), [fetch/cache](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CLEF_TEMPLATE_FETCH.md), and [PSND missing sound](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CLEF_PSND_MISSING_SOUND.md).
- [Terrain ramps](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_TERRAIN_TGA_RAMPS.md) and [MKAT markers](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_MARKER_APPEARANCE_CORPUS.md).

The direct shader-version payload-row CSV was unchanged by the size correction despite 20 additional live OPST shaders; its live-winner count changes from 23,286 to 23,306. The already-correct palette scanner was rerun against the same overlay: 264 effective palettes, zero parse failures, and a byte-identical CSV to its prior inventory, including the four affected kind-0 records. The three affected WAV assets also have valid RIFF/WAVE signatures and declared lengths. The earlier 6,204-file CDF denominator and CEFT/ASND totals were corrected in the core checkpoint. No server packet rows were changed.

## Confidence, unknowns, and next queue

The counts, bounded IFF/TGA structures, configured resource-path availability, and executable loader/cache branches are **confirmed mechanically or by code**. The decompiler output is reconstructed pseudocode, not source. Original-asset compatibility remains mandatory, but path presence alone does not prove successful runtime instantiation or correct rendering.

Highest-value remaining targeted work:

1. Trace CLEF `CAMS` (`template +0x18`), `CLGT` (`+0x14`), and `FFBK` (`+0x1C`) consumer methods to establish scalar semantics and lifecycle.
2. Trace effect-instance behavior when PSND caches a null sound handle; check whether absent direct CEF dependencies cause silent skip, warning, or another fallback. This has since been addressed for PSND and CPAP in the evening CLEF checkpoints.
3. Continue descendant closure from CPAP `.prt` through particle shaders/textures and from cockpit MSH through materials/texture resources, then verify with runtime behavior where permitted.
4. Audit the 84 corrected CDFs and 53 ramps against any previously generated aggregate tables that may still use stale denominators; the authoritative corrected CSVs are in ignored `local-private/generated-evidence/`.

## Research state and handoff

Stable provisional-name registry: **2,214 unique addresses, zero duplicates** (ten new names this session). Server packet matrix: **345 documented, zero reachable gaps**; no new packet handler mapping was claimed. Planning estimates remain **96% implementation-grade** and **90% expanded comprehensive**; these are heuristic replacement-readiness estimates, not a percentage of all executable functions reverse-engineered. No prototype host or DX12/Vulkan build was attempted. No Git stage, commit, or push was performed; user retains commit control.

Reproduction scripts live under `tools/python/`; read-only Ghidra pseudocode, CSV evidence, and original asset bytes remain in ignored `local-private/` (not for Git). The original executable, assets, and server source were not modified.
