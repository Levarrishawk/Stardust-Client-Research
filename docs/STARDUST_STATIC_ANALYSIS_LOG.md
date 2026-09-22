# Stardust.exe Static Analysis Log

Primary artifact: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

This log records direct executable evidence. Repository source correlations are explicitly secondary.

## 2026-09-11 — Active ObjectController schema closure

- Confirmed from `ObjectControllerMessageCallback::parse` that registered inner callbacks begin at outer offset `0x16`, after priority, type, and object ID. Their common initial four-byte skip consumes the envelope scalar.
- Documented the wire fields and server-side result for all 24 ObjectController inner types currently accepted by Stardust-3 in `ACTIVE_OBJECT_CONTROLLER_SCHEMAS.md`.
- Identified an architectural requirement: controller payload codecs are keyed by direction plus type. For example, `0x117` C→S cancellation and S→C queue removal use different layouts, while `0x142` serves both request and response roles.
- Kept unresolved padding/scalar fields explicitly unknown for `0xED`, `0x115`, `0x146`, and `0x3FA`; no semantics were invented.

### `0x115` follow-up correction

- `MessageQueueSecureTrade` constructor `0x00AE2E40` and serializer `0x00AE3010` resolve the formerly opaque 12 bytes: the typed payload is U32 operation plus two U64 NetworkIds.
- With the outer scalar included, server `shiftOffset(16)` skips scalar, operation, and the first NetworkId; the following U64 is the trade target.
- RTTI xref led to `secure_trade_queue_dispatch_VA_0062CAC0`, whose operation table maps 1=Requested(first ID), 2=no-op, 3=RequestDenied(second ID), 4=RequestDeniedBusy(second ID), and 5=RequestDeniedUnreachable(second ID).
- The true server request path `trade_request_target_VA_005E13F0` constructs operation 0 with invalid first ID and selected target as second ID. Thus the exact C→S typed payload is `{U32 0,U64 0,U64 target}` and no padding bytes remain unknown.

## 2026-09-11 — Server-emitted ObjectController intersection

- Added `tools/server_object_controller_usage.py`, a repeatable direct-constant constructor scanner correlated with the executable's recovered factory registry.
- Found 71 construction sites and 51 distinct constant S→C types; 50/51 have a Stardust.exe registration.
- Isolated a two-ID quest journal mismatch. The executable registers `MessageQueueQuestTaskCounterMessage` as `0x440` and `MessageQueueQuestTaskTimerMessage` as `0x441`; the current server emits counter as `0x441` and timer as `0x444`.
- The associations are backed by RTTI/vtables, class-specific memory managers, factory construction calls, and immediate registration sites `0x00B0F3B6` and `0x00B0EDA6`. This supersedes the weaker inference from names alone.
- Current server code can construct `0x444`, but the Lua tree contains no caller of `Journal.timer()` beyond its definition. Classified the pair as an optional quest-journal protocol mismatch, not a demonstrated scene/login blocker.
- Correlated the registry boolean: all 24 server-accepted/client-originatable types are `1`, while 14 checked S→C-only types are `0`; bidirectional types remain `1`. This strongly identifies a locally/client-originatable capability annotation. A later executable-wide code-and-data reference audit found no direct call or absolute pointer reference to `obj_controller_policy_lookup_VA_00B0D200`; the metadata has no active enforcement effect in this build.

## 2026-09-08 — version token and authentication messages

| String | File offset | VA |
|---|---:|---:|
| `20260817-SD:01` | `0x014AFBAC` | `0x018AFBAC` |
| `LoginClientId` | `0x014AFB58` | `0x018AFB58` |
| RTTI `LoginClientToken` | `0x014AFB98` | `0x018AFB98` |
| `ClientIdMsg` | `0x014B139C` | `0x018B139C` |
| `EnumerateCharacterId` | `0x0149B01C` | `0x0189B01C` |
| `LoginClusterStatus` | `0x0149B034` | `0x0189B034` |
| `LoginEnumCluster` | `0x0149B048` | `0x0189B048` |

### Global version object

Provisional identifier: `stardust_protocol_version_global_01946468`.

Static initializer near `0x00ACAF20` clears a three-pointer string object at `0x01946468`, measures the literal at `0x018AFBAC`, allocates storage, copies it, and terminates it. A destructor guard/thunk at `0x00ACB000` references the same object. **Confirmed:** `20260817-SD:01` is installed into a runtime global string.

### Login message path

Provisional identifier: `login_client_id_message_ctor_VA_00ACA1B0`.

- Builds a class/name string from `LoginClientId` at `0x018AFB58`.
- Builds string-like members at object offsets `+0x38`, `+0x48`, and `+0x58`; the first two originate from function arguments.
- Performs a `SharedNetworkMessages` configuration lookup.
- At `0x00ACA30A`, the fallback third value is global `0x01946468`; it is copied to offset `+0x58` and registered through calls at `0x0118B5F0`.

**Confirmed:** the `LoginClientId` constructor/archive constructor takes two strings supplied from GameNetwork singleton offsets `+0x14` and `+0x20`, registers them at message offsets `+0x38` and `+0x48`, and registers the Stardust version as its third string at `+0x58`. Server `AccountVersionMessage::parse` confirms the first two as username and password and the third as version.

Direct caller `login_connection_opened_sender_VA_0069BC30` obtains two values through `0x00463700` and `0x004636F0`, constructs the message at `0x00ACA1B0`, then passes it with flag `1` to `message_submit_VA_005A8D60`. The surrounding event registration uses `LoginConnectionOpened` at VA `0x0186CDEC`. **Confirmed call path; field semantics of the two getter results remain Strong.**

### Zone authentication path

Provisional identifier: `client_id_msg_ctor_VA_00AE4090`.

- Builds a class/name string from `ClientIdMsg` at `0x018B139C`.
- Builds fields at object offsets `+0x38`, `+0x40`, and `+0x50`, then allocates/copies a caller-supplied byte block at `+0x60`.
- Performs the same `SharedNetworkMessages` lookup.
- At `0x00AE41D7`, fallback global `0x01946468` is copied into the member at `+0x50`.

The constructor registers three serialized fields, in order: the scalar at `+0x38`, the length-delimited byte sequence at `+0x40`, and the version string at `+0x50`. It copies exactly the caller-supplied 16-bit length from the caller-supplied pointer into owned storage at `+0x60`. The fallback version is the installed Stardust protocol version global (`20260817-SD:01`), not the historical `20050408-18:00` literal used by the repository helper.

**Confirmed:** this is the outbound `ClientIdMsg` constructor/archive constructor. Correlation with the server parser identifies the fields as `uint32 gameBits`, a length-prefixed opaque credential blob, and ASCII16 client version.

Direct caller `connection_server_opened_sender_VA_0069B430` obtains the scalar through `0x005035B0`, the credential length through `0x004636E0`, and the credential pointer through `0x004636D0`, constructs the message at `0x00AE4090`, and submits it through `message_submit_VA_005A8D60`. Its event registration uses `ConnectionServerConnectionOpened` at VA `0x0186CDB0`. `login_client_token_handler_VA_00462DD0` previously stores the complete login token byte range at the singleton's `+0x10` pointer and its exact length at `+0x50`; the two getters read those same members. **Confirmed end-to-end credential handoff from `LoginClientToken` to `ClientIdMsg`.** The scalar's public label `gameBits` is confirmed by the server parser; the current runtime value behind global `0x019110EC` still requires observation.

### Zone permissions response

`ClientPermissionsMessage` is dispatched at `0x00898812..0x00898859`, parsed by `client_permissions_parse_VA_00AEC5B0`, and applied by `client_permissions_handler_VA_00899340`.

The executable parser registers exactly three one-byte fields at parsed-object offsets `+0x38`, `+0x40`, and `+0x48`. The handler reads their payload bytes at `+0x3C`, `+0x44`, and `+0x4C`. It uses the first as the connection/login permission notification value and stores the second and third in globals `0x01927869` and `0x0192786A`. **Confirmed:** this Stardust.exe consumes three permission bytes. The server currently transmits four bytes; the fourth (`canSkipTutorial` in the repository test client) is trailing compatible data ignored by this executable path. Semantic labels for the first three are server-correlated: can connect/login, can create a regular character, and can create a Jedi character.

### Selected cluster to zone transport

`login_cluster_status_handler_VA_00898C70` installs each matched endpoint into the cluster record beginning at node `+0x14`. Within that record, the connect path uses:

| Cluster-record offset | Use at connection boundary | Confidence |
|---:|---|---|
| `+0x00` | galaxy ID | Confirmed |
| `+0x04` | cluster name string | Confirmed |
| `+0x10..+0x13` | availability/loading/locked/status bytes used as pre-connect gates | Confirmed mechanics; individual labels source/UI correlated |
| `+0x14` / `+0x18` | population and capacity | Confirmed; selection rejects at/above capacity |
| `+0x28` | resolved network-address object | Confirmed |
| `+0x34` | 16-bit zone port | Confirmed |
| `+0x36` | 16-bit ping port | Confirmed |

The cluster-selection UI path around `0x00C7B2C9..0x00C7B4B7` resolves the requested galaxy through `0x00899670`, rejects unavailable/loading/locked, missing-address, and full-capacity states, checks whether another cluster is already connected, and then calls `cui_login_manager_open_cluster_VA_0089A400` with the selected record. It presents a connecting mediator/message and retains the requested galaxy ID while the operation is pending. **Confirmed control flow; exact localized UI labels are correlated from embedded tokens.**

`cui_login_manager_open_cluster_VA_0089A400` passes the address object at record `+0x28`, the zero-extended port at `+0x34`, and cluster name at `+0x04` to `game_network_open_connection_server_VA_00462060`. It then records the selected galaxy ID in globals `0x01927878` and `0x0192787C`. **Confirmed endpoint handoff.**

`game_network_open_connection_server_VA_00462060` first invokes `game_network_close_connection_server_VA_00462330`, creates a fresh `0x110`-byte `ConnectionServerConnection` through constructor `0x0069B2F0`, stores it at GameNetwork singleton `+0x04`, and copies the cluster name into singleton member `+0x44`. Constructor `0x0069B2F0` forwards address, port, and a default-initialized connection-options object to base constructor `0x005A8960`, initializes lag bookkeeping, and installs the connection-server vtable. **Confirmed fresh-connection lifecycle; no reuse of the login connection.**

When the connection publishes `ConnectionServerConnectionOpened`, GameNetwork dispatcher branch `0x004619B3..0x004619E2` calls `0x00462D10`, which copies pending cluster-name state, logs `Cluster: %s`, performs connection-open setup, and triggers the listener path that reaches `connection_server_opened_sender_VA_0069B430`; that sender emits `ClientIdMsg`. A close request or replacement calls `0x00462330`, disconnects the current connection through `0x012B7270`, clears singleton `+0x04`, and broadcasts connection-state changes. **Confirmed open/close event chain.**

The selection mediators explicitly drop an existing connection when it targets the wrong galaxy and also expose cancel/drop paths. No application-level automatic reconnect policy has yet been proven in this path; lower UDP transport reconnection/timeouts remain a separate analysis target.

### Character selection and scene-start boundary

The avatar-selection path calls `select_character_message_ctor_VA_00AE4B60` at `0x00C62BB8` with the selected avatar's 64-bit identifier object, then sends the message through `game_network_send_VA_00462670` at `0x00C62BCA`. The constructor names the message `SelectCharacter`, creates one registered 64-bit field at object `+0x38` (payload at `+0x40`), and registers no other field. **Confirmed:** Stardust.exe wire output is `LE16(2) | LE32(hash("SelectCharacter")) | LE64(characterObjectId)`. The repository helper's appended `STRING_HASHCODE("SWGEmu")` is not emitted by this executable and is ignored by the server callback, which parses the LE64 at offset 6.

The GameNetwork dispatcher recognizes `CmdStartScene` at `0x00461A82`, creates its typed parser with `cmd_start_scene_parse_VA_00AC9E20`, and calls `game_network_cmd_start_scene_handler_VA_00462E60`. The parsed object registers six fields:

| Parsed object field | Wire interpretation | Confidence |
|---|---|---|
| byte payload at `+0x3C` | initial unknown/scene-reset byte | Confirmed field; semantics Unknown |
| 64-bit payload at `+0x48` | selected player object ID | Confirmed, server-correlated |
| string payload at `+0x54` | terrain path | Confirmed |
| three floats at `+0x64/+0x68/+0x6C` | world X/Z/Y | Confirmed ordering from server constructor |
| string payload at `+0x74` | player client-template path | Confirmed |
| 64-bit payload at `+0x88` | galactic time | Confirmed |

Handler `0x00462E60` first checks GameNetwork singleton scene-entry intent flag `+0x54`; when false it emits `Discarding CmdStartScene message because we don't want to enter a scene right now.` When true, it preserves the player ID, converts galactic time for the scene layer, and passes the terrain, player/template, position, time, and leading byte into `scene_transition_begin_VA_00462AE0`. **Confirmed:** `CmdStartScene` initiates scene/resource setup rather than sending an acknowledgement synchronously inside the packet handler.

The outbound acknowledgement is constructed by `cmd_scene_ready_sender_VA_00462790`. It verifies a connection-server object exists, constructs a no-field `CmdSceneReady` through `cmd_scene_ready_ctor_VA_00AC99F0`, and submits it through `game_network_send_VA_00462670`. The only direct caller is scene lifecycle code at `0x0051B428`, after a conjunction of readiness predicates and immediately before setting a one-shot flag at scene object `+0xA4`. **Confirmed:** the first client `CmdSceneReady` is an asynchronous, once-per-transition readiness acknowledgement. Its wire shape is exactly `LE16(1) | LE32(0x43FD1C22)`.

The same message name also appears in a later inbound scene dispatcher branch at `0x0051C206`; that server-to-client `CmdSceneReady` notifies multiple scene subsystems and sets a ready flag at `+0x95`. This is distinct from the first outbound acknowledgement and corresponds to the server's final scene-ready signal after object/baseline delivery.

### Message serialization/submission

`message_submit_VA_005A8D60` is more than a container insertion:

1. optionally applies a send-throttle/time gate through `0x005A8E40` when its boolean argument is false;
2. queries message metadata through `0x00ACBC70` and records a value at connection offset `+0xF0`;
3. initializes a temporary archive/buffer at `[ebp-0x24]`;
4. calls the message object's virtual function at vtable offset `+8` with that buffer, strongly indicating polymorphic message serialization;
5. passes the serialized buffer and boolean flag to `0x012B7F60` on the connection object;
6. destroys the temporary buffer.

Provisional downstream identifier: `serialized_message_transport_submit_VA_012B7F60`. **Confirmed serialization boundary; whether `0x012B7F60` immediately writes or queues remains unknown.**

### Socket implementations

- `tcp_overlapped_send_VA_012BE540` prepares an overlapped operation and calls the `WSASend` IAT entry at `0x015DC374` from instruction `0x012BE57B`. It accepts a data pointer and length, records operation type `2`, and treats expected asynchronous/socket error codes separately from fatal closure. **Confirmed.**
- `tcp_overlapped_receive_VA_012BE870` prepares operation type `3` and calls `WSARecv` through IAT entry `0x015DC37C` at `0x012BE8A2`. **Confirmed.**
- Buffered TCP flushing occurs through `tcp_flush_send_buffer_VA_012BE5D0`, which passes `[object+0x20]` and length `[object+0x24]` to `0x012BE540`, then resets the buffered length.
- The containing TCP constructor at `0x012BE200` calls `WSASocketA` with `AF_INET` (`2`), `SOCK_STREAM` (`1`), `WSA_FLAG_OVERLAPPED` (`1`) and then `WSAConnect`. A related constructor at `0x012BE040` uses the same stream-socket form. **Confirmed TCP classification.**

The prior provisional association between this TCP implementation and the game-message submit path is withdrawn: no direct xref proves that `0x012B7F60` reaches `0x012BE540`.

The game transport has a separate UDP implementation. `udp_socket_ctor_VA_012BA8D0`, whose RTTI name is `UdpSock`, calls `socket(AF_INET, SOCK_DGRAM, 0)`. More importantly, the larger UDP manager creates the same datagram socket at `0x012C124B`, calls `recvfrom` at `0x012C1D02`, and calls `sendto` at `0x012C20ED`. **Confirmed UDP boundaries.**

`connection_infrastructure_ctor_VA_012B3930` allocates an adapter at object offset `+0x18` using `0x012BCF50`; that constructor derives through `0x012BD480`, whose vtable RTTI identifies `UdpManagerHandlerMT`. This directly ties the `Connection`/`GameNetworkConnection` infrastructure to the UDP manager rather than the TCP socket class.

### Login list dispatcher

Provisional identifier: `login_ui_message_dispatch_VA_008986C0` (entry inferred from prologue immediately before `0x008986C1`). Its message-name pointer table is directly resolved as:

| Global pointer | Message | Parser | Handler |
|---:|---|---:|---:|
| `0x015E7FC4` → `0x0189B048` | `LoginEnumCluster` | `0x00AEDA70` | `0x00898AC0` |
| `0x015E7FC8` → `0x0189B034` | `LoginClusterStatus` | `0x00AEC9D0` | `0x00898C70` |
| `0x015E7FCC` → `0x0189B01C` | `EnumerateCharacterId` | `0x00AE4800` | `0x00898F80` |
| `0x015E7FD0` → `0x0189B000` | `ClientPermissionsMessage` | `0x00AEC5B0` | `0x00899340` |

For every branch the function hashes the pointed-to message name with `0x00AC4D10`, compares it to incoming identifier `[esi+4]`, obtains the archive through `0x00ACB9E0`, creates a typed parsed object, calls its handler, and destroys the temporary. **Confirmed direct Stardust dispatch pattern.**

`login_enum_cluster_handler_VA_00898AC0` clears two existing global cluster containers, iterates fixed-size `0x14`-byte parsed records from message offsets `+0x3C..+0x40`, creates/updates cluster entries keyed by the first record field, stores the associated name/string data, and finally signals a state object at `0x019277D4` with value `1`. **Confirmed structure mutation; UI/state class names pending.**

`login_cluster_status_handler_VA_00898C70` iterates `0x2C`-byte status records, matches them to previously enumerated clusters, and updates availability booleans, address, two 16-bit ports, population/capacity-like 32-bit values, and status fields. It also creates fallback entries for unmatched records. **Confirmed data movement; semantic labels partly corroborated by server source.**

`enumerate_character_id_handler_VA_00898F80` clears/rebuilds character-selection state, iterates parsed character records, resolves or creates associated entries, copies the Unicode/name value and identifiers, associates characters with server/cluster context, and updates selection bookkeeping. **Confirmed subsystem effect; detailed member offsets still in progress.**

### Parsed login record layouts

These are Stardust in-memory parsed-record layouts, not wire structs to copy with native packing.

`LoginEnumCluster` records have stride `0x14`:

| Offset | Size/internal form | Interpretation | Confidence |
|---:|---|---|---|
| `+0x00` | 4 | galaxy/server ID | Confirmed by handler keying; source-correlated |
| `+0x04` | 12-byte string object | galaxy name | Confirmed by string lifecycle and handler copy |
| `+0x10` | 4 | server flags/status value; current server emits `0xFFFF8F80` | Confirmed wire value; public semantics Unknown |

`LoginClusterStatus` records have stride `0x2C`:

| Offset | Size/internal form | Interpretation | Confidence |
|---:|---|---|---|
| `+0x00` | 4 | galaxy/server ID | Confirmed |
| `+0x04` | 12-byte string object | address/hostname | Confirmed |
| `+0x10` | 2 | zone port | Confirmed by 16-bit setter path; source-correlated |
| `+0x12` | 2 | ping port | Confirmed by 16-bit setter path; source-correlated |
| `+0x14` | 4 | population; current server emits `100` | Confirmed label from server construction |
| `+0x18` | 4 | maximum capacity; current server emits `0xCB2` | Confirmed label from server/test-client correlation |
| `+0x1C` | 4 | distance/category field; current server emits `8` | Confirmed value; precise semantics Strong |
| `+0x20` | 4 | server status; current server emits `2` | Confirmed label and handler use |
| `+0x24` | 4 | recommended/auxiliary field; current server emits `0xFFFF8F80` | Confirmed value; precise semantics Unknown |
| `+0x28` | 1 + padding | trailing flag/unknown byte | Confirmed width; semantics Unknown |

`EnumerateCharacterId` records have stride `0x20`:

| Offset | Size/internal form | Interpretation | Confidence |
|---:|---|---|---|
| `+0x00` | 12-byte UTF-16/vector object | character name | Confirmed type behavior; source-correlated |
| `+0x0C` | 4 | race/template CRC | Confirmed by server construction |
| `+0x10` | 8 | object ID | Confirmed width and handler usage |
| `+0x18` | 4 | galaxy/server ID | Confirmed by server construction and association lookup |
| `+0x1C` | 4 | character type (`1` regular, `2` historical pre-Pub9 Jedi) | Confirmed server semantics |

The character record constructor at `0x00AE4A60` accepts five serialized values and places them at these offsets. The 64-bit object ID is already an 8-byte wire/domain identifier inside the 32-bit client; a future x64 implementation must preserve that wire width without serializing native pointers.

`login_cluster_status_handler_VA_00898C70` now establishes the endpoint handoff. For a matched nonzero-status record it sets three availability booleans from status comparisons, copies the trailing byte, hostname, zone port, ping port, population, capacity and status fields into the cluster object, then constructs a network-address value from hostname plus the **zone port** and associates that address key with the galaxy ID. Ping port remains separate. Records with unknown galaxy IDs take the diagnostic/fallback path. This is **Confirmed executable behavior**; later UI selection consumes the resulting cluster/address association, although the final click-to-connect caller is not yet named.

The server constructors close the remaining wire layouts. `LoginEnumCluster` is `LE32 count`, repeated `LE32 galaxyID | ASCII16 name | LE32 flags`, followed by a `LE32` footer currently equal to 8. `LoginClusterStatus` repeats `LE32 galaxyID | ASCII16 hostname | LE16 zonePort | LE16 pingPort | five LE32 values | U8 trailingFlag`. `EnumerateCharacterId` repeats `Unicode32 name | LE32 raceCRC | LE64 objectID | LE32 galaxyID | LE32 characterType`. These are serialized field streams, not packed copies of the executable's 0x14/0x2C/0x20 in-memory records.

### Receive frame reassembly

After the UDP manager delivers logical connection bytes, `tcp_receive_frame_reassembler_VA_012B7840` processes a byte span:

1. accumulates exactly four bytes into an object at connection offset `+0xAC`;
2. extracts the expected payload length and compares it with the payload buffer state at `+0xB0`;
3. accumulates partial data across completions;
4. when complete, calls `received_frame_dispatch_VA_012B4120` with the connection, payload pointer, and payload length;
5. resets header/payload lengths and continues if the completion contained more frames.

This is **Confirmed four-byte length-prefixed framing in the game Connection layer**. The direct `UdpManagerHandlerMT` ownership makes it **Strong** that this is logical-message framing above the SOE/UDP datagram transport, not the datagram header itself. Exact adapter callbacks between UDP reassembly and this function remain to be assigned VAs.

`received_frame_dispatch_VA_012B4120` allocates/reuses a `0x20`-byte work item from a global pool, attaches the connection with reference-management calls, copies the completed payload into the work item, and routes it into an asynchronous processing/logging path. It does not itself decode a named SWG message. **Confirmed queue handoff.** The next protocol boundary is the consumer of this pooled work item.

### Connection callback tables and event dispatch

Constructor analysis resolves two vptrs in each connection object. The primary vptr is stored at object offset `+0x00`: ConnectionServer uses `0x015E114C`, and Login uses `0x015E11C0`. A secondary-interface vptr is stored at object offset `+0xC8`: ConnectionServer uses `0x015E1144`, and Login uses `0x015E11B8`. Entries in the secondary tables include `this`-adjustor thunks (for example `0x0069BB30` subtracts `0xC8`), which confirms the multiple-inheritance/subobject interpretation. The earlier table description used the secondary addresses as though they were the primary tables; the corrected primary slots follow.

Important slots observed directly:

| Vtable slot | ConnectionServer | Login | Interpretation | Confidence |
|---:|---:|---:|---|---|
| `+0x10` | `0x0069B3D0` | `0x0069BBD0` | connection-closed callback | Strong; nearby event names and symmetric layout |
| `+0x14` | `0x0069B430` | `0x0069BC30` | connection-opened callback and authentication sender | Confirmed |
| `+0x18` | `0x0069B500` | `0x0069BCF0` | connection-overflow callback | Confirmed by constructed event name |
| `+0x28` | `0x0069B510` | `0x005A8C50` | received named-message/event callback | Confirmed call path |
| `+0x30` | `0x012B8C60` | `0x012B8C60` | zero-argument Boolean transport policy query | Confirmed signature; exact policy unknown |

`connection_overflow_event_VA_005A8BF0` constructs an engine message using the literal `ConnectionOverflowing` at VA `0x0187CA24`, stores the callback argument in the event object, and dispatches it through the listener/event map at `0x00AC2AB0`. This corrects the earlier working hypothesis that slot `+0x20` carried ordinary packet payloads: it is an overflow notification.

`connection_event_dispatch_VA_00AC2AB0` uses the event/message key at argument offset `+4` to find a listener collection, iterates listeners and invokes virtual callback slot `+4`, then forwards the event to a global dispatcher at `0x0194642C` via `0x00AC5E80`. **Confirmed observer/event-bus behavior; concrete class names remain provisional.**

`connection_server_named_message_VA_0069B510` is the stronger candidate for normal post-decode message delivery. It reads the incoming message identifier, recognizes three literal message names by the standard hash routine, updates connection lag state, and then forwards every message to `connection_message_dispatch_VA_005A8C50`:

- `LagRequest` (`0x018879E0`)
- `GameServerLagResponse` (`0x018879EC`)
- `ConnectionServerLagResponse` (`0x01887A04`)

The lag-response branches timestamp two independently tracked response values at connection offsets `+0xFC` and `+0x100`. Once both replies arrive, `0x0069B690` sends a generic pair-valued `LagReport`; diagnostic text identifies the values as `ConnectionServer(%s)` and `GameServer`. `LagRequest` participates in the periodic request/state logic at `0x0069B8D0`.

The lower callback link is now confirmed. `connection_received_message_VA_012B79B0` accepts the completed message object, optionally invokes primary vtable slot `+0x18` when overflow reporting is enabled, performs byte/rate accounting, and finally invokes primary vtable slot `+0x28` with the same message argument. On ConnectionServer that resolves to `0x0069B510`; on Login it resolves to `0x005A8C50`. Therefore the direct chain is:

`0x012B7840` frame reassembly -> `0x012B4120` pooled work item -> worker `0x012B3B80` -> `0x012B79B0` -> primary vtable `+0x28` -> connection-specific named-message handler -> `0x005A8C50` -> keyed event dispatch `0x00AC2AB0`.

What remains unresolved is not the callback invocation but where the raw completed frame becomes the message object whose identifier is at offset `+4` and archive/payload metadata begins at offset `+8`.

### Application-message envelope construction

That remaining conversion is performed by `game_network_message_from_stream_VA_00ACB790`, called inside `connection_message_dispatch_VA_005A8C50`.

The direct sequence is:

1. `0x005A8C50` snapshots the incoming byte-stream read state from stream offset `+0x08`.
2. It constructs a temporary outer message at `[ebp-0x44]` with `0x00ACB790`.
3. It passes the embedded message/event interface at outer offset `+0x10` (`[ebp-0x34]`) to `0x00AC2AB0`.
4. Typed handlers obtain the remaining archive using `0x00ACB9E0`, which simply returns outer-message offset `+0x18`.

`0x00ACB790` creates an archive/read iterator from the received stream and stores it at outer offset `+0x18`. It registers one serializable member at outer offset `+0x30`, invokes the archive deserializer at `0x0118B740`, then copies the parsed 32-bit value at `+0x34` into the embedded interface's key at outer offset `+0x14` (interface-relative `+0x04`).

`0x0118B740` first reads a two-byte value from the archive and then invokes each registered member's deserialize virtual method. With the server constructors as corroboration, the two-byte value is the application operand/field count and the registered 32-bit value is the SWG message opcode/hash. Thus the envelope is:

`uint16 operand_count` -> `uint32 message_opcode` -> message-specific fields remaining in the archive.

The byte widths, parsing sequence, key propagation, and remaining-archive handoff are **Confirmed by Stardust.exe**. The semantic names `operand_count` and `message_opcode` are **Strong**, corroborated by the Stardust server constructors and the subsequent string-hash comparisons. This is the direct executable bridge from received bytes to the opcode-keyed handler system.

### Inbound LoginClientToken dispatch

Provisional dispatcher: `game_network_message_dispatch_VA_00461xxx`; exact start pending.

At `0x00461A17`, the dispatcher hashes `LoginClientToken` at VA `0x0186CD78` with `0x00AC4D10`, compares it with the incoming message identifier at `[esi+4]`, and on equality:

1. obtains the message archive/payload through `0x00ACB9E0`;
2. constructs/parses a typed token object with `login_client_token_parse_VA_00ACA8F0`;
3. calls `login_client_token_handler_VA_00462DD0` on the GameNetwork listener singleton at `0x0190A298`;
4. destroys the temporary parsed object.

The parser registers three serialized members: a byte-vector-like member at `+0x38`, a 32-bit member at `+0x4C/+0x50`, and a string/vector-like member at `+0x54`. After archive parsing it copies the first vector into separately allocated storage at `+0x48`.

The handler copies the token bytes into persistent listener storage (`listener +0x10`), records the token length at `+0x50`, copies the parsed 32-bit member to `+0x58`, passes the third parsed member to `0x00462800` for storage on the singleton, logs the result, and clears byte flag `0x01909417`. **Confirmed data movement; account/station/username labels are not yet assigned to the trailing fields.**

### Conclusion

Wire use of `20260817-SD:01` is **strongly inferred from direct Stardust.exe xrefs** in both login and zone-authentication construction. It is not based on another executable. Exact serialization order and the outer send call remain to be recovered before marking wire output confirmed.

### UDP manager and SOE opcode dispatcher

The actual game transport has now been followed through the UDP receive side. `udp_manager_receive_VA_012C1CA0` calls `recvfrom` at `0x012C1D02`, rotates its receive buffers, and preserves the source IPv4 address and network-to-host converted port. It feeds `udp_manager_packet_dispatch_VA_012C2190`, which treats management/control packets separately from packets belonging to an established peer.

For an unknown peer, `00 01` is accepted only at length `0x0E` and only when the big-endian 32-bit value at bytes 2-5 equals 2; the manager then allocates a `0x340`-byte connection object at `0x012C2880`. This is **Confirmed behavior** and a **Strong session-request/version interpretation**. `00 1E` carries two 32-bit identifiers and can update/migrate a connection mapping when the relevant option is enabled. `00 1D` is also manager-special. Exact names for `1D` and `1E` remain unknown.

Established peers enter `udp_connection_packet_receive_VA_012C36B0`. It performs accounting, ignores `00 06` ping early, validates a configurable one-to-four-byte big-endian trailer using `0x012C8D30` and the value stored at connection offset `+0x11C`, then invokes up to two configured receive transforms before entering `soe_opcode_dispatch_VA_012C3B60`. Session/control opcodes `01`, `02`, `1D`, and `1E` bypass that normal validation/transform path. The trailer/checksum behavior is **Confirmed**; the exact checksum and transform algorithm names are not yet assigned.

The dispatch table and branch bodies directly establish:

- `01` session request -> `0x012C3BCB`; parses identifiers and emits `00 02`.
- `02` session response -> `0x012C3D0E`; stores connection ID, seed and negotiated bytes at `+0x11C`, transitions to connected, invokes callback.
- `03` Multi-SOE -> `0x012C3FB5`; recursively dispatches one-byte-length-prefixed embedded packets.
- `05` disconnect -> `0x012C3E9D`; validates ID and closes.
- `07` net-status request -> `0x012C3FF7`; parses request statistics and emits `00 08`.
- `08` net-status response -> `0x012C446F`; parses and records timing/statistical data.
- `09`-`10` -> `0x012C484D`; this combined range covers four data variants (`09`-`0C`) and four fragment variants (`0D`-`10`), maps each to one of four channel indices, lazily allocates receiver state, then calls `0x012C7CF0`.
- `11`-`14` -> `0x012C48D2`; four out-of-order variants, 16-bit-to-logical sequence expansion, then `0x012C82C0`.
- `15`-`18` -> `0x012C4921`; four acknowledgement variants routed to `0x012C8210`.
- `19` -> `0x012C493E`; walks variable-length embedded application-message blocks and recursively dispatches their contents.
- `1D` -> `0x012C3EEC`; liveness/mapping control, including construction of `00 1E` with two IDs.

The server's base opcode names corroborate data (`09`), fragment (`0D`), out-of-order (`11`), acknowledgement (`15`), and multipayload (`19`). The executable adds an important architectural fact: each reliable operation has four channel variants. This must be retained in the replacement transport model even if Stardust currently uses only the base channel.

The channel receiver at `0x012C7CF0` independently confirms sequence and fragment behavior. It expands bytes 2-3 from network-order 16-bit sequence into a wider logical counter. An exact expected packet is delivered through `0x012C80E0`, the counter is incremented, and consecutive buffered packets are drained. Future packets are copied into a sequence-indexed ring/window; duplicate/occupied/old cases increment error statistics. It constructs `00 (15 + channel) sequence` for acknowledgement or `00 (11 + channel) sequence` for an out-of-order indication. These opcode roles are therefore now **Confirmed by Stardust.exe**, not merely source-correlated.

`0x012C80E0` dispatches ordinary data directly. In fragment form it reads the first fragment's big-endian 32-bit total, allocates an assembly buffer, appends bounded chunks, and recursively dispatches the completed buffer before freeing and resetting the assembly state. `0x012C8210` parses a received acknowledgement, expands its sequence, and cumulatively calls `0x012C82C0` to release retained outgoing entries. The latter also updates adaptive timing/statistics.

The fragment-total limit question is now closed negatively. `0x012C80E0` allocates the peer-declared U32 total directly with no upper bound and reads that U32 before checking that the first fragment has four payload bytes. `0x012C6650` constrains per-datagram/window sizes only; it does not cap total reassembly. Overlong continuation data is detected by comparing copied minimum bytes with supplied bytes and throws, but only after the declared allocation is live. This is a legacy allocation-exhaustion/out-of-bounds-read boundary that the new transport must replace with pre-allocation length and cap validation.

The apparent out-of-order resend ambiguity is now resolved. Branch `0x012C48D2` expands the `00 11` sequence and calls `0x012C82C0` exactly once. Since that callee releases and clears the named retained entry, the executable treats the future sequence carried by `00 11` as a selective acknowledgement. It does **not** directly retransmit from this control handler. Missing lower sequences remain retained and are handled by the timer-driven `0x012C7530` path. This differs from the Stardust source-shaped path `doOutOfOrder -> resendPackets(seq)`; the current `resendPackets(int)` is disabled by an unconditional first-statement return.

### Outbound application-to-UDP path

`serialized_message_transport_submit_VA_012B7F60` is now confirmed as the connection-layer submission boundary. **Correction:** its second argument does not add a four-byte logical-length prefix. It maps false/true to delivery modes 0/4; the true path calls `0x012B7800` only to check pending capacity using the serialized payload length. The active UDP adapter call is `0x012BD9D0`, which forwards the original buffer and size plus mode 4 to `0x012C3020`. The four-byte reassembler `0x012B7840` has a sole direct caller at `0x012BE80B` adjacent to overlapped `WSARecv`, proving that framing belongs to a distinct TCP/stream subsystem.

`0x012C3020` validates connection state and forwards buffer spans to `soe_send_dispatch_VA_012C3180`. Numeric modes are now mapped: 0 aggregate, 1 immediate, 2 internal `00 1B` plus aggregate, 3 internal `00 1B` plus immediate, and 4-7 reliable channels 0-3 through `0x012C6A40`; oversized 0-3 traffic promotes to mode 4. Public enum names for 0-3 remain Unknown.

`datagram_prepare_VA_012C5980` provides the confirmed final wire order. It preserves one opcode byte, or two bytes for leading-zero SOE opcodes, passes the remainder through up to two configured callbacks, computes `0x012C8D30` over the transformed packet with the seed at `+0x11C`, appends the configured one-to-four-byte checksum in big-endian order, and calls `udp_connection_raw_send_VA_012C54B0`. That routine records counters/timestamps and invokes manager raw-send `0x012C1F20`; the system call is `sendto` at `0x012C20ED`.

Transform installation at `0x012C63C0` selects callback pairs from negotiated mode values. `0x012C6240`, `0x012C62B0`, `0x012C6320`, and `0x012C6370` visibly implement seed/key-stream XOR word and tail-byte transforms. Their exact names and whether each mode represents the server's historical encryption toggle are not yet promoted beyond tentative correlation.

`0x012C8D30` is now identified as the seeded SOE CRC-32 routine. Its table lookups, four-byte seed initialization, per-byte recurrence, and final complement match `BaseProtocol::generateCrc` in the Stardust server source. The sender serializes the selected high-order CRC bytes in big-endian order; the receiver reconstructs the same one-to-four-byte value for comparison. **Confirmed by both sides.**

Reliable sender state retains prepared transport entries until `0x012C8210`/`0x012C82C0` processes cumulative acknowledgement. This establishes that the future transport design must keep immutable prepared datagrams or equivalent exact resend state separate from mutable application messages.

### Replication view storage and count-tolerance behavior

The common client-object constructor `0x00554240` constructs six RTTI-confirmed `Archive::AutoDeltaByteStream` members with vtable `0x015FE388`. The baseline/delta view switches map view 1 to object `+0xA4`, view 3 to `+0x124`, view 4 to `+0xC4`, view 6 to `+0x144`, view 8 to `+0xE4`, view 9 to `+0x104`, views 2/5 to no fixed stream, and view 7 to a dynamic UI-package pointer at `+0x164`. Dynamic wrapper constructor `0x006F24D0` embeds another byte stream at wrapper `+0x08`.

`autodelta_byte_stream_snapshot_decode_VA_0118B740` consumes the baseline's U16 declared variable count but does not use it as the decode loop bound; it iterates every registered per-field decoder. `autodelta_byte_stream_delta_decode_VA_0118AA00` also consumes but does not use the U16 declared update count; it instead loops to bounded payload exhaustion, reading a U16 field index for each update and indexing the registered decoder array without an explicit local bounds check. A clean implementation should preserve characterized count tolerance but reject out-of-range delta indices before state mutation.

### Reliable sender, fragmentation, and retransmission

`reliable_channel_ctor_VA_012C6650` constructs each of four channel objects and derives usable size from negotiated datagram limit `+0x12C` minus transform overhead `+0x194`, checksum width `+0x120`, and the four-byte SOE reliable header. A fragmentation option reduces the direct threshold by another five bytes. Packet limits are therefore configuration-derived in Stardust.exe.

`reliable_channel_submit_VA_012C6A40` sends fitting payloads to `0x012C7210`, which aggregates them into `00 19` multipayload packets with variable-length size prefixes. Oversized payloads flush aggregation, are copied into owned storage, and enter the linked send queue through `0x012C7340`.

`0x012C8E60` and `0x012C8EC0` are confirmed inverse size codecs: one byte below `0xFE`, `FF` plus big-endian 16-bit below `0xFFFF`, otherwise `FF FF FF` plus big-endian 32-bit.

`reliable_sender_pump_VA_012C73B0` consumes queued messages subject to configured outstanding/window limits. It stores `0x20`-byte sequence entries, writes the low 16 bits in network order, uses `09 + channel` for data and `0D + channel` for fragments, and includes the original total length in big-endian order in the first fragment. Entries retain prepared packet references and timing/accounting state.

`reliable_resend_check_VA_012C7530` scans outstanding sequences, checks timestamps against adaptive delay fields, resends eligible prepared packets, updates counters, and computes the next wake time. A configured excessive-delay path disconnects with internal reason 8. The constructor initializes one adaptive field to 800, but its exact name/unit remains provisional. Server `BaseClient.cpp` confirms the same overall architecture; its numerical thresholds are not automatically attributed to the client.

Resend inspection further confirms that expired retained entries keep their logical sequence. The routine reconstructs opcode `0x09 + channel` or `0x0D + channel`, writes the existing low 16-bit sequence in network order, includes fragment-total metadata where required, submits through `0x012C5B60`, and refreshes the retained-entry timestamp. Direct callers at `0x012C4D9C`, `0x012C6DCC`, and `0x012C7146` place the check in normal connection/channel pumping. Thus selective `00 11` release and timeout retransmission are separate mechanisms.

### Transform selector table completed

The complete jump table in `transport_transform_install_VA_012C63C0` maps selectors 0–4 for each of the two negotiated transform slots. Mode 0 is identity copy; mode 1 delegates to the shared compressor's first method pair; mode 2 delegates to its second pair; mode 3 combines the seed with a generated byte stream; mode 4 is the seed-only chaining transform used by Stardust. The deployed response `1,4` therefore has a fully address-correlated implementation path. Values greater than 4 take no installation branch. Public names and intended deployment cases for modes 2 and 3 remain unknown.

### Additional ground-scene and space-message routes

The main `GroundScene` network listener contains further direct string-hash branches now recorded in `PACKET_HANDLER_MAP.md`: `ClientOpenContainerMessage` (`0x0051CBC8` / parser `0x00AD9B70`), `LogoutMessage` (`0x0051CCF6`), `CreateProjectileMessage` (`0x0051CFED` / parser `0x00AD7830`), `CreateMissileMessage` (`0x0051D2AD` / parser `0x00AD72D0`), `UpdateMissileMessage` (`0x0051D65C` / parser `0x00AD6EF0`), `ShipUpdateTransformMessage` (`0x0051D783` / parser `0x00AD6930`), and `ShipUpdateTransformCollisionMessage` (`0x0051DAC8` / parser `0x00AD6190`). Current server constructors fix their wire field orders. Client control flow additionally proves missile-result four-way dispatch, ship-controller delivery, and the local-player-root guard on collision corrections.

The same listener sweep also recovered `UpdateCellPermissionMessage` (`0x0051C287` / parser `0x00ADC3A0`), `ServerTimeMessage` (`0x0051C41E` / parser `0x00ADC0F0`), `PlayMusicMessage` (`0x0051CB49` / parser `0x00AD9F40`), `HyperspaceMessage` (`0x0051DFF0` / parser `0x00AD5EE0`), `IsFlattenedTheaterMessage` (`0x0051E071` / parser `0x00520C20`), and `LaunchBrowserMessage` (`0x0051E142` / parser `0x00520E80`). `AiDebugString` and `SaveTextOnClient` are recognized but intentionally ignored in this build. The browser route passes the parsed server-supplied string into a platform-launch function, so a replacement should require explicit URL-scheme allowlisting and user confirmation rather than copying that trust boundary blindly.

The `ServerTimeMessage` route is now closed through GroundEnvironment. Stardust sends elapsed Zone lifetime in seconds as LE64 every 60 seconds and includes the same value in `CmdStartScene`. Client branch `0x0051C41E` parses the full value, computes its remainder within the terrain PTAT cycle duration, and calls normalized-time setter `0x00B5CBD0`/`0x00846BF0`. GroundEnvironment update `0x008479B0` advances cycle seconds by frame delta unless `disableTimeOfDay` is set, wraps at duration, derives normalized phase and updates environment interpolation. A separate scene-clock compatibility offset truncates the server value to low32. The effective Tatooine PTAT/0014 asset supplies `11280.0` seconds rather than the constructor's `1800.0` default.

The `PlayMusicMessage` route is now closed through audio submission. Parser `0x00AD9F40` registers the server's ASCII16 path, U64 target, 32-bit selector, and trailing byte in matching wire order. The listener reads the path and selector only: selector zero calls `0x006C9300`, while any nonzero selector calls `0x006C93B0`. These are equivalent policies backed by separate channel holders (`0x0191B770` and `0x0191B980`): an existing channel sound is stopped/faded through `0x0040F360` with scalar `1.0`, then the new path is submitted through ordinary audio entry `0x0040D990` and its AudioId is retained. The parsed target and trailing flag are unused in this branch. Because this reaches the common `.snd` runtime without a category override, `PlayMusicMessage` does not provide a category-13 bypass. Representative server-used music/tutorial/ambience descriptors occupy categories 8, 4, 2, and 10; the 558 category-13 files remain a separate, apparently legacy dialogue corpus whose packed-content reachability and intended gain policy are unresolved.

Configured-overlay reference scanning covered 203,617 effective TRE records. Only two category-13 paths appeared, both as `musicOnActivate` values (`sound/han_94.snd` and `sound/han_95.snd`) inside `quest/npe_new_main_quest.qst`. This executable has no `.qst` format literal and instead constructs runtime quest-task paths beneath `datatables/questtask/` with `.iff`; 791 compiled quest-task tables exist but no NPE family and neither Han path occurs in compiled payloads. The two references are therefore strongly an editor/source artifact rather than demonstrated runtime content. Dynamic server emission and non-string references remain possible, so category 13 is not declared unreachable.

`GameMusicManager` loader `0x006C8920` consumes `scene/game_music_manager.iff` as `GMUS/0000`. ENTR, EXIT, DETH, INCP, SENT and SEXI chunks populate vectors; WATR populates a singleton. Update `0x006C9450` maps ground combat entry/exit, posture 13 incapacitation, posture 14 death, space-combat entry/exit, and day/night transitions into a nine-way state table. Space SENT/SEXI lists are positionally paired: the entry selection's random index is retained for the corresponding exit cue. Selector-zero `PlayMusicMessage` and local `SpaceDeath` use the principal music holder as a temporary override; while its flag is set the manager waits for playback completion, clears the override, and resumes normal state selection later. The server's eight C++ construction sites all use default selector 1 and therefore its separate event channel.

The environment layer is data-table driven. `datatables/environment/<planet>.iff` explicitly provides Day Ambient Sound 1/2, Night Ambient Sound 1/2, First Music, Sunrise Music and Sunset Music, matching runtime slots `+0x4148..+0x4160`. Containment helper `0x00B22BE0` identifies world-cell versus interior-cell placement; outdoor environment families contribute both ambient layers, whereas the smaller interior override contributes ambient layer 1 and first music. Separate visited-object sets make First Music a once-per-encounter cue. GMUS WATR is loaded at `0x0191B4F4` and released by remove routine `0x006C8FC0`, but has no other reference; it is not an active playback route in this executable.

The smaller override is now identified by the executable's original `InteriorEnvironmentBlockManager::remove` diagnostic. Install/load routines `0x00791E30`/`0x007922A0` consume effective `datatables/interior/interior.iff` (`DTII/0001`, 13 columns). Lookup `0x007920F0` tries exact `PobName_CellName`, `PobName_default`, then `default_default`. The 52-byte record supplies day sound at `+4`, night sound at `+8`, first music at `+0xC`, surface type, fog enable/density/color, environment map and room type. GroundScene consumes fog/environment-map fields and surface resolution consumes the surface field; room-type application has no direct accessor caller in this executable.

`ServerWeatherMessage` (`0x486356EA`) is dispatched at `0x0051C4E9`, parsed at `0x00AD3F90` as S32 weather plus a three-float wind vector, and delivered to `0x00844AA0` (normalize/store/publish wind) and `0x00845C90` (store GroundEnvironment weather index). Update `0x008479B0` supplies player position and weather index to environment-family selector `0x0085D7F0`. GameMusicManager then observes the selected row and cross-fades changed ambient paths; the message has no direct weather-sound or GMUS-WATR playback route.

Selector `0x0085D7F0` is now fully resolved. Terrain query `0x0084ADF0`/ClientChunk query `0x008628E0` samples byte grid `+0xD4` and resolves a PTAT environment family. The selector searches `(familyId<<16)|weather`, retries the same family with weather zero when necessary, and otherwise returns an empty default descriptor. Loader `0x0085D880` validates family names against PTAT and reads all 25 DTII columns. Descriptor changes at `0x008485DE` retain previous/current records and drive a confirmed five-second interpolation through `0x00846D00`. The configured corpus has 50 valid effective environment tables, 1,337 rows and 290 family groups; every family supplies weather zero. Tatooine is a complete 17×5 matrix.

### Session negotiation fields and initial fixtures

The `00 02` branch at `0x012C3D0E` has now been resolved field-by-field. It requires the response's connection ID at offsets 2-5 to equal the connection's locally stored ID, then reads a big-endian seed at 6-9, three unsigned selector/configuration bytes at 10-12, and a big-endian 32-bit value at 13-16. These are stored contiguously as DWORD fields at connection `+0x11C`, `+0x120`, `+0x124`, `+0x128`, and `+0x12C` before transform installation and the connected transition.

Downstream executable use confirms `+0x120` as the CRC trailer width and `+0x12C` as maximum datagram size. `0x012C63C0` uses the middle two fields as transform selectors. Stardust server `SessionIDResponseMessage` emits `02 01 04 00 00 01 F0`, establishing the deployed settings as CRC width 2, selector 1, selector 4, and 496-byte datagram. Mode 1 delegates to a provider interface and is strongly correlated with compression; mode 4 installs the seed-chained XOR pair and is strongly correlated with the server's historical transport encryption setting.

The reverse-direction session request is also bounded: the executable manager requires a 14-byte `00 01` packet, a big-endian value 2 at offsets 2-5, connection ID at 6-9, and requested datagram size at 10-13. The server's checked-in `SessionIDRequestMessage` constructor builds a different legacy 10-byte packet, while its receive parser reads only the connection ID at offset 6. This is recorded as source drift; the executable's validated 14-byte layout is authoritative for a replacement-client handshake.

Initial deterministic fixtures are in `SOE_TRANSPORT_TEST_VECTORS.md`, including request/response examples, seeded CRC output, and all variable-length size boundaries. They are analysis vectors only; complete data datagrams still require exact mode-1 and mode-4 transform fixtures.

The mode-4 primitive has subsequently been reduced to a deterministic word-and-tail vector. With seed `0x12345678`, preserved header `00 09`, and body `00 01 10 20 30 40 50`, the output body is `78 57 24 32 48 38 28`. Its full seeded CRC is `0x64AEB373`, yielding deployed-width trailer `B3 73`. This confirms the little-endian 32-bit ciphertext-chain behavior and the low-chain-byte tail rule on both client and server. It remains a transform primitive rather than a complete deployed packet until mode-1 framing is resolved.

### Selector-1 compression provider

Selector 1 is no longer merely a semantic correlation. The executable contains MSVC RTTI for `Compressor` and `ZlibCompressor`, installation/removal strings for `SetupSharedCompression` and `ZlibCompressorNamespace`, and embedded zlib version string `1.1.4`. The selector callbacks at `0x012C6180`/`0x012C61B0` dereference the compressor provider stored through the connection's `NetworkSetupData` and invoke its adjacent virtual operations.

The concrete `ZlibCompressor` vtable leads to `0x00A88B20` and `0x00A88BE0`. Both construct a `0x38`-byte zlib stream structure with custom pooled allocation/free callbacks at `0x00A886D0` and `0x00A88740`. The first calls the statically linked deflate path (`0x01394B90`, run at `0x01395010`, finalization at `0x01395370`); the second calls the corresponding inflate path (`0x01396400`, run at `0x01396420`, finalization at `0x013962A0`). They return the produced byte count on success and `-1` on setup, run, or finalization failure. This establishes selector 1 as **Confirmed zlib compression/decompression**, not a tentative label.

Server `BaseProtocol::compress` corroborates the surrounding packet framing: it excludes the preserved opcode prefix and final three bytes from the zlib input, retains compression only when it reduces the packet after framing, and writes a one-byte compression marker immediately before the two CRC bytes. A retained stream uses marker `1`; a non-beneficial stream leaves the body uncompressed and changes the marker to `0`. The receive order is CRC validation, decryption, marker inspection, optional zlib decompression, then removal of the marker and CRC bytes.

The remaining blocker for a byte-exact compressed fixture is codec-version output. The executable identifies zlib 1.1.4, but no external zlib DLL is imported because the implementation is statically linked. Modern zlib output may be semantically compatible without being byte-identical. A golden compressed vector should therefore come from the embedded codec under controlled execution or a server-linked harness, not from an assumed modern encoder.

The uncompressed selector-1 case is sufficient to close the first complete established-datagram fixture. For seed `0x12345678`, channel-0 data opcode `00 09`, sequence `0000`, synthetic payload `AA BB`, and compression marker `00`, pre-transform bytes are `00 09 00 00 AA BB 00`. Selector 4 produces `00 09 78 56 9E A9 78`; the two-byte seeded CRC is `92 31`, giving complete UDP payload `00 09 78 56 9E A9 78 92 31`. This is confirmed at the transport level but intentionally does not claim `AA BB` is a valid application message.

## Next address-backed work

### Outbound LoginClientId resolved

`login_connection_opened_sender_VA_0069BC30` is the direct first-message path. After its base connection-open handling, it obtains references into the GameNetwork singleton through `0x004636F0` (`singleton +0x14`) and `0x00463700` (`singleton +0x20`), passes those two strings to `login_client_id_message_ctor_VA_00ACA1B0`, and submits the completed object through `message_submit_VA_005A8D60` with framing argument 1.

The constructor creates the named message `LoginClientId`, registers the first argument at object `+0x38`, the second at `+0x48`, and a version string at `+0x58`. The version defaults to the global initialized from literal `20260817-SD:01`; a `SharedNetworkMessages` setting can override it and emits diagnostic `Network version override detected: %s`. Stardust server `AccountVersionMessage::parse` independently fixes the semantic field order as username, password, version. This makes all three labels and their ordering **Confirmed**.

The serialized application shape is `LE16(4) | LE32(0x41131F96) | ASCII16(username) | ASCII16(password) | ASCII16(version)`, where ASCII16 is a little-endian 16-bit byte length followed by bytes without a terminator. The executable class registers only these three fields. The server helper constructor's trailing `STRING_HASHCODE("SWGEmu")` is legacy/convenience output ignored by its parser and must not be attributed to `Stardust.exe`.

A credential-free vector using username `u`, password `p`, seed `0x12345678`, sequence 0, compression marker 0, and CRC width 2 is now in `SOE_TRANSPORT_TEST_VECTORS.md`. Its complete UDP payload ends in CRC trailer `F7 04`.

### First login response and acknowledgement

The executable's `LoginClientToken` archive parser at `0x00ACA8F0` registers three members: a length-delimited byte array at `+0x38`, a 32-bit scalar at `+0x4C`, and a string at `+0x54`. After parsing, it copies the complete byte array into owned storage at `+0x48`. Handler `0x00462DD0` copies that entire array into persistent GameNetwork state, stores the scalar and string, then clears the login-pending flag.

Server `LoginClientToken.h` fixes the wire interpretation. Its first `uint32 sessionLength` equals token length plus four; it writes token bytes followed immediately by the little-endian account ID inside that declared region, then writes station ID and ASCII16 username. Consequently the original client treats token plus account ID as one opaque credential blob. The `+0x4C` scalar is station ID and the `+0x54` string is username. These labels and the preservation requirement are **Confirmed**. The replacement client must not discard or re-encode the embedded account-ID bytes before zone authentication.

The failure alternative is now address-backed. Dispatcher branch `0x00461B58` recognizes `ErrorMessage`, invokes parser `0x00AC90A0`, and conditionally reports its two strings when the parsed byte is nonzero. The parser registers exactly two strings followed by one byte, matching server opcode `0xB5ABF91A` and wire shape `LE16(3) | LE32(opcode) | ASCII16(type) | ASCII16(text) | U8(fatal)`.

Reliable receipt also requires a cumulative ACK. For an accepted channel-0 sequence 0, the logical response is `00 15 00 00` with the normal non-compressed marker and deployed transforms. With analysis seed `0x12345678`, the complete deterministic ACK datagram is `00 15 78 78 78 39 46`; details are in `SOE_TRANSPORT_TEST_VECTORS.md`.

The general inbound GameNetwork dispatcher entry is now confirmed at prologue `0x00461770`. It registers/compares named events and application messages; the `ConnectionServerConnectionOpened` branch is `0x004619B3`, while other branches include login/connection close and early zone messages. This supersedes the prior boundary uncertainty.

### Initial replicated-object boundary

The scene dispatcher beginning in the `0x0051BDxx` region now supplies direct Stardust.exe mappings for the first three replication message types.

`SceneCreateObjectByCrc` compares at `0x0051BD7F` and enters shared create logic at `0x0051E1B3`. Parser `scene_create_by_crc_parse_VA_00AD51B0` registers four serialized members: a 64-bit object ID, a 48-byte transform value, a 32-bit template CRC, and a byte flag. The server constructor fixes the transform wire order as quaternion X/Y/Z/W followed by position X/Z/Y and labels the flag as hyperspace. This is **Confirmed by executable parsing plus server construction**.

The create path calls `client_template_lookup_by_crc_VA_00B28AA0`. A missing template emits the executable diagnostic `SceneCreateObjectByCrc crc %08x was not found in object template list` and aborts. On success, the branch checks/reconciles an existing object ID, obtains a new instance from the resolved template name through `0x00B2E760`, initializes template/object state through `0x007664F0` and `0x00B2A950`, copies the 48-byte transform into object offset `+0x50`, invokes virtual transform/setup methods, assigns the object ID via `0x00B23C70`, and performs further initial attachment/notification calls. The factory boundary and operation order are **Confirmed**; exact class and subordinate method names remain provisional.

`BaselinesMessage` compares at `0x0051C6BE`, parses with `baseline_message_parse_VA_00ADBC80`, resolves the parsed object ID, and calls `baseline_view_dispatch_VA_00554CF0`. The parser registers LE64 object ID, LE32 object tag, U8 view, and a variable payload wrapper. Server `BaseLineMessage.h` establishes the serialized payload wrapper as LE32 size, LE16 variable count, then view data. `0x00554CF0` subtracts one from the view byte and dispatches views 1 through 9 to per-object replicated-view unpackers; it also contains an object-specific dynamic-view fallback. This is stronger than the limited Core3 automation client, which only implements views 3 and 6: the replacement architecture must support a registry of object-type/view parsers rather than hard-code two cases.

`UpdateContainmentMessage` compares at `0x0051BEEC` and parser `containment_message_parse_VA_00ADC6D0` registers two 64-bit values followed by one 32-bit value. Server `UpdateContainmentMessage.h` confirms object ID, container ID, and arrangement/type. The handler resolves the child and parent, changes containment, reconciles parent-relative state, and validates position magnitudes. Its explicit diagnostic says invalid object-to-parent position is reset to the parent origin. Field layout and primary state effects are **Confirmed**; individual virtual method identities are not.

`SceneEndBaselines` compares at `0x0051C314`, parses with `scene_end_baselines_parse_VA_00AD58D0`, and contains exactly one LE64 object ID. The dispatcher resolves that object and calls `scene_end_baselines_finalize_VA_0051F9B0`. The finalizer performs object readiness/attachment checks and calls object virtual methods, establishing this message as a per-object post-baseline lifecycle barrier. The call path is **Confirmed**; a complete semantic naming of every condition is still **Strong**, not confirmed.

The resulting executable-backed ordering model is:

`SceneCreateObjectByCrc` -> object exists in client registry -> one or more `BaselinesMessage` view updates and possible containment changes -> `SceneEndBaselines` per-object finalization. Final scene-level `CmdSceneReady` remains a distinct later barrier.

### Baseline/delta shared view registry

`DeltasMessage` is now address-backed. Dispatcher branch `0x0051C76D` uses parser `delta_message_parse_VA_00ADB2D0`, whose four registered members match the baseline wrapper: LE64 object ID, LE32 object tag, U8 view and variable payload. It resolves the object and calls `delta_view_dispatch_VA_00554E90`.

The baseline dispatcher `0x00554CF0` and delta dispatcher `0x00554E90` use the same view-number switch and the same object member offsets. Baselines invoke each view member's snapshot-unpack operation. Deltas invoke its delta-apply operation and then a second virtual post-update/commit operation. Shared registry reuse is therefore **Confirmed by Stardust.exe**.

Server `BaseLineMessage.h` and `DeltaMessage.h` fix the payload distinction: baselines contain a variable count followed by sequential schema values; deltas contain an update count followed by U16 field indices and corresponding values. The first detailed `CREO`, `PLAY`, `TANO`, and `ITNO` schema/index map is in `REPLICATION_SCHEMA_MAP.md`. Field semantics come from the Stardust server constructors and are source-confirmed, while executable acceptance is currently confirmed at the tag/view envelope and dispatcher level.

Two server anomalies are deliberately unresolved: `CreatureObjectMessage6` declares 22 variables although the visible constructor chain emits indices 0-17, and `PlayerObjectMessage6` declares three variables while visibly writing two. These must not be filled by guesswork.

### AutoDelta vector decoding and reconciliation

MSVC RTTI resolves `Archive::AutoDeltaVector<int, CreatureObject>` to vtable `0x015DD07C`. Its snapshot decoder `autodelta_vector_int_snapshot_decode_VA_0043B050` clears the vector, reads U32 element count and U32 update counter, then reads that many U32 elements. This confirms the server `DeltaVector::insertToMessage` framing directly in Stardust.exe.

Delta decoder `autodelta_vector_int_delta_decode_VA_0043B1A0` reads U32 operation count and U32 new counter. It reconciles those values against the local counter, consumes an already-seen leading prefix without applying it, and applies the remaining suffix. Its command switch confirms vector operations 0 remove-by-U16-index, 1 insert/add with index and value, 2 set with index and value, 3 replace from a U16-counted value sequence, and 4 clear. Commands 0/1/2/4 match current server methods; command 3 is an executable-supported compatibility operation not emitted by the generic checked-in class.

Remove validates `index < size`. Insert and set can grow storage; the snapshot path visibly trusts its U32 count for iteration/allocation and exposes no schema-level maximum. These facts are **Confirmed** and motivate stricter checked limits in the x64 replacement. Full formats, RTTI examples for sets/maps, and defensive requirements are recorded in `REPLICATION_SCHEMA_MAP.md`.

### AutoDelta map and set decoding

Representative set RTTI `Archive::AutoDeltaSet<SkillObject const*, CreatureObject>` resolves to vtable `0x015DCF30`. Snapshot decoder `0x0043D820` reads U32 count/counter and keys into a cleared set. Delta decoder `0x0043D9A0` uses the vector-style stale-prefix reconciliation and confirms commands 0 remove-present, 1 add-absent, and 2 clear. Stardust server `SkillList` identifies the creature skill wire key as ASCII16 skill name; the client resolves it into `SkillObject const*`.

Representative map RTTI `Archive::AutoDeltaMap<int,float,...>` resolves to vtable `0x015DE068`. Baseline decoder `0x00540E30` consumes U32 count/counter and repeated command/key/value entries. Delta decoder `0x00540F60` reconciles counters and confirms commands 0 ADD, 1 DROP, and 2 SET; all carry key and value. DROP retains the prior value on wire, and SET suppresses a no-op replacement when values compare equal.

Complex map/vector specializations retain this control framing. Server correlation now fixes high-priority element shapes for creature skills, abilities, wearables, experience, waypoints, and quest bit arrays. Details and confidence boundaries are in `REPLICATION_SCHEMA_MAP.md`.

### Replication callback specialization pass

MSVC RTTI resolves typed callback containers for `CreatureObject`, `PlayerObject`, and `TangibleObject`. Address-backed examples include visual posture (`vtable 0x015DD104`, decode `0x0043A3B0`), group ID (`0x015DCE4C`), performance type (`0x015DCE04`, decode `0x00440160`), experience (`0x015E0C78`, decode `0x00664940`), spoken language (`0x015E0BC4`, decode `0x006652D0`), completed/active quest bit arrays (`0x015E0B58` / `0x015E0D44`, decode `0x00665580` / `0x00665820`), maximum hit points (`0x015E0654`, decode `0x00641430`) and condition flags (`0x015E05BC`, decode `0x00643360`).

The scalar decoders retain the old value, deserialize the new fixed-width value, execute their post-read hook, compare values and conditionally invoke an owner-specific callback. Max-hit-point and condition callbacks dispatch through tangible-object virtual slots `+0xB0` and `+0xAC`, respectively. Quest callbacks copy/compare the prior `BitArray` and suppress unchanged notifications. These mechanics are **Confirmed by Stardust.exe**; downstream UI/gameplay consumers remain to be traced.

Server source fixes two additional specialized wire layouts used by these replicated systems. Creature wearables serialize ASCII16 customization, U32 containment/arrangement, U64 object ID and U32 client template CRC. Waypoints serialize U64 key plus U32 cell ID, F32 X/Z/Y, U64 unknown, U32 planet CRC, Unicode32 name, U64 object ID, U8 color and U8 active, under specialized commands 0 add/set, 1 drop and 2 update. These source-confirmed layouts are now cross-referenced in `REPLICATION_SCHEMA_MAP.md`.

An explicit pre-build decision gate was added to `64BIT_PORT_PLAN.md`: after required client analysis, compare Godot and other viable engines against custom DX12/Vulkan approaches before authorizing implementation.

### Callback subscriber and complex-container pass

RTTI exposes concrete `OwnedTransceiver` subscriber pairs. `ExperienceChanged` reaches the skill manager callback, experience monitor, profession, stats and profession-graph UI classes. `GroupChanged` reaches ground and space group/ship/formation UI classes. `WaypointsChanged` reaches the waypoint monitor and space-zone map. Active/completed quest changes reach the quest journal; visual posture reaches ground status UI; Jedi state reaches profession UI and the command-table parser. Subscriber identities are **Confirmed**; exact per-subscriber methods are not yet named.

`Archive::AutoDeltaVector<WearableEntry,CreatureObject>` resolves to vtable `0x015DCD34`, snapshot decode `0x00441740`, and delta decode `0x00441920`. Snapshot cleanup walks elements with a `0x20` runtime stride. This confirms the legacy in-memory wearable value size but not a wire fixed size; current server source establishes its variable-length ASCII16 customization plus U32/U64/U32 fields.

`Archive::AutoDeltaMap<NetworkId,Waypoint,PlayerObject>` resolves to vtable `0x015E0BA0`, snapshot decode `0x00669F40`, and delta decode `0x0066A1D0`. RTTI also exposes `ClientWaypointObject` and typed activation, location and color events. A replicated player-owned waypoint-map to client presentation-object conversion layer is therefore **Strongly inferred**.

### Asset corpus, terrain and appearance pass

All 58 installed TRE archives mechanically parse as the same TREE/0005 profile with zlib-compressed record and name blocks. Their indices contain 312,860 records, 203,676 unique normalized paths and 101,913 names present in more than one archive. Current server `tre3` source matches the 36-byte header, 24-byte record layout, zlib type 2 and per-record MD5 tail.

Representative extraction confirms static appearance and shader chains, shared-object template inheritance, a human SAT referencing LMG/SKT/LAT resources, and a human CDF referencing sound/effect resources. Ground `terrain/corellia.trn` is PTAT/0014 with DATA, TGEN/0000 and two 524,288-byte map chunks. Server terrain source supplies rule behavior while client RTTI confirms client chunks and a terrain worker wrapper.

Wearable diagnostics establish `WearableAppearanceMap::install` at `0x011B9F70`, using `datatables/appearance/wearable_appearance_map.iff`. Diagnostics confirm rejection of null/non-skeletal wearable appearances and typed customization data with local/remote directory mounts.

The installed wearable table is DTII/0001 with three string columns (`source_wearable_appearance_name`, `wearer_appearance_name`, `mapped_wearable_appearance_name`) and 55 rows. Server `DataTableIff.cpp` confirms the generic COLS/TYPE/ROWS decoding model and scalar type families.

### Main-loop and render-phase pass

`game_loop_VA_00423780` repeatedly invokes `frame_tick_VA_004237C0` until `quit_requested_VA_00423750`. The frame calculates a shared float delta, pumps platform events, performs ordered subsystem calls, updates scene/UI state, enters a quit-guarded graphics block, executes render/present paths, services resource/async work, and increments frame/time counters. Ordering is **Confirmed**; most phase identities remain provisional.

`client_graphics_application_install_VA_0075A220` registers semantic render phases for environment, terrain, main, pre-alpha/alpha/blend, below/at/above water, post-alpha, shadow and final content. `SetupClientTerrain::install` at `0x00840060` registers terrain template factories near `0x0085CA90`.

1. Trace callback callees/transceivers from experience, language, quest, posture, condition and maximum-hit-point changes into their affected UI/gameplay subsystems.
2. Trace waypoint value conversion into `ClientWaypointObject` and its activation/location/color events.
3. Trace wearable vector mutations into `WearableAppearanceMap` and skeletal appearance attachment changes.
3. Trace the runtime initialization of `gameBits` global `0x019110EC`.
4. Continue propagation analysis for the decoded default connection options from `0x012B9E10` into the live UDP object; the option layout and state-0 retry are now confirmed, but the specific caller-supplied establishment timeout remains unresolved.

## 2026-09-11 one-hour continuation: timeouts, options and lag control

`shared_network_options_ctor_VA_012B9E10` constructs a 136-byte (`0x88`) option object. Confirmed offset/name pairs include `+0x04 crcBytes`, `+0x08 hashTableSize`, `+0x0C incomingBufferSize`, `+0x10 outgoingBufferSize`, `+0x18 maxConnections`, `+0x1C maxRawPacketSize`, `+0x20 maxInstandingPackets`, `+0x24 maxOutstandingBytes`, `+0x28 maxOutstandingPackets`, `+0x30/+0x31 processIcmpErrors/processOnSend`, `+0x34 fragmentSize`, `+0x38 pooledPacketMax`, `+0x3C pooledPacketSize`, `+0x40 pooledPacketInitial`, `+0x44 packetHistoryMax`, `+0x48 logAllNetworkTraffic`, `+0x4C oldestUnacknowledgedTimeout`, `+0x50 overflowLimit`, `+0x54 congestionWindowMinimum`, `+0x58 maxDataHoldTime`, `+0x5C resendDelayAdjust`, `+0x60 resendDelayPercent`, `+0x64 noDataTimeout`, `+0x68 reliableOverflowBytes`, `+0x6C icmpErrorRetryPeriod`, `+0x70 maxDataHoldSize`, `+0x85 allowPortRemapping`, and `+0x86 useTcp`. Offsets are confirmed by the getter order and direct reads in `game_connection_base_ctor_VA_012B6150`; semantic propagation beyond that copy remains partly inferred.

The similarly named `[ClientGame] connectionTimeout` is a distinct floating-point UI/session timer. Getter `0x00503580` has only one code caller, `avatar_selection_update_VA_00C62970`. After the mediator submits `SelectCharacterMessage`, it accumulates frame delta in a float and, when the default 600.0 seconds is exceeded, disables its wait/progress state and presents a failure path. It does not configure the state-0 UDP session request.

ConnectionServer lag control is now end-to-end confirmed. Tick `0x0069B8D0` waits for both pending flags to be clear, compares the elapsed millisecond tick count to `[ClientGame] lagRequestDelay` (600000), records a send tick, creates the zero-field `LagRequest`, resets the next-delay value and submits reliably. Callback `0x0069B510` recognizes `ConnectionServerLagResponse` and `GameServerLagResponse`, clears separate pending flags and stores elapsed values at `+0xFC/+0x100`. When both have arrived, `connection_server_send_lag_report_VA_0069B690` serializes those values in connection-first/game-second order. Stardust source confirms `LagRequest=0x31805EE0`, `LagReport=0xC5ED2F85`, `ConnectionServerLagResponse=0x1590F63C`, `GameServerLagResponse=0x789A4E0A`, empty request/response payloads, and two LE32 report fields. The server currently parses but does not act on the report.

`[ClientGame] lagReportThreshold` (10000 ms) is cached by `0x0069B690` and used only to decide whether to emit a warning containing both measured values. It does not reject the connection or suppress the report.
5. Characterize malformed count/string handling and practical allocation caps for the scene replication parsers.

## 2026-09-10 — 75% architecture-constraint pass

`graphics_install_VA_007548A0` directly formats `.\\s2%02d_r.dll`, calls `LoadLibraryA`, resolves `GetApi`, validates the returned interface and installs it. Configured raster 4 is rejected as obsolete DX8; majors 5–7 are accepted. Installed `s205_r.dll` is x86, exports only `GetApi`, and imports `Direct3DCreate9` plus D3DPERF markers. This confirms that device-level D3D9 behavior is in the raster module, not Stardust.exe.

The deployed `appearance/yt1300.pob` was extracted read-only from `stardust_3_00.tre`: `PRTO/0004`, eight indexed portal geometries, exterior APT, seven cell MSH references and seven FLR references. Its hall floor is `FLOR/0006`, with 38 vertices, 36 60-byte triangles, BTRE, BEDG and PGRF. Server PRTO/CELL/FLOR readers fix portal/cell/door/collision/path semantics; details are in `INTERIOR_PORTAL_ARCHITECTURE.md`.

Scene dispatcher branches now map `SceneDestroyObject` at `0x0051BD97` through parser `0x00AD55D0` and guarded handler region `0x0051BDFD`–`0x0051BEC9`. `UpdateTransform` maps from `0x0051C823` through `0x00ADAC80` into a controller command path; `UpdateTransformWithParent` maps from `0x0051C9AC` through `0x00ADA510` into its parent-relative controller path. Server constructors fix quarter-unit versus eighth-unit coordinate quantization and field order.

DirectInput and Miles imports/RTTI/config diagnostics establish the concrete legacy input and audio backends. File-streaming/terrain thread types and pervasive `MemoryBlockManager` instances establish specialist workers and fixed-size pools. These are replacement boundaries, not compatibility contracts.

Server `CustomizationVariables` source recovers the version-1 escaped byte grammar, optional signed 16-bit values and `FF 03` terminator; Stardust.exe contains matching corruption/version/ID diagnostics. `CHECKPOINT_75_PERCENT.md` consolidates conclusions and remaining work.

## 2026-09-10 — final pre-build decision pass

`scene_transition_begin_VA_00462AE0` RTTI-checks/destroys an existing `GroundScene`, constructs a new 0xF4-byte scene through `0x00519C40`, and transfers it to `game_set_scene_VA_00424220`. Process cleanup uses `game_cleanup_scene_VA_00423700`. Together with the deferred `CmdSceneReady` and server-documented same-frame race, this establishes a mandatory generation-scoped transition/readiness barrier.

Configured module `s207_r.dll` confirms the raster contract: `GetApi` returns verifier/installer bootstrap entries; installer `0x62A01BE0` consumes engine callbacks and fills a table of well over one hundred backend functions. D3D9 adapter/device/reset/present, buffer/texture/target/state/shader operations are backend concerns; the clean client need not reproduce the ABI.

2026-09-12 PASS consumer continuation: imported and analyzed the configured `s207_r.dll` (SHA-256 `D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D`). Engine graphics slots `+0xA0/+0xA4` resolve to backend selection/application entries `0x62A046A0/0x62A046B0`. Backend `0x62A0F3C0 -> 0x62A0E9D0` compiles each engine PASS into a 0xA4-byte cached record; `0x62A0F4B0 -> 0x62A0EF30` applies it. Named StaticShader calls identify material, texture-factor, scroll, alpha-reference and stencil-reference PASS offsets. Texture-stage versions 0000/0001 and exact read widths were also recovered. Evidence is archived with the checkpoint rather than treating decompiler output as source.

Function `creature_underwear_wearable_refresh_VA_00437450` manages default underwear/fallback wearable objects. Its create branch mounts `/shared_owner` customization, RTTI-checks `SkeletalAppearance2`, and calls `0x007C9A50`; its removal branch calls `0x007C9C30`, destroys the retained object, and clears the slot. Both skeletal methods retain/remove worn-object watchers and call `0x007CB440` to mark the affected slot interval dirty. This proves the general skeletal ownership mechanism but is not, by itself, the replicated wearable-vector callback path.

The read-only TRE probe now advances SWG odd-sized IFF forms correctly. Full `yt1300.pob` structure exposes eight cells, cell bounds/lights, building-wide PGRF and CRC. The installed corpus contains 354 FLOR/0004 records; sampled v4 payloads use raw 12-byte VERT and 60-byte TRIS arrays plus empty PNOD/PEDG chunks. Sound loader `0x0041CFD0` supports SD2D versions 0000-0003; deployed SD3D wraps SD2D and adds a 3D version tail.

`cui_manager_install_VA_008802F0` establishes a process-lifetime UI service/mediator layer. Stardust.exe does not import Qt, so adjacent Qt binaries are not treated as client UI architecture. Current server SHIP and SCLT baseline schemas were added to bound space/cell object modeling.

The authoritative synthesis is `FINAL_PREBUILD_ANALYSIS_REPORT.md`. `ANALYSIS_STATUS.md` now distinguishes 100% completion of the engine/build-decision analysis from likely-under-5% individual-function semantic coverage.

## 2026-09-11 — secure-trade protocol closure

The bounded `ClientSecureTradeManager` pass maps all 12 inbound named branches and the principal outbound request functions. U64 item/player identifiers and U32 cash remain fixed-width wire values; every identified client request uses the common reliable-send boundary. Confirmed hashes from the current server registry include AddItem `0x1E8D1356`, GiveMoney `0xD1527EE8`, Accept `0xB131CA17`, UnAccept `0xE81E4382`, Verify `0x9AE247EE`, and Deny `0x6EC28670`.

Two local state bytes separately track peer acceptance (`0x019161F0`) and local acceptance (`0x019161F1`); `0x019161F2` tracks an active trade. Inbound `BeginVerificationMessage` publishes `RequestVerify(true)`, whereas inbound `VerifyTradeMessage` reaches a literal one-instruction no-op at `0x005E4790`. Verification is operative in the other direction: the client sends Verify, and the server commits only once both sessions are verified and all ownership, capacity, currency, distance, and tradeability checks pass.

The current tree lacks exact named server counterparts for `RemoveItemMessage`, `AddItemFailedMessage`, and `BeginVerificationMessage`, despite confirmed client decoders. This source/executable asymmetry is now recorded as an explicit research target rather than papered over. Full evidence and a replacement state model are in `TRADE_PROTOCOL.md`.

### Object-controller envelope and registry

`ObjControllerMessage` hash `0x80CE5E46` is parsed at `0x00AC9660` as fixed-width priority U32, inner type S32/U32, object ID U64 and float scalar, followed by a typed nested payload constructed through registry lookup `0x00B0D190`. Dispatch `0x00462F60` resolves the addressed object/controller and enqueues the nested message through `0x00B3D250`.

A new deterministic probe, `tools/object_controller_catalog.py`, recovered 139 direct-immediate inner-type registrations at `0x00B0CFC0`, including both outbound and inbound factory VAs. All 24 inner types currently accepted by Stardust-3's `registerObjectControllerMessages()` are present in the executable registry: a 24/24 (100%) numeric intersection. This is availability evidence, not complete field-schema coverage. The envelope, active intersection and replacement boundary are documented in `OBJECT_CONTROLLER_PROTOCOL.md`.

## 2026-09-11 — timed protocol-correlation continuation

All 24 server-accepted ObjectController types now have server-derived directional schemas. The server callback cursor begins on the outer scalar, resolving its initial four-byte handling. `MessageQueueSecureTrade` type `0x115` is fully decoded as operation plus two NetworkIds, and its request form emits operation zero, invalid first ID and selected target second ID.

The server-emission scanner found 71 direct construction sites and 51 constant inner types. Fifty have executable factories. RTTI and registration calls prove the remaining quest anomaly is a genuine mismatch: client counter `0x440`, client timer `0x441`, server counter `0x441`, server timer `0x444`, with no recovered client `0x444` factory. Extended harvesting, JTL, loot, image-design and crafting schemas are in `EXTENDED_OBJECT_CONTROLLER_SCHEMAS.md`.

The 37 server message-class names missing an exact executable string were audited. Thirty-one now have aliases, direct routes, registered controller identities, or executable class anchors, raising the identity-resolved lower bound to 108/114 (94.74%). Direct additions include append-comment parser `0x00AFA390`, knowledge-base parser `0x00AF8060`, PvP parser `0x00AD3000` with object state writes, player-money parser `0x013F63A0`, and harvester activation parser `0x013F3F60` with UI boundary `0x008CC460`. Six source-defined identities still lack direct client routes.

Offline verifier `tools/verify_transport_vectors.py` passes nine CRC/selector-4 assertions, including a complete synthetic reliable `LoginClientId` datagram and channel-zero ACK. It performs no network access. The updated heuristic function inventory finds 94,689 candidates and 163 intersecting stable names (0.17%); this remains separate from the 38% weighted replacement-relevant research estimate in `ANALYSIS_CHECKPOINT_2026-09-11.md`.

## 2026-09-11 — persistent Ghidra and replication-registry pass

Official Ghidra 12.1.3 was installed locally under `toolchain/` and its published archive SHA-256 was verified as `93A5D11A9AD510622ACAAF908C556A7B9B764D338E78A7567F3689BF5081FD54`. The exact `Stardust.exe` baseline was imported into the persistent project `ghidra/StardustClient`, analyzed as 32-bit little-endian x86/Windows, and saved. No PDB was recovered. Ghidra reported 243,265 function objects, of which roughly 121,817 are eight bytes or smaller; this is a noisy tooling inventory rather than a semantic-function denominator.

Reusable scripts now import stable provisional names, export a function inventory, create address-confirmed functions missed by automatic discovery, decompile address lists, and export xrefs. The first name import applied 162 function names and 28 labels. Automatic function discovery missed several real aligned-stack functions, including group-change decode `0x0043F960` and three `AutoDeltaByteStream` methods; these were created only after vtable/disassembly evidence confirmed their boundaries.

The common object constructor at `0x00554240` creates six embedded view registries. Baseline dispatcher `0x00554CF0` and delta dispatcher `0x00554E90` share the view mapping; delta additionally commits deferred callbacks. Registry methods `0x0118A840`, `0x0118AA00`, and `0x0118A970` confirm U16 ordinal assignment, payload-end-driven delta parsing, and deferred change notification. Declared U16 baseline/update counts are read but do not control iteration.

Player view-8 waypoint callbacks at `0x0065C640`, `0x0065C920`, and `0x0065CC40` directly bridge replicated `NetworkId -> Waypoint` values into `ClientWaypointObject` lifecycle and `WaypointsChanged` publication. This confirms the replacement boundary between protocol value state and presentation objects.

Direct comparison of the view-9 callbacks at `0x0065CF50`, `0x0065D010`, and `0x0065D110` resolves their precedence rule: each searches the primary view-8 map and mutates presentation state only when the NetworkId is absent there. The client presents a union with view 8 dominant and view 9 acting as a fallback/legacy source. Its historical producer is still unknown.

Cell, Static, and Building constructors at `0x006E58A0`, `0x00709FE0`, and `0x0070DBB0` close the initial ground-object inheritance model. Cell adds one view-3 cell-number field; Static adds none; Building inherits Tangible and adds none. Current server view-3 constructors match the composed client registration counts. Common view-6 U32 decoders at `0x00542DA0` and `0x00542DC0` show that Cell/Static legacy view-6 tails are left unused, while Building's inherited tangible defender vector consumes the second logical field.

Constructor registration order plus field RTTI closes all 18 client `CREO:6` indices. The server's group inviter ID and invite counter are decoded by one pair-valued AutoDelta field, while HAM, max-HAM and wearables are three distinct containers. The declared operand count of 22 is not the client schema size. PLAY:8's seven positions are also fully ordered, and PLAY:9's 18 positions are bounded by scalar/container type even where historical semantics remain unknown.

Common view-3 RTTI identifies complexity, StringId, custom name, and volume. Tangible adds its seven view-3 fields; Creature adds seven more, exactly matching `CREO:3`. Common view 1 is bank then cash; Creature adds base HAM and skills. Creature view 4 has exactly 14 registrations matching the server's movement/encumbrance/skill-mod/listen/space-mission order. All principal `CREO` baseline views now have confirmed cardinality and ordering.

Dirty-field enqueue `0x0118A7C0` performs ordered-set insertion keyed by field identity and suppresses duplicates. Commit `0x0118A970` traverses that set, invokes one callback per dirty field, clears all nodes, and resets the count. Multiple updates to the same field inside one delta therefore coalesce; callback order is not packet order.

## 2026-09-11 — containment-driven wearables and PLAY codec closure

The suspected replicated-wearable callback path was resolved negatively and replaced with a confirmed containment path. `CreatureObject` constructor `0x0042ECF0` initializes the `AutoDeltaVector<WearableEntry,...>` callback slots at `+0x944/+0x948/+0x94C/+0x950` to null and installs no later functors. The generic vector decoder supports callbacks, but this concrete field uses none.

CreatureObject vtable data at `0x015DCCC0/0x015DCCC4` points to containment add/remove wrappers `0x00436D90/0x00437050`. These reach `0x0067A060/0x0067A140`, which perform valid-appearance checks and call the confirmed skeletal wear/stop-wearing functions. The GroundScene `UpdateContainmentMessage` branch parses child ID, parent ID and U32 arrangement and applies them through `0x00555020`. Server `UpdateContainmentMessage.h` and `PlayerContainerComponent.cpp` corroborate the separate relationship and wearables-vector updates.

Specialized PlayerObject vtables yielded ability snapshot/delta `0x00661E20/0x00662030`, draft-schematic snapshot/delta `0x006633A0/0x00663530`, and quest packed-map snapshot/delta `0x00665C40/0x00666430`. Ability elements are length-prefixed ASCII byte strings with U16 length and `0xFFFF`/U32 extended length. Draft schematics are U32 pairs. Quest values decode as the exact server `PlayerQuestData` shape: U64 owner, two U16 step masks, U8 completion and U32 counter, keyed by U32 quest CRC.

Malformed decoding is not transactional. `0x0118B030` bounds-checks fixed reads and throws on overrun, but `0x0118AA00` directly indexes the field registry with an unvalidated U16. Earlier mutations survive a later throw while normal outer commit is skipped. Replacement behavior must reject the entire payload before state mutation.

The appearance-map branch is now closed. Loader `0x011B9B80` reads the exact three-column source/wearer/mapped table, and lookup `0x011B9FF0` has only two consumers. Creature containment callback `0x00436D90` uses the child's source appearance plus the creature's wearer appearance: a non-empty mapped result substitutes the child appearance, an empty mapped result suppresses wear, and no match preserves the original. `0x00647E20` queries the empty-source fallback case. Skeletal wear retains the worn object reference; `0x007CB440` marks affected slots dirty rather than performing the entire rebuild synchronously.

Malformed Archive exceptions were followed outward through GroundScene receive `0x0051BD10`, keyed event fan-out `0x00AC2AB0`, and connection message dispatch `0x005A8C50`. None catches the exception; their EH tables provide cleanup/unwind only. The connection traffic counter update after fan-out is skipped on an exception. The first outer catch or termination policy remains unknown.

Three more derived-object schemas were constructor-correlated. WeaponObject `0x00709D00` adds no replicated fields beyond TangibleObject. ResourceContainerObject `0x0070A230` adds quantity/resource ID to view 3 and max-stack/type/name to view 6. Its current server view-6 byte padding is intentionally consumed by inherited common and defender fields. Server delta indices 5/6 for resource type/name and the commented index 14 for resource ID exceed the actual client registries; the source's “Causes CTD” comment is therefore explained. Callback `0x0070A9E0` dirties resource presentation, and virtual `0x0070A570` derives a texture from type/name and applies it under `REP0`. InstallationObject `0x0070AD70` adds active byte, surplus-power float and base-rate float at view-3 indices 11-13.

GroundScene's header-only Logout route calls `0x0089A430` to close the connection server and clear retained selection state before scene cleanup. The server-driven browser route was also finished: `0x00AA13D0` preserves only an exact `http://` prefix, prepends `http://` otherwise, and calls `ShellExecuteA("open")` without allowlisting or confirmation. This is evidence for a replacement security policy, not behavior to copy.

The resource icon resolver was then carried through its asset boundary. `0x004B5480` resolves `appearance/ui_res_<resource-class>.spr` and recursively substitutes `inorganic` when the requested class is unavailable. `0x004B5F40` requires the loaded appearance to be a `SpriteAppearance`, its shader to be a `StaticShader`, and a texture under four-character tag `MAIN`; the resulting texture is what `0x0070A570` binds under `REP0`. This closes the full replicated resource metadata -> sprite -> shader -> texture -> object appearance chain.

Malformed inbound propagation was extended through `connection_server_named_message_VA_0069B510`, the sole code caller of `0x005A8C50`. It performs lag-message bookkeeping and then invokes the generic connection dispatcher without a catch. This is one additional confirmed uncaught layer; the first process-level boundary remains unknown.

## 2026-09-11 — process exception boundary, SHIP and GRUP

The outer malformed-Archive path is now closed. Received-message service loop `0x012B3B80` owns catch thunk `0x012B3D42`, which obtains the exception's virtual text, logs `Unhandled Archive read error: %s`, performs runtime cleanup and rethrows using Archive throw metadata `0x0164239C`. The exception then crosses GameNetwork update `0x00462BC0`, frame tick `0x004237C0`, loop `0x00423780`, application bootstrap `0x00401050`, WinMain wrapper `0x00401810`, and CRT entry `0x0131DC7A`. None supplies another application catch; the CRT uses the normal MSVC frame handler. The legacy behavior is therefore diagnostic plus process-fatal propagation, not packet rejection/recovery.

RTTI located `ShipObject` vtables `0x015DDE6C/0x015DDE60` and constructor `0x00536670`. Constructor registration independently matches current server SHIP views: 22 composed view-3 fields, 38 view-1 fields, 23 view-6 fields, and five view-4 fields. Component add/set/drop callbacks `0x0053C3E0/0x0053C130/0x0053BE80` converge on `0x00539B70`, which refreshes chassis attachment state from the affected component slot and then publishes `ComponentsChanged`.

SHIP target state is now tied to concrete semantic hooks. Target NetworkId codecs `0x00545290/0x005453C0` invoke `0x0053D040` and publish `LookAtTargetChanged`; target-slot codecs `0x00545550/0x005455A0` invoke `0x00696760` and publish `LookAtTargetSlotChanged`; targetable-bitfield codecs are `0x00545860/0x005458F0`. The client-originated target-slot path is also closed: `0x00696480` sends integer controller payload `0x3FA` through `0x006189A0` with controller flags `0x83`, then applies the slot locally. Stardust `UpdateShipTargetSlotCallback` parses two U32 values (payload size and target slot), validates slot range/pilot/root ship, and changes the authoritative field, which is returned through SHIP:6 index 13.

Client startup function `0x01197250` directly registers 50 controller-payload IDs and factories, including `0x3FA -> MessageQueueGenericValueType<int>`. This is a concrete subset of the broader 139-entry registry already cataloged and confirms that the registration factory pair is a typed serializer/deserializer boundary. Other directly RTTI-recovered payload types include pair<int,int>, pair<int,float>, bool, signed byte, NetworkId, NetworkId+U32, NetworkId+Unicode, vectors of matchmaking results or spheres, and hyperspace string+Vector.

`GuildObject` constructor `0x0070B5F0` closes `GILD:3/6`. Its added view-3 field is a string set whose wire values are `guildID:guildAbbrev`; callback `0x0070B8C0` splits at the first colon and rebuilds numeric-ID and abbreviation lookup structures. Client delta decoder `0x0070C720` accepts command 0 remove, 1 add, and 2 clear. Current server `GuildObjectDeltaMessage3::removeAllGuilds()` emits 3, which the executable ignores. This is a confirmed protocol incompatibility and a candidate tolerant-decoder fixture for the future client.

The two PLAY:9 values at indices 14 and 15 are generic U32 AutoVariables. Only trivial getters `0x00660A20/0x00660A30` expose them. A raw executable-wide scan of every `E8 rel32` call encoding found no call site for either getter, and current Stardust emits zero for each. Width, position, and dormant behavior are confirmed. They are now classified as reserved compatibility slots rather than material unknowns; only their historical source-level names remain unknown.

Containment-driven skeletal wear ordering is now closed on both branches. `0x00679E20` mounts customization directory `/shared_owner` before `0x007C9A50` attaches the wearable; removal calls `0x007C9C30` before `0x00679F70` unmounts it. For the alternate/non-skeletal child branch, `0x007C9680` allocates and appends an appearance-attachment record keyed by worn-object identity and rebuilds the skeletal appearance; `0x007C97B0` finds that record, destroys it, erases it, and rebuilds. This is a lifecycle/order requirement, not an x64 memory-layout requirement.

Two handshake-adjacent ambiguities were also closed. `ParametersMessage` is one U32 stored by `0x004630B0` at `GameNetwork +0x60`; disassembly-confirmed getter `0x00463750` has no static callers, so Stardust's `0x384` is retained but dormant and its historical units remain unknown. `ConnectPlayerResponseMessage` is handled by the customer-service listener `0x00A19580`, converted to a boolean, and published as `CuiCSManagerListener::Messages::ConnectPlayerResponse`; it is not part of zone authentication.

`PlayerMoneyResponse` was traced into the character-sheet listener `0x00FA0100`. The matching branch constructs the inbound form at `0x013F64D0`, decodes its two U32 AutoVariables, and immediately destroys it. Neither field has a callback and the branch never reads their values. This response is therefore confirmed inert/legacy in Stardust.exe; active balances must be sourced from replicated creature/player state rather than this message.

Avatar-creation result handling is now address-backed at listener `0x00BB4770`. `ClientCreateCharacterSuccess` parses the new U64 object ID at `0x00AE5710` and passes it through `0x00BB6380`, which associates/saves the avatar against current galaxy state, refreshes avatar records, closes pending message boxes and publishes success. `ClientCreateCharacterFailed` parses at `0x00AE5A40`, resolves the current server's `ui:<error token>` localization shape, then calls the same result boundary with failure. `ClientRandomNameResponse` parses at `0x00AE63A0`; `0x00BB6880` verifies template/race correlation and approval metadata before applying the Unicode name, with explicit warnings for wrong-template, error-token, and empty-name responses.

The outbound creation path is `0x00BB4D10` -> `0x00AE4E30` -> reliable send. Its eleven registered fields exactly match current server parsing: body customization, Unicode name, race template, starting location, hair template/customization, profession, unknown byte, height float, Unicode biography, and tutorial byte. The client derives body/hair customization strings from live creation objects before construction and enters a pending-dialog state after submission.

The critical gameplay-command path is now closed through controller `0x116`. `ClientCommandQueue` routine `0x0046D7A0` calls sender `0x0062CB50` with action counter, command CRC, target NetworkId and Unicode arguments; typed payload constructor `0x00AE3B60` and serializer `0x00AE3E80` preserve that exact order and queue priority `0x23`. Stardust parses the typed size plus those fields, applies spam gating/command lookup, then calls `CreatureObject::enqueueCommand`. This gives the replacement a confirmed command DTO and separates local queue behavior from authoritative execution.

The paired `0x117` removal/result path corrects prior server-only documentation. Client codec `0x00AE37E0/0x00AE3850` always uses `{U32 actionCount,F32 timer,U32 tab1,U32 tab2}`. Cancellation sender `0x0062CBD0` fills `{actionCount,0,0,0}`; current server consumes the first two typed dwords and ignores/unreads the trailing zeros before deleting the action. Server-to-client results use all four. Dispatcher `0x0062AA10` extracts them and `0x0046E720` publishes `ClientCommandQueue::Messages::Removing`, processes optional result metadata and removes the local entry.

Controller `0x126` is now tied to ship target acquisition. Sole sender caller `0x0053CB70` sends the selected target NetworkId through `0x00618930` at flags `0x83`, resets component targeting to slot `0x70`, updates the ship controller target and acquisition state, and may publish `TargetAcquisition`. Stardust validates the object and sets both player target and piloted root-ship target; authoritative deltas return through CREO:6 index 9 and SHIP:6 index 12, where the ship callback publishes `LookAtTargetChanged`.

RTTI also located `GroupObject` vtable `0x015E0ED4` and constructor `0x0067AFC0`. Seven added view-6 registrations plus the inherited common field exactly compose the server's eight GRUP:6 fields. Member snapshot/delta `0x0067E500/0x0067E750` decode `NetworkId + ASCII`; member-ship snapshot/delta `0x0067FA90/0x0067FCA0` decode `NetworkId + U32`. Six callbacks expose aggregate and granular MembersChanged/MemberAdded/MemberRemoved and MemberShipsChanged/MemberShipAdded/MemberShipRemoved events.

`ManufactureSchematicObject` constructor `0x00701500` and its nine field RTTI types close views 3 and 6. View 3 composes common fields, inherited intangible status used by the server for manufacture limit, `AutoDeltaMap<StringId,float>` crafting values, creator Unicode, S32 and F32. Map snapshot/delta code confirms U32 count/counter plus U8 operation, StringId and F32 per entry. The server's `updateManufactureLimit()` incorrectly sends an integer to map index 5; baseline manufacture limit occupies inherited index 4. No call to that helper exists in the current source tree, making the defect dormant rather than an observed live failure. View 6 adds two ASCII appearance strings, schematic CRC, active bool and resource-slot U8; the current baseline's U32 zero and final U16 are byte-level shortcuts for two empty ASCII16 fields and bool/U8. Client `FactoryObject` constructor `0x0070D940` adds no replicated fields beyond TangibleObject.

MSCO callback helpers `0x00701EA0`, `0x00702180`, `0x00702550`, and `0x00701C10` close the downstream behavior. Schematic-CRC changes resolve the draft-schematic object template and appearance; `AppearanceData` validates a `SharedTangibleObjectTemplate` and builds a custom appearance; `Appearance` updates the live appearance/customization path; resource-slot U8 changes publish `ManufactureSchematicObject::Messages::ChangeSignal`. Failed CRC/template/appearance lookups issue dedicated warnings. This is a second confirmed replication-to-appearance bridge alongside resource containers and wearable containment.

`ClientMissionObject` constructor `0x006C34B0` adds twelve fields after `ITNO:3`, exactly producing the server's 17-field `MISO:3`: difficulty; destination `Location`; creator Unicode; reward; start `Location`; target-template CRC; description/title StringIds; refresh; mission-type CRC; target ASCII; and packed Waypoint. All current `MissionObjectDeltaMessage3` indices align. Refresh callback `0x006C3B00` publishes `EnteredMissionBag`; type callback `0x006C3F30` selects mapped mission presentation; waypoint callback `0x006C3D90` resolves or constructs a `ClientWaypointObject`, associates it with the mission, and refreshes presentation.

The resulting `ClientWaypointObject` constructor `0x005252B0` adds seven view-3 fields after IntangibleObject: callback Vector, callback bool, NetworkId, callback ASCII, Unicode, bool, and callback ASCII. Callback RTTI and bodies explicitly publish `LocationChanged` and `ColorChanged`; the leading bool updates the active-waypoint collection/visibility path. This confirms the presentation object's typed boundary while leaving the historical names of two bools and one ASCII field tentative.

Mission request/response lifecycle was closed for accept and abort. The accept request path constructs a three-field payload at `0x00AF5030` from mission NetworkId, retained terminal NetworkId and an incrementing U8 terminal-list index, sends controller `0xF9` with flags `0x83`, and opens a pending UI dialog. Stardust validates both objects and replies with `0xFA` containing mission ID, success byte and terminal index. Player-controller dispatcher `0x0062AA10` casts the payload as `MessageQueueMissionGenericResponse`; handler `0x00978B70` reconciles the pending sequence and publishes `CuiMissionManager::Messages::ResponseAccept`.

Abort sender `0x00979960` constructs a `MessageQueueNetworkId` through `0x00ADE420`, sends controller `0x142` with flags `0x83`, and establishes a separate pending dialog. Stardust validates and aborts the mission and responds using the same inner type with the mission ID. Client handler `0x00978E20` clears abort-pending state and publishes `CuiMissionManager::Messages::ResponseAbort`. This supplies a second confirmed direction-dependent controller schema in addition to command removal `0x117`.

Mission terminal list request `0x00979340` builds controller `0xF5` through `0x00AF4840/0x00AF4930`: flags byte, incrementing sequence byte and terminal NetworkId. It opens a pending dialog and clears prior list/result collections. Current Stardust `MissionListRequestCallback` consumes the same fields, validates the terminal/range/cooldown, then repopulates MISO objects and sets their refresh counters. The separately supported named `PopulateMissionBrowserMessage` (`0x88D9885C`) decodes `U32 count + repeated U64 mission IDs` and publishes `ReceiveList`, but its server constructor has no source call site; it is dormant in the current implementation.

Mission details sender `0x00979660` constructs controller `0xF7` through `0x00AF4BE0/0x00AF4CE0` as mission ID, terminal ID and U8 sequence. Dispatcher `0x0062AA10` handles expected `0xF8` as `MessageQueueMissionDetailsResponse`; `0x009788B0` rejects stale sequence values and otherwise publishes `CuiMissionManager::Messages::ReceiveDetails`. Current Stardust registers no `0xF7` callback and has no `0xF8` emitter, so the feature is a confirmed legacy client capability but current-server protocol gap.

The outstanding `ClientIdMsg.gameBits` source is confirmed rather than inferred. `ConfigClientGame::install` routine `0x00503600` reads integer key `[ClientGame] gameBitsToClear`, default zero, into `0x019110EC`. Getter `0x005035B0` has one executable caller, `0x0069B430`, and feeds the first U32 constructor field. The current server parses it but takes no action based on it. This removes it as a POC blocker and fixes the expected default to zero.

Client-permission consumption was carried through the login UI. `0x00899340` stores regular/Jedi bytes, publishes the login byte as `CuiLoginManager::Messages::ClusterConnection`, and clears selected-cluster state on denial. `SwgCuiClusterSelection` consumer `0x00C7B920` rejects false, unsolicited and wrong-cluster results before activating avatar selection. It selects Jedi-only creation only when Jedi is allowed and effective regular creation is denied. Getter `0x0089A3D0` computes regular permission OR global `0x01908858`; `Game::install` `0x00422E80` sets that global to one and the binary uses it widely as installed state. Consequently normal runtime effectively bypasses a server regular-creation denial, while the Jedi byte remains direct. The fourth server skip-tutorial byte is still not decoded. A new client should not reproduce this conflation of lifecycle state and permission policy.

## 2026-09-11 core controller client effects

Controller dispatch is a confirmed fallback chain: player/UI dispatcher `0x0062AA10` delegates unhandled types to creature dispatcher `0x006392E0`, which delegates to common object dispatcher `0x006E5210`. High-frequency server emissions now have executable-backed effects for animation `0xF2`, spatial chat `0xF4`, posture `0x131`, combat spam `0x134`, weapon ranges `0x140`, and buffs `0x229/0x22A`.

CombatAction `0xCC` has a material legacy limitation. Handler `0x006E4950` accepts zero or one decoded 0x18-byte defender records, but any vector length greater than one logs `Ignoring combat action, currently only support one defender, got %d` and returns before presentation. The current server can construct multi-target records up to 20. A replacement can improve this without changing the wire protocol, but compatibility fixtures must cover zero-, one- and multi-defender forms.

## 2026-09-11 VFS/TRE runtime lookup

The public `TreeFile::open` at `0x00A931E0` and installer `0x00A92800` now establish exact overlay behavior. SharedFile nodes are created for every configured value at each numeric priority, inserted into a descending-priority vector, and queried in vector order. The first successful node therefore wins. SearchAbsolute and SearchCache are installed after configured path/tree/TOC nodes with derived trailing priorities.

Logical paths are normalized before lookup by `0x00A930D0`: leading separators and leading dot-relative segments are removed, slash forms become `/`, repeated separators collapse, and ordinary bytes are case-folded. SearchTree and SearchTOC both use CRC-plus-full-string binary search, which means CRC collisions are disambiguated by normalized path rather than treated as identity.

SearchTree accepts versions 0004/0005. Its 24-byte record table and names may be stored or decompressed at mount time. A zero payload-size record acts as an archive tombstone/mask. Stored payloads become archive-backed slices; compressed payloads are eagerly decompressed to owned memory. TOC version 0001 follows an analogous lookup and open model. This closes mount precedence and direct-runtime TRE consumption as implementation-grade requirements; bounded decompression and exact CRC/case-fold reproduction remain validation items.

## 2026-09-11 filename CRC and appearance factory dispatch

`0x00AA4790` proves archive filename checksums use non-reflected CRC-32 polynomial `0x04C11DB7`, with all-one initialization and final complement. It matches current server `String::hashCode`. A standalone verifier matched all 1,477 records in `stardust_3_03.tre` and all 808 records in `bottom.tre`. Normal path mode lowercases ASCII A-Z before hashing through `0x0131D6E8`.

Appearance fetcher `0x00B263C0` treats `APT/0000/NAME` as a single-level redirect, rejects redirect-to-redirect, recursively resolves the target, and caches both redirect and concrete templates by CRC string. Concrete IFFs are dispatched by outer FORM tag through registry `0x00B25E80`. Eighteen tag registrations were recovered, including mesh, skeletal, sprite/quad, effects, terrain, APPR and no-render families. Base APPR loader `0x00B2CB60` accepts versions 0001-0003. This establishes the VFS-to-IFF-to-typed-decoder boundary.

## 2026-09-11 MESH and hardpoint boundary

MESH factory `0x007962D0` reaches a four-version loader at `0x007968B0`. Versions 0002/0003 load explicit CNTR/RADI bounds; 0004/0005 load APPR extent data and derive bounds. Every version then creates an SPS container. SPS requires outer `SPS ` and supports versions 0000/0001. Runtime DPVS models are generated from BoxExtent or fallback bounds and should not contaminate the portable decoder model.

APPR optional HPTS data is a counted sequence of HPNT records containing transform plus name, sorted after load. This closes hardpoints as first-class asset semantics needed by equipment/effect attachment. Vertex declarations, index buffers, primitive topology and shader names remain inside the two SPS version loaders and are the exact next target.

## 2026-09-11 SPS and VTXA decoding boundary

Both SPS versions contain counted shader groups. A group resolves an external NAME shader or inline shader template, then creates its counted primitives. SPS 0001 primitive INFO exposes topology plus index/sorted-index flags; VTXA supplies vertex data, INDX supplies U16 indices, and optional SIDX supplies sorted-index ranges. Legacy SPS 0000 uses U32 file indices truncated to the same U16 runtime buffer.

### 2026-09-12 — topology, SIDX, shader/effect envelope, and mesh anomaly closure

Raw disassembly at `0x0076F890` establishes the complete twelve-entry SPS topology table. IDs 0-5 bind non-indexed point list, line list, line strip, triangle list, triangle strip and triangle fan wrappers; IDs 6-11 bind indexed equivalents. Statistics helper `0x007709F0` corroborates the mapping with `/3` triangle-list and `count-2` strip/fan primitive calculations.

`0x007707D0` resolves SIDX semantics. It binds the primitive vertex buffer, transforms the camera displacement through the supplied object transform into local coordinates, computes a dot product against every 12-byte direction in the 16-byte SIDX record, chooses the smallest value, and binds that record's index buffer. Without SIDX it binds ordinary INDX.

Shader tracing now reaches the material/effect/pass boundary. SHT fetch is cached with `shader/defaultshader.sht` fallback and registry-based outer-tag construction. EFT fetch is cached with `effect/defaulteffect.eft` fallback. EFCT accepts 0000/0001 and selects the first supported inline or NAME implementation from its U8 count. IMPL supports 0000-0007; later forms introduce SCAP and OPTN gates. Each implementation owns counted PASS records, whose dispatcher supports 0000-0009. Exact PASS state/program fields remain intentionally open.

The detailed corpus auditor reclassified all 171 formerly unresolved MSH occurrences as zero-payload SearchTree masks. The split is 88/11/7 across `data_sku1_00/06/07.tre` and 65 in `mtg_patch_004_appearance_04.tre`. The SKU masks often retain nonzero uncompressed-size metadata, so zero compressed/stored payload size is the correct mask discriminator. No live alternate mesh schema remains in the installed corpus.

### 2026-09-12 — PASS layout and shader corpus

All ten PASS loaders at `0x0075E940` through `0x007619D0` were exported. PASS 0009 explicitly reads one child-stage count, 23 additional U8 states, and eight U32 states/tags into stable runtime offsets through `+0x7C`. It then loads `PFFP` or `PVSH`, followed by `PPSH` or repeated texture stages. PFFP supports versions 0000/0001. Older PASS versions are additive subsets sharing the same constructor defaults.

Mechanical scanning covered 36,686 live SHT, 1,243 SHT tombstones, and 467 live EFT with no framing/decompression failures. SSHT is 34,302 version 0000 and 2,266 version 0001; EFCT is four version 0000 and 463 version 0001. PASS 0008/0009 dominate the embedded live forms.

The scan also found 108 PASS 0010 and 82 IMPL 0008/0009 raw forms in later patch assets. Main executable dispatch is demonstrably limited to PASS 0009 and IMPL 0007. Searching `Stardust.exe` for the later version immediates found `0010` only in an unrelated `EMTR` particle-emitter version dispatcher at `0x007B9B90`; it is not evidence of shader support. Runtime reachability/effective overlay analysis remains required. This mismatch is now a first-class original-asset compatibility risk.

VTXA versions 0001-0003 use bitfield declarations for position, normal, packed color/scalar channels, and up to 15 texture-coordinate sets with 1-4 float dimensions. Exact naming of every auxiliary channel and topology enumeration remains open. Installed corpus counts show 40,839 of 41,016 raw MSH occurrences use modern 0004/0005 MESH with SPS 0001 and VTXA 0003; 171 records require explicit anomaly/alternate-structure review.
## 2026-09-11 — persistent Ghidra and replication-view pass

Installed official Ghidra 12.1.3 locally under the non-Git research toolchain and verified archive SHA-256 `93A5D11A9AD510622ACAAF908C556A7B9B764D338E78A7567F3689BF5081FD54`. Headless analysis of the established Stardust.exe hash completed in 2,170 seconds and saved a persistent project. A reproducible 190-address name set imported 162 entries as existing functions and 28 as labels. Targeted decompiler and xref scripts now preserve pseudocode, function boundaries and constructor/vtable evidence.

The common six-view registry, view-number/offset mapping, registration helpers and `AutoDeltaByteStream` vtable were recovered. Baseline declared counts are read but ignored in favor of the client registry; delta declared counts are read but iteration is bounded by payload exhaustion; deferred callbacks commit after delta application. Constructor call order now bounds class-specific field additions for ClientObject, IntangibleObject, TangibleObject, CreatureObject and PlayerObject.

The PlayerObject view-8 and view-9 waypoint maps were separated and both callback triplets traced. PLAY:8 mutations create/update/remove `ClientWaypointObject` presentation state and publish `WaypointsChanged`; the second PLAY:9 map has a parallel guarded path but no confirmed deployed Stardust content. This closes the previously inferred waypoint-value-to-presentation bridge while leaving the secondary map's purpose open.

## 2026-09-11 transport creation and remapping policy

Instruction-level verification resolves the outbound establishment-timeout argument. `game_connection_base_ctor_VA_012B6150` pushes address, port and zero into wrapper `0x012BCDA0`. Wrapper disassembly forwards all three arguments to manager factory `0x012C1990`; the final value reaches `outbound_udp_connection_ctor_VA_012C2810` and live offset `+0xF8`. Normal game UDP attempts therefore disable the state-0 deadline while retaining retransmission.

The options byte at `+0x86`, populated by `SharedNetwork.useTcp`, selects materially different implementations. Zero constructs the UDP manager and outbound SOE connection. Nonzero constructs the stream object at `0x012BE1C0`; imports prove AF_INET/SOCK_STREAM TCP, nonblocking connect, `TCP_NODELAY`, close-event notification and IOCP creation. The loose deployed CFG leaves the executable default true untouched. Stardust's inspected login and zone implementations create datagram services, so actual deployment topology remains unknown.

`udp_manager_packet_dispatch_VA_012C2190` handles endpoint lookup and authenticated remapping. For opcode `0x001E`, it extracts a connection ID, finds the existing connection, checks policy/source conditions, verifies the negotiated token, removes the old endpoint-hash node, replaces address/port from datagram metadata, recomputes the hash and reinserts it.

Evidence-scoped close labels are now: `2` no-data timeout, `5` establishment timeout, `9` session-response mismatch, `10` unaccepted inbound connection, `11/12` invalid session-request identity/state responses, and `13` reliable pending-byte overflow. These are provisional behavioral labels, not recovered original enum names.

## 2026-09-11 TCP framing and configuration precedence

`tcp_frame_queue_VA_012BE8F0` writes a native U32 payload size then payload bytes into a retained send buffer. On x86 this is a little-endian four-byte prefix. It triggers `tcp_flush_send_buffer_VA_012BE5D0` once queued bytes reach `tcpMinimumFrame` (1000); the shared connection pump also flushes pending buffers. IOCP operation tags are 2/send and 3/receive.

Receive completion `0x012BE7F0` posts another overlapped receive and feeds completed bytes to `tcp_receive_frame_reassembler_VA_012B7840`. The reassembler retains partial four-byte headers and partial bodies, dispatches exact complete payloads through `received_frame_dispatch_VA_012B4120`, and loops over coalesced frames. It subtracts already accumulated body bytes from the declared U32 length but shows no explicit maximum or underflow guard at this layer. This is a confirmed validation risk.

Startup constructs a SharedFoundation setup record whose config filename is `swgemu.cfg` (`test.cfg` only for a `testswgclient` executable path). `0x00A9F970` initializes ConfigFile, loads that root through `config_file_load_VA_00A9C780`, then processes the command line. `.include` directives recurse immediately; the installed root orders `swgemu_login.cfg`, `swgemu_live.cfg`, `swgemu_preload.cfg`, `options.cfg`, then `user.cfg`. Key values append, and scalar getters retrieve the last indexed value. Later includes and later command-line assignments therefore win. Command-line `@file` loads another late file and `-s` selects a section. No installed loose include sets `useTcp`.

## 2026-09-12 PASS render-state and effective-overlay continuation

Backend `0x62A0B340` maps PASS fields to literal D3D9 states covering shade, depth, blend, alpha test, color writes, and complete one-/two-sided stencil behavior. `0x62A0E570` compiles max-anisotropy, address U/V/W and mip/min/mag sampler states. A corrected overlay scanner honored all 48 configured TRE priorities and tombstones: 23,393 winning SHT/EFT names, 23,286 live, 107 tombstones, zero parse errors. Twenty-seven live winners use IMPL 0009/PASS 0010; raw IMPL 0008 records are shadowed. Namespace visibility is confirmed; session-level requests remain unproven.

## 2026-09-12 effective PASS 0010 / IMPL 0009 grammar and generated vertex variants

The configured-overlay inventory now records every implementation/pass DATA payload and child signature. Across 23,286 live winners, all 82 IMPL 0009 records use seven-byte DATA and the same mandatory-SCAP, optional-OPTN, counted-PASS envelope as supported IMPL 0007. All 108 PASS 0010 records use 57-byte DATA and retain the PFFP/STAG or PVSH/PPSH alternatives used by PASS 0009. The extra PASS byte lies in the early scalar prefix before the first U32 tag. Its exact ordinal and meaning remain unknown.

No affected winning asset has an older same-name live definition in lower-priority configured archives, so version differencing cannot resolve the new scalar. A replacement decoder can preserve it losslessly but must not assign semantics yet.

Raster function `0x62A15070` is now classified as a packed texture-coordinate-map vertex-shader cache, not the STAG color/alpha combiner. Cache misses call `0x62A14740`, which constructs texture-coordinate macros/declarations, selects vs_1_1 or vs_2_0 by capability, invokes the D3DX compiler, and creates an IDirect3DVertexShader9. This explains the programmable vertex half of the PFFP fallback path while leaving color/alpha operation translation open.

## 2026-09-12 fixed-function STAG schema and inactive backend path

The effective shader inventory now captures raw STAG version chunks. It found 951 STAG 0000 records, all 33 bytes, with 128 unique payloads; no winning STAG 0001 record exists. The v0000 loader order matches the fixed-function color/alpha operation and argument pattern, followed by two parameter tags, address U/V/W, three filter overrides and one unknown trailing byte. Corpus defaults/ranges support D3D-style operation values offset by one and address/filter inherited sentinels 5/6.

Backend initialization `0x62A0C2C0` sets D3DTSS defaults for all relevant classic states. `0x62A0B230` applies cached stage-state pairs through device slot `+0x10C` (SetTextureStageState), but it has no Ghidra code/data xref and its absolute address is absent as a stored pointer in the DLL. The active factory `0x62A04D40` instead constructs programmable implementation compiler `0x62A0BAA0/0x62A0B340`. The evidence therefore supports STAG as a required asset/fallback contract but not as an active D3DTSS execution route in this deployed backend.
# 2026-09-12 — StaticShader texture parameter and TXM binding path

- `0x00780140` and `0x00780A60` parse the SSHT MATS/TXMS/TCSS parameter families; TXMS delegates each `TXM ` form to `0x007813E0`.
- TXM loaders `0x00781470`, `0x00781720`, and `0x00781A20` establish versions 0000/0001/0002. Their DATA sizes are 5, 11 and 12 bytes respectively, apart from eleven v0001 corpus records with one tolerated trailing zero.
- Texture records are tag-keyed. The v0001/v0002 bytes are tag, special flag, address U/V/W, mip/min/mag filter, with v0002 adding maximum anisotropy. v0000 uses sampler sentinels/defaults.
- `0x00764F90 -> 0x00764F20` reads optional NAME, constructs a TemporaryCrcString and fetches the named texture through the central cache.
- Exported `StaticShader::getTextureData` at `0x0076AEF0` copies a 36-byte tag value. `getTextureCoordinateSet` at `0x0076AF80` independently returns the U8 coordinate set. These close the two STAG tag roles.
- s207 sampler table dumps map address values 0-4 to D3D values 1-5 and filter values 0-3 to 0-3. Inheritance sentinel 5 for address ultimately falls back to WRAP; sentinel 6 for filter falls back to LINEAR.
- s207 `0x62A12380` classifies ENVM and underscore-prefixed tags as global dynamic textures. `0x62A12340` finds the mutable registry slot; `0x62A123B0` inserts/replaces it with balanced Texture fetch/release ownership.
- Effective configured corpus: 41,741 TXM records, 45 tags. Top identities are MAIN 22,893; SPEC 4,975; NRML 2,936; CNRM 2,806; LKUP 2,657; ENVM 1,435; DETA 1,379.

Conclusion: original SHT assets express texture identity, resource source, sampler policy and coordinate-set selection as separable concepts. Preserve that separation in the renderer-neutral material IR.

# 2026-09-12 — DDS resource creation and upload

- `TextureList` uses a mutex-protected CRC-string map, while individual Texture fetch/release counters are plain non-atomic increments/decrements.
- `0x00769170` reads DDS magic plus the 124-byte header from TreeFile, recognizes 2D/cubemap topology, clamps mip discard, selects internal TextureFormat and falls back to the configured default texture on failure.
- Recognized compressed FourCCs are DXT1-DXT5 and PNM1. The current effective corpus uses DXT1, DXT3 and DXT5 only.
- `ClientGraphics.discardHighestMipMapLevels` and `discardHighestNormalMipMapLevels` are loaded at `0x00779A50`; `_n.dds` resources take the greater setting.
- Graphics slot +0x114 reaches s207 `0x62A12510`, which tries candidate formats and creates D3D9 managed-pool 2D or cube textures. `0x62A127F0/0x62A12A70` implement lock/unlock and staging-surface conversion.
- `0x00768FE0` uploads retained levels with correct block geometry and skips discarded/unsupported levels without decoding them.
- Effective overlay inventory: 30,109 `.dds` winners; 29,903 valid DDS, 203 tombstones and three SSHT payloads misnamed `.dds`. Valid set: DXT5 21,673; DXT1 4,874; BGRA32 2,325; DXT3 910; BGR24 113; A1R5G5B5 4; L8 4. Cubemaps 174; NPOT textures 8; non-full declared mip chains 1,668.

Replacement consequence: retain DDS bytes or an asset key for explicit GPU re-creation; never model D3D9 managed-pool behavior or 32-bit Texture pointers as the compatibility boundary.

# 2026-09-12 — procedural textures, render-target copy and reset lifecycle

- Executable factories `0x00765000`, `0x00765070`, `0x007650E0` construct solid-color, CPU-uploaded and empty procedural/renderable Texture objects respectively.
- Direct empty-texture users include the bloom/downsample/blur chain, dynamic normal-map test, terrain/environment resources and UI/radar canvases.
- s207 `0x62A0AD10` saves the original RT0/depth surfaces and creates fixed 512x512 A8R8G8B8 render-target and system-memory scratch textures.
- `0x62A0AF40` binds a selected texture subresource as RT0; `0x62A0B120` performs `GetRenderTargetData` plus `D3DXLoadSurfaceFromSurface`, releases temporary surfaces and restores the prior bindings via `0x62A0AED0`.
- `0x62A0AE30` is called by pre-reset resource release `0x62A03520`; paired post-reset `0x62A03560` recreates resource families and invokes registered callbacks.
- The earlier `0x62A110C0..0x62A11F60` hypothesis was rejected: that region is compiled-PASS vector and vertex-buffer logic, not this render-target service.

Replacement consequence: expose renderer-neutral renderable-image/subresource/copy/readback operations and explicit lifecycle callbacks; do not preserve global COM state or D3D9 pool behavior.

# 2026-09-12 — frame schedule and presentation

- Correct `GetApi` base `0x62B835B8` maps frame API offsets +0x80/+0x84/+0x88/+0x8C/+0x90 to raster frame-state, BeginScene, EndScene, default Present and destination Present entries.
- `frame_tick_VA_004237C0` performs network/simulation/scene updates before a guarded BeginScene -> IoWin render -> EndScene -> Present sequence; resource/input tail service follows presentation.
- Common s207 presenter `0x62A03E60` owns device-lost/reset recovery and calls paired pre-reset `0x62A03520` and post-reset `0x62A03560` routines.
- IoWinManager stack head virtual +8 is the outer composition call. GroundScene, CuiManager and transition windows are confirmed stack participants; GroundScene render is `0x0051B770`.
- ShaderPrimitiveSorter camera/environment begin/end routines are `0x00773B10`/`0x00773DE0`. Twelve installed phase indices constrain environment, terrain, opaque, alpha, water, shadow and final ordering.

Replacement consequence: use explicit frame and layer graphs while retaining the recovered semantic ordering and resource-recovery boundary.

# 2026-09-12 — GroundScene, bloom and CUI composition

- `0x0093B2B0` is the CuiIoWin render-stage candidate. It calls bloom begin, renders the next/lower IoWin through `0x00AB58E0`, resolves bloom, conditionally invokes GroundScene post rendering, renders CUI, then may draw a final screen overlay.
- `0x00AB58E0` directly invokes virtual `+8` on the pointer at IoWin `+8`, proving lower-layer recursion.
- Bloom install/resource/resolve functions recover the ClientGame controls, three shader assets, four render targets/tags, downsample, separable Gaussian blur, and final composite.
- GroundScene `0x0051B770` reaches the confirmed TerrainObject singleton and its attached appearance environment-color virtual. Main graphics wrappers reach s207 Clear and SetViewport. Geometry submission was subsequently resolved through camera callbacks.
- `0x00881210` is a full-screen CUI stage: it establishes viewport/state, draws the UI manager/root object, performs UI vertex cleanup and restores state. A separate optional world-marker/debug overlay follows within it.
- The visible frame order is now world -> bloom -> GroundScene post overlays -> CUI -> final fade/overlay -> EndScene/Present.

Replacement consequence: keep UI outside scene bloom and represent this sequence as explicit frame-graph/layer dependencies. Do not reproduce the IoWin linked list or legacy render-target implementation internally.

# 2026-09-12 — procedural terrain visibility and sorter submission

- RTTI descriptor `0x018791E8` resolves to `ClientProceduralTerrainAppearance` vtable `0x015E66B4`; constructor `0x00849350` writes it.
- Prior wrapper `0x00B5CB50` reaches vtable `+0x128 = 0x0084B770`, now proven to return environment/clear color rather than render geometry.
- Actual terrain drawing is integrated through callbacks registered by `0x00561060` around each camera draw in `0x00561180`.
- `0x0084AAA0` traverses terrain visibility nodes and dispatches selected renderables. `0x0084EDB0` builds bounded batches in `ClientTerrainSorter`; `0x0084F110` submits their primitives through `ShaderPrimitiveSorter`.
- Sorter installation allocates five category buckets and pools of 100 temporary node/vector objects. These counts are legacy policies, not asset contracts.
- Constructor evidence identifies separate ClientGlobalWaterManager2 and ClientLocalWaterManager ownership and terrain material dependencies on dot3 terrain, patterned blend shaders, cloud tiles and environment IFFs.

Replacement consequence: separate environment color from geometry; use asynchronous terrain renderables plus engine-native culling/batching while preserving original terrain material and global/local water semantics.

# 2026-09-12 — global and local water rendering

- RTTI resolves `ClientGlobalWaterManager2` vtable `0x015E6F6C` and `ClientLocalWaterManager` vtable `0x015E7434`, plus global static/dynamic and local shader-primitive classes.
- Both manager registration paths use literal sorter tag `WATR` (`0x57415452`). Local water frustum-tests each bounded primitive before registration.
- Global manager chooses static or dynamic planar implementations based on graphics capability. Static geometry exposes four independently drawn quadrants; dynamic geometry is regenerated relative to active camera state.
- Prepare methods derive above/below-water plane state, advance shader animation time, upload constants, bind shader and vertex/index buffers, then issue indexed geometry.
- No additional camera or render-target selection is present in the recovered paths, so planar reflection/refraction is not established. Shader-based environment reflection remains possible pending shader-asset decoding.

Replacement consequence: retain global-plane versus local-polygon semantics and WATR ordering; treat reflection/refraction as an optional enhancement unless later asset evidence proves it part of compatibility.

# 2026-09-12 — asynchronous terrain chunk lifecycle

- `ClientChunk` RTTI resolves vtable `0x015E731C`; `MemberFunctionThreadZero<ClientProceduralTerrainAppearance>` resolves vtable `0x015E66A8`.
- Constructor `0x00849350` binds worker callback `0x0085EE60`. The mutex/condition region is at appearance +0x20C/+0x224, stop byte at +0x230, pending ordered container at +0x234 and completion vector at +0x238.
- Work records are 20 bytes. Worker `0x0085EE60` removes one pending record under lock, appends a completion record, builds via `0x0084A530` outside the lock, then fills result pointer +0x10 under lock.
- `0x0085EFD0` is a main-thread three-state normal/drain/reconcile controller. Normal commit installs only into a still-vacant tree slot and destroys stale duplicates. Rebuild states drain completed objects, requeue retained requests and reset state only after reconciliation.
- `0x0085F950` starts region rebuild from a 16-byte region queue, cancels pending work and gathers intersecting four-way-tree descriptors. `0x0085F400` inserts filtered requests into the ordered worker queue and signals it.
- Destructor `0x00849FB0` sets the stop byte, signals, joins through `WaitForSingleObject(INFINITE)`, then clears requests and destroys uncommitted chunks.

Replacement consequence: use generation-tagged jobs and immutable CPU results, commit only on the main/engine thread after stale-result validation, and fence all zone-owned jobs before freeing terrain or assets.

# 2026-09-12 — terrain LOD, height bias and retained cache

- ConfigClientTerrain defaults recovered at `0x00844D90`: threshold 1.0, highLevelOfDetailThreshold 3.5, heightBiasFactor 8.0, heightBiasMax 50, heightBiasDisabled true and terrainMultiThreaded true.
- `0x0085E350` computes squared observer-to-node-AABB distance, subtracts a fine-detail radius, scales by the inverse configured threshold and optionally subtracts relief-derived height bias before clamping target scale to at least 1.
- `0x0085E620` splits when target scale is finer than node scale and collapses otherwise. `0x0085E710` reconciles four neighbor-edge bits and calls `0x00861870` to propagate the seam mask into all chunk render primitives.
- `0x0084A960` reruns LOD after integer observer-coordinate changes, a controller configuration change or incomplete prior traversal. No separate split/merge thresholds were observed.
- `0x00862980` activates the chunk's render-world object; `0x00862A10` deactivates it. Normal coarsening retains ClientChunk and child-node allocations rather than evicting them.
- The work record is X, Z, scale, frame-relative order value and result pointer. `0x00754D80` supplies the global frame counter used by the fourth field.
- Correction: virtual `0x0085F820` queues a 16-byte dirty terrain region. `0x0085F950` regenerates resident chunks intersecting it; this is distinct from camera-driven LOD.

Replacement consequence: preserve deterministic quadtree identity, configurable distance behavior and seam relationships, but use a bounded modern cache if desired. Dirty terrain invalidation must remain distinct from residency eviction.

# 2026-09-12 — ClientChunk mesh, material blending and seams

- `0x008618D0` converts generated arrays into ClientChunk-owned grids and pooled 0x148-byte per-cell primitives grouped by 0x10-byte ShaderSets.
- Each primitive contains nine positions, nine normal-like vectors, nine packed values, bounds, a shader reference, a selected index list and eight triangle planes. Vertex 0 is the center; 1..8 form the perimeter.
- `0x008612B0` samples a 3x3 terrain-classification neighborhood. `0x008654F0` caches one-to-four-layer material combinations and constructs `shader/terrain_%sblend%i.sht` using `TERR`, `LVL0..3`, `NRM0..3` and `ALP1..3` parameters.
- `0x00865FF0` selects one of nine embedded 16-bit index patterns. Counts are 24/21/18 indices for the normal fan, one collapsed edge, or two adjacent collapsed edges. Seam updates change indices and triangle planes, not positions.
- `0x008668E0` appends visible cells into per-shader CPU vertex/index arrays. ClientTerrainSorter and ShaderPrimitiveSorter own the later GPU handoff.
- ClientChunk separately creates real-grid or eight-vertex proxy occlusion geometry according to `useOcclusion` and `useRealGeometryForOcclusion`.

Replacement consequence: retain deterministic generation, material layering and crack-free LOD, but expose renderer-neutral mesh/material data. The legacy cell granularity, pools, pointer layouts and D3D9 upload strategy need not be reproduced.

# 2026-09-12 — terrain collision and surface queries

- `0x0084BDF0` traverses the procedural terrain four-way tree for segment collision and invokes a resident leaf object's collision virtual when enabled.
- ClientChunk collision virtual `0x00860F60` rejects against the chunk extent and visits every material ShaderSet.
- `0x008662E0` tests the current stitched index list and triangle planes, retaining the nearest hit point, normal and Euclidean distance.
- ClientChunk virtuals `0x00860FF0` and `0x00861140` cast vertically through chunk Y bounds at a supplied X/Z, returning height and optionally the surface normal through `0x00866550`.
- Collision and vertical queries use visible cell primitives; neither references the optional render-world occlusion model at ClientChunk `+0xDC`.
- Coordinate-indexed grid access at `0x00862720`/`0x008628E0` is structurally confirmed, but exact flora/radial/environment labels remain withheld pending stronger evidence.
- Stardust server source independently evaluates PTAT/TGEN height and uses it authoritatively, so replacement visual collision must agree with—not replace—the server result.

Replacement consequence: an engine BVH or heightfield is suitable, provided it is generated deterministically from compatible terrain data, remains synchronized with seams and is not confused with occlusion geometry.

# 2026-09-12 — auxiliary terrain grids and flora runtime

- Exact constructor labels connect `+0xC8` to `FloraStaticNonCollidable`, `+0xCC` to `FloraDynamicNear`, and `+0xD0` to `FloraDynamicFar`; `+0xA4` is the shared static-collidable flora channel.
- `+0xC4` supplies three packed color bytes to both dynamic flora queries; `+0xD4` is a one-byte environment-family selection; `+0xD8` contains surface normals.
- Dynamic packed flora samples contain family ID plus a normalized weighted-child selection byte.
- Server source names WMAP as waterMap and SMAP as seedMap but has no runtime consumer. No direct FourCC instruction reference was found in Stardust.exe; client use remains unproved.
- RTTI resolves ClientStaticRadialFloraManager vtable `0x015E64C8`, ClientDynamicRadialFloraManager `0x015E6FFC`, ClientRadialFloraManager `0x015E704C`, and FloraManager `0x015ED0E4`.
- Client terrain owns one static-noncollidable and two dynamic near/far managers. Dynamic rendering material-batches up to 500 quads per primitive using a shared 3,000-index buffer.
- Camera-relative updates manage radial residency and fading; dynamic records support normalized color and optional time-driven sinusoidal motion.

Replacement consequence: publish typed terrain-generation channels and use engine-native instancing, while preserving category separation, deterministic selection, transformations, color and distance policies.
# 2026-09-22 — INLY/0000 interior-layout runtime

- `client_interior_layout_manager_install_VA_0059B500` registers the manager and reads `ClientGame.disableLazyInteriorLayoutCreation`; this is distinct from default-true `useInteriorLayoutFiles`.
- `client_interior_layout_manager_apply_pending_world_layouts_VA_0059B5A0` is called from the normal frame update and scans eligible world buildings when lazy creation is enabled.
- `client_interior_layout_manager_apply_building_layout_VA_0059B7B0` resolves node cell names, creates object templates, binds them to building cells, copies authored 3x4 transforms, inserts the local objects into the world, and skips failed nodes independently.
- Complete configured-overlay parsing found 297 effective `INLY/0000` winners, one tombstone, 87,937 nodes, 2,278 unique object templates, 664 cell names and zero parse failures. The former 411 count is raw archive copies.

# 2026-09-22 — CMPA component-appearance composition

- `component_appearance_template_load_VA_00798E40` accepts CMPA 0001-0005. Deployed effective winners are 13 v0003, six v0004 and 146 v0005.
- Common `PART` records resolve an appearance-template path and retain twelve little-endian F32 transform values in a 0x34-byte pointer-plus-transform record.
- All 165 effective CMPA winners parse without error: 779 children comprising 640 LOD, 87 particle, 26 mesh, 16 sprite and 10 nested component references.
- `component_appearance_construct_VA_007A0320` instantiates/owns child objects and appearances, creates a parent visibility object and attaches each child's visibility object below it. Runtime operations compose per-part transforms and fan out across children.
- v0005 `RADR/INFO` is a U32-gated auxiliary runtime object. Its exact historical semantic label remains unknown and is intentionally not invented.
- Namespace-normalized dependency auditing found ten nested CMP edges, zero cycles and eight unresolved legacy laser-bolt mesh parts. The affected components retain sprite children; child resolution failure is therefore a bounded part-local compatibility condition.

# 2026-09-22 — SPRT/QUAD v0000 appearance

- `sprite_quad_template_install_VA_0079B8B0` registers both `SPRT` and `QUAD` to the same factory and unregisters both together.
- `sprite_quad_template_load_v0000_VA_0079BC50` reads center/offset Vector3, radius/half-size F32, RGBA Vector4 and a shader-template path.
- Configured-overlay parsing found 469 effective winners (369 QUAD, 100 SPRT), 385 shaders and zero parse failures. All deployed colors are white; geometry centers/sizes vary.
- `sprite_quad_appearance_construct_VA_00789AF0` applies `sqrt(2)` size conversion, creates the shader-backed quad primitive and render/visibility object, and exposes the primitive bounds.

# 2026-09-22 — BEAM/0000 dormant compatibility family

- Stardust.exe registers BEAM at `0x00798240`; `0x00798460` accepts only version 0000.
- `0x00798540` decodes two little-endian F32 dimensions, a Vector4 and shader-template path. Runtime use establishes the two scalar fields as geometry dimensions.
- `0x00792EC0 -> 0x007928D0` owns a shader-backed four-vertex primitive, regenerates geometry and derives a bound as `sqrt(dimension0^2 + dimension1^2)`.
- The Vector4 is passed to shader-instance creation, but its historical label is not established. It remains intentionally opaque.
- No configured effective appearance winner has outer tag BEAM and no `.beam` namespace winner exists. This is supported dormant code, so its parser contract is retained without invented corpus ranges.

# 2026-09-22 — LEFX lightning appearance

- `0x007B3570` registers LEFX; `0x007B3630` dispatches versions 0000, 0001 and 0002 to `0x007B37C0`, `0x007B3B80` and `0x007B3E90`.
- All versions embed PTEX/0000 and a final 0000 scalar/string chunk; 0001/0002 additionally embed two WVFM channels.
- Overlay-aware parsing found 49 live `.ltn` assets, no tombstones and zero errors: two v0000, six v0001 and 41 v0002.
- Three strings are start appearance, end appearance and optional sound. Corpus positions confirm `.prt` start/end naming; only tractor beam supplies the third `.snd`.
- V0000 has 16 F32 values; v0001 has 16 F32, two U32 attachment-transform flags and one discarded legacy byte; v0002 has 19 F32 and the two flags. Runtime tests at `0x007AB412` and `0x007AB4AA` gate full transform/orientation handling for start/end attachment objects. Every deployed legacy byte is zero. Unknown scalar labels remain ordered typed fields.
- `0x007A9BE0` constructs a local shader primitive with five fixed branch records. Rendering samples template channels and allocates dynamic geometry proportional to active branches.
