# Modern normals from stored coordinate-height patches

[TerrainModernHeightNormals](../poc/Stardust.Core/Geometry/TerrainModernHeightNormals.cs) derives a complete owned row-major normal field from an actual frozen `TerrainCoordinateHeightPatch`. Policy `TerrainModernNormalPolicy.ModernBinary64CentralDifference` is mandatory; `OriginalNormalParity=false`. This is an explicit modern rendering input, not recovered original normal generation or arithmetic-control parity. The actual immutable source receipt remains available as `Source`.

`Build(source, policy, limits, cancellation)` reads the source's **stored position** X/Z axes and `CopyPositionHeightBits()`. It does not use layer axes, `CopyInputHeightBits()`, unrounded prior heights or the source's supplied normals. For each column/row, select adjacent indices on both sides when interior; at the outer full-grid boundary, use the point and its sole neighbor. Compute gx=(h[right]-h[left])/(x[right]-x[left]) and gz=(h[next]-h[previous])/(z[next]-z[previous]) in binary64. This centered secant is the defined stencil for nonuniform axes; no alternate quadratic interpolation is inferred. Normalize (-gx,1,-gz) with binary64 sqrt(gx*gx+1+gz*gz), then store each component as finite F32.

The entire padded grid participates. Active patch edges use their real padding neighbors when available; only the outer complete-grid boundary uses a one-sided stencil. The field preserves source row-major indices and does not crop, average or regenerate coordinates. `Normals` is read-only; copying normals, stored heights and position axes cannot mutate the receipt. Signed zero follows host operations/F32 conversion. Exact double normals point upward; all executed valid fixtures also retain positive F32 Y and unit length within declared tolerance.

Axes must have width2..32 and cells at most1024. Authentic source construction imposes its own active/padding and surface eligibility first. Stored axes/heights and every derived span/derivative/length/component must be finite; spans must be strictly positive. The builder validates borrowed position axes, owns exported axes/heights, revalidates the copied channels, then allocates normal output. It retains no engine resource, callback or thread-affine object.

| Logical reservation |Formula for width W and cells C=W squared|
|---|---:|
| Copied source bytes |8W+4C|
| Arrays/receipt allowance |256+8W+16C|
| Work units |128+4W+80C|

All policy/limit/shape/count/copy/array/work caps precede source exports or arrays. Defaults are32 axes/1024 cells/65536 copy bytes/131072 array bytes/262144 work units; hard maxima32/1024/1MiB/2MiB/4Mi units. These are declared logical policy charges, not heap/CPU measurements or original resource sizes. Caller-held source storage and later requested exports are outside the per-call charge. Cancellation is checked before exports, during validation and each sample, and before atomic publication. A failure returns no field; prior fields remain intact.

## Optional adapter policy

The only existing adapter change is [TerrainPatchVisual](../poc/Stardust.Godot/TerrainPatchVisual.cs). `TerrainPatchRenderPolicy` has `DiagnosticUvColor` and `ModernLitVertexColor`. The original two-argument constructor delegates to `DiagnosticUvColor`, preserving its exact unshaded UV/COLOR shader string. A three-argument overload opts into `ModernLitVertexColor`; actual policy is exposed as `Policy`. Unknown enums reject before even the Godot thread query or any root/GPU allocation.

The lit shader uses supplied mesh normals through the existing normal array and COLOR.rgb as albedo, with opaque default spatial shading, roughness1 and metallic0. It does not assign ALPHA or claim material reconstruction. Existing Z reflection, normal normalization, source winding/index order, UV/COLOR arrays, semantic readback, ownership counters/three resource owners, exception cleanup and disposal remain intact. It still validates coherence with supplied normals. Consumers must explicitly assemble geometry with the derived full-grid normals; creating the field does not mutate the source's older supplied-normal geometries.

No lights exist in the frozen scene-pumped preview. Worker validation compiles Core/Godot and tests normal math independently; it does **not** execute a lit GPU scene or claim shader-runtime/readback/default-GPU regression verification. Lead's new join supplies a bounded light and validates lit/old diagnostic behavior. No existing host/scene/project/entrypoint is changed by this milestone.

## Evidence and reproducibility

Observation, **Confirmed implementation:** actual source identity, copied stored channels, explicit stencil/normalization/F32 store, full-grid ownership/preflight and enum admission are directly inspectable. Interpretation: a bounded modern derived normal field suitable for a separately validated renderer. Consequence: preserve modern policy and false original parity; source coordinate controls remain explicit conditional profiles, not recovered active original controls.

The [runner](../poc/round59-tests/Rendering/Invoke-Normals.ps1) SHA-gates the baseline executable before model imports, builds actual Core and Godot with installed offline caches and runs [native scenarios](../poc/Stardust.Core.SmokeTests/TerrainModernHeightNormalsScenarios.cs). The [independent oracle](../poc/round59-tests/Rendering/verify_normals.py) uses frozen Fraction coordinate/store/source-patch eligibility and100-digit Decimal normal reference. Declared absolute tolerance is3e-6 per F32 vector component and3e-6 for squared unit length. Expected values never enter the Core normal builder. Plane/wave/flat grids, all81 coordinate-profile combinations, all16 seam masks, stored signed-zero behavior, nonuniform axes, differing layer/position axes, largest field, extreme heights/small spacing, caps/rejections/ownership/repeat/threads/cancellation and adjacent padded seam agreement are covered.

Actual source receipts cannot publicly contain nonfinite stored channels: malformed inputs execute the frozen source builder's rejection; the new copied-channel defensive guards are additionally source-reviewed. Later cancellation checkpoints are source-reviewed, with pre-cancel rejection executed. No original data/corpus or new executable-body investigation is needed for this synthetic numerical milestone. Final counts/scope/build/guard evidence is recorded in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R59_MODERN_HEIGHT_NORMALS.md).
