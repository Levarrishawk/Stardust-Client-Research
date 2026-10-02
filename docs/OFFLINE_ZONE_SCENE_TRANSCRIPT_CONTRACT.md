# Offline reliable transport to zone-scene transcript

R3-NET-01, October 1, 2026. This engine-neutral proof composes existing
`SoeClientTransport`, R1 scene codecs and R2 transition coordinator. It opens
no socket, resolves no host, handles no credentials and constructs/applies no
scene or replicated objects. No production application router is added: the
bounded dispatcher is private to the proof.

## Evidence and scope

[SOE transport specification](SOE_TRANSPORT_PROTOCOL.md) records raw negotiation,
selector-4/seeded CRC, BE16 reliable sequences, ACK/reorder and BE32 fragment
totals. Existing transport supplies these functions unchanged.
[Scene codecs](ZONE_SCENE_PROTOCOL_CODEC_CONTRACT.md) supply the exact original
SelectCharacter and CmdStartScene layout; [R2 coordinator](ZONE_SCENE_TRANSITION_CONTRACT.md)
supplies owned opaque staging and deferred readiness. The client-backed
[scene lifecycle specification](LOGIN_AND_ZONE_STATE_MACHINE.md), lines 133–146,
and [packet map](PACKET_HANDLER_MAP.md) establish asynchronous start, one-shot
outgoing ready and later inbound ready. The executable SHA-256 was rechecked as
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Those recovered codec/lifecycle observations are **Confirmed**. This proof
establishes **Confirmed execution of synthetic offline cases**, not original
client execution or real server interoperability.

Every queue/generation/time/phase/failure choice is **modern policy**. Original
resource readiness, replication semantics, scheduler and notified subsystem
identities are not recovered by this transcript. The deferred lifecycle prose
is used; the older abbreviated sequence diagram does not define object timing.

The Python peer reuses `verify_status_vectors.frame`, an existing independently
written source-correlation selector-4/CRC model, and the R1 independent scene
parser. No peer frames are produced by C# framing or session codecs. Pinned hex
fixtures in the proof match independently regenerated Python frames, and an
oracle decrypts/validates actual C# outbound bytes and control sequence order.

## Fixed synthetic session and applications

Session request: connection ID 12345678, version 2, maximum datagram 32.
Raw response: `000212345678A1B2C3D402010400000020`: same ID, seed A1B2C3D4,
CRC width 2, transform selectors 1/4, maximum 32. This intentionally small size
forces fragmentation. It is a fixture negotiation, not a production default.

Client sequence 0 is exact SelectCharacter U64 F122334455667788, framed reliable
channel 0. The proof has no zone authentication phase: selection permission is
an explicit fixture precondition, not established by the raw SOE session.

Peer applications in reliable sequence order:

| Sequence | Transport/application |
|---|---|
| 0/1 | Two fragments carrying hand-authored CmdStartScene: flag A5, object F122334455667788, terrain A/B, floats X=1.5 Z=-2.25 Y=3.75, template T\C, U64 time FE02030405060708 |
| 2 | Ordinary opaque 14-byte ASCII SYNTH-CREATE plus 00/01 |
| 3/4/5 | Three fragments carrying opaque 71-byte SYNTH-BASELINE plus bytes 0 through 56 |
| 6 | Exact six-byte CmdSceneReady |

SYNTH-CREATE/SYNTH-BASELINE are labels/content of invented opaque test data.
They have **no Stardust opcodes, replication layout or object semantics**.
Their concatenated staged budget is 85 bytes/two messages. First fragment body
holds BE32 total plus at most 21 application bytes; later bodies hold at most 25.

## Positive transcript and observable gates

1. Encode SelectCharacter; independent peer ACK sequence 0 releases its retention.
2. Receive scene fragment 1 then opaque sequence 2. Both remain reordered; scene
   DTO/state is still Selecting. Destroy the borrowed input arrays.
3. CRC-corrupt and truncated scene fragment 0 both reject framing without
   advancing scene or receive order. Good fragment 0 then releases ordered 0/1/2:
   decode one owned scene DTO and stage exactly the opaque create. Mutate the
   delivered application arrays after routing to verify coordinator ownership.
4. Duplicate scene fragment 0 cannot dispatch a second start. Receive baseline
   fragment 5 before 3, then 4. No partial fragmented application is routed;
   complete baseline stages behind create. Transport control ACKs exist here,
   but **zero application CmdSceneReady publications** exist before the barrier.
5. Stale barrier completion is inert. Current completion at synthetic time 200ms
   publishes one owned result and hands ready to reliable outbound channel 0,
   sequence 1. Repeated completion is null. The wire is
   `0009D4C2B3A1F6DE4EE2F62BD8`.
6. Timeout policy 100ms: no retry at 299, exact retry at 300; mutate the returned
   initial datagram, then verify retry at 400 is still unchanged. Transport ACK
   sequence 1 releases retention; no retry remains at 500.
7. Later inbound reliable ready sequence 6 marks Ready. Its duplicate dispatches
   no second application. This server-ready signal is distinct from transport ACK.

The independently checked control sequence is OutOfOrder1, OutOfOrder2, ACK2,
ACK0 (old duplicate), OutOfOrder5, ACK3, ACK5, ACK6, ACK6. Payload publication is
owned and order-preserving; inspection copies do not count as new publication.
There is no old-scene model or application callback to mutate at any point.

## Failure, cancellation, phase and ownership limits

The private fixture dispatcher runs only after transport returns complete
ordered application messages. It examines LE32 opcode at offset 2; recognized
start/ready use exact codecs, every other fixture message stages opaquely only
while Initializing. Too-short application, wrong selected ID, premature ready,
or stage count/byte exhaustion fails the transition and closes its transport.
It publishes no ready or consumer data. This terminal integration policy drops
already staged data after failure; R2's individual budget rejection remains
atomic before the integration layer chooses failure/cleanup.

Reset/replacement closes the old transport before creating a fresh raw fixture
session; reorder buffers, fragment assembly and reliable retention are cleared.
The proof covers cancellation with both partial fragment and future message,
and replacement after an outgoing ready is retained but before server echo.
Old completion and old-token inbound data are ignored before transport decode.
Current completion alone transfers the current queue. Caller-owned publication
copies survive replacement; the caller must inspect their generation before use.
Close is idempotent and clears transport/coordinator-owned state.

Generation tags are local and supplied by the caller. The proof deliberately
uses a fixed session ID/seed when replaying fixtures, so it cannot authenticate
an old wire frame mislabeled as current. Neither SOE framing nor header-only
ready adds a scene generation field. A future host must define connection/routing
identity and delayed-frame policy. Same-transport replacement while old fragments
remain buffered is explicitly outside this proof. Unknown/post-ready traffic is
not applied or generally supported; it would trigger the private fixture policy.
No account ownership, terrain completion or full zone-in claim follows.

## API and reproduction

`OfflineZoneSceneScenario.PeerFixtures()` returns fresh read-only hex fixtures.
`Run(optionalPeerHexDictionary)` executes positive/negative transcript assertions
and returns an owned immutable-string/read-only transcript snapshot. Inputs can
be independently generated by Python; the default runner is self-contained.
`OfflineZoneSceneScenarios.Run()` is the proposed shared runner registration.
The standalone console runs defaults and optionally `--peer file --export dir`.
[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R3_OFFLINE_ZONE_SCENE.md)
records commands/results and baseline scope. Next question for lead review:
production phase/routing identity policy before any authorized real-server work.
