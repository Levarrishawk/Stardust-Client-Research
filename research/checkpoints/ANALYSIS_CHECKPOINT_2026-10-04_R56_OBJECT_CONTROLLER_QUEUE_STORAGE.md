# Round 56: controller queue writer and cleanup boundary

Date: 2026-10-04. Initial question/bound: selected `00B4AE50` and actual vtable-selected destruction, two primary bodies plus necessary tiny deleting thunk/accessor, at most 20 candidate edges. The observed 28-byte/11-byte NetworkController wrappers delegated the queue work. **Explicit lead refinement authorized `00B3D0F0` and `00B3D190`: four primary bodies total, including both thin wrappers.** No further transitive body expansion was authorized or performed.

## Identity and method

Executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; static PE bytes/complete decoding, direct register flow/branches/calls/tail jump and actual installed vtables/RTTI. No original execution/live contact, native production, registry, stage/commit/publication, or external mutation.

Frozen56: 3,763 files; manifest SHA-256 `3185e1f40f315f216ec298817111c08e47b083f658747499d7e391618ea66185`; ZIP SHA-256 `8e6a7ca90963a56d9cc5ef3f9f8f6b013bd78d254c1ceb06a025f2666ac72fd6`; source HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`. Actual prior55 final handoff/four reviewed hashes were crosschecked before overlay. Every archive/member/name/case/hash/size, root/lexical ancestor/link/reparse/containment and destination hash was preflighted before overlay writes; all frozen hashes and HEAD/index/unrelated bytes passed postflight.

Current entrypoint/accepted55/daytime/living status-ledger-function vocabulary and relevant durable/checkpoint/tools were read. Prior55 carriers and54scene bind proof are reused. The assigned filename does not establish class: RTTI identifies NetworkController and Controller, not a queue class.

## Findings and confidence

Canonical evidence/consequences and four exact primary hashes are in [the durable contract](../../docs/OBJECT_CONTROLLER_QUEUE_STORAGE_CONTRACT.md). **Confirmed:** NetworkController constructor `00B4AE50` forwards to Controller constructor `00B3D0F0`; NetworkController destruction body `00B4AEA0`, selected through installed vtable, tail-jumps to `00B3D190`. Body sizes/instruction counts are 28/12, 11/2, 99/33, and 43/16 respectively.

Controller constructor requests `0x14` bytes through `00AC15C0`, calls unexpanded `00AA6050` with the allocation as receiver and raw argument `0x10`, and stores that call's EAX return at controller `+4` at `00B3D134`. Allocation returning zero branches to EAX=0 before the same store. Controller object argument is stored at `+8`; installed vtable/RTTI identify Controller.

Matching destruction loads the same `+4` field at `00B3D194`, skips both queue calls for zero, otherwise calls `00AA6310` on that pointer before passing it to `00AC1640`. Normal completion clears `+8`, not `+4`. **Confirmed:** writer/field identity, null branches, call order and state writes. **Strongly inferred:** owned returned-object storage and cleanup/release roles. **Unknown:** initializer return equals allocation, concrete queue class/RTTI/fields/counts, meaning of raw 16, deeper allocation/error/exception/callback policies and helper internals.

Two actual vtable-selected 33-byte/14-instruction deleting thunks (NetworkController and Controller) call their corresponding body before flags-bit-zero conditional self-release. Shape/order Confirmed; deleting/release semantics Strongly inferred. Selected Controller queue cleanup/release precedes thunk self-release on normal return. Entire derived player destruction chain and map detachment relative to it remain Unknown. Destruction re-reads current `+4`; unchanged identity since construction and intervening queue-pointer replacement are not proved.

## Validation, corrected assumptions, and scope

Final bound: four primary bodies, two necessary tiny deleting thunks, one known four-byte accessor; 13 selected call/jump/vtable references under20. No broad caller/queue census, queue initializer/cleanup body, or full prior55/54/producer replay. A preliminary two-body partial receipt remains private; it is superseded by the final four-body handoff. The actual RTTI corrects class wording implied by the assigned filename; no canonical semantic name changes. No positive static/independent gate failed.

[Verifier](../../../SWG-Godot-Client/tools/python/verify_object_controller_queue_storage.py): explicit `--exe`, `--packages`, fresh ignored `--receipt`. [Independent runner](../../../SWG-Godot-Client/poc/round56-tests/Networking/research_checks.py): explicit `--exe`, `--packages`, fresh ignored `--output-dir`. SHA before VA use. Final positives:44 anchors/four primary signatures/exits/two deleting thunks/accessor/two RTTI chains/13 target edges. Independent:17 bytes/ten calls/two RTTI names/four pointers, constructor carrier/tail/both deleting shapes, and separate allocation-null-store / load-null-cleanup-release parsing. Twenty-two negatives cover identity/evidence/edge/offset/null branch/mask/tail/truncation/unmapped and malformed outputs, pre-resolution link/reparse rejection, and cap overflow. Fresh nested output and rejected output-state preservation pass.

Final validation reruns affected checks with fresh outputs after extension, plus workspace links/organization, UTF-8/LF/Python syntax, offline locked Core/SmokeTests restore/build zero warnings/errors, whitespace, every frozen hash, unchanged HEAD/empty index and four assigned additions only. The final ignored receipt contains exact source hashes/results/commands. Unrun: queue initializer/cleanup internals, original execution, actual physical/live occurrence, complete player/map teardown timing, and unrelated smoke runtime replay. No static pass is represented as runtime lifetime verification.

## Exact next action

Inspect `00AA6050` and `00AA6310` as the next selected initializer/cleanup pair. Close return identity, actual queue fields and meaning of raw 16, internal allocation/null/failure and cleanup; seek type evidence without naming from a nearby string. Keep full scene detachment/derived destructor and physical profile work separate. Lead owns shared canonical integration; stop idle after one final handoff.
