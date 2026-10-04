# Round35 BCIR serialized input checkpoint — 2026-10-03

## Bounded question and stopping condition

What bytes do the client's BCIR version loaders consume, in which order, and
which immutable engine-neutral input can retain them without implementing
boundary influence? Stop when supported versions/inherited headers and opaque
modern policy have direct static evidence, native/independent raw-byte fixtures
agree, all256 BCIR occurrences in the same pinned input match in order, and
scope/ownership checks pass. No BREC/BPOL/BPLN evaluator or original terrain
execution is included.

## Inputs, tools and scope

Original executable read-only SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
was confirmed before VAs. Pinned root/config/archive/path/full terrain/TGEN
identities and exact reproduction commands are in the
[durable contract](../../docs/TERRAIN_BOUNDARY_CIRCLE_INPUT_CONTRACT.md).
Static tools: existing disasm_range.py with cached pefile/capstone, inspected
original factory/vtable/header/caller/teardown ranges, independent byte parser
and synthetic generator, offline .NET8 native scenarios. Existing Round33
TGEN oracle/owned sections are reused; no new schema registry or corpus sweep.
Server BoundaryCircle.h is correlation only.

Frozen accepted34 snapshot:2905 files, manifest SHA
`edd3e8aac46561b3dc1ff3349d590e32149fe0ceb4b3aab1639d7d5df0b91e6a`,
ZIP SHA `b60f83b6d137925d800ff4f3af8beabd8ed16112edf5641110e97a3e912e5ee0`.
Before overlay writes, every member/listed path passed hash/size/set/uniqueness,
casefold collision, containment and target/ancestor symlink/reparse preflight;
all-file postflight passed. Worker HEAD/index and unlisted/private bytes were
preserved. Four assigned new files and three own runner/oracle additions only;
existing production and shared artifacts immutable. No downloads, staging,
commit, publication, live contact, original executable execution or subagents.

## Evidence and interpretation

| Observation | Interpretation/confidence | Compatibility consequence |
|---|---|---|
| Factory00B79DF0 matches BCIR, constructs00B72330, calls vtable+8; vtable015ED37C slot+8→00B725A0 | Confirmed boundary-family path | Decode BCIR only; leave other families unchanged |
|00B725A0 routes0000/1/2→00B72660/00B72700/00B727B0; each calls00B62FA0 then DATA | Confirmed versions and inherited order | Three typed versions, strict known record order |
|3/4/5 four-byte reads;0001's first value overwritten before geometry stores | Confirmed structure; discarded prefix meaning Unknown | DATA12/16/20; preserve raw legacy word |
| Geometry stores+1C/+20/+24; query00B72430 subtracts supplied pair and uses derived radius-square+28 | Confirmed center/radius role; coordinate axes/units Unknown | First/second components, radius rawbits; no world mapping |
| Loader stores radius directly; editing setter00B723C0 has fabs | Confirmed distinct load/edit behavior | Preserve negative radius; no normalization during decode |
|0002 selector stored+14; amount clamp0/1 then slot+10→plain+18 store | Confirmed metadata use and ordered clamp | Preserve serialized fields; runtime clamp/exception policy separate |
| Boundary base00B63320 zeros+14/+18; old versions do not read feather fields | Confirmed fresh runtime defaults; no serialized defaults | Nullable serialized metadata for0000/1 |
| Caller00B63D20 checks local enabled then query+24; unsigned selector table0..3 yields identity/square/sqrt/polynomial, defaultzero | Confirmed caller role, enum spelling unassigned | Document selector separately; implement no influence/shaping |
| Removal00B638C0 calls deleting destructor;00B72390→00B63350→00B62EF0 frees name+0C | Confirmed local ownership/teardown chain | Immutable owned model, no borrowed legacy objects/callbacks |
| Unknown dispatcher version does not call a known loader | Confirmed dispatch gap; legacy recovery Unknown | Modern opaque preservation is explicit, not evaluator acceptance |

IHDR0001 is local raw32 flag/NUL byte name;0000 adds three discarded single
bytes. Direct client header evidence was reconfirmed, not inferred from server.
The decoder does not reproduce legacy100-byte buffer behavior, exact diagnostics
or exceptional x87 behavior. Known exact extent/trailing rejection and logical
caps are stricter modern policies. Raw nonfinite bits and signed zeros survive.
Finite predicates inspect fields without imposing evaluation eligibility.

## Implementation

[TerrainBoundaryCircleInput](../../poc/Stardust.Core/Assets/TerrainBoundaryCircleInput.cs)
accepts a span or TerrainGeneratorSection, checks logical input caps before
snapshot/section copy, retains all encoded bytes, and publishes immutable
header/nullable fields plus independent encoded/word/header copies. Opaque
unknown BCIR or IHDR versions retain complete bytes; malformed known framing
rejects. No IffNode/memory/array escapes. Defaults64KiB/64nodes/depth8/name4096;
all options and modern policies are documented in the contract. Reused IFF
parse makes an additional bounded temporary snapshot; no measured heap claim.

## Verification

Offline .NET8 build: zero warnings/errors. Native and independent Python pass
244 cases:40 accepted/204 rejected,13 ownership/section/cap/retention checks,
37 instruction anchors plus exact vtable entries, DATA read widths,0001 discard
order, caller selector jump table, constants and header/teardown evidence.
Tests include every byte truncation of a representative record, malformed
known sizes/tags/order/headers/strings, exact/one-less/invalid limits, both
IHDR versions, all three BCIR versions, raw signedzero/nonfinite/subnormal bits,
opaque byte retention, section isolation and earlier-result retention.

One readonly original comparison matches all256 BCIR records' layer/child
order, TGEN offsets, full-section hashes, header identities and raw field bits.
All are BCIR0002/IHDR0001;246 locally enabled,256 finite geometry/amount,
zero negative radii. Selector0/1/2/3 counts42/86/35/93. No influence/height
calculation was performed. These are sample counts, not coverage metrics.
Private native/raw/static receipts remain ignored; no original asset content
is promoted into public fixtures. Workspace validation passes1340 Markdown
files with zero broken/private links,2626 names/zero duplicates and345 packet
rows. Whitespace and scope checks pass:2905 unchanged baseline files, seven
additions, zero existing modifications/deletions, unchanged worker HEAD,
empty index, and ignored private/build output.

## Remaining boundary and precise resume point

World axes/units/sample mapping, original worker arithmetic precision/traps,
full boundary/filter/ancestor composition and unknown-version runtime recovery
remain Unknown or outside scope. Next action: close the circle query's bounded
arithmetic/exception contract and supplied-coordinate producer, then caller
shaping, before selecting an evaluator. Do not infer those contracts from the
server's analogous formula. Lead owns shared status/ledger/session reconciliation.
