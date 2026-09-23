# Crafting ObjectController Codec Boundary

Date: 2026-09-22

## Result

The principal crafting-session ObjectController payload classes are now mapped
in `Stardust.exe` and correlated with current Stardust callbacks. This block
closes the byte-level client codecs for schematic-list delivery, ingredient
fill/empty, experimentation, customization, generic stage responses and window
closure. Detailed behavior of each of the seven crafting mediators remains a
separate workflow layer.

## Controller map

All rows below are inner ObjectController message types. The common outer
ObjectController envelope, target ID, flags and sequencing are handled by the
already documented controller transport and are not native C++ object layouts.

| Inner type | Direction | Executable codec | Inner typed payload | Stardust action / client effect |
|---:|---|---|---|---|
| `0x102` | Z→C | `0x00AF6D00` | `U64 toolId`, `U64 stationId`, `S32 count`, repeated `{U32 serverCrc, U32 clientCrc, S32 toolTab}` | `CraftingSession::startSession` supplies the allowed schematic list; `CuiCraftManager` publishes `DraftSchematicsReceived`. |
| `0x107` | C→Z | `0x00AEC240` | `U64 ingredientId`, `S32 slot`, `S32 extra field (ignored by current server)`, `U8 counter` | `CraftingAddIngredientCallback` resolves a tangible, aborts concurrent trade, and calls `session->addIngredient`. |
| `0x108` | C→Z | `0x00AF5430` | `S32 slot`, `U64 ingredientId`, `U8 counter` | `CraftingRemoveIngredientCallback` validates session/object and calls `session->removeIngredient`. |
| `0x106` | C→Z | `0x00AF5DF0` | `U8 counter`, `S32 count`, repeated `{S32 row, S32 points}` | Server requires crafting state 3 and calls `session->experiment`; client record also accumulates total requested points locally. |
| `0x15A` | C→Z | `0x00AF57B0` | `Unicode16 name`, `U8 templateChoice`, `S32 schematicCount`, `U8 pairCount`, repeated `{S32 index, S32 value}` | Server calls `session->customization` with the name, template selection, schematic count and customization pairs. |
| `0x10C` | Z→C | `0x00B1F4D0` | `S32 operation`, `S32 result`, `U8 counter` | Shared `MessageQueueGenericIntResponse`; crafting uses nested operation IDs `0x107..0x10B` for slot, assembly/customization and hopper/status acknowledgements. |
| `0x1BE` | Z→C | same generic-int codec | `S32 operation`, `S32 result`, `U8 counter` | Crafting experimentation/customization response family; exact operation semantics are produced by session state. |
| `0x113` | Z→C | same generic-int codec | `S32 operation`, `S32 result`, `U8 counter` | Stage response used by the experimentation path. |
| `0x1C2` | Z→C | `0x011951C0` | `U8/Boolean value` | Close/end crafting-window signal; current server sends the client counter byte through this generic Boolean-shaped record. |

The executable registers `0x107`, `0x108`, `0x106`, and `0x15A` with the
locally-originatable factory flag set. It registers `0x102`, `0x10C`, `0x1BE`,
`0x113`, and `0x1C2` without that flag, matching their server-to-client use.

## State and ownership observations

- Stardust session states are externally meaningful: state 1 sends the draft
  list, state 2 owns ingredient assembly, and state 3 permits experimentation.
- Starting a session writes PLAY:9 crafting state, experimentation eligibility,
  closest station ID and point count before sending `0x102`.
- Choosing a draft creates both `ManufactureSchematic` and prototype objects;
  their baselines/deltas, rather than controller payloads alone, drive assembly
  UI data.
- Ingredient or experiment actions abort an active trade. A replacement must
  not model crafting and trade as independent overlapping modal sessions.
- Server-side session validation enforces player/tool/station lifetime and a
  seven-metre station range. Invalid state closes the window and clears the
  session.
- Cancellation removes active session ownership from both player and tool,
  resets PLAY:9 crafting state, destroys transient schematic/ingredient objects,
  and conditionally destroys an uncommitted prototype.

## Replacement contract

- Encode/decode each controller payload with explicit fixed-width types; never
  serialize host structs or pointer-sized values.
- Treat the `U8 counter` fields as request/response correlation tokens and
  preserve wrap behavior rather than widening the wire format.
- Keep crafting session/model state outside UI mediators. UI screens subscribe
  to manager events and replicated `ManufactureSchematicObject` changes.
- Apply server `0x10C/0x1BE/0x113` results by `(operation, counter)` and ignore or
  diagnose stale responses after a session ends.
- Retain original draft/manufacture/crafted templates and localization as
  required data dependencies; the server's `0x102` list is identifiers and
  categories, not a complete schematic description.

## Confidence and remaining scope

- Factory registrations, direction flags, codec field order/widths: **Confirmed
  by code**.
- Current server parsing, validation and session ownership: **Confirmed by
  current Stardust source**.
- `0x102` manager publication and seven-mediator topology: **Confirmed by RTTI
  and typed transceivers**, with individual screen behavior still incomplete.
- Human names for every generic-int operation/result value: **Partially known**;
  these require the next crafting-session response/state block.

## Evidence

- Executable registrations/codecs: `0x00AEC190`, `0x00AEC240`, `0x00AF52F0`,
  `0x00AF5430`, `0x00AF56A0`, `0x00AF57B0`, `0x00AF5D10`, `0x00AF5DF0`,
  `0x00AF6920`, `0x00AF6D00`, `0x00B193D0`, `0x00B1F4D0`, `0x011951C0`
- Stardust: `ZonePacketHandler.cpp`, the four `Crafting*Callback.h` files, and
  `CraftingSessionImplementation.cpp`
