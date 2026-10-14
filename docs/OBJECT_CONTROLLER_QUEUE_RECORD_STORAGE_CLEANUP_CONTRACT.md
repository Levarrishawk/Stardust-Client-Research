# Object-controller record backing-storage cleanup

Round 59, 2026-10-04. Bounded question: which record fields does `00AA6710` read, what does it clear or release, and how does that operation precede descriptor release? Stop at its two release delegates and registered exception-handler target. One primary body and one necessary tiny direct entry wrapper are inspected. This extends the [record payload cleanup contract](OBJECT_CONTROLLER_QUEUE_RECORD_CLEANUP_CONTRACT.md) and [initializer/outer cleanup contract](OBJECT_CONTROLLER_QUEUE_INITIALIZATION_CONTRACT.md).

## Evidence identity and method

Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; addresses are VAs for this baseline. The static verifier (external historical reference) checks identity before importing pefile/Capstone or using addresses. The independent runner (external historical reference) uses its own raw PE section mapping, selected bytes, branch displacements, caller edges, field/register witnesses and integer arithmetic fixtures.

| Body | Extent | Instructions | SHA-256 |
| --- | --- | --- | --- |
| `00AA6710` | 129 bytes through `00AA6790` | 45 | `203197aa56cd00dc435839a3bf228ee3bd4e1481517b89d13d8c9972ff3c2909` |
| Tiny wrapper `00AA6040` | 8 bytes through `00AA6047` | 2 | `35647cd18c19f4482ebc2ce05d5e7caabe8ffc5b5c2147adc62a917b4a9fc6b1` |

Complete decoding reaches both primary return instructions (`00AA6778`, `00AA6790`) and the tiny wrapper return. There are three actual direct call sites, three distinct direct targets, zero indirect calls and zero external xref searches. The wrapper is expanded; `00AC1640` and `012EA920` remain two unexpanded callee leads within the 12-lead cap. The pushed exception handler `014BA44B` is observed as a separate unresolved handler reference, not an additional call site. No allocator/library/handler body, entry producer or payload vtable is expanded.

## Observations, confidence and compatibility consequences

| Observation | Interpretation / confidence | Consequence |
| --- | --- | --- |
| `00AA6729` copies incoming ECX to EDX; `00AA6732` saves EDX to `[ebp-10]`. `00AA672B/2E` read record+4 into EAX and record+0 into ECX. | Confirmed receiver identity and raw offsets; start/end roles strongly inferred from accepted 57/58 arithmetic and loops. | Do not confuse entry ECX with descriptor EDX. |
| `00AA6730/35` skip traversal if begin equals end; `00AA6737` calls `00AA6040`; `00AA673C` adds 16; `00AA673F/41` loop until ECX equals captured EAX end. | Confirmed stride/equality iteration. | No count/range/alignment check or end refresh is observed. |
| `00AA6040` only stores zero at `[ecx+8]`; `00AA6047` returns. No call, stack argument or other register modification. | Confirmed pointer-field clearing with ECX/EAX/EDX loop values preserved. | This pass adds no virtual payload disposal or callback. It repeats zeroing after accepted 58 payload cleanup on the ordinary valid sequence. |
| `00AA6743` reloads record+0 after iteration; `00AA6745/4E` skip storage release if it is null. | Confirmed storage-phase null behavior. | This is not a null-record guard, and a null begin does not protect an earlier nonempty invalid range from entry writes. |
| `00AA6750/53` read record+8 and subtract begin. `00AA6755/58` arithmetic-shift right 4 then left 4. | Confirmed 32-bit arithmetic; limit/capacity role strongly inferred from reserve behavior. | Computed bytes are `(signed32(limit-begin) >> 4) << 4`, modulo 32 bits. For a valid aligned allocation this is its span. No malformed-span rejection is established. |
| `00AA675B/60` compare against 128 and take unsigned `jbe` to the small route. Large route pushes begin and calls `00AC1640` at `00AA6763`; small route pushes bytes then begin and calls `012EA920` at `00AA677B`. Callers restore 4/8 stack bytes respectively. | Route/argument order confirmed. Backing storage release strongly inferred from accepted allocation/release context; actual delegate behavior Unknown. | Separate the backing pointer from the descriptor pointer; small route receives pointer and computed size, large route receives pointer only. |
| No direct descriptor+0/+4/+8 write or reset occurs. The primary returns with bare `ret`; EAX initially holds end, later computed size or delegate return. | Confirmed direct effects and ABI; stable returned receiver identity is not established. | Do not reuse this as a clear-and-reuse API or infer a returned record pointer. |
| An exception frame with handler reference `014BA44B` is registered/restored; state `[ebp-4]` is set to zero before storage null/route handling. | Confirmed registration/state writes. Handler behavior Unknown. | Do not claim no exceptions, guaranteed recovery, or handler cleanup from the normal-return instructions. |

All direct record reads are +0/+4/+8; direct record writes are absent. Entry+8 clearing occurs before either backing-release delegate. There is no local cancellation argument, test or callback in the inspected bodies. Delegate behavior remains unresolved. Empty begin=end skips entry writes, but a non-null begin still proceeds to storage release. A null descriptor has no local rejection path.

## Three separate cleanup layers

Accepted round 58 `00AA6370` calls non-null entry+8 payload virtual slot zero with internal argument 1, then clears that pointer after normal return. This round's `00AA6710` repeats entry+8 zero stores, then routes the record's backing pointer to a release delegate. Accepted round 57 outer cleanup calls `00AA6710` at `00AA6333/00AA634A`, then separately pushes the descriptor pointer and calls `00AC1640` at `00AA6339/00AA6350` on normal return. It processes A before B. The independent runner checks those four caller edges and their receiver/pushed descriptor-register bytes without repeating the whole prior audit.

Backing storage release and descriptor-object release are strongly inferred roles; exact delegate free policy, allocator families, concrete class RTTI and unique ownership remain Unknown. The entry wrapper has no payload call, so this round supplies no new proof of actual payload deletion. No original callback, allocator, handler or executable was run, and no real corpus occurrence was established within this static probe.

## Normal-return and failure limits

The entry wrapper has no explicit throw or call to another routine; invalid memory stores may still fault. If traversal faults partway through, earlier visited entry+8 fields have been zeroed, and backing release has not yet been reached. If a storage delegate does not return normally, the subsequent outer descriptor-release call is not reached by the observed normal-return path. Registered handler recovery, delegated mutation and exceptional unwinding effects remain Unknown. On ordinary return there is no direct descriptor reset; the descriptor immediately proceeds toward release in the accepted outer caller. Retrying or reusing the unchanged bounds requires an external lifetime policy and is not established as safe.

## Validation and exact next prerequisite

Reproduce the verifier with explicit `--exe`, `--packages`, fresh ignored `--receipt`; reproduce the runner with the same inputs and fresh ignored `--output-dir`. Identity, truncation/empty/unmapped spans, instruction expectations, field/register identities, signed shifts, unsigned route threshold, output containment/existing destinations/lexical symlink and reparse ancestors, and the 12-lead cap have negative checks. The runner's bounded synthetic event oracle preserves descriptor values and lists entry-clear addresses; its range rejection is a harness policy, not legacy rejection. It executes no original code. Arithmetic fixtures include zero, empty allocated, 16/128/144-byte, unaligned, reversed and wrapping spans; invalid spans demonstrate raw arithmetic only.

Results are recorded in the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R59_OBJECT_CONTROLLER_QUEUE_RECORD_STORAGE_CLEANUP.md). No production implementation or canonical registry is changed.

Next prerequisite: bound one entry+8 producer and connect its stored pointer to a genuine payload vtable before implementing payload ownership/deletion. Storage-release delegates and exception-handler internals are separate remaining questions. Preserve the established call-then-zero, entry-clear, backing-release, descriptor-release ordering without claiming the unresolved targets' behavior.
