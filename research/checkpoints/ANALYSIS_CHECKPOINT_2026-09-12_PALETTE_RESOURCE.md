# Analysis checkpoint: `.pal` palette resources

Scope: Stardust.exe's palette cache/list loader and every effective `.pal`
resource selected by `swgemu_live.cfg`. Findings are based on
`C:\Stardust-DEV\Stardust.exe`, the actual TRE corpus, and the server's
`PaletteTemplate` parser.

## Exact resource grammar

Palette files use the standard Windows RIFF palette envelope:

```text
char[4]  "RIFF"
uint32le riffSize              // must equal dataSize + 12
char[4]  "PAL "
char[4]  "data"
uint32le dataSize              // must equal 4 + entryCount*4
uint16le version               // 0x0300
uint16le entryCount            // maximum accepted by client: 1024
repeat entryCount:
    uint8 red
    uint8 green
    uint8 blue
    uint8 flags
```

`palette_argb_list_parse_riff_VA_00AB7E60` validates a minimum file size of 24
bytes and maximum of `0x1018` (4,120), requires the three signatures, requires
`riffSize == dataSize + 12`, requires component/version byte 3, accepts at
most 1,024 entries, and requires `dataSize == 4 + 4*entryCount`. The low byte
of the version word is expected to be zero but produces only a warning when
nonzero. This is **Confirmed by code**.

The client converts every RGB triplet into internal `0xFFRRGGBB` and ignores
the file flags byte, forcing alpha to 255. The flags byte is therefore not
render alpha in this build. `palette_argb_list_get_mutable_clamped` logs and
clamps an invalid index to zero; the const accessor also clamps to zero
without that diagnostic. A closest-color helper compares squared distance in
all four internal ARGB bytes. These behaviors are **Confirmed by code**.

## Effective corpus

`palette_overlay_inventory.py` enumerated effective overlay winners without
reading shadowed duplicates as live content:

- 264 effective palettes; zero signature/version/count parse failures;
- 226 are referenced by the effective ACST PNOF table; all 226 exist;
- 38 valid effective palettes are not referenced by that ACST table;
- entry counts range from 2 to 256; common sizes are 64 (70 files), 52 (48),
  and 256 (39); no effective asset approaches the client's 1,024 limit;
- every effective file declares version `0x0300`.

Three files are physically 1,048 bytes but declare shorter RIFF/data extents:
`npc_jinda_light.pal` (192 entries), `npc_skin_nightsister.pal` (201), and
`npc_skin_sing_mt_clan.pal` (234). Their trailing bytes include nonzero data.
The client reads the whole file but validates only the declared RIFF and data
sizes against one another, then imports exactly `entryCount` records; trailing
bytes are ignored. The server parser likewise stops at declared `dataSize`.
This permissive trailing-data behavior is **Confirmed by code and corpus**.

## Cache and fallback behavior

`palette_cache_fetch_VA_00AB6910` returns a retained cached palette by name.
When creation is disabled and no cache row exists, it returns null. When
creation is enabled, it first verifies the TreeFile member. A missing palette
is diagnosed and replaced with a one-entry `0xFF00FFFF` opaque magenta list.
An existing member constructs `PaletteArgbList`, opens it through
`TreeFile::open`, parses it, closes it, and inserts it into the cache. This is
**Confirmed by code**.

## Stable provisional names

| Address | Name | Confidence |
|---:|---|---|
| `0x00AB6910` | `palette_cache_fetch` | Confirmed |
| `0x00AB7B00` | `palette_argb_list_get_count` | Confirmed |
| `0x00AB7B10` | `palette_argb_list_get_const_clamped` | Confirmed |
| `0x00AB7B40` | `palette_argb_list_get_mutable_clamped` | Confirmed |
| `0x00AB7C50` | `palette_argb_list_find_closest_argb` | Confirmed |
| `0x00AB7D30` | `palette_argb_list_construct_from_name` | Confirmed |
| `0x00AB7E60` | `palette_argb_list_parse_riff` | Confirmed |

## Replacement contract

Use fixed-width fields and checked arithmetic; reject unsupported signatures,
versions, counts, and inconsistent declared chunk sizes; accept and ignore
bytes after the declared RIFF extent for compatibility; convert entries to
opaque sRGB-like RGBA while retaining raw flags for diagnostics; clamp invalid
indices to zero at the legacy compatibility boundary. Missing resources need
an explicit opaque-magenta sentinel, not an accidental null texture.

The `.pal` binary format and runtime lookup policy are closed. The remaining
palette-related question is not file decoding: it is the separate
`datatables/customization/palette_columns.iff` permission/column policy used
by character creation and image design.


## Correction recorded 2026-09-30

The historical word “magenta” above is incorrect: original00AB69FC writes0xFF00FFFF, and00AB8135–00AB8182 confirms0xFFRRGGBB packing. The actual missing-resource sentinel is opaque cyan. Historical counts/wording are preserved; the current [owned palette contract](../../docs/PALETTE_DECODER_CONTRACT.md) governs implementation.
