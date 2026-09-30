# 2026-09-30 — Piecewise floor-path box collision

## Question and evidence

Can conservative actor-box collision follow actual floor height through a crease and return the first original-request contact parameter? Stop at analytic crease/chord counterexamples, negative gates and independent rotated-box agreement. The [durable contract](../../docs/FLOOR_PATH_BOX_SWEEP_CONTRACT.md) records exact policy and limits.

This checkpoint uses synthetic authored fixtures, not new executable-address evidence. The rechecked original baseline remains SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Original floor trace evidence remains in the [prior checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_PATH_TRACES.md). No assets or private detailed output are promoted.

Observation: a flat/ramp crease path hits a low obstacle missed by a terminal-height chord, while an overhead obstacle gives a false chord contact. Interpretation: per-triangle 3D segments are required for this bounded actor-box policy. Confidence: Confirmed analytic synthetic gates. Native tests also cover ramp/reverse/contact parameters, permissions, overlap/retreat, accepted prefixes and invalid/resource input.

## Verification and resume

Independent Python plane reconstruction and oriented-box interval reference passes 6,000 seeded synthetic queries: 1,946 Blocked, 2,898 Clear, 394 InitialOverlap and 762 PermissionUnknown. Fixture identity and reproduction are in the durable contract. Core smoke passes. No original actor execution, live server, publication, new names or rescore. Scores remain 89% / 94%, names 2,599 and packet rows 345.

Next: single-cell contact landing and deterministic precision-failure handling before enabling any portal-actor slope integration.
