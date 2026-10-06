# Protocol Coverage and Deferred Gameplay Surface

## Coverage tiers

The replacement decision does not require naming every gameplay message before selecting an engine. It does require proving that unknown messages can be added without changing transport, object ownership or engine coupling. Current evidence supports that separation.

| Tier | Coverage | Evidence | Pre-build status |
|---|---|---|---|
| SOE UDP transport | session, negotiated CRC/transforms/MTU, zlib receive, channels, ACK/resend, fragmentation, multipayload | Stardust.exe VAs plus server algorithms and golden-vector specification | Architecture complete; live validation deferred to POC |
| Login/application envelope | typed message header, LoginClientId/token, clusters/status/characters/errors | executable constructors/parsers/handlers plus server classes | Complete for POC |
| Zone authentication and scene entry | endpoint selection, new zone connection, ClientIdMsg, permissions, selection, CmdStartScene/readiness | executable call chains plus server callbacks | Complete for session harness |
| Core object lifecycle | create-by-CRC, baseline, delta, containment, end-baselines, destroy, world/parent transforms | executable dispatcher/parsers and server layouts | Framework complete; schema breadth incremental |
| Representative replicated state | tangible, creature and player views; AutoDelta vectors/maps/sets; wearables, waypoints, quests; ship/cell views | server constructors plus selected executable decoders/subscribers | Sufficient to constrain object/event architecture |
| Long-tail gameplay | crafting, mission, trade, chat, combat, ship commands, scripted UI and many feature messages | hundreds of server packet/callback headers and retained client message/queue RTTI | Intentionally deferred vertical slices |

## Why server source materially reduces unknowns

The server tree directly supplies outbound constructors and inbound callbacks, including opcode constants, operand counts, field order, coordinate quantization and container element codecs. Stardust.exe supplies the other half: whether a message exists in this deployed client, its parser/handler route, and resulting client state/subscribers. A server-only schema is marked as wire-confirmed, not client-behavior-confirmed.

Packet interception is still required for integration validation, deployed configuration differences and ambiguous source anomalies, but it is not the primary discovery mechanism for the transport, login, scene entry or replication envelope.

## Extensibility contract for deferred messages

```text
validated transport payload
  -> (operand count, opcode/name)
  -> registered bounded codec
  -> typed command/event or unknown-message record
  -> transactional object/session commit on owner thread
  -> optional UI/audio/animation/world subscribers
```

Unknown messages must remain loggable/skippable within their validated payload boundary. Adding a gameplay codec must not require changes to socket code, renderer types, engine nodes or object-ID representation. This is the engine-selection-critical result.

## Function-level coverage disclaimer

Only a small minority of Stardust.exe's individual functions have stable provisional names. The exact percentage cannot be established without a complete disassembler function database, and the current mapped set is likely under 5% of all compiled functions. “Pre-build analysis complete” therefore means the unknown function majority is bounded behind understood interfaces; it never means the entire executable has been reconstructed.

## Deferred validation ledger

Round6 envelope clarification (2026-10-02): baseline/delta application header length is23, minimum message25 including the inner U16 count. Server27 includes4 SOE opcode/sequence bytes. [Schema map](REPLICATION_SCHEMA_MAP.md) records offsets; the source-class matrix remains345 documented rows because this is an offset clarification, not another packet family or field-coverage increment.

- Generate opcode/codecs automatically or semi-automatically from reviewed server packet classes where practical.
- Add one gameplay vertical slice at a time and cross-check client behavior using controlled traces.
- Preserve unknown trailing fields and source anomalies as fixtures rather than guessing.
- Record server revision/config with every capture.
- Keep malformed-input limits stricter than the legacy client while preserving valid wire values.


## Bounded implementation update — October 2, 2026

The replacement now decodes the complete eighteen-field [CREO:3 baseline](CREATURE_VIEW3_BASELINE_CONTRACT.md) and ordered [scalar deltas at indices11–16](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md). It preserves raw byte strings/UTF16 units, collection entries/counters, scalar bits and declared-count tolerance; malformed tails and budgets reject before state publication. Raw component-entry preservation does not establish original duplicate-set semantics. [Scalar state](SCENE_CREATURE_SCALAR_STORE_CONTRACT.md) requires an explicit caller seed and exact registry revision. The offline world still records opaque envelope history rather than automatically applying these codecs. These are implementation gates for previously documented schemas; the345-row packet matrix and research denominators do not change.

## Round9 implementation note

The existing CREO3 schema now has bounded [wounds plans](CREATURE_VIEW3_WOUNDS_DELTA_CONTRACT.md) and [incarnation-bound baseline/scalar snapshots](SCENE_CREATURE_REPLICATION_STORE_CONTRACT.md). Native and independent byte/state checks pass; modern strict counter/index behavior does not establish original interoperability. No new reachable packet gaps or coverage denominators.

## Round10 state boundary

Full CREO3/scalar world binding and a [recovered bounded vector kernel](CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md) pass native/independent/static gates. [Typed mode](SCENE_WORLD_REPLICATION_CONTRACT.md) keeps state/resource ownership separate. Wounds are not yet applied by world; no live/full-view coverage claim or denominator change.

## Round64 full-transform implementation depth

[Directional transform implementation](OBJECT_CONTROLLER_TRANSFORM_CODEC_CONTRACT.md) adds tested62/70-byte root/parent codecs while retaining the same documented345 packet-source rows. The four DataTransform/WithParent emitter/callback matrix rows remain documented; no matrix schema/status/count changes. Current-server outbound word is U32 timestamp; client-parsed inbound word is F32 scalar. Offline240-frame host projections do not measure live dispatch/world-application coverage.

## Round65 modern full-root application

The [explicit-direction application router](SCENE_WORLD_FULL_TRANSFORM_APPLICATION_CONTRACT.md) now consumes the existing full0x71 schema into the current typed authoritative motion store under explicit modern immediate-correction/zero-quaternion policy. Parent0xF1, contained, stale or ineligible targets decline; unsigned counters preserve prior issued rows on duplicates. Independent pose/serial/lifecycle tests and direct offline host correction pass. This adds no recovered schema, original interpolation/queued-ingress proof or live contact. Canonical packet-source345-row matrix and coverage counts remain unchanged.

## Round66 modern queued motion ingress

The [combined router](SCENE_WORLD_COMBINED_MOTION_INGRESS_CONTRACT.md) selects accepted compact opcodes, explicit inbound full-root/parent codecs or existing world fallback. Recognized malformed full packets propagate errors; unsigned duplicate/wrap behavior remains in existing children. Actual queued host correction and duplicate are processed before subsequent grounded steering; malformed61-byte root drains the actual world/presentation/controller prefix. No wire schema, original interpolation claim, live contact or345-row packet-matrix change follows.

## Round67 modern outbound local root construction

[Current-local builder](SCENE_WORLD_CLIENT_ROOT_MOTION_CONTRACT.md) constructs the existing62-byte ClientToServer root grammar from exact current store-issued local typed motion. Flags, counter and timestamp bits are explicit caller inputs; native matrix conversion preserves XYZ and wire X/Z/Y without engine reflection. Nine cases/93 checks/eight exact whole-frame fixtures and sign-equivalent near180 basis checks pass. No queue/send/cadence or original sender policy is established. Wire schema and345-row matrix remain unchanged; malformed numerical guards unreachable through store admission are source-inspected rather than claimed as dynamically exercised.

## Round68 modern root FIFO ownership

[Offline queue](SCENE_WORLD_CLIENT_ROOT_MOTION_QUEUE_CONTRACT.md) owns builder-produced root arrays and essential generation/incarnation metadata. Capacity is checked before build; eligible take drops stale prefixes then transfers a same-incarnation historical frame. Typed/motion/containment refresh does not authorize current send or stale already-built bytes. Three exact frames/70 checks/six lifecycle gates and nine joined FIFO frames pass. No transport send, schema or345-row matrix change; sender cadence/units/gameplay policy remain Unknown.

## Round69 offline root queue-to-SOE admission

[Transport bridge](SCENE_WORLD_ROOT_MOTION_TRANSPORT_BRIDGE_CONTRACT.md) adopts historical root FIFO bytes and lets existing SOE copy to reliable channel0. Whole-transport pending backpressure is checked before take. Bridge owns no socket/pump/send and preserves borrowed queue/transport on disposal. Three exact application frames/five exact datagrams/five ACKs/80 component checks and nine joined transfers/ACK releases pass. Layered joined framing uses independently validated application fields/basis; rare post-take fault is source-inspected. No schema/345-row matrix change or original sender cadence/timestamp/gameplay inference.

## Round70 offline SOE application ingress

[Ingress contract](SCENE_WORLD_SOE_APPLICATION_INGRESS_CONTRACT.md) borrows current world/pump/transport, copies transport-owned applications to pump and transfers caller-owned unsent controls. Reliability may be consumed before count/byte/capacity failure; four actual failure prefixes are explicit. Five exact controls/73 component checks, two joined correction/duplicate messages and20-update later malformed-application cleanup pass. No schema/345-row matrix change, socket/send or live zone legality inference.

## Round71 peer-disconnect lifecycle proof

[Disconnect ownership](SCENE_WORLD_PEER_DISCONNECT_CONTRACT.md) uses actual frozen codec/ingress/bridge/FIFO/pump. Matching ID closes transport without closing borrowed scene; caller drains pending scene/root owners and canceled resources.39 component checks and109-update joined closure/wrong-ID failure pass. Reason bytes remain uninterpreted; no schema or345-row matrix change, socket/send/live phase claim.

## Round72 outer disconnect completion prefix

[Ordering and ownership](SCENE_WORLD_MULTIPACKET_DISCONNECT_CONTRACT.md) pass52 component checks and109-update joined closure/failure. Outer transport validates all framing then dispatches through matching disconnect, retaining only prior application/control output. Caller explicitly applies or drops queued prefix; suffix never dispatches. Exceptional untransferred ACK arrays now clear with applications; successful caller-owned arrays survive. Source-inspected private zeroing is distinguished from executed lifetime tests. No wire schema/345-row matrix, reason semantics or live phase claim changes.
