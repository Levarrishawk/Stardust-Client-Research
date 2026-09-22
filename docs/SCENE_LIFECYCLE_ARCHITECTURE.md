# Scene Lifecycle and Transition Safety

## Confirmed transition path

`CmdStartScene` reaches `game_network_cmd_start_scene_handler_VA_00462E60`, which passes terrain, player template/ID, X/Z/Y location, galactic time and the leading scene byte into `scene_transition_begin_VA_00462AE0`.

The transition function first obtains the current global `Scene` (`0x0190885C`) and uses MSVC RTTI to test it as `GroundScene`. If present, it invokes a ground-scene preparation/cleanup method (`0x00AB5900`), destroys the old instance, and sets the GameNetwork scene-entry intent flag through `0x00462CF0`. It then allocates `0xF4` bytes, constructs the new scene through `ground_scene_ctor_VA_00519C40`, and hands the result to `game_set_scene_VA_00424220`. Failure emits `Could not load scene %s`. The ownership replacement and concrete RTTI identities are **Confirmed**; the exact semantic name of `0x00AB5900` is still Strong.

`game_set_scene_VA_00424220` is more than a pointer assignment. It clears prior scene state, stores the new scene, rebuilds scene-name/state strings, publishes scene-change notifications, updates environment/global state and toggles downstream systems based on whether a scene exists. Process shutdown registers `game_cleanup_scene_VA_00423700`, which calls the current scene virtual cleanup slot and then `game_set_scene_VA_00424220(nullptr)`.

## Readiness protocol

```mermaid
sequenceDiagram
    participant Z as Stardust server
    participant N as Network/session
    participant S as Scene generation N
    participant R as Replication staging
    Z->>N: CmdStartScene
    N->>S: retire old GroundScene; construct new scene
    S->>S: minimum terrain/resource readiness
    Z-->>R: creates/baselines may begin
    S->>N: readiness predicates satisfied
    N->>Z: CmdSceneReady
    R->>S: ordered commit for generation N
    Z->>N: final CmdSceneReady
    N->>S: enter ready/post-load state
```

The original sends the first client `CmdSceneReady` only from `scene_ready_ack_trigger_VA_0051B428` after a conjunction of predicates and protects it with a one-shot flag. A later inbound message of the same name reaches `server_scene_ready_dispatch_VA_0051C206`. Server source independently warns that create delivery in the same frame as `CmdStartScene` can crash the old client because scene initialization is asynchronous.

## Replacement invariant

Each transition must own a monotonically increasing scene-generation token. Network decode can continue, but world-mutating messages must be tagged/staged for the intended generation until the minimum scene barrier opens. Old work follows its ownership scope: the scene-owned terrain worker is stopped, signalled and joined during terrain destruction; process-global asynchronous asset work may finish, but exact callback/context cancellation suppresses publication and cleanup remains unconditional; file-streamer requests finish synchronously for their request owner. GPU commits, object registration and callbacks in the replacement must verify generation before mutation.

Recommended state machine:

```text
NoScene -> RetiringOld -> Constructing -> MinimumReady
        -> AckSent -> StreamingObjects -> ServerReady -> Active
                         \-> Failed/Disconnected -> RetiringOld
```

Scene object destruction remains distinct from whole-scene retirement. `SceneDestroyObject` maps through `0x00AD55D0` and a guarded handler region at `0x0051BDFD`; hyperspace selects a specialized disappearance path. Whole-scene replacement must invalidate the registry in bulk without synthesizing individual wire destroys.

## Engine constraint

An engine integration must permit deterministic scene-generation ownership, staged replication, cancellation/discard of background resource results and headless protocol operation. If an engine's scene-tree replacement implicitly frees objects while callbacks or worker completions can still address them, the integration layer must enforce this generation barrier outside engine nodes.

## Bounded unknowns

- Names of every predicate in the client readiness conjunction.
- Historical names and exact call sites for every scene owner's async-loader cancellation request.
- Exact ordering of every scene-change subscriber.
- Failure recovery after partial terrain/resource initialization.

These details affect robustness tests but no longer change the required replacement architecture: generation-scoped ownership and an explicit readiness barrier are mandatory.
