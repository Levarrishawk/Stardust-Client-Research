# Round44 observer-state and terrain prerequisites

Date: 2026-10-03. Accepted after independent review and the combined gate at
2026-10-04 00:30:47UTC. No recipient-store join is included in this round.

## Bounded observer question

Does the accepted Round43 opt-in full CREO4 world accept the server-shaped
three-scalar observer update after object creation and before end-baselines,
without a full private baseline? Stop after the active and staged cases and
the default opaque regression have deterministic outcomes.

The [delivery contract](../../docs/CREATURE_VIEW4_DELIVERY_CONTRACT.md) supplies
the separately pinned server call order and original client dispatch evidence.
This new probe runs only the replacement Core. Its literal packet is synthetic:
object DABC001122334455, fields1/5/10, raw F32 words3F800000/40000000/40400000.
It is not a packet capture or proof of deployed server floating-point behavior.
The original executable and assets are not executed or mutated.

Inputs: accepted main HEAD5493338d9cd4255cf58f25de6c4cd15f6507b818 and the
Round44 frozen3310-file snapshot identified in the
[round session](../sessions/PARALLEL_DEVELOPMENT_ROUND_44_2026-10-03.md).
Tools: .NET8 native runner with an offline NuGet configuration, and independent
Python struct parsing; no production codec is imported into the Python oracle.

## Observation, interpretation and consequence

The [native probe](../../../SWG-Godot-Client/poc/round44-tests/Integration/Program.cs) passed23
lifecycle counterexample checks and20 accepted partial-state join checks. With
opt-in full4, active pending-baseline delivery rejects with
`Finalized CREO4 target required.` and retains the current incarnation and active
scene, with zero typed state bytes. The same packet staged before publication
rejects during ordered replay and leaves the world Failed with no objects or
typed bytes. The default opaque path publishes the create/update/end sequence,
retains one envelope and supplies no typed scalar semantics.

Observation confidence: Confirmed replacement behavior from execution.
Interpretation: the opt-in complete-model policy cannot implement this ordinary
observer path. This independently confirms the already documented compatibility
gap; rejection is not an original-client admission rule. Consequence: add a
separate partial model with received-field provenance and explicit absence,
then validate lifecycle admission against this exact order. Do not invent an
all-zero baseline, make private view4 mandatory, or guess untouched fields.

The independent [literal oracle](../../../SWG-Godot-Client/poc/round44-tests/Integration/observer_peer.py)
checks all envelope scalars, exact length and ordered raw assignments, both
failure outcomes and four independently modeled partial states. The same literal
observer wire applied to the accepted partial kernel makes only1/5/10 known;
repeated equal values update provenance, raw signed zero remains received, and
untouched fields remain absent. Earlier states and exact receipts are immutable.
This is a kernel join, not world admission. Retained detailed output remains
private and regenerable.

## Validation and remaining work

The first build used an incorrect enum spelling `Created`; inspection showed
the canonical phase is `PendingBaselines`. The spelling was corrected before
the successful build and23-check execution. The successful build has zero
warnings and errors. The full4 regression was first invoked with an incorrect
private fixture path and failed before fixture reading. Discovery located the
canonical public ordered-fixtures.json; corrected replay passes121 sequences,
3719 checks and the independent2424-event oracle. The networking research runner
was first called without its required arguments; corrected explicit inputs pass
all18 checks. Default smoke passes in a64-bit process with18 journal events and
zero failures; its successful separate build has zero warnings/errors.
The probe's opt-in payload limits are explicitly1024 bytes
to fit the enclosing world policy; no production limits were changed.

## Independently reviewed specialist results

Runtime's received-only kernel passes115 cases/528 messages/12516 ordered events
against the literal Python oracle;24425 native checks,124323 malformed-input
negatives,7925 budget checks and9917 ownership checks pass. Root replay uses
the actual separate Core project and has zero build warnings/errors.

The [slope initializer](../../docs/TERRAIN_FILTER_SLOPE_INITIALIZATION_CONTRACT.md)
replay passes62 anchors, the26340-slot startup range, eleven direct clamp
references, eight identity negatives and66 synthetic structural negatives.
Slot018560AC registers thunk00B77660 ->00B77670 and exact3FC90FDB is stored
before ordinary application entry on the returning startup path. Universal
earlier-callback constructor ordering and FSIN results remain Unknown.

The [original height order](../../docs/TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md)
replay passes150 new anchors, accepted preparation/FHGT prerequisites and eight
identity negatives. One pinned source provides the selected five-layer prefix:
root/earlier-sibling AHFR can write the shared height before selected FHGT;
FFRA precedes that FHGT, and the prefix has no AHCN. Three grids and derived-
cache demand/invalidation are distinguished. Numeric AHFR/FFRA/FSLP/AFDN/cache
values remain Unknown; no original generator or GPU proof is claimed.

The [template initializer](../../docs/CREATURE_VIEW4_INITIALIZATION_CONTRACT.md)
replay passes101 new anchors plus accepted58 constructor/24 dispatch/107
registration prerequisites and18 research checks. Concrete client/shared
template RTTI/factory, speed slots, slope/water fields, loaded/parent routing,
conditional fallback, mutable-global mode4 and nominal relative operand order
are Confirmed. Actual effective values, sampling state and transitive overrides
remain Unknown; no defaults are promoted into production.

All four final diffs were independently scoped and reviewed before integration.
No existing production file changes: this round adds only the partial kernel,
tests and research/probe artifacts. Final combined workspace acceptance passed. Next bounded implementation: a prepared current-incarnation partial
store, followed by pending-baseline world admission. Next terrain prerequisite:
actual AHFR and FFRA consumer contracts with explicit supplied fractal input,
before reproducing original noise/map or admitting original generated terrain.
