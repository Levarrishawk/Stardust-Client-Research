# CLEF template fetch, cache, and missing-path boundary

Date: 2026-09-22  
Target: original `C:\Stardust-DEV\Stardust.exe`; Ghidra output is reconstructed pseudocode

## Confirmed path

`0x005F40D0` (`client_effect_template_fetch_cached`, provisional) receives a CRC-string resource path. It first checks the template-list state, looks up an existing entry in its ordered map, and returns that template on a cache hit. On a miss, it obtains the path string, tests its existence through the previously mapped `tree_file_exists_normalized` at `0x00A93030`, and **logs a warning and returns null** when the top-level `.cef` path is unavailable. If present, it opens the IFF, allocates the template, calls the `CLEF/0001..0003` constructor/dispatch at `0x00698020 → 0x006984E0`, inserts the resulting object in the cache, closes the IFF, and increments the template reference at `0x00699860` before returning. `0x00699860` is a four-byte refcount increment at object offset `+0x04`.

This distinguishes two different missing-resource boundaries:

```text
requested top-level .cef absent
  → ClientEffectTemplateList fetch warns and returns null

top-level .cef present but CPAP child appearance path absent
  → CLEF reader calls 0x00B260F0 fetch-or-default appearance wrapper
```

The first boundary is fail/skip for the effect request; the second can substitute `appearance/defaultappearance.apt` for a missing particle/appearance child. It would be incorrect to apply the appearance fallback to the missing top-level effect itself. For three currently observed missing child references in the short CEF corpus, this block establishes only the *CPAP* fallback path; it does not establish the PSND sound-template fallback for the two missing `.snd` children.

## Ownership and limits

The template constructor at `0x00698020` allocates five vectors at offsets `+0x0C` through `+0x1C` for `CPAP`, `PSND`, `CLGT`, `CAMS`, and `FFBK` data, then invokes the CLEF reader. The loader's vector appends and the template refcount are confirmed. This block did not close all downstream consumer methods for the `CAMS` and `FFBK` vectors; their detailed effect semantics remain **unknown**. The next targeted executable block should trace reads of template `+0x18` and `+0x1C` through effect instantiation and per-frame dispatch, not infer field meaning from names.

Evidence: private read-only decompilations of `0x005F40D0`, `0x00698020`, `0x006984E0`, `0x00699860`, plus constructor/loader xrefs. Six total new stable names in this CLEF sequence leave the registry at **2,210 unique addresses / zero duplicates**. No packet mapping or rounded planning-estimate change.

Confidence: cache/open/existence/refcount path **confirmed by code**; top-level missing-path warning/null return **confirmed**; nested CPAP default fetch **confirmed**; PSND missing-template and CAMS/FFBK runtime consequences **unknown**.
