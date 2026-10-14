# Replacement client login-to-zone session

## Bounded capability and evidence

ReplacementClientSession (external historical reference) connects the existing real login coordinator/codecs to a separate fresh SOE zone transport. Its endpoint is permission-gated SelectCharacter submission. It delivers subsequent complete zone application payloads to the caller, but does not apply CmdStartScene, create a world, publish scene readiness, render or open sockets. This is a reusable engine-neutral component and an authored in-memory transcript proof; live server compatibility remains Unknown.

The byte layouts are the existing [login/zone specification](LOGIN_AND_ZONE_STATE_MACHINE.md) and lead-reviewed ZoneAuthenticationCodec. Server correlation supports the permission gate; it does not independently prove acceptance by a running server. The Runtime worker consumed only the lead-reviewed staged codec SHA256 `05f104518ec19bd01b5c27c7c48fff36e08ce4df9756f388e933da43529ebf37`. That dependency was copied into private evidence, not the frozen public worker tree.

## Stable API

Namespace `Stardust.Core.Session`:

```csharp
var session = new ReplacementClientSession(LoginClientOptions options,
    Action<ReplacementClientDiagnostic>? diagnostics = null);
ReplacementClientExchange first = session.StartLogin(uint connectionId, long nowMilliseconds);
ReplacementClientExchange auth = session.Authenticate(string username, string password, long nowMilliseconds);
ReplacementClientExchange received = session.AcceptIncoming(ReplacementConnection connection,
    ReadOnlySpan<byte> datagram, long nowMilliseconds);
ReplacementClientExchange handoff = session.BeginZone(ulong objectId, uint zoneConnectionId, long nowMilliseconds);
ReplacementClientExchange polled = session.Poll(long nowMilliseconds);
session.Dispose();
```

`ReplacementConnection` is Login or Zone. Each `ReplacementClientDatagram` has that routing tag and owned byte array. Exchanges contain outgoing Datagrams, owned ZoneApplications, RejectedFraming and safe FailureType. Callers must send all returned controls and own/clear outgoing arrays, including encoded authentication datagrams. The session cannot zero arrays transferred to a caller. No socket, DNS lookup or port ownership exists here.

Public observations are Phase, Characters (defensive read-only array), ZoneEndpoint, FailureType, DiagnosticFailures, SelectionSubmissions, CredentialLength, ZonePendingMessages and ZoneRetainedPackets. ZoneEndpoint exposes Hostname/Port solely for explicit in-memory routing and has a redacted ToString. Session ToString and diagnostics contain phase/count/failure type, never endpoint, account, username, credential or password. Immutable login options supply version and timing; the session adds a 256-character version bound required by the zone codec.

## Transitions and selection admission

| Phase | Allowed next boundary |
| --- | --- |
| Offline | StartLogin returns Login SessionRequest |
| LoginHandshake | Valid matching SessionResponse establishes login; retry/deadline apply |
| LoginAuthentication | Explicit Authenticate sends real login application; bounded wait for credentials |
| LoginResponses | Real token/cluster/status/character messages complete the login success set |
| CharacterList | BeginZone admits identity/endpoint and returns a new Zone SessionRequest |
| ZoneHandshake | Matching fresh response establishes zone before any ClientId is queued |
| ZonePermissions | Valid permissions with CanLogin=true queue exactly one SelectCharacter |
| Selected | Poll/receive continue reliability and transfer complete zone application payloads |
| Faulted / Closed | No successor, selection or active transport; Dispose is idempotent |

All characters must have unique object IDs. The selected ID must occur exactly once and be nonzero (explicit local impossible-identity rejection policy). Its galaxy must have exactly one cluster and status record. The status needs a bounded nonempty hostname and nonzero zone port; ping port is never substituted. Whitespace/control, URI/path-shaped hostnames reject; IPv6 literals are allowed without resolving them. This admission does not invent a new availability/population policy. Zone connection ID must differ from login connection ID.

BeginZone defensively copies the opaque token-plus-account blob, bounded 4..65536 bytes, then disposes/releases login state. A newly owned zone SOE transport receives a fresh handshake. Only after establishment does the reviewed codec encode ClientId; the temporary application and unencoded credential are zeroed after queueing. Queued reliable pumping handles messages larger than negotiated datagrams and larger than one send window. ACK releases, receive and Poll advance queued fragments; retransmission remains on the established zone transport. Credentials inside queued/retained transport plaintext remain owned until ACK or close and are zeroed by transport cleanup.

Permissions are required after all authentication fragments have entered the send window; they need not all have been ACKed. No Select is queued on denial, malformed permission or an unexpected pre-permission application. A duplicated permissions application is invalid. A retransmitted reliable packet already consumed by SOE is harmless and does not become a second permissions application. When a valid permissions application and a later invalid permissions application share one delivered multipayload, failure clears the queued Select before any Select datagram publication. SelectionSubmissions counts accepted logical queue submissions, not retransmitted wire packets.

After selection, complete non-permissions payloads are copied to ZoneApplications and originals are zeroed. The next world bridge must validate their semantics; this component deliberately makes no scene-ready claim. Within a failed receive transaction, earlier copied application results are zeroed rather than partially published.

## Ownership, failure and clock rules

Mutating operations require the creator managed thread and prohibit reentry. Diagnostic exceptions are counted and cannot change the owner. Clock arguments are nonnegative and never decrease across operations. A regression rejects before mutation. Equal clock readings are legal. Handshake retries/deadlines use configured intervals; login response and zone permission waits use LoginResponseTimeout. Deadlines are checked on incoming calls as well as polls, so a late handshake cannot evade timeout by arriving before the next poll.

Invalid actions called through StartLogin/Authenticate/BeginZone/Poll throw and close owned transports. Incoming protocol/transition/deadline failures return an explicit FailureType with Faulted phase. Already assembled controls are preserved when available. If the frozen login decoder throws before returning controls, the entire failed connection is closed deterministically; no retry/selection buffer remains. Damaged protected framing leaves reliable state unchanged; zone exchanges expose RejectedFraming. Closed state remains Closed after rejected reuse. Cleanup zeroes credentials, queued/retained transport plaintext, endpoint and selection identity, and releases login/zone state. Returned arrays remain caller-owned.

## Reproduction and observed validation

The standalone project (external historical reference) compiles actual Core sources with the explicit reviewed codec. In an integrated checkout it defaults to the Core codec; in a frozen worker without that file, supply `-p:ZoneCodecSource=<reviewed-private-source>`. No unpublished sibling source is required.

```powershell
python poc/round83-tests/Runtime/fixtures.py --out <fresh-private-fixture.json> --client-id-opcode 0xD5899226 --permissions-opcode 0xE00730E5
dotnet build poc/round83-tests/Runtime/Runtime.csproj --artifacts-path <private-artifacts> --configfile poc/round83-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false -p:ZoneCodecSource=<reviewed-private-source>
dotnet <private-artifacts>/bin/Runtime/debug/Runtime.dll <private-fixture.json>
python poc/round83-tests/Runtime/verify.py --fixtures <private-fixture.json> --result <private-test-output.log> --out <fresh-private-receipt.json>
```

Use private APPDATA/NuGet.Config, DOTNET_CLI_HOME and existing exact offline NUGET_PACKAGES; all build output belongs below private artifacts. Fixture generation/output guards reuse the established symlink-aware private path validator. The accepted independent Python SOE peer is reused instead of adding a duplicate transport codec. Program receives exactly one fixture path and emits JSON to stdout, so capture that output for verify.py. All credentials/endpoints in these fixtures are authored synthetic values; no original account data is used.

Confirmed on 2026-10-06: warning-free standalone full-Core-source build; 4245 checks and 27 rejection cases. The real login path populates enumeration, a fresh zone handshake preserves opaque credentials, accepted permissions submit one Select and a subsequent independently authored valid operand9 CmdStartScene is delivered unchanged. Its existing decoder confirms selected object ID, finite coordinates and galactic time; no world is created. The independent Python peer verifies 65566 authentication bytes over 1151 protected fragments with contiguous sequence numbers. Negatives cover duplicate/missing identities, endpoint/port errors, distinct connection IDs, wrong phase/response, denied/malformed/repeated permissions, deadlines, clock regression, foreign owner and damaged framing. Reflection in tests observes actual private pending/retained arrays zeroed on partial-auth cancellation/deadline; it is not a production inspection API.

Exact next action: lead replays this component and connects selected zone application delivery to the existing world owner in a separately authorized slice. Actual server login, selection and disconnect remain unverified until bounded controlled-server authorization and validation.

## Round84 selected application submission

`SelectedObjectId` exposes the admitted selected identity and is cleared on every release/failure/close. `QueueZoneApplication(ReadOnlySpan<byte> application, long nowMilliseconds)` returns a ReplacementClientExchange and requires Selected phase plus a complete application header (at least six bytes). It copies the application into the owned reliable queue and pumps every currently available fragment. ACK/receive/Poll continue a message that exceeds the send window. Returned datagrams are caller-owned; this method does not validate application semantics, send a socket, or bypass the permission gate. Invalid queue admission follows the existing fail-closed session policy.

The [scene bridge](REPLACEMENT_SCENE_BRIDGE_CONTRACT.md) uses this operation for one ready message after its independent minimum-resource barrier. It calls world.BeginSelection only to bind generation; the session remains the sole SelectCharacter sender. The session itself still makes no scene readiness or visual publication claim.
