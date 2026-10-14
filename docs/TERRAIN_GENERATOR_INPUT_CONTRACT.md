# Terrain generator input contract

Round33 adds TerrainGeneratorInput (external historical reference),
a bounded engine-neutral decoder over the complete owned TGEN form from
[ProceduralTerrainInput](PROCEDURAL_TERRAIN_INPUT_CONTRACT.md). It retains every
accepted input byte, exposes group and layer structure, and decodes one rule
family: AHCN. It does not evaluate heights, boundaries, filters, flora, maps,
materials, or ancestor activation.

## Client framing and supported boundary

**Confirmed executable evidence:** SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`;
TGEN/0000 loader `0x00B65CC0` calls these group loaders in order:

| Role | Literal form | Loader | Client-recognized versions |
|---|---|---|---|
| Shader | SGRP | `0x00B69C90` | 0000..0006 |
| Flora | FGRP | `0x00B67300` | 0001..0008 |
| Radial | RGRP | `0x00B6C1A0` | 0000..0003 |
| Environment | EGRP | `0x00B6D990` | 0000..0002 |
| Fractal map | MGRP | `0x00B75920` | 0000 |
| Optional bitmap | MGRP | `0x00B7AE80` | 0000 |

The two MGRP objects have the same literal tag, distinguished by position and
loader. The bitmap loader tries optional MGRP and returns without consuming it
when absent. Next, the client tries optional FORM LYRS (`0x00B65D29`) and loads
its direct LAYR children through the layer vtable. There is no direct top-level
LAYR fallback in this observed client path. The analogous server
`MMOCoreORB/src/terrain/TerrainGenerator.cpp`,
`parseFromIffStream(IffStream*, Version<'0000'>)`, does have that fallback;
it is correlation, not client proof.

```text
FORM TGEN
  FORM 0000
    FORM SGRP / version
    FORM FGRP / version
    FORM RGRP / version
    FORM EGRP / version
    FORM MGRP / version
    [FORM MGRP / version]       optional bitmap object
    [FORM LYRS                 optional, may be empty
       FORM LAYR / version
       ...]
```

**Modern profile:** this decoder requires the first five groups and their
order, one version form per group, at most one bitmap group, optional LYRS,
and no trailing top-level records. Legacy group loaders individually try
optional forms, so this is deliberately stricter than their missing-group
behavior. It is an input prerequisite profile, not the entire legacy grammar.
Unknown group versions remain opaque, with `ClientVersionRecognized=false`;
known group payload semantics also remain unsupported. Group `RecordCount`
is the number of immediate records inside its version, not a unique family-ID
count. It conservatively budgets serialized family/group records.

## Layers and inherited metadata

Client layer dispatcher `0x00B647C0` recognizes0000..0004. Version0000's
ACTN migration is left opaque. Versions0001..0004 call common base loader
`0x00B62FA0`, then consume ADTA, then dispatch remaining records through
`0x00B79D00`. Exact structure:

| Layer version | Ordered prefix | ADTA payload |
|---|---|---|
| 0001 | IHDR, ADTA | two LE raw32 words |
| 0002 | IHDR, ADTA | three LE raw32 words |
| 0003 | IHDR, ADTA | three LE raw32 words, NUL byte string |
| 0004 | IHDR, ADTA | four LE raw32 words, NUL byte string |

The three/four-word distinction is confirmed by exact four-byte calls in
`0x00B64AE0`, `0x00B64BB0`, and `0x00B64CA0`; names or floating-point semantics
of individual ADTA words are not assigned by this API. Layer0003 is the only
version observed in the pinned sample. Other supported structures are tested
synthetically against client read evidence.

Common inherited metadata is FORM IHDR, one version form, one DATA chunk:

| IHDR version | Exact DATA payload |
|---|---|
| 0001 | LE raw32 enabled flag, NUL byte string |
| 0000 | same flag and string, then three discarded **one-byte** values |

`0x00B63120` and `0x00B63070` respectively establish these layouts. The flag
becomes true for any nonzero value; original bits remain available. Byte-string
encoding and the meanings of the three legacy bytes remain unknown. The
legacy readers use a100-byte string buffer; this API preserves bounded raw
bytes and does not emulate truncation or unsafe legacy boundary handling.

`Layers` is a depth-first immutable flattened list with encoded containment
`ParentIndex`, depth, source section, local header, raw ADTA words, description
bytes and ordered remaining sections. The parent index is not an inferred
runtime pointer or inherited enable policy. Unknown layer versions,0000, or
unknown inherited IHDR versions retain the entire layer section as opaque;
children under such a layer are not semantically traversed.

## Typed AHCN prerequisite

**Confirmed factory and inheritance chain:** affector factory `0x00B7A240`
recognizes AHCN and calls constructor `0x00B764B0`. The constructor sets vtable
`0x015ED678`; its loader slot at `0x015ED680` is `0x00B76650`. That dispatcher
accepts0000 and calls `0x00B766F0`, which invokes the same base IHDR loader
`0x00B62FA0`, then reads two four-byte words into `+0x14` and `+0x18`.
Arithmetic in virtual `0x00B76540` uses the latter as F32 height input and the
former to select a branch. No operation enum or numerical evaluator is
implemented here. Destruction at `0x00B76510` delegates to the shared base
teardown; the replacement retains immutable values instead of legacy pointers.

```text
FORM AHCN
  FORM 0000
    FORM IHDR
      FORM 0000 or FORM 0001
        DATA                  inherited local flag/name/legacy bytes
    DATA                      exactly8 bytes
      LE raw32 operation
      LE F32 height bits
```

`TerrainHeightConstantInput` has `Header`, `OperationBits`, the signed
2's-complement inspection `Operation`, `HeightBits`, `Height`, and
`HasFiniteHeight`. All height bits, including NaNs, infinities and signed zero,
are retained. Decoding is not evaluation eligibility; a future evaluator must
set its own finite policy. Unsupported AHCN versions or inherited header
versions remain opaque rules. Malformed known0000 structure, wrong order,
missing fields, and wrong known DATA lengths reject. Unknown rule tags or
leaf records are retained exactly, without guessing factory semantics.

Server `MMOCoreORB/src/terrain/layer/affectors/AffectorHeightConstant.h`,
`parseFromIffStream(..., Version<'0000'>)` and `process`, supplies class/field
names and analogous operation descriptions only. It is not copied into this
implementation. Server `layer/InformationHeader.h` supports only0001; direct
client evidence additionally closes the three discarded bytes of0000.

## API, ownership and budgets

`TerrainGeneratorInput.Decode(ReadOnlySpan<byte>, TerrainGeneratorInputLimits?)`
snapshots the complete input. `Groups`, `Layers`, `Rules`, and layer `Children`
are read-only collections of immutable models. `Rules` preserves each record's
layer index and child ordinal; typed AHCN is nullable. Section offsets refer to
the TGEN input, not the surrounding PTAT. Every copy accessor returns separate
bytes or words. Sections share an inaccessible owned snapshot internally;
no mutable IFF node, caller array, or `ReadOnlyMemory` escapes.

Default budgets: input8MiB,50000 IFF nodes, IFF depth64,4096 decoded layers,
containment depth32,32768 decoded rule records,16384 immediate group records,
4096 bytes per decoded string,1MiB combined decoded strings including NULs,
and4MiB per inspected section. Invalid options throw
`ArgumentOutOfRangeException`; malformed or over-budget input throws
`InvalidDataException`. Input cap precedes allocation. Section/semantic caps
follow the already bounded IFF parse. Unknown payloads stay byte/node/depth
bounded; string, layer and rule counts describe decoded structures, not hidden
semantics inside opaque versions. Read-only snapshot and model creation are
bounded by input/nodes; nested sections share bytes rather than retaining a
separate full copy per ancestor. Inspection copies are caller-owned allocations
up to the selected section size. No aggregate retained-subtree byte sum or
terrain evaluation work count is claimed.

## One original sample and reproducible gates

Same readonly sample as Round32: root `C:/Stardust-DEV`, config
`swgemu_live.cfg`, config SHA
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`,
path `terrain/tatooine.trn`, priority42,1188446 bytes, full SHA
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
Complete TGEN at PTAT offset210,139608 bytes, SHA
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
No bitmap group occurs in this sample. Group versions are0006/0008/0003/0002/0000,
with15/11/5/17/43 immediate records (91 total). There are257 decoded layers,
849 ordered rule records,87 typed AHCN records and762 unsupported rule records.
Decoded header/description strings consume5907 bytes including terminators.
These are counts for one pinned input, not corpus or implementation coverage.

Native scenarios (external historical reference)
and independent oracle (external historical reference) pass787
synthetic fixtures (21 accepted,766 rejected) and ten ownership checks. They
compare complete section hashes, group versions/counts, layer containment,
ADTA words, header/name/legacy identities, rule order and AHCN raw fields for
every accepted fixture and the pinned original. Static audit hash-gates the
executable and verifies ordered loaders, optional bitmap/LYRS, factory/vtable,
inherited AHCN DATA reads, legacy header byte reads and layer ADTA widths.

```powershell
python poc/round33-tests/Assets/verify_tgen_input.py generate local-private/round33/fixtures.json
dotnet restore poc/round33-tests/Assets/Assets.csproj --configfile local-private/round33/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round33-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round33-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round33/fixtures.json > local-private/round33/native.txt
python poc/round33-tests/Assets/verify_tgen_input.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round33/native.txt
```

Use cached .NET8/Python, clear offline NuGet feeds/private packages,
task-specific APPDATA and PYTHONUTF8=1. Static audit uses existing pefile and
capstone through PYTHONPATH. The private config is a NuGet configuration with
`packageSources` cleared. No downloads, original execution, live contact,
corpus sweep, or original public fixture promotion is involved.

Next bounded probe: one boundary/filter family and its layer influence contract,
plus exact AHCN operation and inherited activation behavior before implementing
any height evaluation. Group family payloads and legacy0000 layer migration
remain independent prerequisites.

## Round52 immutable source identity

EncodedBytes and cached uppercase SourceSha256 expose complete owned input identity without a root copy. Hashing occurs once after successful grammar/semantic budget validation. Exported copies remain independent. [Identity/budget contract](TERRAIN_GENERATOR_IDENTITY_BUDGET_CONTRACT.md) records actual reservation and ownership checks.

## Round53 selected height rule

[Owned AHFR selection](TERRAIN_GENERATOR_HEIGHT_FRACTAL_RULE_CONTRACT.md) uses the
existing Rules index and identity without a new parser. Serialized ChildOrdinal
includes IHDR/ADTA; indexed layer.Children omits those two positions. Selection
checks their exact offset relationship, signed family binding and pre-copy caps.
It does not infer layer activation.

## Round61 separate shader-family decoder

The original input decoder retains its opaque group policy. A separate [SGRP0006 typed decoder](TERRAIN_GENERATOR_SHADER_FAMILIES_CONTRACT.md) now binds the actual shader-group section and retains raw ordered SFAM records, with the observed static read widths plus one original occurrence. This closes structural fields and ownership, not shader semantics, resolution or materials. Other recognized versions remain opaque here.
