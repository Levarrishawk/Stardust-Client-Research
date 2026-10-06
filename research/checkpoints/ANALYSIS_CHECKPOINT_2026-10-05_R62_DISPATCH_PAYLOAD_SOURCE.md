# Round 62: dispatch payload source

Date: 2026-10-05 Eastern; UTC phases cross into October6. One renewed authorized round, then idle/no63.

Question: does documented `00462F60` load incoming message+38 and supply it as argument three to `00B3D250`, and what are its branch/ABI/lifecycle boundaries? Stop at message field source; factory/per-type construction is later.

## Evidence and conclusions

The [durable contract](../../docs/OBJECT_CONTROLLER_DISPATCH_PAYLOAD_SOURCE_CONTRACT.md) records baseline SHA-256, complete 280-byte/102-instruction body hash and `ret4` exit, exact input/field/stack/receiver/branch/call observations, confidence and consequences. The [verifier](../../tools/python/verify_object_controller_dispatch_payload_source.py) and [independent runner](../../poc/round62-tests/Networking/research_checks.py) are reproducible gates. Existing caller/producer/RTTI/protocol facts are reused; their full audits are not repeated.

Confirmed: EDI is the single stack-input carrier; DWORD+38 loads into EDX and is pushed as argument three. The call receiver is the saved first cast-shaped result, with the second result used as a gate. Flags clear bit0/set bit2; scalar uses x87 F32 load/store into the second-argument placeholder. Missing eligible target takes non-null +38 first-word slot0 invocation with argument1; no direct +38 clear exists. The eligible route skips fallback after the queue caller returns, without verifying insertion success. Strongly inferred: payload pointer and fallback disposal shape. Unknown: concrete payload vtable/type/owner/deletion, field stability across delegates, callbacks/reentrancy/exception/thread/phase and message teardown.

One primary body, zero tiny forwarders, 45 anchors. Twelve actual internal call sites (six direct/six indirect), five direct targets, four indirect source/slot leads. Conservative total12 includes primary, handler and unresolved cast descriptor; known Controller/NetworkController descriptors are reused without new reads. No external xref query, class/corpus scan, helper/library/handler/descriptor-string/virtual-body or factory expansion. Twelve-site and twelve-lead caps are separately enforced.

## Frozen input and scope

Incoming62: 3,984 files; manifest SHA-256 `5cddfb9820610ab9b3fbc226afd862960afc00492b07b2b973c0d44aac85e077`; ZIP SHA-256 `695d6f22d668be5ece3f34bf98e78931053a3c4971bbeeebaf9e677a7a6020ff`. Every actual own61 final file hash was crosschecked with lead integration before writes; previous frozen61 manifest was pinned. Full archive/member/name/case/hash/size/lexical ancestor/link/reparse/root/containment/destination preflight passed, then all3,984 postflight hashes passed. HEAD/index/private/unrelated bytes preserved. Three assigned new files plus own62 runner only; all existing files frozen. Lead owns shared integration.

## Validation and phases

Initial gate passed 30 bytes/six direct calls/six branches and28 negatives. Refined gate adds root-object source/virtual-slot/full-width-cast witnesses and rejects three confusion cases:44 bytes, six direct calls, six branches,31 negatives. Six flags, six finite binary32 representation, six eligibility/fallback and five first-cast receiver vectors are synthetic/static evidence; no original pointer or code is executed. Both site/lead cap12 positive and13 rejection checks pass. Offline locked restore/build passed with zero warnings/errors; final replay retains44 byte witnesses/six direct calls/six branches and31 negative checks plus all synthetic flag/scalar/gate vectors. Workspace/link/UTF-8/LF/AST/whitespace/frozen-scope/HEAD/index gates run after this result update. Default native smoke execution is unrun because no native production code changed; focused static/synthetic gates verify this milestone.

Dispatch2026-10-06 01:25:26UTC; worker start01:27:00UTC. Overlay/body/verification/completion timestamps are retained in final handoff. Original executable/payload/callback/factory execution, live contact/capture/fuzzing, runtime occurrence, exact NaN/x87 exception behavior and concrete lifetime ownership remain unrun/unresolved. No native production or canonical registry/status change occurs.

## Exact next probe

Verify the documented factory-return +38 writer around `00AC97A4` in parser `00AC9660`, with field/lifetime policy. Factory `00B0D190` and per-type construction remain separate later questions. Finish this handoff and idle; no63.
