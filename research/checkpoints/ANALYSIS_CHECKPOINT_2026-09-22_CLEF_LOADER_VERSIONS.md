# Stardust.exe CLEF 0001–0003 loader and short-effect corpus

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`; decompiled output is reconstructed pseudocode, not source

## Entry and dispatch

The original executable contains a contiguous client-effect IFF reader:

| Address | Stable provisional name | Observed role |
|---|---|---|
| `0x006984E0` | `client_effect_read_CLEF_dispatch` | Opens `CLEF`, dispatches nested `0001`, `0002`, or `0003`; other versions enter diagnostic path. |
| `0x00698560` | `client_effect_read_CLEF_v0001` | Repeatedly reads supported leaves into the corresponding template vectors. |
| `0x00698B70` | `client_effect_read_CLEF_v0002` | Same leaf family, revised `CPAP` payload. |
| `0x00699100` | `client_effect_read_CLEF_v0003` | Same leaf family, further expanded `CPAP` payload. |

The leaf dispatch compares explicit tags `CPAP`, `PSND`, `CAMS`, `CLGT`, and `FFBK`; an unrecognized leaf enters the IFF diagnostic/error path. Each recognized leaf is opened, read, closed, then appended to one of the template's vectors. The `CPAP` resource path is passed through `0x00B260F0` (previously established appearance-template fetch-or-default wrapper) before insertion. `PSND` reads one string and prepares a sound resource handle. `CAMS` reads four 32-bit scalars into a 16-byte entry. `CLGT` reads three single-byte fields plus five 32-bit scalars into a 24-byte entry. `FFBK` reads one string and two 32-bit scalars into a 24-byte entry. The exact user-facing meaning of every scalar, and the expansion of the `FFBK` acronym, remain unconfirmed without consumer tracing.

`CPAP` is version-sensitive:

| CLEF version | Observed `CPAP` payload reader |
|---|---|
| `0001` | appearance path string + one 32-bit scalar |
| `0002` | v0001 fields + one byte interpreted as a boolean |
| `0003` | v0002 fields + four further 32-bit scalars |

The loader reads the `CPAP` result into 0x28-byte vector entries. Those are internal 32-bit process structures, not wire-format specifications. A new 64-bit client should parse fields into explicit typed values rather than port this layout.

## Asset corroboration

The 430 newly recovered short CEF assets are all valid `CLEF`: 319 v0001, 29 v0002, and 82 v0003. Their bounded leaves are `PSND` (344 total occurrences), `CPAP` (111), `CAMS` (30), `FFBK` (two), and no `CLGT` in this short subset. This is a corpus subset, not an assertion that `CLGT` is unused across all 1,146 effective CEFs. The two aftershock assets with no sound/particle path each contain a 16-byte `CAMS` leaf, exactly matching the reader. The 457 recognized child paths and three unresolved references are listed in `ANALYSIS_CHECKPOINT_2026-09-22_ZERO_CSIZE_CLIENT_EFFECTS.md`.

The data verifies the reader's version and leaf support but does not prove the runtime consumer of camera/light/feedback entries or establish complete descendant particle/shader/texture closure. A later full-overlay pass identified `FFBK` strings as `forcefeedback/*.ffe` paths, making the force-feedback resource class concrete while leaving its two scalar meanings and runtime policy unknown. In particular `CAMS` can now be identified as a four-scalar camera-effect record by its loader destination and corpus use, but its exact field order and transform/impulse semantics are still **unknown**.

## Evidence and confidence

- Read-only Ghidra decompilations of `0x006984E0`, `0x00698560`, `0x00698B70`, and `0x00699100`, plus original executable tag immediates at `0x006984F5..0x00699658`. Private pseudocode is retained under `local-private/decompiler/cef_tag_decomp_2026-09-22/`.
- `tools/python/cef_zero_csize_corpus.py` and private `cef_zero_csize_corpus_2026-09-22.csv` supply asset-byte corroboration; the subsequent full-overlay results are in `ANALYSIS_CHECKPOINT_2026-09-22_FULL_CLEF_OVERLAY.md`.
- Dispatch, accepted versions, leaf read order, per-version `CPAP` extension, and vector destinations: **Confirmed by code**.
- User-facing effect semantics beyond resource type and field count: **Unknown or provisional**.

The stable-name registry now has **2,208 unique addresses, zero duplicate addresses**. Packet mapping is unchanged at 345 documented / zero reachable matrix gaps; rounded planning estimates remain 96% implementation-grade and 90% comprehensive.
