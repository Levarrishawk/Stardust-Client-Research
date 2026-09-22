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

- Generate opcode/codecs automatically or semi-automatically from reviewed server packet classes where practical.
- Add one gameplay vertical slice at a time and cross-check client behavior using controlled traces.
- Preserve unknown trailing fields and source anomalies as fixtures rather than guessing.
- Record server revision/config with every capture.
- Keep malformed-input limits stricter than the legacy client while preserving valid wire values.
