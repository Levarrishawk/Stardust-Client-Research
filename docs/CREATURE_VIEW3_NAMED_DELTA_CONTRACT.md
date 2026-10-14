# Ordered named CREO:3 delta contract

Round25 Networking, October3,2026. [CreatureView3NamedDeltaCodec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView3NamedDeltaCodec.cs) joins names1/2 with the accepted [extended union](CREATURE_VIEW3_EXTENDED_DELTA_CONTRACT.md), supporting ordered indices0..17 for CREO view3. It consumes the frozen [named plan](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView3NamedDeltaPlan.cs) and [name update/state](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView3NameState.cs). Other families/views, name display, precedence, path validation, locale and table lookup remain outside this codec.

## API and allocation bounds

All types are in Stardust.Core.Protocol:

```csharp
public sealed record CreatureView3NamedDeltaLimits(
    CreatureView3ExtendedDeltaLimits Extended,
    int MaximumNameUpdates, int MaximumNameStringBytes,
    int MaximumUnicodeUnits);
```

CreatureView3NamedDeltaCodec exposes Decode(ReadOnlySpan<byte>,limits,woundsPolicy=StrictInterval), Decode(ReplicationEnvelope,limits,woundsPolicy=StrictInterval), Encode(plan,limits) returning APPLICATION bytes and EncodeEnvelope(plan,limits) returning an owned envelope. RawTag4352454F and View3 reuse the extended codec. ObjectId, raw DeclaredCount, global ordered Updates, WoundsPolicy and both state-element caps remain explicit plan properties.

Extended.Full.MaximumFields caps ALL fields, including names, at0..4096. Its payload cap includes raw declared count and every index/prefix/header/value. Its aggregate decoded cap includes ALL common/full/name values: name file/key bytes plus spacer4, custom units2 each, common widths1/4, creature scalar widths1/4/8, raw customization content, component keys4 and wounds values4. Unicode count, name length prefixes, field indices and compound count/counter/opcode bytes are excluded. No charge resets between fields or kinds. This logical charge does not measure CLR or temporary-copy overhead; independent payload/field/command bounds constrain allocations.

MaximumNameUpdates independently accepts0..4096. MaximumNameStringBytes and MaximumUnicodeUnits independently accept0..65536; the string cap applies separately to file and key. They are distinct from Extended.Full.MaximumStringBytes, which still governs raw customization. Zero name caps permit old fields; name-only plans can use zero common/full kind caps. Empty StringId costs4 decoded bytes for its spacer; empty custom name costs0, but each consumes a name/total field.

The frozen NameUpdate hardcaps new strings/units at65536 and NamedPlan hardcaps aggregate name DecodedBytes at1048576, including StringId spacers. Codec preflight checks these before nested allocations; exactly1MiB is accepted and nine65536-unit vectors reject even under larger caller payload/decoded caps. These are explicit modern update policies. Existing baseline decoder limits are unchanged. Frozen NameState.FromBaseline shares its already-owned immutable fields and does not route baseline values through the smaller update factories; this is an observed API boundary, not a new large-baseline execution claim.

## Exact names and ordered preservation

The existing Delta APPLICATION envelope, byte order and field-span termination remain unchanged. Raw declared count is preserved without imposing it as a loop bound. At least one actual update is required. Existing fields0/3..17 retain their [accepted grammar and policy](CREATURE_VIEW3_EXTENDED_DELTA_CONTRACT.md); each is delegated per field to ExtendedCodec after whole-input preflight.

| Field | Bytes after U16LE index | Preservation |
| --- | --- | --- |
| 1 StringId | File byte string; U32LE spacer; key byte string | Each string independently uses U16LE length orFFFF/U32LE length; exact spelling, raw bytes and spacer retained |
| 2 custom name | U32LE unit count; exactly count U16LE units | Raw units retained, including NUL,FFFF and paired/unpaired surrogate units |

There is no alignment, NUL termination, decoding to a CLR string, surrogate repair, ASCII replacement, path normalization or implicit spacer interpretation at this boundary. Short and extended empty/small file/key strings remain distinct encodings. Length65534 may be short;65535/65536 use the extended spelling. Unicode count represents raw units, not code points or bytes. Complete field prefixes can form valid shorter messages once envelope length is corrected; residual bytes are subsequent fields and cannot be silently discarded.

Global order, duplicate fields, finite float bits, raw string spelling, counters and raw wounds loop bounds remain exact. StrictInterval uses one wounds loop unit per command; LegacyBounded assignment uses valueCount+1 and may finish beyond the bound. Encoder checks raw-loop consistency under the selected plan policy. State-element caps remain metadata for application: assignments parse under value budgets even if their values would exceed current state caps when executed. The stale/fresh assignment regression from23/24 remains valid with names interspersed.

## Evidence, ownership and failure

Observation: byte-message Decode first uses the accepted bounded opaque envelope copy, then scans every field's extent, known discriminator, finite float and global/kind/name budgets. Only bounded range metadata is allocated during this scan. After all input passes, name byte strings use owned ReplicationByteString factories, custom units use frozen owned update factories, and existing fields use accepted nested codecs. Encode computes full extent, all charges/counts and selected wounds loop shape before allocating output or temporary sibling buffers. Caller wire/list/unit arrays, copy-returned strings and encoded outputs cannot mutate issued plans.

A late truncation, unsupported index/tag/view/kind, nonfinite float or budget failure yields no plan/output. Invalid/null configuration or policy arguments throw argument exceptions; malformed input and content/budget violations throw InvalidDataException. Confidence: Confirmed for executed bounded codec behavior. Interpretation/consequence: a later transaction can preserve all raw name attempts and choose explicit state/display policy. There are no callbacks, state commits, rendering resources, generations or reentry operations here; their tests belong to that later adapter. No new executable address, original player/locale behavior or research coverage claim follows.

## Verification and reproduction

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CreatureView3NamedDeltaScenarios.cs) expose Run(peerPath,exportPath). [Runner](../../SWG-Godot-Client/poc/round25-tests/Networking/Networking.csproj) and [literal independent Python producer/oracle](../../SWG-Godot-Client/poc/round25-tests/Networking/peer.py) use synthetic bytes only, without production imports or original/live inputs.136 complete wires,22,958 negative probes and746,008 ownership/cap/sibling checks pass. The assertion count includes exhaustive per-unit comparisons in large Unicode fixtures; it is not a coverage percentage. Python independently verifies every complete exported wire, ordered indices and logical decoded costs, rejects15 malformed/aggregate cases and127 one-less decoded-budget cases.

Fixtures cover all18 fields together and repeated/both orders/policies; separate file/key prefix choices; high/NUL bytes, unpaired units and raw spacers0/1/80000000/FFFFFFFF; raw count mismatch; finite float extremes;65534/65535/65536 string/unit lengths;4096 empty custom-name fields; exactly1MiB name payload; counter wrap; empty compounds and stale/fresh assignment growth. Negatives cover late18/FFFF, malformed file/key extended lengths or unit counts, odd/truncated/trailing bytes, wrong envelope identity/kind, nonfinite complexity/Height, compound discriminators/extents, inconsistent selected raw loops and every logical budget category. Small prefixes are exhaustive; large fixtures use first64/final16 prefixes. Native tests independently compare name bytes/prefixes/spacers/units to literal field bytes, exact aggregate caps and one-less caps, and immutable unit/list/input/output ownership.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r25'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round25 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round25/NuGet.Config
& $python poc/round25-tests/Networking/peer.py --generate local-private/round25/peer.json
dotnet restore poc/round25-tests/Networking/Networking.csproj --configfile local-private/round25/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/round25-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round25-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round25/peer.json local-private/round25/export.json
& $python poc/round25-tests/Networking/peer.py --peer local-private/round25/peer.json --verify local-private/round25/export.json
```

Prior24 passes108/18042/11051 with independent oracle, prior23 passes65/12119/1802 with independent oracle, prior22 passes58/4292/6887, and the shared default suite passes. Builds have zero warnings/errors. Siblings, frozen APIs and shared Program remain unchanged. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R25_NAMED_DELTA_CODEC.md) records input identity/scope. Next lead-owned action: register the scenario, inject NamedCodec into the named current-world transaction and verify raw names survive old ingress and the separate name resolver/presentation boundary.
