# Packed appearance customization codec

Round20 Networking, October3,2026. [Codec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/PackedCustomizationCodec.cs) owns raw packed version1 records, without CIDM lookup, variable construction, world state, face composition or live replication. [Prior ingress evidence](CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md) supplies the outer field4 and callback boundary; this increment closes the concrete ranged/palette byte-reader question and implements a strict bounded replacement grammar.

## API and ownership

All types are in Stardust.Core.Protocol:

```csharp
new PackedCustomizationLimits(
    MaximumEncodedBytes: 1536, MaximumRecords: 255, MaximumDecodedBytes: 767);
new PackedCustomizationRecord(byte id, ReadOnlySpan<byte> rawValue);
new PackedCustomizationSnapshot(bool isOuterEmpty,
    IReadOnlyList<PackedCustomizationRecord> records);
PackedCustomizationSnapshot PackedCustomizationCodec.Decode(
    ReadOnlySpan<byte> source, PackedCustomizationLimits? limits = null);
byte[] PackedCustomizationCodec.Encode(
    PackedCustomizationSnapshot snapshot, PackedCustomizationLimits? limits = null);
```

Record properties: Id, ValueWidth and CopyRawValue(). IDs are1..127; raw value length is exactly1 or2. Constructor owns the supplied bytes; copies never expose retained buffers. Snapshot copies a bounded record list, preserving order and duplicates. Its properties are IsOuterEmpty, nullable Version, read-only Records and DecodedByteCount. Records are immutable, so the list can retain their references safely. Outer empty requires zero records, Version=null and decoded charge0; a nonempty version1/count0 snapshot has Version=1 and decoded charge2. No original variable clear/default action is implied by either form.

Decode publishes nothing until all structure, budgets and final EOS pass; rejected partial records remain unreachable. Decoded snapshots retain an owned exact original encoded span. Encode of such a snapshot returns a new exact wire copy, preserving permitted raw00/noncanonical escape spelling. Encode of a newly constructed snapshot produces canonical escaping. This distinction makes decoding lossless while allowing explicit construction without inventing integer quantization. Zero-length outputs contain no mutable elements. There are no external resources, queues, owner-thread requirements or Dispose lifecycle in this pure codec.

## Strict grammar and limits

```text
outer-empty = zero bytes
otherwise:
  decoded-byte version = 1
  decoded-byte recordCount = 0..255
  repeat recordCount:
    decoded-byte tag = ID | (valueWidth == 2 ? 0x80 : 0)
    decoded-byte rawLow
    if tag & 0x80: decoded-byte rawHigh
  raw FF 03 exactly at end
```

A decoded byte is a non-FF raw octet, or FF01=>00, or FF02=>FF. FF03 is EOS and is never decoded into a field. Unknown escapes, loneFF, premature EOS, wrong version, ID0, truncation, absent/incorrect final EOS and any trailing bytes reject with InvalidDataException. Unescaped00 is accepted and preserved because the original unescaper appends it. No textual/Unicode interpretation, sorting, deduplication, CIDM resolution or palette selection occurs. A high bit on tag is value width; it is unrelated to factoryFlag/private namespace.

Encoded byte budget is checked before decode allocations. Record count is checked before allocating the record array. Decoded charge includes version/count and each ID/value octet, excludes escape expansion and EOS; it is checked as fields are read. Encode preflights count, decoded and complete encoded length before output allocation. Defaults are1536 encoded/255 records/767 decoded bytes. Valid configurable ranges: encoded0..1MiB, records0..255, decoded0..767; negative/overrange options throw ArgumentOutOfRangeException. A valid record set's maximum canonical length is1535; default1536 is a conservative cap. All-zero budgets accept only outer-empty. Null snapshot/list, null list element, invalid constructed ID/width/count and outer-empty with records throw argument exceptions. No caller data or callback is mutated.

Strict final EOS, full-tail rejection and atomic publication are **replacement policy**. Original00B33F70 invokes setters sequentially and lacks a success-tail equality test;00B33020 diagnoses some escape anomalies. The new codec does not reproduce unsafe/prefix mutation recovery. Count255 is a decoded count here, not the server parser's early-return sentinel. Duplicates survive as ordered records so a later adapter can explicitly choose its application policy.

## Concrete original setter evidence

Input Stardust.exe SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` was verified before VA reads. Existing RTTI/vtable tools identify:

| Class | RTTI type descriptor | Vtable | +10 packed setter | +1C int getter | +20 int setter |
| --- | --- | --- | --- | --- | --- |
| BasicRangedIntCustomizationVariable | 018B75E4 | 015EC8C4 | 00B49FD0 | 00B4ADE0 | 00B4ADF0 |
| PaletteColorCustomizationVariable | 0187C6C4 | 015EC920 | 00B49FD0 | 00B4B8D0 | 00B4B8E0 |

RTTI complete-object locators01625524/016255A0 have offset0. Both concrete classes share the same +10 function used by record reader00B3418C.

Observation:00B49FD0 takes decoded buffer, signed offset and width. Negative offset returns false at00B49FD6..FF1. Width1 bounds-checks offset against buffer length at00B4A05A..7A, reads movzx U8 at00B4A07D, and calls vtable+20 at00B4A086. Width2 bounds-checks offset+1 at00B4A015..38, forms little-endian S16 by high byte intoCH, movsxCX and OR low byte at00B4A03B..4A, then calls +20 at00B4A04F. Successful reader branches return true after the setter call, regardless of whether the concrete range check stored the attempted value. Unsupported width has a diagnostic branch; version1 only supplies1/2.

Observation: Basic setter00B4ADF0 performs signed minimum-inclusive/maximum-exclusive comparisons against object+14/+18 at00B4ADFD..AE04. Only in-range values replace object+10 and call00B4A300 (forwards to owning customization data). Getter00B4ADE0 returns object+10 directly. Palette setter00B4B8E0 rejects negative or index>=palette entry count (00AB7B00 returns vector-byte-span/4); only valid index stores object+14 and invokes the same owning-data helper. Getter00B4B8D0 returns object+14 directly. Neither concrete integer setter clamps an out-of-range attempt into a stored endpoint.

Both setters still call00B4A270 with the attempted value. That helper forwards to linked object+8's virtual+20 when present, including attempts invalid for the first object's range. Notification/linked-variable graph behavior is outside this codec. Palette color-fetch00B4B920 separately clamps a lookup index for a nonempty palette and has a null-resource fallback; that is distinct from stored integer mutation and must not be conflated with the packed setter.

Interpretation/confidence: **Confirmed** concrete one-byte U8/two-byte S16 conversion and local reject-with-old-value range policy from instructions, RTTI and vtable slots. This closes Round19's typed conversion question for these two classes only. Wire records remain raw because other variable classes, live mount/context, dependent forwarding and adapter application policy are separate. No original executable was run. Source correlation remains [the pinned server reference](CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md#server-correlation-and-limits), whose encoding/parse edge cases do not override these client instructions.

## Verification and handoff

[Public smoke entry point](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/PackedCustomizationScenarios.cs): PackedCustomizationScenarios.Run(peerPath, exportPath). [Runner](../../SWG-Godot-Client/poc/round20-tests/Networking/Networking.csproj) and [independent Python producer/oracle](../../SWG-Godot-Client/poc/round20-tests/Networking/peer.py) use only synthetic bytes, without production imports or original assets.773 exact cases cover all256 one-byte values, every high byte for a two-byte value, all127 IDs at both widths, signed boundaries, ordered duplicates, count255, outer-empty/version1-count0 and permitted raw zeros.11,995 malformed/budget/constructor probes and6,699 ownership checks pass. Every nonempty proper prefix rejects; zero prefix is the valid outer-empty alternative. Exact decoded-wire and separately constructed canonical outputs match the independent oracle.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r20'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round20 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round20/NuGet.Config
& $python poc/round20-tests/Networking/peer.py --generate local-private/round20/peer.json
dotnet restore poc/round20-tests/Networking/Networking.csproj --configfile local-private/round20/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round20-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round20/peer.json local-private/round20/export.json
& $python poc/round20-tests/Networking/peer.py --peer local-private/round20/peer.json --verify local-private/round20/export.json
```

Shared smoke build has zero warnings/errors and default offline regression suite passes. New shared registration is lead-owned. Static reproduction ranges and baseline/scope evidence are in the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R20_PACKED_CUSTOMIZATION.md). Next: independently join this raw snapshot to installed CIDM and declared variable/range context, preserving ordered duplicates and explicit empty/default policy, before current-incarnation appearance presentation. No dependency on another worker's same-round decoder/state and no live field4 implementation here. Original player values remain Unknown; research89%/readiness94%,2602 names and345 matrix rows unchanged.
