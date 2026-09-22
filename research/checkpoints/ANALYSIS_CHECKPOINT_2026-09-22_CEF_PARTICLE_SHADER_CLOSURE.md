# CEF particle, attachment, shader, and texture dependency closure

Date: 2026-09-22. Target: configured original-asset overlay at `C:\Stardust-DEV`, for original `Stardust.exe` SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Bounded question and reproducibility

For every live `CLEF` effect, are its direct `CPAP` particle files and their `PATT` descendants available, and are the reached `PTEX` shader and shader `NAME` resources available? Stop at shader `NAME` resources; do not claim full interpretation of effect PASS or every runtime optional texture slot.

Inputs: the 1,145 live CEF rows from corrected configured-overlay inventory; corrected particle corpus, attachment and texture inventories (rerun on 2026-09-22 and byte-identical to their earlier generated CSVs); configured `swgemu_live.cfg` priority order across 48 existing configured TREs. Scripts: `tools/python/cef_particle_dependency_closure.py` and `tools/python/cef_shader_name_closure.py`. Their detailed JSON, including proprietary asset path lists, stays private under `local-private/`.

| Boundary | Result | Confidence |
|---|---:|---|
| Direct CEF `CPAP` references | 815 occurrences; 464 distinct `.prt` names | Confirmed by corpus |
| Direct absent `.prt` children | 3 occurrences: two absent names plus one authored path with a trailing space | Confirmed in configured overlay; see CPAP correction below |
| Distinct live `.prt` nodes reached through recursive `PATT` | 576 | Confirmed by literal-path corpus graph |
| Reached `PATT` edges | 253: 224 `.prt`, 25 `.swh`, 3 `.apt`, 1 `.msh`; all target names live | Confirmed for immediate target existence |
| Reached particle attachment cycles | 0 | Confirmed in this graph |
| Reached `PTEX` shader slots | 1,891 occurrences, including 87 empty shader names | Confirmed; empty name is not a missing file |
| Distinct nonempty `.sht` shader names | 278; all live and all parsed as bounded IFF | Confirmed |
| Shader `NAME` resources | 278 `.dds` occurrences (270 distinct) and 270 `.eft` occurrences (11 distinct) | Confirmed by IFF `NAME` leaves |
| Missing shader `NAME` resources | 7 occurrences, all the same `.eft`: `effect/e_particle_alpha_emis_full_bloom_no_fog.eft`; no missing `.dds` | Confirmed; also absent from all 58 installed TREs and corresponding loose path |

The three absent direct particle references pass through `0x00B260F0` at CLEF load. That appearance-template wrapper is already proven to substitute `appearance/defaultappearance.apt` for an unavailable child, so the CEF's particle slot has an explicit fallback. This does not restore the intended effect visually. The seven missing EFT edges enter the previously established shader-effect cache fallback to `effect/defaulteffect.eft`; that is a different fallback at a deeper resource layer. Both should be modeled explicitly in a replacement client. The 87 empty `PTEX` shader slots are authored empty strings, not missing files. Follow-up runtime analysis shows that all 87 are enabled and select `shader/3d_vertexcolorac.sht` through the primitive's fallback shader accessor; see `ANALYSIS_CHECKPOINT_2026-09-22_PTEX_EMPTY_SHADER_FALLBACK.md`.

The graph confirms a substantial original-asset dependency: CEF presentation is not just a flat `.cef` parser. It traverses PEFT emitters, recursively attached particles/other appearances, shader templates, named textures, and effect programs. The counts are an installed-corpus dependency closure, **not** proof that each branch executes in normal gameplay or that every shader PASS/texture slot is semantically implemented. Shader `NAME` scanning classifies path-suffixed names; non-path dynamic tags and effect PASS internals are outside this bounded closure.

## Correction to prior direct-reference inventory

Initial re-run reported **1,889 direct references, 1,877 live and 12 absent** (ten `.snd`, two `.prt`). A subsequent literal-path audit found that its regex silently dropped an authored trailing space in one `.prt` name. The corrected split is **1,876 live / 13 absent**, comprising ten `.snd` and three `.prt` references. The third `.prt` is present only *without* that trailing space, and TreeFile does not trim it. Re-running the closure without whitespace trimming removes one reached particle and one shader-slot occurrence; all other table results remain unchanged. See `ANALYSIS_CHECKPOINT_2026-09-22_CLEF_CPAP_RUNTIME_AND_PATH_CORRECTION.md`. Packet map and planning percentages remain unchanged.
