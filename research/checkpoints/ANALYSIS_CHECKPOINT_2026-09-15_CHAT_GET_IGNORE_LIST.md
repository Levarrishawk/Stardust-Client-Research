# Stardust.exe Analysis Checkpoint: Legacy Ignore-List Snapshot

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes Stardust's `ChatOnGetIgnoreList` packet (`0xF8C275B0`). This exact executable contains no named codec, RTTI object, opcode literal, listener registration, or adjacent-DLL implementation for it. The server sends this compressed legacy snapshot immediately before active `PlayerObject` view-9 replication; `PLAY:9[8]` is the authoritative structural ignore-name vector.

There is therefore no honest executable parser or handler to assign. This is a confirmed compatibility classification, not invented semantics.

## Confirmed server wire contract

| Order | Field | Wire representation |
|---:|---|---|
| 1 | owning creature ID | LE64 (`ghost->getParentID()`) |
| 2 | ignored-name count | LE32 |
| 3 | repeated identity | count × (`ASCII16("SWG")`, `ASCII16(galaxy)`, `ASCII16(name)`) |

The constructor begins with operand count 3 and the opcode, then enables compression. Compression belongs at the previously documented message/transport boundary, not in the application model.

## Evidence of executable absence

- Complete PE strings contain neither the message name nor an MSVC RTTI fragment.
- Neither little-endian opcode bytes nor an instruction immediate exist for `0xF8C275B0`.
- The recovered chat listener contains no registration/comparison for it.
- Neighboring EXE/DLL files under `C:\Stardust-DEV` contain no matching string.

The same methods recover supported chat message descriptors, parsers and handlers, making the negative result message-specific.

## Active state path

`PlayerObjectImplementation::sendFriendLists()` sends both legacy social snapshots, then a `PlayerObjectDeltaMessage9` containing friend and ignore vectors. The executable's `PlayerObject` registrar at `0x0065B800` establishes:

| View/index | Object offset | Client type | Meaning |
|---|---:|---|---|
| `PLAY:9[7]` | `+0x480` | `AutoDeltaVector<std::string, PlayerObject>` | friend names |
| `PLAY:9[8]` | `+0x4B8` | `AutoDeltaVector<std::string, PlayerObject>` | ignored names |

Server `addIgnore()` and `removeIgnore()` lower-case the name, mutate the vector and send an index-8 delta. Initial and continuing structural state therefore use the same replication mechanism.

CommunityManager also exposes thin script-dispatch wrappers: `getIgnoreList` at `0x005AA820`, `removeIgnore` at `0x005AA8B0`, and `addIgnore` at `0x005AA8E0`. These prove UI/API access, not a codec for the absent legacy packet.

Stardust enforces ignore state server-side across chat delivery and multiple direct-interaction paths. A replacement must retain that server authority; client filtering is only an additional presentation defense.

```text
sendFriendLists()
  +-- legacy snapshot 0xF8C275B0 -> no Stardust.exe consumer
  `-- PLAY:9[8] vector -> initial state and later add/remove deltas

CommunityManager/UI -> getIgnoreList / addIgnore / removeIgnore
```

## Packet-handler map

| Server message / opcode | Candidate client function | Parsed fields | Resulting action | Confidence | Evidence |
|---|---|---|---|---|---|
| `ChatOnGetIgnoreList` / `0xF8C275B0` (Z→C) | none; unregistered/ignored legacy message | owner U64, count U32, repeated game/galaxy/name ASCII triples | no direct effect; state arrives through `PLAY:9[8]` | Confirmed absence; schema confirmed | complete PE scans, adjacent-DLL scan, server constructor/send sequence |
| `PLAY:9[8]` ignore vector | generic baseline/delta engine; registrar `0x0065B800` | AutoDeltaVector string operations | maintain structural ignored-name vector | Confirmed principal path | client field registry and server initialization/add/remove producers |

## Replacement requirements

- Implement `PLAY:9[8]` baseline/delta operations as authoritative ignore membership.
- Preserve U64 owner IDs, U32 counts and original string framing; x64 pointers must never leak onto the wire.
- Preserve server lower-case normalization or equivalent normalized lookup keys.
- Explicitly ignore the legacy snapshot, matching this executable, or accept it only as an idempotent bootstrap source that yields authority to replication.
- Keep decompression outside the social-list model.
- Keep filtering authoritative on the server and defensive/presentational on the client.

## Confidence and boundary

- **Confirmed by code:** server schema/compression/producers; executable absence; view-9 index, offset and type; normalized add/remove deltas; CommunityManager wrappers.
- **Strongly inferred:** the legacy snapshot remains for other client generations.
- **Unknown:** its historical target revision and the complete observer chain from vector mutation into every UI view.

No DRM, licensing, authentication protection or access-control mechanism was examined or altered.

## Progress

One reachable packet moves to documented: **243 documented / 102 reachable gaps / 36 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
