# Object-controller parser payload writer

Date: 2026-10-05 Eastern; UTC validation crosses into October 6. Round 63 verifies canonical parser `00AC9660` and the factory-return writer after `00AC97A4`. Receiver+38 is directly initialized to zero, then receives the factory's EAX return at `00AC97AC`, including zero. Normal return is the same incoming receiver. Factory construction, per-type class/vtable, actual deletion, retained/transfer/unique ownership and exceptional cleanup remain Unknown.

This extends the [dispatch source contract](OBJECT_CONTROLLER_DISPATCH_PAYLOAD_SOURCE_CONTRACT.md) and [protocol specification](OBJECT_CONTROLLER_PROTOCOL.md). The already documented parser/factory names are reused, with no canonical rename. This routine has a construction-shaped base/member initialization sequence; it is not established as a reusable decode-into-existing-object API.

## Inputs, complete body and amended budget

Executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All addresses are baseline VAs. The [verifier](../../SWG-Godot-Client/tools/python/verify_object_controller_parser_payload_writer.py) checks identity before imports/VA reads and checks sealed accepted62 tool/document hashes before output. The [independent runner](../../SWG-Godot-Client/poc/round63-tests/Networking/research_checks.py) uses its own raw PE mapping, independently hashes the complete span, and checks semantic call/register/stack/write witnesses.

| Inspected body | Bytes / instructions | Normal exit | SHA-256 |
| --- | --- | --- | --- |
| `00AC9660` | 530 / 162 | `00AC986F`, `ret 4` | `a75e091a364e4153604d88a29fe948562f12430156b88d10abdfc2b1b247fc37` |

One complete primary and zero tiny helpers are expanded. The full body contains 20 direct call sites and 13 distinct direct targets; the verifier retains every site rather than truncating the inventory. The lead explicitly amended the original generic cap to **20 complete-body sites and 16 distinct total leads**, including primary and registered SEH reference, with no callee expansion. Actual total is 15: primary, all 13 direct targets, and handler `014BC29C`. There are no indirect call sites or external xref queries in this body/probe. The two reader/factory sites are semantic witnesses, not a replacement budget denominator. No callee, handler, per-type constructor, registry entry, arbitrary vtable or corpus is expanded.

## Exact observations and consequences

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| `00AC9680` saves incoming ECX in ESI. `00AC985D` copies ESI to EAX before restoring saved registers and `ret 4`. | Confirmed normal receiver/return identity and one stack argument. | Parser receiver and input archive are separate values; normal return is the receiver, not the nested payload or a boolean status. |
| `00AC9686` zeroes EBX; `00AC96E9` writes EBX to DWORD[ESI+38]. | Confirmed direct initialization write under normal calling-convention preservation. | +38 is zeroed before archive decoding/factory invocation. No local previous-payload read or direct disposal precedes this store. Reusable replacement safety is not proved. |
| Base/member helper calls, zero-valued wrapper fields and installed tables at receiver+0/+10 precede decoding. | Construction-shaped receiver initialization strongly inferred; current message semantic name comes from prior protocol evidence. | These receiver tables do not identify the nested payload's vtable/type. No nested construction is performed locally. |
| `00AC9794` loads `[ebp+8]` into EDI; `00AC9797/98/9A` pass it with receiver ESI to `0118B740`. | Confirmed archive-input carrier and reader boundary. Reader internals/malformed input behavior Unknown. | This probe proves the same input argument is forwarded; it does not recover archive consumption policy. |
| `00AC979F` loads DWORD[ESI+48] to EAX. `00AC97A2/A3` push EDI then EAX; `00AC97A4` calls `00B0D190`. | Confirmed logical stacked order: discriminant, same archive parameter. Discriminant width is 4 bytes; signed/key semantics remain a factory question. | Factory source is the current +48 field after reader normal return, without a local transform. |
| `00AC97A9` reloads +48 into EDX; `00AC97AC` writes EAX to DWORD[ESI+38]. | Confirmed factory-return identity; intervening EDX load leaves EAX untouched. | The factory's returned DWORD is stored verbatim. The subsequent +48 reload need not equal its pre-factory value if delegated code mutated it. |
| There is no explicit conditional jump/null-result test after the call. Later format/string/record helpers run before normal return. | Confirmed local straight-line normal flow; REP string scans are separate implicit loops. | A factory return of zero is retained and does not cause a local early rejection. Factory or later helpers may still throw or fail to return. |
| The only direct +38 references are the zero store and factory-result store. No later direct clear, read, retain or payload virtual call occurs. | Confirmed local effects; delegated effects Unknown. | The message field is the storage carrier. Payload lifetime owner, refcounting, transfer, duplicate handling and deletion cannot be derived from these stores. |
| `00AC97E5` balances 24 caller stack bytes after the later formatter call. | Confirmed caller stack instruction; it follows two factory pushes and four formatter pushes. | Factory argument cleanup is not immediate. Do not silently model the receiver or formatter arguments as extra factory arguments; factory internals remain unexpanded. |

The accepted dispatch/caller/producer chain reads this +38 field and forwards its value into entry+8. This round closes the local writer origin as the factory return. It does not prove that the returned object is newly allocated, uniquely owned, non-null for a supported type, or compatible with the later disposal-shaped virtual calls.

## Overwrite, failure and lifecycle limits

The raw sequence would overwrite any preexisting +38 value with zero without a local read/disposal. That observation is not proof of a leak in valid construction: fresh/uninitialized receiver storage is strongly suggested by the surrounding base/member initialization. No safe repeated parse/replacement contract, idempotence or old-value ownership is established.

The parser registers handler `014BC29C` and updates construction-state bytes. The handler is unexpanded. If an earlier reader/helper or the factory fails to return normally, the final factory-return store is not reached by the ordinary path. Zero is the last known local +38 write before the factory, but actual field/cleanup after delegated or handler effects remains Unknown. If a later helper fails after `00AC97AC`, the result store has already occurred; rollback, freeing and final field state are not proved.

There is no explicit local cancellation test, no direct nested payload destructor/free call, and no payload-result null branch. These local absences do not constrain the factory or handler. Thread affinity, archive error policy, phase/runtime occurrence and incoming receiver allocation owner remain Unknown. Genuine nested payload RTTI/vtable/type and retained/transfer/unique ownership stay outside this probe.

## Reproduce and next factory discriminant probe

Verifier: explicit `--exe`, `--packages`, fresh ignored `--receipt`. Runner: same inputs with fresh ignored `--output-dir`. Accepted62 hashes, SHA/span/full-decode/call/write inventories, raw PE/register/stack/return witnesses, synthetic factory arguments/null/non-null result and exception cutpoints are retained. Negative checks cover wrong identity/sealed inputs/truncation/unmapped spans, changed fields/registers/archive/discriminant/return, output containment/existing paths/lexical symlink/reparse rejection and amended site/lead limits. Positive full inventories pass; 21 sites, 17 leads and one-less actual limits (19 sites/14 leads) reject. Synthetic input validation is harness policy, not original archive rejection.

See the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-05_R63_PARSER_PAYLOAD_WRITER.md) for actual results and the scope amendment. Original execution, factory/handler/callback execution, live contact/capture/fuzzing and proprietary extraction remain unrun.

Next bounded question: inspect `00B0D190` using the first stacked argument sourced from +48. Determine whether its registry key is transformed/reinterpreted, what installed/absent-key gates return, and which archive argument is passed to the selected registry callback. Stop at that callback without a per-type registry/class/vtable survey. This is the next prerequisite before assigning concrete payload construction or lifetime ownership.
