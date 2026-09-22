# Stardust.exe Analysis Checkpoint: Tutorial HUD Element Control

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes active `NewbieTutorialEnableHudElement` (`0xCA375124`). The packet carries an ASCII HUD-element key, U8 enable flag and F32 blink count. Broad UI listener `0x00E40E40` dispatches at `0x00E413AA`, parses with `0x013F29E0`, compares the key against retained HUD-element identifiers (including the shared `all` key), configures blink behavior through `0x00E43730`, and shows or hides the associated widget.

## Server correlation

The producer is exposed through the server object's `sendNewbieTutorialEnableHudElement` method and its Lua binding. The server header records supported keys `radar`, `toolbar`, `buttonbar`, `hambar`, `chatbox` and `all`. Thus tutorial screenplays can manipulate the HUD without coupling Lua to client widget pointers.

## Replacement requirements

- Preserve ASCII key, U8 boolean and IEEE-754 F32 blink count exactly.
- Implement semantic HUD roles rather than copying original widget addresses.
- Treat `all` as a broadcast selector across supported tutorial HUD roles.
- Clamp/validate blink count and ignore unknown keys with diagnostics.
- Apply visibility and blink animation on the UI thread; no rendering API dependency belongs in the protocol handler.

## Confidence

- **Confirmed:** opcode, complete three-field codec/accessors, UI listener branch, key comparison fan-out, blink configuration and show/hide operations; active C++ and Lua server exposure.
- **Strongly inferred:** retained client key globals correspond to the server-documented role set; exact key-to-original-widget-offset pairs are unnecessary for replacement behavior.
- **Unknown:** whether historical content used aliases beyond the six documented keys.

## Progress

One reachable packet moves to documented: **268 documented / 77 reachable gaps / 14 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
