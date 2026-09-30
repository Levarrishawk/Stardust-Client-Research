# Original Godot LOD inspector — 2026-09-29

## Bounded question and inputs

Can all four authored children of the verified original DTLA load into an owned inspector and switch according to explicit selection distance? Uses identities and geometry in the [DTLA reader](IMPLEMENTATION_CHECKPOINT_2026-09-29_DTLA_READER.md) and material graph in the [selector/material checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-29_DETAIL_SELECTOR_AND_MATERIALS.md).

## Implemented boundary

`TexturedDetailAppearanceReader` eagerly creates an owned engine-neutral inspector bundle, with a shared logical-path cache and 256 source limit. Unique encoded source and aggregate decoded texture budgets are 128 MiB each; the decoded limit is checked after each bounded child decode, so temporary allocation may exceed the aggregate retained limit. Missing/masked children fail explicitly, rather than silently selecting a default. This inspector is distinct from a lazy game appearance instance.

`DetailAppearancePreview` owns one verified StaticMeshPreview per child, checks every geometry/texture upload, selects with explicit distance and retains previous index. The Main adapter offers an offline selection-distance slider, orbit/zoom/reset, and a clear unlit-fallback label. Distance is deliberately separate from camera zoom: PIVT/local-origin derivation is not yet implemented. Auxiliary UV sets remain owned in core; the MAIN fallback renders UV0 only.

The preview now requires an authored opaque single-stage MAIN/MAIN fixed fallback rather than requiring every alternative to be single texture. Other candidate/program data and all source textures are preserved. It does not execute the legacy fallback combiner or reproduce lighting; its sampler policy remains the earlier explicit unlit policy.

## Verification

Native synthetic detail-bundle tests pass shared-dependency single-read caching, identity mismatch, missing and masked resources. Existing selector and parser negatives pass. Godot Debug build passes with zero warnings/errors. Both headless and real OpenGL 3.3/RTX 4070 SUPER inspector tests pass. The real GPU run verifies all four resource sets, selected-child switches at authored band midpoints and the asymmetric UV/front-face fixture. A private 1280×720 capture was visually inspected and shows L0 (552 vertices/534 triangles) with the slider. Source-derived image stays ignored. The existing root certificate diagnostic still appears with successful exit 0.

Run `tools/powershell/Invoke-StardustAssetPreview.ps1` using the original DTLA member/hash and explicit runtime/archive/configuration paths, adding `-Detail`. `-Headless` validates resources; omitted runs GPU verification. `-Interactive` opens the inspector; `-CapturePath` requires a fresh private filename.

## Remaining work and precise next probe

APT redirects and CMPA composition are not part of this detail bundle. Runtime lazy loading, cancellation, default-child policy, actual PIVT origin, visibility-model adaptation, forced details and original full materials remain open. Next appearance milestone: strict APT redirect resolution and a bounded composed appearance, preserving distinct missing-path versus missing-target behavior. Modern DDS BC7 remains an independent extension with verified codec fixtures required.
