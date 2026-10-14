# Queue initialization and cleanup: receiver, fields, and reserve arithmetic

Initializer `00AA6050` returns the original receiver on normal return. Combined with [the Controller storage contract](OBJECT_CONTROLLER_QUEUE_STORAGE_CONTRACT.md), this closes the selected initializer-return/allocation identity: the outer 20-byte allocation passed as the receiver is returned and stored at controller `+4`.

Five four-byte fields, two internal 12-byte record allocation requests, 16-byte pointer-span arithmetic, signed request clamping, and cleanup call ordering are **Confirmed**. A reserve/growth role is **Strongly inferred**. Concrete queue class/RTTI, element fields/payload ownership, read/write/ring roles, hard queue limits, callbacks, cancellation, and deeper allocation/failure cleanup remain **Unknown**.

## Input, scope, and reproduction

All VAs use executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. [Verifier](../../SWG-Godot-Client/tools/python/verify_object_controller_queue_initialization.py): explicit `--exe`, `--packages`, fresh ignored `--receipt`. [Independent runner](../../SWG-Godot-Client/poc/round57-tests/Networking/research_checks.py): explicit `--exe`, `--packages`, fresh ignored `--output-dir`. Identity rejection precedes package import/VA use; no original execution or live contact occurs.

Exactly two new primary bodies are decoded: initializer `00AA6050`, 696 bytes/254 instructions, SHA-256 `3d9aecdf55a2b69d44c5becb7ba669254c5d250de66bf9c4c6a04616863f1c5e`, ending `ret 4`; cleanup `00AA6310`, 96 bytes/34 instructions, SHA-256 `e8d84f8aa8b8c1dac2109867b3aec627a21c91169f60645bacca1aeeb6e0787b`, ending `ret`. No helper body, tiny forwarder/accessor, external xref search, or full frozen Controller/scene audit is added.

The bodies contain **23 direct call sites** to **ten distinct unexpanded callee leads**. The fresh candidate cap of 20 applies to those distinct leads; call-site count is recorded separately rather than represented as fewer calls. This inventory is local to the two explicitly authorized bodies, not a caller census. No callee lead is expanded.

## Observation, confidence, and compatibility consequence

| Observation | Interpretation/confidence | Consequence |
| --- | --- | --- |
| `00AA606E` saves incoming ECX in EDI; `00AA6074` stores EDI at stack slot `[ebp-18]`; `00AA62F5` returns that saved slot in EAX. | Confirmed normal-return receiver identity under the normal ABI/valid stack premises. | The selected outer allocation becomes the controller queue reference; no substitute return object is introduced. |
| Calls `00AA607A` and `00AA60BA` request12 bytes each through `00AC15C0`. The first result is stored at queue `+0`; the second at `+4`. | Confirmed allocation-request and pointer-store shape; internal-record ownership is Strongly inferred from corresponding cleanup/release calls. | Keep the queue object and its two referenced records separate; do not equate the outer 20-byte object with entry storage. |
| First record gets zero stores at `+0/+4`, then `00AA60A6` calls unexpanded `00AA67C0` on record `+8`, with a local-byte address and zero stack arguments. Second record gets three explicit zero DWORD stores. | Confirmed direct initial writes. First record's complete post-helper initialization remains Unknown. | Do not assert a zero first capacity without closing `00AA67C0`. |
| Queue aliases are installed from those record pointers; the fifth field is initialized zero. | Confirmed five-field layout below; alias roles are Unknown. | Preserve logical references while keeping read/write/swap/ring names unassigned. |
| Negative signed request selects a zero local; non-negative selects the supplied argument, twice. Span differences are arithmetic-right-shifted4; growth-byte requests left-shift the selected demand4. | Confirmed `n=max(signed request,0)` and16-byte arithmetic. Reserve/growth interpretation Strongly inferred. | Raw16 is an extent/growth demand, not evidence of a hard queue maximum or current message count. |
| If existing capacity-like extent is sufficient, unsigned `jae` skips growth. Otherwise the branches manipulate three pointers while retaining the prior end-minus-start extent. | Confirmed local comparisons/stores; three-pointer reserve shape Strongly inferred, concrete container/type Unknown. | A replacement may use different internal storage, but must first recover the consumer/growth behavior before treating16 as a limit. |
| Cleanup calls `00AA6370` twice with queue receiver, current record pointer, raw flag1, A then B. It then re-reads each pointer and conditionally calls `00AA6710` and `00AC1640` when non-null. | Confirmed argument/order/null checks. Entry/callback/deletion/cancellation meaning of flag1 and cleanup-helper semantics remain Unknown. | Keep descriptor release separate from element/payload ownership and cancellation. |

## Five observed fields

All offsets are relative to the outer receiver, all widths four bytes in this 32-bit executable. Names below are neutral descriptions, not canonical class members.

| Offset | Initial direct value | Selected cleanup direct behavior |
| --- | --- | --- |
| `+00` | first 12-byte record allocation result A (`00AA60B8`) | re-read for record operations/release; no direct field clear |
| `+04` | second 12-byte record allocation result B (`00AA60D4`) | re-read for record operations/release; no direct field clear |
| `+08` | alias of A (`00AA60DD`) | zero store at `00AA6359` |
| `+0C` | alias of B (`00AA60D7`) | zero store at `00AA6360` |
| `+10` | zero (`00AA60E0`); semantic role Unknown | zero store at `00AA6367` |

The initializer establishes initial alias equality. Later alias switching and read/write ownership are not closed here. Cleanup re-reads `+0/+4` after the record-operation calls; unchanged identity across those opaque helpers is Unknown. Indirect helper mutations are not ruled out by the absence of direct clears.

No genuine queue RTTI or vtable/class identity is established by these bodies. The five observed stores cover the 20-byte outer request, but do not establish a complete named C++ class, entry schema, or native64-bit memory requirement.

## Reserve arithmetic, allocation branches, and errors

Each referenced record is used as a three-DWORD pointer-span structure: `+0` start-like, `+4` end-like, `+8` limit-like. Capacity-like comparison uses `(limit-start)>>4`; preserved used extent uses `(end-start)>>4`. Growing assigns new start, new end at `start + prior_used*16`, and new limit at `start + n*16`. Thus growth does not itself establish an active element count of n. Roles are Strongly inferred from the direct arithmetic and old-range loops, not from a library/class name.

Requested byte size is a 32-bit `n<<4`. A byte request unsigned-at-most `0x80` routes to `012EA770`; larger routes to `00AC15C0`. For the selected caller's n16, **if growth is needed**, the request is256 bytes and takes the latter route. Zero/negative demand makes the unsigned extent comparison sufficient without growth; it does not avoid the two record requests or subsequent record dereferences. Positive multiplication can wrap; no local overflow rejection is established by these blocks. The harness's eight arithmetic fixtures test only these observed local operations/thresholds, not an original allocator or usable queue simulation.

Old-range growth branches delegate transfer at `00AA6850`, per 16-byte stepping operation at `00AA6890`, and old-storage operations at `00AA67D0` / `00AA6810`. None is expanded, so copying versus moving, payload destruction, and allocation/release correctness remain Unknown.

Record-allocation null branches explicitly produce/store null pointers. Later reserve arithmetic dereferences the records without a new local null guard. Storage-allocation results feed pointer arithmetic without a demonstrated local null/error recovery policy. If allocation helpers return null rather than throw, these observations do not establish a successfully initialized/usable object. Exception unwind, partial cleanup, helper failure, and allocator behavior remain Unknown; do not infer either graceful recovery or an observed crash.

Cleanup's two `00AA6370` calls occur **before** its record null guards. Under normal return it then handles A before B, calls the record helper before its release call, and clears aliases/final field. The outer Controller routine releases the outer queue only after this cleanup returns, per the prior contract. Paired allocation/release supports internal-record ownership as Strongly inferred, not complete entry/payload ownership. No full scene detachment or current-pointer lifetime guarantee is added.

## Validation and next probe

The verifier checks 80 selected anchors, exact full-body hashes/decoding/exits and the ten-lead/23-call local inventory. Independent gates check23 literal byte witnesses,13 relative calls, saved-receiver/return-slot identity, five fields/aliases, signed clamp and threshold branches, eight local arithmetic cases, and cleanup flags/clears. Twenty-one rejection cases cover wrong/missing executable, altered anchor/span/lead/return slot/alias/clamp/threshold/cleanup flag/clear, truncation/unmapped reads, malformed output choices, pre-resolution lexical link/reparse rejection, and cap overflow. Fresh nested output and rejected output-state preservation pass. There is no asynchronous tool lifecycle to cancel; actual queue cancellation semantics remain unrun/Unknown.

Next minimal pair: `00AA67C0` at initializer call `00AA60A6` (receiver record A+8; local-byte address and zero arguments), and `00AA6370` at cleanup calls `00AA6319/00AA6326` (queue receiver; record pointer; raw1). Close first record's limit-like initialization and per-record element/payload cleanup/flag meaning before pursuing `00AA6710` or other growth helpers. Physical profiles, scene detachment, and operational queue/ring behavior stay separate.
