# News mediator and configured disabled asset

Date: 2026-09-25. Workstream A09/R01. Target: verified `Stardust.exe` and the current `swgemu_live.cfg` TRE overlay. Decompiled output is reconstructed pseudocode.

## Result

`SwgCuiNews` is an executable-present, factory-registered mediator for a declarative news page, but the effective Stardust asset `ui/ui_news.inc` is a zero-byte override. The current configured client therefore does not provide a usable News page through this asset path.

## Executable structure

RTTI identifies `SwgCuiNews` with primary/callback vtable `0x015F082C` and mediator-base vtable `0x015F08B0`. Factory slot `0x015EE84C` reaches factory method `0x00BCC160`, which allocates `0x9C` bytes and invokes constructor `0x00CA8470`.

The constructor binds:

- `buttonBack`;
- `buttonNext`;
- `buttonIntro`;
- one required UI object whose source name is not recovered by the current symbol table;
- `image`.

It contains no URL, HTTP, browser, socket, command-queue, or packet-send path. Class-specific lifecycle methods `0x00CA8720` and `0x00CA8760` attach and detach three callbacks through the inherited callback interface. The class-specific event slot at `0x00CA87A0` is only `RET 4`; no executable navigation mutation was recovered from it. Destructor `0x00CA86A0` nulls all five widget pointers and delegates to the inherited mediator destructor.

This indicates a static/declarative page shell rather than a network news client. Exact declarative button behavior cannot be inspected from the active asset because that asset is intentionally empty.

## Current asset reachability

Three archives contain the same member name:

| Configured priority | Archive | Member |
|---:|---|---|
| 53 | `stardust_3_01.tre` | `ui/ui_news.inc` |
| 42 | `mtg_patch_013_configurable_02.tre` | `ui/ui_news.inc` |
| 3 | `data_other_00.tre` | `ui/ui_news.inc` |

The normal descending-priority lookup selects `stardust_3_01.tre`. Direct extraction confirms its payload length is **zero bytes**. Lower-priority historical copies are shadowed. This is executable/configuration evidence for a disabled current News UI, not a claim that the original lower-priority asset never contained content.

## Replacement contract

The replacement client does not need this legacy News mediator for login, networking, world entry, or gameplay compatibility. If a news surface is desired later, implement it as an optional modern service/UI feature. Do not infer an undocumented Stardust protocol message from the legacy class name, and do not make startup depend on the empty current asset.

## Confidence

- **Confirmed by code/config/assets:** RTTI, factory construction, bound widgets, lifecycle callbacks, no-op event slot, absence of a direct send in inspected methods, configured archive order, and zero-byte effective member.
- **Strongly inferred:** the high-priority empty file intentionally disables the legacy page and makes the current configured page unusable.
- **Unknown/non-blocking:** historical content and declarative behavior in the shadowed lower-priority copies; whether an obscure action still attempts to open the absent page.

Ignored reproduction evidence: `local-private/generated-evidence/swg_cui_news_*`, `ui_news_effective.inc`, and `local-private/ghidra/decompiled/swg-cui-news*/`.
