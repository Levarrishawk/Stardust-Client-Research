# Terrain boundary circle serialized input

Round35 adds TerrainBoundaryCircleInput (external historical reference)
over the owned rule sections of [TerrainGeneratorInput](TERRAIN_GENERATOR_INPUT_CONTRACT.md).
It decodes BCIR serialized fields, preserving every input byte and raw32 field.
It does not compute influence, activate ancestors, compose boundary/filter
results, or generate terrain. BREC, BPOL and BPLN remain unsupported here.

## Direct client evidence and confidence

All addresses are VAs for executable SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
The static verifier hashes the input before inspecting any address. No original
executable execution is involved.

**Confirmed:** layer record dispatcher `0x00B79D00` first calls boundary factory
`0x00B79DF0`, then filter/affector factories. BCIR tag comparison is at
`0x00B79E36`; the matching branch opens BCIR, allocates0x2C bytes, calls
constructor `0x00B72330` at `0x00B79EBD`, calls virtual loader slot+8 at
`0x00B79ED4`, closes BCIR and returns the object. The dispatcher inserts the
result in its boundary array through `0x00B63830` at `0x00B79D31`.
Constructor calls boundary base `0x00B63320`, clears four geometry/cache words
at+1C/+20/+24/+28 and installs vtable `0x015ED37C`.

| Vtable slot | Target | Observed role |
|---|---|---|
| +00 | `0x00B72360` | deleting destructor |
| +08 | `0x00B725A0` | version dispatcher |
| +0C | `0x00B728C0` |0002 serializer |
| +10 | `0x00B65FC0` | store supplied amount at+18 |
| +1C | `0x00B723F0` | add supplied pair to center |
| +20 | `0x00B72410` | scale radius and recompute square |
| +24 | `0x00B72430` | circle query using two supplied coordinates |
| +2C | `0x00B723A0` | copy center pair |

The base initializes feather selector+14 and amount+18 to positive zero.
Geometry loaders bypass the separate editing setter `0x00B723C0`, whose `fabs`
normalizes a supplied radius. Therefore no absolute-value conversion belongs
in this serialized decoder. Runtime radius-square+28 is derived by x87
multiply/F32 store, not serialized; its computation and precision are omitted.

## Serialized grammar

**Confirmed structure:** outer record headers and FORM extents are big endian;
DATA fields below are little-endian raw32 words. IFF records have no added
alignment padding. The dispatcher recognizes0000,0001,0002 and routes to
`0x00B72660`, `0x00B72700`, `0x00B727B0` respectively. Each loader opens its
version, calls inherited IHDR loader `0x00B62FA0`, then opens DATA. Read calls
`0x00A94F00` have exact four-byte widths, with3/4/5 calls respectively.

```text
FORM BCIR
  FORM 0000,0001 or0002
    FORM IHDR / version / DATA
    DATA
```

| BCIR version | Ordered DATA fields | Exact modern extent |
|---|---|---|
| 0000 | center first F32 bits, center second F32 bits, radius F32 bits |12 bytes |
| 0001 | discarded raw32 prefix, then the same three geometry fields |16 bytes |
| 0002 | same three geometry fields, raw32 feather selector, feather amount F32 bits |20 bytes |

Version0001's first read at `0x00B7273A` is overwritten by the second at
`0x00B72747` before the first object store at `0x00B72757`. Its width/order are
**Confirmed**; meaning is **Unknown**. The API retains it as `LegacyPrefixBits`.
Geometry stores target+1C/+20/+24. Circle query `0x00B72430` subtracts the two
supplied coordinates from+1C/+20, squares and sums differences, and compares
against radius-square+28; this confirms center/radius roles. World X/Y/Z axes,
units and terrain sampling correspondence are **Unknown** at this boundary.
The API uses first/second component names to preserve that distinction.

Version0002 reads selector then calls `0x00B65FB0` to store+14. It reads amount,
compares against constants0/1 at `0x015DC6A0`/`0x015DC6F8`, clamps ordered
values below0/above1, and calls vtable+10 to store+18. Equal signed zero survives;
unordered NaNs take the unchanged branch if the comparisons return. Exception
mask/trap policy is unresolved. **The API retains serialized amount bits and
does not apply this runtime clamp.** Versions0000/0001 have no serialized
feather fields; nullable fields mean absent, not a guessed encoded zero. Their
fresh legacy object has zero base defaults, which remain a separate lifecycle
observation.

Inherited IHDR framing reuses the directly verified Round33 header contract:
0001 DATA = raw32 local enabled flag, NUL-terminated byte name;0000 adds three
individual discarded bytes after the terminator. Nonzero flag becomes true at
legacy load; original fullwidth bits remain available. Meaning/encoding of the
name and three discarded bytes remain unassigned. Legacy100-byte string-buffer
behavior is not reproduced by this bounded raw-byte API.

## Caller interpretation, lifecycle and errors

**Confirmed:** layer processing `0x00B63D20` checks local enable through
`0x00B62F20` at `0x00B63F8E`, loads selector+14 at `0x00B63F97`, then calls
boundary query vtable+24 at `0x00B63FAF`. It subsequently uses an unsigned
selector<=3 table at `0x00B642A0`:0 passes query value,1 squares it,2 takes its
square root,3 computes the observed polynomial `u*u*(3-2*u)` in x87 order.
Values outside0..3, including high-bit raw values, replace the result with zero
at `0x00B63FD8`. Selector shaping belongs to the layer caller, not the BCIR
geometry query. Enum spelling and broader ancestor/filter composition are not
claimed. This decoder does not implement those operations or choose an
arithmetic precision profile.

The array insertion retains the newly constructed pointer. Removal
`0x00B638C0` calls deleting destructor slot0 when non-null. BCIR destructor
`0x00B72390` jumps to boundary base `0x00B63350`, then common teardown
`0x00B62EF0`, which frees name storage+0C; deleting wrapper optionally frees
the object. Replacement models own immutable managed values and need no
callbacks or legacy pointer/cache lifetime. Loader amount callback resolves to
a plain field store in this vtable. No external owner callback is introduced.
Full layer shutdown/thread affinity is outside this serialized input task.

Unknown BCIR versions take the dispatcher's non-loader path; no recognized
geometry loader is called. Exact legacy diagnostics, recovery and later object
use are not closed. **Modern policy:** retain structurally valid unknown
versions as opaque, bounded IFF bytes. Unknown IHDR versions in a known BCIR
also make the complete input opaque after checking known outer record order
and DATA extent. This is preservation, not a claim of legacy evaluation
acceptance. Malformed known records reject with `InvalidDataException`:
wrong root/order/tags, duplicate/missing records, wrong extents, missing NUL,
truncation and budget overflow. Exact known extents and no trailing records
are explicit stricter modern policies, not recovered legacy failure handling.

Server correlation only: `MMOCoreORB/src/terrain/layer/boundaries/BoundaryCircle.h`,
`parseFromIffStream(..., Version<'0002'>)` lists centerX/centerY/radius/type/amount
and clamps amount. `checkInfluence` supplies analogous names. It supports only
0002 and is not proof for client0000/0001, the discarded prefix, editing
normalization, caller selector shaping or malformed-input behavior.

## API, ownership and budgets

`Decode(ReadOnlySpan<byte>, TerrainBoundaryCircleInputLimits?)` and
`Decode(TerrainGeneratorSection, TerrainBoundaryCircleInputLimits?)` return an
owned envelope. `Version`, `ClientVersionRecognized`, `HasDecodedFields`,
nullable `Header` and nullable immutable `Fields` distinguish known outer
version from decoded metadata. Unknown inputs have null header/fields and an
empty decoded word array; `CopyEncodedBytes()` still returns their full bytes.
`CopyDataWordBits()` returns known words in exact serialized order.
All copy accessors, including header name/legacy, return independent arrays.
The fields record supports independent `with` copies without mutating a retained
model. No IffNode, mutable collection, borrowed memory or caller array escapes.

Every F32 bit pattern is accepted as serialized input, including signaling/
quiet NaNs, infinities, negative radius and signed zero. `HasFiniteGeometry`
and nullable `HasFiniteSerializedFeatheringAmount` are inspection predicates;
absence yields null for the latter. These predicates impose no evaluator
eligibility policy. No radius square, normalized amount, endpoint computation
or influence result is exposed.

Default logical caps: input64KiB,64 IFF nodes,depth8, decoded name4096 bytes.
Option ranges: input24..4MiB,nodes1..65536,depth1..64,name0..65536.
Invalid configuration throws `ArgumentOutOfRangeException`; null section throws
`ArgumentNullException`. Byte cap is checked before snapshot and before a
section's `CopyEncodedBytes`. Node/depth caps constrain the reused IFF parser;
name cap applies only to decoded known IHDR. Opaque inputs still obey byte,
node and depth caps, without assigning semantics to hidden strings. No combined
corpus budget or evaluator work budget is claimed.

The span path snapshots input, and IffDocument creates another bounded parse
snapshot; model source plus small decoded header/word copies are retained,
while the parse tree is temporary. The section overload additionally makes a
bounded temporary source copy. These are logical allocation bounds, not measured
heap/RSS claims. No per-ancestor proprietary subtree corpus is retained.

## Reproduction and one original occurrence set

Readonly root `C:/Stardust-DEV`, config `swgemu_live.cfg`, config SHA
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
Effective path `terrain/tatooine.trn`, archive priority42,1188446 bytes, SHA
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
TGEN offset210,139608 bytes, SHA
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
Same input as Rounds31..34, not a new sweep. There are256 BCIR records, all0002
with IHDR0001;246 locally enabled, all geometry/amount fields finite and no
negative radius bits. Selector counts0/1/2/3 are42/86/35/93. These are sample
occurrences, not coverage denominators. No original influence or generated
height computation was performed; only independently decoded raw records were
compared in layer/child order, with offsets, full-section/header hashes and
fullwidth fields. Detailed proprietary-derived receipts stay ignored privately.

Native scenarios (external historical reference)
and independent oracle (external historical reference) cover all
three versions, both header versions, arbitrary raw bits, malformed structures,
opaque retention, exact/one-less budgets, source/inspection/section isolation
and retained valid input after later failure. Static checks hash-gate the
executable and verify factories/vtable, exact widths, discarded prefix,
field stores, normalization split, caller selector table and teardown.
Verification passes244 synthetic cases (40 accepted/204 rejected),13 ownership
checks,37 instruction anchors and all256 pinned original records. Offline
.NET8 build passes with zero warnings/errors.

```powershell
python poc/round35-tests/Assets/verify_bcir_input.py generate local-private/round35/fixtures.json
dotnet restore poc/round35-tests/Assets/Assets.csproj --configfile local-private/round35/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round35-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round35-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round35/fixtures.json > local-private/round35/native.txt
python poc/round35-tests/Assets/verify_bcir_input.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round35/native.txt
```

Use cached .NET8/Python, cleared offline NuGet package sources/private packages,
task-specific APPDATA and PYTHONUTF8=1. Static checks use cached pefile/capstone
through PYTHONPATH. No downloads, original execution, live contact or publication.
Next probe: recover a bounded circle-query/caller-shaping arithmetic contract
only after establishing its precision, exceptional inputs and supplied sample
coordinates; ancestor enable/filter composition remains separately open.

## Round36 composite world and terrain boundaries

A separate [supplied-circle influence evaluator](TERRAIN_BOUNDARY_CIRCLE_INFLUENCE_CONTRACT.md) now covers the finite graph and caller selector, with explicit distinct cache/loading and query/shaping precision. Raw serialized input policy remains unchanged. Coordinate production and layer composition remain open.

## Round37 complete creature kernel and supplied terrain

[Supplied height-patch inspection](TERRAIN_CIRCLE_HEIGHT_PATCH_CONTRACT.md) joins the accepted circle and AHCN kernels with explicit separate cache/query/height profiles. Raw circle-input ownership and acceptance remain unchanged; original layer composition and generated terrain remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_COMPLETE_CREATURE_AND_SUPPLIED_TERRAIN.md).
