# Terrain shader-family predicate: conditional byte comparison

Round63, October5,2026 Eastern. `01320590`, reached by the accepted
[first-string consumer](TERRAIN_SHADER_FAMILY_CONSUMER_CONTRACT.md), has a closed
ASCII comparison branch and a separate unclosed delegated branch. This proves
the previously opaque caller argument is used as a byte-string pointer. It does
**not** prove that either string names an asset or that a material resolver runs.

## Identity and bounded evidence

Baseline executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
All addresses are VAs for that PE32 image. The selected body is exactly
`01320590..0132065F`,208 bytes,85 instructions, SHA
`ad7f167f1af74dd4aa9d8bdd3e65e868dd15586b88bc6f535ba112b61b7bd966`.
The verifier (external historical reference) gates
identity before PE parsing or VA reads, independently maps raw PE headers and
sections, crosschecks pefile's mapping, verifies53 raw-byte/decoded-operand
anchors and the full body hash, and reconciles four direct call sites and12
direct branches with decoded instruction boundaries.

Four distinct function targets and four direct callee sites are recorded
separately under cap12. Only the selected predicate is expanded; no tiny forward
or delegate body is expanded. Calls are:

| Site | Unexpanded target | Observed input |
| --- | --- | --- |
|`01320600`|`013234B3`|stack word `0x13`|
|`01320624`|`0131D757`|first string's byte-bearing value|
|`0132062E`|`0131D757`|second string's previously pushed byte-bearing value|
|`01320651`|`01323514`|stack word `0x13`|

No whole-section xref/corpus scan or further graph walk is required. The accepted
caller `00B6B360`,71 bytes, is gated by its existing SHA
`8253dabf507f11e0fb35d335c0a3043aa6dbf4fa7630bc221de15b8ef8fd278b`,
without a fresh caller-body expansion. Frozen62 consumer verifier and durable
contract bytes are also SHA-gated before reuse; their hashes are constants in
the new verifier and included in its receipt.

## Argument roles and ABI

**Observation / Confirmed:** `01320596` loads stack argument two into ESI;
`01320599` loads argument one into EDI. The zero-mode loop reads a byte from
ESI at `013205B0`, then a byte from EDI at `013205B3`; the other mode likewise
reads at `01320618`/`0132061B`. Thus on the valid read path argument one is a
byte-string pointer and argument two is a byte-string pointer. In the accepted
consumer, argument one comes from its caller, and argument two is the actual
owned first-SFAM-string pointer returned by `00B69300`. The earlier opaque
argument is now structurally typed by dereference, not by a guessed callee name.
Its producer, user-facing meaning and resource role remain Unknown.

The function preserves EBX/ESI/EDI, establishes/restores EBP, and returns through
plain `ret` at `0132065F`. The existing caller cleans up8 bytes. **Confirmed
ABI:** two stack arguments, pointer reads, EAX result, caller cleanup.
**Strongly inferred:** conventional x86 cdecl byte-string comparison routine.
No recovered class/function name is entered in the shared registry.

There is no null-pointer check, pointer-equality shortcut, explicit length,
maximum string bound, size parameter or error sentinel before byte reads.
Identical pointers still undergo reads. Invalid addresses or a missing terminator
on an otherwise equal prefix have no local controlled rejection path; actual
fault/handler behavior is not executed or inferred. The function reads both
bytes before deciding mismatch or termination, with the second argument read
first. Readable extent and stable input storage are caller preconditions for the
closed contract. No string buffer writes or ownership transfers appear in this
body; the delegated calls receive scalar values, not the source string pointers.
Behavior of the unexpanded delegates and concurrent source/global mutation is
outside the new contract.

## Closed branch: mode word equals zero

**Observation / Confirmed:** `0132059C` takes address `019AA108` and
`013205A2` compares its DWORD+8 (`019AA110`) to zero. Equal selects the inline
branch; nonzero branches to `013205E3`.

In the zero branch, raw equal bytes bypass folding. The arithmetic at
`013205BA..013205D4` transforms only ASCII `A..Z` to `a..z`: subtract65
modulo256, unsigned compare against26, mask the borrow-derived byte with32,
add that mask, and restore65 modulo256. All other bytes, including128..255,
remain unchanged. Two independent formulations agree for all256 byte values.

**Interpretation / Confirmed by byte algebra and branches:** compare unsigned
folded bytes lexicographically. Continue while equal and nonzero; return zero
at equal NUL. At the first folded mismatch, `013205D6` supplies the unsigned
borrow, `013205DA`/`013205DC` form signed low-byte -1 or+1, and
`013205DE` sign-extends that byte to EAX. Result is exactly:

| Condition | EAX |
| --- | ---: |
|first folded string is less|`-1`|
|equal through first NUL|`0`|
|first folded string is greater|`1`|

This is sign normalization, not the arithmetic difference between byte values.
Prefixes order by NUL against the next nonzero byte; bytes after an equal NUL
are ignored. ASCII case differences alone compare equal. Non-ASCII case pairs,
slash/backslash, punctuation and path segments are not normalized. No UTF-8,
Unicode, locale table, archive lookup or path parsing occurs in the zero branch.
The inspected caller tests EAX==0 and returns AL=1 for the first family that
matches under the selected comparator; positive/negative mismatch both continue.
This gives conditional case-insensitive first-string membership, not asset lookup.

## Unclosed branch: nonzero mode word

**Observation / Confirmed:** alternate entry increments DWORD `019BCFC8`
atomically and tests signed DWORD `019BCFC4`. For a nonpositive value it pushes
marker0. For a positive value it decrements the counter, calls `013234B3`
with `0x13`, and replaces the marker with1. Completion either decrements the
counter for marker0 or calls `01323514` with `0x13` for marker1.
**Strongly inferred:** synchronization around process-dependent byte policy.
Exact lock/counter/global meanings and the delegates' failure/reentrancy rules
are Unknown. This task stops at those direct forwarding edges.

Raw equal bytes bypass the transform calls. When bytes differ, the first byte's
value is passed to `0131D757` at `01320624`, the return is saved in EBX,
then the second's saved stack value is passed at `0132062E`. Only BL/AL are
compared at `01320636`. A transformed mismatch is normalized to -1/+1 by
`0132063A..0132063C`; equality loops. The initial EAX/EBX setup makes the
first raw bytes unsigned0..255. After delegate calls, subsequent byte loads
overwrite only AL/BL, leaving any returned upper bits intact. If a delegate
returned an equal low-byte NUL with nonzero upper EAX, the local termination
path can retain that full value. Therefore a global all-mode result set or
zero-extension/termination rule cannot be asserted without the delegate contract.
Raw equal NUL before any transform returns zero. After a transform, even later
raw equal NUL can retain upper bits from its return because byte loads do not
clear them. A universal locale/case-folding interpretation is not proved.

The mode/counter addresses are in a virtual-only tail of `.data`: no original
file bytes back those four-byte ranges. The independent mapper rejects reading
them as file data rather than inventing zero values from an empty read. Runtime
initialization and later writes are not traced. No claim that every installed
call selects the zero branch follows from the static image.

## Witnesses, limits and replay

The portable peer (external historical reference) passes127 negative guards:
7 wrong identities,3 sealed-input,6 PE truncation,6 PE-header,7 mapped-extent,
54 raw/operand-anchor,6 body/caller,16 control-edge,10 lead/site-budget,
8 output-path and4 no-publication integration checks. Reparse checks use
synthetic metadata; no actual junction is created. Existing-directory and
non-directory-ancestor fixtures are created under the supplied fresh RunRoot,
so they do not depend on worker scratch files. Failed inputs cannot publish a
new positive output, and existing positive bytes remain unchanged on rejection.

Local arithmetic witnesses cover256 folds and65,536 ordered byte pairs;
11 byte-string vectors include empty strings, NUL tails, prefixes, punctuation,
non-ASCII and long equal prefixes. Five null/finite-extent witness rejections
are harness limitations, not original rejection behavior. An independent
prefix-sequence oracle agrees on65,541 comparisons. These are independently
authored deterministic static/algebra fixtures, not original CPU execution.

One sealed original SGRP0006 occurrence is reused through the SHA-gated62
verifier: group1977 bytes, SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`,
15 families/35 entries. Thirty conditional zero-mode model comparisons test
self and privately derived ASCII-case variants of its first strings. They
establish byte-model applicability, not an observed original query, runtime
mode, consumer result, shader/material meaning or new corpus coverage. Original
strings and instruction details remain ignored; public stdout is aggregate.

Replay Invoke-SelectedRule.ps1 (external historical reference)
with cached Python/static packages and explicit original inputs:

```powershell
& ./poc/round63-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config
```

Omitting RunRoot generates a fresh private directory, so the command is reusable
from main. The standalone verifier accepts `--exe`, `--packages`, `--asset-root`,
`--config`, `--output` and optional `--maximum-leads` (default12; required minimum4
for this exact trace). No native/GPU production component changed, so native
build results are not newly claimed. No downloads or original execution occurred.

The [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-05_R63_SHADER_FAMILY_PREDICATE.md)
records scope and gates. Next semantic probe: a separately bounded inspection of
`0131D757`, stopping if its body exceeds the authorized tiny-forward scope or
delegates further. Next asset-consumer probe: start from the already observed
second-SFAM-string callsite `00B6ABCB ->00B69390`, establish its stored field,
then follow one directly proved read consumer to the first open/fetch boundary.
No resource-name role is assumed from either string's spelling. This worker
finishes63 and idles for lead acceptance before another assignment.
