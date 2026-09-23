# ULAT lightning runtime effect (block 2 of 3)

Static target: original `Stardust.exe`, correlated with effective `playback/use_lightning_appearance.pst`.

## Confirmed executable behavior

`0x005F7500` creates a runtime object and installs vtable `0x015DF140`, linking the registered `ULAT` template to runtime methods. Its per-frame method `0x005F7380` starts once through `0x005F76B0`; start failure terminates the action. It then manages several condition-gated phases: `0x005F7F30`/`0x005F7F70`, `0x005F7D90`/`0x005F7DD0`, and final `0x005F8050` cleanup/termination. Those predicates use runtime fields near `+0x230/+0x234/+0x238` and a resource pointer at `+0x22C`; exact time units are not established.

`0x005F76B0` requires a valid actor object and casts its appearance to `SkeletalAppearance2`. It resolves a string-like resource, requests/starts a sound handle through `0x007C99D0`/`0x007C9990`, constructs and retains additional effect objects, and computes a hardpoint-local vector through a skeletal-appearance virtual method followed by `Object::getTransform_o2w`. It then positions a created effect object, attaches/adds it through client-object/effect helpers, and initializes scalar phase thresholds. This is stronger than merely inferring a lightning effect from `NTPA=appearance/force_lightning.ltn`: the executable has an explicit **skeletal hardpoint → world transform → placed effect** path, plus audio start and later cleanup (`0x005F7410`).

The script's twelve tagged fields and original `.ltn` and sound path variables remain the necessary asset-facing inputs. This trace does not yet individually associate each of the twelve tags with each runtime lookup, nor establish all secondary effects, randomization, or exact synchronization with skeletal animation. The start path has many early returns, so a compatible replacement must treat absent appearance, hardpoint, sound, or effect resources as lifecycle cases rather than assume success.

Provisional identifiers: `0x005F7500` `construct_ULAT_runtime`; `0x005F7380` `update_ULAT_runtime_phases`; `0x005F76B0` `start_ULAT_skeletal_lightning_effect`; `0x005F7410` `cleanup_ULAT_effect_and_audio`. The names summarize observed operations; they are not recovered original symbols.
