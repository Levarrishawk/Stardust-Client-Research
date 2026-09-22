# Recovered short client-effect assets and dependency boundary

Date: 2026-09-22  
Target: configured `C:\Stardust-DEV\Stardust.exe` asset overlay

## Result

The corrected TRE size rule exposes **430 live `.cef` records** that older compressed-size-based extraction could hide. They are part of 1,146 effective CEF paths. All 430 are structurally bounded `FORM CLEF` IFFs: 319 `0001`, 29 `0002`, and 82 `0003`. The first-level leaf combinations are:

| Leaf tags | Files |
|---|---:|
| `PSND` | 298 |
| `CPAP` | 81 |
| `CPAP;PSND` | 20 |
| `PSND;CAMS` | 17 |
| `CPAP;PSND;CAMS` | 7 |
| `CPAP;CAMS` | 3 |
| `CAMS` | 2 |
| `PSND;FFBK` | 1 |
| `PSND;CAMS;FFBK` | 1 |

The 430 records hold 457 recognizable asset references: 344 sound-template `.snd`, 111 particle-appearance `.prt`, and two force-feedback `.ffe`. **454 resolve to live effective overlay paths.** Three do not resolve either in configured TREs or as loose files at the matching installation-relative paths:

- `clienteffect/e3_explode_lair_med.cef` → `appearance/pt_md_explosion.prt`
- `clienteffect/e3_womprat_emote_vocalize.cef` → `sound/cr_womprat_emote_vocalize.snd`
- `clienteffect/e3_bantha_attack_light.cef` → `sound/cr_bantha_attack_light.snd`

The two CEFs without recognized resource paths, `space_command/aftershock_small.cef` and `aftershock_medium.cef`, are **not** empty assets. Both contain a `CAMS` chunk with 16 bytes of numeric camera-effect data. The short-file group therefore includes sound-only, particle-only, mixed, and camera-effect behaviors. Treating these records as masks would erase actual client presentation.

## Executable-side linkage and limits

Existing `ClientEffectManager` analysis (`ANALYSIS_CHECKPOINT_2026-09-21_CLIENT_EFFECT_MANAGER.md`) confirms `.cef` template creation from incoming effect messages, local events, and object/scene paths; particle/light instances are tracked and retired per frame. This block analyzes asset bytes and references, **not** the leaf-tag loader or exact `CAMS`/`FFBK` field meanings. The provisional leaf labels should not be expanded beyond observed resource type and payload shape until decompiler validation. In particular, `CAMS` is strongly suggestive of camera shake but the four numeric field semantics are not established here.

`tools/python/cef_zero_csize_corpus.py` reproduces the bounded outer/version/leaf inventory and reference check. Private row evidence is `local-private/generated-evidence/cef_zero_csize_corpus_2026-09-22.csv`. The reference extractor recognizes installed `appearance/`, `sound/`, `clienteffect/`, and `forcefeedback/` paths (including backslash separators); it is not a complete parser of every potential CEF field. The three unresolved references are concrete asset-closure risks, not proof that the runtime fails to play those effects.

## Confidence

- Effective counts, IFF structure, leaf-tag combinations, recognized references and configured path availability: **Confirmed mechanically**.
- Runtime effect-manager link: **Confirmed by earlier executable analysis**.
- Exact semantics of `CAMS` and `FFBK`, fallback for three absent child paths, and complete nested particle/shader/texture dependency closure: **Unknown**.
