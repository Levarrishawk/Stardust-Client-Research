# Mission UI and Replication Boundary

## Scope

This block reconciles the mission-terminal manager, mission-object replication,
browser/details mediators, and current Stardust server behavior. It extends the
previously closed wire codecs with the presentation/state boundary needed by a
replacement client.

## Confirmed findings

### The live Stardust list path is replication-driven

`MissionListRequest` controller `0xF5` carries flags, an incrementing U8 request
sequence, and the terminal NetworkId. The current server validates the terminal,
range and cooldown, then creates or refreshes `MISO` mission objects. The client
`ClientMissionObject` consumes the 17-field `MISO:3` baseline and publishes
`EnteredMissionBag` when its refresh field changes. `SwgCuiMissionBrowser` owns
subscriptions for both `EnteredMissionBag` and `LeftMissionBag`, so the browser
can maintain its projection as replicated mission objects appear and disappear.

This is the interoperable path a new client must implement first. It must not
wait for `PopulateMissionBrowserMessage` before showing terminal results.

### A dormant explicit list/details protocol remains in the executable

The listener at `0x009783D0` recognizes `PopulateMissionBrowserMessage`
(`0x88D9885C`) and publishes `CuiMissionManager::Messages::ReceiveList` after
decoding `U32 count + repeated U64 mission IDs`. The current server retains a
constructor for the message but has no emission call site.

The client can also send controller `0xF7` containing mission ID, terminal ID
and a U8 sequence, and can receive `0xF8` through a sequence guard before
publishing `ReceiveDetails`. Current Stardust registers no `0xF7` callback and
emits no `0xF8`. This pair is historical capability, not a requirement for the
first compatible client.

### Accept and abort are active authoritative workflows

- Accept sends `0xF9` with mission ID, terminal ID and terminal-list index. The
  server validates and returns `0xFA`; `0x00978B70` reconciles the pending index
  and publishes `ResponseAccept`.
- Abort sends `0x142` with a mission NetworkId. Stardust validates ownership and
  returns the same inner type; `0x00978E20` clears pending state and publishes
  `ResponseAbort`.
- `0x009783D0` receives message-box completion events and converts confirmed
  accept/abort dialogs into those request calls. Close events clear the six
  tracked pending-dialog globals without transmitting a request.

### UI event topology is explicit in RTTI

`SwgCuiMissionBrowser` subscribes to `ReceiveList`, `ReceiveDetails`,
`ResponseAccept`, `EnteredMissionBag`, and `LeftMissionBag`.
`SwgCuiMissionDetails` subscribes to `ResponseAccept`, `ResponseRemove`, and
`ResponseAbort`. The `ResponseRemove` transceiver is real RTTI-backed code, but
no current Stardust callback/emitter or independently identified live request
was found. It remains a bounded legacy branch rather than an assumed live wire
contract.

## Replacement-client consequence

Implement mission browsing as a projection over replicated `ClientMissionObject`
state, keyed by NetworkId and updated by baseline/delta/object-lifetime events.
Keep terminal request sequencing and pending UI state separate from that durable
projection. Treat explicit list/details messages as optional compatibility
extensions until a server path or capture proves them live.

## Confidence

- **Confirmed:** active request/response codecs, `MISO:3` field order, manager
  event types, browser/details subscriptions, and current server registrations.
- **Strongly inferred:** `EnteredMissionBag`/`LeftMissionBag` are the primary live
  browser refresh mechanism, from the absence of current explicit-list emission
  plus the concrete mediator subscriptions and server object refresh behavior.
- **Unknown:** historical `ResponseRemove` request/opcode and the remaining
  fields of the unused `0xF8` details response.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- mission manager listener `0x009783D0`
- mission browser RTTI/vtables `0x015F06D4..0x015F0704`
- mission details RTTI/vtables `0x015F0804..0x015F081C`
- `MMOCoreORB/src/server/zone/packets/object/MissionListRequestCallback.h`
- `MMOCoreORB/src/server/zone/packets/object/MissionAcceptCallback.h`
- `MMOCoreORB/src/server/zone/packets/object/MissionAbortCallback.h`
- prior `MISO:3` and packet-handler evidence in the canonical research set
