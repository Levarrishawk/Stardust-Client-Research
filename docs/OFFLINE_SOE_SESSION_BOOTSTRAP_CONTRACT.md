# Explicit synthetic offline session bootstrap

OfflineSoeSessionBootstrap.Create(connectionId, seed) (external historical reference) constructs an established socket-independent transport for explicitly synthetic offline profiles. Receipt has caller-owned SoeClientTransport Transport and immutable RequestHex/ResponseHex strings. Neither string represents a sent request or received server response.

```csharp
var bootstrap = OfflineSoeSessionBootstrap.Create(connectionId, seed);
try { /* use bootstrap.Transport only in the explicit offline profile */ }
finally { bootstrap.Transport.Close(); }
```

Fixed modern settings: maximum datagram496, CRC width2, transform selectors1/4 and request protocol2. Actual BeginSession creates the14-byte request. Because frozen codec has no response encoder, helper packs the documented17-byte raw response boundary, invokes actual DecodeResponse then actual AcceptSessionResponse. No new codec/session negotiation policy or configurable transport extension. Raw handshake bytes have no established-session compression marker/selector4/CRC trailer.

| Packet | Raw field order |
| --- | --- |
| Request14 | `00 01`, protocol U32BE2, supplied connection ID U32BE, maximum U32BE496 |
| Response17 | `00 02`, supplied connection ID U32BE, supplied seed U32BE, CRC U8=2, selectors U8=1/U8=4, maximum U32BE496 |

Representative connection F00D00DB/seed A1B2C3D4: RequestHex `000100000002F00D00DB000001F0`; ResponseHex `0002F00D00DBA1B2C3D4020104000001F0`. This uses the existing [transport handshake schema](SOE_TRANSPORT_PROTOCOL.md), with values selected by modern synthetic profile policy. Zero/all-one identifiers and seeds are preserved bits; acceptance by this helper does not establish live server legality.

Successful transport is Established with zero pending/retained packets. Helper retains no transport or mutable scratch after return. Request and response arrays are zeroed in finally after string creation. If construction fails after transport creation, catch closes that transport before rethrow, and finally clears scratch. Private zeroing/exception cleanup is source-inspected, not exposed with reflection or artificial fault hooks. Fixed valid configuration/all U32 inputs expose no ordinary invalid-parameter failure path. Create is synchronous and has no cancellation API; caller/session operations retain their own token semantics.

Caller owns transport lifetime and serialization. Receipt has no automatic Dispose/Close wrapper. Close clears actual pending/reliability/session state according to existing transport policy; immutable strings survive. Using returned transport for a composed root session does not transfer ownership: RootSession.Dispose closes only its own children. Caller subsequently closes bootstrap transport and actual scene owners.

Independent replay (external historical reference) packs exact14/17-byte expectations before native over three connection IDs0/FFFFFFFF/F00D00DB × three seeds0/FFFFFFFF/A1B2C3D4. Nine actual bootstrap receipts match every byte. Native codec decode verifies supplied fields/fixed configuration. Caller creates one retained packet plus a pending message, closes twice, verifies cleared counts/Closed state and preserved strings; decoded caller scratch arrays are independently cleared.

Brief integration creates actual frozen76 point driver, issued LOCAL rows, frozen RootSession and ACK factory with returned bootstrap transport. Three ordinary updates move native Z at speed2 twice, then stop. Immediate-stop cadence yields one independently predicted root/protected datagram/ACK at30ms; all three combined receipts match. Precanceled session Observe preserves zero sample/FIFO progress and established bootstrap owner. Session disposal preserves transport until caller finally Close; string history survives. No full cadence/reliability resurvey, sibling77 settings import, original asset load or resource completion.

Final57 native checks, zero build warnings/errors; fresh guarded ignored output and cached offline .NET8 dependencies:

```powershell
python poc/round77-tests/Networking/replay.py --output-dir local-private/round77-review-replay
```

Confidence: Confirmed tested synthetic construction/raw bytes/owner cleanup; exceptional construction scratch cleanup source-inspected. Server handshake provenance, live phase legality and original sender behavior remain Unknown. No socket/send/receive/login/EXE execution. Next: lead's new user settings supply connection ID/seed to this helper, retain synthetic labeling and caller Close, and remove hand-authored handshake hex.
