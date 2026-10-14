# Conditional CKAT key getter

Getter00AA41C0 reads a32-bit pointer word from this+0x0C. A nonzero word is returned unchanged; zero selects fixed address01908090. The13-byte body has no calls or memory writes and does not access source scalar+0x10. Both previously identified key vtable+4 slots resolve to it, conditional on the source actually having one of those types. This closes the local getter effect without proving dynamic type, pointer lifetime, alias behavior or complete source-zero preservation across the larger key-copy path.

## Exact evidence and confidence

| VA | Observation | Consequence/confidence |
| --- | --- | --- |
|00AA41C0 | EAX receives dword[ECX+0C] | Confirmed field offset and four-byte read; current pointer word, not copied pointed bytes |
|00AA41C3 | TEST EAX,EAX | Confirmed zero-pointer test, not returned-buffer emptiness |
|00AA41C5 | JNE00AA41CC | Confirmed nonzero value proceeds directly to return |
|00AA41C7 | EAX receives immediate01908090 | Confirmed null-field fallback address; no fallback-byte read here |
|00AA41CC | Plain RET | Confirmed register return, no explicit argument-pop or retained ownership operation |

Region00AA41C0..00AA41CC inclusive:13 bytes/five instructions, SHAd16c026950a9c3e8eded66bc5f342573d7e6c3311ca277299abc93678b15a04a. There is one conditional branch and no callee. EAX/condition flags change and normal return occurs; no object, scalar or pointed-buffer memory is written. This concrete body has no delegated side effect. Other initializers/setters and different dynamic getter implementations remain outside the proof.

The [accepted79 predicate/base-copy contract](COMPRESSED_ROOT_KEY_BASE_COPY_CONTRACT.md) consumes the returned pointer and may examine its first byte or call the getter again. That separate behavior is not replayed here. Getter00AA41C0 itself does not dereference the returned pointer, retain it, validate its contents, compare scalar bits, or manufacture a new buffer. Nonzero invalid words are forwarded without local validation. A null source object is not tolerated merely because the field-null branch exists: the first instruction requires readable this+0C.

## Conditional type association and fallback

| Known vtable | Key RTTI | Slot+4 | Target |
| --- | --- | --- | --- |
|015EA588 | CrcLowerString |015EA58C |00AA41C0 |
|015EA5B8 | PersistentCrcString |015EA5BC |00AA41C0 |

The selective verifier checks the exact two slots and bounded COL/type-name chains: CrcLowerString COL0161EB24/type018ADC64 and PersistentCrcString COL0161EB9C/type018ADC40. These Confirmed table associations do not establish the source's actual dynamic type at every key-copy invocation or exclude other virtual implementations. No global vtable/pointer census or function-name update occurs.

Fallback01908090 is virtual-only .data at the one-byte range relevant to the consuming predicate. File-backed reading rejects; runtime byte contents and initialization/writer are Unknown. Do not call it an empty string or infer a zero byte from absent file data. Pointer choice is Confirmed independently of pointed contents. Buffer encoding, termination, CRC policy, runtime property name, scalar units and lookup object identity remain Unknown.

## Ownership and compatibility limits

For this exact getter body, the local source+10 nonmutation question is closed: it only reads this+C and has no writes/calls. It does not close aliases or concurrent changes to the field/pointee, buffer retain/deep-copy semantics, getter result stability across two separate calls, node/animation lifetime, destination setter effects or full miss-default behavior. Prior scalar MOV copy remains Confirmed; effective earlier-zero preservation through all other delegates/routes remains unproved. Do not turn this getter's local purity into whole-constructor/lifetime proof.

Caller must keep the containing object readable; getter returns its current field value or fixed fallback. Replacement internals may differ, but must preserve this boundary's pointer-choice/borrowed-value semantics if exposing equivalent key data. Borrowed interpretation is Strongly inferred from direct field forwarding with no retain; allocation ownership/transfer are Unknown. No original execution or malformed-pointer behavior was tested.

## Validation and precise resume

[Portable explicit-input runner](../../SWG-Godot-Client/poc/round80-tests/Assets/Invoke-SelectedRule.ps1) gates executable SHA E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VAs. It checks independent RawPe/pefile body/slot/RTTI mappings, five exact instruction roles, no calls, one internal branch, two typed associations,19 deterministic admission negatives and eight synthetic pointer-word choices. Synthetic fixtures test zero/nonzero selection, not original CPU execution, buffer validity or lifetime. Cached getter decompilation is a lead/audited identity; original bytes establish the result. Raw witnesses stay private.

Only this13-byte body is expanded; no optional child, prior body, setter, allocator, lifetime/erase/actor/corpus work, production/name/registry/shared-ledger edit. Native production build is not applicable. [Checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R80_ROOT_KEY_GETTER.md).

Round80's next question was28-byte00AA4350 destination setter. [Round81](COMPRESSED_ROOT_KEY_SETTER_CONTRACT.md) now closes its local forwarding/store order;193-byte00AA4250 delegated reference/copy/failure effects remain Unknown. Next separately bounded probe is18-byte00AA2D70 initializer. Keep lifetime/allocator/erase graph separate. Getter field+0C writer and fallback runtime initialization are not closed by the getter/setter observations.

Round82 follow-up: the [18-byte initializer](COMPRESSED_ROOT_INITIALIZER_CONTRACT.md) is now closed for direct local writes only. It copies current global019446B8 to receiver+4 and installs015EA570 at receiver+0; global value/meaning and aliases remain Unknown. The next separately bounded probe is193-byte00AA4250 delegation. Earlier dated body results above remain unchanged.
