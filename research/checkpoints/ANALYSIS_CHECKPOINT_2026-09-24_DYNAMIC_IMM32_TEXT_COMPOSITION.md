# Dynamic IMM32 text-composition and candidate boundary

Date: 2026-09-24. Scope W02+W10/A09 follow-up. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Ghidra output is reconstructed pseudocode; addresses are provisional.

## Confirmed by code

The main PE does **not** statically import IMM32, but it contains method-name strings. Initializer `0x009B4FF0` calls `0x009B5540` once, captures `GetKeyboardLayout(0)`, installs exit cleanup and input callbacks, and can enable an initial IME state. Loader `0x009B5540` constructs a path from `GetSystemDirectoryA` plus `\imm32.dll`, invokes `LoadLibraryA`, and resolves named APIs with `GetProcAddress` into global function pointers. The resolved family includes `ImmGetContext`/`ImmReleaseContext`, `ImmGetCompositionStringW`, `ImmSetCompositionStringW`, `ImmGetCandidateListW`, open/conversion status, window/layout helpers, and context locking. It separately loads `version.dll` for version-info APIs. A missing DLL or individual export leaves its pointer unset; caller guard policy was not exhaustively audited.

`0x009B5FF0` obtains the active input context and reads `ImmGetCompositionStringW` under observed flags including `0x800` and `8`, places UTF-16 data into a retained composition buffer, updates local state and releases the context. This identifies the client-side *composition-text* boundary; precise commit/cancel and all flag semantics remain to be traced.

`0x009B6310` obtains the context, queries `ImmGetCandidateListW` for required byte size, allocates/fills a candidate-list buffer, reads count/selection/page fields and walks offset-addressed UTF-16 candidate strings into a retained vector, then releases the context. This is a **confirmed** OS candidate-enumeration and local-data conversion path. A separate [IMEInput mediator](ANALYSIS_CHECKPOINT_2026-09-24_IME_INPUT_MEDIATOR_BOUNDARY.md) binds `listCandidate` and presents an overlay, but the exact transfer/callback edge from this vector to that widget was not established here. Do not infer that every Windows text-service mode uses this legacy path.

The initializer registers Win32 message callback `0x009B51F0`. It distinguishes message codes `0x51` (input-language change), `0x10D` (IME start composition), `0x10E` (IME end composition), `0x10F` (IME composition) and `0x282` (IME notify). Start invokes `0x009B5FF0(0x88)`; composition passes the incoming parameter to that reader. IME-notify subcodes `3` and `5` call candidate enumeration `0x009B6310`; `4` changes candidate visibility state, and `6`/`8` take another local path. It publishes local input event IDs through `0x00AB5670`. This closes the OS-message→composition/candidate-reader trigger but **not** the final widget consumption or edit commit semantics.

## Replacement-client consequence and limits

The modern platform layer needs Unicode composition and candidate-selection events, independent of chat/server serialization. On Windows the implementation may use current text-input facilities rather than copying these dynamic IMM32 pointers, provided user-visible composition/candidate behavior is preserved. Do not derive network string encoding from the OS IME buffer alone.

- **Confirmed:** dynamic DLL path/import names, one-time initializer, Win32 IME message trigger, composition/candidate API calls, UTF-16 candidate extraction and local storage.
- **Strongly inferred:** this service supplies the observed IME candidate presentation mediator, but the final vector→widget edge is still open.
- **Unknown:** exact UI subscription, edit commit/cancel behavior, per-export null handling and coverage of modern TSF input methods.

Evidence: PE import/string/reference scans; read-only Ghidra decompilation of `0x009B4FF0`, `0x009B5540`, `0x009B51F0`, `0x009B5FF0`, `0x009B6310`. Transient output is ignored under `local-private/scratch/ime_platform/`.
