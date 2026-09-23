# ULAT tag lookup and runtime phases (overnight block 2)

Static analysis of original `Stardust.exe` plus effective `playback/use_lightning_appearance.pst`. This refines, but does not replace, the prior ULAT runtime checkpoint.

`ULAT/0002` parser `0x005F6BD0` reads two 16-bit actor selectors and twelve 32-bit tags. The script's ordered tags are `NTPA`, `TPHE`, `2PHE`, `LVEL`, `LTTA`, `CRFG`, `CRFD`, `BDNS`, `LDNS`, `EDNS`, `SUDR`, `ETAR`. The template accessors `0x005F8250..0x005F8300` confirm consecutive four-byte fields at `+8..+0x34`; `+0x38/+0x3C` remain to be matched to their consumers. Runtime `0x005F7500` retains the template through base constructor `0x0040AEF0` at runtime `+4`, so these accessors operate on the template, not the 0x23C-byte runtime object's similarly numbered offsets.

| Template field | Effective tag | Observed accessor/use |
|---|---|---|
| `+8`, `+0xC` | 16-bit selectors | `0x005F8250/60` select playback actors via `0x00407A20` |
| `+0x10` | `NTPA` | `0x005F8270` → string-variable lookup `0x00407D10` in `0x005F6F00` |
| `+0x14` | `TPHE` | `0x005F8280` → float lookup `0x00407FD0`, default 270, then multiply by global scalar in `0x005F6F50` |
| `+0x18` | `2PHE` | `0x005F8290` → float lookup, default 10, in `0x005F6F90` |
| `+0x1C` | `LVEL` | `0x005F82A0` → integer lookup, positive test, in `0x005F6FC0` |
| `+0x20` | `LTTA` | `0x005F82B0` → float lookup, default 2, in `0x005F7000` |
| `+0x24` | `CRFG` | `0x005F82C0` → float lookup, default 0.75, in `0x005F7030` |
| `+0x28` | `CRFD` | `0x005F82D0` → string lookup in `0x005F7060` |
| `+0x30` | `LDNS` | `0x005F82F0` → float lookup, default 1, in `0x005F7100` |
| `+0x34` | `EDNS` | `0x005F8300` → string lookup in `0x005F7130` |

**Important anomaly:** the observed lookup types at `TPHE`, `2PHE`, `CRFD`, and `LDNS` do not align with the effective script's apparent typed defaults (string entries for `TPHE`/`2PHE`/`LDNS`, float for `CRFD`). Exact x86 instructions for `0x005F6F50` confirm it loads the retained template, fetches `+0x14`, then calls the float-variable lookup; this is not merely a decompiler argument error. Possible version/content mismatch, optional fallback, or distinct meaning of tags must be tested before documenting final visual semantics. Do not silently coerce values across type maps in a replacement.

The update phase `0x005F7380` starts with `0x005F76B0`, later calls `0x005F7F70` for an animation-message-like event when a threshold is reached, and calls `0x005F7DD0` in another phase. `0x005F7DD0` may play a sound/effect on the retained object, append a `CAHD` helper action, and stop a retained audio/effect resource. The exact phase durations remain unresolved; predicates compare floats at runtime `+0x230/+0x234/+0x238` against an external scalar `0x00B4A720`. The deeper path `0x005F7180` uses two skeletal hardpoints and constructs a Swoosh-like appearance, strengthening the need for original animation/hardpoint and appearance assets.

Provisional names: `0x005F6F00` `ULAT_lookup_NTPA_string`; `0x005F6F50` `ULAT_lookup_TPHE_float_or_default`; `0x005F6F90` `ULAT_lookup_2PHE_float_or_default`; `0x005F7180` `ULAT_build_between_hardpoints_swoosh` (last is strongly inferred from calls; precise object class must be checked).
