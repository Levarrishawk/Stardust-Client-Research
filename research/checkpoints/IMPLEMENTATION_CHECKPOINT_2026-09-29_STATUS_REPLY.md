# Extended status reply layout and explicit builder

Date: 2026-09-29, America/New_York. Question: which fields does the original
client place in a 40-byte status reply, and when are its counters sampled?
Stop after direct construction/send/receive-accounting evidence, an engine-neutral
builder and independently authored positive/negative fixtures. Automatic scheduling
and missing short-request fields are outside this checkpoint.

## Inputs and reproduction

Executable identity checked before address use:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Server source remains Stardust-3 HEAD `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`.
Direct Capstone disassembly through existing `tools/python/disasm_range.py`
checked `0x012C3FF7`–`0x012C446F`, `0x012C5980`–`0x012C5B4A`,
`0x012C54B0`–`0x012C54F9`, and `0x012C8C40`–`0x012C8C71`.
The existing private decompiler export was a lead; instructions resolve its
misleading four-byte word grouping and shift-helper expressions.

```powershell
python tools/python/disasm_range.py C:\Stardust-DEV\Stardust.exe 0x012C3FF7 0x012C446F
python tools/python/disasm_range.py C:\Stardust-DEV\Stardust.exe 0x012C5980 0x012C5B4A
python tools/python/disasm_range.py C:\Stardust-DEV\Stardust.exe 0x012C54B0 0x012C54F9
python tools/python/verify_status_vectors.py
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj
python tools/python/validate_organized_workspace.py .
git diff --check
```

Disassembly output is private evidence; curated documents retain authored
descriptions and addresses. No executable execution, socket activity or captures.

## Recovered schema

Extended `00 07` request: opcode (2), BE16 tick (2), five BE32 fields at offsets
4/8/12/16/20, BE64 packet-sent count at 24 and BE64 packet-received count at 32.
The five 32-bit fields are decoded into connection slots `+AC/+B0/+B4/+B8/+BC`;
their detailed meanings remain unknown here. Counter labels are **Strongly
inferred**, supported by independent Core3 parser correlation below. Structure,
offsets, byte order and echo relationships are **Confirmed**.

Reply layout:

| Offset | Width/order | Value | Evidence / confidence |
|---:|---|---|---|
| 0 | 2 bytes | `00 08` | writes at `0x012C41A3`/`0x012C41AB`; Confirmed |
| 2 | BE16 | request tick | read `0x012C400E`/`0x012C4014`, emitted `0x012C41DE`/`0x012C41E2`; Confirmed |
| 4 | BE32 | low 32 bits of local time helper | call `0x012C41E6`, serialize through `0x012C420B`; Confirmed value; timestamp interpretation Strong |
| 8 | BE64 | request bytes 24–31 | decode `0x012C40B4`–`0x012C4144`, encode `0x012C4215`–`0x012C4281`; Confirmed echo |
| 16 | BE64 | request bytes 32–39 | decode through `0x012C41DA`, encode `0x012C428D`–`0x012C4300`; Confirmed echo |
| 24 | BE64 | connection counter `+50/+54` | reads/serialization `0x012C42EA`–`0x012C4374`; Confirmed; packet-sent meaning Strong |
| 32 | BE64 | connection counter `+58/+5C` | reads/serialization `0x012C4361`–`0x012C43E9`; Confirmed; packet-received meaning Strong |

The time helper at `0x012C8C40` calls IAT `0x015DC174` (`KERNEL32!GetTickCount`,
verified with `tools/python/pe_imports.py`), tracks low-word wrap,
and returns an extended value; the reply stores only EAX. Clock epoch is not
part of the recovered wire contract. The replacement takes an explicit uint
timestamp rather than introducing a clock dependency.

## Ownership, ordering and send consequences

- At `0x012C3FF7` the original handler skips work if connection field `+1B0`
  exceeds 1000. No request-length check appears in this branch before reads
  through offset 39. This is observed legacy behavior, not a policy to reproduce.
- Receive accounting at `0x012C3708`–`0x012C3719` increments `+58/+5C` before
  CRC validation/dispatch. Its lifetime includes received datagrams, not only
  delivered application messages.
- Raw send at `0x012C54CE` calls the manager, then `0x012C54D3`–`0x012C54EE`
  increments `+50/+54`. Reply snapshots are serialized before this send call;
  the reply itself is excluded from the sent value it reports.
- At `0x012C43DE`–`0x012C43ED`, the handler passes a 40-byte body to
  `0x012C5980`. That function applies configured outbound transforms, calculates
  normal seeded CRC at `0x012C5AC0`, appends the configured trailer, then calls
  raw send at `0x012C5B3B`. No reliable sequence is assigned here.
- Independent server correlation:
  `MMOCoreORB/utils/engine3/MMOEngine/src/engine/service/proto/BaseClient.cpp`,
  `handleNetStatusRequest` at line 1368, consumes five 32-bit unknowns after tick
  and two network-order long counters, storing total packets sent/received.
  `system/net/Packet.h:235` defines `parseNetLong` as uint64.
  Its opcode-08 receive branch remains disabled; it does not prove reply use.

Counters preserve all 64 wire bits. The prototype uses unsigned representations
without asserting legacy signed overflow behavior. Automatic filtering by the
legacy `+1B0` field is not implemented; the builder has no such runtime field.

## Implementation and independent fixtures

`SoeStatusCodec.CreateResponse` requires exactly a 40-byte `00 07` request and
a caller-owned timestamp/sent/received snapshot; it echoes counter bytes directly.
`DecodeResponse` provides the six typed fields above. Raw observation remains
available for unresolved request words. `CreateStatusResponseDatagram` requires
an established transport, protects the response normally, clears its temporary
plaintext, and retains no reliable retransmission entry. The caller must decide
whether to reply and supply counters measured before sending it.

Python `struct.pack` and the independent bitwise framing model produce synthetic
fixtures with distinct high/low counter halves:

```text
request: 0007FFFE000000010000000200000003000000040000000501020304818283841112131491929394
reply: 0008FFFEAABBCCDD0102030481828384111213149192939421222324A1A2A3A431323334B1B2B3B4
wire: 00082B3D181AE7E01918E4E4989A676089887474181AE7E03938C4C4989A6760A9A85454181AE7E054A8DD
```

C# matches both reply and protected wire, decodes exact typed fields, preserves
zero/max 64-bit counters, rejects all truncated prefixes (including the short
server request), extra bytes and wrong direction, and confirms no retransmission
entry. The complete core suite and independent Python model pass. Consolidated
validator: 705 Markdown files, zero broken/private links, 2,585 provisional rows
with zero duplicates and 345 packet rows. `git diff --check` passes. Prior dirty
work is preserved; nothing is staged or committed.

## Next action and limits

Implement caller-owned datagram accounting and monotonic-clock binding at the
host boundary, with explicit pre-send snapshots and tests for retries, damaged
receives and cleanup. Short four-byte server requests remain observations only:
no echo counters exist, and zero-filling would be replacement policy requiring
explicit documentation. Automatic replies/probes, RTT matching, idle expiry and
adaptive timing remain open. The normal CRC gate and known server mismatch are
unchanged. No full liveness or live interoperability claim; no metrics/registry
denominator changes or new provisional names.
