# Terrain boundary rectangle serialized input

Round36 adds [TerrainBoundaryRectangleInput](../../SWG-Godot-Client/poc/Stardust.Core/Assets/TerrainBoundaryRectangleInput.cs)
over the frozen owned sections of [TerrainGeneratorInput](TERRAIN_GENERATOR_INPUT_CONTRACT.md).
It preserves complete BREC bytes and exposes supported serialized fields.
Influence, layer/filter/ancestor composition, coordinate mapping and original
terrain generation remain outside this decoder. The earlier
[BCIR input](TERRAIN_BOUNDARY_CIRCLE_INPUT_CONTRACT.md) remains unchanged.

## Direct executable identity and construction

All VAs refer to readonly executable SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`,
hashed before address-dependent inspection. No original execution was used.

**Confirmed:** boundary factory `0x00B79DF0` compares BREC at `0x00B79F4C`,
opens the matching form, allocates0x68 bytes, calls constructor `0x00B72960`
at `0x00B79F99`, then virtual loader+8 at `0x00B79FB0`. Layer dispatcher
`0x00B79D00` inserts the returned pointer through `0x00B63830` at
`0x00B79D31`, before trying filter/affector families. Constructor invokes
shared boundary base `0x00B63320` and sets vtable `0x015ED3B8`.

| Slot | Target | Observed behavior |
|---|---|---|
| +00 | `0x00B729C0` | deleting destructor |
| +08 | `0x00B730F0` | version dispatcher |
| +0C | `0x00B735D0` |0003 serialization |
| +10 | `0x00B72AA0` | store amount, recompute rectangle caches |
| +1C | `0x00B72B70` | translate corner coordinates, recompute |
| +20 | `0x00B72BA0` | edit geometry, recompute |
| +24 | `0x00B72BC0` | query supplied coordinate pair |
| +2C | `0x00B72A50` | copy corner rectangle |

Shared base clears selector+14 and amount+18. Constructor clears corner words
+1C/+20/+24/+28 and inner-rectangle cache+2C/+30/+34/+38. Extension flags
+58/+59 startfalse, word+5C starts0, word+60 starts`0x40000000`, string pointer
+64 startsnull. These are fresh runtime object defaults, not serialized fields
in older versions. Additional transform state+3C/+40..+54 is constructed
internally and is not part of the recovered DATA grammar below.

## Exact serialized layout

**Confirmed:** client dispatcher recognizes0000..0003, using table at
`0x00B731E8`. There is no0004 case. Known loaders call shared IHDR loader
`0x00B62FA0`, then consume DATA through four-byte `0x00A94F00` reads. Reused
IFF framing uses big-endian FORM/chunk extents, printable four-byte tags and
no padding. DATA words below are little-endian raw32.

```text
FORM BREC
  FORM 0000,0001,0002 or0003
    FORM IHDR / version / DATA
    DATA
```

| Version | Loader | Ordered DATA | Modern exact extent |
|---|---|---|---|
| 0000 | `0x00B73200` | firstA,secondA,firstB,secondB |16 bytes |
| 0001 | `0x00B732A0` | discarded prefix, same four corners |20 bytes |
| 0002 | `0x00B73350` | four corners, feather selector, feather amount |24 bytes |
| 0003 | `0x00B73460` | same six words, flag first, flag second, scalar first, scalar second, NUL byte string |40 bytes plus string and NUL |

Read counts are4/5/6/10. Geometry words store at+1C/+20/+24/+28. Their F32
geometry role is **Confirmed** by normalization and query comparisons. API
names are `CornerFirstABits`, `CornerSecondABits`, `CornerFirstBBits`,
`CornerSecondBBits`; A/B reflect serialized order, not guaranteed runtime
minima/maxima. World axis assignment, units and sampling mapping are **Unknown**.

Version0001's first read at `0x00B732DA` is overwritten by the second at
`0x00B732E7` before any geometry store. It is a known raw32 discarded prefix
with **Unknown** meaning, retained as nullable `LegacyPrefixBits`.

Versions0002/0003 read feather selector then call `0x00B65FB0` (store+14),
and read amount then compare against0/1 at `0x015DC6A0`/`0x015DC6F8`.
Ordered values below0 become0; above1 become1; equal signedzero is retained.
Unordered NaNs take unchanged branches if comparisons return; exceptional
x87/trap policy remains unresolved. Amount virtual+10 resolves to
`0x00B72AA0`, which calls base setter `0x00B65FC0` and normalization/cache
helper `0x00B72AC0`. Older versions serialize no feather fields; API values
are null rather than copied runtime defaults. **The decoder retains raw amount
bits and does not execute clamp, normalization or cache recomputation.**

Version0003's final four words load at `0x00B7354E`, `0x00B73566`,
`0x00B7357E`, `0x00B73591`. First/second words are converted by `test/setne`
into bool bytes+58/+59; following words store unchanged at+5C/+60. The API
preserves full raw flag widths and exposes nullable nonzero inspection values.
Getter functions `0x00B73030`/`0x00B73050` return those bool bytes. Precise
feature semantics of flags/scalars remain **Unknown** here, so neutral
`ExtensionFlagFirstBits`, `ExtensionFlagSecondBits`, `ExtensionScalarFirstBits`
and `ExtensionScalarSecondBits` names are intentional; no guessed water or
shader evaluator is exposed.

String helper `0x00A95200`, called at `0x00B7359E`, scans bytes for NUL,
allocates/copies through the terminator and advances the stream cursor by that
length. Pointer is stored at+64. No encoding conversion occurs on this path;
byte encoding and feature meaning remain **Unknown**. The replacement returns
raw string bytes excluding NUL from `CopyExtensionNameBytes()`. Required
terminator and exact remaining extent are stricter modern rules. The helper's
behavior on an unterminated string is not reproduced or treated as acceptance.

Inherited metadata is the accepted direct client IHDR contract:0001 DATA =
raw32 local enabled flag plus NUL byte name;0000 adds three discarded individual
bytes. Nonzero flag means locally enabled. Header/name/legacy raw bytes are
preserved; legacy100-byte buffer behavior and unknown byte semantics are not
emulated. Name and extension strings have separate per-string checks and one
combined decoded-byte budget including terminators.

## Runtime normalization and caller boundary

**Confirmed:** dispatcher calls `0x00B72AC0` after each known loader returns,
and also on its unknown-version path. Helper swaps first-component endpoints
when+1C is ordered greater than+24, and similarly second endpoints+20/+28.
It then derives an inset rectangle from endpoint extents and amount+18. The
separate editing setter `0x00B72A70` copies four supplied words and calls the
same helper. This is not BCIR's load/edit normalization split: BREC loading
also normalizes. Unordered comparisons, signaling NaNs, swap F32 stores and
arithmetic precision/traps require an evaluator-specific contract. Serialized
input fields therefore remain raw, including inverted endpoints and signedzero.

Query `0x00B72BC0` compares supplied coordinates against outer and inner
rectangles, reading+1C/+20/+24/+28 and derived+2C/+30/+34/+38 plus amount+18.
It also has optional transform branch controlled by+3C; that state is not
serialized in the grammar above. Layer caller `0x00B63D20` checks local enable
at `0x00B63F8E`, calls query+24 at `0x00B63FAF`, and then applies selector+14
shaping through the already recovered table0..3 (identity/square/sqrt/polynomial;
unsigned outside0..3 giveszero). This confirms selector metadata's caller role;
full query math, transform producer, influence combination, inversion flags,
ancestor activation and filter state are not implemented here.

## Ownership, errors and modern profile

**Confirmed local lifecycle:** boundary array insertion retains the constructed
pointer; removal `0x00B638C0` deletes through slot0. BREC destructor
`0x00B729F0` frees extension string+64 when non-null, clears it and calls
`0x00B63350`; shared teardown `0x00B62EF0` frees IHDR name+0C. Deleting wrapper
`0x00B729C0` conditionally frees object storage. The replacement owns immutable
values, without legacy pointers, caches, resource lookup or owner callbacks.
The amount callback has internal recomputation rather than external publication.
Full worker/thread lifetime and layer shutdown are outside this input decoder.

Unknown BREC version calls no known loader, but dispatcher still normalizes
its object; recovery/diagnostics and subsequent legacy use are **Unknown**.
**Modern policy:** structurally valid unknown versions, including0004, retain
bounded opaque bytes, null header/fields, empty decoded words/string and zero
decoded-string accounting. Known BREC with unknown IHDR also retains the whole
input as opaque after checking known outer order and fixed/minimum DATA extent.
Unknown IHDR does not trigger semantic validation of its hidden strings.
Opaque retention is not a legacy evaluation-acceptance claim.

Known layouts reject wrong root/tag/order, missing/duplicate versions/records,
truncation, wrong DATA extent, absent/extra string bytes and exceeded budgets.
Known0003 must terminate exactly after its one extension string. Exact extents
and strict record ordering are deliberately bounded modern policies; legacy
malformed-input handling is not inferred from server behavior.

`Decode(span|TerrainGeneratorSection, TerrainBoundaryRectangleInputLimits?)`
checks options and byte cap before source snapshot or section copy. Defaults:
64KiB input,64nodes,depth8,4096 bytes per decoded string,8192 combined decoded
string bytes including NULs. Option ranges: input24..4MiB,nodes1..65536,
depth1..64,per-string0..65536,total0..131072. Invalid options throw
`ArgumentOutOfRangeException`, null section `ArgumentNullException`, malformed
or over-budget input `InvalidDataException`. Reused IFF parser constrains all
opaque bytes/nodes/depth too. Decoded string totals describe known fields only.
String caps are checked before decoded header/string copies.

`Version` and `ClientVersionRecognized` describe BREC outer recognition;
`HasDecodedFields`, nullable `Header`/`Fields` and `HasSerializedExtension`
distinguish decoded/absent/opaque metadata. `DecodedStringBytes` counts known
name/string bytes including terminators. `HasSerializedExtension` is true only
for decoded0003, including an empty extension string; opaque0003 remainsfalse.
All byte/word/header/extension accessors
return independent arrays. Immutable fields permit independent `with` copies;
no mutable IffNode, caller array or borrowed memory escapes. The span path owns
its source and a temporary IFF parse snapshot; section decoding adds a bounded
temporary section copy. Small decoded copies are retained. These are logical
bounds, not measured heap/RSS claims or a whole-terrain work budget.

All F32 bit patterns, nonfinite geometry/amount, arbitrary flag/selector bits
and inverted corner order are accepted as serialized data.
`HasFiniteGeometry` and nullable `HasFiniteSerializedFeatheringAmount` inspect
raw exponents and impose no evaluator eligibility rule. Extension scalars have
no assigned float semantics or finite policy. No normalized rectangle, inset,
feature resource or influence value is published.

Server `MMOCoreORB/src/terrain/layer/boundaries/BoundaryRectangle.h`,
`parseFromIffStream(..., Version<'0002'>)` and bool overload for0003/0004,
is correlation only. Its water/shader names are leads, not client proof. Its0004
trailing word is not recognized by this client dispatcher and stays opaque.
Server enum comments/formulas are not used to implement query arithmetic.

## One pinned original set and reproduction

Readonly root `C:/Stardust-DEV`, config `swgemu_live.cfg`, SHA
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`;
effective `terrain/tatooine.trn`, priority42,1188446 bytes, SHA
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
TGEN offset210,139608 bytes, SHA
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
Same sample as Rounds31..35; no new sweep. All29 BREC are0003 with IHDR0001,
locally enabled and finite geometry/amount. Selector0/1/2/3 counts10/5/3/11.
Both extension flag words are zero in all29, and no serialized endpoints are
inverted. These occurrence counts do not close dormant feature semantics or
constitute coverage metrics. No original query/height computation was performed.

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainBoundaryRectangleInputScenarios.cs)
and [independent oracle](../../SWG-Godot-Client/poc/round36-tests/Assets/verify_brec_input.py) compare
all29 records in layer/child order, TGEN offsets, full-section/header/extension
hashes and complete raw DATA words. Synthetic fixtures cover all four known
versions, both IHDR versions, fullwidth/nonfinite/signedzero bits, inverted
corners, unsupported0004/unknown payload retention, malformed extents/tags/order/
strings, byte truncation, individual/combined string caps, exact/one-less/invalid
budgets and source/section/accessor/record ownership. Private receipts remain
ignored; no original asset content is promoted as a public fixture.
Verification passes350 cases (56 accepted/294 rejected),17 ownership checks,
46 instruction anchors plus exact vtable/version/read-order evidence, and all29
original records. Offline .NET8 build has zero warnings/errors.

```powershell
python poc/round36-tests/Assets/verify_brec_input.py generate local-private/round36/fixtures.json
dotnet restore poc/round36-tests/Assets/Assets.csproj --configfile local-private/round36/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round36-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round36-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round36/fixtures.json > local-private/round36/native.txt
python poc/round36-tests/Assets/verify_brec_input.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round36/native.txt
```

Use cached .NET8/Python, cleared offline NuGet sources/private packages,
task-specific APPDATA and PYTHONUTF8=1. Static verifier uses cached pefile and
capstone through PYTHONPATH and hash-gates the executable before VAs.
No downloads, original execution, live contact, staging or publication.
Next probe: trace extension flags/scalars/string consumers to establish feature
roles, or close a separately scoped query/normalization arithmetic contract with
explicit coordinate and precision inputs before any influence implementation.
