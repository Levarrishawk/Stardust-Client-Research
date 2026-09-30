# Status observation and framing boundary

Date: 2026-09-29 (America/New_York). Question: what status packets can the
offline transport accept without inventing field meanings or weakening its CRC
gate? Stop after tracing server construction/send/receive, reviewing the existing
client dispatcher evidence, implementing bounded observations and negative tests.

## Inputs and reproduction

- Executable SHA-256 checked with `Get-FileHash` before reviewing address evidence:
  `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
- Stardust-3 source HEAD: `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`.
  Paths below are relative to that repository; prefix
  `MMOCoreORB/utils/engine3/MMOEngine/src/engine/service/proto/`.
- `BaseProtocol.cpp` SHA-256:
  `20D2FF71A8842FA796E7A172C384477E19B6B656BA1A68248A263E3C269BA094`.
- Tools: PowerShell source/hash inspection, existing private Ghidra dispatcher
  export, .NET 8 smoke suite, independent Python bitwise CRC/XOR model. No
  original executable, server, sockets or packet captures were run.

Commands from the client root:

```powershell
python tools/python/verify_status_vectors.py
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj
python tools/python/validate_organized_workspace.py .
git diff --check
```

## Evidence and interpretation

| Observation | Interpretation / confidence | Compatibility consequence |
|---|---|---|
| `packets/NetStatusRequestMessage.h` constructor writes opcode and BE16 tick only; `BaseClient::requestNetStatus`, line 1469, constructs and sends it without appending statistics | Confirmed source behavior: four-byte server request | Accept this bounded short form; do not require statistics |
| `packets/NetStatusResponseMessage.h` constructor writes tick and nine zero 32-bit values; sequencing false, compression true, CRC checking false | Confirmed source behavior: 40-byte response, field semantics not established by zeros | Keep raw wire words unnamed |
| `BasePacket::close`, line 63, disables compression for bodies under 100 bytes and appends marker plus two zero bytes; `BaseClient::sendUnreliablePacket`, line 682, and `sendUnreliablePackets`, line 715, close then transform | Confirmed source chain: both status forms are uncompressed and retain trailer space | CRC-disabled means zero reserved bytes, not a shorter datagram |
| `BaseProtocol::prepareEncryptionAndCompression` encrypts before conditional CRC append; `encrypt` excludes the two reserved bytes | Confirmed source chain: response marker/body encrypted, trailer stays zero | Do not strip CRC space or change selector-4 extent |
| `BasePacketHandler.cpp`, lines 65-76, processes `00 07`; `00 08` processing is commented out | Confirmed source behavior: directional asymmetry | Server response construction does not define server acceptance of a client reply |
| `BaseClient::handleNetStatusRequest`, line 1368, tries extra fields after tick inside a caught exception path, then sends a response | Confirmed source behavior: parser and minimal sender differ | Preserve discrepancy; no symmetric status codec claim |
| Existing client dispatcher at `0x012C3B60`, branch `0x012C3FF7`, reads through offset `0x27`, builds 40-byte `00 08`; branch `0x012C446F` also reads through `0x27` | Strongly inferred from existing decompiler export: extended status body spans 40 bytes; field labels remain unresolved | A short server request must not reproduce unchecked legacy reads; automatic client reply is deferred |

The original client's established receive CRC exception has **not** been traced
in this checkpoint. Server source alone cannot establish that client behavior.
The short request mismatch may reflect source drift or legacy tolerance; neither
is asserted as the explanation. No liveness/readiness percentage changes follow.

## Independent synthetic fixtures

Seed `A1B2C3D4`, tick `FFFE`; these are invented inputs, not captures.
`verify_status_vectors.py` independently models close/encrypt/conditional CRC:

```text
request: 00072B2AD493D0
response: 00082B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B2B2B0000
```

The 7-byte request has a computed CRC; the 43-byte response has zero reserved
CRC bytes. The C# suite consumes these independently generated fixed bytes.
It accepts the request and deliberately rejects the zero-trailer response.
This verifies replacement policy, not original-client interoperability.

## Implementation and validation

`SoeStatusCodec` accepts request lengths 4 or 40 and response length 40. Tick is
BE16; the remaining bytes are copied into nine BE32 **raw wire slots**. This
representation assigns no counter, signedness or 64-bit pairing semantics.
`SoeClientTransport.LastStatusPacket` records the latest validated observation,
including supported outer leaves; close clears it. No application payload or
control reply is emitted, no sequence state changes, and no deadline resets.
Normal CRC validation remains mandatory for both opcodes.

Smoke scenarios verify both shapes, all unsupported truncated prefixes, extra
bytes, input ownership, independent short-request framing, protected response,
corruption rejection, zero-trailer rejection, malformed outer-leaf prevalidation
and close cleanup. Full suite and Python fixture model pass. Workspace/link and
diff validation pass: 703 Markdown files, zero broken/private links, 2,585
provisional rows with zero duplicates, and 345 packet rows. `git diff --check`
passes. Prior dirty work is preserved; nothing is staged or committed.

## Remaining uncertainty and exact next probe

Follow-up: [direct receive-rule investigation](ANALYSIS_CHECKPOINT_2026-09-29_STATUS_CRC_RECEIVE.md)
confirms no opcode-08 CRC exception at positive width. The uncertainty recorded
below is historical; the current next probe is status reply fields and ownership.

Trace `0x012C36B0` receive validation and its CRC dispatch/callback chain for
opcode `00 08`: identify the exact opcode/length/configuration condition before
changing the framing gate. Then recover `00 07` request/reply field order and
counter ownership, including short-input/error paths and send preparation at
`0x012C5980`. Add an independent fixture for each supported exception and a
negative case for nearby opcodes. Automatic responses, periodic probes, RTT
matching, idle expiry and adaptive timing remain open. No full liveness closure
or live compatibility is claimed.
