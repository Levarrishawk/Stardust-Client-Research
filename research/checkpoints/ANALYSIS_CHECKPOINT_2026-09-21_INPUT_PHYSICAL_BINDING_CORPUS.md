# Input Physical-Binding Corpus

Date: 2026-09-21

## Scope

This block applies the confirmed `IMAP/0006` decoder to every effective input
map in the configured TRE overlay and resolves its physical bindings against the
effective command dictionaries. It tests the non-negotiable requirement that a
replacement client consume the original assets rather than rebuilding bindings
by hand.

Reproduction:

```text
input_binding_inventory.py C:\Stardust-DEV input_physical_bindings.csv
```

## Corpus result

Fourteen effective maps parse successfully, all as version `0006`, with 1,939
physical bindings:

| Family | Bindings |
|---|---:|
| `KEYS` | 1,848 |
| `MOSB` | 56 |
| `JOYB` | 19 |
| `JOYX` | 10 |
| `JOYS` | 2 |
| `POV ` | 4 |

The corpus includes nine production-era ground profiles, two added ground
profiles (`swg_classic`, `swg_modern`), two diagnostic camera profiles, two
space profiles and the dedicated UI map. Each map contains six shift-key
records. Binding counts range from 68 for the UI map to 195 for the default
space map.

The exact row-level inventory is `input_physical_bindings.csv`. Together with
`input_command_actions.csv`, it forms a complete asset chain:

```text
physical family/index + map state
  -> command name
     -> four action tuples
        -> numeric action interpreter / semantic payload
```

## Confirmed stale references

Twenty-four binding rows refer to eleven command names absent from the winning
command dictionaries. They are concentrated in later/custom profiles:

| Profile | Unresolved rows |
|---|---:|
| `groundinputmap_debugportalcamera.iff` | 1 |
| `groundinputmap_freecamera.iff` | 1 |
| `groundinputmap_swg_classic.iff` | 9 |
| `groundinputmap_swg_modern.iff` | 11 |
| `spaceinputmap_default.iff` | 2 |

Examples include `CMD_uiRoadmap`, `CMD_toggleSafety`,
`CMD_primaryActionAndAttack`, `CMD_uiMyCollections`, and
`CMD_shipToggleTractor`. The paired executable loader already diagnoses a
missing command and does not create a resolved command pointer, so these are
asset-overlay inconsistencies rather than evidence for hidden command records.
A replacement importer should report and skip each unresolved binding while
continuing to load the profile. It must not invent actions for them.

Two rows in `groundinputmap_mmorpg2.iff` bind the same physical tuple (KEYS
index 14, state zero) to both `CMD_clearCombatQueue` and `CMD_chatBackspace`.
The runtime applies records in serialized order to a dense table, so the later
`CMD_chatBackspace` row wins. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_MAP_INHERITANCE_PRECEDENCE.md`.

Across the corpus, 352 distinct command names are physically referenced and 95
effective command definitions have no default binding. Unbound definitions are
valid because commands may be invoked through UI, console, scripts, or user
customization.

## Device-family and state fields

Observed MAP `INFO` shift-state values are `0`, `2`, `4`, and `6`; map flags are
0 or 1. The command U32 formerly labeled as an allowed shift-state mask is now
confirmed as an allowed device-family mask. The loaders compare it against
constants `0x1` for keyboard/mouse/button, `0x2` for POV, `0x4` for joystick
axis, and `0x8` for joystick slider. This explains every effective command mask
and keeps MAP `INFO` correctly separate as the shift-state selector.

## Replacement contract

1. Decode all fourteen profiles through one bounded `IMAP/0006` reader.
2. Resolve binding names only after loading the referenced external command
   dictionary.
3. Emit diagnostics and skip unresolved names; never synthesize action IDs.
4. Preserve serialized order; later duplicate bindings overwrite earlier ones.
5. Translate legacy physical indices through a platform adapter. Gameplay sees
   only resolved logical actions.
6. Regression-test importer output against all 1,939 rows and the 24 expected
   unresolved references.

## Confidence

- **Confirmed by assets:** profile list, versions, command-file references,
  shift records, all binding rows, family totals, duplicate and unresolved
  references.
- **Confirmed by executable reader:** missing command references are diagnosed
  during name resolution and cannot yield a valid command pointer.
- **Confirmed by runtime rebuild:** MAP flag means inherit state-zero bindings;
  base applies first, selected map second, and later duplicate rows win.
