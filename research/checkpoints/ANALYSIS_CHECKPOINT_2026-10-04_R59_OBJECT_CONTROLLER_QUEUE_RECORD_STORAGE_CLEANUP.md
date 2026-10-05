# Round 59: record backing-storage cleanup

Date: 2026-10-04. Question: does `00AA6710` release/reset the record or its backing storage, and in what order? Stopping condition: complete this one primary body and at most one necessary tiny direct wrapper, with no allocator/library/indirect/handler expansion.

## Inputs, evidence and findings

The [durable contract](../../docs/OBJECT_CONTROLLER_QUEUE_RECORD_STORAGE_CLEANUP_CONTRACT.md) records baseline executable identity, body hashes/extents, exits, exact field/register/arithmetic/branch/call observations, confidence, failure limits and compatibility consequences. The [verifier](../../tools/python/verify_object_controller_queue_record_storage_cleanup.py) and [independent runner](../../poc/round59-tests/Networking/research_checks.py) preserve the reproducible gates. Prior accepted 57/58 facts are reused, with four outer caller edges checked independently. No original execution, live contact, packet capture, fuzzing, entry-producer/vtable survey or registry/production change occurred.

Confirmed: reads record+0/+4/+8; captured begin/end traversal at stride 16; tiny `00AA6040` clears only entry+8 and preserves loop registers; no direct descriptor field reset; after traversal, a null begin skips release; non-null backing release routes by unsigned computed-byte threshold 128. Strongly inferred: limit/capacity field and backing-storage release role. Unknown: release delegates' exact allocator/free behavior, concrete RTTI/unique ownership, exceptional handler recovery and actual payload deletion. Empty used ranges with non-null backing still take a release route. No local null-record/malformed-range/cancel guard exists.

One primary body (129 bytes/45 instructions) plus one tiny direct wrapper (8 bytes/two instructions), 36 selected anchors, three internal direct call sites/three distinct targets, zero indirect sites, two unexpanded direct callee leads under cap 12 and one unexpanded pushed handler reference. No broad xref or corpus search. The wrapper was necessary to prove loop register preservation and absence of an additional payload callback; its single store is the complete body.

## Snapshot and scope

Incoming round 59: 3,872 files; manifest SHA-256 `a21d1d531c1cbd5d34d6acd9abed367b1b0d669153d2cbc6df24a1b86fe2c070`; ZIP SHA-256 `1c235f4e30ec369056db120e6adc6eaf0eef9b83a66cd7dbaaa17e7a8ba913c6`; source HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. Prior frozen 58 manifest and all four actual final worker hashes were crosschecked against the lead's integration receipt before writes. Archive/member/name/case/hash/size/root/lexical ancestor/link/reparse/containment/destination preflight passed; all 3,872 postflight hashes passed; HEAD/index/unrelated bytes were preserved. Four assigned additions only; shared integration remains the lead's responsibility.

## Validation

Initial independent gate passed 36 bytes/seven calls/four branches/eight arithmetic cases and 25 negative cases. Its synthetic event/ownership representation and malformed-range rejection are explicitly harness policy; actual deletion and exception recovery are unrun. Workspace validator and offline locked restore/build passed; build reported zero warnings/errors. AST/UTF-8/LF/whitespace, all 3,872 frozen hashes, four assigned additions and unchanged HEAD/empty index passed. Final independent replay adds four outer receiver/pushed-descriptor witnesses and two call-inventory rejections (27 negative cases total). Relative links and the final file hashes are checked in final review. Smoke executable replay was not run because no native production code changed. An initial PowerShell wildcard argument to rg was invalid; the corrected `-g` search succeeded, without affecting evidence or public files.

## Exact resume point

Bound one entry+8 producer and connect its stored pointer to a genuine payload vtable before implementing ownership/deletion. Do not expand it in this round. The release delegates and registered exception handler are separately unresolved. No completion percentages, canonical address names, packet rows or function labels change.
