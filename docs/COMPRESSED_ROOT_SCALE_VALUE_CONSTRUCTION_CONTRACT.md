# CKAT scalar value construction

Value constructor007D64B0 explicitly copies one32-bit word from source+0x10 to destination+0x10 after a key-construction call returns. The [accepted node forwarder](COMPRESSED_ROOT_SCALE_INSERTION_CONTRACT.md) supplies destination=node+0x10, so this copy lands at node+0x20, exactly the [lookup's returned scalar storage](COMPRESSED_ROOT_SCALE_STORAGE_CONTRACT.md). This closes the scalar copy itself. It does not close all missing-key defaults, source preservation across the unexpanded key-base delegate, or node lifetime.

## Observations, confidence and consequence

| Observation | Interpretation | Confidence | Compatibility consequence |
| --- | --- | --- | --- |
|007D64CA loads ESI from EBP+08;007D64DC loads EDI from EBP+0C | Explicit destination argument1 and source argument2 | Confirmed | Distinguish value-construction destination from allocation base |
|007D64D0 tests destination;007D64D9 branches to cleanup007D64EE if zero | Null destination skips key construction and scalar copy | Confirmed local branch | No scalar default/store occurs on null path; do not infer caller allocation recovery |
|007D64DF pushes source;007D64E0 sets ECX=destination;007D64E2 calls00AA3270 | Key construction precedes scalar copy | Confirmed call/order; key copy semantics Strongly inferred | A failing key constructor prevents this ordinary scalar store |
|007D64E7 MOV EAX,[EDI+10];007D64EA MOV[ESI+10],EAX | Exact four-byte scalar payload transfer | Confirmed width/offset/bit-preserving operation | No arithmetic, float conversion, normalization, finite check or default substitution in this MOV pair |
| Accepted007D6310 destination=node+10 plus this destination+10 | Resulting scalar occupies node+20 | Confirmed address composition from separately closed boundaries | Legacy pointer binding targets live node payload, not a detached snapshot |
|007D64FC plain RET after compiler exception-chain restoration | Caller cleans explicit arguments; no returned retained pointer contract | Confirmed return instruction; original C++ return type Unknown | EAX contains the loaded payload bits on successful non-null path; do not treat it as an owning node pointer |

Body007D64B0..007D64FC inclusive is77 bytes/28 instructions, SHA22fca2f6fbbcc8a85f62bbf64f124faf70447f611da1de73e447e619ccde5c72. It has one call and one conditional branch. Source is read after key construction returns; the store copies the then-current bits unchanged. The accepted miss temporary's00000000 therefore remains00000000 at node+20 if those source bits are still zero at this load. Interpreted as binary32 these bits are positive zero, not default one. The bit transfer is Confirmed; preservation of the earlier zero across the unopened base-copy delegate and every alternative insertion route is not newly proved. Effective default across all misses remains Strongly inferred.

## Optional tiny key wrapper and type evidence

The sole optional immediate callee00AA3270..00AA328B is28 bytes/12 instructions, SHA1a0d6939d5ff39e488d135b0836628da341a7dba238dd7f98d171b767b6d5199. It forwards source to00AA3FE0, retains destination in ESI, installs015EA588 at destination+0, returns destination in EAX and ret4.00AA3FE0 is107 bytes in cached inventory and is not opened.

Typed metadata: vtable015EA588 has COL pointer0161EB24 at015EA584. COL words are(0,0,0,018ADC64,0161EB14); type descriptor018ADC64 carries .?AVCrcLowerString@@. The destination key subobject's constructed CrcLowerString type is Confirmed by the installation and RTTI. Matching source-key role is Strongly inferred from forwarding; the caller's runtime key contents/property spelling at01923F7C, string encoding/normalization/CRC policy and lookup object's original type remain Unknown. A recovered key class name does not close those semantics. No canonical function-name rows are added.

The wrapper itself writes a vtable word only; it does not explicitly touch source+10 or destination scalar+10. Its unopened base delegate may have construction/reference/allocation/failure effects, so no complete key-copy ownership or source nonmutation proof follows. This is the precise remaining key-construction boundary. No second child is expanded.

## Ownership and failure limits

Neither body allocates/releases the node, retains the scalar payload, handles container erasure/destruction or establishes thread affinity. Immediate storage is the externally supplied destination; successful scalar copy alone does not prove borrowed-versus-transferred allocation ownership, stable pointers across insertions/mutations, teardown order or exception rollback. These remain Unknown. Prior borrowed-interior interpretation remains Strongly inferred at the lookup/animation boundary.

There is no local source-null check, buffer-size/range validation, scalar fallback, finite policy or allocation error handling. Destination-null is a real skip branch, not proof of successful null allocation recovery in the caller. Compiler exception-frame setup/restoration does not establish thrown-path cleanup or key-base failure behavior; none was executed. Source/destination aliasing and invalid-pointer behavior remain untested. No whole-container lifetime or actor runtime parity claim follows.

## Reproduction and next boundary

Portable runner (external historical reference) requires explicit Python, baseline EXE, cached static packages and private inventory root. It gates EXE SHA E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VAs. Independent RawPe/pefile reads agree for both bodies and bounded key RTTI metadata.28+12 instructions,13+5 role anchors, exact call/branch/return structure,21 synthetic rejection guards and eight isolated bit-copy/address fixtures pass. Fixtures include zero, negative zero, ordinary values, infinity/NaN bit patterns and arbitrary words; they verify the four-byte MOV address model, not original CPU/key-constructor behavior or float validity. Raw instructions remain ignored/private.

Only007D64B0,00AA3270 and107-byte00AA3FE0 inventory metadata were considered, plus bounded installed-vtable RTTI. One77-byte body and one optional28-byte immediate wrapper were expanded. No prior insertion/lookup/constructor/actor chain, allocator/erase graph or corpus was reopened; no production/shared-ledger/name/registry edits or new actor coverage.

Next separate bounded probe:00AA3FE0 key-base copy, specifically its source/destination field widths, reference ownership and source+10 preservation/failure behavior. Leave allocator/erase graph untouched. Alternate007D58C0/007D5980 route coverage and node lifetime remain separate questions. [Checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R78_ROOT_SCALE_VALUE_CONSTRUCTION.md).
