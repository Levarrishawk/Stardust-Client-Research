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

The recognized direct reference set is **1,889 references**: 999 `.snd`, 815 `.prt`, 75 `.ffe`. A literal-path correction finds **1,876 live paths and 13 absent** in the configured overlay; none is a mask. All 75 `.ffe` references resolve to live paths. The absent set is **ten `.snd` and three `.prt`** links. Two `.prt` names are genuinely absent; the third is an authored path with a trailing ASCII space, whereas the corresponding archive name has none. The executable's TreeFile normalizer does not trim whitespace, so this is an actual lookup miss rather than a live reference. The initial regex scanner silently dropped that space; see `ANALYSIS_CHECKPOINT_2026-09-22_CLEF_CPAP_RUNTIME_AND_PATH_CORRECTION.md`. Alternative loose overrides remain outside this configured-overlay claim.

## Replacement consequence

An original-asset-compatible replacement cannot model CEF as a particle file alone. It needs the versioned CLEF grammar, sound and particle child references, light and camera records, and the force-feedback reference class—even if an initial proof of concept deliberately defers presentation. The one genuine masked CEF should preserve configured overlay semantics; the 430 kind-0/zero-csize files are live, not masks.

`tools/python/cef_full_overlay_inventory.py` writes the private row table `local-private/generated-evidence/cef_full_overlay_2026-09-22.csv`; `tools/python/cef_zero_csize_corpus.py` remains the focused reproduction for the 430 corrected records. Version/tag counts are **confirmed by executable dispatch plus original asset bytes**. Direct reference availability is **confirmed for configured TREs, all 58 installed TRE archives, and installation-relative loose paths**, not for every possible working-directory mount. Runtime consumers are now partially closed by `ANALYSIS_CHECKPOINT_2026-09-22_CLEF_RUNTIME_CONSUMERS.md` and `ANALYSIS_CHECKPOINT_2026-09-22_CLEF_LIGHT_RUNTIME.md`; exact CLGT scalar-trio semantics remain open.
