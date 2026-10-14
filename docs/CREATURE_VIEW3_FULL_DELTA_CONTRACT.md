# Ordered full CREO:3 delta contract

Round23 Networking, October3,2026. The codec (external historical reference) consumes the read-only frozen full plan (external historical reference). It joins field4, component5, scalar11..16 and wounds17 while preserving global order and duplicates. Customization bytes remain opaque. This is an offline application-message codec; it performs no state application, packed interpretation, callbacks, engine/resource operations or networking.

## API and limits

All types are in Stardust.Core.Protocol:

```csharp
public sealed record CreatureView3FullDeltaLimits(
    int MaximumPayloadBytes, int MaximumFields,
    int MaximumScalarUpdates, int MaximumRawUpdates,
    int MaximumStringBytes, long MaximumDecodedBytes,
    int MaximumComponentCommands, int MaximumComponentKeys,
    int MaximumComponentElements, int MaximumWoundsCommands,
    int MaximumWoundsValues, int MaximumWoundsElements);
```

CreatureView3FullDeltaCodec exposes Decode(ReadOnlySpan<byte>, limits, woundsPolicy=StrictInterval), Decode(ReplicationEnvelope, limits, woundsPolicy=StrictInterval), Encode(plan, limits) and EncodeEnvelope(plan, limits). RawTag is4352454F; View is3. Encode returns the complete existing Delta APPLICATION envelope. DeclaredCount U16 is raw metadata; parsing continues through field-span exhaustion. At least one actual update is required. Unsupported fields, baseline envelopes, wrong tag/view and nonfinite Height reject.

Payload budget includes the raw count, indices, compound headers, prefixes and values; its accepted range is2..int.MaxValue-23. Total/scalar/raw counts are independently0..4096. String byte cap is a nonnegative int. Aggregate decoded byte cap is a nonnegative long and charges raw string content, scalar widths1/4/8, signed component keys4 and wounds values4. It excludes indices, length/count/counter prefixes and command opcodes. Independent payload and count limits bound those bytes and managed metadata. Component commands/keys/elements and wounds commands/values/elements are independently0..65536. The frozen plan adds4096 actual commands per compound field and65536 aggregate component plus raw wounds commands and aggregate wounds values.

Element caps are explicit plan metadata for later application. Assignment vectors are bounded by MaximumWoundsValues, independent of the state-element cap. Assignments, indexed wounds commands and component membership require initial state and skip/counter policy to assess legality/growth; that remains the accepted state kernel's responsibility. Encode rejects plan element metadata exceeding supplied caps. Policy and element metadata are explicit inputs, not wire fields; decoding with different metadata can produce a different plan from identical bytes.

## Grammar and preservation

The established [raw/scalar contract](CREATURE_VIEW3_CUSTOMIZATION_SCALAR_DELTA_CONTRACT.md) is retained: field4 uses U16LE length orFFFF/U32LE length and exact short/extended spelling;11 posture U8;12 rank U8;13 link U64LE;14 finite Height F32LE;15 shock U32LE;16 states U64LE. Finite negative, signed-zero, subnormal and extreme Height values preserve their bits. Arbitrary raw bytes, including invalid inner packed escape bytes, are never projected.

Component5 uses U32LE command count and counter, then opcodes0 drop/1 add with signed S32LE key, or2 clear without a key. Counter wrap and signed keys remain unchanged. Wounds17 uses raw U32LE loop bound and counter:0 remove U16 index;1 insert/2 replace U16 index and S32 value;3 assign U16 value count then S32 values;4 clear. No count, counter or command normalization occurs.

StrictInterval consumes one loop unit per wounds command. LegacyBounded assignment consumes valueCount+1 units, following the accepted sibling parser. A final assignment may advance beyond the raw bound. Residual bytes belong to subsequent outer fields; they are not silently absorbed into wounds or discarded. Encode checks that every command starts below the raw bound and the terminal loop cursor reaches or exceeds it, using the selected policy. An incomplete or overpopulated sequence rejects before output allocation. Strict encoding therefore requires raw count equal to actual command count; legacy encoding preserves its independent raw bound. Raw field/count mismatch and repeated mixed fields remain valid.

## Evidence, ownership and atomicity

Observation: the implementation scans every field boundary, finite Height, command discriminator, remaining extent and independent aggregate limit before constructing nested owned values. The scan allocates only bounded range metadata. It then dispatches each complete slice to the accepted raw/scalar/components/wounds codec and frozen owned union factory. Encode preflights complete wire extent, loop shape and all aggregate caps before allocating its output or temporary sibling buffers. Temporary encodings reuse accepted sibling grammars. Earlier local values never escape when a later field fails; input arrays, copied byte strings and encoded output do not alias the issued plan. Confidence: Confirmed by executed tests for this bounded implementation; grammar derives from accepted sibling evidence, with no new executable claims.

Interpretation/consequence: the ordered plan can be passed intact to a later atomic current-world transaction. Initial membership, skipped-command state effects, wounds counter intervals, callbacks and rollback remain outside the codec. Resource generation, stale completion, retirement and reentry tests are not applicable to this pure parser; they remain required at a future adapter. Original current player/customization semantics remain Unknown. Research scores89%/94%,2602 canonical names and345 packet rows do not change.

## Reproduction and verification

Native scenarios (external historical reference) expose Run(peerPath,exportPath). The runner (external historical reference) and independent Python oracle (external historical reference) require no original or live inputs.65 exact complete wires,12,119 negative probes and1,802 ownership/cap/sibling checks pass. Python independently handpacks and reparses every exported wire and ordered index sequence. Cases include both orders, repetitions, every scalar, embedded/high raw bytes, empty short/extended strings, count mismatches, signed key/value extremes, counter wrap, empty containers/assignments and strict versus legacy assignments including raw-bound overshoot. Negatives include envelope and recomputed-envelope field truncation, late unknown fields/commands, nonfinite Height, all wire/value budget categories, invalid limits/policy and policy-inconsistent raw loops. Complete field prefixes remain positive fixtures. A three-value LegacyBounded assignment with raw count4 parses and round-trips under MaximumWoundsValues3/MaximumWoundsElements1: counter5 over initial counter5/empty skips unchanged; counter9 parses then rejects growth during sibling Apply. Raw fields before and after survive both cases.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r23'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round23 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round23/NuGet.Config
& $python poc/round23-tests/Networking/peer.py --generate local-private/round23/peer.json
dotnet restore poc/round23-tests/Networking/Networking.csproj --configfile local-private/round23/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/round23-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round23-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round23/peer.json local-private/round23/export.json
& $python poc/round23-tests/Networking/peer.py --peer local-private/round23/peer.json --verify local-private/round23/export.json
```

Round22 mixed regression58/4292/6887 passes. Shared build has zero warnings/errors and default suite passes, including strict wounds, legacy policy and components kernels. Existing codecs, frozen DTO and shared Program are unchanged. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R23_FULL_DELTA_CODEC.md). Next lead-owned step: register the new scenario and bind one ordered full plan to the current-target preflight/commit adapter, explicitly selecting wounds policy and initial-state limits.
