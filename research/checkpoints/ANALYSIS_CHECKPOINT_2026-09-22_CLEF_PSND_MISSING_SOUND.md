# CLEF PSND missing-sound template path

Date: 2026-09-22  
Target: original `C:\Stardust-DEV\Stardust.exe`; pseudocode is reconstructed, not original source

## Result

`CLEF` versions 0001–0003 process a `PSND` leaf by reading one sound path string and calling `0x00417320` (`sound_template_fetch_cached`, stable provisional name). That routine checks the sound system, queries the sound-template cache, then on a cache miss opens the path through `0x00A94210` with **allow-missing** enabled. If the file cannot be opened, it closes the IFF context and returns null. If opened, it selects the registered IFF sound-template factory, creates/caches the template, and returns a reference. The CLEF reader appends a 16-byte sound entry containing the path/string and resulting template handle; the vector-copy helper preserves the handle field. There is no replacement-sound path at this loader boundary.

For the two short CEFs with absent `.snd` children (`e3_womprat_emote_vocalize.cef` and `e3_bantha_attack_light.cef`), the observed behavior is therefore a **null sound-template handle inside an otherwise parsed CLEF template**, not failure of the top-level `.cef` file load. Whether the later effect-instance code skips that entry, warns, or fails is not yet traced and must not be invented. This differs from a missing top-level CEF (warning/null return from `0x005F40D0`) and a missing `CPAP` appearance child (default appearance fetch via `0x00B260F0`).

```text
PSND child path → 0x00417320 sound-template cache/open(allow-missing)
  ├─ found: parsed sound template handle → CLEF sound vector
  └─ absent: null handle → CLEF sound vector (downstream handling unknown)
```

Evidence: read-only decompilation of `0x00417320`, CLEF readers `0x00698560/0x00698B70/0x00699100`, and sound-vector copy `0x0069A7F0`. Private pseudocode is retained under `local-private/decompiler/cef_tag_decomp_2026-09-22/`. The corrected short-CEF corpus and absent paths are in `ANALYSIS_CHECKPOINT_2026-09-22_ZERO_CSIZE_CLIENT_EFFECTS.md`.

Confidence: cache/open/allow-missing/null-return and CLEF append **confirmed by code**; effect-instance behavior with null sound handle **unknown**. Registry: **2,211 unique addresses / zero duplicates**. No packet or rounded planning-estimate change.
