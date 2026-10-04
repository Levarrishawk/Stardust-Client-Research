# Owned family to conditional terrain consumer join

Date: 2026-10-04. This bounded composition uses the [owned family set](TERRAIN_FRACTAL_FAMILY_SET_CONTRACT.md), [opt-in sampler modes](TERRAIN_CONDITIONAL_FRACTAL_MODE_CONTRACT.md), and the existing retained AHFR/FFRA consumers. Original generated-terrain parity remains Unknown.

## Composition and controls

Decode the selected original MGRP/0000 once, preserve all 43 ordered records, and explicitly select signed family ID 1. Verify independently the full 5,633-byte group SHA, every signed ID, relative offset, extent, terminated name and thirteen raw words. Only name lengths/hashes and aggregate identity appear in public results; extracted names and word arrays remain private.

For each case, construct a fresh owned 1x1 sampler with explicit nearest-even table, construction and query precision24/53/64. Evaluate the actual selected raw MFRC and seeded kernel twice at the same supplied cell and coordinate. The first result is a computed retained miss; the second is the stored F32 reload. Feed those actual results separately through AHFR, FFRA, influence multiplication and child composition. No expected noise or supplied sample is injected into native code. Caller cell assignment and fresh-cache lifetime are explicit modern policies, not recovered work-request behavior.

The original unchanged full group supplies216 mode0 cases. Another2376 cases use explicitly synthetic single-entry groups with mode words0..5 and original-derived or authored three-octave parameters. Their copied source names remain private. All test coordinates and arithmetic profiles are synthetic; mode overrides do not claim an original corpus occurrence.

## Independent verification

The [Python peer](../poc/round50-tests/Integration/family_sampler_join_peer.py) independently parses the installed overlay and group, computes mode contributions and octave/normalization stores with Fraction/integer rounding, and composes the frozen independent consumer models. The [native program](../poc/round50-tests/Integration/FamilySamplerJoinProgram.cs) uses actual Core components and immutable family selection. Input identity is gated before address-based static evidence is reused.

Lead replay passed2592 exact comparisons:216 unchanged original-group cases and2376 synthetic-group cases across six modes. Computed/cache differences propagate in426 AHFR,152 FFRA and462 final results. All43 original entries match their independently parsed metadata and raw words. Default mode0 factory behavior remains unchanged.

Generate and verify with the peer's positional interface: `generate|verify asset-root config exe private-fixture [private-native private-receipt]`, from the repository root. Build [FamilySamplerJoin.csproj](../poc/round50-tests/Integration/FamilySamplerJoin.csproj) against installed offline dependencies; execute its DLL with private fixture/output arguments. Producer and native output contain proprietary-derived bytes and must stay ignored.

Observation: original grammar/group bytes and recovered finite arithmetic stores are Confirmed. Interpretation: the composed result matches the supplied conditional profiles, independently verified. Compatibility consequence: retain wide miss/F32 cache distinction across consumers, keep parser acceptance separate from sampler eligibility, and require explicit lookup failure/control/cache policy. Remaining Unknown: actual original controls, optional sine/curves, work-request cell assignment/invalidation, layer routing, map reload/quiescence and complete terrain parity. Next probe: a bounded family-backed accepted-layer evaluation under an explicit caller cache lifecycle.
