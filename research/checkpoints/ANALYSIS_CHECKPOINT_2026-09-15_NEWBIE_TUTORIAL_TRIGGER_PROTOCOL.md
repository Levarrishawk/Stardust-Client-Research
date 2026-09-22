# Stardust.exe Analysis Checkpoint: Newbie Tutorial Trigger Protocol

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes the bidirectional tutorial trigger protocol. Server-to-client `NewbieTutorialRequest` (`0x90DD61AF`) installs a named one-shot UI/gameplay trigger. When that action occurs, the client constructs `NewbieTutorialResponse` at `0x00AD2390` and echoes the action string to server callback `NewbieTutorialResponseCallback`, registered at the distinct opcode `0xCA88FBAD`.

The server source places both classes in one header, causing a mechanical opcode extractor to associate the response row with the request's earlier literal. That is a matrix-source limitation: the authoritative response opcode is the callback-factory registration `0xCA88FBAD`, not `0x90DD61AF`.

## Wire contracts

| Direction | Message | Opcode | Payload |
|---|---|---:|---|
| Z→C | `NewbieTutorialRequest` | `0x90DD61AF` | ASCII16 action key |
| C→Z | `NewbieTutorialResponse` / server `NewbieTutorialResponseCallback` | `0xCA88FBAD` | ASCII16 echoed action key |

## Client request path

The executable embeds both class names and registers request descriptor `0x015EAF70` in multiple listeners. The broad UI listener provides the principal action-trigger path:

```text
descriptor 0x015EAF70
  -> listener 0x00E40E40 branch 0x00E4114C
  -> request parser 0x00AD3C60
  -> action accessor 0x00AD3E30
  -> exact action-key comparisons
  -> set corresponding retained one-shot trigger byte (+0x54 through +0x5B)
```

The recovered literal set includes `zoomCamera`, `changeLookAtTarget`, `overlayMap`, `clientReady`, `changeMouseMode`, `openCharacterSheet`, `closeCharacterSheet`, `openInventory`, `chatbox`, `openStatMigration`, `closeStatMigration`, `openHolocron`, `closeHolocron`, and `equipToolbar`. Different listeners own actions outside the broad UI listener's local flag range.

Direct callers of response constructor `0x00AD2390` occur in scene, UI, camera and inventory/event functions, including `0x005187C0`, `0x00518870`, `0x00518920`, `0x0051AF10`, `0x0087FA90`, `0x00C87D50`, several `0x00E41xxx`/`0x00E42xxx` functions and `0x00F669D0`. This confirms distributed event-edge emission rather than polling one monolithic tutorial system.

## Server response behavior

The callback parses the echoed ASCII key, validates session/player and notifies observers for:

- `zoomCamera`;
- `chatbox`;
- `closeHolocron`;
- `openInventory`;
- `closeInventory`.

For `clientReady`, it additionally requires the tutorial zone, resolves the enclosing tutorial building, checks ownership, then starts `TutorialScreenPlay`. Other echoed keys may be meaningful to client trigger management without a current server callback branch.

## Replacement requirements

- Preserve the two distinct opcodes despite the shared source header.
- Store multiple trigger keys concurrently and treat them as one-shot unless compatibility evidence establishes the historical `closeInventory` reset defect must be reproduced.
- Emit the exact requested key only when its corresponding user/client event occurs.
- Route each trigger through the subsystem that owns the event (camera, inventory, UI, targeting), not through frame-time UI scraping.
- Unknown keys should be retained or safely ignored according to policy, never executed as commands or paths.
- Keep the tutorial response usable before world rendering is complete for `clientReady`.

## Confidence and unknowns

- **Confirmed:** both executable codecs/names, request listener/parser/action comparisons, distributed response constructor calls, server registration opcode and callback branches.
- **Strongly inferred:** retained bytes are one-shot armed flags cleared by their respective event callbacks.
- **Unknown:** exact reset behavior for every key, whether Stardust content uses the full historical key set, and whether the documented `closeInventory` defect is required compatibility.

## Progress

Two reachable rows move to documented: **252 documented / 93 reachable gaps / 27 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
