# Procedural terrain input contract

Round32 implements an engine-neutral, immutable PTAT input envelope in
[ProceduralTerrainInput.cs](../../SWG-Godot-Client/poc/Stardust.Core/Assets/ProceduralTerrainInput.cs).
It owns DATA, the complete encoded TGEN form, and the accepted
[terrain map masks](TERRAIN_MAP_MASKS_CONTRACT.md). It does not evaluate rules,
generate heights, place flora, convert world coordinates, or render terrain.

## Literal framing and evidence

**Confirmed:** baseline executable SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`;
PTAT dispatcher `0x00B6E980`, version0013 loader `0x00B6EAB0`, and
version0014 loader `0x00B6EEA0` establish ordered DATA, generator load, then
map load. The generator dispatcher `0x00B65C50` requires TGEN and accepts
version0000. The existing map loader evidence accepts direct version0000 or
version0001 forms. One effective Tatooine occurrence confirms these literal
bytes and order. The executable dispatcher also recognizes MPTA; that separate
family is deliberately outside this API.

```text
FORM PTAT
  FORM 0013 or FORM 0014
    DATA
    FORM TGEN
      FORM 0000
        ...opaque, structurally bounded IFF records...
    FORM 0000 (empty maps) or FORM 0001 (DATA, WMAP, SMAP)
```

There is one root, one PTAT version, and exactly three ordered version children.
IFF tags and sizes are big-endian; scalar DATA words are little-endian.
Records advance without RIFF padding. TGEN must have exactly one version0000
form, but its inner grammar is not validated against generator factories.
Passing this decoder establishes envelope eligibility, not an executable
terrain generator. Other TGEN versions, outer families, and top-level optional
children are rejected by this bounded implementation.

## DATA schema

`S` below means a NUL-terminated byte string, with its terminator owned by DATA.
No text encoding, Unicode normalization, filesystem access, or dependency fetch
is implied. The API's string-copy accessors omit that terminator. `W` means
exactly four raw little-endian bytes. The client uses raw copying reader
`0x00A94F00`; string readers `0x00A95200` and `0x00A95270` scan byte NULs.

| Order | Wire representation | Client destination | Meaning / confidence |
|---|---|---|---|
| 1 | S | `+0x20` | Terrain filename label, strongly inferred with server correlation; retained as bytes |
| 2 | F32 raw bits | `+0x24` | World size, strongly inferred; F32 accessor `0x00B6E500` confirmed |
| 3 | F32 raw bits | `+0x28` | Chunk size, strongly inferred; F32 accessor `0x00B6E510` confirmed |
| 4 | W | `+0x2C` | Tiles per chunk, strongly inferred; signed `fild` in derived chunk-size division confirmed |
| 5 | W | nonzero becomes byte at `+0x30` | Global water flag, strongly inferred; exact nonzero Boolean conversion confirmed |
| 6 | F32 raw bits | `+0x34` | Global water height, strongly inferred; F32 accessor `0x00B6E540` confirmed |
| 7 | F32 raw bits | `+0x3C` | Global water shader size, strongly inferred; F32 accessor `0x00B6E560` confirmed |
| 8 | S | `+0x38` | Water shader label, strongly inferred; retained as bytes |
| 9 | F32 raw bits | `+0x40` | Environment cycle duration, confirmed by prior client consumer evidence and getter `0x00B6E6C0` |
| 10 | Only0013: `S S W S W S W S W W S` | discarded temporary values | Six strings and five words confirmed; meanings and discarded word types unknown |
| 11 | Twenty W, in order | `+0x44` through `+0x90`, step4 | Exact structure and typed accessors confirmed; positional API avoids unverified semantic names |
| 12 | Remaining bytes, possibly empty | no semantic interpretation | Unknown DATA tail, copied verbatim |

The metadata words are four consecutive five-word groups. Within each group,
indices0..3 are returned through F32 `fld` accessors and index4 through EAX.
Thus metadata indices0..19 have the repeating typed shape
`F32 F32 F32 F32 raw32`. Integer signedness and gameplay semantics are not
asserted. `GetMetadataWordBits(index)` preserves every bit, including NaNs,
signed zero, and integer high bits. The 20 accessor addresses are
`0x00B6E570 + 16*index`; destination offsets are `0x44 + 4*index`.
The implementation does not expose the client's derived float at `+0x94`.

**Server correlation only:** `MMOCoreORB/src/terrain/ProceduralTerrainAppearance.cpp`,
`parseFromIffStream(IffStream*, uint32)`, lines125..188, provides the terrain,
water, cycle, flora/radial and seed names. It agrees with branch/order/widths,
but calls unsigned reads for the first three group minimum-distance words.
Client getters at `0x00B6E570`, `0x00B6E5C0`, and `0x00B6E610` independently
return those words as F32. Do not infer the client types from the server reads.
The server's group names are leads for subsequent consumer tracing.

## Ownership and modern bounds

`Decode(ReadOnlySpan<byte>, ProceduralTerrainInputLimits?)` snapshots its input,
uses the existing bounded IFF reader, and retains no mutable IFF node. Scalars
are get-only; all byte accessors return separate copies. `Maps` is the accepted
immutable `TerrainMapMasks`, whose inspection methods also clone. The API offers
`CopyDataRecord`, `CopyGeneratorForm`, byte-string copies,
`CopyVersion13Branch`, `CopyUnknownDataTail`, and index-bounded metadata words.
The complete generator copy includes FORM header, size, TGEN type, version form,
and all nested records. DATA copy contains the chunk payload only.

Default budgets are input16MiB, DATA64KiB, generator8MiB, each byte string4096,
100000 IFF nodes, depth32, and tiles per chunk4096. Map budgets use the accepted
map decoder defaults and may be supplied explicitly through `MapLimits`.
Invalid options throw `ArgumentOutOfRangeException`; malformed/over-budget
input throws `InvalidDataException`. Input cap is checked before allocation;
node/depth limits bound parsing. Individual section caps are checked after the
bounded IFF parse. Copies can allocate up to their retained section size.

**Modern stricter eligibility:** world/chunk sizes and cycle duration must be
finite and positive; tiles must be positive and within budget. Active water
(nonzero raw flag) requires finite height and positive finite shader size.
Inactive-water bits, positional metadata words, discarded0013 values, and
unknown tail have no invented finite/range policy. Map eligibility remains its
own contract. No equality between PTAT dimensions and map metadata is imposed.
These checks are replacement policies, not claims that the legacy loader
rejected these values. Missing string NULs are rejected rather than reproducing
legacy boundary behavior. Outer trailing bytes/forms reject; unknown bytes
inside the DATA extent are deliberately retained, not interpreted as an outer
trailer.

## Reproduction and original sample

[Scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/ProceduralTerrainInputScenarios.cs) and
[independent oracle](../../SWG-Godot-Client/poc/round32-tests/Assets/verify_ptat_input.py) cover both
versions, exact budgets, every synthetic truncation, order/tag/version/extent,
active-water eligibility, opaque nonfinite bits, raw byte strings, unknown tail,
composed masks, and owned copies. All fixture bytes are declared synthetic.

```powershell
python poc/round32-tests/Assets/verify_ptat_input.py generate local-private/round32/fixtures.json
dotnet restore poc/round32-tests/Assets/Assets.csproj --configfile local-private/round32/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round32-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round32-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round32/fixtures.json > local-private/round32/native.txt
python poc/round32-tests/Assets/verify_ptat_input.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round32/native.txt
```

Use the available offline .NET8 runtime, empty package-source NuGet config,
cached packages, task-specific APPDATA, and UTF-8 Python. Static audit requires
existing `pefile` and `capstone` packages, discoverable through PYTHONPATH.
No installation or download is required. Original input is read through the
existing priority overlay in native and Python independently; it is never
exported into public fixtures.

Pinned root `C:/Stardust-DEV`, configuration `swgemu_live.cfg`, configuration SHA
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`,
path `terrain/tatooine.trn`, priority42, full1188446 bytes, SHA
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
Version0014: world16384, chunk8, tiles2, water flag0, cycle11280.

| Section | Offset | Bytes | SHA-256 |
|---|---:|---:|---|
| DATA payload (chunk at24) | 32 | 178 | `FB4F9F2EDD59EDB4F75E25AF0BCF2D991944B1A5A340DF557F4B1967D67CEDBF` |
| Complete TGEN form | 210 | 139608 | `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32` |
| Complete direct map form | 139818 | 1048628 | `D3F7B55C110D6454E328507743FA0CF4A126CD6D8A802DC040752543CC18AD9D` |
| WMAP payload | within map | 524288 | `B893659B5BCE74ACFC851BF4613F6B25A8DEB9EC1DEE27D7103910376743F3FF` |
| SMAP payload | within map | 524288 | `D14368C40FDAB9F37C197F9BA0F6C92E7AE236579AC6B3BF6D3FA0E14CB114D9` |

This sample has no0013 branch and no unknown DATA tail. No installed0013
occurrence or terrain corpus sweep was attempted. Synthetic0013 is checked
against exact static read structure, not an original0013 runtime.

Next bounded probe: decode TGEN/0000 group headers and one layer/height-affector
family with client factory evidence and independent byte fixtures, using the
owned generator form here. Consumer tracing for the four metadata groups can
proceed separately before applying their inferred flora/radial names.
