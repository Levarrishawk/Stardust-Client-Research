# Round 55: bounded player queue constructor/destructor carriers

Date: 2026-10-04. Question: does the player constructor's base call establish actual controller `+4` queue allocation and destruction? Bound: one selected lifecycle, at most two new primary bodies plus directly needed tiny accessor/deleting thunks, and at most 20 fresh selected candidate references. Stop at an unresolved callee rather than expand a whole queue inventory.

## Identity and orientation

Executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static PE mapping, instruction decoding, constructor register flow, RTTI/vtable pointers, and exact small byte patterns. No original execution, live contact, extraction, native production edit, registry update, or external mutation.

Frozen55: 3,725 files; manifest SHA-256 `ab13317c6eaaf55ff276c79c6c580ccddfc165ab1a5d6fe966eeb3a025064438`; ZIP SHA-256 `3ccf19f23a5616f9831e3a891cb824d9cf228bd06812c14b73c72b72562ebe60`; source HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`. Actual round54 final handoff and four hashes were crosschecked with the lead's reviewed integration before overlay. All archive/member/name/case/hash/size, lexical ancestor/link/reparse, root containment, and destination hashes were preflighted before writes; all frozen hashes and HEAD/index/unrelated bytes passed postflight.

Read current entrypoint/accepted54/daytime record and living status/ledger/function terminology, then reused [the accepted map assignment contract](../../docs/GROUND_SCENE_INPUT_MAP_QUEUE_ASSIGNMENT_CONTRACT.md) and prior checkpoint/tool evidence. The new round55 contract did not yet exist during initial discovery; this was not treated as evidence. Required specifications are created only within the assigned four-path scope.

## Result and confidence

Canonical observations and consequences are in [the durable contract](../../docs/PLAYER_CONTROLLER_QUEUE_LIFECYCLE_CONTRACT.md). **Confirmed:** two complete carriers preserve the same receiver and object argument along `00629400 -> 006385C0 -> 006E4A20 -> 00B4AE50`. The two new bodies are 267/85 bytes, 90/29 instructions, SHA-256 `a4b53f3370a13de4f2ff7ca43dc0548b8e013a83b202d74d824871be4ff6a354` and `11a0d30ff1bc7ba39e81b02a8425cb9759b36066dcfa318db26aea27013207a4`.

Actual installed vtables/RTTI identify CreatureController and ClientController in this chain. Together with accepted PlayerCreatureController metadata, their slot-zero 33-byte/14-instruction thunks call `00629600`, `00638700`, and `006E4AB0` respectively, then conditionally pass the same receiver to `00AC1640` according to flags bit zero. Call shape and order are **Confirmed**; deleting-destructor/release roles are **Strongly inferred**. The non-deleting bodies are unexpanded, so base-destructor and queue cleanup order are not inferred.

**Partial result:** controller `+4` queue allocation, exact queue class/RTTI/count/layout, ownership, non-null/failure preconditions, and actual destruction remain **Unknown**. No direct controller `+4` assignment appears in the two selected carriers. Higher-offset support allocations and their null branches are explicitly separate. `00B4B070` post-base helper also remains unexpanded. The prior scene pointer-expression assignment stays Confirmed; its ordering relative to controller destruction is Unknown.

## Budget and verification

Two new primary bodies exactly; three directly needed tiny deleting-destructor-shaped bodies; two known four-byte accessors. Thirteen explicitly selected call/vtable references (including the reused player base boundary) fit the fresh 20-candidate cap. No broad raw-call census, queue inventory, physical profile/event probe, or frozen full GroundScene/producer replay was performed.

[Verifier](../../../SWG-Godot-Client/tools/python/verify_player_controller_queue_lifecycle.py) requires explicit `--exe`, `--packages`, and a fresh ignored `--receipt`. [Independent runner](../../../SWG-Godot-Client/poc/round55-tests/Networking/research_checks.py) requires explicit `--exe`, `--packages`, and a fresh ignored `--output-dir`. SHA rejection precedes VA use. Positive gates cover 38 anchors, bounded bodies/accessors, and three RTTI chains. Independent gates cover 11 bytes/ten calls/three RTTI names/three virtual slots, two preserved-receiver carrier patterns, and three deleting-thunk patterns. Eighteen negatives include altered identity/evidence/edge, truncation, receiver change, flags-mask change, invalid spans and outputs, pre-resolution lexical link/reparse rejection, and cap overflow. Fresh nested output and rejected output-state preservation are checked.

Final handoff validation covers workspace links/organization, UTF-8/LF and Python syntax, offline locked Core/SmokeTests restore/build with zero warnings/errors, whitespace, all 3,725 frozen hashes, unchanged HEAD and empty index, and exactly four assigned additions. Exact hashes/results belong in the ignored final receipt. Unrun gates: original execution, physical/live occurrence, underlying queue allocation/destruction, and unrelated smoke runtime replay. No successful static gate is represented as runtime lifetime verification.

## Resume

Next construction body: `00B4AE50`, reached directly at `006E4A43` with unchanged receiver/object argument. Choose its corresponding destruction path from its actual installed vtable, not an adjacent address. Close the direct controller `+4` writer and queue identity/count/layout/null policy before extending cleanup. Reuse the current two carriers and accepted GroundScene bind proof. Shared canonical integration remains lead-owned; stop idle after this handoff.
