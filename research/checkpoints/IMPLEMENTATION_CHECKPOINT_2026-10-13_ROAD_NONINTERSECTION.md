# October13 bounded road nonintersection

Question: what target road-height behavior is required to admit the sole
remaining sparse-survey rejection, chunk(-140,-160), AROA layer167?
Stopping condition: identify its original active geometry, prove target no-hit
gating and either implement bounded exclusion or identify missing height proof.

Canonical evidence, confidence, original identities, target addresses and
modern limits: [road nonintersection contract](../../docs/TERRAIN_ROAD_NONINTERSECTION_CONTRACT.md).
Existing terrain/session/ledger orientation was retained; searches found no
previous semantic road contract beyond conservative full-width bounding boxes.
Baseline executable hash was rechecked before VA probes. Existing static tools
find_pe_strings.py, find_vtables_by_rtti.py, dump_vtable.py,
find_pe_pointer_refs.py and disasm_range.py supplied target observations.
No original execution, live contact, publication or raw decompiler promotion.

## Findings and consequence

The post-terrace survey reports440 generated and one AROA rejection. Layer167
contains a disabled road and two enabled roads; none of the196 actual padded
samples reaches an enabled target halfwidth corridor. Target00B87FC9 calls
00B7BFF0 and00B87FD0 skips writes onfalse. Target finder accepts only strictly
closer vertex/segment distances, finally strictly below squared halfwidth.
Thus this request needs no road-height interpolation. Observation and no-hit
gate are **Confirmed**; original x87 results remain Unknown. Independent exact
rectangle geometry gives nearest distance23.316598m vs halfwidth11m, even
beyond the modern fullwidth+1m guard23m.

Implemented strict AROA0005 geometry input with owned opaque HDTA and tail
bytes; conservative binary64 segment-capsule exclusion with bounded coordinate,
input and work domains; and tiny traversal substitution for the prior bounding
box exclusion. Any possible hit continues to throw the existing unsupported
writer failure. No height/material/flora arithmetic or0006 fixed-height path
was implemented. NGE source only corroborated target structure/roles.

## Validation and exact continuation

Standalone tests pass1039 checks/828 oracle cases. This suite
has240 synthetic geometry samples plus588 cases over three original roads
and196 padded samples each. Every synthetic truncation, malformed/domain and
degenerate negatives, ownership copies and actual chunk construction also pass.
Neighboring possible-hit chunk(-140,-161) still rejects with the exact
unsupported AROA layer167 reason.
Independent verify_terrain_road_exclusion.py compares exact-rational capsule
distance and separately checks the authored original separating-plane proof.
Baseline workspace validator passes1938 Markdown files with zero broken links,
zero private links and zero duplicate canonical addresses. Focused document
links and git diff --check pass.
Original terrain and TGEN pins remain unchanged; receipts stay beneath ignored
local-private. No actual road application or original numeric parity is claimed.

Next parent action: register the standalone suite, independently replay oracle,
rerun all441 sparse points and selected original-region/GPU gates, reconcile
living status/session/ledger, and preserve possible-hit failures. To implement
actual roads later, first close target SGMT height interpolation/endpoint/fallback
semantics at00B7D020->00B7C3D0 using an original confirmed-hit fixture.
