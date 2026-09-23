# Force-feedback filename resolution: an unresolved compatibility edge

Date: 2026-09-22. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Confirmed static path

`0x00421ED0` calls `0x00A931A0` with the authored `.ffe` path and a destination buffer. `0x00A931A0` selects the first matching mounted search node through `0x00A92FD0`, then invokes its virtual slot `+0x10` to obtain a resolved name. `0x00421ED0` uses that returned name as a cache key and passes it into the feedback loader. `0x00420010` ultimately calls the selected DirectInput device's `EnumEffectsInFile` virtual slot `+0x6C` with that name and callback `0x00420360`; the callback creates/records individual effect instances. Installed Windows SDK `dinput.h` identifies the interface slot.

The SearchTree virtual-name method is `0x00A99710` (vtbl `0x015EA454`, confirmed by constructor `0x00A992E0`). It formats `archive-path[logical-name]`: the delimiters at `0x0186F53C` and `0x0186F538` are ASCII `[` and `]`. SearchTOC `0x00A99E20` uses related source/member notation; SearchCache `0x00A9A7B0` formats `SearchCache[logical-name]`. This is an internal resolved-name notation, **not evidence of an ordinary physical pathname or extraction**. A prior working hypothesis that `0x00A9B1E0` materialized `.ffe` files was rejected: that function lazily wraps decompressed archive bytes in a file-view object. Its vtable `0x015EA4E0` is not a SearchNode; slot `+0x10` is `0x00A9B540`, a seek operation.

All eight referenced `.ffe` entries are present in the installed TRE overlay. Their payloads begin `RIFF`, followed by a zero four-byte size field and `FORC` at byte 8; sizes range 264–1240 bytes. Seven winning entries come from `mtg_patch_011_files_01.tre` (configured priority 40), and one from `stardust_3_00.tre` (priority 52). This checks archive presence and a common force-feedback container signature, **not** that Windows can consume the resolved notation.

## Open question and engineering consequence

Static evidence does not yet show how `archive[logical]` is made acceptable to `EnumEffectsInFile`, or whether that path fails when effects are archive-only. Possibilities include a materialization path, an interception layer, or a dormant/failing optional feature; none is established. Do not claim original haptics work solely because `.ffe` assets exist. A replacement client's asset manager should read the archive payload and hand it to a native haptic implementation or a deliberately managed temporary file interface; haptics remain outside the first x64 networking POC.

Confidence: **confirmed** for name formatting and DirectInput call; **unknown** for successful runtime consumption of archived `.ffe` files. No live DirectInput/hardware test was performed.

Follow-up (2026-09-22 late): `0x00421ED0` passes the search-tree-resolved name into the cached feedback construction path `0x004216B0` → `0x00420E20` → `0x00420010`. In the inspected path, `0x00420E20` checks device availability and calls `0x00420010`, which forwards that same name directly to the DirectInput vtable `+0x6C` `EnumEffectsInFile` call. There is **no intervening extraction or path rewrite in this call chain**. This narrows the open question: any mechanism making `archive[member]` acceptable must occur outside the inspected chain, or archive-only feedback may not play. Do not infer a working interception layer from the static path.
