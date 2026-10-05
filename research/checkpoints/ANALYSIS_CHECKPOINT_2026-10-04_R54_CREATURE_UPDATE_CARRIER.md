# R54 Runtime: CreatureObject update carrier

Date: 2026-10-04. Bounded question: does CreatureObject slot `+10` override `00430260` preserve the scheduler update word and receiver into the accepted Object/controller route? Stopping condition: two new complete carrier bodies, or closure with evidence. The two-body limit was reached at the input of `00554C60`; actual player callback units remain Unknown.

## Inputs and tools

The executable and SHA-256, complete extents, addresses, tools, and reproducible command interfaces are recorded in the [durable contract](../../docs/CREATURE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md). Static-only analysis used installed offline Python/pefile/Capstone and independent raw PE mapping. The frozen R54 snapshot contains 3691 public files, manifest SHA-256 `08df3c05dd838854b14d34c235d22ce2be0b566e34fa91f8366a07abe70179dc`, ZIP SHA-256 `66bc9a7d2ce0c88bf7c89a28291bfafba804cadb6bbbce273104d6cc835b157f`, and main HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`.

Before the single overlay, all five reviewed R53 Runtime path/hash pairs were matched against the worker handoff, integration receipt, and current bytes. Archive membership, member identity, destination containment, reparse/link rejection, and every destination were preflighted before writes. All 3691 incoming files were verified afterward. Worker HEAD and index were preserved; the main checkout was read-only.

Required orientation covered START_HERE, its accepted R53 session, current status/ledger/unknown-function terminology, the frozen player update units contract, checkpoints, existing tools, and curated names. Prior direct proof for `00554C60` was not found in the scoped public evidence search; its body was deliberately not followed.

## Result

Observation: `004302B3/C0/C1/C3` load and push the original input word and restore the original object ECX for `0063F3B0`. The optional `00660E10` branch rejoins before forwarding. The intermediate body's `0063F3B6/BA/BB/BD` forwards the same word and unchanged ECX into `00554C60`. Both selected call sites are unique; bounded direct control flow contains no bypass, input-slot overwrite before the calls, or post-call return to their prefixes.

Interpretation: strongly inferred ordinary ABI forwarding preserves the input word and object pointer, without local time conversion or substep division in these prefixes. This assumes normal return and the usual nonvolatile-register convention; optional side-call effects, indirect calls, exceptions, and reentrancy remain unclosed.

Observation: `0063F3C2` spills returned ST0 into the argument slot and `0063F4DB` reloads it. CreatureObject's three later argument-slot stores likewise occur after forwarding. Interpretation: these are return/scratch uses, not evidence of changing the already forwarded input. Return semantics, precision/control state, and downstream ownership remain Unknown.

Confidence: exact instructions, extents, direct branch boundaries, RTTI/dispatch, and ordering are Confirmed. Conditional seconds at the input of `00554C60` follow from the accepted ordinary scheduler route plus these raw-word copies. Actual player callback units and selected controller binding are Unknown.

Compatibility consequence: preserve the two new boundary copies and receiver identity; do not implement native acceleration or assume callback seconds from this partial path. No wire/asset coverage metric changes, canonical semantic names, or combined completion percentages are introduced. Shared living status/ledger and prior frozen documents remain untouched for lead reconciliation within their separate scope.

## Validation and reproducible artifacts

The [producer](../../tools/python/verify_creature_controller_update_carrier.py) verifies 25 selected anchors, complete bodies of 2848/309 bytes and 882/96 instructions, 127/8 internal direct jumps, normal return forms, forwarding call uniqueness, and bounded input-slot/control-flow conditions. It reuses frozen prior evidence rather than repeating the clock/FPU/caller survey.

The [independent runner](../../poc/round54-tests/Runtime/research_checks.py) checks 16 literal byte windows, three relative calls, optional branch, RTTI/dispatch, and two complete-body digests through its own raw PE mapping. It passes 36 synthetic opaque-word/receiver cases, four conditional seconds cases, and eight negative checks. These fixtures are explicitly synthetic and are not original-client execution evidence. The first runner attempt failed on an incorrect 20-byte RTTI terminator span; the expectation was corrected to 21, and a fresh output directory passed. Failed and corrected logs are retained privately.

The scope audit and proportional workspace/Core.SmokeTests checks are recorded in the worker handoff receipt. No original execution, live server, capture, fuzzing, external mutation, downloaded toolchain, generated proprietary corpus publication, staging, commit, or push occurred.

## Resume point

The next precise unclosed carrier is `00554C60 -> accepted Object 00B23100/controller wrapper 00B3D1C0`. Follow `00554C60` and at most one necessary carrier body in a new bounded probe; establish word transformations, gates, receiver continuity and the selected controller attachment before promoting actual player callback units. The current two-body investigation is complete at that boundary.
