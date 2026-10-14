# CKAT key setter forwarding boundary

Setter00AA4350 forwards its first explicit argument and literal zero to00AA4250, then stores its second explicit argument as a32-bit word at destination+0x04 after that delegate returns. It performs no other explicit object-field store. The193-byte delegate remains unopened: buffer copy/retention, source preservation, failure side effects and lifetime are not established by this wrapper.

## Exact observations and confidence

| VA | Observation | Interpretation/consequence |
| --- | --- | --- |
|00AA4353 | EAX reads dword[EBP+08] | Confirmed first explicit argument; source/buffer pointer role Strongly inferred from accepted caller |
|00AA4357/4359 | Push literal0 then EAX | Confirmed delegate source-order arguments: first caller argument, second0; zero discriminator meaning Unknown |
|00AA435A | ESI retains incoming ECX | Confirmed destination object pointer retained for later local store |
|00AA435C | Direct call00AA4250 with incoming ECX | Confirmed immediate destination-directed delegate call; effects Unknown |
|00AA4361/4364 | ECX reads dword[EBP+0C];dword[ESI+04] receives ECX | Confirmed four-byte metadata-word copy, after ordinary delegate return |
|00AA4369 | RET8 | Confirmed two explicit stack arguments consumed; no owning-pointer return contract |

Body00AA4350..00AA436B inclusive is28 bytes/13 instructions, SHAbea5da39201c5445ad342e7bdc0dc357adf21f5487d34af8a97f467ecaed3b01. One direct call, no branches, one ret8. ESI is saved/restored. Wrapper does not assign a final EAX result of its own; do not infer returned destination ownership from delegate result. No local source-buffer dereference, copy loop, reference increment/decrement, allocator/release or scalar+10 access establishes copy-versus-retain behavior. Argument-word reads and the explicit destination+4 store are structurally known; friendly metadata/CRC semantics remain Unknown.

The [accepted key-base copy](COMPRESSED_ROOT_KEY_BASE_COPY_CONTRACT.md) supplies its source+4 dword as this second argument and a virtual getter result as the first. This inherited caller boundary explains forwarding; its107-byte body is not repeated here. The local wrapper writes destination metadata, not caller source scalar, under the distinct-source/destination ordinary interpretation. Aliasing is not proven absent, and delegate00AA4250 may have further writes/nonlocal effects. Do not claim full source+10 preservation from this one explicit store.

## Type association and ownership limits

The setter neither installs a vtable nor makes a virtual call. Its key context comes from the already closed direct caller in PersistentCrcString construction, followed by the CrcLowerString wrapper. Bounded type metadata is independently checked: vtable015EA5B8 COL0161EB9C/type018ADC40 names PersistentCrcString; vtable015EA588 COL0161EB24/type018ADC64 names CrcLowerString. These Confirmed tables corroborate inherited context, not a newly recovered virtual setter slot or proof of every receiver/source's runtime type. No function-name registry changes are made.

Literal zero is an input to the delegate, not a proven zero scalar default or retain/deep-copy flag. The current source+10 scalar copy remains separately Confirmed only at the accepted value constructor's MOV pair. Full earlier-zero preservation across all delegates/routes remains unproved. Key contents/property spelling/encoding/CRC policy, metadata-word meaning, scalar units, buffer ownership/retention, aliasing/concurrency, node lifetime/erase/destruction and pointer stability remain Unknown.

The metadata store is sequenced after normal delegate return. If00AA4250 does not return normally, this ordinary-path store is not reached; partial child effects and caller recovery/rollback are Unknown. No local exception frame, source/null/length validation, error-status test, fallback or failure cleanup appears here. This is not a guarantee that the delegate succeeds or that failure leaves the destination unchanged. No original CPU/malformed-input/exception behavior was executed.

## Scope, validation and resume

Only the28-byte setter is expanded. Immediate00AA4250 is193 bytes in the cached function inventory; its body is not needed to establish wrapper forwarding/store order and is deliberately unopened. The optional failure child allowance is unused.18-byte00AA2D70 initializer is inventory metadata only. No previous getter/base/value/insertion/actor body, corpus, allocator/erase/lifetime graph or global census is reopened; no production/shared-ledger/name/registry edit.

Portable explicit-input runner (external historical reference) gates original EXE SHA E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VAs. Independent RawPe/pefile body and bounded RTTI reads agree.13 instructions/eight exact role anchors/one call/no branches, exact sole local field store,18 deterministic admission negatives and eight authored isolated dword-store fixtures pass. Fixture expected bytes are authored before any native comparison and check only destination+4 little-endian store/neighbor preservation; they do not model delegate side effects, key copying or original CPU behavior. Raw witnesses stay ignored/private. Native production build does not apply.

Next assigned research lead: separately authorize18-byte00AA2D70 initializer for initial key-field state/local writes and immediate constructor boundary. It remains unopened in this milestone.193-byte00AA4250 buffer/reference/failure behavior is a distinct later question; do not chase allocator/erase/lifetime graph. [Checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R81_ROOT_KEY_SETTER.md).

Round82 follow-up: the [18-byte initializer](COMPRESSED_ROOT_INITIALIZER_CONTRACT.md) is now closed for direct local writes only. It copies current global019446B8 to receiver+4 and installs015EA570 at receiver+0; global value/meaning and aliases remain Unknown. The next separately bounded probe is193-byte00AA4250 delegation. Earlier dated body results above remain unchanged.
