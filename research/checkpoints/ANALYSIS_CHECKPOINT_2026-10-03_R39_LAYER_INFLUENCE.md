# Round39: local layer influence and incoming product

Date: 2026-10-03. Question: in the general per-sample path of `0x00B63D20`,
what ordering and storage boundaries combine local boundary/filter queries
before an affector receives the incoming-weight product?

Stopping condition: SHA-gated instruction/table/constant verification and a
durable finite selection/gating specification. Complete queries, layer runtime
activation, generated original heights and implementation are excluded.

## Input and tools

Original executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Read-only PE mapping, Capstone, existing coordinate verifier and the new
`tools/python/verify_terrain_layer_influence.py`; the original was not executed.
Detailed disassembly and verifier receipt remain ignored private evidence.
The pinned Tatooine TGEN has original layer/boundary/AHCN occurrences already
established in [Round33](IMPLEMENTATION_CHECKPOINT_2026-10-03_R33_TERRAIN_AND_CREATURE_STATE.md).
No new corpus sweep or original influence comparison occurred.

## Observation and interpretation

The [canonical contract](../../docs/TERRAIN_LAYER_INFLUENCE_CONTRACT.md)
records exact addresses and separately labels direct facts and inference.
The verifier passes 60 new instruction anchors, two four-entry selector tables,
three F32 constants and twelve status-branch cases, alongside the 58 inherited
coordinate anchors and executable identity gate.

**Confirmed:** source-ordered boundary maximum selection starts at zero,
with one as the no-enabled-boundary fallback; compare an unspilled shaped
candidate against the old F32 word, then retain a stored F32 word. Early exit
tests exact one. Boundary inversion precedes a positive gate. Filters perform
minimum selection against this accumulator and early-exit at exact zero.
Filter inversion applies to the combined accumulator, including an empty
filter loop, before a second positive gate. Each enabled affector receives an
F32-stored product of local accumulator and saved incoming F32 weight.
The weight cache stores that product separately, including skipped samples.

**Strongly inferred:** recursive children consume ancestor influence through
the layer cache selected at `0x00B64239`. Complete parser/runtime ownership and
cache activation transitions have not been closed here.

## Discovery corrections and compatibility consequences

An initial magnitude shorthand for boundary early exit would have been wrong:
`test ah,0x44` plus `jnp` selects the equality status, not all values above one.
This was corrected by interpreting all four x87 compare outcomes and verifying
the literal branch. Likewise, filter inversion cannot be modeled as inversion
of an independently accumulated filter-only result. These corrections are
recorded before any implementation was accepted.

Maximum/minimum shorthand is valid for query values in `[0,1]`; arbitrary
finite values require the observed early-exit graph. No general clamp is
observed. Active original precision/rounding/exceptions remain **Unknown**.
The incoming product does not gate earlier queries; negative/zero parent
products require the affector's separately specified behavior.

## Validation and resume

Static verifier execution passes. Combined Round39 scope/link/whitespace
validation is recorded in the Round39 acceptance report after the four leaf
handoffs; this checkpoint itself does not accept that round.

Next: implement a bounded immutable conditional combine receipt over supplied
ordered query results under explicit arithmetic profiles, then validate an
independent rational oracle including unspilled comparison, equal signed
zeros, exact early exits, inversion, empty/disabled loops and incoming products.
Do not call it a full TGEN evaluator or infer original generated-height parity.
