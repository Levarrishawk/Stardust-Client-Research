# Retained skeletal mesh bindings — October1,2026

## Bounded question and finding

Can stable mesh transform-name mappings be validated once per owner rather than resolved on every visual pose, while retaining original material resources? The prototype host preview now resolves owned immutable body/head skin transform names before GPU construction and retains each mesh's owned joint-index list. Per-frame palettes consume those lists. No global cache or mutable corpus sharing is introduced; mappings remain specific to one binding/geometry owner. The original52 mesh transform names are resolved once rather than once per update.

## Verification and confidence

Prototype host build passes. Original textured standalone GPU480-update/two-cycle and play/focus/pause/seek gates pass. Original textured interior GPU two-owner/two-cycle actor-foot/door invariants pass. Full vertex/direction/UV/index and all texture/mip readbacks pass, with new resource-instance identity checks showing each surface retains its material across pose updates. Source closure, core pose math and independent byte oracles are unchanged. Confirmed prototype ownership/host behavior; no original cache equivalence is claimed.

## Consequence and resume

Stable name matching no longer consumes frame work, and pose reconstruction cannot silently replace retained materials. See the [deformation contract](../../docs/SKELETAL_SHADER_DEFORMATION_CONTRACT.md). Next: carry original authored palette dependencies and selected colors in the engine-neutral character bundle, preserving fallback diagnostics before translating any shading.
