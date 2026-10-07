# Replacement-client outbound disconnect peer

Round87, October6, 2026. Question: can the accepted replacement session finish its selected-player movement prefix, attempt one exact protected SOE disconnect, and report final-send completion or failure honestly while draining? Networking supplies independently authored expectations and comparisons; joined actor/send execution belongs to the lead.

## Evidence and bounded consequence

Observation: [accepted peer disconnect contract](SCENE_WORLD_PEER_DISCONNECT_CONTRACT.md) and [transport protocol](SOE_TRANSPORT_PROTOCOL.md) establish an eight-byte decoded opcode05 packet, connection ID and two raw reason bytes. Frozen Round71's independent author preserves reason bytes `00 06`; accepted transport validates exact length and matching connection ID but does not interpret the reason. Existing `SoeClientTransport.AcceptDisconnect` closes on a matching packet, returns no application/control reply and clears its own pending/reliability state. Framing rejection and semantic rejection remain distinct.

Interpretation: this supplies a bounded outbound packet oracle for the admitted zone connection. Confidence: Confirmed for independently authored packet bytes and existing native synthetic receiver behavior. Consequence: use the exact zone ID/protection, preserve reason bytes, distinguish attempted submission from completed send, and keep teardown separate from server release. Reason meaning, phase legality, server-side release and live interoperability remain Unknown. No new executable/source probe or movement timing claim is made.

## Authored packet and API

```
produce.py --template-crc U32 --object-id NONZERO_U64 --object-path ASCII_PATH --terrain-path ASCII_PATH --output FRESH_PRIVATE_DIR
verify.py --transcript JSON --result JSON --output FRESH_PRIVATE_RECEIPT
test.py --output FRESH_PRIVATE_DIR --native-dll DLL --dotnet EXE
```

[Producer](../poc/round87-tests/Networking/produce.py) reuses accepted Round86's normal peer without changing any existing field/value. It adds `ExpectedDisconnectPlain`, `ExpectedDisconnectWire`, `DisconnectReasonBytes`, `GracefulDisconnectExpected`, test packets, null `DisconnectReliableSequence` and `DisconnectReplyRequired=false`. All fixtures are synthetic. This round's producer emits the complete normal movement/correction prefix; malformed-correction abort remains an accepted Round86 test, not a graceful-disconnect scenario.

The exact decoded packet is `00 05 87 65 43 21 00 06`: big-endian U16 opcode5, admitted big-endian zone connection ID87654321, raw two-byte reason0006. Independent Round14 protection uses the unchanged zone seed0BADCAFE and seeded CRC2/selector4 framing. No reliable sequence or reply is required by this oracle. Movement outputs remain sequence3/4/5 following ready2; no extra movement or ready is expected during disconnect.

The result preserves every Round86 observation, including protected auth/select/ready, typed selected4/6 state, twelve deferred ingress applications, three movement wires, local motion and actual actor positions, four selected correction decisions, two applied/two rejected corrections, and three exact movement remainder messages. New observations are `DisconnectWire` (actual captured attempted-send bytes), `DisconnectRequested`, `DisconnectSubmitted`, `DisconnectSendCompleted`, `TypedFailureObserved` and `Mode`.

| Mode | Required final-send observations |
|---|---|
| success | Requested/submitted/completed true; exact wire; FailureObserved/TypedFailureObserved false |
| send-error | Requested true; submitted actual boolean; completed false; FailureObserved/TypedFailureObserved true |
| send-timeout | Requested true; submitted actual boolean; completed false; FailureObserved/TypedFailureObserved true |

All modes require `Clean=true` and exactly one `ReadySubmissions`. Submitted sends require the exact captured disconnect wire even when sending fails/times out. Unsubmitted failures require absent/null/empty DisconnectWire and must not claim attempted bytes. The boolean failure observations must come from the actual typed final-send failure path; the comparator itself does not establish an exception or timeout occurred.

## Complete-prefix comparison policy

[Verifier](../poc/round87-tests/Networking/verify.py) first validates the actual disconnect mode/flags/wire. It then compares a detached result copy through the accepted Round86 verifier, replacing only `FailureObserved` with false for that prefix comparison. A final disconnect I/O failure must not falsely mark the already completed movement prefix as failed. Every other prefix observation remains required and unchanged: no masking of wire, position, state, correction, remainder, ready-count or cleanup differences. The caller result is never mutated. Accepted binary32 comparison resolves JSON decimal representation differences without numerical tolerance.

The verifier loads the frozen Round86 producer dependency under its existing module name only during loading of the frozen verifier, restoring the caller's module binding immediately afterward. This makes its independent primitive comparator available without importing unpublished Runtime87 symbols or changing accepted sources.

## Validation, confidence and next action

Offline build passed with zero warnings/errors. Component tests passed17 oracle/CLI checks, five fabricated-result positive comparisons,70 comparator rejection cases and32 actual existing native transport checks. Every accepted Round86 transcript field/value is independently compared with a freshly authored Round86 transcript before the new fields are used.

Native tests compare exact independently protected/plain bytes and feed them into an established matching synthetic transport with one retained and one pending message. Matching disconnect and alternate raw reasonFE DC close, clear both counts and return no controls/applications. Bad CRC returns framing rejection; wrong ID and short/extra decoded lengths throw. Rejected packets retain Established state and the pending/retained prefix. Explicit repeated caller Close clears all cases. Receiver acceptance does not prove the replacement's outbound request/send or a server release.

Comparator tests deliberately fabricate result dictionaries to prove success/error/timeout classification, submitted/unsubmitted wire handling and refusal of damaged movement prefixes or extra sends/readies. Five actual verifier CLI invocations pass on those dictionaries. They are not native session/send/actor evidence. No component failures occurred in this round. No original executable, live/server/DNS contact, capture/fuzzing, GPU, staging, commit, push or publication.

Exact next action: lead completes the actual original-actor movement/correction path, requests disconnect through the replacement session, records actual attempted-send bytes and final-send completion/error/timeout, drains all owned work, then runs this verifier in each mode. Server acknowledgement/release, original phase/reason semantics and live interoperability remain separate gates.
