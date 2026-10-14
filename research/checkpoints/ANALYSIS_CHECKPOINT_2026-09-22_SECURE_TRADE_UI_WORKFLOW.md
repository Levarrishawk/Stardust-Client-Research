# Secure Trade UI and Wire Workflow

Date: 2026-09-22

## Result

The secure player-to-player trade path is now reconstructed from concrete
`Stardust.exe` UI classes through `ClientSecureTradeManager`, named-message
serialization, Stardust server session state, and final completion. This closes
the representative trade workflow; it does not claim every generic container
helper has been renamed.

## Client classes and ownership

- `SwgCuiTrade` owns the top-level mediator at `/pda.trade`, its Cancel and OK
  buttons, waiting text, two `SwgCuiTrade::Panel` children, and subscriptions to
  `BeginTrade`, `GiveMoney`, `Accept`, `RequestVerify`, `Complete`, and
  `AbortTrade` manager events.
- Each `Panel` owns a `SwgCuiContainerProviderTrade`, a container view, an
  acceptance checkbox, a name field, and a money edit box.
- The provider derives its displayed object list from the manager's local or
  remote offer vector. On the editable/local side, a valid inventory drop calls
  `trade_send_add_item_VA_005E17F0` with the selected object's `NetworkId`.
- Event subscriptions are explicitly removed during mediator teardown; panel
  providers and child mediators are released after unsubscription.

## UI-to-network transitions

| User/state event | Candidate client function | Wire message | Result |
|---|---|---|---|
| Start trade with selected player | `trade_request_target_VA_005E13F0` / `trade_request_begin_VA_005E1760` | ObjectController `0x115`, target `U64` after the controller header | Stardust pairs reciprocal requests and sends `BeginTradeMessage` to both clients. |
| Drop eligible local inventory item | `swg_trade_provider_drop_add_item_VA_00FF61E0` -> `trade_send_add_item_VA_005E17F0` | `0x1E8D1356`, `U64 itemId` | Server validates ownership, permissions and no-trade state, records item, creates it for the peer, then echoes `AddItemMessage` only to the peer. |
| Commit local money edit | `swg_trade_panel_money_commit_VA_00ED4830` -> `trade_send_give_money_VA_005E1B20` | `0xD1527EE8`, `U32 credits` | Server clamps to current cash, stores the offer, and sends the clamped value to the peer. |
| Toggle local acceptance on | `swg_trade_panel_emit_acceptance_VA_00ED4C10` -> `trade_send_accept_VA_005E1E00` | `0xB131CA17`, no body | Server marks the sender accepted and notifies the peer. |
| Toggle local acceptance off | same dispatcher -> `trade_send_unaccept_VA_005E20F0` | `0xE81E4382`, no body | Server clears acceptance and notifies the peer. |
| Both manager acceptance flags become true | `swg_cui_trade_accept_event_VA_00D942D0` | none | Both panels are updated and the top-level OK/verify control is enabled. |
| Press final OK | `swg_cui_trade_button_callback_VA_00D93D40` -> `trade_send_verify_VA_005E23F0` | `0x9AE247EE`, no body | Client enters waiting/confirmation presentation; server marks the sender verified. |
| Both sides verified | server `handleVerifyTradeMessage` | `0xC542038B`, no body | Server revalidates items, capacity, money and range; transfers objects and cash, sends completion to both, and clears both sessions. |
| Cancel/close/range or peer loss | `swg_cui_trade_close_abort_VA_00D94430` -> `trade_send_abort_VA_005E2740` | `0x9CA80F98`, no body | Server drops both sessions, clears target IDs, and sends abort to affected clients. |

The server's named trade messages are symmetrical notifications: the same
opcodes used as client requests are sent to the peer for add-item, money,
acceptance, unacceptance and abort. `BeginTradeMessage` uniquely carries the
other player's `U64 NetworkId`; `TradeCompleteMessage` and verify are bodyless.

## Acceptance and verification policy

`SwgCuiTrade` retrieves two money values and two acceptance flags from
`ClientSecureTradeManager`. It renders local and peer values separately and
enables final verification only when both acceptance flags are true. Programmatic
checkbox updates are guarded so inbound peer/state refresh does not emit a new
network request. Editing the local offered amount validates it against the
player's cash balance; an invalid amount disables acceptance/verification and
can produce the trade money warning.

Verification is therefore a second phase after acceptance, not an alias for
accept. The server independently requires both `TradeSession::verifiedTrade`
flags and re-runs authoritative validation before moving anything.

## Lifetime and failure handling

- `SwgCuiTrade` periodically resolves the counterpart object and checks
  separation after accounting for object radii. Missing counterpart or excess
  range reports a localized failure and closes the mediator.
- Stardust performs its own final distance check (`< 15.0f`) plus inventory,
  datapad, no-trade, ownership, cash and container-capacity checks. Client-side
  gating is presentation only and must never replace these checks.
- Trade cancellation can also originate from server gameplay paths (crafting,
  auction and control-device transitions), so the replacement client must treat
  inbound abort as authoritative at every UI phase.
- `DenyTradeMessage` (`0x6EC28670`) forwards refusal to the counterpart and is
  distinct from abort/session cleanup.

## Replacement contract

Implement secure trade as an explicit session state machine:

`Idle -> Requested -> Open -> OffersMutable -> BothAccepted -> Verifying -> Completed/Aborted`

Preserve all wire widths and opcodes exactly. Represent object IDs internally as
an explicit protocol `uint64`, not as native pointers. Any offer mutation must
clear or invalidate the local accepted/verified presentation state even if the
server remains the authority. Keep offer-model state outside prototype host/UI nodes so
UI recreation cannot create or lose a protocol session.

## Confidence

- Classes, ownership, widget bindings, message opcodes and body widths:
  **Confirmed by code**.
- Money/accept/verify gating and event-to-widget effects: **Confirmed by code**.
- Server transfer, validation and cleanup behavior: **Confirmed by current
  Stardust source**.
- Some generic container-provider helper names and the historical label of the
  client distance constant remain **Unknown**, but do not block the workflow or
  codec contract.

## Evidence

- `Stardust.exe` RTTI/vtables: `SwgCuiTrade`, `SwgCuiTrade::Panel`,
  `SwgCuiContainerProviderTrade`
- Decompilation: `0x00D935F0`, `0x00D93AA0`, `0x00D93D40`, `0x00D94070`,
  `0x00D94280`, `0x00D942D0`, `0x00D94430`, `0x00D94450`, `0x00ED4300`,
  `0x00ED4830`, `0x00ED4A00`, `0x00ED4A30`, `0x00ED4B10`, `0x00ED4C10`,
  `0x00ED4C90`, `0x00FF54D0`, `0x00FF5950`, `0x00FF5B80`, `0x00FF5DC0`,
  `0x00FF61E0`, `0x00FF6290`
- Existing manager/codec analysis at `0x005E13F0..0x005E4CE0`
- Stardust source: `ZonePacketHandler.cpp`, `packets/trade/*.h`,
  `ItemDropTradeCallback.h`, and `PlayerManagerImplementation.cpp`
