# Exact compact scene transform APPLICATION codec

`CompactSceneTransformCodec` supports only top-level UpdateTransform `0x1B24F808` and UpdateTransformWithParent `0xC867AB5A`. It consumes whole APPLICATION bytes after transport reassembly, preserving raw signed coordinates and bytes. It performs no object lookup, world ingress, motion-store update, parent resolution, counter ordering or interpolation.

## API and exact layouts

All types are in `Stardust.Core.Protocol`:

```csharp
public sealed record CompactSceneTransformMessage(
    ulong ObjectId, ulong? ParentId,
    short RawX, short RawZ, short RawY,
    uint MovementCounter, sbyte SpeedRaw, sbyte DirectionRaw);

CompactSceneTransformMessage CompactSceneTransformCodec.Decode(ReadOnlySpan<byte> message);
byte[] CompactSceneTransformCodec.Encode(CompactSceneTransformMessage value);
// Derived DTO properties:
float CoordinateScale;
System.Numerics.Vector3 NativePosition;
```

Constants: OperandCount=8, UpdateTransformOpcode=`0x1B24F808`, UpdateTransformWithParentOpcode=`0xC867AB5A`, UpdateTransformLength=26 and UpdateTransformWithParentLength=34. ParentId null selects the quarter-unit branch; any present parent, including zero, selects the eighth-unit branch. ID zero is structurally accepted; runtime object validity belongs to callers.

| Field | Plain offset | Parent offset | Representation |
| --- | --- | --- | --- |
| count | 0 | 0 | U16 LE, exactly 8 |
| opcode | 2 | 2 | U32 LE, one of the two exact opcodes |
| parent ID | absent | 6 | U64 LE |
| object ID | 6 | 14 | U64 LE |
| X | 14 | 22 | S16 LE |
| Z | 16 | 24 | S16 LE |
| Y | 18 | 26 | S16 LE |
| movement counter | 20 | 28 | U32 LE |
| speed | 24 | 32 | S8, raw bit pattern retained |
| direction | 25 | 33 | S8, raw bit pattern retained |

Decode checks header, opcode and exact length before reading fields. Truncation, trailing bytes, unknown opcode, altered count and wrong-branch length reject with InvalidDataException. There is no variable count/allocation, negotiated transport trailer or SOE framing in this parser. Encode reconstructs the same bounded message from raw scalar fields, not from floats. Null encode throws ArgumentNullException. DTOs retain no caller buffer; every encode returns its own array.

NativePosition maps wire X/Z/Y to native `(X,Y,Z)` by multiplying raw values by 0.25 or 0.125. All such S16 coordinates are exactly representable in binary32. Plain coordinate range is -8192..8191.75; parent range is -4096..4095.875. Parent coordinates remain parent-local metadata: no world-space composition is attempted.

## Evidence, conversion limits and confidence

Existing [packet map](PACKET_HANDLER_MAP.md) and [controller contract](OBJECT_CONTROLLER_PROTOCOL.md) confirm the compact signed grammar, scales and queued controller boundary. Existing [parent movement checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_PARENT_RELATIVE_MOVEMENT.md) distinguishes these top-level observer packets from full-precision inner0x71/0xF1 messages. No original executable/address probe was performed in this implementation.

Exact local Stardust-3 correlation at repository HEAD `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`:

- `MMOCoreORB/src/server/zone/packets/scene/UpdateTransformMessage.h`, both constructors: count8, opcode, object ID, signed X/Z/Y multiplied by4, movement counter, creature currentSpeed cast to int8 (otherwise0), special direction cast to int8. File SHA-256 `62C6F2C2D8A39C5FF8220D9DED64765F496F0BD74FBC0ED4FC87DBC1D975CACB`.
- `MMOCoreORB/src/server/zone/packets/scene/UpdateTransformWithParentMessage.h`, both constructors: count8, parent then object ID, signed X/Z/Y multiplied by8 and the same counter/byte tail. File SHA-256 `56C8FB8DA4B8D3174214BD3DF6A696B89F1F1A774F08E2E8AFF0BAC00860CB59`.
- `SceneObject.idl::getSpecialDirectionAngle` delegates to quaternion getSpecialDegrees. `MMOCoreORB/utils/engine3/MMOEngine/src/engine/util/u3d/Quaternion.h::getSpecialDegrees` uses getRadians divided by6.283 and multiplied by100; file SHA-256 `6D874BB89F83F82C0ED380BC129FA276AE7781877150297CBEDE2439A60B3636`.

Observation: server construction and existing client durable evidence agree on this raw boundary. Confidence: Confirmed inherited wire structure, with server source as correlation. The server heading helper alone does not establish the exact client's conversion, quaternion layout or interpolation. **No yaw/quaternion or physical speed conversion is included.** Preserve negative/out-of-range direction and speed bytes verbatim; no clamp, reinterpretation as unsigned angle, normalization or displacement is inferred. Lead may explicitly use a modern diagnostic zero-yaw pose outside the codec while retaining raw metadata.

The plain header's coordinate-only static parse helper uses `/4` expressions and different local variable names. It is not a complete application decoder, and this codec does not emulate that helper's arithmetic/cursor assumptions. NativePosition follows the already documented client quarter/eighth-unit contract and constructor order. Float-to-S16 quantization, overflow behavior, helper reachability and original client smoothing remain outside scope; encode starts from raw S16 values so no conversion rule is invented.

## Validation and handoff

Native and independently handpacked Python fixtures match exact complete bytes and DTO values for522 cases: signed coordinate extrema/fractions, endian-sensitive high/zero IDs and counters, parent0, both branch scales and every possible speed/direction byte value in both branches. All19,324 malformed probes reject: every truncated prefix, appended byte, all six count/opcode-byte mutations and two wrong-branch length cases. Caller/output clearing, null encode and parent-zero distinction pass. Tests allocate no external resources or runtime world state.

`CompactSceneTransformScenarios.Run(string peerPath, string exportPath)` is the public smoke entry point. Runner (external historical reference) takes those two paths. the separate implementation record has no production-code imports or original inputs. Generated peer SHA-256 is `2FC85EDF2CE45148096E11F0099463D2D36F6CBA59E04B7521D630F94F83ABCA` (UTF-8/LF).

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r17'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round17 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round17/NuGet.Config
& $python poc/round17-tests/Networking/peer.py --generate local-private/round17/peer.json
dotnet restore poc/round17-tests/Networking/Networking.csproj --configfile local-private/round17/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round17-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round17/peer.json local-private/round17/export.json
& $python poc/round17-tests/Networking/peer.py --peer local-private/round17/peer.json --verify local-private/round17/export.json
```

Detailed exports/logs remain ignored. Lead owns shared registration and routing decoded DTOs to its motion store; baseline world.Accept still rejects these opcodes. Frozen motion-source interface is unchanged. The next bounded probe is separate incarnation/currentness-aware motion routing, with explicit parent and diagnostic orientation policies. Existing345 packet rows remain unchanged because these boundaries were already documented.
