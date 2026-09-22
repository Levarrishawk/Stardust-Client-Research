# Stardust.exe Analysis Checkpoint: Chat Server Status / Duplicate Opcode

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes two Stardust packet classes sharing opcode `0x7102B15F`: `ChatServerStatus` and legacy `unkByteFlag`. Neither has a producer in the current server tree, and `Stardust.exe` contains neither name, opcode literal, RTTI, descriptor nor listener. They are dormant compatibility artifacts, not required active client behavior.

## Server definitions

Both serialize operand count two followed by the shared opcode and one U8 status flag:

- `ChatServerStatus(byte flag)` writes the caller-provided value, documented as zero offline or one online.
- `unkByteFlag()` uses a reliable `BaseMessage(7)` construction and always writes one.

The latter also contains a static parser that reads an acknowledgement sequence U16, but no current call site establishes an active round trip. No semantics beyond the bytes are assigned.

The repository's separate lightweight test/client `ZonePacketHandler` recognizes `ChatServerStatus` and deliberately takes no action. That is corroborating historical compatibility evidence, not evidence about `Stardust.exe` itself.

## Executable and source evidence

- Complete PE string and RTTI scan: neither class name.
- Raw/immediate scan: no `0x7102B15F`.
- Recovered listener registrations: no matching descriptor.
- Adjacent EXE/DLL scan: no `ChatServerStatus` name.
- Server-wide constructor searches: no active construction of either class.
- Duplicate opcode and one-byte payload confirmed directly in both headers.

## Replacement requirements

- Do not gate login, chat readiness or reconnect behavior on this dormant opcode.
- A compatibility parser may accept operand count two, opcode `0x7102B15F`, and one U8 status, log it, and otherwise no-op.
- Do not assign transport acknowledgement semantics to the application flag based only on the unused `unkByteFlag::parse` helper.
- Keep the opcode registry capable of recording historical aliases without registering two competing active handlers.

## Confidence

- **Confirmed:** identical opcode, byte layouts, absence from this executable, and absence of current server producers.
- **Strongly inferred:** both definitions are historical/test compatibility remnants.
- **Unknown:** original distinction between the aliases and whether another server deployment ever emits either form.

## Progress

The remaining reachable alias moves to documented (`unkByteFlag` was already recognized by the corpus): **248 documented / 97 reachable gaps / 31 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
