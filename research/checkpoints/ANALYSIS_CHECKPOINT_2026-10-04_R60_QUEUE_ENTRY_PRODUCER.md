# Round 60: one queue entry producer

Date: 2026-10-04. Question: what is stored at entry+8 by one insertion path, and is there direct payload construction/vtable evidence? Stop after one primary producer and one necessary tiny constructor, under 12 distinct combined leads.

## Input, evidence, confidence and consequence

The [durable contract](../../docs/OBJECT_CONTROLLER_QUEUE_ENTRY_PRODUCER_CONTRACT.md) records baseline SHA-256, complete body hashes/extents/exits, ABI, exact receiver/argument/field/arithmetic/call/branch observations and failure/ownership limits. The [verifier](../../../SWG-Godot-Client/tools/python/verify_object_controller_queue_entry_producer.py) and [independent runner](../../../SWG-Godot-Client/poc/round60-tests/Networking/research_checks.py) are the reproducible consumer gates.

Confirmed: `00AA6480` selects the current queue+0C record and stores its third stack argument at entry+8 on spare/growth insertion paths. All four entry DWORDs come from the four arguments. Growth copies the value, clears old entry+8 fields, calls the old-storage delegate, then publishes end/limit/begin. Optional queue+10 callback uses slot offset 4 after publication. Strongly inferred: transfer-shaped relocation and payload-pointer role. Unknown: payload construction/class/genuine vtable, actual insertion caller/occurrence, unique ownership/deletion and exceptional/callback policy. The callback receiver is loaded independently from queue+10; equality with the payload argument remains Unknown.

Tiny `00AA6030` zeros a local four-DWORD entry, not a payload object. One exact call-target xref found `00AA64CA`; nearest containing inventory owner `00AA6480` was validated by full decoding and exact instruction boundary. Initial first-containing lookup incorrectly picked overlapping broad range `007934F0`; no such body was decoded. Correction is preserved as a counted discarded lead. An initial PowerShell rg path wildcard was invalid; corrected explicit paths were used. Neither issue changed public evidence conclusions.

One primary body (448 bytes/159 instructions), one tiny constructor (16 bytes/seven instructions), 73 selected anchors. Eight internal actual call sites (seven direct/one indirect), six distinct direct targets. Conservative total 10 distinct leads under cap 12, including discarded owner, reused clear helper and unresolved handler. Five callee targets and the handler remain unexpanded. One exact-target xref search/one raw candidate call site; no broad class/corpus scan, payload caller body or arbitrary vtable slot expansion.

## Frozen scope and overlay

Round 60 snapshot: 3,914 files; manifest SHA-256 `599da255f204f74bd6f7d1a1cfb813c8987aea5f365d0453a447e26485f0be8c`; ZIP SHA-256 `addeb02195e6d123bd3c014014ef1d5afac8dec4c0884f24e8a041b347e672cc`; source HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. Prior frozen59 manifest and every actual final59 reviewed hash were crosschecked with the lead receipt. Complete archive/member/name/case/hash/size/root/lexical ancestor/link/reparse/containment/destination preflight preceded writes; all 3,914 postflight hashes passed. HEAD/index/private/unrelated bytes preserved. Only three assigned additions plus new own60 runner; existing files remain frozen and shared integration belongs to the lead.

## Validation

Initial independent replay passed 40 byte witnesses, seven relative calls, five branches, seven arithmetic cases and 24 negatives. Synthetic field fixtures include null/non-null payload values and duplicate representability; they do not execute pointers, enforce original ownership or prove allocator/exception behavior. Workspace validator, offline locked restore/build and initial scope passed; build reported zero warnings/errors. Final independent replay adds an exact-target xref expectation rejection (25 negatives total). Final relative Markdown links, UTF-8/LF/AST/whitespace, all 3,914 frozen hashes, four-addition scope and HEAD/empty-index preservation are checked after these final edits. Smoke replay was not run because no native production code changed. Original executable, payload/callback/handler/allocator execution, live contact, captures, fuzzing and runtime insertion occurrence remain unrun.

## Exact next action

Bound an exact-call xref search for `00AA6480`, select one actual caller and follow only its third argument to direct construction/vtable evidence. Stop at unresolved forwarding/allocation if genuine payload RTTI cannot be proved within that next budget. Keep ownership/deletion Unknown meanwhile. No metrics, provisional names, packet rows, production implementation or shared artifacts change.
