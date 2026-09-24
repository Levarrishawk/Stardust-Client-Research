# Default command parser: literal-token inventory and quit boundary

## Scope

`SwgCuiCommandParserDefault` constructor `0x01010980` installs the all-commands root. Its dispatch `0x010116C0` spans 13,838 executable bytes / roughly 2,139 decompiler lines. This checkpoint inventories literal comparisons and closes only the `quit` branch; [the `/serverhelp` outbound branch](ANALYSIS_CHECKPOINT_2026-09-24_SERVERHELP_CONGENERIC_OUTBOUND.md) has its own mapping. A comparison list is **not** a behavioral reconstruction of all branches. Source is original `C:\Stardust-DEV\Stardust.exe`; decompiled C is reconstructed pseudocode.

The reproducible `tools/python/extract_command_parser_tokens.py` scan finds **44 compare sites for 42 distinct literals** against the command-token comparator `0x00A86B20` within this function. Two alarm tokens recur at branch continuations. All extracted strings are ordinary ASCII targets of the compare call:

```text
help                    serverhelp              history
version                 breakPoint              memoryReport
garbageCollect          brightnessContrastGamma resize
createMail              fatal                   exception
quit                    spatialChat             alias
unalias                 save_alias              load_alias
mood                    moods                   chatTypes
emoticons               reloadClientTemplate    vtune
who                     bug                     alarmAddIn
alarmAddInRepeat        alarmAddAt              alarmAddAtRepeat
alarm                   alarmRemove             alarmSnooze
earthTime               filter                  afk
afktime                 afkmessage              match
flushGraphicsResources  copyCrashReportInformation
reloadTextures
```

This array is an entry-point map only. Names such as `fatal` and `exception` must not be assumed safe to execute; no command was run. Subparsers installed by the constructor (including `/nebula` and ship commands) have their own functions and are **not** included in the 42.

For `quit`, compare call at `0x01012015` enters a branch that checks `DAT_01908854 == 1` and otherwise calls `0x00423720`. That helper sets `DAT_019085A2=1`. The same byte is read by `quit_requested_VA_00423750` and `frame_tick_VA_004237C0`, establishing a real client-loop quit request. This is importantly distinct from LoginScreen Cancel's shared CuiManager mode helper `0x00881450`, for which no quit-flag write was found.

**Confirmed:** constructor/dispatch addresses, 44/42 static token comparisons, `/quit` flag write and frame-loop read. **Tentative:** why `DAT_01908854==1` bypasses quit (possibly local-mode policy). **Unknown:** semantics of most 40 remaining literals, their argument validation, side effects and whether all are user-reachable in this build. The scanner is a discovery tool, not a proof that every control-flow branch is reachable or every argument path is valid. No new packet or whole-client percentage is claimed.
