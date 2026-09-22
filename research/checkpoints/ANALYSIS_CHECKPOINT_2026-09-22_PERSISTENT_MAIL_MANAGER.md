# Persistent Mail Manager Workflow

## Scope

This block closes the active persistent-mail lifecycle above the previously
mapped request/acknowledgement packets: incoming header/body merge, unread
state, body-request and deletion queues, OOB attachment extraction, local
events, and optional mailbox export.

## Wire and manager path

`ChatPersistentMessageToClient` (`0x08485E17`) carries, in order:

1. sender `ChatAvatarId` (name, game, galaxy);
2. U32 mail ID;
3. U8 type (`0` body, nonzero header-only in current Stardust construction);
4. Unicode body or a zero-length placeholder;
5. Unicode subject;
6. length-delimited out-of-band parameter stream, or zero for header-only;
7. U8 status (`N`, `U`, or `R`);
8. U32 timestamp.

The exact client parser is `0x00AF01C0`; listener `0x008EFE80` constructs the
decoded object and passes it to `persistent_mail_apply_incoming_VA_008F0ED0`.
The current server constructors independently confirm the field order and the
`type = !sendBody` convention.

## Confirmed state behavior

- Mail is keyed by U32 ID in a manager-owned ordered collection. A header-only
  message inserts or refreshes summary state and publishes `HeaderReceived`.
- A body response for an existing entry copies body, subject and status into
  that entry, normalizes incoming `N`/`U` to read status `R`, parses its OOB
  stream, and publishes `BodyReceived` through the browser-facing path.
- A body response without a known header is warned and discarded; it does not
  silently create an orphan full-message record.
- Header arrival invokes additional registered header observers before the
  typed `HeaderReceived` transceiver. `SwgCuiChatWindow` and
  `SwgCuiPersistentMessageBrowser` are concrete RTTI subscribers.
- New (`N`) headers drive the unread-mail notification state. The manager can
  scan the complete collection to enable/disable that indicator rather than
  trusting an incremental counter.

## OOB content and attachments

`persistent_mail_expand_oob_VA_008F15F0` parses the body/OOB token stream.
Waypoint tokens become owned waypoint objects associated with the mail; prose
package tokens are expanded through the normal localized-prose machinery.
Recipient/OOB parsing also recognizes a literal `group` recipient form and
expands it against the current group membership before send. These are semantic
attachments, not arbitrary serialized client objects.

The manager publishes `AttachObject` for object handoff and `StartComposing`
for the HUD/window manager. A replacement should represent attachments as
typed immutable data (waypoint/prose records) and resolve live scene objects by
stable ID only when presentation needs them.

## Queues, throttling, and deletion

`persistent_mail_tick_queues_VA_008F3400` owns two rate-limited queues:

- body requests are popped and sent as `ChatRequestPersistentMessage`
  (`0x07E3559F`) no faster than one every 5 seconds;
- deletes are popped and sent as `ChatDeletePersistentMessage`
  (`0x8F251641`) no faster than one every 0.1 seconds.

Opening a mailbox can queue every header lacking a body and display a
"please wait" count. `ChatDeleteAllPersistentMessages` remains a separate
self-targeted operation. The send path increments a U32 correlation sequence,
records the pending send with a timeout, and processes
`ChatOnSendPersistentMessage` by sequence/result.

## Optional local export

The manager contains an explicit user-triggered mailbox export path. It writes
human-readable messages beneath a profile-specific `mail` directory and
reports saved/failed counts. This is presentation/tooling behavior, not a wire
or asset compatibility requirement; a modern client may redesign or omit it
unless preserving the feature is desired.

## Replacement-client model

Use a mailbox repository keyed by the protocol U32 mail ID, with independently
tracked header/body availability and a small rate-limited command queue. Keep
send-sequence correlation separate from received-mail identity. Preserve the
wire status bytes and OOB records, but do not preserve x86 node or string
layouts. Publish immutable `HeaderReceived`, `BodyReceived`, `ListChanged`, and
failure events to UI subscribers.

## Confidence

- **Confirmed:** incoming field order, header/body discriminator, merge rules,
  status transition, U32 identity, queue intervals, outgoing request opcodes,
  send correlation, event topology and current server constructors.
- **Strongly inferred:** exact user-facing ownership of a few notification/UI
  helper calls, based on the collection scans and concrete RTTI subscribers.
- **Unknown:** friendly names for every legacy send-result value and every OOB
  tag supported by the shared token parser; unsupported tags must remain
  skippable by length.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- `0x008EFE80`, `0x008F0ED0`, `0x008F15F0`, `0x008F2010`,
  `0x008F20D0`, `0x008F2D50`, `0x008F3400`, `0x00AF01C0`
- `MMOCoreORB/src/server/zone/packets/chat/ChatPersistentMessageToClient.h`
- current persistent-message server callbacks and constructors
- `manager_event_graph.tsv`
