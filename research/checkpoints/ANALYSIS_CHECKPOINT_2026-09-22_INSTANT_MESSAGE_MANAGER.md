# Instant-Message Manager State and Correlation

## End-to-end mapping

| Direction/message | Client path | Fields | Result |
|---|---|---|---|
| C→Z `ChatInstantMessageToCharacter` / `0x84BB21F7` | `cui_instant_message_send_VA_00900A60` → archive ctor `0x00AF2900` → reliable send | ASCII game, galaxy, avatar; Unicode text; U32 zero/spacer; U32 sequence | create pending-send record keyed by sequence and optionally show local outgoing text |
| Z→C `ChatOnSendInstantMessage` / `0x88DBB381` | listener `cui_instant_message_listener_receive_VA_00900550` → parser `0x00AF2290` | U32 result; U32 sequence | find pending record, publish localized outcome/failure, retire it |
| Z→C `ChatInstantMessageToClient` / `0x3C565CED` | same listener → parser `0x00AF2610` → `cui_instant_message_handle_incoming_VA_00900FB0` | sender ChatAvatarId; Unicode text; U32 zero/compatibility field | ignore-filter sender, update reply target/history, publish received-message event, optionally auto-reply |

All three routes are **Confirmed** by executable parsers and active Stardust source.

## Pending outbound state

`cui_instant_message_send_VA_00900A60` increments global U32 sequence `0x0192C3C8`, builds the wire message, and stores a pending element containing recipient identity, message text, sequence, an expiry timestamp derived from current time plus a fixed timeout, and a Boolean presentation mode. Before insertion it removes expired pending entries.

The acknowledgement handler performs sequence lookup. A missing sequence falls through an `UNKNOWN` diagnostic/result path rather than binding the acknowledgement to an arbitrary tell. A replacement must therefore use a monotonic U32 request ID and an explicit pending map; it must preserve U32 wrap semantics and reject stale/unknown replies.

## Result codes

The executable has dedicated branches for the same values declared in current `ChatManager.idl`:

| Value | Server name | Meaning |
|---:|---|---|
| `0` | `IM_SUCCESS` | delivered |
| `4` | `IM_OFFLINE` / shared `NOAVATAR` value | recipient unavailable or not found, depending on server path |
| `9` | `IM_IGNORED` | recipient ignores sender |
| `16` | `IM_TOOLONG` | text exceeds server limit |

Current Stardust limits instant-message text to 255 Unicode characters. For privacy, an ignored sender is not told whether the target is online: the server returns the ignored result before exposing online status.

## Incoming behavior

`cui_instant_message_handle_incoming_VA_00900FB0` first normalizes the sender identity and calls the social ignore predicate. Ignored senders do not enter the reply/history state and do not publish `CuiInstantMessageManager::Messages::MessageReceived`.

For accepted messages, the manager copies game/galaxy/avatar plus text into current/history state, updates the reply identity, and publishes a typed `CuiInstantMessageManagerElement`. If the local away/automatic-response state is active and the sender is not the locally excluded identity, it composes the configured auto-reply and sends it through the same sequenced outbound path. That generated send is flagged to avoid ordinary local presentation behavior.

RTTI confirms two public event boundaries:

- `MessageReceived` carrying `CuiInstantMessageManagerElement const&`;
- `MessageFailed` carrying a Unicode string.

Chat windows and the system-message mediator own subscribers to these events.

## Replacement requirements

- Keep chat identity as value data `{game, galaxy, avatar}`; do not collapse it to a display string.
- Preserve the exact wire order and U32 sequence even in the x64 client.
- Hold pending sends in an x64-safe map keyed by U32 and expire them deterministically.
- Apply ignore filtering before updating reply state or emitting notifications.
- Route automatic replies through the same protocol service with an explicit recursion/presentation flag.
- Keep UI localization/result formatting outside the protocol parser.

The trailing U32 in the incoming message is zero in current Stardust and remains an opaque compatibility field. Its position and width are confirmed; no gameplay semantic is invented.
