# Family-entry accessor and group wrapper

Round66, October6,2026 Eastern/UTC. The selected candidate `00B692A0` constructs
an output record; selected caller `00B69B40` merely selects a family and forwards
that record. **The resource-consumer hypothesis is not established.** No resource
name, conversion, open/fetch, production resolver or original execution is added.
This follows the [raw SGRP layout](TERRAIN_GENERATOR_SHADER_FAMILIES_CONTRACT.md)
and the [Round65 stopping boundary](TERRAIN_SHADER_FAMILY_SECOND_STRING_CALLER_CONTRACT.md).

## Scope and evidence

Executable SHA-256, checked before VAs:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

| Complete selected body | Extent | Instructions | SHA-256 |
| --- | --- | ---: | --- |
|Accessor00B692A0|84 bytes through00B692F3|30|9bc611f07cae269fe4656d82cdbc556288f3076896cff4eb3c2635c731936608|
|Caller00B69B40|43 bytes through00B69B6A|18|58ad4d3736af830699184b9de4f8f991412c4f9c7f42b073f5a2eec9c6a5ddd4|

The [verifier](../../SWG-Godot-Client/tools/python/verify_terrain_shader_family_entry_accessor.py)
checks complete body hashes, every decoded instruction's exact bytes/operands,
all direct calls and absence of branches, independent bounded raw-PE and pefile
mapping, plus complete raw E8 candidate inventory for the accessor. Its frozen
mapping/path and one-occurrence decoding dependencies are SHA-gated. Reuse does
not rerun setter, predicate, serializer, prior native suites or another corpus.

Two complete selected bodies; one explicitly approved tiny prerequisite helper. Four distinct function leads: the two bodies and00B69160/00B69170.
Four distinct raw sites:00B692B3,00B692E8,00B69B5D and00B69779; each denominator
remains below the separate12 ceilings. No SEH/indirect edge occurs in these bodies.
The other raw candidate00B69779 is not decoded into a caller body or assigned a
semantic name. Raw E8 matching alone does not prove its instruction boundary,
reachable use, indirect callers or exports.

## Approved prerequisite helper

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

## Observation and interpretation

At00B692A6 the incoming ECX family receiver is saved in EDX. A local four-word
area is reserved; its last word atEBP-4 is set to zero before the first helper
call at00B692B3, receiving the local area asECX. The body then reads family+0
and family+0x20. Stack argument two atEBP+0x0C is multiplied by3 then4, with
32-bit wrap, and added to that entry-array pointer. It reads selected entry+4
and entry+8, keeping a local triple of family word, entry pointer and entry word.
Verified helper EDX preservation connects the saved incoming family receiver
to these reads; ECX is explicitly reloaded afterward. No ABI is guessed from convention.

Stack argument one atEBP+8 becomes ESI destination. Stores at00B692DA,
00B692DF and00B692E2 fill destination+0,+4,+8 from that triple, respectively.
Then00B692E8 calls00B69170 withECX pointing to the local area. This is the
first unresolved post-fill helper. Its content, pointer lifetime, exception and
retention behavior are not inspected. The body restoresESI/EBP and ends ret8.
It does not reload EAX after the helper: **its EAX return after cleanup is Unknown**.

The complete selected wrapper00B69B40 reads group+4, selects its family pointer
using stack argument two atEBP+0x0C with stride4, and supplies that asECX.
It pushes stack argument three atEBP+0x10 (entry index), then argument one
atEBP+8 (destination), calling00B692A0 at00B69B5D. It explicitly loads
EAX=ESI destination at00B69B62 afterward, restoresESI/EBP and returns ret12.
**Confirmed:** the wrapper returns the supplied destination. **Strongly inferred:**
member-style aggregate-return accessor with a caller-supplied output buffer.
No named source signature, class/RTTI, compiler return convention or resource
operation follows from that inference.

The earlier accepted loader/append evidence correlates family+0 with the first
raw SFAM word, entry+4 with its owned NUL string pointer, and entry+8 with the
entry's raw word. **Confirmed structural provenance** follows the verified EDX preservation
and accepted ordered-entry layout;
entry string's asset/path/shader meaning remains **Unknown**. Destination+0 is
read from the family itself, not selected entry+0. These fields must remain
separate even where real bytes coincide.

## Bounds, ownership and failure limits

Neither selected body tests family or entry count, null pointers, index sign,
record/string length, versions/discriminators, return status or destination size.
Index arithmetic is32-bit wrapping; negative/out-of-range indices are not safely
rejected by these bodies. No malformed-input execution is performed. Modern
model witnesses supply in-bounds records; arithmetic vectors only witness address
calculation, without dereferencing invalid addresses or inventing a legacy policy.

No string byte is read, normalized, compared or cloned in either selected body.
The triple carries the existing entry pointer. A synthetic mutation remains visible
through that same pointer; this is a borrowed-identity model, not an executed
lifetime proof. No allocation/free/retain exists inline. The unexpanded00B69170
may affect the local record or pointed storage; end-to-end retention/destruction,
thread affinity, concurrency and exceptional cleanup remain Unknown. A zero
fourth local word is observed, not assigned an ownership-flag meaning.

Normal destination-store prefix is+0 then+4 then+8, before the helper. An exception
or invalid memory could occur after a partial prefix; no rollback or atomic output
publication is visible or tested. Caller output is returned only on normal return.
The wrapper itself does not read or use the entry string bytes. Accepted65 also records
00B6AD70 calling this wrapper at00B6AEB9; that inherited context confirms the
wrapper can support serialization. Its local body is not a serializer, and no
separate runtime resource context is proved here. Therefore this
round closes record access and forwarding, while preserving the negative finding
that the selected caller supplies no resource/conversion evidence.

## Reproduction and final verification

Replay using the [portable runner](../../SWG-Godot-Client/poc/round66-tests/Assets/Invoke-SelectedRule.ps1):

```powershell
& ./poc/round66-tests/Assets/Invoke-SelectedRule.ps1 -Python $Python -Exe $Exe -Packages $CachedStaticPackages -AssetRoot $AssetRoot -Config $Config
```

Explicit executable/cache/root/config inputs; automatic fresh ignored output.
[Peer](../../SWG-Godot-Client/poc/round66-tests/Assets/peer.py) independently checks raw-word/pointer/
destination identity, 32-bit wrap and observable synthetic alias mutation. Original
witnesses reuse exactly one accepted1977-byte SGRP0006 occurrence, SHA
`C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`,
15 families/35 entries. All35 entries supply raw family/entry words and byte-string
hash witnesses through symbolic records. Config/terrain/TGEN identity and
priority42 are checked. This is not captured runtime memory, original calls,
resource loading or full byte-output parity. Original names and instruction dumps
remain ignored; public code contains only independently authored models and
small necessary structural anchors.

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


[Checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R66_FAMILY_ENTRY_CONSUMER.md)
records scope, amendment, validation and precise resume. Next actual-call candidate
is00B69779: independently prove its instruction boundary and bound its containing
body, following returned entry-pointer use only to its first unresolved forward.
If cleanup/lifetime is the blocker instead, separately bound00B69170. Do not expand
both paths or infer filenames from string spelling. No canonical names/counts or
shared implementation are changed; lead reconciles acceptance.
