# Family-entry lookup caller and exact stopping boundaries

Round67, October6,2026 Eastern/UTC. `00B69779` is a proved call instruction in
selected body `00B69700..00B69788`, not a call inferred solely from raw E8 bytes.
The caller searches ordered entry pointers through a zero-result predicate and
returns the selected three-word record, or fills a zero record on no match.
**Record lookup is Strongly inferred; resource meaning/open/fetch remains Unknown.**
This extends the [accepted accessor](TERRAIN_SHADER_FAMILY_ENTRY_ACCESSOR_CONTRACT.md)
and inherits the [conditional predicate](TERRAIN_SHADER_FAMILY_PREDICATE_CONTRACT.md)
without re-expanding either body or implementing a resolver.

## Identity, containing extent and complete inventory

Executable SHA, gated before PE/VA inspection:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Selected137-byte body/58 instructions SHA:
`6ec6af1d3814942cfe2354b786e82edd86c49768aa3c93678ebbe5624b353458`.

The [verifier](../tools/python/verify_terrain_shader_family_entry_consumer.py)
independently maps bounded raw PE headers/sections and crosschecks pefile;
verifies complete body hash,58 exact instruction-byte/operand anchors and two
raw extent fences; reconciles4 calls,3 branches and2 returns; and traverses the
selected control graph to prove all58 instruction boundaries reachable from the
prologue under some supplied branch outcomes. No original process is invoked.

**Observation / Confirmed:** `00B696F0` is the predecessor's final pop-EBP byte,
followed by raw ret4 at00B696F1 and12 NOPs through00B696FF. At00B69700,
pushEBP/movEBP,ESP/subESP,16 establishes the selected frame. Both normal exits
restore savedEDI/ESI/EBX/EBP and end ret8, at00B6976F and00B69786. Seven NOPs
follow through00B6978F. The conditional edge00B69736 goes to00B69772,
after the first return, and reaches the exact call at00B69779. That block uses
the established frame/ESI/EDI; it is not assigned an independent function start.
No selected branch leaves the137-byte interval. **Strongly inferred:** this is
one complete containing function extent. There is no external symbol/debug
function-range authority or proof of all indirect entries/runtime reachability.

A raw executable-section scan finds one incoming E8 candidate to00B69700,
at00B6B5A8. Its containing caller/instruction legality is not established here.
It corroborates an entry address, not a proved runtime call. The original E8
candidate00B69779 is independently proved by the complete decoded selected graph.

| Complete accounting | Count |
| --- | ---: |
|Selected complete body / new prerequisite helper contracts|1 /0|
|Selected instructions / operand anchors / extent fences|58 /58 /2|
|Selected direct calls / branches / returns|4 /3 /2|
|Selected primary plus callee function leads|5|
|Discovery-only prologue/adjacent-handler leads|5|
|**All function/handler leads**|**10**|
|Selected control/reference/fence sites plus incoming raw reference|10|
|Discovery-only handler-reference site|1|
|**All raw sites under the inventory ceiling**|**11**|
|Separate authorized lead/site ceilings|12 /12|

Lead explicitly reviewed the complete10/11 inventory and required that the5/10
selected subset never replace it. No amendment or new helper expansion was
needed. Discovery-only leads are00B69630,00B69670,00B69790,00B697B0 and014D23E8;
the incidental handler reference is00B697B5. Five short raw lead/reference anchors
and executable-handler metadata are checked, without a neighboring semantic
contract or handler-byte read. Discovery windows included adjacent instruction
fragments; none is promoted into a helper analysis, fixture or reusable body.
The11-site denominator counts calls, branches, raw references and extent fences,
not every one of the58 instruction operands. All budgets are enforced separately.

## Actual search, result discriminator and record use

IncomingECX family receiver is saved inESI. The sole visible inputs are destination
atEBP+8 and search pointer atEBP+0x0C; EDI entry index andEBX byte offset start0.
At00B6970A the family+0x24 count is loaded and tested signed. Count<=0 skips
search. Positive count enters the ordered loop at00B69720.

Each iteration re-reads family+0x20 entry-array pointer and reads entry pointer
atarray+EBX+4. At00B6972A the search pointer is pushed; at00B6972B the entry
pointer is pushed. Thus00B6972C calls01320590 with **entry pointer as argument
one, search pointer as argument two**. The caller cleans8 stack bytes at00B69731
and tests fullEAX at00B69734. Exactly zero jumps to the match block; any nonzero
value continues. After nonzero, family+0x24 is reloaded, EDI increments by1,
EBX by12, and signed EDI<count gates the next iteration. The array and count
are not captured as one immutable snapshot by this body.

**Confirmed:** ordered first-zero-result selection with12-byte entries and count
reload after every nonzero result. **Strongly inferred:** entry-string lookup.
The accepted63 predicate uses two byte pointers; only whenDWORD019AA110 equals
zero is ASCII A..Z folding and normalized comparison proven. Therefore under
that separately established mode, zero corresponds to folded byte-string equality.
The actual original mode/producer is unknown, and no mode is read or forced in67.
Nonzero-mode delegates, full-EAX behavior and error semantics remain unresolved.
The first unresolved search forward is01320590 at00B6972C; its body is not rerun.
No asset-name, path or resource identity follows from comparison.

On match,00B69772 pushes the still-currentEDI index, then loadsEDI fromEBP+8
and pushes that destination. ECX=ESI family at00B69777;00B69779 calls accepted
accessor00B692A0 withdestination/index. It fills destination+0,+4,+8 from family
word, selected entry pointer and entry raw word. At00B6977E the caller explicitly
setsEAX=EDI destination, before restoring registers and ret8. No returned string
byte is read or transformed in this match block. No local later forward/open occurs.
The selected entry is **re-read by the accessor**, rather than reusing the pointer
previously passed to the predicate. Changing source state between operations can
make these pointers/words differ; the supplied-state model demonstrates this control
possibility, not an original concurrent mutation, legal race or synchronization policy.

On no match or nonpositive initial count,00B69746 calls accepted initializer
00B69160 on the local triple. Accepted66 proves that helper zeros its three words.
The caller stores those to destination+0,+4,+8 in order at00B69753/58/5E, then
calls00B69170 at00B69764 with the local triple asECX. **Confirmed:** zero triple
is filled before cleanup. **Unknown:** EAX after cleanup; it is not reloaded into
the destination before ret8. The model's absent return value means unestablished
EAX, not an observed null return. Cleanup is the first unresolved no-match helper;
the match accessor's internal cleanup/lifetime limit is inherited from66.

## ABI, ownership and failure limits

Confirmed local ABI: ECX receiver, two stack arguments, saved/restoredEBX/ESI/EDI/
EBP, ret8 at both exits. Member-style record lookup with a caller-supplied output
buffer is Strongly inferred. No recovered function/class signature, RTTI or stable
semantic name is introduced. The destination is explicitly returned only on match.

No local null/length/terminator/destination-size check, version discriminator,
exception handler registration or controlled fault result is observed. A nonpositive
count bypasses the predicate and does not locally dereference search; positive
counts pass entry/search pointers without checking them. Predicate readable-input
preconditions and possible faults inherit63; no invalid memory is executed here.
Malformed counts can drive unchecked reads;32-bit index/offset overflow, exceptional
helper exits and mutation during traversal are unexecuted/unclosed behavior.

Neither selected search nor match block clones, retains or normalizes string bytes.
Destination receives the existing pointer under the accepted accessor provenance;
callee retention/freeing, thread affinity and eventual destination ownership remain
Unknown. Model alias mutation is visible through the same object, not a lifetime
proof. Destination stores form a+0,+4,+8 prefix; no rollback/atomic publication is
shown. Failed comparator/accessor/cleanup may leave an earlier side-effect/store
prefix; these external effects and abnormal exits are not executed or guaranteed.

## Reproduction and bounded witnesses

Use the [portable runner](../poc/round67-tests/Assets/Invoke-SelectedRule.ps1):

```powershell
& ./poc/round67-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config
```

Explicit cached inputs; fresh ignored output is automatic. The [peer](../poc/round67-tests/Assets/peer.py)
checks identity before PE inspection, truncation/headers/extents, exact operands,
all control edges/reachability, early-return truncation, discovery/incoming inventories,
separate12 caps, portable output/reparse guards and no-publication preservation.
`lookup_witness` uses supplied full-EAX results; it does not execute any original
predicate/accessor/helper. Signed count gates, first-zero selection, nonzero exhaustion,
count shrink/growth, argument identity/order, post-comparison selected-record changes,
store order, pointer alias mutation and match/no-match return differences are modeled.
Finite witness extents/types reject safely as **modern test policy**, not recovered
legacy rejection. No exhaustive predicate suite or setter/accessor/serializer suite reruns.

Exactly one accepted SGRP0006 occurrence1977 bytes, SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`,
15 families/35 entries, supplies35 conditional lookup witnesses. Each record uses
a supplied nonzero prefix then zero to select that occurrence; it does not claim
that actual original comparisons select every duplicate occurrence. Config/source/
TGEN SHA/priority42 remain gated by frozen66 decoding. Original names/bytes and
instruction dumps stay ignored; no additional corpus/runtime state is gathered.

Final replay passes150 negative guards:7 identity,3 frozen-source,6 PE
truncation,6 headers,7 mapped extents,59 raw/operand,9 complete-body/extent,
10 control/reachability,2 raw incoming inventory,5 discovery-only anchors,
18 lead/site budget,8 output path,6 modern safe-witness policy and4 no-publication.
Fifteen local and23 independent lookup checks pass. Existing positive output hashes
survive expected rejection. Portable RunRoot/sentinel fixtures and synthetic reparse
metadata are used; no actual junction/invalid-memory read is made.

Final scope/text gates: exactly five new owned files, all4157 frozen public files
unchanged, no existing edits/deletions/unrelated differences; HEAD preserved/index
empty/private ignored. UTF-8/no-BOM/LF/single-final-newline, Python AST, PowerShell
parse, per-file whitespace/focused diff and git diff --check pass. Validator:
1696 Markdown files, zero broken/private links,2626 names/zero duplicate addresses,
345 packet rows, zero public files at least25MiB. No native/GPU source/build changes.


See the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R67_FAMILY_ENTRY_CONSUMER.md).
Next precise resource-use probe: establish raw incoming00B6B5A8's instruction/
containing-body boundary and follow the supplied search pointer's producer and
returned record-pointer use to the first unresolved forward. Its function start
and resource role are not guessed. If lifetime is instead required, separately
bound00B69170; no automatic graph/corpus expansion or resolver follows this result.
