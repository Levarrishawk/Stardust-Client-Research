# Object-controller dispatch payload source

Date: 2026-10-05 Eastern; UTC validation crosses into October 6. Round 62 verifies the documented `00462F60` dispatch body and stops at its incoming message field source. The selected route loads one DWORD at incoming argument+38 and pushes it as argument three to `00B3D250`. This closes the source chain to the accepted entry+8 producer; it does not identify payload construction, concrete RTTI/vtable, retained/transfer/unique ownership or actual deletion.

This extends the [selected caller contract](OBJECT_CONTROLLER_QUEUE_PAYLOAD_CALLER_CONTRACT.md), [entry producer contract](OBJECT_CONTROLLER_QUEUE_ENTRY_PRODUCER_CONTRACT.md), and [protocol specification](OBJECT_CONTROLLER_PROTOCOL.md). The latter's ObjControllerMessage/parser interpretation is reused durable evidence. This body does not inspect RTTI of the incoming message or expand the factory.

## Input, scope and reproducibility

Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses are VAs. The [verifier](../../SWG-Godot-Client/tools/python/verify_object_controller_dispatch_payload_source.py) rejects another executable before analysis imports/VA reads, then decodes the complete selected span. The [independent runner](../../SWG-Godot-Client/poc/round62-tests/Networking/research_checks.py) separately maps raw PE sections and checks bytes, direct targets, stack/register identities, object/payload sources, masks and branches.

| Body | Bytes / instructions | Exit | SHA-256 |
| --- | --- | --- | --- |
| `00462F60` | 280 / 102 | `00463075`, `ret 4` | `2f672b9e0a39fd81e6df0ce425aebdc17db1cc0874fc1043a9b0d2b6a245b949` |

One primary body, zero tiny forwarders and no external xref search. Its complete call inventory has 12 actual sites: six direct calls to five distinct targets and six indirect sites grouped into four unresolved source/slot leads. The repeated root-object slot+44 expression has three actual sites; grouping it does not prove equal dynamic targets across calls. The separately enforced site ceiling is 12; no additional raw xref candidates are collected.

The conservative combined lead union is also 12: selected primary, five direct targets, four indirect source/slot leads, registered handler reference `0141A7D8`, and unresolved cast target descriptor `0186D06C`. Controller/NetworkController descriptor facts at `0186AE78`/`0186D08C` are accepted [controller RTTI evidence](OBJECT_CONTROLLER_QUEUE_STORAGE_CONTRACT.md), reused without new descriptor reads. No helper, cast library, indirect target, handler, descriptor string, payload class, factory or corpus is expanded. This is the stopping budget, not a class-survey allowance.

## Exact payload and argument provenance

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| `00462F61` preserves the post-push entry stack anchor in EBX; `00462F91` reads `[ebx+8]` into EDI. The function later restores its aligned frame and returns with adjustment 4. | Confirmed one four-byte stack input and carrier identity. Incoming ECX is not the source of the message. | Interpret +38 relative to the incoming argument retained in EDI, not the controller or a new stack-frame assumption. |
| Message accessor `00463080`, lookup `00B380E0` and temporary helper `00AA4940` run before lookup-result testing. Root-object methods may run before its current +2C controller field is read. | Call/field flow confirmed; helper roles are contextual inferences, internals Unknown. | No root/controller lifetime or stable callback-return object is established. |
| `0046300B` is followed by `00463010` saving the first cast-shaped result in ESI. The second cast-shaped result at `00463028` is tested nonzero; successful dispatch still uses ESI at `0046304D`. | Confirmed result identity and two gates. Cast interpretation strongly inferred from the RTTI arguments; unresolved target type remains Unknown. | The receiver is the first gate's result, not an adjusted second-result pointer. |
| `0046303A` loads `EDX = DWORD[EDI+38]`; `00463047` pushes EDX before the scalar and operation pushes; `0046304F` calls `00B3D250`. | Confirmed width 4, storage source and argument three. Polymorphic payload-pointer role strongly inferred through the fallback and accepted cleanup. | Preserve this value's logical identity. Concrete payload class and lifetime owner remain Unknown. |
| `00463034/40/43` read +40, clear bit zero and set bit two. `00463046` pushes the normalized word. | Confirmed U32 bit transformation `(word & FFFFFFFE) | 4`. Flag meanings are prior protocol interpretation. | This becomes argument four; do not mistake its duplicate scalar placeholder for the payload. |
| `00463037` loads F32 at +64 with x87; `00463048` pushes a placeholder; `00463049` stores the x87 value to DWORD[ESP]. `0046303D/4C` load/push +48 as argument one. | Confirmed scalar source, F32 stack store and overwrite order. | Final push order is flags, payload, scalar, operation. FPU/NaN exception or raw-bit behavior is not execution-verified. |
| The accepted caller forwards its incoming argument three unchanged, and the accepted producer stores it at entry+8 on spare/growth paths. | Reused Confirmed chain. | Current incoming message+38 → caller argument three → producer argument three → entry+8. No construction is added by these forwarding layers. |

Width and field storage carrier are Confirmed. The semantic class name of that carrier is reused from the protocol/parser record; this selected body does not independently recover its type. The nested object's construction/type/lifetime owner are Unknown. +38 is read after helper/controller gates; its value is not proven invariant across earlier delegated calls.

## Controller eligibility, callbacks and fallback

Lookup returning zero, first cast-shaped gate returning zero, or second gate returning zero branches to `00463056`. Eligibility uses full DWORD tests, not an AL predicate. Earlier root-object calls use slot+44 (three sites), a slot+44 result's slot+80, and root slot+3C. They are different source expressions from the nested payload; pointer-value aliasing is Unknown. Later slot+44 results are not each independently null-guarded, and repeated results need not be identical. Their lifecycle/thread/reentrancy behavior is unexpanded.

At `00463056`, the dispatcher rereads DWORD[EDI+38]. Zero skips cleanup invocation. Nonzero becomes ECX; `0046305D` loads its first word, `0046305F` pushes 1, and `00463061` calls slot zero through that word. The invocation, receiver, slot and argument are Confirmed. Disposal/deleting-destructor shape is Strongly inferred; concrete target, class and actual free policy remain Unknown. Earlier durable policy calls this destruction; this probe pins the call shape without expanding the target.

No direct +38 clear/write occurs after this fallback call or on the eligible route. Delegated changes to that field are Unknown. On the eligible route there is no local payload-null guard: zero can be forwarded to `00B3D250`. After that call returns, `00463054` jumps past fallback cleanup without examining an insertion result. Therefore reaching the queue caller does not prove successful insertion or unconditional transfer, including the accepted producer's zero-operation early-return case.

The fallback payload slot-zero call is separate from the dispatcher's root-object callbacks and the accepted caller's receiver+8 calls. None supplies a concrete payload vtable address or RTTI chain in this round. Duplicated references, retained versus transferred ownership, eventual message teardown and cleanup after partial failure remain unresolved.

## Normal-return and exception limits

The selected body registers handler `0141A7D8`, tracks a temporary's state, and restores the exception frame on ordinary return. The handler and delegates are unexpanded; guaranteed cleanup, rollback or exception suppression is not established. An accessor/lookup/temporary helper/object callback/cast or queue call that does not return normally prevents the later normal-path operations. A fallback virtual call that does not return normally likewise does not reach the ordinary epilogue.

There is no explicit cancellation test. Thread affinity, dispatch phase legality and runtime occurrence are not proved by this body; inbound meaning is reused from the protocol record. EAX is path/delegate-dependent, with no established payload/controller/result return contract. No original execution, live server, packet capture, fuzzing or mutation is used.

## Validation and next bounded source probe

Verifier CLI: explicit `--exe`, `--packages`, fresh ignored `--receipt`. Independent runner: same inputs and fresh ignored `--output-dir`. Full-body/anchor/call inventories, raw PE bytes/calls/branches, message/first-cast identity, x87 stack overwrite, full-width gates and payload fallback are tested. Negatives cover identity/missing/truncation/unmapped spans, wrong fields/registers/receiver/stack adjustment/normalization/branches/slots, mistaken object/payload sources, existing/public outputs, lexical link/reparse ancestors before resolution and both 12/13 lead/site boundaries.

Synthetic flag, finite binary32 representation, cast-receiver and eligible/fallback vectors are independently authored. They execute no original target or instruction; eligible means the caller is invoked, not proven queue publication. Finite representation fixtures do not verify original x87 control state, NaN preservation, denormal handling or exceptions. Actual results appear in the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-05_R62_DISPATCH_PAYLOAD_SOURCE.md).

The precise stop is incoming message+38. Next: verify the documented factory-return writer around `00AC97A4` within parser `00AC9660`, including its field/lifetime policy, while treating `00B0D190` factory/per-type construction as a separate later probe. Do not infer per-type RTTI or deletion from these forwarding and disposal-shaped calls.
