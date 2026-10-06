# Zone authentication application codec

[ZoneAuthenticationCodec](../poc/Stardust.Core/Protocol/ZoneAuthenticationCodec.cs) implements the existing [login-to-zone specification](LOGIN_AND_ZONE_STATE_MACHINE.md) in engine-neutral `Stardust.Core.Protocol`. This codec does not establish transport, validate credentials, enforce session phases or send packets.

```csharp
byte[] owned = ZoneAuthenticationCodec.EncodeClientId(opaqueCredential, configuredClientVersion);
ZonePermissions flags = ZoneAuthenticationCodec.DecodePermissions(applicationMessage);
```

`ZonePermissions` is a sealed immutable record with `CanLogin`, `CanCreateRegular`, `CanCreateJedi` Boolean properties and nullable Boolean `CanSkipTutorial`. Three-byte historical permission payloads produce null for the fourth field, distinct from an explicit false. Login denial parses successfully as false; the session owner must gate character selection.

## Bytes and admission

`EncodeClientId(ReadOnlySpan<byte> credential, string clientVersion)` returns a fresh caller-owned array. Header is operand3 U16LE, `ClientIdOpcode=0xD5899226` U32LE. Payload is gameBits0xFE U32LE, credential byte count U32LE, exact opaque credential bytes, version byte count U16LE and exact ASCII version bytes. Length is16+credential length+version length. No account ID is appended or reconstructed: the incoming login credential already includes its trailing account bytes.

Credential bounds4..65536 and version bounds1..256 are explicit modern admission limits. Version additionally requires printable ASCII0x20..0x7E, matching the existing login-options policy. Null/empty/oversized/nonprintable/non-ASCII version and short/oversized credential reject before allocation. NUL/control/DEL version rejection is local admission policy, not a recovered wire restriction; credential NUL/high bytes remain opaque and unchanged. Caller must provide its explicit compatible version. The codec has no default version, credential storage or diagnostic output. Exceptions use fixed messages/parameter names without credential/version contents. Input spans are borrowed for the duration of the call; returned bytes are independent and sensitive, so the owner must clear/release them after submission. The codec cannot clear its borrowed input.

`DecodePermissions(ReadOnlySpan<byte> message)` requires exactly9 or10 total bytes: operand4 U16LE, `PermissionsOpcode=0xE00730E5` U32LE, then exactly3 or4 flag bytes. Each byte must be0 or1. Truncation below9, extra bytes, wrong operand/opcode and noncanonical Boolean bytes reject with `InvalidDataException`. Results retain only primitive flags and no source memory. Strict flag/size rejection is a replacement policy; it does not claim the historical client rejected every malformed variant.

## Evidence and confidence

Observation: `MMOCoreORB/src/server/zone/packets/zone/ClientIdMessage.h` lines13–25 writes operand3, name-hash opcode, gameBits0xFE, token length+4, token bytes, account ID and ASCII version. `ClientIdMessageCallback.h` parse reads gameBits, length minus4 token bytes, account ID and ASCII version. `ClientPermissionsMessage.h` lines13–26 and30–43 writes operand4, name-hash opcode and four Boolean bytes. Server correlation supports field order, while the durable client specification independently records complete opaque login-to-zone credential handoff and tolerated historical three-flag parsing. No new executable analysis or virtual-address reliance is involved here.

Opcodes are calculated by the reused [independent login/zone fixture tool](../tools/python/verify_login_zone_fixtures.py), first checked against fixed LoginEnumCluster0xC11C63B9 and LoginClusterStatus0x3436AEB6. Exact case-sensitive names ClientIdMsg/ClientPermissionsMessage yield0xD5899226/0xE00730E5. Canonical [server packet rows](../data/curated/server_packet_gap_matrix.csv) establish the corresponding message/header boundary. Their coarse ClientId literal0xFE is gameBits, not the name-hash opcode; it is not used as opcode authority and the registry is unchanged.

Confidence: Confirmed for the implemented bytes/bounds/ownership by independent synthetic fixtures and for existing documented raw structure. Field labels follow the durable specification; server comments alone do not prove all client gameplay semantics. Effective deployed version acceptance, real credential validation and live zone interoperability remain Unknown. Historical helper version20050408-18:00 differs from the documented Stardust default20260817-SD:01; this API preserves explicit caller configuration pending controlled validation.

## Reproduction and handoff

```powershell
python poc/round83-tests/Networking/replay.py --output local-private/round83-review
```

The standalone [test project](../poc/round83-tests/Networking/Networking.csproj) compiles only this codec, with empty NuGet sources and private artifacts. Independent Python expectations are written before native .NET execution. Three exact ClientId fixtures cover credential4/8/65536 bytes, version1/13/256 bytes, opaque NUL/high bytes and account preservation. All24 possible three/four-flag combinations, primitive independence after source mutation, malformed headers/lengths/flags and encode bounds/types including NUL/control/DEL version rejection pass65 checks/59 rejections. Build has zero warnings/errors. No real credentials, sockets, executable/server execution or GPU use.

Implementation consequence: submit ClientId only after a fresh zone SOE session is established, pass the unchanged login credential, and wait for CanLogin before exactly one SelectCharacter; these are session-owner responsibilities. Lead must review the sealed codec SHA before staging it for the Runtime worker. Next bounded integration ends at permission-gated selection, without scene-ready or live-compatibility claims. Shared canonical status/ledger remains lead-owned; no registry/coverage denominator changes.
