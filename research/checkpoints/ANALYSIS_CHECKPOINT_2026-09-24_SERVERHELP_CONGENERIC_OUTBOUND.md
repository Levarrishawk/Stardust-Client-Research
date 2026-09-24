# `/serverhelp` sends `ConGenericMessage` in the reverse direction

## Evidence

This is one bounded branch of the large `SwgCuiCommandParserDefault` parser in original `C:\Stardust-DEV\Stardust.exe`. The rest of that parser remains unclassified. The token pointer loaded at `0x010118AE` is `0x015F7FF0`, which resolves to literal `serverhelp`. The branch joins its remaining command arguments with spaces, constructs `ConGenericMessage` at `0x013F4CF0` with trailing scalar zero, then calls reliable `game_network_send_VA_00462670` at `0x010119FC`. The constructor embeds the same named message and opcode `0x08C5FC76` established by the earlier [server-to-client ConGeneric checkpoint](ANALYSIS_CHECKPOINT_2026-09-15_CON_GENERIC_MESSAGE.md). Instruction-level call order at `0x010119DE..0x01011A0E` confirms construction, send and teardown; Ghidra's high-level parser pseudocode drops some arguments and should not be read as source.

The current Stardust-3 server **constructs** `ConGenericMessage` in `MMOCoreORB/src/server/zone/packets/ui/ConGenericMessage.h` to display ASCII text and zero U32 in chat, but a repository search found no `serverhelp` command implementation and no `0x08C5FC76` receive callback registration in `ZonePacketHandler.cpp`. The current server packet-gap matrix correctly lists the current server-source use as S→C; this client-originating branch is an additional original-client capability, not a contradiction in that server-source matrix.

## Contract and limits

| Direction | Source | Body | Observed action | Current Stardust handling |
|---|---|---|---|---|
| C→Z | `/serverhelp` in `SwgCuiCommandParserDefault` | ASCII joined arguments; U32 zero | reliable request/send | no matching receive registration found in current source |
| Z→C | current server `ConGenericMessage` | ASCII chat text; U32 zero | client chat presentation | documented earlier |

**Confirmed:** literal command, constructor/send path, common opcode/body shape, and absence of an exact server registration in the examined source. **Strongly inferred:** `/serverhelp` was intended for a historical server-side responder. **Unknown:** original intended response/command policy, and whether a different or dynamic server handler outside the searched current source can consume it. Do not design a replacement around a response that current Stardust does not implement. This is a functional compatibility observation, not a first x64 networking-POC gate or evidence of a new opcode.
