# Injected replacement-client datagram pump

[ReplacementDatagramPump](../poc/Stardust.Core/Hosting/ReplacementDatagramPump.cs), namespace `Stardust.Core.Hosting`, drives the frozen [ReplacementClientSession](REPLACEMENT_CLIENT_SESSION_CONTRACT.md) through caller-injected `IDatagramPort` instances. It creates no UDP socket, DNS resolver, host timer, worker thread, or original executable process. The tested capability is real offline login/enumeration → fresh zone authentication → permission-gated selection through asynchronous injected ports; live interoperability remains Unknown.

## Frozen API and ownership

```csharp
var pump = new ReplacementDatagramPump(session, loginPort, loginPeer);
pump.StartLogin(loginId, now);
pump.Authenticate(username, password, now);
pump.BeginZone(objectId, zoneId, zonePort, zonePeer, now);
pump.Submit(exchange);
IReadOnlyList<ReplacementClientExchange> observations = pump.Tick(now);
pump.Dispose();
await pump.DrainCompletion;
```

Signatures are `ReplacementDatagramPump(ReplacementClientSession, IDatagramPort, DatagramPeer)`, `StartLogin(uint,long)`, `Authenticate(string,string,long)`, `BeginZone(ulong,uint,IDatagramPort,DatagramPeer,long)`, `Submit(ReplacementClientExchange)`, `Tick(long)`, `Dispose()` and `Task DrainCompletion`. Additional primitive `FailureType` reports only a type name. All mutable calls require the creating managed thread, reject reentry and validate nonnegative nondecreasing clocks before mutation. Equal clocks are permitted. Injected ValueTask methods must return promptly: the pump does not block waiting for I/O in Tick.

The pump owns the admitted login port from successful construction. A different zone port becomes owned only after session selection/endpoint admission and exact zone-port validation succeed. Rejected zone candidates remain caller-owned. Explicit `DatagramPeer` address and port are pinned verbatim; no implicit hostname trust or resolution occurs. Zone peer port must equal the admitted `ZoneEndpoint.Port`, not its ping port. Mapping the admitted hostname to an explicit numeric address and providing a login peer consistent with login configuration remain caller responsibilities.

The session is **borrowed**: caller closes/disposes it, including credentials and internal transport buffers, after normal completion or pump failure. Pump Dispose never competes for that lifetime. Tick-produced exchanges transfer owned application arrays and flags to the caller and always have empty Datagrams. `Submit` takes only wire-buffer ownership; caller application arrays/flags remain caller-owned. Exchanges produced internally by session calls retain flags/applications for the next Tick. Applications not yet transferred are cleared on pump failure/Dispose. The caller must clear transferred applications after routing them.

## Send/receive, bounds and retirement

StartLogin/Authenticate/BeginZone/Submit enqueue wire output; Tick begins I/O. Each lane has one pending receive and one serialized send. Completed receive/send tasks are observed only on Tick; session.AcceptIncoming/Poll and all other session mutations occur on the creator thread. A completed foreign receive (wrong address **or** port) is cleared without framing/session delivery. Accepted owned receive buffers are cleared after session ingestion, including failure. Tick calls the frozen session Poll to preserve reliable retransmission and fragment progression, and sends at most one queued wire buffer per Tick. Callers must tick promptly and provide bounded nonblocking ports; capacity overflow is terminal rather than silent loss/backpressure fabrication.

Admission bounds are256 queued/in-flight buffers and1048576 total owned wire bytes per lane, each wire1..65507 bytes. Full batch validation precedes enqueue: wrong connection tag, null/empty/oversized bytes, aliases and capacity overflow reject. Accepted buffers remain intact while SendAsync borrows them and are zeroed only after its completion. A synchronous send failure clears the unsent buffer during retirement; canceled late sends clear after actual completion. Caller reuse/aliasing of transferred arrays violates ownership and rejects; an in-flight borrowed alias is not prematurely cleared by rejected Submit.

BeginZone first uses the frozen session's identity/cluster/credential/zone handshake admission, then retires login I/O. It cancels the login token, invokes port disposal, clears queued unsent bytes, and asynchronously joins the outstanding send/receive/disposal tasks. The zone request is retained but **no zone send or receive begins until Tick observes successful complete retirement**. Late login receive results are cleared by detached cleanup and never enter the zone session. Failed retirement blocks zone traffic and retires its admitted candidate too. Cancellation/disposal invocation alone is not drain completion.

Detached cleanup may finish on a worker thread, but touches only retired lane buffers/tasks, never pump/session/successor state. Retirement task errors are observed and reduced to type names without provider messages/payload data. Dispose is owner-thread/idempotent, cancels ports and clears queued bytes, and leaves a Task that includes late results. Read/await the current DrainCompletion after Dispose or handoff; a Task previously read is a snapshot of retirements already admitted at that time. During active operation it does not mean the current port is closed. An injected port that ignores cancellation/disposal indefinitely can leave drain pending; no successor is admitted to wire I/O while this remains unresolved.

Clock/thread/reentry preflight rejects without wire ownership transfer. Operational/session/send/receive/capacity errors stop transmissions, cancel/retire owned ports and clear their queued buffers. Failed Tick returns a safe FailureType exchange; direct operation failures throw. Caller still owns session cleanup. No arbitrary exception message, endpoint, account or credential appears in pump diagnostics; original provider exceptions are not retained inside retirement error aggregates.

## Independent synthetic transcript and CLI

[transcript.py](../poc/round84-tests/Networking/transcript.py) reuses accepted83's independent fixture author and the accepted14 SOE encoder. All metadata is explicit:

```powershell
python poc/round84-tests/Networking/transcript.py generate --template-crc 0x11223344 --object-id 0x1122334455667788 --object-path object/creature/player/shared_authored.iff --terrain-path terrain/authored.trn --output local-private/round84-peer
python poc/round84-tests/Networking/transcript.py verify --transcript <transcript.json> --captured <captured.json>
```

`generate(output, template_crc, object_id, object_path, terrain_path)` validates bounded explicit metadata and writes ignored `transcript.json` plus the reused83 input fixture. JSON fields are LoginResponse/LoginReplies (four real small success grammars), ZoneResponse/Permissions, ordered WorldFrames (Start/Create/OpaqueCreoEnvelope/End/NonworldSentinel), separate ServerReady, ZoneAcks for auth/select/ready, Applications and expected auth/select/ready plaintext and protected wire strings. LoginId12345678/seedA1B2C3D4; zoneId87654321/seed0BADCAFE; max496; credential5 bytes. Character list identity/template CRC and scene/create identity match supplied metadata. Placement is native XYZ(1.25,2.5,-3.75), identity quaternion; raw scene coordinates serialize X/Z/Y. Known nonworld sentinel is a nonfatal authored ErrorMessage.

Peer reliable sequences are permissions0, Start1, Create2, baseline3, End4, sentinel5, server-ready6. Client expected auth/select/ready use0/1/2. A canned peer must withhold ServerReady until it observes actual ready submission, then provide ZoneAcks[2]; this author provides bytes, not automatic phase behavior. `verify(transcript, captured)` checks exact captured `{AuthWire, SelectWire, ReadyWire}` against independently encoded expected frames.

CREO view3 baseline has count0 and empty **opaque** fields: only its envelope is valid/verified. This is not a typed creature baseline, corpus evidence or full server-replication fidelity claim. It supports ordered staging tests and selected original-resource loading in the lead's later join. The current component ready frame is explicitly fed through Submit because frozen83 lacks a queue-application API; it is not evidence of the new minimum-resource barrier. No unpublished Runtime84/Assets84/Rendering84 dependency is consumed.

## Evidence, reproduction and precise next action

Round84, October6,2026: trusted sync passed accepted83 frozen4752 public files, manifest c980d7caf4cc29ee60a71032f9de41dbc80e53e5e00698fe191e909a1fa0bde8, archive d50f6d1dd0afae594ff9da236f0f20be0e0ab987d5d6e42b9f861ab2b4eba474; prior own seal/HEAD/index/private preserved. Renewed single-round authorization supersedes stale no-next-round navigation wording. Observations are direct frozen session/port/source APIs and independently authored byte fixtures; confidence Confirmed for the tested component boundaries, Unknown for running-server acceptance. No executable/VA analysis or registry/coverage changes.

```powershell
python poc/round84-tests/Networking/replay.py --output local-private/round84-review --dotnet <dotnet.exe> --packages <existing-offline-package-cache>
```

[Standalone project](../poc/round84-tests/Networking/Networking.csproj) builds with empty NuGet sources and private artifacts. Final180 native checks/10 rejection or failure cases plus7 Python negatives pass, zero warnings/errors. Real asynchronous late completion, exact peer filtering, serialized ownership, send/disposal failure, both capacity bounds, receive size, deadlines, thread/clock/reentry/cancellation and actual83 selection pass. An added in-flight alias test initially escaped its helper because InvalidDataException was missing from the expected catch set; the runner watchdog stopped that failed attempt. Private phase tracing confirmed correct production rejection/preserved borrowed bytes; the helper was corrected and drain waits bounded to two seconds. Process inventory was denied; the identified private diagnostic session was interrupted directly. The corrected replay passes. Python independently verifies exact auth/select/ready frames and rejects corrupted captures/invalid metadata. Auth payloads are authored synthetic credentials; private generated bytes are not real account material. Native ready is Submit-only and SceneBarrierNotTested is explicitly true. GPU/live/server/scene-ready/minimum-resource/resource actor integration is unexecuted by this component.

Next: lead consumes these sealed bytes and transcript, wires the reviewed bridge's owned output into Submit, checks one actual barrier-triggered ready on wire, gates server-ready injection, and joins installed player publication/cleanup. Caller must close session and await current pump DrainCompletion. This worker seals six exact scoped files and idles after one round; shared canonical reconciliation belongs to lead.
