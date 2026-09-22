# Death and Cloning Workflow

Date: 2026-09-22  
Scope: `Stardust.exe` client boundary correlated with current Stardust server

## Architectural finding

The current server does not require a dedicated cloning network message or a
hard-coded cloning client mediator. Ground cloning is implemented through the
already-recovered generic SUI protocol. This explains the absence of a distinct
`SwgCuiClone` class/string in `Stardust.exe`.

## End-to-end flow

1. Server death handling sets authoritative dead/posture state and calls
   `PlayerManagerImplementation::sendActivateCloneRequest`.
2. For space death, the server translates the search origin to the stored
   ground launch zone/location.
3. The server replaces an existing `CLONE_REQUEST` SUI box and constructs a
   `SuiListBox` with `@base_player:revive_title`.
4. It resolves the pre-designated facility, searches `cloningfacility` objects,
   applies ban/faction/Jedi eligibility, and chooses the closest valid facility.
5. Rows retain the facility's U64 object ID server-side; visible prompt text
   includes nearest/predesignated names and cash.
6. The client receives ordinary `SuiCreatePageMessage`, creates the original
   list-box root page, applies commands, and returns `SuiEventNotification` with
   the selected row index.
7. `CloningRequestSuiCallback` revalidates box type, death state, event,
   argument count and index. It resolves the retained facility ID and calls
   `sendPlayerToCloner`.
8. Cancel/invalid selection leaves the player dead and `/activateClone` can
   reopen the UI.

The client returns a row index, not the facility object ID. The server's retained
SUI state owns the authoritative mapping.

## Replacement implications

- Implement generic SUI before cloning UI; no special clone packet is needed.
- Do not locally enumerate eligible facilities; the server filters choices.
- Preserve page/event correlation and U64 facility IDs without narrowing them
  to native UI handles.
- Treat response indices and Unicode arguments as untrusted input.

The current server flow is **Confirmed by source** and the generic SUI client
codec/interpreter is **Confirmed by `Stardust.exe`**.
