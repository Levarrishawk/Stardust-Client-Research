# Explicit cell-lighting preview policy

This engine-neutral policy consumes [original cell-light records](PORTAL_CELL_LIGHT_CONTRACT.md). It is a bounded replacement lighting policy; original selection limits, prioritization, global environment contributions and vertex LIT behavior remain unresolved.

## Selection and ownership

`CellLightingPreview` copies at most eight logical records from one actor owner cell. The first authored directional record becomes the normal-mapped pixel light. All other directional and point records become ordered vertex inputs. Ambient records contribute their diffuse RGB only. The caller supplies explicit nonnegative ambient; the current fixture uses0.1 per channel. No automatic defaults or intensity ranking are recovered.

A rigid, right-handed owner pose transforms point positions and directional vectors. Directional vectors are normalized negative runtime K. Logical diffuse/specular channels preserve the source colors without requiring the legacy separate secondary objects. HDR is retained; alpha does not participate. Negative colors/point coefficients are rejected by this preview policy although the raw decoder preserves them.

## Vertex arithmetic

For point distance r, attenuation is `1/max(0.01,a0+a1*r+a2*r*r)`. Directional attenuation is1. A coincident point has zero direction. A vector is normalized with denominator `max(length,1e-6)`; zero view/half vectors remain zero.

Diffuse accumulates `ambient + sum(diffuseRGB * max(dot(N,L),0) * attenuation)`. Specular accumulates `specularRGB * materialSpecularRGB * pow(clamp(dot(N,normalize(L+V)),0,1),power) * attenuation` only when N.L is positive. Literal power0..1024 is explicit; this does not establish the original vertex instruction exponent behavior. Inputs/outputs must be finite and bounded. No pixel diffuse saturation, specular mask, bloom or display conversion occurs in this evaluator: those belong to the recovered material boundary.

Normal-mapped rendering excludes the primary from vertex sums. Vertex-lit rendering includes it. Unknown original range/selection is not approximated silently; this preview selects every record within its stated budget and imposes its stated near-light cap.

## Verification

Synthetic fixtures independently calculate a distance2 inverse-square point, primary inclusion, back-facing gates and coincident view/light positions. Owned selection, invalid directions, coefficients, powers and budgets are checked.

The existing original three-room POB supplies19 records.288 fixture cases cover both rigid owner poses, four positions, three normals, two material powers and primary inclusion/exclusion. `verify_cell_lighting_preview.py` independently computes double-precision sums after the original record audit. Maximum error is3.14701e-7 against tolerance0.002. This is policy arithmetic agreement, not original rendered-frame equivalence.

## Retained GPU integration

`InteriorSceneContentBundleReader.Load(...loadCellLights:true)` decodes only selected room records on the CPU worker and publishes immutable cell-indexed collections. It requires selected rooms and the owned character specular resource boundary. No extra archive reads are added: LGHT is already in the pinned POB.

`-SceneCellLighting` enables the opt-in preview. Actor owner-cell or rigid building-owner changes publish reflected light arrays into existing ShaderMaterials. Camera ownership/visibility alone does not republish lights. Eight packed input slots, diffuse/specular/attenuation arrays and active count are checked exactly after upload. Static room/door materials remain their documented MAIN inspection variants at this checkpoint.

Normal-mapped materials compute remaining-light diffuse/specular at vertices and preserve recovered primary pixel-light equations, mask, bloom and alpha handling. Mouth includes primary in the vertex sum and adds retained material emissive. Literal power is explicit; power0 returns1 under the positive N.L gate. Owner-cell lighting does not change textures, palette/shape revisions, pose or collision authority.

GPU checks cover32 arithmetic cases, both reflected owner poses, point cap/back-facing/specular behavior and all eight slots; max display error0.002961. Twelve additional normal-mapped material cases with active cell lighting agree at max0.00304. Original route visits all three light owners and publishes50 updates with geometry/material/pose retention. Two cases with differing camera/actor cells prove lighting follows the actor. Headless and cancellation gates pass.

See the [GPU checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CELL_LIGHTING_PREVIEW.md). See the [math checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CELL_LIGHTING_MATH.md).

The [legacy pixel COLOR boundary](LEGACY_PIXEL_COLOR_INPUT_CONTRACT.md) now saturates interpolated vertex terms before shader use; the engine-neutral light accumulator itself remains HDR.

The [bounded legacy light-selection contract](LEGACY_LIGHT_SELECTION_CONTRACT.md) now closes material participation masks, slot capacities, ranking and channel roles. Live visibility/order and vertex LIT remain open; the prior explicit modern policy is not original slot selection.

The [recovered slot adapter](LEGACY_LIGHT_SELECTION_CONTRACT.md) now supplies confirmed secondary-only cell inputs to baked rooms; optional recovered selection also covers unbaked character/doors. Prior all-record room diffuse contributions are superseded. Ambient/attenuation/vertex exponent remain explicit policies.
