# TANO view3 owned baseline and delta codecs

Round26 Networking, October3,2026. [TangibleView3BaselineCodec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/TangibleView3BaselineCodec.cs) is an independent eleven-field TANO:3 reader/writer. [TangibleView3DeltaCodec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/TangibleView3DeltaCodec.cs) accepts ordered indices0..10 only. Both use frozen [TangibleView3Models](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/TangibleView3Models.cs). This is a standalone offline codec stage; typed TANO world binding, atomic transactions, display and component state application remain separate.

## Evidence and schema boundary

The established [replication schema](REPLICATION_SCHEMA_MAP.md), [corrected tangible checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_TANGIBLE_VIEW3_REPLICATION.md) and [component-width correction](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_TANGIBLE_COMPONENT_WIDTH_CORRECTION.md) define eleven ordered fields and S32 component keys. Prior [baseline contract](CREATURE_VIEW3_BASELINE_CONTRACT.md) records generic snapshot traversal through registered fields despite declared count, extended strings and raw UTF16 ownership. This increment uses those durable conclusions; it makes no new executable-address claim.

Server correlation: sibling Stardust/Core3 commit9a8529ef8291efc5ca1b41d9927f0e88d64b79bb, `MMOCoreORB/src/server/zone/packets/tangible/TangibleObjectMessage3.h::TangibleObjectMessage3`, emits fields0..10 with default tag54414E4F/count11. `tangible/TangibleObjectDeltaMessage3.h` methods updateComplexity, updateObjectName, updateCustomName, updateCustomizationString, updateOptionsBitmask, updateCountdownTimer, updateConditionDamage and updateMaxCondition correlate indices0/1/2/4/6/7/8/9. Component producers use the established AutoDeltaSet boundary; that builder has no direct volume/visibility setter. These are server-source observations, not proof of every client callback or reachable delta occurrence. Original set normalization is not inferred from server uniqueness.

| Index | Baseline value, in sequence | Delta value after U16LE index |
| --- | --- | --- |
| 0 | Finite F32LE complexity | Same |
| 1 | File byte string, U32LE spacer, key byte string | Same |
| 2 | U32LE count followed by raw U16LE units | Same |
| 3 | U32LE volume | Same |
| 4 | Raw customization byte string | Same |
| 5 | U32LE count/counter then S32LE raw keys | U32LE command count/counter;0 drop/1 add S32 key;2 clear |
| 6..9 | U32LE options/use count/condition damage/maximum condition | Same |
| 10 | U8 visibility | Same |

Byte strings independently preserve U16LE length orFFFF/U32LE length spelling, including extended empty/small strings. High/NUL bytes, unknown spacer values, unpaired UTF16 units, signed component extremes, raw ordered duplicate snapshot keys, counter bits and raw declared count survive. No string/path/display conversion, set normalization, clamping or visibility boolean conversion occurs. Complexity permits every finite F32, including negative values, signed zero, subnormals and extremes.

RawTag54414E4F and View3 are explicit codec constants. Existing APPLICATION baseline/delta envelopes retain count5, their known opcode, U64 object ID, tag/view, U32 payload size and raw U16 declared count. Baseline always consumes exactly the complete eleven-field grammar and rejects any tail; its declared count is metadata. Delta iterates to field-span exhaustion, preserves global order/repeats and rejects indices11..17/18/FFFF, wrong family/view/kind and incomplete tail. Complete delta field prefixes remain valid shorter messages after correcting envelope size.

## APIs and budgets

Baseline exposes Decode(ReadOnlySpan<byte>,CreatureView3BaselineLimits), Decode(ReplicationEnvelope,limits), Encode(snapshot,limits) and EncodeEnvelope(snapshot,limits). It reuses the baseline limit record without shrinking its string/unit limits to the smaller name-update caps. Payload must fit the managed envelope; string caps are nonnegative int, Unicode cap0..int.MaxValue/2, component cap0..int.MaxValue/4 and decoded cap nonnegative long.

FixedDecodedBytes is47: object ID8 + raw count2 + complexity4 + spacer4 + volume4 + component count/counter8 + four U32 common values16 + visibility1. Add all three string contents, twice Unicode units and four times RAW component entries, including duplicates. Unicode count and wire string prefixes are excluded. This equals accepted CREO81 minus creature scalar26 minus wounds header8. It is logical retained accounting, not CLR/temporary-copy heap measurement. Actual field wire length is independently checked; minimum body47 plus contents/unit/key bytes and4 extra bytes per extended prefix.

Delta exposes Decode(ReadOnlySpan<byte>,TangibleView3DeltaLimits), Decode(ReplicationEnvelope,limits), Encode(plan,limits) and EncodeEnvelope(plan,limits). Its explicit limits have no creature scalar/wounds members:

```csharp
public sealed record TangibleView3DeltaLimits(
    int MaximumPayloadBytes, int MaximumFields,
    int MaximumTangibleUpdates, int MaximumRawUpdates,
    int MaximumStringBytes, long MaximumDecodedBytes,
    int MaximumComponentCommands, int MaximumComponentKeys,
    int MaximumComponentElements, int MaximumNameUpdates,
    int MaximumNameStringBytes, int MaximumUnicodeUnits);
```

Payload/total fields/decoded caps apply to the entire delta. Fields/common/raw/name attempt caps independently accept0..4096; common attempts are0/3/6..10. Raw string cap is a nonnegative int. Name strings/units independently accept0..65536. Component commands/keys/state elements accept0..65536, with frozen4096 commands per field. Frozen named-model aggregate name cap1MiB remains explicit, including name spacers. Delta decoded charge includes common widths1/4, StringId file/key bytes plus spacer4, custom units2, raw customization content and component keys4; envelope/index/prefix/command/count/counter metadata is excluded.

State-element cap is explicit plan metadata for later application, not a parse-time membership/growth test. Baseline raw duplicates remain intact until a consumer deliberately constructs normalized state. Component skip, mutation-dependent counter changes and unsigned comparisons remain the accepted kernel's state concern. TANO has no wounds policy or creature-extension budget to select accidentally.

## Ownership and allocation preflight

Observation: each decode entry validates envelope identity and payload. Baseline performs a complete extent/finite/count/aggregate/tail scan without nested arrays, then reads owned strings/units/components independently. Delta performs a TANO-only full field scan and all aggregate/kind/name/command/key checks before constructing nested values. Only bounded range metadata is allocated during scans; byte-message entry first uses the accepted bounded opaque envelope copy.

The baseline reader/writer never pads TANO bytes into a synthetic CREO baseline and never calls FromCreatureCommonFields. That frozen method is an explicit model projection for separate tests, not wire decoding. Delta reuses accepted common field grammars through isolated checked temporary CREO envelopes AFTER the strict TANO scan; scalar/wounds budgets are fixed at zero internally. Output always carries TANO. This is shared field grammar reuse, not acceptance of a creature extension.

Baseline encode independently recomputes wire length, logical charge, finite value and each cap/invariant before allocation. Delta encode validates the TANO plan and delegates the complete common union to the accepted named encoder's whole-input preflight before buffers, then wraps its owned fields in TANO. Copy-returned bytes, caller arrays/lists and issued outputs do not alias snapshots/plans. Late failures yield no snapshot/plan/output. Invalid limits/null arguments throw argument exceptions; malformed/unsupported/budget input throws InvalidDataException. Confidence: Confirmed for these executed bounded behaviors. Compatibility consequence: later typed TANO stages receive exact raw metadata/attempts without prematurely applying state or presentation policy.

## Verification and reproduction

[Native scenario](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TangibleView3CodecScenarios.cs), [runner](../../SWG-Godot-Client/poc/round26-tests/Networking/Networking.csproj) and [independent Python producer/oracle](../../SWG-Godot-Client/poc/round26-tests/Networking/peer.py) require only synthetic bytes.45 baselines/30 deltas round-trip exactly;3,447,617 negative probes and18,335 ownership/cap/sibling checks pass. Python independently revalidates75 complete wires, all baseline fields/raw component duplicates, ordered delta indices/values and charges, rejects21 malformed cases and74 one-less decoded budgets.

The negative count is dominated by3,433,054 large-frame prefixes and is not a coverage percentage. EVERY fixture frame prefix is checked; corrected-envelope internal-field prefixes are exhaustive for small fixtures and first64/final16 for large fixtures. Other negatives cover trailing baseline bytes, wrong family/view/kind/opcode/size, nonfinite floats, every creature-extension index, malformed/overflow string/unit/component counts, aggregate names, invalid limit arithmetic and all logical cap categories. Exact limits pass and one-less applicable limits reject on both decode/encode.

Positive cases include both delta orders/repeats, raw count mismatch0/1/11/FFFF, all prefix flag combinations, signed-zero/subnormal/finite extremes, high/NUL bytes and unpaired units, raw U32/visibility extremes, duplicate/empty components, name/string/unit lengths65534/65535/65536 and baseline65537, raw delta customization65537,4096 empty name fields and exactly1MiB custom units. Baseline alias tests confirm frozen NameState retains the owned TANO fields even above smaller delta-update caps. Existing common field encodings agree with the accepted named sibling. CREO25 regression136/22958/746008 plus independent oracle and shared default suite pass; builds have zero warnings/errors.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r26'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round26 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round26/NuGet.Config
& $python poc/round26-tests/Networking/peer.py --generate local-private/round26/peer.json
dotnet restore poc/round26-tests/Networking/Networking.csproj --configfile local-private/round26/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/round26-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round26-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round26/peer.json local-private/round26/export.json
& $python poc/round26-tests/Networking/peer.py --peer local-private/round26/peer.json --verify local-private/round26/export.json
```

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R26_TANGIBLE_VIEW3_CODEC.md) records identity/scope. Next lead-owned action: register the scenario, review independent TANO baseline/plan together with standalone typed stages, then design atomic current-world TANO ingress. No original-runtime/live/typed-world coverage, locale/display or generation/reentry/resource behavior is asserted by this pure codec milestone.
