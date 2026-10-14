# Object-controller record initialization and payload cleanup

Round 58, 2026-10-04. Bounded question: what does `00AA67C0` initialize, and what does `00AA6370` do to a record and its entries? The stopping condition is complete static inspection of these two bodies, without expanding their indirect target. This extends the [queue initialization contract](OBJECT_CONTROLLER_QUEUE_INITIALIZATION_CONTRACT.md); it does not replace its reserve and outer cleanup evidence.

## Input and reproducibility

Baseline `Stardust.exe` SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses are baseline VAs. The [verifier](../../SWG-Godot-Client/tools/python/verify_object_controller_queue_record_cleanup.py) rejects another executable before importing analysis packages or using addresses. It decodes only two full bodies with pefile/Capstone in 32-bit x86 mode. The [independent checks](../../SWG-Godot-Client/poc/round58-tests/Networking/research_checks.py) map PE sections manually and inspect selected bytes and the three accepted caller edges.

| Body | Bytes / instructions | SHA-256 |
| --- | --- | --- |
| `00AA67C0` | 14 / 7 | `8b18d6a4d23f077e4ae2893f88df892f999a47d113c918c38cad13cbb32e03a6` |
| `00AA6370` | 50 / 23 | `19e098e27ead67e78b06ab077d9d25b1c9489f68f229414657c55cea6df198c5` |

There are zero direct calls and one indirect call site, `00AA638C`, in these bodies. That site is one unresolved callee lead, within the 20-lead budget. No external xref search or additional callee-body inspection was performed. Neither body establishes genuine RTTI for the queue or payload class.

## Observations and consequences

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| `00AA67C3` copies incoming ECX to EAX; `00AA67C5` loads `[ebp+0C]`; `00AA67C8` stores that DWORD through EAX; `00AA67CB` returns with stack adjustment 8. | Confirmed: receiver returned, second stack argument stored, first ignored. | Combined with accepted caller `00AA60A6` (receiver record A+8, second argument zero), record A+8 starts at zero. The local-byte address argument has no effect in this helper. |
| `00AA6373`, `00AA6377`, `00AA637A` load the first stack argument and snapshot its DWORDs +0/+4 into ESI/EDI. | Confirmed raw structure; start/end roles strongly inferred with round 57 arithmetic. | Iterate the captured range; do not infer fresh descriptor reads after each callback. |
| `00AA637D/7F` skip the loop when the captured pointers are equal. | Confirmed empty-range behavior. | An empty range causes no entry or virtual-target access. A null record has no local guard. |
| `00AA6381` reads entry+8; `00AA6384/86` skip a null pointer. | Confirmed pointer field at offset 8. Other entry fields remain Unknown. | Preserve null-entry tolerance. There is no virtual call for an already-zero field. |
| `00AA6388` loads the payload's first DWORD; `00AA638A` pushes 1; `00AA638C` calls slot zero through it. | Invocation and argument confirmed. Payload disposal is strongly inferred from the destructor-shaped call and surrounding cleanup. Actual target/class/free policy Unknown. | The internal argument 1 is distinct from the caller's second argument. No concrete destructor or unique ownership claim is established. |
| `00AA638E` stores zero to entry+8 after the call. | Confirmed normal-return order. | If that call fails to return normally, this store has not occurred. Earlier completed entries have already been cleared; remaining entries have not been visited. |
| `00AA6395` adds 16; `00AA6398/9A` compare against captured end and repeat until equal. | Confirmed 16-byte stride and equality termination. | Require a valid finite range in a replacement's own representation. Legacy malformed-range rejection is not established. |
| The complete body never reads `[ebp+0C]`, never uses incoming queue ECX, and never writes record descriptor fields. It has no local exception handler or storage-release call. | Confirmed within this body. | Caller raw flag 1 does not establish cancellation. Pointer clearing does not reset the end, drain the descriptor, or free record storage. Delegated target effects remain Unknown. |

The accepted outer cleanup calls this operation for A and B, then separately calls `00AA6710` and `00AC1640` for each non-null current record. This round closes the selected operation's direct effects only. Queue receiver fields and record bounds could still change through the unresolved virtual target. Captured end and current entry are not refreshed by this body.

## Failure and ownership limits

This is static evidence, not execution-based verification. No original callback, executable, live server, capture, fuzzing, or asset corpus was executed. Real entry occurrences and payload vtables have not been established. A duplicated non-null payload pointer, callback mutation, invalid range, and retry after partial cleanup require ownership and lifecycle evidence; this helper supplies no local policy for them. Its EAX return is not a stable queue or record identity contract.

The independent runner includes synthetic normal-return event fixtures for caller values 0, 1 and 255, empty/null entries, and a scripted non-return cutpoint. These express the observed call-then-zero order only. The scripted failure does not verify the original target's exception ABI, deletion, cancellation, or cleanup recovery. Identity, spans, selected instructions, output-path guards and candidate budgets have rejection checks. Lexical symlink/reparse rejection is checked before path resolution with synthetic lstat attributes.

## Reproduce and next probe

Run the verifier with explicit `--exe`, `--packages`, and a fresh ignored `--receipt`; run the independent runner with the same inputs and fresh ignored `--output-dir`. Run `python tools/python/validate_organized_workspace.py .` and `git diff --check`. Exact validation results belong in the [round-58 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R58_OBJECT_CONTROLLER_QUEUE_RECORD_CLEANUP.md).

Next bounded probe: inspect `00AA6710` to determine which record/storage fields it releases or resets, then separately identify one entry+8 producer and its concrete payload vtable before claiming payload deletion or ownership. Do not infer a hard capacity, ring policy, or cancellation API from the ignored caller flag.
