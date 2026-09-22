# Newly recovered CDFs: resource dependency check

Date: 2026-09-22  
Target: corrected configured overlay for `C:\Stardust-DEV\Stardust.exe`

## Result

The 84 effective live CDF paths omitted by the old TRE compressed-size reader introduce **no new first-level CLDF record tag**. All parse as `CLDF/0000`; 75 contain a single `ASND`, five a single `CEFT`, three a single `HOBJ`, and one (`clientdata/soundobject/client_shared_soundobject_cloning_facility.cdf`) is a valid childless CDF. This confirms the executable-side CDF grammar already documented remains applicable, while the corpus counts and resource coverage have increased.

The 83 records contain 75 `.snd`, four `.cef`, and four `.prt` recognizable resource references. Eighty references resolve to live effective overlay paths. Three ASND sound paths are absent from both the configured TRE overlay and their matching loose-file locations:

- `clientdata/client_shared_instrument_organ_figrin_dan.cdf` → `sound/mus_figrin_dan_song_1.snd`
- `clientdata/soundobject/client_shared_soundobject_seashore_outdoors.cdf` → `sound/amb_seashore_outdoors_lp.snd`
- `clientdata/soundobject/client_shared_soundobject_snow_outside.cdf` → `sound/amb_snow_outside_lp.snd`

The five recovered `CEFT` records include four disease/poison grenade client effects and one snowball weapon reference to `appearance/pt_snow_throw.prt`. The latter independently reinforces the earlier executable/corpus finding that CEFT's tag name does **not** guarantee a `.cef` target: resource dispatch must inspect the actual resolved path/type. The three `HOBJ` geyser CDFs all point to the live `appearance/pt_geyser_01_l0.prt`.

## Evidence and limits

`tools/python/cdf_recovered_dependencies.py` differences historical `effective_cdf_inventory.csv` against corrected `cdf_tags_corrected_2026-09-22.csv`, extracts those 84 winning payloads, checks bounded CLDF IFF leaves, and resolves recognizable resources by configured overlay priority. Private row evidence is `local-private/generated-evidence/cdf_recovered_dependencies_2026-09-22.csv`.

The three absent sounds are concrete original-asset closure risks, not proof of crashes or an assertion that every authored sound-object is reachable in gameplay. This block did not run Stardust.exe or analyze the detailed runtime fallback for missing ASND templates. All recovered leaf tags already have executable-side loader coverage in prior CDF checkpoints; no new provisional function name is assigned.

Confidence: file/tag counts and path availability **confirmed mechanically**; implications for audio playback **inferred only at resource boundary**; exact missing-template runtime outcome **unknown**.
