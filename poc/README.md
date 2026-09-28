# Stardust Client Prototype

This prototype develops the engine-neutral Stardust client core and verifies
that Godot can host it. It does not contact a live server.

## Boundaries

- `Stardust.Core` contains no Godot references.
- `Stardust.Core.SmokeTests` validates exact recovered wire vectors, bounds,
  reliability behavior, and deterministic rejection of malformed data.
- `Stardust.Godot` hosts the same offline transport transcript in headless and
  graphical modes.
- No credentials, live endpoint, socket, TRE archive, or game asset is used.

## Offline SOE implementation in progress

`Stardust.Core.Protocol.SoeSessionCodec` implements the recovered raw 14-byte
SessionRequest and 17-byte SessionResponse layouts. It checks packet length,
opcode, version, echoed connection ID, CRC width, and nonzero datagram size.
`SoeSeededCrc`, `SoeSelector4`, and `SoeDatagramCodec` implement the confirmed
established-session framing, including bounded receive-side zlib inflation and
uncompressed outbound packets. `SoeReliableChannel` covers bounded
four-channel sequencing, reorder, cumulative ACK, selective out-of-order ACK,
retransmit timing, and 16-bit wrap expansion. `LoginProtocolCodec` serializes
the confirmed `LoginClientId` shape and parses the fixture-backed token and
error responses while retaining the credential blob byte-for-byte. The core
also handles multipayload length boundaries and bounded inbound/outbound
fragmentation, including channel-zero fragments received out of order. The offline hosted
transcript continues through cluster enumeration, separate zone/ping endpoints,
and character enumeration with explicit 64-bit object IDs. `LoginClientSession`
owns this phase's state and clears retained credential-bearing buffers during
replacement and shutdown. `LoginClientCoordinator` turns monotonic clock ticks
and inbound datagrams into session retries, login sends, ACKs, and reliable
retransmissions without owning a socket.

Outbound compression and UDP socket ownership are not implemented yet. Live
connection testing remains a separately authorized milestone.

## Pass criteria

1. Both projects compile for x64-capable .NET 8.
2. The standalone core matches the documented SOE and login golden vectors and
   rejects malformed, corrupt, oversized, and out-of-window input.
3. Godot invokes the offline SOE session-to-login transcript without a render
   window or frame-dependent state.
4. Godot graphical mode presents resulting state without owning it.
5. Startup and shutdown finish without errors.

Run the engine-neutral fixture and state-machine suite from the repository root:

```powershell
dotnet run --project .\poc\Stardust.Core.SmokeTests\Stardust.Core.SmokeTests.csproj
```

Set `$godot` to the Godot 4.7.2 .NET x64 console executable, then run:

```powershell
& $godot --headless --path .\poc\Stardust.Godot -- --self-test
```
