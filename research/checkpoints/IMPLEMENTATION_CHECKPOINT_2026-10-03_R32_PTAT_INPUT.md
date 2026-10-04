# Round32 PTAT input implementation checkpoint

Date: 2026-10-03. Question: can an immutable, bounded engine-neutral decoder
retain PTAT0013/0014 DATA, complete raw TGEN/0000, and direct-version maps without
inventing generator behavior or DATA field semantics? Stopping condition:
exact client read structure, synthetic negative/ownership gates, one pinned
effective Tatooine match, focused scope and workspace checks.

## Input identity and method

Frozen Round32 manifest2786 files, SHA
`07d6abdbdab081bad0d3fb3c67a4724c6cd2865ac5411e04e9dc6b2821d9dd04`;
ZIP SHA `e98dc53bba40cc13dda5298b5c713a51d870bfc3ee43944239a6d62e5e627487`.
All archive members, duplicates, path containment, every ancestor's symlink and
Windows reparse status, sizes/hashes were checked before overlay writes. All
listed postcopy bytes verified; HEAD/index and unlisted/private files preserved.

Original executable matched SHA
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
before VA use. Existing `disasm_range.py`, PE/capstone packages, existing IFF
reader, TRE overlay, and previous map contract were used. Server loader served
only as correlation. No original executable execution, live contact, corpus
sweep, proprietary public fixtures, registry edits or publication occurred.

## Evidence and interpretation

**Observation / Confirmed:** dispatcher `0x00B6E980` branches0013 to
`0x00B6EAB0` and0014 to `0x00B6EEA0`. They read DATA then call generator
`0x00B65C50` and map `0x00B9A470`. Both common prefixes have two NUL byte strings
and seven four-byte words;0013 alone reads six temporary strings and five words.
The complete discard sequence is `S S W S W S W S W W S`.
0013 makes32 raw four-byte reads;0014 makes27. Raw copy reader `0x00A94F00`
preserves x86 little-endian words. Generator dispatcher accepts TGEN/0000.

**Observation / Confirmed:** each loader stores20 words at template offsets
`+0x44..+0x90`. Twenty getters at `0x00B6E570 + 16*index` return repeating
`F32 F32 F32 F32 raw32` groups. The server's first three minimum slots use
unsigned reads; client `fld` instructions establish their F32 interpretation.
No seed signedness or group gameplay behavior follows from raw EAX getters.

**Interpretation:** dimension/water/string names are strongly inferred with
server correlation, while environment cycle has prior confirmed client
consumer evidence. The API preserves20 positional raw words, discarded0013
bytes, and any DATA tail without semantic names. Generator bytes are owned
input for a subsequent milestone, not evaluated terrain. The exact schema,
confidence, API and compatibility consequences live in the
[durable contract](../../docs/PROCEDURAL_TERRAIN_INPUT_CONTRACT.md).

**Implementation consequence:** strict outer order/version/extent and modern
resource/finite eligibility can be enforced now. Raw unknown branches and
inactive-water values must remain lossless. The complete generator form includes
its encoded header, allowing a future independent decoder without re-reading
archives or retaining mutable IFF memory. Reuse accepted immutable map masks.

## Validation

Offline .NET8 build: zero warnings/errors. Native and independent Python audit
pass587 synthetic fixtures (8 accepted,579 rejected), nine ownership checks and
two index rejections. Accepted cases cover both versions, unknown DATA tail,
non-ASCII byte strings, empty maps, inactive-water nonfinite bits, subnormals,
and exact budgets. Rejections cover all synthetic DATA/envelope truncations,
wrong order/tag/version/extent/trailing, malformed generator/map structure,
active-field eligibility and configured resource limits. Static oracle checks
32/27 word reads, six0013 string reads,40 metadata stores,20 typed getters,
raw copy and generator version anchors against the hash-gated executable.

Native overlay and independent Python raw parsing agree for one readonly
`terrain/tatooine.trn`: root `C:/Stardust-DEV`, configuration SHA
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`,
priority42,1188446 bytes, full SHA
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
Version0014; DATA178 bytes; complete TGEN139608 bytes at210;
map1048628 bytes at139818; both mask hashes match Round31.
DATA/TGEN/map exact hashes and commands are in the durable contract.
No unknown tail exists in this sample. Detailed native/audit evidence is ignored
under `local-private/round32/`; it is not curated source material.

Workspace validator passes1310 Markdown files, zero broken/private links,
2626 provisional rows with zero duplicates, and345 packet rows. Baseline-relative
scope review confirms all2786 baseline files unchanged, seven assigned additions,
no existing modifications/deletions, unchanged worker HEAD and empty index.
Focused no-index whitespace checks and git diff --check pass. Private evidence
and build output remain ignored.

## Known boundary and next action

0013 original occurrence remains untested; discarded branch meanings and
metadata group consumer semantics remain unknown. MPTA, additional top-level
records, TGEN semantic decoding/evaluation, world-coordinate queries, flora,
rendering and gameplay terrain are outside this milestone. Preservation of an
unknown DATA tail does not establish its format or runtime relevance.

Next: decode TGEN/0000 group headers and one layer/height-affector family from
client factory evidence, with deterministic positive/malformed byte fixtures
and one owned sample. Stop before height evaluation until that boundary closes.
Lead owns reconciliation of shared status/session/ledger; this worker adds only
its assigned four files plus the three-file Round32 Assets runner/oracle.
