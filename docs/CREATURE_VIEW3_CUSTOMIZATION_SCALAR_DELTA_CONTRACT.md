# Ordered CREO customization and scalar delta codec

Round22 Networking, October3,2026. [Codec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView3CustomizationScalarDeltaCodec.cs) implements only CREO:3 field4 and indices11..16 using the lead-frozen [union plan](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView3CustomizationScalarDeltaPlan.cs). It joins the accepted [raw field4](CREATURE_VIEW3_CUSTOMIZATION_DELTA_CONTRACT.md) and [scalar](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md) wire encodings, preserving order across both kinds. No inner packed parser, CIDM/range projector, current state/journal, callback, resource or engine behavior is added.

## API and independent limits

All types are in Stardust.Core.Protocol. CreatureView3CustomizationScalarDeltaLimits has six required members:

```csharp
public sealed record CreatureView3CustomizationScalarDeltaLimits(
    int MaximumPayloadBytes, int MaximumFields,
    int MaximumScalarUpdates, int MaximumRawUpdates,
    int MaximumStringBytes, long MaximumDecodedBytes);
```

CreatureView3CustomizationScalarDeltaCodec provides Decode(ReadOnlySpan<byte>,limits), Decode(ReplicationEnvelope,limits), Encode(plan,limits) returning complete APPLICATION bytes, and EncodeEnvelope(plan,limits) returning an owned envelope. RawTag4352454F and View3 reuse accepted scalar constants; field4 reuses the accepted raw codec index. Plan exposes ObjectId U64, raw DeclaredCount U16 and read-only ordered union Updates. Each union has exactly one Customization (owned ReplicationByteString) or one of the six accepted Scalar records. The frozen factory rejects unknown scalar classes/nonfinite Height.

Payload cap follows ReplicationEnvelopeCodec:2..int.MaxValue-23, including count/index/prefix/value bytes. Total fields, scalar updates and raw updates are independently0..4096; zero valid caps admit only the other permitted kind, and zero total fields admits no plan. MaximumStringBytes is nonnegative int per raw update. MaximumDecodedBytes is nonnegative long over raw string bytes plus scalar value widths. Fixed indices, length prefixes, object/count metadata and CLR overhead are excluded from this logical charge; independent payload/count caps bound their allocation. Empty raw update charges zero decoded bytes but one raw/total field. No default limits are silently selected.

## Exact ordered boundary

The existing Delta APPLICATION envelope is retained unchanged: count5 U16LE/opcode12862153 U32LE/object U64LE/tag U32LE/view U8/payloadSize U32LE/raw declaredCount U16LE, then complete bounded field bytes. Decode iterates to field-span exhaustion rather than imposing raw DeclaredCount as a loop bound, following the accepted scalar/field4 count policy. Count mismatch is retained through encode; fixtures cover0/1/2/255/256/FFFF metadata. At least one actual field is required.

| Field index | Value bytes | Behavior |
| --- | --- | --- |
| 4 | U16LE length, orFFFF plus U32LE length, then raw bytes | Exact short/extended spelling and opaque bytes retained |
| 11 | U8 | Posture, no wire enum restriction |
| 12 | U8 | Rank |
| 13 | U64LE | Link ID |
| 14 | F32LE | Height must be finite; no positivity/range normalization |
| 15 | U32LE | Shock |
| 16 | U64LE | States |

Order is preserved across every raw/scalar element, including repeats. Extended-empty/small strings, embedded00/high octets, invalid-to-consumer inner escape bytes and arbitrary packed content remain opaque. No strlen, Unicode conversion, deduplication or last-value collapse occurs. Finite Height signed-zero/subnormal/extreme bits re-encode exactly. Negative finite Height is structurally accepted, as in the prior scalar codec; a consumer's additional physical/display policy is separate.

Component5, wounds17, all other indices/families/views and baseline envelopes reject. This slice does not claim broader mixed-field support or original packet illegality. Existing codecs and frozen DTOs/interfaces are unchanged.

## Atomicity and ownership

Decode validates envelope identity/payload, then independently caps each field/kind and value. String length/remaining extent/decoded charge are validated before owned copies. Extended lengths above int.MaxValue reject before narrowing through the int string cap. Scalar reads charge exactly1/4/8 decoded bytes and finite-check Height before union construction. Any late malformed field/index/string/Height/budget failure yields no plan; earlier local copies remain unpublished. No state callback or generation mutation is possible in this codec.

Encode preflights total/type counts, raw string length, scalar type/finite Height, aggregate charge and complete wire extent before output allocation. Long extent comparisons precede int arithmetic. Issued plan/raw string/envelope/output ownership remains independent from caller array/list/copy clearing. Typed scalar records and union are immutable. Null/invalid policy arguments throw argument exceptions; malformed or unsupported content/zero fields/budget violations throw InvalidDataException.

Strict full-plan rejection is modern replacement policy. Original ordered setters and their callback effects remain separately documented. No owner-thread/reentry/stale-generation lifecycle is asserted here; those tests belong to the later current-world transaction/journal adapter. Ordered raw updates must survive to that adapter: a later invalid consumer attempt cannot erase an earlier valid record merely because both appeared in one delta.

## Evidence and verification

Observation/confidence: Confirmed implemented wire/ownership/budget behavior via native execution and independent Python byte fixtures. Wire grammar and count policy reuse durable prior evidence and accepted codecs; no new executable/address/server sweep, canonical names or original values were introduced. Isolated mixed encodings are compared to the existing field4 and scalar encoders/decoders for every distinct small fixture row. Implementation uses their constants, DTOs, envelope and byte-string ownership primitives; it adds only ordered dispatch and independent caps.

[Public scenario](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CreatureView3CustomizationScalarDeltaScenarios.cs): CreatureView3CustomizationScalarDeltaScenarios.Run(peerPath,exportPath). [Runner](../../SWG-Godot-Client/poc/round22-tests/Networking/Networking.csproj) and [independent producer/oracle](../../SWG-Godot-Client/poc/round22-tests/Networking/peer.py) have no production-code imports or original inputs.58 exact cases,4,292 negative probes and6,887 ownership/cap/sibling checks pass. Cases cover each scalar/raw ordering, all scalar types, repeats, invalid inner data, empty/extended strings, raw count mismatch, finite min/max/subnormal/signed-zero Height, integer min/max, U64 high IDs, length65535 and4096 fields. Negatives cover late unknown indices, wrong tag/view/kind, partial scalar/string/extents, zero fields, all independent caps/options and NaN/infinity/signaling-NaN bit patterns. Small-fixture prefixes are exhaustive; large extents use initial/header and final16 prefixes. Python independently checks exact order, raw/scalar bytes, float bits and malformed producer cases.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r22'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round22 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round22/NuGet.Config
& $python poc/round22-tests/Networking/peer.py --generate local-private/round22/peer.json
dotnet restore poc/round22-tests/Networking/Networking.csproj --configfile local-private/round22/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round22-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round22/peer.json local-private/round22/export.json
& $python poc/round22-tests/Networking/peer.py --peer local-private/round22/peer.json --verify local-private/round22/export.json
```

Field4 regression21/2098/8376, shared default suite (including existing scalar scenarios) and zero-warning/error shared build pass. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_SCALAR_CODEC.md) records baseline/scope/reproduction. Next lead-owned step is register the scenario and feed one ordered plan into a current-target preflight/commit transaction spanning raw journal and scalar state. Broader component/wounds/TANO coverage and appearance/projector semantics remain separate. Original player values remain Unknown;89%/94%,2602 names and345 packet rows unchanged.
