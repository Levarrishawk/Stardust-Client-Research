# One queue payload caller and forwarding boundary

Date: 2026-10-05 Eastern (validation UTC timestamps cross into October 6). Round 61 asks: where does one actual exact caller of `00AA6480` obtain the third stack argument that becomes entry+8? Selected caller `00B3D250` forwards its own third incoming argument unchanged. No payload construction or vtable store occurs in this body. Genuine payload RTTI, retained/transfer/unique ownership and the source object's lifetime remain Unknown at this incoming forwarding boundary.

This extends the [round-60 producer contract](OBJECT_CONTROLLER_QUEUE_ENTRY_PRODUCER_CONTRACT.md). The [protocol specification](OBJECT_CONTROLLER_PROTOCOL.md) already names this controller message boundary, and the [controller storage contract](OBJECT_CONTROLLER_QUEUE_STORAGE_CONTRACT.md) establishes its family's stored queue/object fields. Those prior facts are reused; this round does not replay their entire proofs or infer a class solely from address proximity.

## Inputs, bounded selection and reproducibility

Executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All addresses are baseline VAs. The [verifier](../../SWG-Godot-Client/tools/python/verify_object_controller_queue_payload_caller.py) checks identity before package imports or VA interpretation, completely decodes the selected body, and repeats one exact call-target query in executable PE sections. The [independent runner](../../SWG-Godot-Client/poc/round61-tests/Networking/research_checks.py) separately maps raw PE sections and checks relative calls, stack/register/byte identities, branches and guards. No original code or pointer target is executed.

| Exact-target raw site | Nearest containing inventory candidate | Current inspection |
| --- | --- | --- |
| `00AA6651` | `00AA6640` | Unexpanded; prior empty-payload path exists in durable research. |
| `00AB3538` | `00AB33E0` | Unexpanded; other input-map branch is outside this round. |
| `00B3D269` | `00B3D250` | Selected; full decode proves an actual instruction boundary calling `00AA6480`. |

Three raw sites are below the separately enforced ceiling of 12. Only the selected candidate's body is expanded. Its queue-field forwarding relationship and existing controller-boundary documentation motivated selection. Inventory range membership alone is not proof of ownership; nearest-containing selection is followed by complete instruction-boundary validation, preserving the round-60 overlap lesson. No broad class/corpus scan or arbitrary vtable-body expansion occurs.

Selected `00B3D250`: 59 bytes, 27 instructions, one exit at `00B3D288` (`ret 16`), complete span SHA-256 `203326319dab0ffc8e614c5153b84b44340e22b4699b129d876758862eeb577b`. Exactly one primary caller and zero tiny wrappers are decoded. There are three actual internal call sites: one direct producer call and two indirect calls. Six conservative distinct combined leads fit cap 12: three xref owner candidates, accepted producer target, and two unresolved virtual slot/source leads. The two indirect targets could resolve to equal addresses; they are counted separately because target identity is Unknown. No new genuine payload RTTI/vtable chain is established.

## Observations, confidence and implementation consequences

| Observation | Interpretation / confidence | Consequence |
| --- | --- | --- |
| `00B3D253/256/25B` load arguments 4/2/1 from `[ebp+14]`, `[ebp+0C]`, `[ebp+8]`. `00B3D25E` preserves incoming receiver in ESI. | Confirmed incoming source identities. Raw widths are four bytes; prior semantic names are separate reused evidence. | Preserve the four values without reinterpreting raw bits in this adapter. |
| `00B3D260` loads `[ebp+10]` into ECX; `00B3D263` pushes ECX. Pushes at `00B3D25A/263/267/268` are arguments 4/3/2/1 respectively. | Confirmed third-argument provenance and right-to-left stack order. | Producer argument three equals this caller's incoming argument three. No construction, copy object, add-reference, null-payload test or vtable dereference is observed on that value. |
| `00B3D264` loads ECX from saved receiver+4; `00B3D269` directly calls `00AA6480`. | Confirmed selected queue source, consistent with accepted controller field identity. | The stored queue is forwarded as producer receiver. There is no local queue-null guard or first-argument guard in this caller. |
| Accepted round 60 stores producer argument three into entry+8 on both spare and growth routes. | Reused Confirmed field flow; pointer role strongly inferred through accepted payload cleanup. | This closes caller-to-entry identity, not payload origin or ownership. |
| `00B3D26E/271/273` reload receiver+8 and skip later calls when zero. Otherwise `00B3D275/277` load its first-word table and call slot+4. | Confirmed post-producer source and null branch. Actual target/class/meaning Unknown. | It is sourced from receiver+8, not from the payload argument. Pointer-value aliasing remains Unknown. |
| `00B3D27A/27C` test AL only and skip the second call on zero. `00B3D27E` rereads receiver+8; `00B3D281/283` call its first-word table at slot+3C. | Confirmed low-byte gate and reread. | A nonzero upper return word with AL zero still skips. There is no fresh null guard after the reread; mutation/reentrancy semantics remain Unknown. |
| `00B3D288` returns with 16-byte argument adjustment after restoring ESI/EBP. EAX may still reflect producer, predicate or second virtual result. | Confirmed ABI; stable returned receiver/payload/success value not established. | Do not treat EAX as a queue insertion success API or returned retained payload. |

There is no local allocation/free/refcount call or payload-member write in this caller. The producer's own allocations, callback and exception handler remain the accepted round-60 boundaries. No payload constructor or genuine payload vtable can be reached by backward dataflow inside this body: provenance stops at incoming `[ebp+10]`.

## Normal-return, failure and lifecycle limits

The direct producer must return normally before the receiver+8 operations are reached on the observed path. This caller does not test a producer success result. Consequently, the accepted producer's zero-first-argument diagnostic/early-return path can still be followed by these operations if it returns normally. Queue, insertion preconditions and diagnostic delegate policy remain external.

If the first receiver+8 virtual call returns AL nonzero, the member is reread before the second call. If that member changed, the second receiver can differ; no stable identity or retained reference is established. If it became null, the observed path has no second guard before dereferencing it. No mutation scenario or pointer target was executed. If any delegate does not return normally, subsequent instructions on this straight-line path are not reached; there is no local exception-frame registration/catch in the inspected body. Exceptional recovery elsewhere remains Unknown.

Retained versus transferred payload ownership, duplicate-pointer policy, deletion compatibility, callback aliasing, thread affinity and execution phase are Unknown. The earlier protocol document names a network dispatch path, but this body alone does not prove that path's runtime occurrence or exclude other callers/phases. No server contact or executable execution is used to establish reachability.

## Validation and exact next probe

Verifier CLI: explicit `--exe`, `--packages`, fresh ignored `--receipt`. Runner: same inputs and fresh ignored `--output-dir`. Static SHA/full-span/decode/anchor/xref gates and independently authored field/stack/receiver/call/branch witnesses are retained. Negative cases cover identity/missing/truncated/unmapped inputs, wrong argument/queue/callback fields, wrong registers/AL test/return adjustment/virtual slot/branches, existing/public outputs, lexical symlink/reparse ancestors before resolution, and both lead/raw-site budgets. Synthetic DWORD and low-byte predicate fixtures are harness-only; their input rejection is not a legacy validation policy. Raw-site boundary 12 and overflow 13 are exercised on an authored synthetic PE surface.

Actual checks and timing are recorded in the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-05_R61_QUEUE_PAYLOAD_CALLER.md). Existing files and registries remain frozen; no production behavior is implemented.

Next bounded probe: inspect the already documented upstream dispatcher `00462F60`, verify whether its nested message+38 load supplies argument three to `00B3D250`, and stop at that source boundary. The documented factory `00B0D190` is a separate later construction question; do not infer per-type payload RTTI or ownership from the dispatcher or arbitrary vtable slots.

## Round62 upstream source closure

[00462F60 dispatch](OBJECT_CONTROLLER_DISPATCH_PAYLOAD_SOURCE_CONTRACT.md) loads incoming message+38 into this caller argument3. The ineligible path invokes that non-null value through slot0 with1, separately from controller/object callbacks. This closes storage provenance, not construction/type/ownership/deletion. The parser field writer is the next bounded probe.
