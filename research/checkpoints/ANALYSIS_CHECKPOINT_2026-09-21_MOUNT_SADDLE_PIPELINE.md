# Mount and Saddle Presentation Pipeline

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The mounted-creature/vehicle presentation path is a data-driven join across four original tables, appearance/skeleton identity, wearable creation, attachment hardpoints, client-data files, animation posture, and scale validation. A replacement cannot render mounts correctly by merely parenting the rider to the mount object.

`SaddleManager` performs the first three joins; `MountValidScaleRangeTable` independently constrains compatible creature/vehicle scales and capacities. The compiled runtime builds sorted polymorphic entry vectors, but those pointer-heavy containers are an implementation detail.

## Install and lookup chain

`SetupClientGame::install` at `0x004297C0` calls `SaddleManager::install` (`0x006476B0`) with:

1. `datatables/mount/logical_saddle_name_map.iff`
2. `datatables/mount/saddle_appearance_map.iff`
3. `datatables/mount/rider_pose_map.iff`

The install routine loads and sorts each table, then registers removal on the exit chain. Missing files are fatal; missing required columns have explicit diagnostics.

| VA | Provisional name | Behavior |
|---|---|---|
| `0x006469E0` | `saddle_manager_load_logical_map` | Reads skeleton/SAT name → logical saddle key. **Confirmed.** |
| `0x00646D40` | `saddle_manager_load_appearance_map` | Reads logical key, capacity, saddle appearance and optional client-data file. **Confirmed.** |
| `0x006470B0` | `saddle_manager_load_rider_pose_map` | Reads saddle appearance, seat index and rider pose. **Confirmed.** |
| `0x00647740` | `saddle_manager_find_logical_saddle` | Sorted lookup by mount SAT/SKT identity. **Strong.** |
| `0x006478A0` | `saddle_manager_find_saddle_appearance` | Sorted lookup by logical saddle and selection key. **Strong.** |
| `0x00647A50` | `saddle_manager_get_client_data_filename` | Returns optional client-data resource from the selected appearance entry. **Strong.** |
| `0x00647B90` | `saddle_manager_find_rider_pose` | Finds pose by saddle appearance and seat index. **Strong.** |
| `0x00647CD0` | `creature_resolve_saddle_for_mount` | Resolves mount appearance template, falls back through skeletal template identity, then resolves saddle appearance/client data. **Confirmed behavior.** |
| `0x00647D80` | `creature_resolve_rider_pose_for_mount_seat` | Resolves saddle, then the rider pose for a seat. **Confirmed behavior.** |
| `0x00647430` | `creature_instantiate_and_wear_mapped_object` | Instantiates mapped saddle object/appearance and attaches it as skeletal wearable. **Confirmed.** |
| `0x01258060` | `mount_valid_scale_range_table_load` | Loads per-appearance capacity and min/max scale ranges into CRC-keyed entries. **Confirmed.** |

## Effective asset schemas and corpus

All four effective tables currently resolve from `mtg_patch_022.tre`.

### Logical identity map

`logical_saddle_name_map.iff`: **105 rows**, 102 unique source identities, 57 logical saddle names.

| Field | Type |
|---|---|
| `sat_or_skt_name` | string |
| `logical_saddle_name` | string |

The lookup may begin with the mount's appearance template and, for a skeletal appearance, fall back to its skeleton template identity. Thus both SAT and SKT names are legitimate keys.

### Saddle appearance map

`saddle_appearance_map.iff`: **60 rows**, 57 unique logical keys, 55 unique saddle appearances.

| Field | Type |
|---|---|
| `logical_saddle_name` | string |
| `saddle_capacity` | int32 |
| `saddle_appearance_name` | string |
| `client_data_filename` | string |

All deployed rows currently store capacity 1, but the runtime and schema retain the field. Creature saddles often use `.apt` appearance resources with an empty client-data value; vehicles may also supply entries such as `clientdata/vehicle/landspeeder.iff`.

### Rider pose map

`rider_pose_map.iff`: **75 rows**, 70 saddle appearances, 35 distinct pose names.

| Field | Type |
|---|---|
| `saddle_appearance_name` | string |
| `seat_index` | int32 |
| `rider_pose` | string |

All effective rows use seat index 1, but the executable lookup is explicitly keyed by seat and must not hard-code that corpus accident. Pose-only records exist for vehicle/chair/walker appearances not present in the saddle-appearance table, showing that the pose service is reused beyond conventional creature saddles.

### Valid scale ranges

`valid_scale_range.iff`: **102 rows**, 75 appearance names.

| Field | Type |
|---|---|
| `appearance_name` | string/CRC identity |
| `saddle_capacity` | int32 |
| `valid_scale_min` | float32 |
| `valid_scale_max` | float32 |

There are 84 capacity-1 rows and 18 capacity-2 rows. Multiple rows per appearance therefore describe capacity-specific compatible scale intervals. These float32 values are original compatibility data, not a suggestion to infer dimensions from mesh bounds.

## Runtime presentation behavior

The mount object's SAT/SKT identity selects a logical saddle class. The logical class selects an actual saddle appearance plus optional client-data resource. That saddle is instantiated and worn on the mount's skeletal appearance. The saddle appearance and requested seat select the rider animation pose. Separately, the rider/mount relationship uses the `rider` hardpoint and `spine1` modifier path already recovered in the skeletal-pose block.

Executable sanity diagnostics enforce a two-way relationship:

- a mounted creature's rider must identify that creature as its mount;
- a rider in `States::RidingMount` must be contained by a creature and that mount must point back to the rider.

The state is replicated/contained through ordinary creature state and containment mechanics; these tables provide visual resolution, not authority to mount.

The effective joins are not perfectly one-to-one. Three logical keys have no current saddle-map row, while three saddle-map logical keys have no logical source row. One saddle appearance lacks a pose and sixteen pose appearances have no saddle-map row. These are legitimate sparse/fallback conditions; lookup failure must be handled without crashing or fabricating a resource.

## Replacement implications

1. Mount original APT/SAT/SKT, client-data, animation and table assets through the overlay VFS.
2. Preserve the three-stage identity join; do not derive saddle filenames from creature names.
3. Keep seat index and capacity as explicit int32 data even though most current rows are 1.
4. Apply table-driven scale compatibility before presentation setup.
5. Represent rider ↔ mount as validated bidirectional object state, with containment and replicated state remaining server-authoritative.
6. Translate the `rider` hardpoint, worn saddle appearance and rider-pose animation into the host engine's attachment/skeleton system.
7. Treat sparse joins as supported fallback cases and log them with stable asset identities.
8. Internal pointers and sorted polymorphic entry objects must be replaced with x64-safe value records keyed by normalized asset identity/CRC.

