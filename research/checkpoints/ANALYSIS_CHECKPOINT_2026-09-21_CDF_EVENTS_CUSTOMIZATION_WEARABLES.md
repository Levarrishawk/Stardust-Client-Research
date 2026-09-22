# CDF Events, Customization, and Wearables

Date: 2026-09-21

## Result

This block closes the most common remaining `ClientDataFile` leaf records and the runtime path that turns `WEAR` records into skeletal wearables. It is based on `Stardust.exe` loader/runtime control flow and an effective configured-overlay NPC CDF, not on tag-name inference alone.

## Event resource map (`EVNT`, `CEFT`, `CSND`)

All three tags have the same serialized schema:

1. NUL-terminated event name;
2. NUL-terminated resource path.

The loader converts both strings into CRC-string/resource objects and inserts them into the single ordered map at `ClientDataFile +0x34`, keyed by event name. Insertion is conditional on the key not already being present, so the first definition within the effective file wins for a duplicate event key. The apparent tag category does not determine the resource class: corpus examples prove that `CEFT` may refer to `.cef` client effects or `.snd` surface sounds, while `CSND` normally refers to sound resources. Runtime dispatch must therefore select by the resolved resource/path type.

The effective `client_berok_skoff.cdf` provides representative entries:

- `CSND hitlight -> voice/sound/voice_npc_hum_m_dmg_lgt.snd`;
- `CEFT footstep -> clienteffect/e3_player_footstep.cef`;
- `CEFT footstep_grass -> sound/fs_out_grass_bare.snd`.

`FUN_00604110` preloads resources from this map. It examines the first path character and routes `c...` paths through the client-effect preload branch and `s...`/`v...` paths through the sound/voice resource branch. This confirms a common event dictionary with suffix/path-family-aware consumers.

## Object customization (`CSSI`)

`CSSI` is exactly:

1. NUL-terminated customization-variable path (read into a bounded 255-byte temporary);
2. little-endian signed/int32 value.

Each record is retained in the vector at `ClientDataFile +0x5c`. Effective assets use both appearance-local and shared-owner paths, including `/private/index_color_1` and `/shared_owner/blend_*`. Values are full 32-bit integers on disk even when their practical ranges are small; the replacement must not silently narrow them to bytes.

## Wearable form (`FORM WEAR`)

`FUN_006fed50` allocates a 16-byte legacy record and `FUN_006ff150` parses an unordered `FORM WEAR` containing:

- `MESH`: one NUL-terminated logical path to a skeletal logical mesh (`.lmg`), read with a 519-character bound and loaded through the skeletal wearable/mesh factory;
- zero or more `WCSI` chunks, each consisting of a NUL-terminated customization-variable path (255-character bound) followed by one little-endian int32 value.

Unknown child tags are skipped with an IFF diagnostic flag rather than misparsed. The effective Berok Skoff asset demonstrates ten wearable forms and values such as:

```text
MESH appearance/mesh/armor_mandalorian_imperial_chest_plate_m.lmg
WCSI /private/index_color_1 = 60
```

The `WEAR` record contains the loaded wearable appearance/mesh object plus a vector of `(path, int32 value)` customization assignments.

## Runtime wearable construction

For every `WEAR` entry, `FUN_006feec0`:

1. obtains the target object's skeletal appearance/customization owner;
2. creates a customization-data object and imports the target's existing customization variables;
3. iterates the wearable's WCSI list;
4. looks up each variable and uses RTTI to require `RangedIntCustomizationVariable` before applying its value;
5. attaches the customization data to the wearable skeletal appearance;
6. wears/attaches the resulting skeletal object through the established skeletal-appearance path;
7. releases temporary customization references.

When all entries have been applied, `FUN_006fedb0` marks the owning skeletal appearance's wearable state dirty (`+0x88 = 1`) so it can rebuild/reconcile the combined appearance.

The literal `shared_owner` and `shared_owner/` paths in the runtime establish deliberate propagation between owner customization and worn meshes. This is a required behavior for matching body morphs, palettes, and armor coloration.

## Replacement-client consequence

- Treat the event table as `event key -> typed logical resource`, not as separate effect and sound maps.
- Preserve first-key-wins behavior within a CDF unless corpus testing proves overlay generation already removes duplicates.
- Model CSSI/WCSI values as signed 32-bit file values and validate them against the target variable's runtime range.
- A wearable is a skeletal mesh contribution plus customization overrides, not an independently rendered rigid child.
- The modern character system must support owner-to-wearable customization propagation and a deferred combined-skeleton/appearance rebuild.
- Legacy raw pointers and fixed object sizes are internal implementation artifacts and must not be copied into the x64 design.

## Confidence

- `EVNT`/`CEFT`/`CSND` byte schema and common map: confirmed.
- Path-family preload routing: confirmed.
- `CSSI` and `WCSI` string-plus-int32 schema: confirmed.
- `WEAR` MESH/WCSI grammar and runtime ranged-int assignment: confirmed.
- Exact original C++ class name for the 16-byte WEAR wrapper: unknown; provisional naming is used.
