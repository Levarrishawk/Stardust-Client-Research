# Analysis checkpoint: customization presentation data

Scope: effective `datatables/customization/customization_data.iff` and its
Stardust.exe loader/top-level consumer. This table describes character
customization UI and edit semantics; it is distinct from ACST's authoritative
appearance-variable type/range graph.

## Effective table and exact schema

The effective file is priority 42 `mtg_patch_013_configurable_02.tre`, 52,972
bytes, `DTII/0001`, with 18 columns and 467 rows. The exact columns/types are:

| Column | DTII type |
|---|---|
| `SPECIES_GENDER` | string |
| `CUSTOMIZATION_GROUP` | string |
| `TYPE` | string |
| `CUSTOMIZATION_NAME` | string |
| `VARIABLES` | string |
| `IS_SCALE` | bool/int32 |
| `REVERSE` | bool/int32 |
| `COLOR_LINKED` | string |
| `COLOR_LINKED_TO_SELF_0` | string |
| `COLOR_LINKED_TO_SELF_1` | string |
| `CAMERA_YAW` | float32 |
| `DISCRETE` | bool/int32 |
| `RAMDOMIZABLE` | bool/int32 |
| `RANDOMIZABLE_GROUP` | bool/int32 |
| `IS_VAR_HAIR_COLOR` | bool/int32 |
| `IMAGEDESIGN_SKILLMOD` | string |
| `SKILLMOD_VALUE` | int32 |
| `MODIFICATION_TYPE` | string |

`RAMDOMIZABLE` is the literal shipped misspelling and must be queried exactly.
The executable resolves each column by these literals, reads each row, converts
boolean cells by equality with 1, constructs a 0x70-byte logical row, and
inserts it into a hierarchy keyed by species/gender and customization group.
This is **Confirmed by code** in `0x01216600`–`0x01217848` and by the corpus.

## Corpus model

- 20 species/gender keys (male and female for the ten classic playable species);
- 16 presentation groups;
- 338 `hslider` rows and 129 `color` rows; no other control types;
- every `(species_gender, group, customization_name)` tuple is unique;
- every color row names exactly one variable;
- 199 rows name one variable, 248 name two, and exactly 20 name none;
- all 20 no-variable rows are `height` sliders with `IS_SCALE=1`, confirming
  that height edits creature scale rather than a customization byte variable;
- 443 rows are randomizable, 47 discrete, 20 scale controls, 20 hair-color
  variables, 6 reversed controls, and 4 randomizable-group rows;
- modification classes are 384 `physical` and 83 `cosmetic`;
- image-design skill-mod buckets are `body` (129), `face` (276), `hair` (29),
  and `markings` (33), with required values 1–10 or 12;
- camera yaw values are restricted to 0, 180, 195, 200, 225, 250, and 270 degrees.

Six reversed rows are female `chest` / `blend_flat_chest`. The four grouped
randomization rows are female `color_eye_shadow`. Color-link fields are sparse:
3 general, 14 self-0, and 10 self-1 links. These are useful regression fixtures
because a generic importer that treats empty strings as missing columns would
break only a small subset of characters.

## Runtime route

`customization_find_species_gender_VA_01218290` queries the loaded hierarchy.
Creation/image-design setup at `0x00E54B10` derives a species/gender token from
the creature template (`shared_... .iff`), queries the table, iterates its
groups and rows, and constructs UI controller models. Later update/randomize
paths use ACST variables/palette ranges together with the row flags and the
palette-column policy. This route is **Confirmed by code**.

The following field roles are **Strongly inferred from named-column access and
their dedicated consumers**, not claimed as recovered historical source:

- `TYPE` chooses horizontal slider versus color picker;
- `VARIABLES` maps a logical two-ended slider or single color control to the
  underlying customization variable names;
- `REVERSE`, `DISCRETE`, and `IS_SCALE` alter value mapping;
- color-link fields propagate a selected color to related local variables;
- `CAMERA_YAW` selects presentation framing for that control;
- randomization flags govern individual/group random selection;
- skill-mod category/value and modification type gate image-design operations.

## Server correlation

The current Stardust server does not parse this table. It handles the resulting
ImageDesignData/customization values and relies on client-side presentation and
skill filtering. This table is therefore essential for behavioral UI parity,
but it is not a network wire definition and must not become a server authority
assumption.

## Stable provisional name

| Address | Name | Confidence |
|---:|---|---|
| `0x01218290` | `customization_find_species_gender` | Confirmed |

The encompassing customization-manager installer is intentionally not given a
fabricated start address: Ghidra's current inventory failed to recover the
large function boundary, while the table-specific code region is exact.

## Replacement contract

Load the DTII into immutable engine-neutral descriptors. Preserve literal
column names and signed/floating widths; split `VARIABLES` as an ordered comma
list; keep empty link strings distinct from absent schema; validate the two
supported control kinds; index by species/group while retaining file row order.
Resolve named variables against the typed ACST directory and treat height scale
as a separate object transform field. UI layout may be modernized, but these
data-driven groupings, links, limits, reversal, camera intent, and skill gates
are observable client behavior.

This closes the table schema, corpus, loader, and top-level route. Exact visual
widget geometry and localized labels are UI breadth, not unknown table
semantics.
