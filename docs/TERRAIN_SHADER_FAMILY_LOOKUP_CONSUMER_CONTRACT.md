# Family lookup wrapper: incoming pointer and selector boundary

Round68, October6,2026 Eastern/UTC. Raw candidate00B6B5A8 is a proved call
instruction in selected body00B6B580..00B6B5B6. This wrapper stages caller inputs,
uses an unresolved selector's returned index, and forwards to the accepted family
entry lookup. **No search-pointer construction or resource conversion/open is
proved.** This follows the [lookup caller](TERRAIN_SHADER_FAMILY_ENTRY_CONSUMER_CONTRACT.md)
and [record accessor](TERRAIN_SHADER_FAMILY_ENTRY_ACCESSOR_CONTRACT.md).

## Identity, extent and complete scope

Executable SHA, gated before PE/VAs:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Complete55-byte body,24 instructions, SHA
`f1b4be7cfda63e76daa9ee145dfa6d9cecedaa6b4b4d776f1626fd34cb3d30a1`.
The [verifier](../../SWG-Godot-Client/tools/python/verify_terrain_shader_family_lookup_consumer.py)
checks independent bounded raw-PE/pefile mapping, complete hash,24 exact byte/
operand anchors, two raw extent fences and complete calls/return/reachability.
Frozen67 mapping/path/one-occurrence/model sources are SHA-gated before reuse;
no lookup/accessor/predicate/setter/serialization body suite repeats.

**Confirmed observation:** predecessor restoredEDI/ESI/EBX/ESP/EBP sequence
starts00B6B572, ends ret4 at00B6B578, followed by five NOPs through00B6B57F.
Selected pushEBP/movEBP,ESP starts00B6B580. The straight-line body restoresEDI/
ESI/ESP/EBP and ends ret12 at00B6B5B4; nine NOPs follow through00B6B5BF.
All24 instruction boundaries are reachable from that prologue, with no branch,
indirect call or local exceptional registration.00B6B5A8 is decoded after the
selector/family reads, not interpreted from raw E8 alone. **Strongly inferred:**
one complete containing function extent. No external symbol/debug range or
runtime/indirect entry authority is asserted.

| Complete inventory | Count |
| --- | ---: |
|Selected complete body / new tiny helper contracts|1 /0|
|Instructions / operand anchors / extent fences|24 /24 /2|
|Calls / branches / returns|2 /0 /1|
|All distinct function/handler leads|3|
|All raw inventory sites: calls+fences+return|5|
|Separate authorized lead/site ceilings|12 /12|

Leads:00B6B580,00B69860,00B69700. Sites:00B6B59D,00B6B5A8,00B6B572,
00B6B5B7,00B6B5B4. Bounded discovery found only the selected frame prologue;
no incidental function/handler lead or new helper was expanded. Instruction
operand anchors are a separate verification denominator. No scope amendment.
Raw executable-section E8 scanning finds **zero direct incoming candidates** to
00B6B580. This is not proof of runtime unreachability: exports, indirect calls,
other entry points and data references are not investigated.

## Exact caller inputs and unresolved selector

IncomingECX group receiver is saved inESI at00B6B58D. EDI is loaded from caller
argument one atEBP+8 (destination). Caller argument three atEBP+0x10 is loaded
intoEAX at00B6B584 and pushed at00B6B58C; caller argument two atEBP+0x0C
is loaded intoECX at00B6B58F. DestinationEDI is pushed at00B6B592, then that
second argument at00B6B593. ECX=ESI group at00B6B594;00B6B59D calls00B69860.

**Confirmed:** search-like pointer is directly supplied by caller argument three;
it is not locally built, transformed, read as bytes, cloned or normalized before
this first forward. Argument two is opaque input to00B69860; its key/type/error
meaning is Unknown. At selector entry, the visible stack prefix after its return
address is argument two, destination, argument three. **First unresolved boundary:**
00B69860 at00B6B59D. Its body is not inspected or executed.

After that call,00B6B5A2 reads group+4 throughESI;00B6B5A5 loadsECX from
that array atEAX*4. Thus the selector's EAX is used as an unchecked32-bit array
index on the normal path. No count, index sign/sentinel, null, length, version or
status check intervenes. The actual selected group array is read after the helper,
not captured before it. Mutation/invalid index/synchronization and helper error
behavior remain Unknown; no invalid memory is run.

## Pending stack ABI and returned record use

The caller does not push fresh arguments between the selector and00B6B5A8's
call to accepted00B69700. Its intended destination/search pair is already staged
below the selector's top argument. **Strongly inferred:** selector consumes one
stack word (ret4-like ABI), leaving destination/search as lookup arguments one/
two. The selector's cleanup and preservation of pending stack cells/ESI/EDI are
**not verified**. Stack position alone cannot prove its implementation. Models
explicitly supply this conforming ABI state; they do not convert it into original
parity or claim that any arbitrary selector effects still yield the same inputs.

Under those stated preconditions, ECX at00B6B5A8 is the indexed family receiver;
lookup gets original destination/search and fills the accepted three-word record.
Inherited67: lookup's match path explicitly returns destination; no-match fills
zero triple before unknown cleanup/EAX. This wrapper ignores lookupEAX and at
00B6B5AD setsEAX=EDI, then restores registers and ret12. **Confirmed local code:**
returns the currentEDI value after lookup. **Strongly inferred conforming ABI:**
returns the original destination on both lookup-result paths. This does not close
selector preservation, lookup cleanup/lifetime or exceptional returns.

The wrapper neither dereferences nor writes any returned record field/string
byte. There is no subsequent conversion/open/forward in the selected body:
aggregate destination is returned to an unestablished caller. Destination+0 family
word,+4 entry pointer,+8 entry raw word provenance is inherited66/67, not reanalyzed.
Actual argument-three producer outside this wrapper and returned-record consumer
remain Unknown. No resource semantics follow from any original string spelling.

## Ownership, failures and supplied-state model

Local ABI is ECX receiver, three stack inputs, saved/restoredESI/EDI/EBP, ret12.
Member-style group lookup wrapper is Strongly inferred; no class/RTTI or canonical
semantic name is assigned. No allocation, string copy, retain, deletion, output
status test, callback registration or resource open exists inline. External callees'
side effects, exception cleanup, retention/destruction, thread affinity and final
borrowed-pointer lifetime are unclosed.

The first helper runs after outgoing stack slots are staged and before family
selection/lookup. Failure or mutation there may precede or change the subsequent
selection; no rollback is shown. The wrapper makes no own record-store prefix:
field writes and cleanup occur only in inherited lookup/accessor bodies. Synthetic
changed-array/currentEDI and pointer alias mutation fixtures demonstrate supplied
state dependence; they are not executed original races/callbacks or legally allowed
callee ABI violations. Models reject out-of-range indices/short triples safely as
modern witness policy, not recovered original error handling.

`wrapper_witness(group_receiver, families, selector_input, search, destination,
selected_index, lookup_words, ...)` records selector stack prefix, selected family,
lookup destination/search, carried words, ignored lookupEAX and currentEDI return.
Optional post-selector array/EDI state makes the assumptions visible. It executes
no original helper and does not create a resolver or production client component.

## Reproduction and verification

[Portable runner](../../SWG-Godot-Client/poc/round68-tests/Assets/Invoke-SelectedRule.ps1):

```powershell
& ./poc/round68-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config
```

Explicit cached inputs, automatic fresh ignored output. [Independent peer](../../SWG-Godot-Client/poc/round68-tests/Assets/peer.py)
passes102 guards:7 identity,3 frozen-source,6 PE truncation,6 headers,7 mapped
extents,25 raw/operand,7 complete-body/extent,4 control/reachability,1 incoming
inventory,18 budget,8 path,6 safe-model policy and4 no-publication checks.
Twenty-six local and39 independent wrapper checks cover ordered stack-slot/
receiver/pointer identities, zero/nonzero raw words, ignored childEAX, changed
array/currentEDI and visible alias mutation. Existing positive output hashes survive
expected rejection. Portable RunRoot/sentinel and synthetic reparse metadata are
used; no actual junction/invalid-memory access is created.

Exactly one accepted1977-byte SGRP0006 occurrence, SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`,
15 families/35 entries, supplies35 **conditional** wrapper-record witnesses.
Selector index and conforming four-byte cleanup are supplied; inherited lookup
model gets nonzero-prefix/zero results. These are not actual original selector/
predicate outputs, duplicate-selection behavior, runtime pointers or resource use.
Config/source/TGEN SHA/priority42 remain pinned through frozen66/67 machinery.
Original strings/raw dumps stay ignored; no extra corpus.

Final gates: all4191 frozen public files unchanged; exactly five new owned
files, zero existing edits/deletions/unrelated differences. HEAD preserved/index
empty/private outputs ignored. UTF-8/no-BOM/LF/single-final-newline, Python AST,
PowerShell parse, per-file whitespace/focused diff and git diff --check pass.
Workspace validator:1706 Markdown files, zero broken/private links,2626 names/
zero duplicate addresses,345 packet rows, zero public files at least25MiB.
No native/GPU source/build changes or new build result claimed.


See [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R68_LOOKUP_CONSUMER.md).
Next exact probe is00B69860: establish selector input/index/error contract and
actual pending-stack/nonvolatile behavior, stopping at its first unresolved helper
or bounded extent uncertainty. Resource producer/use still needs an actual caller;
zero raw E8 references do not authorize an invented caller or resolver.
