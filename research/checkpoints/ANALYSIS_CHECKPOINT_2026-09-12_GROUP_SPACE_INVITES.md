# Analysis checkpoint: group space-launch invitations

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe` only, correlated with current Stardust server source

## Result

The four ObjectController types `0x436`–`0x439` form a complete request/invite/decision/response circuit for inviting a group member to launch as a passenger. The client and server directions deliberately share two payload factories, but the server-to-client constructors append compatibility fields that this executable does not deserialize.

```mermaid
sequenceDiagram
  participant P as Pilot client
  participant S as Stardust server
  participant M as Member client
  P->>S: 0x436 {memberId}
  S->>S: player/type/parent/range <= 7 m
  S->>M: 0x437 {pilotId, 0, memberId}
  M->>M: consume pilotId; present consent
  M->>S: 0x438 {pilotId, decision}
  S->>S: parent/range <= 7 m revalidation
  S->>P: 0x439 {memberId, decision, pilotId}
  P->>P: consume memberId + decision; publish result
```

## Exact contracts

- `0x436` C→Z: outer U32 scalar, then U64 group-member ID. Client sender `0x00E00570` uses flags `0x23`.
- `0x437` Z→C: outer U32 scalar, then U64 pilot ID, U32 zero, U64 invited-member ID. Factory `0x01195890` is the generic one-NetworkId parser, so only pilot ID enters the queue object.
- `0x438` C→Z: outer U32 scalar, then U64 pilot ID and U8 decision. Sender `0x00652630` uses flags `0x23`.
- `0x439` Z→C: outer U32 scalar, then U64 member ID, U8 decision and U64 pilot ID. Factory `0x01196EE0` consumes only the first U64 and U8.

## Consent lifecycle

`0x00652EE0` stores the pending pilot ID, resolves the pilot for display text, creates a consent window and retains its callback token. If another invitation is already active, the old pending identity is answered/cleared before the new one is presented. `0x00652720` ignores callbacks whose token is no longer the active prompt; an accepted prompt additionally updates retained passenger/launch state before submitting `0x438`.

## Server validation

For `0x436`, Stardust requires a live player, zone server, target player creature, matching parent and distance within 7 metres. For `0x438`, it resolves the pilot and repeats the matching-parent and 7-metre checks before forwarding the decision. This is authoritative server policy; the client prompt is presentation, not security.

## Compatibility consequence

A replacement codec must be keyed by direction as well as numeric inner type. It may consume the full current-server byte sequence into a modern wire DTO, but its behavior layer should treat the redundant member/pilot copies as reserved consistency fields. It should optionally diagnose a mismatch without rejecting packets that the original client accepted, because the original parser never compares those trailing identities.

## Uncertainty classification

No material wire or state-transition unknown remains in this family. Exact localized prompt wording and the concrete types of every launch-result subscriber are later UI breadth. They do not block network or session architecture.
