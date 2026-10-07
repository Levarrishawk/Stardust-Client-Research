# Selected-player movement peer profile

Round86, October6, 2026. Question: can the accepted protected selected-player session carry three local root movements and then authoritative corrections to the same actor, with explicit remainder handling and cleanup? This Networking component independently authors the wire and primitive expectations. The lead owns joined session/actor execution.

## Evidence and interpretation

Observation: accepted [directional transform codec](OBJECT_CONTROLLER_TRANSFORM_CODEC_CONTRACT.md), [local driver](SCENE_WORLD_LOCAL_MOTION_CONTRACT.md), [root builder](SCENE_WORLD_CLIENT_ROOT_MOTION_CONTRACT.md), [motion store](SCENE_WORLD_OBJECT_MOTION_CONTRACT.md) and [full transform router](SCENE_WORLD_FULL_TRANSFORM_APPLICATION_CONTRACT.md) establish the bounded native implementations. Frozen Round66/67 independent authors handpack full-root headers, quaternion XYZW, position XZY and speed; this author retains that layout. The accepted [selected ingress profile](SELECTED_PLAYER_SERVER_INGRESS_PROFILE.md) supplies real codec-shaped protected login/zone and authored selected state.

Interpretation: the existing implementations can be joined at the session boundary without inventing an engine-specific movement protocol. Confidence: Confirmed for independently authored bytes and native codec/placement observations; joined behavior requires separate lead replay. Consequence: preserve exact wire direction, raw flags/counters/timestamp, native XYZ and remainder identity. No new original-executable or server gameplay claim follows.

Original timestamp units, movement sender cadence, server admission and legacy quaternion-sign choice remain Unknown. The fixed step, velocity, caps, counter and timestamp progression below are explicit modern fixture policy, not recovered original policy. No new source probe was needed to implement this bounded test.

## Producer and event mapping

```
produce.py --template-crc U32 --object-id NONZERO_U64 --object-path ASCII_PATH --terrain-path ASCII_PATH --output FRESH_PRIVATE_DIR
produce.py [same arguments] --variant malformed-correction
verify.py --transcript JSON --result JSON --output FRESH_PRIVATE_RECEIPT
test.py --output PRIVATE_DIR --native-dll DLL --dotnet EXE
```

[Producer](../poc/round86-tests/Networking/produce.py) imports only accepted Round85/14 authors. All fixture data is synthetic. It preserves the18 world frames, protected server-ready sequence19, twelve deferred applications, typed selected4/6 expectations and exact auth/select/ready outputs. Movement is outbound sequence3/4/5, following ready2; `ExpectedMovementApplications`, `ExpectedMovementWires` and `MovementAcks` independently pin those three outputs and ACKs.

`LocalMotionPolicy` specifies initial time0, step100000 microseconds, at most one step/update, input native velocity(0,0,2), advances100000/200000/300000, position cap1000, speed cap10, flags0x23, counters10/11/12 and raw timestamp words100/200/300. Create placement maps raw X/Z/Y(1.25,-3.75,2.5) into native XYZ(1.25,2.5,-3.75). Each step independently binary32-rounds2×0.1, then adds that displacement to current native Z. Identity basis and speed2 are expected. This planar driver adds no terrain/collision parity claim.

All seven `CorrectionFrames` are injected after all three actual movement sends. `CorrectionEvents` names each index, reliable inbound sequence20–26 and `AfterMovementCount=3`; no correction occurs during the local trajectory. First four applications are selected ServerToClient root0x71, flagsF1234567, outer scalar bits0, identity quaternion:

| Event | Counter | Native position | Speed | Expected result |
|---|---|---|---|---|
| newer | FFFFFFFF | (10,2.5,-8) | 3 | Applied |
| duplicate | FFFFFFFF | (99,99,99) | 4 | CounterRejected |
| older | FFFFFFFE | (-99,-99,-99) | 5 | CounterRejected |
| wrap-newer | 00000000 | (20,2.5,-9) | 4 | Applied |

The modern unsigned half-range rule accepts first any counter, then distance1..7FFFFFFF. Equal/older rejections retain the actual previous motion record and pose. Local driver initialization publishes revision1; three local steps yield2/3/4; corrections yield5/5/5/6. The remaining messages are unrelated-OID root, valid selected parent0xF1 with separate PLAY parent ID, and readable unknown controller kind0x99. `ExpectedUnhandledMovement` preserves their exact application bytes/order; they do not contribute to the original twelve ingress deferred messages.

The malformed variant truncates the first selected correction by one byte and generates valid SOE protection/CRC. It must fail known-root decode after normal login/readiness and three movement sends, before any correction publication. It does not require a second ready submission. No unsupported kind is silently treated as a malformed known-root fixture.

## Observation and comparison

[Networking86Observation.Project](../poc/round86-tests/Networking/Observation.cs) projects the actual issued `SceneWorldObjectMotionSnapshot` and `store.IsCurrent`: ObjectId, Position(native XYZ), Rows(nine row-major scalars), Speed, Revision, MovementCounter, Authoritative and Current. Call immediately after each step/correction; currentness is an observation at that point, not a promise that historical rows remain current.

Normal result keys are the Round85 `AuthWire`, `SelectWire`, `ReadyWire`, `SelectedModels`, `DeferredHex`, plus `MovementWires`, `LocalMotions`, `CorrectionObservations`, `ActorLocalPositions`, `ActorCorrectionPositions`, `AppliedCorrections`, `CounterRejectedCorrections`, `UnhandledMovementHex`, `ReadySubmissions`, `Clean`, `FailureObserved`. Correction observations are `{Name, Applied, Status, Motion}`. Actor positions are actual Godot XYZ origins after each local step and each of the four selected correction events, compared with independently expected native(X,Y,-Z). Motion rows remain native; no Godot reflection is packed into wire.

[Verifier](../poc/round86-tests/Networking/verify.py) requires exact three movement wires, complete local/correction primitive models, actor positions, two applied/two rejected corrections, exact unhandled bytes, clean drain, exactly one ready and no normal failure. Binary32 values compare after exact binary32 rounding of JSON decimal numbers; this resolves serializer decimal-length differences without a numerical tolerance. Initial Round85 protected wire/state/deferred comparisons remain required. Malformed result requires the completed three local observations, `FailureObserved=true`, clean drain, one ready and zero applied/rejected corrections; success-only correction/actor/remainder arrays are not required after the first fault.

## Validation and precise next action

Offline native build passed with zero warnings/errors. Component tests passed140 oracle checks,33 comparator rejection cases,14 native codec checks and855 malformed native codec checks. Tests verify protected sequences/applications/ACKs, every outbound primitive, identity placement, raw inbound scalar and independent wrap ordering. Native tests reencode exact full-root bytes, reject all truncations, extra bytes and nonfinite components, and check valid parent layout separately. Comparator tests use explicitly fabricated result dictionaries; these are not joined session or actor observations. The initial JSON-decimal assertion failure and corrected successful run remain in private evidence.

No original executable, socket/server/DNS contact, capture, GPU or live interoperability execution. Next action: lead drives the real local-motion driver through the replacement session, records actual protected movement sends and actual actor origins, injects the seven correction frames at the documented event point, then runs this verifier for normal and malformed variants. Disconnect/server release, general movement gameplay, parent-relative motion, cadence and live interoperability remain separate gates.
