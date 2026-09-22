# Full effective CLEF version, leaf, and direct-resource inventory

Date: 2026-09-22  
Target: configured `C:\Stardust-DEV\Stardust.exe` overlay

## Coverage result

The configured overlay has **1,146 effective `.cef` paths**: 1,145 bounded `CLEF` files and one genuine zero-byte mask. Every live file uses a version accepted by the original executable's CLEF dispatcher; no unrecognized outer form, version, or malformed first-level leaf boundary was found.

| Nested version | Live files |
|---|---:|
| `0001` | 650 |
| `0002` | 39 |
| `0003` | 456 |

Leaf occurrence counts across the 1,145 live files are `PSND` 999, `CPAP` 815, `CAMS` 279, `CLGT` 189, and `FFBK` 75. These match the five explicit leaf branches in Stardust.exe at `0x00698560`, `0x00698B70`, and `0x00699100`. The string inside each `FFBK` leaf is a `forcefeedback/*.ffe` resource path, so the original effect grammar includes a real force-feedback asset class; the 32-bit reader also consumes two four-byte fields whose runtime meaning remains untraced.

The recognized direct reference set is **1,889 references**: 999 `.snd`, 815 `.prt`, 75 `.ffe`. In the configured overlay, **1,877 resolve to live paths and 12 are absent**; none is a mask. All 75 `.ffe` references resolve to live paths. The 12 absent references are nine `.snd` and three `.prt` links (full source/path pairs are printed by the scanner and can be regenerated). A follow-up read-only check found none of those 12 names in any of the 58 installed TRE archives or at the corresponding installation-relative loose-file paths. This is direct dependency closure only. Particle descendants, shader/texture chains, alternative working-directory mounts, and runtime handling of null sound handles are not proven by this count.

## Replacement consequence

An original-asset-compatible replacement cannot model CEF as a particle file alone. It needs the versioned CLEF grammar, sound and particle child references, light and camera records, and the force-feedback reference class—even if an initial proof of concept deliberately defers presentation. The one genuine masked CEF should preserve configured overlay semantics; the 430 kind-0/zero-csize files are live, not masks.

`tools/python/cef_full_overlay_inventory.py` writes the private row table `local-private/generated-evidence/cef_full_overlay_2026-09-22.csv`; `tools/python/cef_zero_csize_corpus.py` remains the focused reproduction for the 430 corrected records. Version/tag counts are **confirmed by executable dispatch plus original asset bytes**. Direct reference availability is **confirmed for configured TREs, all 58 installed TRE archives, and installation-relative loose paths**, not for every possible working-directory mount or runtime fallback. Consumer semantics for `CAMS`, `CLGT`, and `FFBK` remain **partly unknown**.
