# Bounded controlled-server validation plan

Prepared October 6, 2026. **Not authorized and not executed.** This document is a plan, not proof of interoperability. Offline preparation and the missing world/presentation session bridge must pass before requesting execution.

## Prerequisites and authorization request

Use an operator-owned Stardust/Core3 test server, with explicit operator permission for one replacement-client test and a disposable existing character/account. Record the exact server revision, configured accepted client version, galaxy/connection-server mapping, login address and zone address privately. Confirm account/session-token mechanism, enabled login, available galaxy, matching character ownership and compatible original installation supplied by Levarris. No creation/deletion of characters or modification of account/server configuration is part of this test.

Inputs are original archive root, explicit mount configuration, approved client version, test endpoint allowlist and temporary credentials supplied outside checked-in files/command histories. The version is explicit: the dated executable's default and a historical server convenience constructor differ. Offline codecs do not establish current server policy.

Request specific authorization only once offline prerequisites pass: **run this replacement client against these operator-approved login and zone endpoints with this disposable account, for one connection attempt lasting at most 120 seconds, one selected existing character, and one short movement followed by disconnect**. Request authorization for DNS resolution if approved endpoints are hostnames. Original executable execution and packet capture remain separate, unrequested permissions.

## Exact exchange sequence

1. Replacement sends SOE SessionRequest to allowlisted login peer; accept only matching SessionResponse connection ID and supported framing selectors/size. No application authentication before establishment.
2. Send LoginClientId with explicit username/password/version. Require LoginClientToken, LoginEnumCluster, LoginClusterStatus and EnumerateCharacterId through the established reliability/framing path. Preserve the credential blob opaquely; do not log it.
3. Select the explicitly approved existing character from the received list; require its galaxy to have exactly one matching cluster/status. Resolve only the approved connection-server hostname/address and use **zone port**, not ping port. Close login lifecycle and open a fresh, separately identified SOE connection.
4. After matching zone SessionResponse, send ClientIdMsg: operand3, gameBits0xFE, uint32 little-endian blob length, exact retained token-plus-account bytes, ASCII16 explicit client version. Accept ClientPermissionsMessage operand4 with documented three/four boolean variants. Stop on denied login; do not send SelectCharacter until permission succeeds.
5. Send exactly one SelectCharacter for approved object ID. Require matching CmdStartScene object ID. Route terrain/template/resource setup and ordered pre-ready replication through the reusable engine-neutral world owner. Send one CmdSceneReady only after minimum-resource readiness; accept subsequent server CmdSceneReady. Preserve control ACK/retransmission ownership throughout.
6. Apply authoritative selected-player create/baseline state and original asset chain to Godot via presentation host. Confirm selected identity, visible player and finite valid transform. No fabricated fixture actor substitutes for this criterion.
7. One bounded operator-approved movement (at most two meters, at most two seconds) through the existing ClientToServer object-controller Root0x71 envelope and [owned root builder](SCENE_WORLD_CLIENT_ROOT_MOTION_CONTRACT.md). Require server-side observation and authoritative position update for the same object; accept a correction rather than treating local movement alone as success. Sender cadence/units remain a compatibility gate to resolve before this step.
8. Submit the documented SOE disconnect with the established connection ID through the same approved peer, then close the replacement's transport, ports, jobs, presentation and credential buffers. Outbound disconnect lifecycle integration must be implemented and independently tested offline first; current local disposal is insufficient proof. Operator confirms server releases session/character; no automatic reconnect. A separate retry requires renewed authorization if the initial attempt reaches a stopping condition.

Message layouts are in [login state machine](LOGIN_AND_ZONE_STATE_MACHINE.md), [zone transcript](OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md) and current session contracts. Current server source remains correlation evidence; successful controlled exchanges must independently establish interoperability.

## Evidence and success

Record private allowlisted diagnostics: monotonic relative times, phase/outcome, packet category/opcode, sizes, sequence/retry counts, selected identity represented by a run-local alias, resource identity hashes, readiness/job/actor counts and cleanup status. No raw datagrams, token/account/password, hostname/address, character name or raw server error strings in curated output. Server operator supplies a redacted confirmation of accepted login, selected character, movement and release. Avoid captured traffic: these are the replacement's own typed diagnostics and operator observations.

Success requires every exchange and visible-player/movement gate above, no skipped phase, no synthetic peer, and clean teardown. Partial success is reported per boundary, with the last accepted exchange and exact unverified boundary. Store private run records under ignored local-private; promote only independently written redacted conclusions and input hashes with confidence.

## Cleanup and stopping conditions

Stop immediately on wrong peer/character/galaxy, unsupported selectors/version/layout, permission rejection, missing resource, invalid transform, crash, unexpected server request or credentials in diagnostics. Stop at the per-phase 30-second maximum or overall 120-second maximum, whichever occurs first. Retransmissions remain within the existing bounded policy and elapsed attempt; no repeated authentication guessing. Cancellation drains outstanding resource work, removes actors/cameras, closes ports, clears owned token buffers and reports any teardown failure. Do not change server/account state to make the test pass. If normal cleanup fails, ask the operator to release only the disposable session; that intervention is not implicitly authorized here.

## Current actionable step

Round84 independently verifies the offline session → world → selected-human Godot path from explicit installation inputs. Its peer/typed CREO3 are authored, its adapter admits one creature, and its minimum provider decodes PTAT without full procedural surface setup. Before a live attempt, complete server-shaped player baseline/lifecycle and nearby-object handling, required avatar/terrain preparation, authoritative movement and outbound protocol disconnect. No live validation is the next actionable step yet. [Milestone plan](REPLACEMENT_CLIENT_MILESTONE_PLAN.md).
