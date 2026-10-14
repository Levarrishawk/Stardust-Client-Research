# Round 61: selected queue payload caller

Date: 2026-10-05 Eastern. UTC validation crosses into October 6. Renewed user authorization is for this one round only; the accepted round-60 stop remains historical and no round 62 is started.

Question and stopping condition: exact-call query for `00AA6480`, select one actual caller, trace third argument backward to construction/vtable evidence or the first unresolved forwarding boundary. One primary body and at most one tiny wrapper; cap 12 raw sites and 12 distinct combined leads.

## Evidence, confidence and compatibility consequence

The [durable contract](../../docs/OBJECT_CONTROLLER_QUEUE_PAYLOAD_CALLER_CONTRACT.md) records baseline identity, complete body hash/exits/ABI, exact stack/register/field/call/branch witnesses, interpretation/confidence and lifecycle limits. The [verifier](../../../SWG-Godot-Client/tools/python/verify_object_controller_queue_payload_caller.py) and [independent runner](../../../SWG-Godot-Client/poc/round61-tests/Networking/research_checks.py) supply reproducible gates.

Exact target query finds three raw sites. Selected `00B3D250` is a completely decoded 59-byte/27-instruction caller with one `ret 16` exit and three internal call sites (one direct, two indirect). Zero tiny wrappers are expanded. Six conservative combined leads fit cap 12. Other candidate bodies and indirect targets remain unexpanded. No class/corpus/vtable sweep occurs.

Confirmed: receiver+4 supplies the producer receiver; four incoming arguments are pushed 4/3/2/1 unchanged. Argument three comes from `[ebp+10]`, passes through ECX/push at `00B3D263`, and accepted60 stores it at entry+8. Receiver+8 supplies the later virtual slot+4 query; AL gates slot+3C on a reread member without a second null guard. Unknown: upstream third-argument construction/vtable, payload retention/transfer/unique/duplicate policy, pointer aliasing, delegate exceptions, reentrancy, thread and execution phase. No stable result API or new payload RTTI is claimed. The exact first unresolved boundary is incoming argument three of `00B3D250`.

## Frozen scope and synchronization

Incoming61: 3,949 files; manifest SHA-256 `e2616419cdb99eabee437e166e2be6e7446d5f7dc6a6a80efc05301b0b7f451a`; ZIP SHA-256 `19526efdb23ecb702420f11df0afdebccdc9defde2877be69fbbabb2fe2fda94`. Prior frozen60 manifest and all four actual own60 final hashes were crosschecked against both lead integration and verified-worker-hash receipts before writes. Full archive/member/name/case/hash/size/root/lexical ancestor/link/reparse/containment/destination preflight passed, then all 3,949 postflight hashes passed; HEAD/index/private/unrelated bytes preserved. Only the three assigned additions and new own61 runner are permitted. Shared integration remains with the lead.

## Validation and timing

Initial independent replay passed 22 byte witnesses, three exact-target calls, two branches, forwarding/receiver identity and 27 negative cases. Subsequent replay also checks the positive raw-site ceiling 12 and rejected 13, four synthetic DWORD vectors and six low-byte predicate vectors. These are static/synthetic checks, not original execution or payload ownership verification. Offline locked restore and build passed with zero warnings/errors. Final research replay retained all 27 negative cases, both raw-site cap boundaries, four DWORD vectors and six low-byte predicate vectors. Final workspace/link/UTF-8/LF/AST/whitespace/frozen-scope/HEAD/index gates are checked after this result update. Default native smoke execution was not run: no native production code changed, and the focused static/synthetic runner verifies this milestone.

Dispatch: 2026-10-06 00:50:40 UTC; worker start: 00:51:05 UTC. Phase timestamps and final completion/elapsed time are retained in the final handoff. Scope comparison: accepted60 inspected 159 primary plus seven tiny instructions; this round inspects 27 primary and no tiny instructions. Accepted60 whole lead round ran 18:08:45 to19:00:17 UTC on October4 (51m32s), including all workers and assembled lead gates. Its standardized individual worker phase timing was not recorded, so that whole-round duration is not directly comparable to this worker-only elapsed measurement. No causal model-performance claim is made from the timing difference.

Original executable/payload/callback execution, live contact/capture/fuzzing, runtime occurrence and concrete payload construction remain unrun/unresolved. No native production implementation, shared status, registry or frozen file changes occur.

## Exact next action

Inspect documented upstream `00462F60`; verify whether its nested message+38 source becomes the third argument of `00B3D250`. Stop at that source boundary; `00B0D190` factory/per-type construction and ownership require a separate bounded probe. Idle after this final handoff.
