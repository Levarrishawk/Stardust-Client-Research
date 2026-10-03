# Ordered extended CREO:3 delta contract

Round24 Networking, October3,2026. [CreatureView3ExtendedDeltaCodec](../poc/Stardust.Core/Protocol/CreatureView3ExtendedDeltaCodec.cs) adds the seven common fixed-width fields to the accepted [full ordered codec](CREATURE_VIEW3_FULL_DELTA_CONTRACT.md). It uses the frozen [extended plan](../poc/Stardust.Core/Protocol/CreatureView3ExtendedDeltaPlan.cs) and [tangible updates/state](../poc/Stardust.Core/Protocol/CreatureView3TangibleScalarState.cs). It implements only CREO view3 indices0,3..17. Names1/2, TANO and other views remain unsupported. This boundary does not apply state, interpret customization, publish callbacks or render visibility.

## API and budgets

All types are in Stardust.Core.Protocol:

```csharp
public sealed record CreatureView3ExtendedDeltaLimits(
    CreatureView3FullDeltaLimits Full, int MaximumTangibleUpdates);
```

CreatureView3ExtendedDeltaCodec exposes Decode(ReadOnlySpan<byte>,limits,woundsPolicy=StrictInterval), Decode(ReplicationEnvelope,limits,woundsPolicy=StrictInterval), Encode(plan,limits) returning APPLICATION bytes and EncodeEnvelope(plan,limits) returning an owned envelope. Constants RawTag4352454F and View3 reuse the full codec. The frozen plan preserves ObjectId, raw DeclaredCount, read-only ordered Updates, WoundsPolicy and both state-element caps. Each update has one Tangible or Full member.

Full.MaximumFields caps ALL actual fields, including common fields, at0..4096. Full.MaximumPayloadBytes caps the complete payload including declared count and all headers/indices/prefixes; accepted range2..int.MaxValue-23. Full.MaximumDecodedBytes is the aggregate logical value charge across BOTH kinds: common widths1/4 plus raw string content, existing scalar widths1/4/8, component keys4 and wounds values4. No independent reset occurs between adjacent kinds or compound fields. This charge excludes count/counter/length/index metadata, command opcodes, temporary copies and CLR overhead; independent payload/field/command caps bound those allocations.

MaximumTangibleUpdates is independently0..4096. Full.MaximumScalarUpdates counts only accepted creature scalar11..16; it is separate from common attempts. Full's raw/string, component command/key and wounds raw-command/value caps retain their exact accepted ranges and meanings. Frozen DTO caps4096 actual commands per compound update,65536 aggregate commands (component commands plus raw wounds counts) and65536 wounds values. Zero tangible cap still permits full fields; zero existing scalar/raw caps still permit common-only plans. At least one actual field is required.

State element caps remain explicit plan metadata. Assignments parse under the value budget even when their vector exceeds the state-element cap: the selected application kernel may skip stale work. Encode rejects plan metadata larger than supplied element caps. Initial-state index/growth/counter validity and global application revisions are later transaction responsibilities.

## Exact field boundary

The existing Delta APPLICATION envelope, LE field indices, raw declared count, count-mismatch tolerance and field-span termination remain unchanged. Order and duplicates are retained across every kind; fields are never sorted, deduplicated or collapsed to current state. The [replication schema map](REPLICATION_SCHEMA_MAP.md) supplies the established common field vocabulary and widths. This implementation adds no new executable address or original-value claim.

| Index | Value | Preserved boundary |
| --- | --- | --- |
| 0 | F32LE complexity | Finite; exact signed-zero/subnormal/extreme bits |
| 3 | U32LE volume | Raw unsigned value |
| 6 | U32LE options | Raw bits |
| 7 | U32LE use count | Raw unsigned value |
| 8 | U32LE condition damage | Raw unsigned value |
| 9 | U32LE maximum condition | Raw unsigned value |
| 10 | U8 visibility | Every byte accepted, including0 and255 |
| 4/5/11..17 | Accepted full codec values | Exact raw strings, scalars and compound grammar |

Complexity and Height must be finite, including negative finite values; neither receives positivity/range normalization. U32 values receive no cross-field clamp. Visibility receives no boolean normalization. Original callback/gameplay meaning is not established by structural acceptance. Raw customization strings keep short/extended spelling and opaque bytes. Component signed keys/counters and wounds raw command counts/counters retain wrapping representations.

StrictInterval consumes one wounds loop unit per command. LegacyBounded assignment consumes valueCount+1 units and may finish beyond the raw loop bound. Residual bytes are subsequent outer fields. Encode checks command starts/terminal cursor against that selected policy and rejects an inconsistent raw loop. It never replaces raw counts with actual command counts. Every accepted existing field is decoded/encoded through CreatureView3FullDeltaCodec after the extended whole-input preflight.

## Ownership and failure policy

Observation: Decode validates bounded envelope identity and scans all field extents, discriminators, finite floats and aggregate caps before nested owned-value construction. Only bounded range metadata is allocated during the scan; byte-message entry first uses the accepted bounded opaque envelope copy. Common values then use frozen factories; each existing field is delegated as a bounded isolated envelope to FullCodec. The returned frozen plan owns its collection and retains immutable nested values.

Encode computes complete length, common/full counts, all logical value charges and wounds loop shape before allocating output or temporary sibling buffers. Common bytes are written directly; full values use accepted per-field FullCodec encoding. Input arrays/lists, copy-returned strings, output and envelope copies cannot alter the issued plan. A late unsupported, malformed or over-budget field produces no plan/output. Invalid configuration/null/policy arguments throw argument exceptions; malformed content/budget violations throw InvalidDataException.

Confidence: Confirmed for the executed bounded implementation, via independent wire fixtures and native checks. Interpretation/consequence: a later adapter can consume the original ordered attempts and explicitly apply state/presentation policy. Resource allocation/retirement, generation guards, reentry and callback atomicity are not operations of this pure codec; those belong to the state adapter. No unsupported coverage percentage follows from adding executable code.

## Verification and reproduction

[Native scenario](../poc/Stardust.Core.SmokeTests/CreatureView3ExtendedDeltaScenarios.cs) exposes Run(peerPath,exportPath). [Runner](../poc/round24-tests/Networking/Networking.csproj) and [independent literal Python producer/oracle](../poc/round24-tests/Networking/peer.py) use synthetic bytes only, with no production imports or original/live inputs.108 exact complete wires,18,042 negative probes and11,051 ownership/cap/sibling checks pass. Python independently handpacks/reparses exported complete messages and ordered indices; native checks common raw integer and float bits against literal field bytes.

Fixtures cover all seven common fields alone and interspersed with the existing union, both orders/repeats, both wounds policies, counter wrap, signed key/value extremes, empty containers/assignments, opaque short/extended strings, raw count mismatch, finite min/max/subnormal/signed-zero complexity, visibility0/1/128/255 and4096 common fields. Exact payload/field/decoded/kind/command/key/value budgets pass; one-less caps reject. Negative probes cover late names1/2/unknown, unsupported tags/views/kind, nonfinite complexity/Height, compound opcodes/value extents,4097 component commands/total fields, raw and recomputed-envelope truncation, invalid limits/policy and empty messages. Complete field prefixes remain positive cases. Small fixture prefixes are exhaustive; large cases use first64/final16 prefixes.

The stale assignment regression intersperses common and raw fields around a three-value legacy assignment, raw count4, value cap3, element cap1. Counter5 over initial counter5/empty skips unchanged; counter9 parses and round-trips then sibling Apply rejects growth. No eager parse-time element-cap rejection was reintroduced.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r24'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round24 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round24/NuGet.Config
& $python poc/round24-tests/Networking/peer.py --generate local-private/round24/peer.json
dotnet restore poc/round24-tests/Networking/Networking.csproj --configfile local-private/round24/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/round24-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round24-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round24/peer.json local-private/round24/export.json
& $python poc/round24-tests/Networking/peer.py --peer local-private/round24/peer.json --verify local-private/round24/export.json
```

Round23 regression65/12119/1802 with independent oracle, Round22 regression58/4292/6887 and shared default suite pass. Builds have zero warnings/errors. Frozen APIs, accepted siblings and shared Program remain unchanged. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_EXTENDED_DELTA_CODEC.md) records scope/input identity. Next lead-owned action: register the scenario and join the extended wire decoder to the current-world transaction, preserving common overlays through older ingress paths; then validate actual visibility presentation. Names1/2, localization/fallback and TANO remain separate gaps.
