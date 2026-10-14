# 2026-09-30 continuous triangle-box sweep

## Question and stopping condition

Can a translating world-axis actor box query first contact against owned stationary triangles without tunneling or substituting triangle bounding boxes? Stop at native controlled cases plus an independent synthetic time-of-contact oracle. This checkpoint uses authored synthetic inputs only; executable addresses and original collision-source claims are absent.

## Observation and interpretation

The [query contract](../../docs/TRIANGLE_BOX_SWEEP_CONTRACT.md) records ownership, limits, separating axes, initial-touch policy and stable triangle IDs. Native smoke gates pass analytic wall contact 5/12, a triangle-edge miss despite overlapping bounds, zero-box ray contact 1/2, outward/tangent support, inward blocking, genuine initial overlap, tie sorting and malformed geometry/query rejection.

The Python linear-program vertex oracle generates 512 float32 cases with seed 20260930. Exact synthetic fixture pin is `445F0E4E577C434F4BB29403052ADD3B0875AE60B3D0D2F1FB5B841DB82F0D3E`. All418 clear paths, 79 moving contacts and15 initial contacts match native classification/time within 1e-8; observed maximum parameter difference is 4.440892098500626e-16. Moving-contact normals are finite unit vectors. Initial strictness is covered by analytic native tests rather than inferred from the LP.

Confidence: Confirmed bounded replacement collision query, with independent sampled contact-time evidence. Implementation consequence: an engine adapter can use owned world-space triangles without importing prototype host physics into the core. No actor movement integration, sliding, acceleration or original wall/ceiling authority is claimed. Research scores and registries are unchanged.

## Reproduction and next action

Run native smoke tests, then `tools/python/verify_triangle_box_sweeps.py` with the built native smoke-test DLL and an ignored output directory. Add floor-path segment consumption and supported-floor rollback tests before integrating any obstacle set into the persistent actor. Source selection remains explicitly separate.
