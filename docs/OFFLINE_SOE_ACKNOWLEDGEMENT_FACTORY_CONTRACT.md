# Synthetic offline channel-zero acknowledgement

[OfflineSoeAcknowledgementFactory](../poc/Stardust.Core/Session/OfflineSoeAcknowledgementFactory.cs) is a stateless modern helper for explicitly synthetic offline profiles. It creates a protected channel-zero cumulative ACK without a socket, session, receive event, counter, retry or alternate selector/channel. Its output is not evidence that a server received or acknowledged a packet.

```csharp
byte[] syntheticAck = OfflineSoeAcknowledgementFactory.Create(
    sequence, seed, crcWidth: 2, maximumDatagram: 496);
try { /* caller supplies it to the explicitly offline session */ }
finally { Array.Clear(syntheticAck); }
```

Plain bytes are exactly `00 15` followed by the supplied U16 sequence in big-endian order. Existing SoeDatagramCodec.EncodeUncompressed adds the zero compression marker, selector4 and seeded CRC trailer; no framing implementation is duplicated. Output length is5+crcWidth, at most9 bytes. Width1..4 is supported by the frozen codec; composed offline RootSession still requires its documented CRC2 transport contract.

Factory validates width1..4 and modern transport maximum range4..65507 first. Maximum below5+width throws InvalidDataException; invalid width/range throws ArgumentOutOfRangeException. These checks precede temporary storage/encoded allocation. Maximum is a capacity ceiling, not allocation size: maximum65507 still returns9 bytes with CRC4. Four-byte temporary packet uses stack storage and is zeroed in finally. Private temporary zeroing/exception cleanup is source-inspected; no instrumentation claims it was observed.

Each successful call returns a fresh exclusive caller-owned mutable array. The helper retains no state or bytes, infers no next sequence/negotiation and closes no owners. Callers retain or clear output when finished. No owner-thread restriction is invented for this pure static helper; actual session/transport owners retain their own affinity/serialization rules.

[Independent replay](../poc/round76-tests/Networking/replay.py) computes all expected bytes before native.72 vectors cross sequences0/1/255/256/32767/65535, seeds0/A1B2C3D4/FFFFFFFF and CRC widths1/2/3/4. Frozen independent Python reflected CRC/selector4 model protects exact four-byte packets, selects low CRC width bytes in big-endian order, and restores its isolated seed override before native. Each vector uses exact capacity5+width. Native decode confirms exact4-byte packet; Python compares complete protected/decoded bytes. Fresh output survives clearing another returned array, then caller clears it.

Negative cases cover widths−1/0/5, maxima−1/0/3/65508/Int32.MaxValue and one insufficient capacity for each supported width. Upper transport bound65507 succeeds with9-byte output. Final350 native checks, zero build warnings/errors.

Brief integration uses actual frozen75 point-driver issued LOCAL rows, RootSession/FIFO/drain/transport:15 steps, movement1..10, pause11..12, resumed movement13..14 and stopped15. Three independently predicted exact roots/protected datagrams/ACKs match with all15 combined cadence/frontier receipts. Factory creation leaves session state/FIFO/transport counts unchanged; subsequent explicit offline Observe processes synthetic ACK and releases one retained frame. Caller ACK bytes and historical receipt strings survive own cleanup; borrowers survive session disposal. No redundant full cadence/retry suite or original assets are involved.

```powershell
python poc/round76-tests/Networking/replay.py --output-dir local-private/round76-review-replay
```

Fresh guarded ignored output and cached offline .NET8 dependencies required. Confidence: Confirmed bounded modern helper/codec bytes and offline integration. Recovered transport framing is reused; synthetic ACK timing, server receipt and live legality are unproved. No EXE execution/socket/send/capture. Next: lead user profile creates caller-owned ACK on demand for its explicitly offline session, clearing it after use instead of storing a256-entry array.
