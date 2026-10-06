# Family selector: DWORD equality, sentinel and normal-path ABI

Round69, October6,2026 Eastern/UTC. `00B69860` is a bounded family-word selector:
it returns the first index whose family+0 DWORD equals the supplied DWORD, or
`FFFFFFFF` on exhaustion/nonpositive signed count. With the explicitly approved
three-byte getter, its normal readable-path stack/nonvolatile behavior is closed.
**Resource identity and caller failure handling remain Unknown.** This closes the
selector prerequisite in the [Round68 wrapper](TERRAIN_SHADER_FAMILY_LOOKUP_CONSUMER_CONTRACT.md),
without rerunning prior lookup/accessor/predicate/serializer/setter suites.

## Input identity, extent and approved prerequisite

Executable SHA, gated before PE/VAs:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

| Complete selected evidence | Bytes/instructions | SHA-256 |
| --- | --- | --- |
|Selector00B69860..00B6989A|59 /31|7b4417782216005f8e6d7c41d944f732cb8391094db9dbbe030724db9149968a|
|Approved getter00B69440..00B69442|3 /2|19d200194d49b34530548575f37a1bb4203ee60f18d42f0084e86b7a8c899faa|

The [verifier](../tools/python/verify_terrain_shader_family_selector.py) gates
frozen68 source SHA before mapping/path/one-occurrence reuse; checks independent
raw-PE/pefile mapping for both extents, full hashes,33 raw instruction/operand
anchors, four raw fences and complete selector graph.31 selector instructions are
reachable from its prologue under some input outcomes. No original execution.

Preceding ret4 at00B69850 and13 NOPs end at00B6985F. Selector frame prologue
at00B69860 and savedEBX/ESI/EDI precede two restored-frame ret4 exits at
00B6988F/00B69898; five NOPs follow through00B6989F. Match branch00B6987E
reaches00B69892 after the first return, so the first ret is not a complete-body
boundary. **Confirmed:** decoded instructions/frame/edges/returns/fences.
**Strongly inferred:** complete containing function extent; no external debug/
symbol range authority, all indirect entries or runtime reachability proof.

Lead explicitly approved **one complete tiny getter beginning00B69440, at most
16 bytes**, only for returned field andEDX preservation. Actual getter is3 bytes/
2 instructions: `mov eax,[ecx]; ret`. Preceding popEBP/ret4/three NOPs spans
00B69439..00B6943F; thirteen NOPs follow getter through00B6944F. It has no call,
branch, stack store or other register write. **Confirmed:** reads one DWORD at
receiver+0, changesEAX only, preservesEDX and nonvolatile registers, plain ret.
No other helper is inspected or semantically expanded.

| Complete inventory | Count |
| --- | ---: |
|Main selected body / approved tiny getter|1 /1|
|Selector instructions / getter instructions / all anchors|31 /2 /33|
|Selector calls / branches / returns; getter returns|1 /3 /2;1|
|All distinct function/handler leads|2|
|All raw sites including helper return/fences|11|
|Separate authorized lead/site ceilings|12 /12|

Leads00B69860/00B69440. Selector sites:callB69877,branchesB6986D/B6987E/B69886,
returnsB6988F/B69898,fencesB69850/B6989B. Getter returnB69442 and fencesB69439/
B69443 add3. No incidental prologue/handler lead beyond these; complete11 includes
the amendment's fences/return, not just initial8. Operand anchors are a separate
verification denominator. No callgraph/corpus/export scan or inventory increase.

## Exact input, traversal and equality

IncomingECX is the group receiver. At00B69866, EDI captures DWORDgroup+8 count.
EDX starts0; signed count<=0 jumps directly to failure without reading the stack
input or array. Positive count capturesESI=group+4 array pointer at00B6986F and
EBX=DWORD[EBP+8] input at00B69872. **Confirmed input width:**32 bits, used as a
whole DWORD rather than pointer/string or float. No signed ordering applies to key
equality; key bit patterns including high bits are compared unchanged.

Loop00B69875 loads family pointer from[ESI] intoECX;00B69877 calls the approved
getter, returning family+0 word intoEAX.00B6987C comparesEAX withEBX; equality
jumps to match. OtherwiseEDX increments by1, ESI by4, and signedEDX<capturedEDI
returns to loop. Count and base are captured once, unlike the accepted67 entry
lookup's count reload. Subsequent pointed family-word reads still depend on current
memory; concurrency/stability of that memory is not established.

Match path00B69892 restoresEDI/ESI, loadsEAX=EDX index at00B69894, restoresEBX/
EBP and ret4. Failure path restoresEDI/ESI, executesOR EAX,FFFFFFFF at00B6988A,
restoresEBX/EBP and ret4. **Confirmed:** ascending first-full-word-equality index;
duplicates choose earliest; failure is exact all-one bits regardless of priorEAX.
No comparator/error/open delegate remains in the inspected selector/getter pair.

The accepted61 family constructor/ordered loader correlates family+0 with the first
raw SFAM DWORD (`FamilyKeyBits` structural label). **Confirmed structural provenance**
plus this numeric equality; resource/CRC/asset/gameplay identity of that word remains
Unknown. No field/semantic name registry change or resource resolver is justified.

## Stack and nonvolatile closure for accepted68

Selector saves/restoresEBX/ESI/EDI/EBP on both exits. The getter does not alter those
orEDX, so loop index/count/base/key remain intact on normal readable execution.
The selector's only stores are pushes of caller frame/nonvolatile values below its
entry stack. It reads only its own first argument atEBP+8, never writes the deeper
caller-staged words and both exits endret4. Getter plainret consumes only its own
return address. **Confirmed normal-path ABI:** one stack input consumed, caller
nonvolatile values and deeper pending destination/search words unchanged.
No convention-only preservation assumption is needed after this approved leaf proof.

For a valid selected index, accepted68's original destination/search slots are correctly presented
to00B69700, originalgroupESI/originaldestinationEDI survive selector return, and
its post-lookup EAX=EDI establishes intended destination return on both normal child
paths. Child lookup/accessor lifetime/cleanup and abnormal exits remain inherited
open limits; those bodies are not rerun or edited. Round68's dated report retains
its historical unknown; this follow-up is the canonical closure, not a rewritten
historical test claim.

## Failure sentinel and unchecked caller consequence

Count<=0 or missing word returnsFFFFFFFF. Accepted68 then immediately indexes
its group+4 array usingEAX*4, with no sentinel test. **Confirmed arithmetic:**
`array_base + FFFFFFFF*4` wraps to `array_base-4` modulo32 bits. This is a read
address before the array, not an observed crash, safe rejection, null fallback or
resource result. No invalid address is dereferenced in models/tests. The other
accepted64 accessor likewise uses this selector result without a local range check;
its prior body is not replayed. Caller inputs must satisfy actual family membership
before these unchecked paths can be treated as valid. Modern explicit sentinel
rejection can be a replacement policy, not a claimed legacy behavior.

There is no local array/count consistency, null/input-size/version/exception/status
check. Nonpositive count avoids input/array reads after group count, but the group
count read itself requires a valid receiver. Positive counts may read invalid arrays/
family pointers;32-bit cursor wrap, huge counts, concurrent changes, faults and
exceptional recovery are unexecuted/unclosed. No allocation, mutation of family data,
string byte read, retain/free, callback or resource open exists in this closed pair.
Borrowed array/family lifetime and thread affinity remain Unknown.

## Reproduction and models

[Portable runner](../poc/round69-tests/Assets/Invoke-SelectedRule.ps1):

```powershell
& ./poc/round69-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config
```

`selector_witness(word_reads,input_word,count,...)` models supplied readable words,
full32-bit equality, captured signed count, first match/sentinel, four-byte cleanup
and preserved supplied pending/nonvolatile identities. `caller_array_address`
models modulo32-bit address arithmetic without memory reads. Finite extent/type
rejection is modern witness policy, not recovered original validation. Model captures
stable count; it does not execute original registers, faults, races or callbacks.

[Independent peer](../poc/round69-tests/Assets/peer.py) passes121 guards:7 identity,
3 source SHA,6 truncation,6 headers,7 mapped extent,34 raw/operand,9 selected
body/fences,7 approved getter/fences,7 control/reachability,18 budget,8 output path,
5 safe-model policy,4 no-publication.22 local and22 independent selector checks
cover raw/high-bit words, duplicates, empty/nonpositive count, missing word, captured
count, pending/nonvolatile identity, ret4 and sentinel/cursor arithmetic. Existing
positive hashes survive expected rejection; portable RunRoot/sentinel and synthetic
reparse metadata fixtures create no actual junction or invalid-memory read.

Exactly one accepted1977-byte SGRP0006 occurrence SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`,
15 families/35 entries, supplies15 family-word selector models. It has15 distinct
family words; each matches its first occurrence with stable supplied fields. This
is actual original-byte structural correlation plus a model, not original CPU calls,
observed runtime keys, resource identity or corpus coverage. Config/source/TGEN
SHA/priority42 remains gated by frozen machinery; original values/output stay ignored.

Final scope/text gates: all4226 frozen public files unchanged, exactly five new
owned files, zero existing edits/deletions/unrelated differences. HEAD preserved,
index empty, private output ignored. UTF-8/no-BOM/LF/single-final-newline, Python
AST, PowerShell parse, per-file whitespace/focused diff and git diff --check pass.
Workspace validator:1716 Markdown files, zero broken/private links,2626 names/
zero duplicate addresses,345 packet rows, zero public files at least25MiB.
No native/GPU source changed; no new build result claimed.


See [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R69_FAMILY_SELECTOR.md).
Next precise producer/use probe: inspect PE export metadata for00B6B580 (accepted68
foundzero directE8 callers), then independently establish one real export/indirect
caller and search/key producer or returned-record consumer. Stop at its first
unresolved forward. No caller, resource name or key-validity guarantee is invented.
