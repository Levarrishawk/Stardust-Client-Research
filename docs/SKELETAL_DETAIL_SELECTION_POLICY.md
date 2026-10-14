# Modern projected-size skeletal detail policy

## Scope and API (October2,2026)

`SkeletalDetailSelectionPolicy` owns three caller-supplied, strictly descending positive projected-diameter thresholds and a hysteresis fraction. `SkeletalDetailSelector` owns serial selection state, exposes `Current`, accepts `SkeletalDetailProjectionSample`, returns `SkeletalDetailSelection` and resets to an uninitialized state. This is a modern presentation policy. No original threshold, automatic legacy detail/skeleton coupling or hardware behavior is inferred. The existing [explicit lower-detail compatibility evidence](SKELETAL_LOWER_DETAIL_CONTRACT.md) remains separate. [Round4 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R4_DETAIL_SELECTION.md) records validation.

Policy construction defensively copies thresholds into a read-only collection. No default threshold values are supplied by production code; `(240,120,60)` pixels and10% are synthetic test choices. The selector holds no asset or renderer resources. Calls on one selector must be serialized by its owner.

## Projection approximation and bounds

For radius `r`, nonnegative camera-space center depth `z`, vertical FOV `f` in degrees and viewport height `H` in pixels:

`diameterPixels = (r / z) * H / tan(f * pi / 360)`

This is the centered-sphere/small-angle diameter approximation using center depth, not an exact perspective silhouette or off-axis frustum calculation. The caller supplies compatible world units for radius/depth and a vertical FOV. Width/aspect ratio, occlusion, clipping and visibility are outside the selector.

When `z <= r`, the eye-plane/near-sphere proxy chooses finest detail0 unconditionally and returns `NearCamera=true`, `ProjectedDiameterPixels=null`. Zero depth is supported without division. For a centered sphere this covers camera-inside cases; center depth alone cannot establish an off-axis camera's actual distance to the sphere. The conservative near rule is explicit modern policy, not a recovered near-plane setting. Negative/behind-camera depth rejects rather than selecting a visible detail.

| Input | Accepted range |
|---|---|
| Radius | finite, `0 < r <= 1e6` |
| Center depth | finite, `0 <= z <= 1e9` |
| Vertical FOV degrees | finite, `1 <= f <= 179` |
| Viewport height | integer, `1 <= H <= 32768` |
| Three descending thresholds | finite, `1e9 >= T0 > T1 > T2 > 0` |
| Hysteresis fraction | finite, `0 <= h <= 0.49` |

A computed outside-sphere projection must also be finite and positive; numeric underflow rejects atomically. All ranges are modern resource/numeric choices. There is no hidden clamp or wall-clock input.

## Equality, initialization, hysteresis and reset

The first outside sample chooses0 when `P >= T0`,1 when `T0 > P >= T1`,2 when `T1 > P >= T2`, otherwise3. Equality chooses the finer side.

From current detail `d`, coarsen across each boundary while `P < Td * (1-h)`. Refine across each boundary while `P >= T(d-1) * (1+h)`. Multiple boundaries may be crossed in one sample. The downward boundary is strict; the upward boundary is inclusive. A sample within the retention band leaves the current detail unchanged. Different prior selections may legitimately retain different details in that band. Repeated identical inputs cannot oscillate: the opposing crossing conditions for one boundary cannot both hold. No dwell time is introduced.

The near-camera rule immediately sets current detail0; subsequent outside samples apply hysteresis from that state. `Reset()` clears `Current` to null, so the next sample uses initialization thresholds. A failed sample leaves the entire prior result unchanged, including pixel estimate/near flag; a failed first sample leaves the selector uninitialized.

## Evidence and compatibility consequence

Native fixed supplied cases cover ascending/descending sweeps,24 alternating near-threshold samples, repeated identical samples, near/inside/just-outside inputs, FOV/radius/depth/viewport extrema, zero/max hysteresis, reset and malformed inputs. Exact arithmetic fixtures verify initial equality, lower hysteresis equality retention, upper equality refinement and just-below-lower coarsening. Invalid policies, input bounds/nonfinite values/projection underflow reject; source-array mutation and attempted collection mutation preserve policy values.

Independent Python uses the projection formula and eligible-crossing sets rather than copying the native transition loops. It agrees on76 ordinary projection/state cases, three near/inside cases, three exact boundary fixtures and three resets, with zero projection difference on this host. A stateless wrong policy differs on25 cases, showing the fixtures exercise retained hysteresis. This is Confirmed implementation agreement for supplied modern fixtures, not research evidence about Stardust.exe.

A later adapter may bind the result to already validated retained detail meshes, preserving explicit skeleton policy. This milestone changes no original parser, asset, renderer or GPU route.

## Self-contained reproduction

Prerequisites: .NET8 SDK, the accepted local package cache containing locked Core dependencies (BCnEncoder.Net2.3.0/CommunityToolkit.HighPerformance8.4.0), and Python3. No private original game input is needed for these synthetic cases.

```powershell
$env:APPDATA = Join-Path $env:TEMP "stardust-assets-round4-appdata"
$env:NUGET_PACKAGES = "C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages"
$env:PYTHONUTF8 = "1"
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round4 | Out-Null
"<configuration><packageSources><clear /></packageSources></configuration>" | Set-Content -Encoding utf8 local-private/round4/NuGet.Config
dotnet restore poc/round4-tests/Assets/Assets.csproj --configfile local-private/round4/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round4-tests/Assets --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round4-tests/Assets/bin/Debug/net8.0/Assets.dll > local-private/round4/native.txt
python tools/python/verify_skeletal_detail_selection.py local-private/round4/native.txt
```

Core API (external historical reference), native scenarios (external historical reference), isolated console (external historical reference), [independent oracle](../tools/python/verify_skeletal_detail_selection.py).
