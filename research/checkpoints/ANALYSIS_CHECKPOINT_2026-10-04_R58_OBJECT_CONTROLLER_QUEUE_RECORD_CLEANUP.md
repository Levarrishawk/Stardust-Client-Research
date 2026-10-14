# Round 58: object-controller record cleanup

Date: 2026-10-04. Question and stopping condition: close `00AA67C0` initialization and `00AA6370` direct record-entry cleanup behavior with two complete bodies; stop at the virtual slot-zero target.

## Evidence and findings

The [durable contract](../../docs/OBJECT_CONTROLLER_QUEUE_RECORD_CLEANUP_CONTRACT.md) records the executable identity, body digests, exact instruction witnesses, observations, interpretations, confidence and implementation consequences. The producer (external historical reference) and independent checks (external historical reference) provide reproducible static gates. No canonical names or shared ledgers were changed; shared integration belongs to the lead.

Confirmed: record A+8 is initialized to zero; the cleanup helper reads entry+8 at stride 16, calls each non-null payload's virtual slot zero with its own pushed 1, and clears the pointer only after normal return. Confirmed: the caller's second argument is unused, incoming queue ECX is unused, record bounds are snapshotted and not directly reset. Strongly inferred: the virtual operation disposes of a payload. Unknown: concrete payload RTTI/target, deletion/free policy, ownership/alias invariants, callback mutation, recovery after partial completion, and record storage release by `00AA6710`.

Two primary bodies, 30 instructions, 20 selected anchors; zero direct and one indirect internal call site; one distinct unresolved callee lead; zero external xref searches. Accepted round-57 caller facts were reused and three caller edges were independently byte-checked; its full audit was not repeated. No further helper body was expanded. No real corpus occurrence or virtual target was available within this bounded probe.

## Frozen scope and dispatch correction

Incoming snapshot is round 58 (3,832 files), manifest SHA-256 `1b7f1accb970175d436e43d0a2fbc381eaf8c460505692741a8f3eeddf94445c`, ZIP SHA-256 `8651f06b9398f910ac8ea2bd1fc246bb448602278c69c5a58bdbb99c7612e2d5`, source HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. The lead corrected the dispatch's incoming snapshot, allowlist and final review labels to round 58. Prior accepted round-57 handoff hashes remain the prior gate. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5` and empty index must remain unchanged.

Overlay preflight checked archive members, normalized names, case collisions, hashes, sizes, containment and lexical ancestor link/reparse safety before public writes. Prior handoff hashes reconciled; all 3,832 postflight frozen hashes passed. Four assigned additions are the only permitted changes relative to that snapshot.

## Validation

Executed validation passed: the SHA-gated producer (20 anchors, two full bodies), independent runner (20 bytes, three caller edges, 20 negative cases and synthetic event cutpoints), organized workspace validator, offline locked restore and smoke-project build (zero warnings/errors), AST/UTF-8/LF checks and whitespace checks. Smoke executable replay was not run because there are no native production edits. Final review rechecks relative Markdown links, all 3,832 frozen hashes, the four assigned additions, HEAD and empty index. Static checks and synthetic event cutpoints do not execute the original callback. Original execution, live contact, fuzzing, full scene teardown, effective physical profile and concrete payload ownership remain unrun/unresolved.

## Exact resume point

Inspect `00AA6710` for record/storage release and reset behavior. A separate entry+8 producer and genuine payload vtable are required to close actual deletion and ownership. Preserve the distinction between ignored caller argument and internal virtual argument 1.
