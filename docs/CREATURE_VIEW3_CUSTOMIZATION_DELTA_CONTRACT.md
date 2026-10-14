# CREO view3 customization deltas

Round21 Networking, October3,2026. Field4 codec (external historical reference) parses an owned ordered field4-only plan using frozen plan (external historical reference), existing ReplicationEnvelope and ReplicationByteString.Create. It does not decode [inner packed records](PACKED_CUSTOMIZATION_CONTRACT.md), resolve CIDM, apply world state, project appearance or notify a journal. Those boundaries remain separate.

## API

All types are in Stardust.Core.Protocol:

```csharp
new CreatureView3CustomizationDeltaLimits(
    int MaximumPayloadBytes, int MaximumFields,
    int MaximumStringBytes, long MaximumDecodedBytes);
CreatureView3CustomizationDeltaPlan Decode(ReadOnlySpan<byte> message, limits);
CreatureView3CustomizationDeltaPlan Decode(ReplicationEnvelope envelope, limits);
byte[] Encode(CreatureView3CustomizationDeltaPlan plan, limits);
ReplicationEnvelope EncodeEnvelope(CreatureView3CustomizationDeltaPlan plan, limits);
```

Methods belong to CreatureView3CustomizationDeltaCodec; RawTag=4352454F, View=3 and FieldIndex=4. The frozen plan exposes ObjectId U64, DeclaredCount U16 and read-only ordered Updates. Each update is an owned ReplicationByteString with Length, UsesExtendedLength and CopyBytes. IDs/counts remain raw values, including zero object ID or mismatched declared count; object/generation validity is a later state concern.

The whole APPLICATION envelope is count5 U16LE, DeltasMessage opcode12862153 U32LE, object ID U64LE, raw family tag U32LE, view U8, payload size U32LE, declared count U16LE, then fields. Payload size includes the declared-count bytes and must equal message length minus23. EncodeEnvelope returns an owned envelope without transport framing; Encode returns complete APPLICATION bytes through the existing envelope codec.

## Field grammar and spelling

```text
repeat until the envelope field span is exhausted:
  U16LE field index = 4
  U16LE prefix
  if prefix == FFFF: U32LE byteLength
  else: byteLength = prefix
  byteLength raw bytes
```

At least one update is required. Empty update bytes are legal. Repeated field4 updates, duplicates, short-empty, extended-empty and extended-small strings preserve exact order and length-prefix choice. Raw00/high bytes/FF escapes, malformed inner packed data and arbitrary opaque content are copied verbatim without strlen, NUL terminator, Unicode conversion, inner validation or default/clear inference. Length65534 can use short form; length65535 and above require extended form, as enforced by the frozen ReplicationByteString.Create factory. Encode reconstructs the same spelling from each update's UsesExtendedLength flag.

DeclaredCount is retained, not used as a loop bound or forced to match the parsed update count. This follows the accepted scalar/component codec and frozen plan's raw-metadata policy. Fixtures deliberately cover declared0/1/2/255/256/FFFF with two actual updates. This is a confirmed prototype policy; no new claim about every original generic-dispatch count edge case is made. Server builders normally supply their own count bookkeeping.

Other family tags (including inherited TANO), views, baseline envelopes, other indices and mixed4+other-field payloads reject. This is a bounded CREO:3 field4-only slice; existing scalar/mixed/component decoders are unchanged. A later composite dispatcher must choose routing explicitly rather than treating this parser's rejection as proof that original mixed deltas were illegal.

## Independent bounds, ownership and failure

MaximumPayloadBytes uses existing envelope bounds: at least2 and at most int.MaxValue-23. It includes declared-count bytes and all field indices/prefixes/content. MaximumFields permits0..4096; zero is a valid policy admitting no nonempty plan. MaximumStringBytes is a nonnegative int per update. MaximumDecodedBytes is a nonnegative long aggregate over retained raw string content only. Length prefixes, fixed indices, object/count metadata and CLR overhead are excluded from this logical decoded charge; the independent field and wire caps bound their allocations. Empty repeated updates can charge zero raw bytes while remaining constrained by count/payload caps.

Decode validates limits and envelope identity/extent first. Before each owned string copy it validates per-string/aggregate length and available bytes. Extended U32 lengths above int.MaxValue are rejected before cast because the per-string cap is an int. Updates remain private until the entire field span validates; a late wrong index or incomplete prefix/string yields no plan. Encode preflights update count, every string, aggregate and complete wire length before allocating fields. Long extent arithmetic precedes narrowing to int. All four APIs enforce the same inclusive caps.

Malformed data/budget/unsupported envelope/index/zero-update errors throw InvalidDataException. Null arguments and invalid limits throw argument exceptions. Input buffers/list changes and CopyBytes/output-array clearing cannot affect an issued plan/envelope. The codec retains no caller buffer and creates no callbacks, queue, lifecycle/generation state, resources or owner-thread guard. Atomic rejection is explicit modern policy; original sequential customization setters remain separately documented. Reentry/stale/generation tests belong to the later journal/world adapter, not this pure byte codec.

## Evidence and compatibility

[Inherited client ingress evidence](CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md) confirms the paired00641030/00641180 extended-byte-string boundary and changed-byte appearance callback; [Round20](PACKED_CUSTOMIZATION_CONTRACT.md) separately confirms concrete U8/S16 setters/range behavior. No new VA probe or broad static sweep occurred here.

Local Stardust-3 HEAD9a8529ef8291efc5ca1b41d9927f0e88d64b79bb correlation:

- `MMOCoreORB/src/server/zone/packets/tangible/TangibleObjectDeltaMessage3.h:23..26`, updateCustomizationString obtains app and addAsciiUpdate(4,app); file SHA-256 C7883C45E68863D8ADDBBC197389D798A7C43FEC0453D3D5327D79ED8383ED58.
- `MMOCoreORB/src/server/zone/packets/DeltaMessage.h:78..80`, addAsciiUpdate calls startUpdate(type) then insertAscii(val.toCharArray()); file SHA-256005F4E15FE6232D75569C37B26B9B659011EA0750605DE80AA397C1593D1036A.

The referenced server char-pointer path can truncate raw embedded NUL; preservation fixtures test client-compatible outer acceptance, not canonical server emission. Source correlation does not establish original callback transactionality or character values. Observation/confidence: Confirmed implemented bytes/ownership/caps via native plus independent fixtures; inherited outer grammar is confirmed prior research. Strict atomic whole-plan errors and field4-only routing are modern compatibility policy. Consequence: feed all ordered updates into the later single-consumer journal; do not collapse them to last bytes and lose earlier valid writes before a later out-of-range value.

## Verification and resume

Scenario (external historical reference) exposes CreatureView3CustomizationDeltaScenarios.Run(peerPath,exportPath). Runner (external historical reference) and the separate implementation record use synthetic bytes only, with no production-code imports, original assets or live contact.21 exact fixtures cover all-octet/embedded bytes, repeated/empty updates, raw count mismatches, length1/2/254/255/256/65534/65535/65536 and4096 fields.2,098 negative probes cover exhaustive small truncations, boundary/final large prefixes, wrong header/family/view/kind/index, late mixed index, short/extended extents, zero updates, tail bytes and each independent budget/invalid option.8,376 checks verify input/raw/list/output/envelope ownership and inclusive exact caps. Python independently reads bytes and verifies positive receipts and malformed producer fixtures.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r21'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round21 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round21/NuGet.Config
& $python poc/round21-tests/Networking/peer.py --generate local-private/round21/peer.json
dotnet restore poc/round21-tests/Networking/Networking.csproj --configfile local-private/round21/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round21-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round21/peer.json local-private/round21/export.json
& $python poc/round21-tests/Networking/peer.py --peer local-private/round21/peer.json --verify local-private/round21/export.json
```

Shared smoke build/suite and prior packed/outer-baseline regressions pass. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_CUSTOMIZATION_DELTA.md) records identities and scope. Next lead-owned step: register the scenario and route issued ordered plans into the current-incarnation raw journal. Mixed4+otherfields, Assets projection/CIDM and live state are separate; no same-round worker implementation is used. Original player values remain Unknown. Metrics89%/94%,2602 names and345 packet rows unchanged.
