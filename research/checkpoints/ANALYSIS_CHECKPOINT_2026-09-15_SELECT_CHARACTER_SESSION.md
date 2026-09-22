# SelectCharacter Session Establishment

Date: 2026-09-15  
Authority: `C:\Stardust-DEV\Stardust.exe` and current Stardust server source  
Scope: avatar selection through initial scene-ready acknowledgement

## Result

The `SelectCharacter` request and server `SelectCharacterCallback` are now closed as the transition from an authenticated zone connection to an owned in-world player/session. The request itself is intentionally small; most complexity is server-side ownership validation, reconnect displacement, parent restoration and ordered scene bootstrap.

This path belongs after login-server character enumeration and zone-server authentication. It is not part of the UDP connection handshake.

## Exact request

`Stardust.exe` sends:

```text
U16 operand count = 2
U32 opcode = 0xB5098D76  // hash("SelectCharacter")
U64 selected character object ID
```

The executable path is:

```text
avatar-selection update 0x00C62970
  -> selected-row/object-ID resolution
  -> constructor 0x00AE4B60
  -> reliable game-network send 0x00462670 at 0x00C62BCA
  -> enter waiting/progress state
```

The current repository helper class appends `hash("SWGEmu")` after the U64 despite retaining operand count 2. The original executable does not emit this trailing word. Current server `SelectCharacterCallback::parse` reads the U64 directly at absolute packet offset 6 and does not consume or validate trailing bytes, so both forms are presently tolerated. A replacement client should emit the exact original 14-byte form unless Stardust introduces a separately versioned extension.

## Client wait state

After sending, the avatar-selection mediator:

- enables a pending/wait flag;
- resets elapsed time to zero;
- activates the connection/progress presentation;
- switches game/session selection state; and
- accumulates frame delta in `0x00C62970`.

The `[ClientGame] connectionTimeout` value is a floating-point selection/session timeout, defaulting to 600 seconds. Expiry clears the pending state and presents failure. It is separate from lower UDP connection and no-data timers.

## Server admission gates

The callback runs on `slowQueue`, reads the U64 character ID and rejects before ownership attachment when:

- the zone server is loading or shutting down;
- the authenticated session does not own that character in the current galaxy;
- the object cannot be loaded or is not a player creature;
- the server population cap is reached for a non-administrator;
- the server is locked for a non-administrator;
- optional external session approval rejects the connection;
- the saved terrain/zone is disabled; or
- the account already has the configured maximum simultaneous characters online.

Failures use `ErrorMessage` and do not proceed into scene bootstrap. If the same character is owned by another session, the old session is disconnected and an ordered disconnect task completes before the new session is attached.

## Ownership and movement initialization

On approval the server binds both directions:

```text
CreatureObject.client = ZoneClientSession
ZoneClientSession.player = CreatureObject
```

It sets teleporting, resets movement counter and client movement timestamp, and marks load-screen state when the creature is not currently in a zone. These gates prevent ordinary movement validation from running during reconstruction.

## Placement restoration

The callback distinguishes:

1. player already attached to a ship, chair, turret or POB hierarchy;
2. saved/current parent is an interior cell;
3. player remains linked-dead on a vehicle;
4. ordinary terrain placement; and
5. an orphan persisted directly in a space zone without recoverable ship containment.

Valid ship and cell parents are restored. Missing interior roots fall back to the saved world position with terrain-floor resolution. Shipless space orphans are moved to their stored ground launch zone/location when valid. Ordinary placement clears structure skill modifiers and space states before transfer. Saved parent ID is cleared when no parent survives.

## Scene bootstrap order

`CreatureObject::sendToOwner` resets movement state and sends, in order:

```text
unknown byte flag
CmdStartScene
ParametersMessage
Guild baselines
root parent or player create/baselines/containment/end-baselines
nearby objects
group state
```

`CmdStartScene` supplies player ID, `terrain/<zone>.trn`, world position in x/z/y order, player client-template path and galactic time. Parent-relative local placement arrives through containment/transform messages in the subsequent object stream.

For ship/POB entry the server deliberately ensures `CmdStartScene` precedes dependent object creates. Existing comments and split-send logic record that sending ship objects in the same processing phase before the client completes its scene reset can crash the original client. A replacement should preserve logical ordering but can replace the historical fixed delay with an explicit internal scene-readiness barrier.

## Ready handshake

When minimum client scene initialization completes, `Stardust.exe` sends empty `CmdSceneReady` (`0x43FD1C22`). Server `CmdSceneReadyCallback` resolves the session player and calls `PlayerObject::notifySceneReady`, which:

- clears teleporting, load-screen and forced-transform flags;
- echoes `CmdSceneReady` to the client;
- broadcasts PvP state and sends buffs;
- sends friend/ignore state;
- restores clone UI when required;
- joins guild/current-planet/persistent chat rooms;
- presents terms of service when applicable;
- sends ground weather and may create the helper droid.

The echoed ready message is the clean completion boundary for a replacement session state machine. Object availability can continue to evolve afterward, so it is not proof that every asset is loaded.

## Minimal replacement-client state machine

```text
ZoneAuthenticated
  -> send exact SelectCharacter(U64)
  -> SelectingCharacter [timeout domain: ClientGame/session]
  -> receive CmdStartScene
  -> ResetScene + begin terrain/resource load
  -> consume object create/containment/baselines/end-baselines stream
  -> minimum scene dependencies ready
  -> send CmdSceneReady
  -> receive CmdSceneReady echo
  -> InWorld
```

For the networking-only proof of concept, stopping after parsing `CmdStartScene` is sufficient. Sending `CmdSceneReady` should be deferred until the proof client can safely consume the ensuing baseline and social/weather traffic, otherwise the server will legitimately advance into a much wider protocol surface.

## x64 and safety rules

- Character and parent/object identifiers stay U64 on the wire and in protocol-domain types.
- Do not cast IDs to pointers or `size_t`.
- The request has a fixed 14-byte original form; native struct padding is forbidden.
- Session ownership comes from the authenticated connection, not the U64 request alone.
- Keep connection, selection and scene-readiness timeouts separate.
- Make scene bootstrap ordering explicit and queue decoded messages while asynchronous assets load.

## Confidence

- Confirmed: request bytes, client constructor/send/wait path, server gates, ownership binding, placement branches, scene send order and ready handshake.
- Strongly inferred: exact user-facing purpose of several client state toggles immediately after send.
- Unknown: whether any deployment depends on the repository helper's trailing `SWGEmu` hash; the current callback does not inspect it.

## Next bounded block

Analyze the high-reference ship view-3 baseline or complete the remaining `ClientIdMessageCallback` authentication-side row before expanding the proof-of-concept specification.
