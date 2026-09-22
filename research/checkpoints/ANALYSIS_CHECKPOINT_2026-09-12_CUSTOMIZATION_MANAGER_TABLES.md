# Analysis checkpoint: customization-manager tables

Scope: the effective `customization/customization_id_manager.iff` and
`customization/asset_customization_manager.iff` selected by
`C:\Stardust-DEV\swgemu_live.cfg`, correlated with Stardust.exe and the
Stardust server parser.

## Findings

Both effective files come from priority 53 `stardust_3_01.tre`:

| Resource | Bytes | Root/version |
|---|---:|---|
| `customization/customization_id_manager.iff` | 3,697 | `CIDM/0001` |
| `customization/asset_customization_manager.iff` | 458,340 | `ACST/0000` |

### CIDM

`CIDM/0001/DATA` is a record stream with no leading count:

```text
repeat until DATA exhausted:
    uint16_le id
    char variableName[] // NUL terminated ASCII
```

The effective corpus has 121 rows, IDs 1 through 121 in exact consecutive
order, no duplicate IDs, and no duplicate names. `Stardust.exe` reads the ID
as a signed 16-bit intermediate, enforces the expected consecutive value,
diagnoses IDs >= 128, allocates an owned string, and inserts it into the
name-to-record map. A duplicate name is diagnosed. Unsupported forms are
diagnosed and not parsed. These mechanics are **Confirmed by code and corpus**
at `0x00B54940`, `0x00B54A00`, and `0x00B54CC0`.

The ID ceiling explains the customization wire codec's seven-bit ID field.
The table contains 120 slash-rooted names and the shipped spelling
`private/alternate_shader_blade` without a leading slash at ID 118. A
replacement must use names verbatim rather than normalizing this anomaly.

### ACST normalized schema

`Stardust.exe` opens each chunk in this exact sequence and derives record
counts by the widths below. The Stardust server parser independently uses the
same layout.

| Chunk | Record layout | Width | Effective count |
|---|---|---:|---:|
| `NAME` | packed NUL-terminated ASCII pool | byte | 9,730 bytes / 336 strings |
| `PNOF` | `uint16 NAME offset` | 2 | 226 |
| `VNOF` | `uint16 NAME offset` | 2 | 110 |
| `DEFV` | `int32 defaultValue` | 4 | 193 |
| `IRNG` | `int32 minimum, int32 exclusiveMaximum` | 8 | 28 |
| `RTYP` | `uint16 typeAndIndex` | 2 | 254 |
| `UCMP` | `uint8 VNOF index, uint8 RTYP index, uint8 DEFV index` | 3 | 1,079 |
| `ULST` | `uint16 UCMP index` | 2 | 30,108 |
| `UIDX` | `uint16 key, uint16 ULST start, uint8 count` | 5 | 11,594 |
| `LLST` | `uint16 inherited key` | 2 | 33,308 |
| `LIDX` | `uint16 key, uint16 LLST start, uint8 count` | 5 | 20,359 |
| `CIDX` | `uint32 filename CRC, uint16 key` | 6 | 26,080 |

All indices in the effective corpus are one-based except NAME byte offsets
and UIDX/LIDX list starts. In `RTYP`, bit 15 selects a palette type and bits
0..14 are a one-based PNOF index; otherwise the low 15 bits select a one-based
IRNG record. `IRNG.maximum` is exclusive. Direct variables are expanded first.
Linked keys are then recursively expanded in LLST order; an already-present
variable name wins and is not replaced. The boolean API option suppresses
names containing `/shared_owner/`.

At `0x01241330`, palette rows resolve the `.pal` path and construct a palette
customization variable; integer rows resolve the min/max pair and construct a
ranged variable. The function then recurses through LIDX/LLST. The top-level
lookup at `0x012412B0` CRC-searches CIDX and passes its key into this expansion.
These semantics are **Confirmed by client code**.

### Corpus validation and coverage

The reusable validator `inspect_customization_managers.py` found:

- zero invalid references and zero inheritance cycles;
- 26,082 total internal keys (two are inheritance-only, not CIDX roots);
- every CIDX appearance expands to at least one variable;
- 1 to 46 effective variables per appearance, mean 3.10;
- 1,077 reachable UCMP records: 111 ranged and 966 palette bindings;
- two unreachable `/shared_owner/index_color_skin` highlight-palette records.

The corpus includes 226 distinct palette paths and 28 integer ranges. It is a
data-driven appearance contract, not a short species-only lookup.

### Confirmed malformed CIDX tail

CIDX is ascending by unsigned CRC except for its final row:

```text
... 0xFFFD72EB -> key 17646  shader/dress_s18_bottom_hcsb21_sm.sht
    0xE201EE84 -> key 52048  appearance/togruta_f.sat
```

The names were resolved by scanning actual TRE member names with the same
MSB-first `0x04C11DB7` String CRC used by Stardust. Keys 52049 and 52050 are
inheritance-only descendants of the appended root. The comparator at
`0x01241310` is a conventional unsigned key-versus-row comparator, and
`0x012412B0` calls CRT binary search over all 26,080 rows. Simulation misses
both displayed rows. The defect is **Confirmed by code and effective corpus**.
A replacement should validate CIDX and build an order-independent index.

## Stable provisional names

| Address | Provisional name | Confidence |
|---:|---|---|
| `0x00B54940` | `customization_id_manager_read_form` | Confirmed |
| `0x00B54A00` | `customization_id_manager_parse_v1` | Confirmed |
| `0x00B54CC0` | `customization_id_manager_install` | Confirmed |
| `0x01240CD0` | `asset_customization_manager_read_form` | Confirmed |
| `0x01240D90` | `asset_customization_manager_parse_v0_tables` | Confirmed |
| `0x012412B0` | `asset_customization_find_root_key_by_crc` | Confirmed |
| `0x01241330` | `asset_customization_expand_key_recursive` | Confirmed |
| `0x01241600` | `asset_customization_manager_install` | Confirmed |

## Replacement contract

Parse into pointer-free records; validate all widths and references before
publication; preserve signed 32-bit ranges/defaults and explicit one-based
indices; detect cycles; apply direct-before-inherited name precedence. File
CRCs and table integers remain 32/16/8-bit on x64. Palette content is a
separate resource dependency; ACST only selects its path and default index.

## Remaining boundary

This closes both manager schemas. Remaining appearance work is palette-file
decoding, structural version coverage for SAT/SKT/MGN/LMG/LAT/ANS, and exact
animation-controller frame-order integration.
