# Analysis checkpoint: palette-column policy

Scope: effective `datatables/customization/palette_columns.iff`, Stardust.exe
load/lookup behavior, and Stardust server use.

## Effective table

The live file is selected from priority 42
`mtg_patch_013_configurable_02.tre` and is 2,939 bytes. It is a standard
`DTII/0001` table with `COLS`, `TYPE`, and `ROWS`: 15 columns, 37 rows, and no
trailing row data. Its schema is:

```text
PALETTE string
CREATION_COLUMNS int32, CREATION_INDEXES int32
ID_NOVICE_COLUMNS int32, ID_NOVICE_INDEXES int32
ID_LEVEL1_COLUMNS int32, ID_LEVEL1_INDEXES int32
ID_LEVEL2_COLUMNS int32, ID_LEVEL2_INDEXES int32
ID_LEVEL3_COLUMNS int32, ID_LEVEL3_INDEXES int32
ID_LEVEL4_COLUMNS int32, ID_LEVEL4_INDEXES int32
ID_MASTER_COLUMNS int32, ID_MASTER_INDEXES int32
```

Every name resolves to an effective `palette/<PALETTE>.pal`. Thirty-one rows
restrict the creation tier to less than the palette's 256 entries. Six rows
use a creation limit equal to or greater than the actual resource count,
including `10000` as an intentional unlimited sentinel. Every master limit is
at least the actual palette count. The progression limits are uniformly 96,
128, 160, 192, 224, and 256 for novice through master; their display column
count is 16. Creation layout varies by family.

## Client mechanics

The executable's customization-manager installer opens the exact resource and
resolves all 15 columns by the literal names above. For each row it copies the
name plus fourteen signed 32-bit values into the palette-policy map. This is
**Confirmed by code** in the `0x01217853`–`0x01217CCC` region and by the
effective table.

`customization_palette_policy_get_or_default_VA_012191D0` obtains a named row,
creating/copying a default record if absent. `palette_policy_select_tier_pair`
at `0x012216A0` derives the palette name from a palette customization
variable, fetches that record, and selects:

| tier value | returned pair |
|---:|---|
| 0 | creation |
| 1 | image-design novice |
| 2 | level 1 |
| 3 | level 2 |
| 4 | level 3 |
| 5 | level 4 |
| 6 | master |
| other / null | columns 0, index limit 10000 |

It returns columns in EAX and the index limit in EDX. Creation and image-design
mediators pass those values to color-picker layout/limit setters. Randomization
paths additionally intersect the policy limit with the customization
variable's actual range before choosing an index. The index value is an
exclusive upper bound. These mechanics are **Confirmed by code** at client
callers `0x00BF8E20`, `0x00E56200`, and `0x00E56410`.

The Stardust server loads the same 15 fields. Its former validation switch
uses skill levels -1 through 5 for creation through master, but that validation
is commented out with an explicit note that the client UI applies the limits.
Current `validatePalette()` therefore returns true for any integer supplied
for a known palette variable. This is **Confirmed by server source** and is a
security/authority distinction the replacement must not obscure.

## Stable provisional names

| Address | Name | Confidence |
|---:|---|---|
| `0x012191A0` | `customization_palette_policy_copy_by_key` | Confirmed |
| `0x012191D0` | `customization_palette_policy_get_or_default` | Strong (map mechanics confirmed; default insertion internals reconstructed) |
| `0x012216A0` | `palette_policy_select_tier_pair` | Confirmed |

## Replacement contract

Load this DTII as content, not hard-coded species logic. Preserve signed
32-bit fields, interpret index limits as exclusive, intersect them with the
actual `.pal` entry count/ranged-variable maximum, and treat `10000` as a
content-supplied unbounded ceiling. Keep display columns independent from
allowed count; zero columns occurs in shipped creation rows and must not be
used as a divisor.

Client-side UI restriction cannot be the server's trust boundary. A future
server hardening task may re-enable equivalent validation, but this analysis
does not change server behavior.

This closes the palette-column schema and runtime tier selection. The next
related table is `customization_data.iff`, which defines per-species variable
presentation and linkage rather than palette binary content.
