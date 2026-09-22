# Analysis Checkpoint: Login/Zone Callbacks and Dispatch Boundary

Date: 2026-09-11  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope completed

This block traced complete inbound frames from transport callbacks to application handlers, and traced transport-open/close callbacks into Login and ConnectionServer state changes. It used the executable as primary evidence and correlated the resulting session order with Stardust server source. No live endpoint was contacted.

## Confirmed findings

- `received_frame_queue_producer_VA_012B4120` copies complete payloads plus their connection reference into a block-backed queue of 0x20-byte records. It does not parse SWG messages.
- Direct data xrefs show the queue globals are shared only by producer `0x012B4120` and consumer `network_received_message_dispatch_loop_VA_012B3B80`.
- `game_network_dispatch_update_VA_00462BC0` calls the consumer during frame/update processing. State-changing message handlers therefore execute after an explicit I/O-to-game-thread handoff.
- The consumer calls `connection_received_message_VA_012B79B0`, which performs receive accounting and invokes primary connection vtable slot `+0x28`. Login routes that slot to the generic named-message dispatcher; ConnectionServer first intercepts lag-control messages and forwards the rest.
- TCP opened completion `0x012BE7C0` marks connected, posts receive, flushes pending output and calls the common open callback. TCP closed completion `0x012BE7A0` clears connected and enters `NetworkHandler::onTerminate` at `0x012B4410`.
- Base connection callbacks `0x012B76A0` and `0x012B7520` bridge transport establishment/closure to derived application open/close virtuals.
- ConnectionServer opened publisher `0x0069B430` constructs and sends `ClientIdMsg`, publishes `ConnectionServerConnectionOpened`, and starts lag tracking.
- ConnectionServer closed publisher `0x0069B3D0` publishes `ConnectionServerConnectionClosed`; `0x004630C0` clears active cluster/connection state and broadcasts false.
- Login close application routine `0x00463350` clears the live login pointer and broadcasts `LoginServerConnectionChanged(false)`.
- ConnectionServer-open apply routine `0x00462D10` promotes the pending cluster name to active state and broadcasts successful cluster connection.

## Server correlation

The repository independently confirms the application sequence `LoginClientToken` -> cluster enumeration/status -> character enumeration, then zone `ClientIdMsg` -> `ClientPermissionsMessage` -> `SelectCharacter` -> `CmdStartScene` -> client `CmdSceneReady` -> replication stream -> server `CmdSceneReady`. The executable analysis adds the missing execution ownership: all named messages cross the queued GameNetwork update boundary before those handlers mutate state.

## Replacement-client implications

Use an I/O executor for socket work and a serialized session executor for protocol/state application. Queue immutable owned byte buffers and stable connection handles; never carry native pointers or legacy 0x20-byte queue layouts across the boundary. Model socket creation, transport establishment, authentication, permission, closing and closed as distinct states. `ClientIdMsg` is triggered only by the established callback.

## Remaining unknowns

- Full ordering of lower transport destruction helpers and deferred UDP teardown.
- Application authentication retry policy beyond transport session-request retries.
- Complete UI behavior for every login/cluster failure reason.
- Whether an unavailable command-line or packed configuration changes the installed `useTcp=true` default.

## Progress

The strict whole-client estimate advances from 40% to **41%**. Login/ConnectionServer/zone state-machine coverage advances from 75% to **82%**. The next highest-value block is the named/controller-message registry and replication dispatch, starting with `DeltasMessage` and controller opcode families.
