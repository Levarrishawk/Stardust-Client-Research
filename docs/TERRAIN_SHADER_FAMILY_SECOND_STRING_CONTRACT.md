# Terrain shader-family second-string boundary

Round64, October5,2026 Eastern. The SGRP0006 second SFAM string is copied to
family+`0x14`, returned by a tiny getter, and read by one selected index-access
consumer. The trace stops at unresolved selector `00B69860` and the consumer's
outward pointer return. **No resource open/fetch or asset-name semantics is
proved.** The accepted [raw shader-family layout](TERRAIN_GENERATOR_SHADER_FAMILIES_CONTRACT.md)
and [conditional first-string comparison](TERRAIN_SHADER_FAMILY_PREDICATE_CONTRACT.md)
remain separate; no first-string/predicate analysis is repeated.

## SHA-gated evidence and scope

Executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
All addresses are baseline PE32 VAs. The
verifier (external historical reference) checks
complete ranges excluding alignment,42 raw-byte/register/operand anchors, four
direct calls and two conditional branches. Independent raw-PE mapping and pefile
mapping agree. Exactly one setter, one four-byte helper and one31-byte consumer
are expanded; no budget amendment is needed.

| Body | Bytes / instructions | SHA-256 |
| --- | ---: | --- |
|`00B69390..00B693E9` setter|90 /41|`4723759e4264a51dd5b4e1cc1422a49cd40984490c8810823613b32e6f62637d`|
|`00B69370..00B69373` getter|4 /2|`01d3abacb68aac3537badfe563d0e21d0244fb61db1d805bcef8f455407fad96`|
|`00B6B480..00B6B49E` selected consumer|31 /13|`544beabfd4ccb4dc944c9d35c048a6f504ac1bd83491475990a38bc9ea10ad44`|

Six distinct followed function targets are the three above, terminal
allocation/release targets `00AC15E0`/`00AC1660`, and terminal selector `00B69860`.
Six unique raw sites count the four followed callsites plus two unselected getter
candidates, with the selected getter site counted once. Both denominators are
enforced separately under cap12. Instruction anchors and prior loader sites are
not falsely substituted for fresh callee/site counts.

Raw rel32 discovery finds three getter candidates: `00B6AE0F`, `00B6AE18`,
`00B6B495`. Only the last one's selected consumer is expanded. The two others
are not given semantics or reachability claims. A similarly bounded raw scan
finds zero direct CALL/JMP candidates to `00B6B480`; this does not exclude
indirect, exported or other entry paths. **Runtime use of this consumer is
Unknown.** The body itself is a directly verified read consumer, not evidence
that the pinned corpus invokes it. Adjacent exploratory fragments are not
promoted into other full-body contracts.

## Second-string provenance, storage and ownership

**Observation / Confirmed:** in the already accepted0006 loader, sites
`00B6ABAF..00B6ABBD` pass the stack buffer and1000 to the NUL reader.
After the read, `00B6ABC2` takes the buffer address, `00B6ABC8` pushes it,
`00B6ABC9` sets ECX to the constructed family, and `00B6ABCB` calls
`00B69390`. These nine exact reused loader anchors establish the second string
independently of the first field's meaning. The accepted raw contract already
establishes that the NUL reader does not enforce the supplied length.

Setter ECX is saved in EBX at `00B69394`. The old pointer is read from+`0x14`
at `00B69396`; non-null invokes `00AC1660` at `00B6939F`, followed by
explicit field clearing at `00B693A7`. Input at stack+8 is loaded into ESI
at `00B693AE`. Null branches to return, leaving the cleared/zero field. Non-null
input is scanned until NUL by `repne scasb` at `00B693BD`; length+1 is passed
to `00AC15E0` at `00B693C6`. Dword and residual byte copying at
`00B693D4`/`00B693DE` copies all bytes including NUL. Returned allocation
pointer is stored at family+`0x14` at `00B693E0`.

**Confirmed structure:** byte-preserving copy, non-null empty string copies one
NUL, null is distinct from an owned empty string. No text transcoding, case
folding, path prefix, separator conversion or extension rewrite occurs in the
setter. **Strongly inferred ownership:** allocation/release roles at the terminal
calls; their bodies/failure policy remain unexpanded. Replacement removes the
old pointer from the field; validity of a previous borrow after the release is
not guaranteed. Input aliasing the old field has no local guard, and the old
release precedes source scan/copy. Such alias validity is unproved. No local
null-allocation check or maximum source length appears between allocation and
copy; exceptions/faults/allocator behavior are Unknown. These are legacy
observations, not requirements to copy unsafe behavior into a modern decoder.

## Getter and one selected consumer

**Observation / Confirmed:** `00B69370` loads ECX+`0x14` into EAX and returns.
It does not dereference the string, clone/retain it, normalize it or open a
resource. A valid family whose field is null returns EAX=0. The pointer is
borrowed at this boundary; complete teardown and global cache lifetime are not
newly established.

**Observation / Confirmed:** `00B6B480` loads its sole stack argument into EAX,
pushes that unchanged word at `00B6B487`, preserves group receiver ECX in ESI
at `00B6B488`, and calls `00B69860` at `00B6B48A`. Selector argument meaning,
selection/duplicate rules, valid range and side effects are Unknown. On return,
the body reads group+4 at `00B6B48F`, indexes the family-pointer array by EAX*4
at `00B6B492`, and calls `00B69370` at `00B6B495`. Thus the selector's full
32-bit EAX is used as an index without a local range/null/count check.

After the getter, only restored registers/frame and `ret4` remain. EAX is the
same getter-returned pointer, including null. There is no local fallback,
transform, retain, string read or resource call after that getter. No direct
open/fetch boundary is identified. The unresolved selector runs **before** the
field read and may have unproved effects; absence of an inline resource call
does not assert resource-free behavior inside the delegate or other callers.

**ABI / Confirmed:** setter and consumer use ECX receiver, one stack word and
callee `ret4`; getter uses ECX and plain `ret`. Getter/consumer pointer result
is in EAX. Consumer preserves ESI/EBP. Setter preserves its observed EBX/ESI/EDI/
EBP. Terminal allocation/release arguments are pushed words with caller cleanup;
selector's ECX receiver and opaque stack input are forwarded as observed.
**Strongly inferred:** conventional x86 thiscall members. Input word's semantic
type and a class/function name remain unassigned.

## Original bytes and deterministic tests

Exactly one pinned effective Tatooine SGRP0006 supplies15 second-string byte
witnesses,35 entries,1977 group bytes, SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`.
Config/terrain/TGEN identities, archive priority42, TGEN file+210 and SGRP
TGEN+24 are gated through the accepted structural/archive tools and documented
hashes. Each witness independently records its second-field offset, source/copy
SHA including NUL, and+`0x14` target. Original strings and detailed instruction
output remain ignored. Names are not parsed as resource paths or loaded.
Repeated replays reuse this occurrence, not additional corpus samples.

Thirteen synthetic local checks cover detached byte copies, empty/non-ASCII/
separator/NUL preservation, null and supplied-selector return identity. The
independent peer (external historical reference) checks12 copy/pointer
properties and111 negative guards:7 identity,3 sealed prerequisites,6 PE
truncation,6 PE headers,7 mapped extents,43 byte/operand anchors,6 complete-body,
6 control-edge,10 budget,8 output-path,5 finite synthetic witnesses,4 no-publication.
Returned null and distinct equal-content pointers are preserved in the model;
selector indices are explicitly supplied, not inferred from original requests.
Finite extent/index rejection is harness policy, not original error handling.

Portable existing-directory/non-directory fixtures are created under the passed
fresh RunRoot. Synthetic reparse metadata tests lexical ancestors without a real
junction. SHA gates prevent address inspection for wrong executable identity.
Sealed prerequisite hashes prevent silently reusing changed mapping/path code.
No first-string/predicate suite is executed; only SHA-gated reusable machinery is
used. Malformed inputs cannot publish a positive directory; rejecting an existing
positive output preserves every file hash. Both initial static verifier and fresh
full peer replay pass; no original executable or native/GPU production code runs.

Replay with cached inputs and a fresh default private directory:

```powershell
& ./poc/round64-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config
```

Runner (external historical reference) generates a fresh
UUID RunRoot when omitted and works in main after integration. Standalone flags:
`--exe`, `--packages`, `--asset-root`, `--config`, `--output`, optional
`--maximum-leads` (default12; this trace needs6). No native build outcome is
invented for static-only source/document additions. See the
[checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R64_SECOND_SFAM_STRING.md).

Next exact consumer probe: the other getter caller at `00B6AE0F`/`00B6AE18`,
locating and bounding its complete body before expansion, to test whether it
serializes the field, forwards it, or reaches open/fetch. A separate selector
probe can inspect `00B69860` to establish valid index/failure/duplicate rules.
Neither result permits calling the string an asset name without a directly
proved resource boundary. This worker ends64 and idles for lead acceptance.
