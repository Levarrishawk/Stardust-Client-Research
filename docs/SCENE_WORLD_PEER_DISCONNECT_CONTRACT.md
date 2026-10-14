# Offline peer disconnect ownership

Round71 tests frozen transport, ingress, outbound FIFO/bridge and scene pump without changing production code. Replay (external historical reference) independently packs bytes before native execution; scenarios (external historical reference) exercise the actual owners.

The decoded packet is `00 05 12 34 56 78 00 06`: two-byte opcode, big-endian connection ID, and two trailing reason bytes. Protected selector4/seeded-CRC2 wire bytes are `00 05 C6 F7 E4 D9 C6 C0 C6 D2 7E`, using seed A1B2C3D4 and the established synthetic session. The [durable transport schema](SOE_TRANSPORT_PROTOCOL.md) records the legacy 0x0600 field; this fixture preserves its `00 06` bytes. Frozen `ValidatePacket` requires exactly eight decoded bytes; `AcceptDisconnect` checks the ID and does not read or constrain the reason. A second independent packet with reason bytes FE DC also closes. No reason semantics or live phase legality is inferred from decoder acceptance.

Matching standalone disconnect returns Accepted/PeerDisconnected, zero applications, zero queued bytes and no control reply. Transport closes, clears pending message count and retained reliability count, resets fragments/session fields. Actual tests create one retained packet and one pending message on separate channels first. Private byte zeroing/reset internals are source-inspected, not exposed with reflection.

Bad CRC and short protected framing return RejectedFraming, preserve Established state and pending/retained counts, and leave ingress healthy. Valid protected packets with wrong connection or seven decoded bytes throw InvalidDataException. Transport remains Established with its pending prefix; ingress latches IsFaulted and rejects subsequent receive before transport, with ProcessedDatagramCount zero for the throwing call. Caller still owns teardown. These are separate framing and semantic failure policies.

After an actual typed scene correction and one ordinary terrain-driver tick, matching disconnect preserves the active world, issued current LOCAL motion row, queued scene duplicate, uncanceled resource jobs and earlier caller-owned ACK. Ingress disposal also preserves them. The outbound bridge checks Closed transport before taking from the actual root FIFO: two retained 62-byte applications remain, AcceptedCount stays zero. Disposing bridge leaves FIFO intact; disposing FIFO drains only its own bytes. FIFO clearing is source-inspected; public count/bytes and borrower survival are tested.

Caller pump.Close closes world, empties queued scene suffix, invalidates current motion and cancels jobs. Two controlled resource failures complete after closure and are observed through DrainCompletion; no new router call occurs. Immutable prior motion remains historical. Store/router/driver disposal is explicit and separate. Caller ACK bytes survive ingress and pump closure until caller clears them. This proves the bounded completion prefix and ownership, not automatic disconnect-driven scene teardown.

Replay passes39 native checks, one exact ACK, one driver tick, four negative packets, two retained root applications and two drained resource jobs. Cached offline .NET8 dependencies are required:

```powershell
python poc/round71-tests/Networking/replay.py --output-dir local-private/round71-review-replay
```

Confidence: Confirmed for tested frozen offline implementation; server reachability, live phase legality and reason semantics remain Unknown. No original assets, socket, send, original executable execution, capture or retransmission suite. Next: host consumes PeerDisconnected after the accepted movement prefix and explicitly orders FIFO/pump/resource teardown.
