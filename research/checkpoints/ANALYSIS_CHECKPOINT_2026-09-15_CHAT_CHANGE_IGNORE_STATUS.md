# Stardust.exe Analysis Checkpoint: Ignore-Status Notification

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes Stardust's `ChatOnChangeIgnoreStatus` message (`0x70E9DA0F`). Despite being sent on every successful server-side ignore add/remove, this exact executable contains no message name, RTTI class, opcode literal, descriptor, parser or listener branch for it. It is an unsupported legacy notification. Actual membership is delivered through `PlayerObject` replication field `PLAY:9[8]`; validation and user feedback use localized system messages.

## Confirmed server wire contract

`ChatOnChangeIgnoreStatus.h` serializes:

| Order | Field | Wire representation | Observed value |
|---:|---|---|---|
| 1 | owning creature ID | LE64 | caller's object ID |
| 2 | game | ASCII16 | `SWG` |
| 3 | galaxy | ASCII16 | configured galaxy name |
| 4 | character | ASCII16 | lower-case target name |
| 5 | reserved/result | LE32 | zero |
| 6 | add flag | U8/bool | one for add, zero for remove |
| 7 | reserved/request | LE32 | zero |

The header declares operand count `0x06`; replacement compatibility must reproduce the bytes rather than reinterpret that count from host-language field counts.

## Producer order and state authority

On successful `PlayerObjectImplementation::addIgnore(name, true)`:

1. lower-case the supplied name;
2. send this legacy notification with `add=true`;
3. add the name to the server ignore vector;
4. send the updated vector through `PlayerObjectDeltaMessage9`;
5. send localized `cmnty:ignore_added` feedback.

Removal follows the same order with `add=false`, vector removal, a view-9 delta and `cmnty:ignore_removed`. Removing a missing entry sends `cmnty:ignore_not_found` and returns without either change message. Adding a duplicate or invalid/self name is rejected earlier by `AddIgnoreCommand` with localized system feedback.

This ordering proves the unsupported named message cannot be the effective membership source for this client: the following `PLAY:9[8]` delta is the state-bearing operation.

## Executable evidence

- Full PE string scan: no `ChatOnChangeIgnoreStatus` name or RTTI.
- Full raw-data and executable-immediate scans: no `0x70E9DA0F`.
- Recovered chat listener: no registration or comparison branch.
- Adjacent executable/DLL scan under `C:\Stardust-DEV`: no class-name match.
- Positive control: CommunityManager retains `IgnoreListChanged` and `ShowIgnoreListMessage` transceiver RTTI, plus script operations `getIgnoreList`, `removeIgnore` and `addIgnore`.

The observer types belong to the replicated/UI state path and do not establish a hidden codec for the absent message.

## Client command path

The executable CommunityManager script wrappers are:

| Address | Provisional name | Role |
|---:|---|---|
| `0x005AA820` | `community_manager_script_get_ignore_list` | expose current ignore list to UI script |
| `0x005AA8B0` | `community_manager_script_remove_ignore` | expose remove operation |
| `0x005AA8E0` | `community_manager_script_add_ignore` | expose add operation |

Stardust receives add/remove as ordinary registered queue commands (`AddIgnoreCommand` and `RemoveIgnoreCommand`), not an ignore-specific callback packet. The exact generic client command enqueue function beneath the script interface remains outside this packet block; no invented address is assigned.

```text
Community UI operation
  `-- normal command-queue request -> AddIgnoreCommand / RemoveIgnoreCommand
       +-- validation failure -> localized system message only
       `-- success
            +-- legacy 0x70E9DA0F -> no Stardust.exe consumer
            +-- PLAY:9[8] delta -> authoritative membership change
            `-- localized added/removed system message

PLAY:9[8] mutation -> CommunityManager IgnoreListChanged observer -> UI refresh
```

The final observer edge is strongly inferred from the replicated field plus named transceiver; its exact callback address has not yet been isolated.

## Packet-handler map

| Server message / opcode | Candidate client function | Parsed fields | Resulting action | Confidence | Evidence |
|---|---|---|---|---|---|
| `ChatOnChangeIgnoreStatus` / `0x70E9DA0F` (Z→C) | none; unsupported legacy message | owner U64; game, galaxy, name ASCII16; zero U32; add U8; zero U32 | no direct effect in this executable | Confirmed absence; server schema confirmed | complete name/opcode/RTTI/listener/adjacent-DLL scans and server constructor |
| `PLAY:9[8]` ignore vector delta (Z→C replication) | generic baseline/delta engine; `PlayerObject` registrar `0x0065B800` | AutoDeltaVector string operation(s) | authoritative add/remove membership update | Confirmed principal path | executable field registry and server producer order |
| add/remove ignore request (C→Z) | standard command-queue transport; exact generic sender not assigned in this block | command name plus Unicode arguments; no ignore-specific callback structure | validate and call `PlayerObject::addIgnore`/`removeIgnore` | Confirmed server route; client lower sender boundary open | command implementations and CommunityManager script operations |

## Replacement-client requirements

- Treat `PLAY:9[8]` as authoritative membership state.
- Implement add/remove through the normal command-queue protocol, not a fabricated dedicated packet.
- Present localized system responses independently from structural state changes.
- If supporting `0x70E9DA0F` for compatibility, parse it idempotently and never apply a second membership mutation after the view-9 delta.
- Preserve owner IDs as U64 and all reserved fields/string framing exactly; internal x64 layouts remain unrelated to wire sizes.
- Retain server-side ignore enforcement even if the client also suppresses presentation locally.

## Confidence and unknowns

- **Confirmed by code:** complete wire bytes; add/remove producer order; rejection behavior; absence from this executable; active view-9 field; script wrapper addresses; server command route.
- **Strongly inferred:** `IgnoreListChanged` is emitted from the replicated-vector/UI projection rather than this absent message.
- **Unknown:** historical client revision consuming this notification; exact callback address linking every `PLAY:9[8]` operation to the CommunityManager transceiver; exact generic command-enqueue call chain beneath the script wrapper.

No DRM, licensing, authentication protection or access-control mechanism was examined or altered.

## Progress

One reachable packet moves to documented: **244 documented / 101 reachable gaps / 35 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
