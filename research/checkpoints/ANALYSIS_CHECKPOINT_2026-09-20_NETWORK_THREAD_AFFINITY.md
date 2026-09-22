# Network thread policy and state-mutation affinity

Date: 2026-09-20  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The ordinary Stardust client configuration does not mutate game state directly from a socket worker. `SharedNetwork/useNetworkThread` is read at `0x012BBCD2` with default false. `SharedNetwork/useTcp` defaults true. Policy function `0x012BB870` explicitly detects the unsupported combination and logs `useNetworkThread is unsupported when useTcp is enabled. useNetworkThread has been disabled.`, then clears the network-thread flag.

Independently of transport choice, complete application frames enter `received_frame_dispatch_VA_012B4120`, which copies the frame and retains its connection in a pooled 0x20-byte work item. The only queue drain is `network_received_message_dispatch_loop_VA_012B3B80`. That drain is called by `game_network_dispatch_update_VA_00462BC0` from the frame-update path. Named SWG message construction, listener publication and object/UI state mutations therefore occur on the game/frame owner in the default configuration.

The transport layer may still use asynchronous Win32 completion and optionally a UDP network thread, but its output crosses the copied-frame queue before named-message dispatch. TCP forbids the optional network thread in this build. Connection-open/close notification production is less completely localized than payload dispatch, so a replacement should make all session-state notifications obey the same main-thread handoff instead of relying on legacy callback affinity.

## Replacement requirements

- Socket/I/O workers may parse only transport framing and immutable packet bytes.
- Queue complete owned application frames to the game/session owner.
- Perform named-message decode, replication commit and transceiver/domain-event publication serially on that owner unless a future subsystem explicitly proves safe parallelism.
- Bound queue bytes/items and disconnect or apply backpressure on overflow.
- Keep Archive decode failure local to the connection; do not reproduce the legacy rethrow/process-termination behavior.
- Treat connection-open/close events as queued session events in the modern architecture.

## Confidence

- Configuration defaults and TCP/network-thread exclusion: **Confirmed by executable code and strings**.
- Single frame producer/consumer queue and frame-update drain: **Confirmed by executable xrefs**.
- Named-message/game-state mutation on frame owner in default configuration: **Confirmed**.
- Exact callback thread for every legacy connection-open/close notification: **Incomplete; replacement policy stated explicitly**.

## Stable provisional names

| VA | Name |
|---:|---|
| `0x012BB870` | `shared_network_enforce_thread_policy_VA_012BB870` |
| `0x012BBA30` | `shared_network_config_install_VA_012BBA30` |
