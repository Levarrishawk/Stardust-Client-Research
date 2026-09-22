# Stardust.exe Analysis Checkpoint: Player Resource Queries

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes `StomachRequestMessageCallback` (`0xB75DD5D7`) and `PlayerMoneyRequestMessageCallback` (`0x9D105AA1`). Both are payload-free client requests. UI subsystem helper `0x00FA1280` constructs and reliably sends the stomach request and then the money request during initialization/refresh.

Stardust answers stomach state through `PlayerObjectDeltaMessage9::updateStomachFilling()`, reusing normal replication. It answers money with `PlayerMoneyResponse` (`0x367E737E`), containing bank and cash LE32 values. This executable parses that response in listener `0x00FA0100` but immediately destroys it without reading either value, indicating replicated object state—not this legacy response—is authoritative for this build.

## Client request flow

```text
player-status/UI initialize or refresh 0x00FA1280
  -> construct StomachRequestMessage 0x013F6A30
  -> reliable send 0x00462670
  -> construct PlayerMoneyRequest 0x013F6840
  -> reliable send 0x00462670

Stomach response
  -> ordinary PLAY:9 delta -> existing replication path

PlayerMoneyResponse descriptor 0x01609988
  -> listener 0x00FA0100 branch 0x00FA03F5
  -> parser 0x013F64D0 (bank U32, cash U32)
  -> no accessor/read; object destroyed
```

## Server behavior

- Stomach callback validates player, obtains PlayerObject, builds view-9 delta, updates stomach filling fields and sends it.
- Money callback validates player and sends bank credits followed by cash credits using a reliable server message.
- Both request parsers consume no payload.

## Replacement requirements

- Preserve request opcodes and empty payloads if implementing legacy UI refresh parity.
- Treat the stomach reply as normal `PLAY:9` state; do not create a parallel stomach model.
- Decode bank/cash as fixed-width 32-bit wire values even if internal currency storage is wider in a new client.
- Prefer replicated object fields as authoritative where available, but accept the money response idempotently for interoperability and diagnostics.
- Coalesce redundant resource refresh requests and send them reliably outside the render loop.
- Guard server callbacks against absent PlayerObject as well as absent player in a hardened implementation; current stomach callback assumes it exists.

## Confidence

- **Confirmed:** both request names/constructors, sequential reliable sends, callback opcodes/empty parsers, server response builders, money response parser fields and discard behavior.
- **Strongly inferred:** the containing UI subsystem requests both values when its player-status display initializes or refreshes.
- **Unknown:** original reason money response remains parsed-but-unused and whether current content invokes later refreshes beyond subsystem initialization.

## Progress

Two reachable callbacks move to documented: **257 documented / 88 reachable gaps / 25 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
