# Decoded scene-create placement

R6-ASSETS-01, October 2, 2026. SceneCreatePlacementBuilder (external historical reference) derives an engine-neutral presentation placement from `SceneCreateObjectByCrcMessage`, using the existing `AppearanceTransform` rows and column-vector convention. Quaternion normalization and zero handling here are **explicit modern presentation policy**, not recovered original client repair behavior.

## Evidence boundary

The accepted [lifecycle codec](SCENE_OBJECT_LIFECYCLE_CODEC_CONTRACT.md) preserves U64 object identity, U32 template CRC, quaternion X/Y/Z/W, position wire order X/Z/Y, raw hyperspace byte, and finite binary32 bit patterns. It accepts zero and nonunit quaternions without repairing them. The documented server overload that emits a zero quaternion is server-only evidence; it does not establish original client behavior.

The accepted [component transform contract](COMPONENT_APPEARANCE_CONTRACT.md) establishes row-stored 3x4 transforms acting on column-vector points and `parent.Compose(local)` applying local first. Its independent original corpus verification covers 165 templates / 779 parts / 9,348 transform floats. This milestone reuses that representation without reopening original asset parsing. The [coordinate adapter investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_PROTOTYPE_COORDINATE_ADAPTER.md) and existing adapter source (external historical reference) establish the chosen Z-reflection convention. No new executable-address, original-dynamic or GPU evidence is claimed.

## API and policy

`SceneCreatePlacementBuilder.Create(raw, zeroPolicy)` requires an explicit `SceneCreateZeroQuaternionPolicy.Reject` or `.Identity`; there is no default argument. Null input, unsupported enum values, or any nonfinite quaternion/position component reject. Failure publishes no result and mutates no input, prior placement or global state. No resolver, archive, token, resource registry or engine type is involved.

Every nonzero finite quaternion is normalized using double intermediates. Let `m=max(abs(x),abs(y),abs(z),abs(w))`, `a=q/m`, `d=sqrt(dot(a,a))`. Input norm is `m*d` in double, and normalized values are `a/d`. This avoids binary32 norm overflow/underflow for finite extrema and subnormals. There is no near-zero epsilon cutoff: only four exact zero components (including signed zeros) trigger the explicit zero policy. Reject throws; Identity derives `(0,0,0,1)` and records the fallback.

The result is a sealed immutable record containing a distinct raw-create scalar snapshot, `AppearanceTransform`, a binary32 normalized quaternion, double `InputQuaternionNorm`, `AppliedNormalization`, `UsedZeroIdentity` and the selected zero policy. Raw scalar bits—including negative zero, subnormals, nonunit values, object ID/CRC/flag—remain unchanged. The normalized quaternion is rounded to binary32 for metadata; rows are calculated from the double normalized values and rounded independently once. Exact unit length after binary32 rounding is not promised.

`AppliedNormalization` is true for nonzero input whose computed double norm differs from exactly 1; all nonzero inputs nevertheless pass through the normalization calculation. It is a diagnostic about this computation, not proof that the original mathematical input was exactly unit length. Zero Identity reports input norm 0, fallback true and AppliedNormalization false.

## Rows, coordinates and composition

For normalized `(x,y,z,w)`, the standard right-handed rotation rows are:

```text
[1-2(y²+z²), 2(xy-zw),   2(xz+yw)]
[2(xy+zw),   1-2(x²+z²), 2(yz-xw)]
[2(xz-yw),   2(yz+xw),   1-2(x²+y²)]
```

Translation is `(raw.PositionX, raw.PositionY, raw.PositionZ)`: the DTO names restore Cartesian X/Y/Z from serialized X/Z/Y. A source point maps to `R*v+p`; positive 90° rotations send X-axis rotation Y→Z, Y-axis rotation Z→X, and Z-axis rotation X→Y. Quaternion `q` and `-q` give the same rotation.

`parent.Compose(local)` means `R=Rparent*Rlocal`, `p=Rparent*plocal+pparent`. Apply the local point first, then the parent. The decoded create does not identify a container or root-motion owner; callers must select a parent explicitly. The builder neither composes a parent nor infers that translation is a world position.

For the existing engine reflection `S=diag(1,1,-1)`, engine position/direction is `S*v`, basis is `S*R*S`, and translation is `S*p`. Therefore `Rengine*(S*v)+pengine = S*(R*v+p)`, and reflecting the composed transform equals composing reflected parent/local transforms. Consumers must pass columns when an engine basis constructor expects columns, despite Core storing rows. This milestone documents/tests this algebra in native fixtures; it adds no prototype host adapter code or engine invocation.

Finite input produces finite rows, translation, norm and normalized metadata at this boundary. Unchanged `AppearanceTransform.TransformPoint` and `Compose` use binary32 operations, so later arbitrary point/parent operations can overflow even from finite inputs. Their callers must validate resulting transforms/points as appropriate; this builder does not clamp position, guarantee future composition safety or hide overflow.

## Validation and confidence

Native scenarios (external historical reference) cover 27 synthetic decoded creates, 105 basis/asymmetric point probes, 26 asymmetric parent compositions and 25 rejections. Cases include axes at 90/180/270 degrees, asymmetric `q/-q`, nonunit values, smallest subnormal and minimum-normal components, maximum finite components, highly unbalanced values, signed zeros, explicit zero identity, extreme/subnormal positions, invalid policy/null and each of seven fields with NaN/±infinity. Clearing encoded source buffers leaves owned raw scalar snapshots unchanged; rejected calls leave previously returned values usable and repeated derivation equal.

[Independent Python oracle](../tools/python/verify_scene_create_placement.py) normalizes using `math.hypot` and obtains basis columns through vector cross-product quaternion rotation, separate from the native coefficient formula. It checks exact raw float bits, metadata, rows, directions, points, parent composition and reflected basis/points. Native binary32 shortest JSON decimals are rounded back to binary32 before numeric comparison; negative-zero integer-shaped JSON is parsed explicitly. Basis tolerance is 2e-7, quaternion tolerance 1e-7, point/composition tolerance is 3e-6 times max(1, expected magnitude); no tolerance was loosened.

Observed maximum errors: basis `2.9225356801276803e-8`, point `5.651403363060581e-7`, composition `3.0472874641418457e-6`, norm relative `1.1102230246251565e-16`. Fixed wrong policies differ in 48 transpose probes, 97 wire-XYZ probes, 52 one-sided-reflection probes and 24 reversed compositions. These are **Confirmed modern implementation/fixture observations**. They do not identify original runtime quaternion normalization, zero recovery, world/container placement, root motion or complete engine precision.

## Reproduction and handoff

From the isolated repository root, .NET 8 and accepted private package cache:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round6-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
New-Item -ItemType Directory -Force local-private/round6 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -Encoding utf8 local-private/round6/NuGet.Config
dotnet restore poc/round6-tests/Assets --configfile local-private/round6/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round6-tests/Assets --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round6-tests/Assets/bin/Debug/net8.0/Assets.dll > local-private/round6/native.txt
python tools/python/verify_scene_create_placement.py local-private/round6/native.txt
python tools/python/validate_organized_workspace.py .
```

Use the configured Python executable when `python` is absent from PATH. The own console project (external historical reference) calls only this scenario. All packet bytes are synthetic and there are no original asset/toolchain/GPU/network inputs. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R6_CREATE_PLACEMENT.md) records baseline, ownership and final gates.

Lead review/integration is next. Shared runner registration and living document reconciliation belong to the lead. A later consumer may bind the owned [creature resources](SCENE_CREATURE_RESOURCE_CONTRACT.md) to this derived placement with an explicitly chosen zero policy and parent. That integration and original zero/normalization semantics remain open; do not begin another round automatically.
