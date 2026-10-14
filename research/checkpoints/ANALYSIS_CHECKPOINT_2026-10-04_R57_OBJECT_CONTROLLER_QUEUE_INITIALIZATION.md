# Round57: queue initializer return identity and five fields

Date: 2026-10-04. Question: what does exact initializer `00AA6050` return/write, how does raw16 affect its allocation arithmetic, and what does matching `00AA6310` clean? Bound: two new primary bodies, at most 20 fresh callee candidates, no transitive expansion/caller inventory/production edit/original execution.

## Identity and orientation

Executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static complete bodies, instructions/branches/calls, stack and field identity, pointer arithmetic and raw mapped-byte checks. No genuine queue RTTI is established; no class name is inferred from nearby text.

Frozen57: 3795 files; manifest SHA-256 `cbc40cfc91ebeb293c68a1a89fd75fede617a729d3cfe1f7618cbe58013d42b5`; ZIP SHA-256 `209bbf9901214cecc8b9ffbd2f8a69eadb225926fee31c0550061f2b2b10ac03`; source HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`. Actual final56 handoff (including its corrected verifier docstring/hash) was crosschecked with the lead's four reviewed integration hashes before overlay. Every archive/member/name/case/hash/size/root/lexical ancestor/link/reparse/containment/destination was preflighted before overlay writes. All 3795 frozen hashes and HEAD/index/private/unrelated bytes passed postflight.

Read current entrypoint, accepted56/daytime, living status/ledger/function vocabulary, and relevant durable/checkpoint/tools. Reuse [the closed outer allocation/store/cleanup boundary](../../docs/OBJECT_CONTROLLER_QUEUE_STORAGE_CONTRACT.md); no full Controller, scene or prior producer audit is repeated.

## Findings and confidence

Canonical evidence/consequences are in [the durable initialization contract](../../docs/OBJECT_CONTROLLER_QUEUE_INITIALIZATION_CONTRACT.md). Initializer 696 bytes/254 instructions/hash `3d9aecdf55a2b69d44c5becb7ba669254c5d250de66bf9c4c6a04616863f1c5e` ends `ret 4`. Cleanup 96 bytes/34 instructions/hash `e8d84f8aa8b8c1dac2109867b3aec627a21c91169f60645bacca1aeeb6e0787b` ends `ret`.

**Confirmed:** saved incoming receiver at `[ebp-18]` is returned, closing selected outer allocation-return identity on normal return. Five DWORD fields are two 12-byte allocation-result record pointers at+0/+4, initial aliases at+8/+0C, and zero/unknown-role field+10. Cleanup directly clears+8/+0C/+10, not+0/+4. Record-pointer identity after opaque record-operation calls remains Unknown.

**Confirmed arithmetic:** signed negative demand clamps to zero; span differences shift right4; growth sizes and new limit shift demand left4; unsigned byte-size-at-most128 selects the small allocation helper. For selected raw16 and growth-needed path, bytes256 selects the large helper. **Strongly inferred:** three-pointer reserve/growth structure and internal-record ownership from paired allocation/release. **Unknown:** hard maximum/ring/read-write roles, concrete class/RTTI, entry fields/count invariants/payload ownership, callback/cancellation and deep allocator/error/unwind behavior. Raw16 is a demand, not proof of current count or maximum.

First record's+8 initialization delegates to `00AA67C0`; second has three direct zero DWORD stores. Constructor has explicit record-null branches but later record dereferences without fresh guards. Zero/negative demand skips growth yet still requires record state. Positive size multiplication can wrap; no local rejection policy is claimed. Cleanup calls `00AA6370(queue,record,1)` A then B before null-guarded record helper/release calls. Flag1 is not labeled deletion/cancellation without its body. Outer queue release remains the frozen56 caller boundary; full scene/current-pointer lifetime is unclosed.

## Budget and reproducible checks

Exactly two primary bodies; no new tiny forwarder/accessor, callee body or external xref probe. **23 direct call sites inside these bodies, ten distinct unexpanded callee leads.** Candidate cap20 is applied to distinct leads; call sites retain their separate count. This is an explicit local inventory, not a reduced call count or broad caller census. No lead is expanded.

[Verifier](../../../SWG-Godot-Client/tools/python/verify_object_controller_queue_initialization.py) requires `--exe`, `--packages`, fresh ignored `--receipt`. [Independent runner](../../../SWG-Godot-Client/poc/round57-tests/Networking/research_checks.py) requires `--exe`, `--packages`, fresh ignored `--output-dir`. SHA precedes VA use. Positives: 80 anchors/two full signatures/exits/ten leads/23 call sites. Independent: 23 bytes/13 calls, receiver slot identity, five fields/aliases, clamp/threshold/cleanup branches, eight local arithmetic cases and cleanup flag/clear patterns. Twenty-one negatives include identity/evidence/lead/return/alias/clamp/threshold/flag/clear/truncation/unmapped/output/reparse/link/cap errors. Fresh nested output and rejected output-state preservation pass. Synthetic local arithmetic is not original queue execution. Cancellation is not applicable to these synchronous tools; queue cancellation meaning is Unknown.

No positive verification gate failed. Final validation covers organized workspace/links, UTF-8/LF/Python syntax, offline locked Core/SmokeTests restore/build zero warnings/errors, whitespace, all 3795 frozen hashes, unchanged HEAD/empty index and four assigned additions only. Exact final hashes/commands/results reside in the ignored handoff. Unrun: delegated helpers, concrete queue class/entry ownership, complete allocation/error/cancellation/unwind and scene lifetime, original process/live/physical profile, unrelated smoke runtime replay. No publication/staging/native/registry/shared-status edits.

## Exact next probe

Inspect only `00AA67C0` (initializer `00AA60A6`, receiver record A+8, local-byte address and zero) and `00AA6370` (cleanup `00AA6319/00AA6326`, queue receiver, current record pointer, raw1). Close record limit-like initialization and per-record cleanup/ownership flag semantics before extending to `00AA6710` or growth helpers. Lead owns shared canonical integration; stop idle after final handoff.
