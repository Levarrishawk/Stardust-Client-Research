# October13 bounded original baked road height

Question: can enabled AROA0005/HDTA0001 height at chunk(-140,-161) be
implemented from target writer, baked association and interpolation evidence?
Stop condition: recover target graph and supported original profile, then pass
independent arithmetic/store checks plus original overlap/seams without adding
shader/RNG/flora or editor regeneration.

Canonical evidence, confidence, input identity and modern limits:
[baked road height contract](../../docs/TERRAIN_BAKED_ROAD_HEIGHT_CONTRACT.md).
Prior orientation/terrain searches were retained, and target SHA-256 was
rechecked before VA use. Existing disasm_range.py/read_pe_values.py and prior
RTTI probes supplied target evidence. No original execution or live contact.

## Findings and implementation

HDTA0001 retains framed SGMT order; each SGMT is F32 XYZ triples without a
count prefix. Vertex winners use exact corresponding endpoint Y, whereas
open-line winners select the matching baked segment and interpolate its
dominant-axis straddle after X/Z endpoint clamping. Strict closest comparisons,
four direction branches, recovered F32 store points and caller height writes
are **Confirmed** by target instructions. Empty/unsafe or collapsed cases and
original arithmetic controls are explicitly outside the modern profile.

Original enabled pad road98781 matches all endpoints/segment counts and shared
junction;45/49 samples are strictly monotone on decreasing Z/increasing X.
Supplied positive amount gates rather than blends. Core directly overwrites
desired height; feather uses distance/halfwidth. NGE source only corroborates
these facts; no0006 fixed-height behavior was imported.

Added strict TerrainRoadHeightInput binding and engine-neutral supplied height
operation. Traversal keeps conservative exclusion first, admits only supported
baked profiles at possible hits, then applies height in original layer/affector
order. Existing exclusion tests now recognize the separately supported neighbor
height slice; historical nonintersection checkpoint behavior remains dated.

## Validation and handoff

Native focused suite and independent Fraction/Decimal verifier run on the
explicit modern graph; the oracle checks winner, projection, baked interpolation,
distance, core/feather and all result bits. Shortest JSON float32 decimals are
restored to exact F32 operands before dyadic verification. No original numeric
parity is inferred. Receipts containing original baked samples remain private.
The focused gate passes3054 assertions/2050 oracle samples. A noninteger
endpoint-distance fixture independently establishes rounded-up best-distance
selection followed by the target's unguarded first-coordinate0/0 route;
the modern helper explicitly refuses that collapsed straddle. Pre-cancelled
binding/query and bounded traversal work/cancellation checks pass.
The prior exclusion suite also replays1039 checks/828 independent cases.
Workspace validator passes1941 Markdown files with zero broken/private links
and zero duplicate canonical addresses. Focused links and git diff --check pass.

Original(-140,-161) contains33 unique hit cells among196 padded cells,17 core
and16 feather, tested at three positive amounts. Visible2x2 crossing region
(-140,-162) and independently generated overlapping1x2 region(-139,-162)
preserve all padded heights/normals bit-for-bit, with38 shared-sample checks.
Current research/address/protocol scores remain unchanged.

Exact next parent action: register TerrainRoadHeightTests, replay independent
oracle and affected original/regression/GPU gates, then reconcile living status,
session and ledger. Unsupported hit profiles must continue to reject; shader,
flora, editor rebuild and exact x87 controls remain unresolved.
