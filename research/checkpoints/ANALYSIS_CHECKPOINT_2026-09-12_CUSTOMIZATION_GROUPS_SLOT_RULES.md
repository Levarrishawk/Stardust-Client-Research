# Stardust.exe checkpoint: customization group instances and slot rules

Scope: static analysis of `C:\Stardust-DEV\Stardust.exe` (SHA-256 prefix
`E782BF1C49B3`) and the effective configured asset overlay. This checkpoint
does not rely on `SWGEmu.exe`.

## Executive result

Two formerly open customization boundaries are now closed for replacement
architecture:

1. `datatables/customization/customization_group_instance.iff` specifies how
   each species/gender customization group is framed by the editor camera and
   whether its color is linked to hair or body.
2. `customization/slot_rule_manager.iff` is a four-node-type recursive Boolean
   rule language used to inject derived customization variables according to
   occupied equipment slots.

These are content-driven policies. A replacement client should decode them
rather than hard-code a particular species list or equipment combination.

## Customization group-instance table

The effective resource is supplied by
`mtg_patch_013_configurable_02.tre` (overlay priority 42), is 3,888 bytes, and
is `DTII/0001`. It has 96 rows and these six columns:

| Column | Wire/table type | Replacement meaning |
|---|---|---|
| `SPECIES_GENDER` | string | top-level editor key |
| `CUSTOMIZATION_GROUP` | string | group key within species/gender |
| `IS_COLOR_LINKED_TO_HAIR` | int32 boolean | group color follows hair color |
| `IS_COLOR_LINKED_TO_BODY` | int32 boolean | group color follows body color |
| `LOOK_AT_BONE` | string | editor camera target bone |
| `ZOOM_FACTOR` | float32 | camera framing scale |

Corpus results:

- 20 species/gender keys and 16 group names occur.
- 84 rows set neither color-link flag, 8 link to hair, and 4 link to body;
  no row sets both.
- Camera tuples are: `root/1.0` (20 rows), `root/1.17` (4),
  `head/1.1` (59), `head/1.3` (5), and `face_focus/0.8` (8).
- Species have 4-6 group rows; the file is sparse and therefore must not be
  represented as a rectangular species-by-all-groups array.

At `0x01216178` the client opens the table and loops over every row. The code
at `0x012161D0-0x0121659D` reads the columns by name, builds a composite
species/group key, and stores a record containing both strings, the two
booleans, target bone, and zoom factor in the global map at `0x0199B300`.
`0x012182D0` performs the two-key lookup and returns the stored payload.

Confidence: **Confirmed by executable and effective asset corpus**.

## Slot-rule file grammar

The effective 650-byte resource comes from
`mtg_patch_012_configurable_01.tre` (priority 41):

```text
SRMG/0000
  BIND
    INFO  NUL slot-name, NUL customization-variable-name
    rule
```

`BIND` repeats to end of form. Both INFO strings are read with a 255-byte
maximum. The recursive `rule` is one of:

```text
SLOT/0000/INFO  NUL slot-name
NEGT/0000       rule
ANDT/0000       rule rule
ORTS/0000       rule rule
```

The client registers factories for all four tags, peeks the next form tag,
looks it up in the registry, then invokes its factory. Unknown tags and
versions produce diagnostics rather than being interpreted generically.

The four effective bindings are:

| Destination variable | Gate slot | Boolean rule |
|---|---|---|
| `/shared_owner/blend_jacket_belt` | `chest2` | occupied(`chest2`) |
| `/shared_owner/blend_jacket_bandolier` | `chest2` | occupied(`chest2`) AND NOT (occupied(`chest3_l`) OR occupied(`chest3_r`)) |
| `/shared_owner/blend_robe_bandolier` | `chest3_l` | occupied(`chest3_l`) OR occupied(`chest3_r`) |
| `/shared_owner/blend_chest_backpack` | `chest2` | occupied(`chest2`) |

The first slot in each BIND is an applicability gate. At `0x0063F6C0`, after
an object's slotted container/appearance state is prepared, the client loops
over every binding. It tests whether the target object supports the BIND slot;
for supported bindings it calls `0x00598FF0`, which adds the named derived
customization variable backed by the Boolean rule to the target customization
data. Thus the AST is evaluated against live occupied-slot state and drives
wearable/body blending; it is not a one-time asset-load condition.

## Stable provisional names

| Address | Provisional name | Confidence |
|---:|---|---|
| `0x012182D0` | `customization_find_group_instance` | Strong |
| `0x00598C00` | `slot_rule_manager_read_form` | Confirmed |
| `0x00598CC0` | `slot_rule_manager_parse_v0_bindings` | Confirmed |
| `0x00598F30` | `slot_rule_manager_install` | Confirmed |
| `0x00598FC0` | `slot_rule_manager_get_binding_count` | Confirmed |
| `0x00598FD0` | `slot_rule_manager_get_binding` | Confirmed |
| `0x00598FF0` | `slot_rule_apply_binding_to_customization` | Strong |
| `0x00599410` | `slot_occupied_rule_evaluate` | Confirmed |
| `0x00599910` | `slot_rule_not_evaluate` | Confirmed |
| `0x00599D20` | `slot_rule_and_evaluate` | Confirmed |
| `0x0059A170` | `slot_rule_or_evaluate` | Confirmed |
| `0x0059A410` | `slot_rule_parse_polymorphic` | Confirmed |
| `0x0063F6C0` | `object_apply_slot_derived_customization_rules` | Strong |

Factory registration is at `0x005991D0` (SLOT), `0x005996D0` (NEGT),
`0x00599AD0` (ANDT), and `0x00599F20` (ORTS). RTTI independently names
`SlotOccupiedRule`, `NegateRule`, `AndRule`, and `OrRule`.

## Replacement contract

- Load both resources through the normal overlay/VFS precedence rules.
- Validate column names/types and reject duplicate composite keys.
- Preserve bone names and zoom as content; do not embed species-specific UI
  camera logic in code.
- Parse slot rules into a bounded typed AST. Limit depth and node count even
  though the shipped file is tiny.
- Resolve slot names through the slot-ID registry, but retain the source name
  for diagnostics.
- Re-evaluate derived variables when relevant slot occupancy changes.
- Keep editor presentation linkage separate from replicated customization
  strings: neither table changes the wire encoding.

## Remaining uncertainty

Historical editor-facing names for internal map/node classes are not needed.
The exact concrete C++ type returned by `0x00B399A0` is only Strongly inferred
as the object's customization-data owner, but the call chain and resulting
variable/rule insertion are sufficient to reproduce behavior. No open wire or
asset-format field remains in either resource.

