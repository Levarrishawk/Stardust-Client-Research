# CKAT key initializer

Initializer00AA2D70 returns its incoming ECX receiver in EAX, copies the current32-bit word at global019446B8 to receiver+0x04, and installs015EA570 at receiver+0x00. The18-byte body has no calls, branches, explicit argument reads or source-scalar+0x10 access. Runtime global value and field meaning remain Unknown; this is not a recovered zero/default-CRC policy or lifetime contract.

## Exact observations and confidence

| VA | Observation | Consequence/confidence |
| --- | --- | --- |
|00AA2D70 | EAX receives incoming ECX | Confirmed receiver retained for writes and return |
|00AA2D72 | ECX receives dword[019446B8] | Confirmed absolute global read, width4; actual runtime word Unknown |
|00AA2D78 | dword[EAX+04] receives ECX | Confirmed four-byte current-value copy into receiver field+4 |
|00AA2D7B | dword[EAX+00] receives015EA570 | Confirmed installed vtable word/address; fresh type name not recovered here |
|00AA2D81 | PlainRET | Confirmed EAX receiver return; no explicit stack arguments popped |

00AA2D70..00AA2D81 inclusive:18 bytes/five instructions, SHA f66d61b0c8353ade3bf643cc9b2fee7dee93c67c801cf2407e4c5c554c5267d7. Both stores occur in that order, using EAX saved from original ECX. ECX is overwritten with the global word. No object-field read, loop, conditional validation, allocator, retain/release, child call or exception frame appears. This body reads no explicit stack argument and does not access receiver+8/+C or scalar+10. No allocation ownership transfer is established by its constructor-style return.

Global019446B8's four-byte range is virtual-only .data; a file-backed read rejects. Do not derive runtime zero, empty-key CRC or metadata semantics from absent file bytes. The instruction copies the then-current global word; repeated invocations may see different words if other code changes it. Global writer/initialization timing/thread policy are unopened. No original CPU execution or runtime value observation occurred.

## Existing typed context and compatibility consequences

The [accepted key-base copy](COMPRESSED_ROOT_KEY_BASE_COPY_CONTRACT.md) invokes this initializer before its own field+8/+C setup and PersistentCrcString vtable installation; the [accepted wrapper](COMPRESSED_ROOT_SCALE_VALUE_CONSTRUCTION_CONTRACT.md) later installs CrcLowerString. Those prior bodies are not replayed. Existing PersistentCrcString/CrcLowerString RTTI chains are selectively verified as inherited context only:015EA5B8/COL0161EB9C/type018ADC40 and015EA588/COL0161EB24/type018ADC64. No new RTTI discovery/class name is asserted for015EA570, nor every receiver/source dynamic type proved. No name/registry increment occurs.

Receiver+4 is structurally known but its friendly CRC/metadata meaning remains Unknown. Key contents/property/encoding/CRC policy and scalar units are likewise unclosed. Compatibility requires the observed current-global-word copy and constructor ordering if reproducing this boundary; replacement internals need not preserve legacy object layout. An unconditional zero or default-one value would need separate evidence.

No local source+10 read/write or delegated effect exists in this initializer. That closes this exact body's local effect only. Aliases between receiver/global/source objects, concurrent writers, full surrounding source preservation, buffer copy/retain policy, node pointer stability and destruction/ownership remain Unknown. The earlier scalar MOV pair stays Confirmed; effective earlier-zero preservation across all other delegates/routes remains unproved. Local receiver writes cannot establish nonmutation of aliased storage.

Readable global and writable receiver0..7 are assumed by instructions, without null/range/status checks or fallback. Fault/exception behavior, partial writes under invalid memory and caller rollback were not executed or recovered. There is no local failure-handling branch to turn into a graceful allocation policy.

## Verification and precise resume

[Portable explicit-input runner](../../SWG-Godot-Client/poc/round82-tests/Assets/Invoke-SelectedRule.ps1) takes Python, original EXE, cached static-package and private inventory paths, creates fresh ignored output, and SHA-gates E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VAs. Independent RawPe/pefile body and inherited RTTI mappings agree. Five instructions/five exact role anchors/no calls/no branches, exact receiver stores,19 deterministic admission negatives and eight authored local-effect fixtures pass. Fixtures vary a synthetic global word and check field+4 bytes, installed vtable+0 and untouched neighboring bytes; they do not establish runtime global contents, aliases or original CPU behavior. Raw witnesses stay ignored/private; native production build does not apply.

Only the18-byte initializer is expanded.193-byte00AA4250 inventory metadata is the next concrete boundary; no child/destructor/allocator/lifetime/erase/actor/corpus expansion, production/shared-ledger/name/registry change or original execution. [Checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R82_ROOT_INITIALIZER.md).

Next separate assignment:193-byte00AA4250 delegate reached by the [accepted setter](COMPRESSED_ROOT_KEY_SETTER_CONTRACT.md), with a bounded question about direct key-field writes, buffer/reference forwarding, discriminator0 path and immediate failure boundary. Keep allocator/erase/lifetime graph unopened; global019446B8 writer/semantics remain a separate unresolved question.
