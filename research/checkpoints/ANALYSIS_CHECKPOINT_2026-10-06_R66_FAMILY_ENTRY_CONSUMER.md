# Round66: family-entry accessor and selected caller

Date: October6,2026 Eastern/UTC. Lead dispatch completed03:59:46UTC; first exact
worker clock read04:01:04UTC (after sync/orientation/probe). Earlier start time is
not inferred from file timestamps. Question: which record bytes/pointer does
candidate00B692A0 expose, and does one directly proved caller use it as a resource?
Stop after the complete accessor and one caller, first unresolved helper/forward,
or a reproducible negative finding. No new corpus or setter/predicate/output work.

## Synchronization and orientation

First ran supplied main round66 sync-worker.py Assets with cached Python from the
existing520a checkout. PASS4123 frozen public files; incoming/prior/actual-own-final
public hashes and archive/member/case/size/path/ancestor/link/reparse/root checks
passed before writes and complete postflight passed. No conflicts/alternate sync.
Frozen manifest SHA
`b6fc2f1db7ebe4a7513b089fca2f68c23824e234e349ce2852b29d99804318f3`;
ZIP `292d84bcfd2180121f82f14b8958f0b2d6be4865e4878cba13287ba221ba98ed`.
Main HEAD49378e7eb5372e32be940239867ae66ceeb3f88c;
worker HEAD65de8c88141071744dc75594be126b3bab3696a5; empty index.

Read AGENTS, START_HERE/latest accepted65 session, raw SGRP/second-string/caller
contracts and prior61/65 checkpoint/tool material; searched status/ledger/UNKNOWN,
curated provisional-name rows and prior checkpoints by addresses/concept. Existing
candidate has no canonical semantic label. Verified executable SHA before static VAs:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Frozen65 verifier plus61/33 one-occurrence decoder sources are separately SHA-gated
before import. Reuse only mapping/path/raw decoding; no old static suite rerun.

Only new assigned verifier (external historical reference),
[contract](../../docs/TERRAIN_SHADER_FAMILY_ENTRY_ACCESSOR_CONTRACT.md), this checkpoint,
and two own66Assets runners. All4123 baseline files/shared hosts/registries/ledgers
remain frozen. Lead owns canonical integration. No unfinished66 sibling dependency,
original execution/live contact/capture/fuzz/download/external mutation or Git writes.

## Complete evidence and bounded amendment

Accessor00B692A0..00B692F3:84 bytes/30 instructions, SHA
`9bc611f07cae269fe4656d82cdbc556288f3076896cff4eb3c2635c731936608`.
Selected caller00B69B40..00B69B6A:43 bytes/18 instructions, SHA
`58ad4d3736af830699184b9de4f8f991412c4f9c7f42b073f5a2eec9c6a5ddd4`.
Complete hashes and all instruction bytes/operands, call/branch inventory and
independent raw-PE/pefile mapping are checked. Exactly two selected bodies.
Four function leads:both bodies,00B69160 and00B69170. Three decoded callsites
B692B3/B692E8/B69B5D plus raw unexpanded candidateB69779 give four raw sites;
separate12 ceilings enforced. No widening into additional caller/callee bodies.

The lead explicitly approved only tiny00B69160 to establish the receiver
provenance prerequisite, with no inventory increase. Its complete13 bytes/
6 instructions through00B6916C have SHA
`1e09d34a9de7a48bd061703ef73f341df8596595468b282c5f19c36b6bf0b98d`.
It copies incomingECX (the local record address) intoEAX, zeroesECX, writes zero
to local+0,+4,+8, and returns. EDX is untouched; **ECX is not preserved**.
The accessor saved its family receiver inEDX before this call, then explicitly
reloadsECX from family+0 afterward. These exact effects establish receiver
provenance without assuming normal volatile-register preservation. Two selected
bodies plus one approved tiny helper,54 instructions/anchors total,3 calls and
zero branches.00B69170 and the other candidate caller remain unexpanded.


Raw E8 executable-section scan has exactly two candidates for00B692A0:
00B69779 and00B69B5D. The latter is a proved instruction inside the selected
complete wrapper. The former's body/instruction-boundary/semantics/reachability
remain unexpanded. This inventory says nothing about indirect/export callers.
One initial private probe failed because the inherited mapper stores section
tuples rather than dictionaries; correction used its documented tuple schema.
A second private harness missed its Path import; correction preceded useful output.
These tool failures were not evidence or original execution.

## Findings, confidence and consequences

| Observation | Interpretation/confidence | Implementation consequence |
| --- | --- | --- |
|family+0 and selected12-byte entry+4/+8 fill destination+0/+4/+8|Structural record flow Confirmed with verified EDX preservation; field meaning Unknown|Keep family word, entry pointer and entry word distinct; do not infer filenames|
|wrapper selects group+4 with family index, passes destination/entry index, reloads EAX=destination|Confirmed local call and normal return; aggregate accessor role Strongly inferred|Wrapper supplies no resource/string use; accessor EAX after cleanup unknown|
|pointer carried unchanged; no inline byte read/clone/retain|Confirmed local absence; end-to-end lifetime Unknown|Borrowed model only; stop at00B69170 for cleanup effects|
|no count/null/sign/version/status test; wrapped index arithmetic|Confirmed local control; safe rejection Unknown|Do not replace legacy absence with a claimed bounds policy; model invalid addresses without dereference|
|stores+0,+4,+8 precede cleanup helper|Confirmed normal store prefix; exception recovery Unknown|No atomicity/rollback claim or captured fault behavior|

This is a useful negative resource finding: candidate accessor and selected
caller only expose/forward structural records. Accepted65 also uses the wrapper
from00B6AEB9 in its serializer-shaped caller; a distinct runtime resource context
is not proved by selecting this non-serializing local wrapper body. No conversion/open/fetch boundary
is reached. No production resolver or registry renaming follows. Durable details
and field/store/ABI addresses are in the contract rather than duplicated here.

## Reproduction and verification

Portable runner (external historical reference):

```powershell
& ./poc/round66-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config
```

Explicit cached executable/packages/assetroot/config; automatic fresh ignored output.
One accepted original SGRP0006 occurrence1977 bytes, SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`,
15 families/35 entries, yields35 raw-word/string-hash symbolic record witnesses.
Config/source/TGEN SHA/priority42 gated. This is static provenance plus model
forwarding, not actual memory/CPU/string loading. Original content stays ignored.

Final replay passes142 negative guards:7 identity,9 sealed-source,
6 PE truncation,6 PE headers,7 mapped extents,55 raw/operand,15 complete-body,
4 control-edge,3 raw-candidate inventory,18 lead/site-budget,8 output-path and
4 no-publication checks. Thirty-four local and27 independent synthetic checks
cover initializer effects, pointer/raw-word/destination identity,32-bit wrap and
alias mutation. Existing positive bytes survive expected rejection. Portable
RunRoot/sentinel and synthetic reparse metadata fixtures are used; no real
junction or invalid-memory dereference is created. Initial pre-amendment replay
is retained separately and does not claim helper provenance.

All4123 frozen public files remain unchanged; exactly five new owned files,
zero existing edits/deletions/unrelated changes. HEAD unchanged/index empty;
private outputs ignored. UTF-8/no-BOM/LF/single-final-newline, Python AST,
PowerShell parser, per-file whitespace, focused diff and git diff --check pass.
Validator:1686 Markdown files, zero broken/private links,2626 names/zero duplicate
addresses,345 packet rows, zero public files at least25MiB. No native/GPU build
result is claimed for this static-only milestone.


No native/GPU source changed; native builds and original execution are not new
verification results. Synthetic pointer mutation tests aliasing, not original
thread/lifetime or unsafe-index execution. Inherited raw loader/append provenance
is accepted evidence, not freshly expanded bodies.

## Next precise probe

Independently prove00B69779's instruction boundary and bound its containing
body; follow its selected entry pointer to the first unresolved use. This is the
remaining actual raw call candidate, not a proved resource consumer. If lifetime
is instead required, separately bound00B69170. Do not widen into both paths or
name from resource-looking original strings. Worker seals66 then idles pending
explicit lead acceptance/next assignment; research/readiness/function/protocol
metrics remain separate and unchanged.
