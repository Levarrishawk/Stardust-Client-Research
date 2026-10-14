# CREO4 delivery and optional admission

**A full CREO4 baseline is optional for observers, but CREO4 deltas are not owner-exclusive.** Current server `CreatureObjectImplementation::sendBaselinesTo` sends a full view4 only when the recipient object is the creature itself. For another recipient observing a player creature, it sends a three-field view4 movement delta between views3 and6, without a view4 baseline. A replacement must not impose a CREO4 seed barrier on every creature's SceneEndBaselines or reject all observer view4 deltas.

This is dated, source-pinned server policy correlated with a separately SHA-pinned client dispatch. Server ownership/receiver selection is not evidence of client authentication or a receiver-side ownership check. The [complete native codec/state](CREATURE_VIEW4_FULL_CONTRACT.md) and [full prepared store](SCENE_CREATURE_VIEW4_FULL_STORE_CONTRACT.md) remain frozen; this investigation adds no world route or protocol implementation.

## Current server baseline path

All paths below are relative to the separately supplied Stardust-3 repository. Source HEAD `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`; twelve investigated files match clean tracked working bytes at the investigation date2026-10-03. Their actual file hashes, rather than an assumed shared date/version with the executable, define this server evidence.

`MMOCoreORB/src/server/zone/objects/creature/CreatureObjectImplementation.cpp`, `sendBaselinesTo`, lines467–504:

| Condition | View delivery |
|---|---|
| Creature has no zone,474–476 | returns before any baseline |
| recipient `player == thisPointer`,478–490 | CREO1, CREO3, complete CREO4, CREO6 |
| different recipient; subject `isPlayerCreature()`,491–496 | CREO3, `sendSpeedAndAccelerationMods(player)` delta, CREO6; no complete CREO4 |
| different recipient; nonplayer subject | CREO3 and CREO6; no view4 in this baseline function |

The owner test is exact object-pointer equality, not any account/crew/group/permission test. Recipient player status at498–503 governs later PvP-status delivery, after baselines. AI `AiAgentImplementation::sendBaselinesTo`, `objects/creature/ai/AiAgentImplementation.cpp`741–745, delegates directly under a despawn read lock; no extra full view4 baseline is added there. This is the inspected path, not a claim that every possible derived server class/feature/plugin has identical delivery.

`packets/creature/CreatureObjectMessage4.h`13–65 constructs tag CREO/view4/declared14 and all fourteen fields in accepted order. Generic `packets/BaseLineMessage.h`14–24 and40–42 supplies opcode68A75F0C, recipient/tag/view/length/count and compression. `packets/DeltaMessage.h`15–32 supplies12862153 and indexed update count. Application envelope size is23 bytes before inner count; server framed size calculation includes four SOE bytes. These are builders, not new message IDs.

## Delta recipients differ by producer

The following bounded call sites are in CreatureObjectImplementation.cpp unless specified. notifyClient=false suppresses the corresponding notification; equality/validity guards differ and are retained in source.

| Producer and exact evidence | Fields / recipient selection |
|---|---|
| `sendSpeedAndAccelerationMods`,1839–1846 | fields1,5,10 directly to supplied recipient; `sendSelf` means recipient is subject or a subchild of subject |
| `broadcastSpeedAndAccelerationMods`,1849–1854 | same fields via broadcast, with caller-supplied sendSelf |
| `setEncumbrance`,1471–1485 | field2 after value-change and notifyClient guards; broadcastMessage(msg,true) |
| `addSkillMod`,1647–1682 | changed visible skill pair plus notifyClient; field3 direct sendMessage |
| scalar setters,2000–2068,2086–2148,2182–2194,2220–2253 | fields0/1/4/5/10/11/7/6/12/8/9 direct sendMessage; per-method notify/equality behavior varies |
| `SkillManager.cpp`,462–473 and660–671 | batch0/1/4/5/7/11/8/9/12 directly through creature sendMessage after skill grant/revoke work |
| `addSpaceMissionObject`,3963–4006; `removeSpaceMissionObject`,4009–4054 | field13 direct sendMessage under validity/notifyClient guards; separate notifyGroup propagation does not make this packet a broadcast |
| `removeAllSpaceMissionObjects`,4057–4070 | constructs/closes a field13 clear under notifyClient but has **no explicit send/broadcast call in this function**; do not count construction as observed delivery |

Creature direct sendMessage3328–3342 reads owner session, drops packet if absent, otherwise sends to that session. This is server-only routing. Derived exceptions matter: VehicleObjectImplementation.cpp168–178 forwards to a linked rider with matching parent; ai/CreatureImplementation.cpp536–552 drops non-mount messages or forwards linked-rider mount traffic. Thus direct sendMessage is not universally synonymous with “same object as session avatar.”

SceneObjectImplementation.cpp728–817 selects broadcast recipients: missing/loading/shutting-down server suppresses; parent routes through root; missing zone suppresses. Normal branch uses close player receivers or range lookup, skips null and selfObject, clones/sends. sendSelf=true permits self if present among receivers; it does not prove inclusion. Compile-time `LOCKFREE_BCLIENT_BUFFERS` with non-null closeobjects takes a fast path762–770 that does not visibly test selfObject. CMakeLists.txt71 defaults ENABLE_LOCKFREE_BCLIENT_BUFFERS OFF and247–248 defines the macro when enabled. Effective deployed build/runtime receiver list is Unknown. Preserve this ambiguity; do not claim identical self exclusion across configurations.

## Producer zero workaround

`CreatureObjectDeltaMessage4.h`, `updateSpeedAndAccelerationMods`,63–78, transmits acceleration modifier1, speed modifier5 and turn scale10 in that order. If acceleration and speed compare zero, transmitted acceleration becomes0.1f. If speed compares zero and sendSelf is false, transmitted speed becomes0.1f. Subject/subchild recipients retain zero speed through this helper; both-zero acceleration substitution still applies. Other scalar setters do not establish a general clamp.

The replacement decoder must preserve received raw F32 words. These producer-side substitutions are not permission to rewrite every zero, negative value, infinity or NaN in a decoder. Synthetic source-model tests include signed zero/high bits/NaNs, but no compiled server or original process was executed; exact floating-point NaN canonicalization in a deployed server build is not established by these source tests.

## Delivery ordering and client correlation

Server `SceneObjectImplementation::sendTo`,363–394, first gates client-only/forceSend, sendToClient, recipient/client existence, then sends create376–377, containment379, baselines382, contained/slotted objects, optional close392–393. `close`,277–280, sends SceneObjectCloseMessage; that packet's header opcode2C436037 (`packets/scene/SceneObjectCloseMessage.h`14–18) is SceneEndBaselines. The observer three-field CREO4 delta therefore occurs during baseline delivery, after create and before close on this ordinary path. Runtime setters/broadcasts supply later deltas. Exceptional send failures, doClose=false, other sends, sender/session phase and transport scheduling remain separate.

Fresh executable SHA gate: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Direct baseline dispatch anchors0051C6FA parser,0051C725 object lookup,0051C744/746 null branch,0051C751 view dispatch; delta0051C7A9 parser,0051C7D4 lookup,0051C7F3/7F5 null branch,0051C800 view dispatch. Null client-object result skips the view handler in these branches; no modern buffer behavior follows from that observation.

Baseline view4 jump-table entry00554E68+12 selects00554D6C, reading stream+0xC4 and calling snapshot slot+0x0C at00554D7C. Delta entry00554FE4+12 selects00554F1A, adds+0xC4 and reaches indexed delta slot+0x18 at00554F0B, then commit slot+0x1C at00554F12. These selected blocks contain no owner comparison or “view4 baseline received” predicate. The accepted [fourteen-field registration](CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md) exists in CreatureObject construction. This establishes registered structural acceptance for an existing creature object, not arbitrary sender authorization or a global absence of security checks. Login versus connection versus zone acceptance outside this scene-message route remains Unknown; server scene ordering supports zone-scene use, not login legality.

## Optional replacement-world requirements

### Lead constructor follow-up

The same SHA-pinned constructor0042ECF0 establishes a useful partial answer:
**an all-zero default view4 model is contradicted by direct instructions.** The
lead verifier adds58 constructor anchors alongside the worker's24 dispatch
anchors and accepted fourteen-entry registration audit. These observations
establish initial assignments at these constructor sites, not the final effective
model after base/template/subclass/other initialization.

| View4 index / registered member | Direct initial-value evidence |
|---|---|
| 0 / +690 | 0042F147 loads3F800000;0042F14C/152 stores to member+C/+10. |
| 1 / +67C | 0042F11D loads3F800000;0042F122/128 stores to member+C/+10. |
| 4 / +640 | 0042F08E loads3F800000;0042F093/096 stores to member+C/+10. |
| 5 / +62C | 0042F06C loads3F800000;0042F071/074 stores to member+C/+10. |
| 10 / +6A4 | 0042F171 loads3F800000;0042F176/17C stores to member+C/+10. |
| 11 / +654 | Input-object call011ADE40 at0042F0AA with argument1; F32 spill0042F0AF, copies0042F0C2/C8. |
| 7 / +668 | Same call at0042F0E0 with argument0; spill0042F0E5, copies0042F0F8/FE. |
| 8 / +6B8 | Input-object call011AE700 at0042F194 with argument0; multiplies words at015DCBD4/015DC5F0, spills0042F1AB, copies0042F1B8/BE. |
| 9 / +6CC | Input-object call011AEA90 at0042F1D6 with argument0; spill0042F1DB, copies0042F1EE/F4. |
| 12 / +6E0 | Input-object call011AEE20 at0042F20C with argument0; spill0042F211, copies0042F226/229. |

Receiver load0042F09F takes the constructor's supplied object from `[ebx+8]`;
the [initialization investigation](CREATURE_VIEW4_INITIALIZATION_CONTRACT.md)
now identifies it as ClientCreatureObjectTemplate through the concrete factory.
Getter loading/inheritance, conditional fallback, sampled modes and relative
operand order are separately evidenced. Literal word3F800000 is F32 one.
Confidence: Confirmed literal five ones and conditional getter inputs; Unknown
effective template values, runtime sampling, subsequent transitive overrides
and complete initialized collections/ID.
Consequence: do not fabricate a zero seed or treat these five ones as a complete
initial baseline. The [received-only partial state](CREATURE_VIEW4_PARTIAL_STATE_CONTRACT.md)
preserves unreceived-field absence without sampling defaults. Exact next probe:
bound effective template values and later writers separately from current-
incarnation pending-baseline partial admission.

1. Track view4 availability per current incarnation independently of required public views and object finalization. Omitted full view4 does not block every observed creature at SceneEndBaselines.
2. Accept validated observer scalar view4 deltas without requiring an earlier full view4 packet. Preserve actual source order, field widths/raw bits and container counters; do not infer packet permission from server owner labels.
3. An instantiated creature needs an explicit template/constructor-initialized view4 model or a separately specified bounded pre-seed policy. The frozen full store requires a baseline seed; it does **not** itself solve observer initialization. Do not fabricate a network baseline or claim guessed zero/default values are recovered. Default/template initialization is the exact next probe.
4. Distinguish actual full snapshot availability from locally initialized defaults and previously received partial deltas. A later full baseline must go through reviewed complete decoding/reseed policy; ownership-transfer ordering is not established here.
5. Admit the characterized pending-baseline observer delta path. Do not add a finalized-only delta barrier that discards this ordinary server path. Keep object existence/incarnation/generation and transaction gates explicit; these are modern lifecycle safeguards, not recovered account checks.
6. Apply complete validated plans atomically under aggregate limits and publish guarded events only after commit. Historical journal ownership must remain separate from current-incarnation delivery, as the [prepared store](SCENE_CREATURE_VIEW4_FULL_STORE_CONTRACT.md) specifies. Presence of field13 callbacks does not establish owner gameplay behavior.

## Verification and remaining uncertainty

Verifier (external historical reference) accepts explicit --server-root/--exe/--packages/--receipt, pins12 source files and executable before optional imports/VA use, checks17 function rules/67 literal tokens,24 client anchors/two jump entries and reuses107 accepted prerequisite anchors/14 registrations. Identity tests (external historical reference) reject two wrong executable identities,12 changed source files, missing root and public receipt with no rejected output. The independent producer peer (external historical reference) supplies200 literal synthetic cases to the actual separate Core runner (external historical reference):10200 negatives,1800 exact/one-less budget checks,400 ownership checks. Existing full codec/state preserve the emitted fields and raw words; no new protocol implementation was added.

Confidence: Confirmed current source conditions and direct dispatch/registration anchors; Strongly inferred requirement to support ordinary observer pre-baseline deltas without full view4 seed; Unknown deployed macro settings, receiver membership, alternate derived/plugin paths, auth/login acceptance, actual captures/NaN producer details, default/template state, ownership transfer and original owner callbacks. [Checkpoint/reproduction](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R43_CREO4_DELIVERY.md).
