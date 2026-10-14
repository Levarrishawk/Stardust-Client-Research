# Second SFAM string caller: structured-output forwarding

Round65, October5,2026 Eastern. The caller containing `00B6AE0F`/`00B6AE18`
is the complete body `00B6AD70..00B6AF3F`. It traverses families and fields in
SGRP/0006/SFAM-shaped output order and forwards the second string to `00A949D0`.
**Serialization is Strongly inferred; exact output-helper behavior remains
Unknown.** No string-to-resource conversion or open/fetch boundary is proved.
This extends the [second-string field/getter contract](TERRAIN_SHADER_FAMILY_SECOND_STRING_CONTRACT.md)
without repeating its setter or the first-string predicate investigation.

## Input identity, scope amendment and accounting

PE32 executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
The selected464-byte body has156 decoded instructions, SHA
`5466348f6df3dc7feb7b240b0c5adb698469854692655b7403c267038697ef5b`.
The [verifier](../../SWG-Godot-Client/tools/python/verify_terrain_shader_family_second_string_caller.py)
gates identity before PE/address inspection, checks independent raw-PE/pefile
mapping,74 exact byte/operand anchors, the full body hash and decoded control
boundaries. The accepted four-byte getter is checked by its existing SHA, without
a fresh body expansion. Frozen64 machinery is SHA-gated before reuse/output.

The original12-lead/site ceiling cannot hold this complete body:28 direct call
sites reach16 distinct targets, plus the selected function gives17 function
leads. A separate executable SEH target `014D2508` is referenced at `00B6AD75`.
The lead first approved17/28 direct-function/callsite accounting, then explicitly
superseded it with **18 function/handler leads and29 raw sites**, including that
unexpanded compiler reference. This narrow amendment permits only this complete
caller inventory and no target-body expansion. The final guards enforce18/29;
original12, under-cap17/28, over-cap, duplicate and unrelated inventories reject.

| Complete denominator | Count |
| --- | ---: |
|Selected complete body / new helper bodies|1 /0|
|Decoded instructions / raw-operand anchors|156 /74|
|Direct call sites / distinct direct targets|28 /16|
|Primary+direct function leads / separate SEH reference|17 /1|
|Total code leads / total call+compiler-reference sites|18 /29|
|Direct conditional/unconditional branch sites|7|

No denominator is replaced by the smaller second-string slice. SEH target is
classified as executable code by PE metadata but not read or expanded. Adjacent
functions in exploratory windows are not new contracts. Runtime reachability,
callers of the selected function and indirect/export usage are not investigated.

## Full-body shape and exact second-string flow

**Observation / Confirmed:** ECX receiver is saved in EDI, later in a stack local;
sole stack argument+8 is saved in ESI. Calls pass SGRP and0006 tags to `00A947A0`
at `00B6AD9E`/`00B6ADAC`. A count-like EAX from `00B69B10` is tested signed;
nonpositive bypasses families. Positive traversal uses receiver array+4, ascending
index, and passes SFAM to `00A94810` at `00B6ADE2`.

Each family path forwards an accessor's word-sized local to `00A94870` with
immediate4, forwards the first-string getter result to `00A949D0`, then handles
the second string. After it, three separately addressed byte-sized fields are
forwarded with immediate1; two x87 getter results are stored in four-byte locals
and forwarded with immediate4; a count-like EAX and each entry's local string/
word follow. Signed nonpositive entry count bypasses that loop. Families and
entries advance in index order. SFAM and outer0006/SGRP tags are supplied to
closing-shaped helpers afterward. Exact helper semantics, output encoding,
float/NaN preservation and overall byte round-trip are not established merely
from pointer/size-like arguments. No production serializer is implemented.

**Interpretation / Strongly inferred:** structured IFF serialization, supported
by tag sequence, field widths/order matching the accepted original schema, and
output-like receiver passed consistently to the forwarding helpers. The incoming
ESI object is an opaque receiver at this evidence level; no RTTI/class or resource
handle is inferred. Helpers remain unexpanded, including the first body forward
`00A947A0`. The selected string reaches the first unresolved string-specific
forward `00A949D0`, which is the task's stopping boundary.

**Observation / Confirmed:** `00B6AE0D` selects the actual family as ECX;
`00B6AE0F` calls accepted getter `00B69370`. `00B6AE14` tests EAX.
At zero, `00B6AE16` branches to `00B6AE1F`, loading address `01908090`.
At nonzero, `00B6AE18` calls the getter again without changing its receiver and
`00B6AE1D` joins the common path. `00B6AE24` pushes the chosen EAX pointer;
`00B6AE25` sets ECX to the incoming ESI object; `00B6AE27` calls `00A949D0`.
Thus this endpoint receives one pointer stack argument and the same incoming
output-like receiver. No intervening byte read, case fold, path construction,
encoding conversion, allocation, retain or resource lookup is observed in this
slice.

| Supplied getter results | Local behavior |
| --- | --- |
|first is null|one read; forwards fallback address01908090|
|first is non-null|two reads; forwards second result unchanged|
|first is non-null, second becomes null|still forwards null; no second check|
|first is non-null empty-string pointer|takes the non-null/two-read path|

The two-read behavior is not an atomic snapshot. Mutation between reads could
change or invalidate the returned pointer; thread affinity, synchronization and
whether such calls are legal remain Unknown. The model supplies distinct read
results to witness this local control, not to claim an original race was run.

## Fallback, ABI, ownership and failure limits

`01908090` is in a virtual-only `.data` tail, not backed by file bytes. The caller
loads its address without reading content. **Confirmed:** fixed fallback pointer.
**Strongly inferred:** conventional shared empty-string fallback, consistent
with the existing [loader-zero string evidence](CREATURE_WEARABLES_VIEW6_CONTRACT.md).
Runtime initialization/writes/content at this use remain unproved. The verifier
rejects file-byte reads of this address instead of treating an unrelated raw-file
offset or empty read as a literal. No fallback-content byte fixture is invented.

**ABI / Confirmed:** caller uses ECX receiver, one stack argument, restores
EBX/ESI/EDI/EBP and registered FS exception state, and ends with `ret4` at
`00B6AF3D`. Field/string forwarding uses ESI as ECX. String pointers are pushed
without visible caller cleanup afterward, consistent with the accepted getter's
preserved ECX and member-style forwarding. **Strongly inferred:** thiscall
serialization member and member-like output helpers. Helper prototypes beyond
observed inputs/stack flow remain unexpanded.

No ownership transfer or clone occurs in the selected string slice; its pointer
is borrowed as established in64. Delegate retention/copying remains Unknown.
There is no explicit status/error result check after the string forward or other
output-shaped calls. SEH presence does not prove rollback or exception cleanup.
Ordinary count-gated paths still close outer-shaped frames; failed-helper paths
are unexecuted. EAX after the final helper is not normalized or tested into a
success result by this body. Null fallback first-read handling does not establish
that `00A949D0` accepts null when the second read changes. No controlled fault,
resource-open failure, byte-output or teardown guarantee is inferred.

## Reproduction and witnesses

The [portable peer](../../SWG-Godot-Client/poc/round65-tests/Assets/peer.py) passes175 negative guards:
7 identity,3 sealed prerequisite,6 PE truncation,6 header,7 mapped extent,
75 byte/operand,6 complete-body/getter,35 complete control-edge,
18 amended lead/site budget,8 output path and4 no-publication guards.
Ten local and ten independent synthetic forwarding checks preserve actual
supplied pointer/receiver identities and one-versus-two reads, including changed
and null second results. Portable path fixtures are created below the explicit
RunRoot; reparse guards use synthetic metadata, not an actual junction. Existing
positive output bytes are unchanged by expected rejection.

One pinned original SGRP0006 occurrence,1977 bytes/SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`,
15 families/35 entries, supplies15 second-string byte-flow model witnesses.
Config/source/TGEN identities and original field extents are asserted by the
SHA-gated64 tool. Model reads return the same non-null source object and its
bytes reach the symbolic forward unchanged. This is not original caller execution,
captured pointer/state, observed output encoding or resource loading. Original
strings and instruction output stay ignored; no additional corpus is read.

Replay with explicit cached inputs and automatic fresh private output:

```powershell
& ./poc/round65-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config
```

[Runner](../../SWG-Godot-Client/poc/round65-tests/Assets/Invoke-SelectedRule.ps1) is portable to main.
Standalone flags are `--exe`, `--packages`, `--asset-root`, `--config`, `--output`,
optional `--maximum-leads`/`--maximum-sites`; defaults are the approved18/29.
No native/GPU source or build result changes. See the
[checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R65_SECOND_STRING_CALLER.md).

Next leaf probe, if byte output matters: separately bound `00A949D0` for its
NUL/null/copy behavior, stopping at the next delegate. Next resource-consumer
probe should move away from these serialization-shaped callers: inspect the
family-entry accessor candidate `00B692A0`, then one directly proven caller
outside `00B6AD70`, establishing string provenance before tracing to an actual
open/fetch boundary. An entry string's resource-looking spelling is not proof.
No shared registry or production resolver is changed; worker idles after65.
