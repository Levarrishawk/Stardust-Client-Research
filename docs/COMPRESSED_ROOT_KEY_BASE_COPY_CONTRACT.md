# CKAT key-base copy boundary

Key-base constructor00AA3FE0 explicitly initializes destination key fields at+0,+8 and+C and invokes delegates to obtain/copy source key material. It contains no local write to source+10 or destination scalar+10. This narrows the [prior scalar-copy uncertainty](COMPRESSED_ROOT_SCALE_VALUE_CONSTRUCTION_CONTRACT.md), but does not prove complete source-zero preservation: its initializer, source getter and destination setter remain unopened, and their side effects must remain Unknown.

## Exact observations and confidence

| Observation | Interpretation | Confidence | Compatibility consequence |
| --- | --- | --- | --- |
|00AA3FFA retains ECX in ESI;00AA4000 calls00AA2D70 on that destination;00AA4005 loads source argument into EDI | Destination key initialization followed by source processing | Confirmed call/order/operands; initializer behavior Unknown | No source pointer is explicitly passed to the first initializer; nonlocal effects are still unproved |
|00AA4008 zeros EAX;00AA400F/4012 store dwords at destination+8/+C;00AA4015 installs015EA5B8 at+0 | Key state locally initialized with two zero fields and vtable | Confirmed offsets/widths; field semantics Unknown | These stores do not initialize or overwrite scalar payload+10 |
|00AA401B calls00AA2E10 with ECX=source;00AA4022 skips copy branch if AL nonzero | Source returned-buffer emptiness gate | Confirmed including tiny predicate below | Gate depends on a virtual source method, not source+10 float value |
|00AA4024 reads source+4 and pushes that dword;00AA402C calls[source.vtable+4];00AA402F pushes returned EAX;00AA4032 calls00AA4350 on destination | Getter result plus source metadata supplied to destination setter | Confirmed forwarding; string/CRC/copy labels Strongly inferred | Do not invent field+4 CRC semantics or retain/deep-copy policy without setter/getter evidence |
|00AA403B returns destination in EAX;00AA4048 ret4 | Constructor-style return/argument cleanup | Confirmed | Returned key destination is not a retained scalar/node ownership transfer |

00AA3FE0..00AA404A inclusive is107 bytes/39 instructions, SHAa8e6b47957aa1251797dac093feb9dd5b275241e9e245cfbb44f4639060e3d90. Four call sites (three direct and one virtual), one conditional branch and one ret4. Explicit field stores are four-byte words at destination0/8/C; stack/exception-chain writes are bookkeeping. No direct source stores or accesses to source scalar+10 occur. This is a local instruction effect, not a purity guarantee for delegated operations, aliases or concurrent writers.

Installed vtable015EA5B8 identifies PersistentCrcString: COL0161EB9C at015EA5B4, words(0,0,0,018ADC40,0161EB8C), type descriptor018ADC40 .?AVPersistentCrcString@@. This constructed base-key type is Confirmed; the [accepted wrapper](COMPRESSED_ROOT_SCALE_VALUE_CONSTRUCTION_CONTRACT.md) subsequently installs CrcLowerString. Source runtime type/property contents, key encoding/normalization/CRC policy, lookup object type and scalar units remain Unknown. No new canonical semantic function names are introduced.

## Sole optional predicate

00AA2E10..00AA2E26 is23 bytes/10 instructions, SHAc3f73467a10d2bf2c40c647fc872b97e7054645e45d81be6694e1cec4be381fa. It invokes source vtable+4, returns1 if the result pointer is null or the pointed first byte is zero; otherwise returns0. There are two internal branches, one virtual call and two plainRET exits. It writes no key/scalar memory locally. Returned pointer validity and getter side effects remain Unknown; null result alone is tolerated, not a null source object.

If this predicate returns false, the main body invokes the same virtual slot a second time. No equality/purity/stability guarantee equates the two results. Conditional metadata: known CrcLowerString slot015EA58C and PersistentCrcString slot015EA5BC both target00AA41C0,13 bytes in inventory. These are exact installed-vtable associations, not proof that every supplied source uses either dynamic type.00AA41C0 remains unopened; no global vtable census or getter body expansion.

## Ownership, defaults and failure limits

Only destination key fields are explicitly written. No local reference increment/retain, allocator, key buffer copy loop, release or scalar-store instruction establishes retain-versus-copy behavior. Destination setter00AA4350 is28 bytes and initializer00AA2D70 is18 bytes in inventory; neither is opened. Setter may forward to a larger owner/copy operation. Source getter effects and indirect dispatch remain unresolved. Preserve them as Unknown rather than proving complete zero preservation from local stores alone.

The earlier scalar MOV pair stays Confirmed: then-current source+10 bits copied to node+20 after ordinary construction. This round establishes no additional source+10 write in its local bodies; effective initial zero across all key delegates/alternate insertion paths remains Strongly inferred. Node lifetime/stability, erasure/destruction, reference ownership, threads, aliasing and actual runtime property value remain Unknown.

The main body assumes readable source/vtable and returned-buffer validity when predicate reports nonempty. It has no local source-null guard, buffer length validation, scalar fallback or allocation-error recovery. Compiler exception-chain bookkeeping does not prove thrown-path cleanup; delegate exceptions/faults were not executed or recovered. Empty returned material skips setter, leaving locally initialized fields subject to initializer effects; do not label them a complete empty-key object invariant.

## Verification and next probe

[Portable explicit-input verifier](../poc/round79-tests/Assets/Invoke-SelectedRule.ps1) SHA-gates EXE E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VAs. Independent RawPe/pefile agree for both bodies, bounded RTTI and two exact getter-slot words.39+10 instructions,17+8 anchors, main four calls/one aligned branch/ret4, tiny call/two aligned branches/two exits,23 synthetic rejection guards and eight isolated returned-buffer predicate fixtures pass. Fixtures test null/zero/nonzero byte logic, not original getters, encoding or source preservation. Raw instructions stay ignored/private; no original CPU execution, corpus, native production or external mutation.

Five explicit function candidates were used: body, three direct callees and conditional getter metadata. Only107-byte main and23-byte predicate expanded. Prior scalar/insertion/lookup/constructor/actor bodies, allocators/erase/alternate routes were not repeated; no production/shared-ledger/name/registry edits.

Next separately bounded probe:13-byte00AA41C0 getter for the two already identified typed slots, establish returned-pointer field and local source mutation. Keep dynamic-type proof conditional. Then28-byte00AA4350 setter is a separate reference/copy boundary; do not chase allocator/lifetime graph without another assignment. [Checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R79_ROOT_KEY_BASE_COPY.md).
