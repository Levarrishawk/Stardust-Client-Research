# Selected-player server-shaped ingress profile

Round85, October 6, 2026. Bounded question: can an independently authored protected login/zone transcript carry complete selected CREO3/4/6 models while preserving opaque and unrelated ingress for a separate consumer?

## Evidence and confidence

Observation, server correlation only: `MMOCoreORB/src/server/zone/objects/creature/CreatureObjectImplementation.cpp`, `CreatureObjectImplementation::sendBaselinesTo`, lines467–498, sends owner CREO1/3/4/6; observers receive3/6, with an additional speed-modification branch for player creatures. A null zone returns before baselines. `MMOCoreORB/src/server/zone/objects/player/PlayerObjectImplementation.cpp`, `PlayerObjectImplementation::sendBaselinesTo`, lines473–489, sends PLAY3/6 and owner8/9 when the recipient equals the parent. `MMOCoreORB/src/server/zone/objects/scene/SceneObjectImplementation.cpp`, `SceneObjectImplementation::sendTo`, lines363–394, sends create, link, baselines, container children, slotted children, then optional close; its caught exception path can still reach close. `close`, lines277–280, emits the close message.

Observation: `MMOCoreORB/src/server/zone/packets/creature/CreatureObjectMessage4.h:13` declares0x0E; `CreatureObjectMessage6.h:14` declares0x16. These are14 and22, respectively. Declared metadata is distinct from the18 registered CREO6 fields. Source identities and inspected line ranges are retained in the ignored run receipt, not embedded proprietary source.

Interpretation: these source branches motivate an owner/observer-shaped test stream. Confidence: Confirmed for inspected server source and authored fixtures; no new executable behavior claim. Compatibility consequence: selected admission must preserve the accepted typed boundaries and retain other messages without silently decoding opaque bodies. Server source is correlation evidence, not proof of client behavior or deployment ordering.

## Authored stream and API

[Producer](../../SWG-Godot-Client/poc/round85-tests/Networking/produce.py) reuses the accepted Round84 protected login/zone author, Round15 independent CREO3 author, Round38 CREO4 baseline author and Round32 CREO6 baseline author. It preserves Round84 transcript fields, adds `AuthoredServerShaped=true`, `LiveInterop=false`, `ExpectedDeferred`, `ExpectedSelectedModels`, `IngressOrder` and classification metadata. The retained historical `OpaqueCreoEnvelope` key aliases typed selected CREO3; `AuthoredTypedCreo3=true` and `OpaqueBaselineSemantics=false` describe selected3. Separate flags identify opaque deferred CREO1/PLAY.

```
produce.py --template-crc U32 --object-id NONZERO_U64 --object-path ASCII_PATH --terrain-path ASCII_PATH --output FRESH_PRIVATE_DIR
produce.py [same arguments] --variant missing4|missing6|truncated6
verify.py --transcript JSON --result NATIVE_JSON --output FRESH_PRIVATE_RECEIPT
```

The normal18-frame zone sequence is Start, selected create/CREO1/3/4/6, nearby creature create/3/6/end, separate PLAY create/3/6/8/9/end, selected end, known notice. Server-ready is sequence19; outbound auth/select/ready remain sequences0/1/2. Nearby and PLAY IDs are deterministically distinct nonzero successors. Their create template CRC is an authored placeholder; no actual PLAY template or parent relationship is inferred. Parent0 containment/link packets are omitted in this bounded profile. The child-shaped PLAY stream precedes selected end; nearby interleaving is a test ordering, not a claim of complete `sendTo` output. Ancillary PvP, speed-modification, containment, configuration and resource messages remain outside the profile.

CREO1 and PLAY bodies are deliberately opaque authored bytes inside valid replication envelopes. `CompleteServerBaselineSet=false`: presence of view labels does not establish semantics or complete server emission. The12 deferred application hex strings preserve exact ingress order: selected1, four nearby messages, six PLAY messages and notice. Deferred identity/count is separately verified from selected typed state.

Selected4 has all fourteen physical fields, finite raw float words, three encumbrances, one skill modifier and one mission pair. Selected6 has all eighteen physical fields, declared22, two short strings, nontrivial HAM/max-HAM, empty defenders/wearables and preserved scalar/empty-string fields. The primitive projection covers every model field and every collection entry, including raw byte-string hex and extended-length flags. Models are authored before native execution. Existing [full4](CREATURE_VIEW4_FULL_CONTRACT.md) and [full6](CREATURE_VIEW6_FULL_CONTRACT.md) contracts retain underlying evidence and normalization limits.

Native joined success results provide `AuthWire`, `SelectWire`, `ReadyWire`, `SelectedModels`, `DeferredHex`. Use [Networking85Observation.Project](../../SWG-Godot-Client/poc/round85-tests/Networking/Observation.cs) with `world.FindCreatureView4Full(generation, oid)!.Model` and `world.FindCreatureView6Full(generation, oid)!.Model`. The verifier compares exact protected wire, all primitive selected fields and exact deferred order/count. Negative results provide exact auth/select, `FailureObserved=true`, `ReadySubmissions=0`, `PlayerPublished=false`, and absent/null/empty `ReadyWire`.

Missing4/missing6 remove only that selected baseline and rebuild all SOE sequences without gaps. Truncated6 removes its final field byte, repairs envelope size, then regenerates protection/CRC: transport and envelope are valid while typed decoding rejects. These support separate joined barrier/failure tests; the Networking component does not claim to execute the session bridge.

## Validation and next action

[Component test](../../SWG-Godot-Client/poc/round85-tests/Networking/test.py) takes `--output FRESH_PRIVATE_DIR --native-dll DLL --dotnet EXE`. Independent Python checks validate protected sequence/application equality and ready sequence; the native component decodes complete4/6, constructs full states and emits observed primitive models. It also rejects truncated6. Comparator tests deliberately fabricate result dictionaries solely to exercise positive/negative comparison; they are not native session observations.

Executed result:163 checks,24 comparator rejection cases, one native truncated6 rejection; offline build passed with zero warnings/errors. No socket, original executable, capture, GPU or live interoperability execution. Exact next action: run the lead's selected-player bridge against all four transcripts, emit the documented observed result keys, and run the independent verifier. Nearby/PLAY ownership and typed semantics remain deferred.
