# Terrain shader-family first-string consumer boundary

Round62, October5,2026 Eastern. One bounded static trace connects the first
SGRP0006/SFAM string to an owned family buffer and one zero-result predicate.
It stops at unexpanded callee `0x01320590`. **Asset-name, comparison and material
semantics remain Unknown.** No production resolver is added. The accepted
[raw-family contract](TERRAIN_GENERATOR_SHADER_FAMILIES_CONTRACT.md) and its
opaque fields remain unchanged.

## Inputs and reproducibility

All addresses are VAs in PE32 baseline SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
The [static verifier](../tools/python/verify_terrain_shader_family_consumer.py)
gates that identity before parsing or reading an address. It verifies three
complete bounded byte ranges,79 decoded instructions, and52 exact instruction
anchors, including the already recovered0006 loader's first-string callsite.

| Range, excluding alignment | Bytes / instructions | SHA-256 |
| --- | ---: | --- |
| Setter `00B69310..00B69369` |90 /41|`1be622270690113c4b456f31d0125fda606bb974deb7b13830c0dfdf92c65088`|
| Tiny getter `00B69300..00B69303` |4 /2|`f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f`|
| Consumer `00B6B360..00B6B3A6` |71 /36|`8253dabf507f11e0fb35d335c0a3043aa6dbf4fa7630bc221de15b8ef8fd278b`|

Exactly one pinned effective Tatooine generator supplies byte witnesses: archive
priority42, terrain1188446 bytes/SHA
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`;
TGEN file offset210,139608 bytes/SHA
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`;
SGRP TGEN offset24,1977 bytes/SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`.
Configuration SHA is
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
The same occurrence is reread by the peer and positive verifier; it is not two
corpus occurrences. All original strings and detailed witnesses stay ignored.

## Observed pointer and byte flow

**Observation / Confirmed:** selected0006 loader `00B6AAF0` calls the NUL reader
at `00B6AB9C`, then takes the same stack-buffer address at `00B6ABA1`, pushes it
at `00B6ABA7`, sets ECX to the constructed family at `00B6ABA8`, and calls
`00B69310` at `00B6ABAA`. The second string reaches a different setter at
`00B6ABCB`; that setter is not expanded here. This trace concerns the first
string only. Other SGRP versions are not reanalyzed or given this consumer
contract merely because their setters have candidate references.

The already recovered loader keeps its group receiver in EDI at `00B6AB15`.
After a resized array copy it restores the group and family pointers at
`00B6AD10`/`00B6AD13`. It reads group count+8 and array+4 at
`00B6AD2D`/`00B6AD30`, stores that family at array[count] at `00B6AD33`,
then increments group count+8 at `00B6AD3E`. These seven reused loader anchors
join the constructed family to the same array/count offsets used by the selected
consumer. They add no fresh loader-body expansion or additional callee trace.

**Observation / Confirmed:** setter ECX is saved in EBX at `00B69314`; the old
family+4 pointer is loaded at `00B69316`. If non-null it is passed to `00AC1660`
at `00B6931F`, followed by explicit field clearing at `00B69327`. The sole stack
argument is loaded at `00B6932E`; null skips allocation/copy and returns.
For non-null input, `repne scasb` at `00B6933D` scans to NUL, the length is
adjusted to exclude NUL and then increased by one, and that size is passed to
`00AC15E0` at `00B69346`. Dword and residual byte copies at `00B69354` and
`00B6935E` copy the length including NUL; the allocated pointer is stored at
family+4 at `00B69360`. A non-null empty string therefore copies one NUL.

**Interpretation / Strongly inferred:** `00AC1660` and `00AC15E0` serve release
and allocation roles at these callsites. Their bodies, exceptional behavior and
global allocation policy remain unexpanded. **Compatibility consequence:** the
temporary input buffer is copied; a previously borrowed family+4 pointer is
invalidated by replacement. The setter has no local maximum-length or source
alias guard. If the caller passed the old owned buffer as input, release would
precede the length scan; validity of such calls is not proved. No allocation
failure branch appears between the returned pointer and copy; the underlying
failure policy is Unknown. These observations do not justify reproducing unsafe
legacy behavior in the modern decoder.

**Observation / Confirmed:** tiny getter `00B69300` consists of a load from
ECX+4 into EAX and `ret`. It returns that actual stored pointer, with no clone,
retain operation, normalization or lookup. It is a borrowed pointer at this
boundary; complete family teardown remains outside the new trace.

## One concrete consumer and stopping condition

**Observation / Confirmed:** consumer `00B6B360` preserves receiver ECX in EDI;
reads its signed count at receiver+8, and for count<=0 returns AL=0 without
reading the query. For positive count it loads the caller's opaque argument into EBX,
reads the pointer array at receiver+4, selects index ESI, and calls `00B69300`
with that family as ECX at `00B6B37A`. The returned first-string pointer is
pushed at `00B6B37F`, then the unchanged caller-supplied value is pushed at
`00B6B380`, before call `01320590` at `00B6B381`. Thus its stack argument one
is the caller-supplied value and argument two is the owned first-string pointer.
There is no copy or asset/path conversion along this inspected forwarding edge.

The call's full EAX is tested at `00B6B389`. Zero branches to `00B6B39E`,
which returns AL=1. Nonzero increments ESI and traverses the serialized pointer
array until its current signed count is exhausted, then returns AL=0. The first
zero result terminates the loop. Order is index0 upward; duplicate pointers or
strings are not removed in this body. A live count is reread after a nonzero
result; concurrent mutation and thread affinity are not established.

**ABI / Confirmed:** setter and consumer use receiver ECX, one stack argument,
callee `ret4`, and preserve the observed nonvolatile registers. Getter returns
the field pointer in EAX with plain `ret`. The unresolved callee receives two
pushed values followed by caller stack cleanup of8 bytes. The consumer sets
only AL at the final true/false exits, so callers must not assume normalized
32-bit EAX from this evidence. **Strongly inferred:** conventional x86 thiscall
member functions and a Boolean low-byte result. This is not a recovered class
name or a prototype for `01320590` beyond its observed call ABI.

**Unknown / stop:** the unresolved callee could plausibly compare strings, but
no case sensitivity, encoding, null handling, normalization, return convention
beyond the observed zero test, or asset lookup is inferred. There is no directly
proven resolver/asset boundary. The first callee argument is an unchanged32-bit
caller value; its pointer/string type is not established by this consumer, which
does not dereference it. No original query or consumer result is captured.
The bounded task ends at this first unresolved forwarding call.

## Verifier and witnesses

The verifier uses the accepted Round33 structural/archive tools for the one
pinned lookup. It independently reads all15 immediate SFAM first strings, checks
the35-entry remainder, and records source/copy SHA and group-relative offsets.
For each first string, a separately labeled synthetic copy witness preserves
all bytes including NUL. That confirms model byte preservation and original
field location, not execution of the original setter.

Synthetic consumer witnesses inject raw callee results explicitly. They check
opaque argument identity/order, repeated family pointers, first-zero stopping, nonzero
continuation and nonpositive-count bypass. They perform no comparison, shader
lookup or resource loading.23 local synthetic checks and eight independent
pointer checks pass. The [peer](../poc/round62-tests/Assets/peer.py) also runs82
negative guards:7 wrong identities,52 synthetic anchor mutations,3 bounded body
mutations,8 lead-budget,8 output-path and4 no-publication integration guards.
Linked-ancestor rejection is tested with synthetic filesystem metadata; no
actual junction is created. Outputs require a fresh Git-ignored directory below
this checkout's `local-private`, and evidence checks finish before publication.

Six distinct followed function targets are accounted under the12-lead cap:
selected setter, tiny getter, one consumer, terminal release/allocation calls and
terminal `01320590`. Raw rel32 candidate scans found nine setter sites and four
getter sites, not globally proven reachable functions. The unselected candidate
bodies are not expanded or semantically labeled. Exploratory adjacent/window
decodes are not additional recovered contracts. This ledger distinguishes
candidate sites from followed function targets and complete body expansions.

Replay with [Invoke-SelectedRule.ps1](../poc/round62-tests/Assets/Invoke-SelectedRule.ps1),
explicit cached Python/packages, original executable/asset/config paths and a
fresh private RunRoot:

```powershell
& ./poc/round62-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config -RunRoot local-private/round62/replay-new
```

The Python entry point accepts the same inputs as `--exe`, `--packages`,
`--asset-root`, `--config`, `--output`, with optional `--maximum-leads` (default12,
minimum6 for this trace). Public stdout is aggregate; detailed raw-byte witnesses
are written privately. No new dependency downloads, original process execution,
live contact, corpus sweep, shared registries or production shader behavior are
part of this result. See the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-05_R62_SHADER_FAMILY_CONSUMER.md)
for frozen scope and final validation. The exact next probe, after renewed
authorization, is the bounded body of `01320590` to establish its byte/comparison
rules; only then can this predicate be named semantically. Asset resolution
requires a separate direct consumer of a proven resource-name field.
