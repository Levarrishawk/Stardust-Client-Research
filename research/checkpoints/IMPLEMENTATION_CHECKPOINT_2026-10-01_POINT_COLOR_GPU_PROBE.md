# Combined point-light vertex-color GPU probe — October 1, 2026

## Question and stopping condition

Does the recovered point-light vertex color stage agree with the owned CPU reference when combined with normal/tangent interpolation, hue and a separate specular mask? Stop at a bounded reproducible discrepancy; do not widen pixel tolerances to close it.

## Inputs and method

Original source identities and the 159-source scene closure are unchanged from [checkpoint 50](IMPLEMENTATION_CHECKPOINT_2026-10-01_RECOVERED_LIGHTING_DEFAULT.md). Baseline executable and graphics DLL hashes were rechecked during checkpoints 40/48. This investigation executes only the independently authored prototype host replacement. The added triangle, lights, textures and material are synthetic; they are not recovered game geometry or protocol.

StaticDoorVaryingGpuScenarios.Verify(owner, true, true) combines two positive-coefficient point lights, one primary directional light, material emissive, hue, separate SPEC alpha, differing tangent bases, two powers, both handedness signs, rigid owners and orthographic/perspective cameras. CPU colors are evaluated per vertex, interpolated with screen barycentrics divided by clip W, then saturated at the pixel boundary. The existing tolerance remains 2/255.

## Initial observation (resolved below)

The earlier 48-case character fixture passes. Combined case 8 fails: identity owner, perspective camera, power 2, positive handedness, sample (28,38). Actual display RGB is (0.5137255, 0.23529412, 0.06666667); expected (0.5176604, 0.24069631, 0.07492795). Maximum channel error is 0.008261278, above 2/255. CPU perspective weights are (0.24228555, 0.45828688, 0.2994275); interpolated diffuse including emissive is (0.8680599, 0.80879194, 0.41991287); specular is (0.17702268, 0.17702268, 0.11801513).

Debug display readbacks for isolated interpolated diffuse/specular are respectively (0.8666667, 0.80784315, 0.41960785) and (0.17254902, 0.17254902, 0.10980392). These include the renderer transfer and quantization, so they must not be compared directly to unconverted CPU colors. They do not establish a raw floating-point GPU identity.

## Initial interpretation and confidence (resolved below)

**Confirmed:** this bounded combined pixel fixture does not meet its acceptance threshold. **Tentative:** the remaining difference is downstream of the per-vertex point calculation, possibly combined shading, tangent precision or renderer transfer. The failure does not establish which production equation is wrong. Original vertex compilation/LIT behavior and hardware color interpolation remain Unknown.

## Compatibility consequence

No production shader equation or tolerance was changed to accommodate this result. The diagnostic is explicitly opt-in: add -PointColorGpuProbe to a noninteractive, nonheadless launcher invocation that already requests CreatureSpecular or SceneCharacterSpecular. Before correcting the fixture it exited with the bounded failure above; the launcher reports its private log. Ordinary fixtures retain the previously accepted path. Headless/interactive/inapplicable probe requests are rejected.

## Validation and next probe

The prototype host build and default engine-neutral smoke suite pass after isolating the diagnostic. Final ordinary scene/cleanup and organized-workspace checks are recorded in [the session report](../sessions/IMPLEMENTATION_SESSION_2026-10-01_UNTIL_2000.md). At that stage this question remained open; the resolution below closes the bounded fixture.

Next: capture isolated primary diffuse, primary specular, tint and mask terms for the same failing pixel, applying the renderer transfer consistently. Compare raw floating-point output if the adapter supports it; separately bound quantized tangent and framebuffer precision. Preserve the failing case before modifying either shader arithmetic or the reference.

## Resolution before the deadline

The synthetic primary light put -light in RowZ, while PortalCellLight.TowardLight calls RuntimeTransform.TransformDirection(UnitZ), which reads the third column (ComponentAppearanceDecoder.cs, AppearanceTransform.TransformDirection). Its GPU direction was therefore different from the supplied CPU tangent direction. The corrected fixture places the three components in RowX.Z/RowY.Z/RowZ.Z. This is a fixture correction; no production shader or acceptance tolerance changed.

The corrected opt-in invocation passes all48 combined cases: maximum display error0.003225, former fragment tangent-policy difference0.023166 and per-fragment point-color recomputation difference0.474411. Full original retained scene gates also pass. Confidence Confirmed for the bounded owned fixture; original hardware precision/compiled LIT remain Unknown. The previous failure observations above are preserved as investigation history and do not describe the corrected fixture.

Final build zero warnings/errors; default native suite, ordinary original GPU capture, headless cancellation and corrected combined GPU preview pass. Organized workspace:1003 Markdown files,0 broken/private links,2602 canonical names with0 duplicate addresses,345 packet rows. No files staged. Next: close selected original vertex-lit mouth VSH source and its color-output staging using static source/bindings and independent fixtures.
