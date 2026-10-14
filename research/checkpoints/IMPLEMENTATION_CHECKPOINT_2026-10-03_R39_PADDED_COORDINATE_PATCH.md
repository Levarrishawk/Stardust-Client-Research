# Round 39: complete padded coordinate patches (2026-10-03)

Question: can the accepted coordinate grid and complete supplied height, normal and resolved ARGB channels produce a bounded immutable patch set using the recovered tile/border mapping and all nine accepted seam patterns? Stop after that boundary and independent verification; procedural channel generation and GPU work are outside this checkpoint.

## Inputs and reproducibility

The frozen Round 39 overlay contains 3,081 files. Its manifest SHA-256 is `4cac8ae4fe7a5315d8958682151ae982ec85de7f41f089afc8e8694ccf3a6e65`; ZIP SHA-256 is `b390959b5cb8e53e40523ea3764e7388a7975ca5708f897873d557e95b92dacb`. Worker HEAD is `65de8c88141071744dc75594be126b3bab3696a5`. Full member identity, containment, uniqueness, reparse-point preflight and postflight checks preceded work. The baseline executable SHA-256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; the verifier rejects a different or truncated executable before decoding. Analysis used cached Python/Capstone and .NET SDK 8.0.300 without downloads or original executable execution.

Orientation covered START_HERE, its accepted session, current status/ledger/unknown-function terminology, accepted coordinate/assembly/query contracts, prior checkpoints and existing tools. Shared living documents remain frozen for lead reconciliation.

## Evidence and confidence

Confirmed static observations: `0084A57D` obtains Tiles through `00B6E520`; `0084A65C` writes render-block +10. `0084A69E` writes appearance Margin to block +28. Assembly `00861912` retains +28 at chunk +68, while `00861927` reads +10 as the loop limit. `00861C18` through `00861C24` form border + twice column/row; `00861FC3` through `00861FD8` bound both loops by Tiles. The nine source offsets at `015E71C8` are independently checked.

Confirmed: original position, normal and color channels use separate descriptor widths (+60, +6C, +0C). The modern API deliberately requires complete channels with one coordinate-grid stride; it does not claim equal original dimensions. Render-block vtable slot `015E66A0` resolves through `008490C0`/`00B5CFC0` to `00B62EC0`, a single return, providing no resize or bounds check. Position dirty handling calls `00B65730`; `00B657D4` adds the vertical origin before the F32 store. Raw negative-zero height therefore remains retained while stored Y becomes positive zero under the supported arithmetic profiles.

Interpretation, strongly inferred: a complete width `2*(Margin+Tiles)` supplies active inclusive indices Margin through Margin+2*Tiles. Leading padding is Margin and trailing padding Margin-1. Display assembly requires Margin at least one, although the coordinate producer can retain other signed margins. Fresh margin three is a prior confirmed constructor value, not an implicit API default.

The static verifier checks 77 anchors, the nine offsets and the render-block slot. Borrowed-object teardown calls and local allocation-null branches are recorded in the [durable contract](../../docs/TERRAIN_COORDINATE_HEIGHT_PATCH_CONTRACT.md). Checked bounds, budgets and atomic exceptions are modern policy; safe legacy malformed-input equivalence is unknown.

## Compatibility result

TerrainCoordinateHeightPatch (external historical reference) retains immutable coordinate provenance, snapshots all supplied channels including unused padding, uses position axes independently of layer axes, applies height plus vertical origin under the retained position arithmetic policy, and delegates each row-major cell to accepted TerrainPatchAssembly. It performs no cropping, resampling or procedural channel evaluation. All patches must also satisfy the frozen surface-query constructor's projected-area requirement before publication.

Default budgets are 65,536 complete samples, 64 patches and 576 selected samples; hard ceilings are 1,048,576/1,024/9,216. Domain, aggregate caps and exact channel lengths precede typed channel allocations. Complete numeric channels and strict display axes are checked. Arbitrary ARGB bits remain data. Failure publishes no partial result; callers cannot mutate retained snapshots. Concurrent source mutation is outside the caller contract. Future point queries retain their own interpolation eligibility policy.

## Validation

The independent rational oracle covers 81 precision combinations, signed requests, adjacent requests on both axes, exact padding, all nine seam patterns, raw signed zero, full channels and caps, invalid numerics, aliases and late failures. Final expected receipts: 2,839 cases, 2,805 accepted and 34 rejected; 8,624 exact patches and 77,616 exact selected samples; 41 ownership checks. These are synthetic fixtures, not recovered terrain channel samples.

The runnable Invoke-PaddedPatch.ps1 (external historical reference) accepts explicit Python, executable, static-package, cached-NuGet and dotnet paths, and writes only beneath an ignored private RunRoot. It generates fixtures, builds a runner referencing the actual separate Core assembly, compares exact receipts, builds the actual Core/SmokeTests projects and checks executable-identity negatives. Cleared feeds and locked restores use existing caches. Final replay receipts are retained privately under Round 39 final-replay-3.

The first runner build failed CS0136 because fixture locals shadowed later scenario variables. Renaming those locals corrected it; subsequent builds passed. No execution-based legacy or GPU verification is claimed.

## Remaining uncertainty and next probe

Actual runtime arithmetic profiles, changing request/margin provenance and original per-channel dimensions still require direct evidence. Supplied heights, materials and normals remain prerequisites. Next action: trace one original layer-to-height generation boundary with its retained phase and request provenance, preserving the separate layer/position coordinate policies. Do not infer procedural channel semantics from this synthetic assembly result.
