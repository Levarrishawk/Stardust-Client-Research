# Appearance, Customization and Animation Architecture

## Confirmed player dependency chain

A representative installed player template resolves as:

```text
object/creature/player/shared_human_male.iff (SCOT)
|-- derives from object/creature/player/base/shared_base_player.iff
|-- appearance/hum_m.sat (SMAT/0003)
|   |-- appearance/mesh/hum_m_body.lmg
|   |-- appearance/mesh/hum_m_head.lmg
|   |-- appearance/skeleton/all_b.skt
|   |-- appearance/skeleton/hum_m_face.skt
|   |-- appearance/lat/all_m.lat
|   `-- appearance/lat/hum_m_face.lat
`-- clientdata/player/client_shared_player_human_m.cdf (CLDF/0000)
    |-- voice and body sound templates
    |-- surface-specific footsteps
    `-- combat/footstep client effects
```

The resource paths and top-level forms are **Confirmed by read-only extraction from the configured TRE corpus**.

## Client runtime class model

Stardust.exe RTTI confirms:

- `SkeletalAppearance2` and `SkeletalAppearanceTemplate`;
- basic and LOD skeleton templates plus skeleton transform-name maps;
- basic, LOD and skeletal mesh-generator templates and generated meshes;
- full-geometry skeletal batch rendering and local shader primitives;
- keyframe animation with quaternion/vector operations;
- direction, yaw, speed, time-scale, controlled/timed blend, priority and masked-priority animation nodes;
- string-selector and proxy animation templates;
- track and transform animation controllers;
- animation-state hierarchy templates, states, links and state-change actions.

This is a layered graph/controller system, not a simple “play clip by name” interface. A replacement can redesign execution, but it needs an intermediate animation graph capable of selectors, priorities, masks, time scaling, directional/yaw selection, state transitions and logical track mapping.

## Wearables and skeletal attachment

Wearable replication supplies customization text, containment/arrangement, network object ID and client template CRC. `AutoDeltaVector<WearableEntry,CreatureObject>` decodes these into 0x20-byte legacy runtime values. Stardust.exe diagnostics state that a wearable is attached to a `SkeletalAppearance` and reject null or non-skeletal wearable appearances.

`WearableAppearanceMap` is installed from `datatables/appearance/wearable_appearance_map.iff`, with source and mapped wearable appearance names. RTTI identifies persistent and temporary map entries. This indicates an appearance-remapping layer between a wearable's nominal template appearance and the appearance attached for a particular wearer/body context.

The installed table is `DTII/0001` with exactly three string columns—`source_wearable_appearance_name`, `wearer_appearance_name`, and `mapped_wearable_appearance_name`—and 55 rows. Lookup `0x011B9FF0` binary-searches the sorted entry vector using `(source appearance, wearer appearance)`. A found, non-empty mapped name substitutes the contained object's appearance before skeletal attachment. A found row whose mapped name is empty suppresses attachment. No match preserves the nominal appearance. The separate path at `0x00647E20` queries an empty source plus wearer appearance and can create a mapped fallback wearable. Table structure, key order, result states, and both consumers are **Confirmed by code**.

The confirmed equipment-presentation flow is split across two protocol mechanisms:

```text
CREO:6 wearable baseline/delta
  -> cache customization, arrangement, object ID and template CRC metadata

UpdateContainmentMessage
  -> resolve child and container objects
  -> apply parent/arrangement through client containment machinery
  -> CreatureObject add/remove virtual callback
  -> lookup source/wearer pair in WearableAppearanceMap
  -> preserve, substitute, or suppress the visual attachment
  -> mount/unmount /shared_owner customization as applicable
  -> SkeletalAppearance2 wear/stop-wearing
  -> retain/remove worn-object watcher and mark affected skeletal slots dirty
```

The split is **Confirmed**. `CreatureObject` constructs the `AutoDeltaVector<WearableEntry,...>` aggregate/remove/insert/replace callback slots as null, and no later constructor installation occurs. The generic vector mutation helpers can invoke those slots, but they are deliberately unused for this field in Stardust.exe. Visible attachment instead follows the object's containment change. The map selection states are now closed; deeper customization-variable merge rules remain incremental work.

### Executable attachment path

`creature_underwear_wearable_refresh_VA_00437450` creates or removes default underwear/fallback wearable objects according to visible-component/body state. Its create path mounts dependent customization under `/shared_owner`, RTTI-checks the wearer's appearance as `SkeletalAppearance2`, and calls `skeletal_wear_object_VA_007C9A50`; its removal path calls `skeletal_stop_wearing_object_VA_007C9C30`, destroys the retained object, and clears the slot. **Confirmed for fallback underwear management.** This demonstrates the skeletal ownership mechanism, but it must not be cited as the generic replicated wearable-vector callback until those installed callback targets are separately traced.

`skeletal_wear_object_VA_007C9A50` rejects null and non-skeletal wearable appearances, retains a watcher/reference to the worn object in the wearer's collection, and calls `0x007CB440`. That helper marks the affected skeletal slot interval dirty; it is not itself the complete rebuild. Removal finds the matching worn object, erases/releases it, and marks the same affected interval dirty. A disable flag at skeletal appearance offset `+0x88` suppresses mutation. This fixes attachment ownership and dirty-state boundaries sufficiently for an engine spike without inventing a monolithic rebuild function.

The non-underwear call path is address-backed. CreatureObject vtable entries at `0x015DCCC0` and `0x015DCCC4` point to add/remove wrappers `0x00436D90` and `0x00437050`. They delegate to `creature_wear_contained_object_VA_0067A060` and `creature_unwear_contained_object_VA_0067A140`; those routines call `skeletal_wear_object_VA_007C9A50` and `skeletal_stop_wearing_object_VA_007C9C30` when the child has a valid appearance. The `UpdateContainmentMessage` branch parses child ID, parent ID and arrangement, resolves the child, then applies the relationship through `client_object_apply_containment_VA_00555020`. This provides the protocol-to-presentation bridge without treating the CREO:6 metadata vector as an object owner.

## Customization model

RTTI and diagnostics confirm ranged-integer, palette-color, slot and const-string variables; local and remote customization directories; template variable mappings; customizable shaders; and blueprint texture renderers. A shared owner's customization directory can be mounted remotely into a wearable/dependent appearance and later dismounted. Diagnostics explicitly cover missing directories, undeclared variables, corrupt persisted strings and ID-table mismatch.

Global resources include:

- `customization/asset_customization_manager.iff`;
- `customization/customization_id_manager.iff`;
- `customization/slot_rule_manager.iff`;
- palette and customization data tables.

The serialized customization string is therefore a compact named/ID-mapped state representation, not merely a material color. It must be decoded into typed customization variables before skeletal mesh blending and shader/palette operations.

### Recovered wire grammar

The Stardust server's `CustomizationVariables` codec provides the exact client-compatible binary grammar, and Stardust.exe diagnostics independently mention the same `0xFF` escape/EOS failures, version 1, 255-variable limit and ID mapping. The encoding is:

```text
decoded-byte version       // currently 1
decoded-byte variableCount // maximum 255 in v1
repeat variableCount:
    decoded-byte idAndSign
    decoded-byte lowValue
    if idAndSign & 0x80:
        decoded-byte highValue
decoded 0xFF 0x03          // raw EOS escape pair
```

`decoded-byte` uses raw bytes except `00 -> FF 01` and `FF -> FF 02`. `FF 03` is end-of-stream and is invalid inside a field. When the high bit of the variable ID is set, the remaining seven bits identify the variable and a second value byte forms a signed 16-bit little-endian value; otherwise the value is a nonnegative byte. IDs map through `customization/customization_id_manager.iff`. A malformed string is rejected transactionally and clears the parsed collection in the server reference.

This grammar is **Confirmed by server source and strongly corroborated by executable diagnostics**. It eliminates packet interception as a prerequisite for customization decoding. The replacement must retain raw input for diagnostics, enforce bounds before decoding, reject unknown escapes/trailing garbage, and apply a parsed set only after complete validation.

## Replacement constraints

### Customization manager tables

The effective CIDM and ACST resources are now decoded and corpus-validated.
CIDM v1 is a repeated little-endian `uint16 id` plus NUL ASCII name stream;
the shipped 121 IDs are unique and consecutive and must remain verbatim. ACST
v0 is a normalized table graph over NAME, PNOF, VNOF, DEFV, IRNG, RTYP, UCMP,
ULST/UIDX, LLST/LIDX, and CIDX. Direct variables override inherited variables
by name; RTYP bit 15 selects palette versus ranged-int records; CIDX maps the
32-bit file-name CRC to the recursive graph root. Exact widths, counts, client
addresses, and validation results are in
`ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_MANAGER_TABLES.md`.

The effective CIDX has one malformed appended ordering transition:
`appearance/togruta_f.sat` follows the maximum CRC row and causes the legacy
binary search to miss both rows. A replacement must construct an
order-independent validated lookup rather than reproduce the miss.

### Palette resources

`.pal` is RIFF `PAL `/`data`: version `0x0300`, a 16-bit entry count, then
RGBA-like four-byte records. Stardust.exe imports RGB as opaque
`0xFFRRGGBB`, deliberately ignores the file flags byte, clamps invalid indices
to entry zero, and uses opaque magenta when a named resource is absent. All
264 effective palettes validate; all 226 ACST references resolve. Three files
contain ignored bytes after their shorter declared RIFF extent, a behavior the
replacement must accept. Full evidence is in
`ANALYSIS_CHECKPOINT_2026-09-12_PALETTE_RESOURCE.md`.

### Palette-column policy

`datatables/customization/palette_columns.iff` is a 37-row DTII table mapping
palette family names to seven `(display columns, exclusive index limit)`
tiers: creation and image-design novice through master. The client returns the
tier pair to color-picker mediators and intersects its limit with the actual
palette-variable range. `10000` is an unbounded ceiling. The current server
loads the same fields but has palette-skill validation commented out, relying
on client UI. Full evidence is in
`ANALYSIS_CHECKPOINT_2026-09-12_PALETTE_COLUMN_POLICY.md`.

### Character customization presentation data

`customization_data.iff` is a 467-row, 18-column DTII table indexed by 20
species/gender keys and 16 logical groups. It specifies slider/color controls,
ordered underlying variable names, scale/reverse/discrete mapping, color links,
camera yaw, randomization, hair-color identity, image-design skill gates, and
physical/cosmetic class. The executable loads it into a species/group hierarchy
and creation/image-design UI queries that hierarchy. Height is represented by
20 scale rows with no customization variable. Full schema and corpus evidence
is in `ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_PRESENTATION_DATA.md`.

### Customization group instances and equipment slot rules

`customization_group_instance.iff` is a 96-row species/group policy table for
editor camera target/zoom and hair/body color linkage. The client stores its
six fields in a composite-key map and exposes a two-key lookup. The separate
`slot_rule_manager.iff` is a recursive SLOT/NOT/AND/OR Boolean AST whose four
bindings inject live, slot-dependent blend variables such as jacket belt,
bandolier, robe, and backpack blends. Exact schemas, shipped expressions, and
consumer call chain are in
`ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_GROUPS_SLOT_RULES.md`.

### Effective appearance/animation version census

A configured-overlay census covers 38,440 effective SAT/SKT/MGN/LMG/LAT/ANS
records. It proves the deployed version matrix and exposes rare importer
paths: SMAT SFSK/APAG flags, 1-7-entry SLOD, 1-8-entry MLOD, SKMG hardpoint,
DOT3, occlusion and texture-renderer families, and 32 CKAT clips carrying
QCHN. See `ANALYSIS_CHECKPOINT_2026-09-12_APPEARANCE_CORPUS_VERSIONS.md`.

SMAT LDTB is a count-driven list of min/max distance intervals. The loader
squares both endpoints; selection begins at the current detail index and scans
only after scaled squared distance leaves that interval. Overlapping intervals
are therefore hysteresis, not corrupt or redundant cutoffs.

MLOD is now closed as a separate container contract. `MLOD/0000` holds a
signed-16 count and repeated NAME asset paths; the corpus contains one to eight
names, but this executable exposes at most four through its virtual interface.
Children are lazy-loaded into a parallel reference-counted cache and explicitly
released. `ClientSkeletalAnimation.skipL0` conditionally omits the first NAME
when more than one exists, compacting later levels down rather than retaining
a null hole. MLOD contains no distances; SAT LDTB remains the
selection-policy source. See `ANALYSIS_CHECKPOINT_2026-09-12_MLOD_RUNTIME.md`.

SLOD mirrors the exposed four-level cap but owns eagerly parsed inline skeleton
templates rather than lazy NAME references. `SLOD/0000` reads a signed-16 count
and recursively dispatches that many children through the skeleton-template
factory. The effective 157-file SLOD corpus carries one to seven SKTM/0002
children. The same `ClientSkeletalAnimation.skipL0` switch can omit child zero.
Both containers reserve then append, so omission compacts later levels down.
See `ANALYSIS_CHECKPOINT_2026-09-12_SLOD_RUNTIME.md`.

- Keep network wearable records separate from instantiated appearance nodes.
- Preserve template CRC and 64-bit object identity without using pointer-sized surrogates.
- Model customization as a typed namespace/tree with explicit local/remote dependency mounts.
- Translate SAT/SKT/MGN/LMG/LAT/ANS data into engine-neutral skeleton, skin, mesh and animation-graph descriptions.
- Support body/face skeleton composition and transform-name remapping.
- Permit palette/shader customization and mesh blend variables to update without recreating unrelated replicated objects.
- Enforce bounded customization-string parsing and reject incomplete/corrupt state transactionally.

## Engine-evaluation implications

For Godot or another engine, the decisive prototype is not loading one static mesh. It is importing one human SAT chain, composing its skeletons and meshes, applying a representative customization string, attaching one remapped wearable, and driving one logical animation state transition. If that requires duplicating the engine skeleton/animation/resource system or maintaining a deep engine fork, a custom client architecture may be safer.

## Remaining analysis

### Skeletal LOD composition closure (2026-09-22)

The former higher-level consumer gap is closed. Skeletal update `0x007C8B60`
selects a shared logical detail with current-index hysteresis/caps and searches
adjacent indices when generated content is unavailable. Lazy builder
`0x007CA9B0` iterates every SAT MSGN path, asks each mesh-generator template
for its exposed level count, fetches the selected child through virtual slot
`+0x04` (MLOD `0x007F8F10`), creates the generated mesh and aggregates it into
the per-detail appearance vector. MLOD and basic SKMG therefore share one
interface and one selected index across body/head/components. See
`ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_LOD_COMPOSITION.md`.

1. ~~Test the explicit SWG-to-host-engine coordinate adapter with pose fixtures.~~
   Closed for Godot: reflect Z for positions/directions, convert SWG
   `[w,x,y,z]` to Godot `[-x,-y,z,w]`, reverse imported triangle winding and
   tangent handedness, and keep wire x/z/y ordering at the codec boundary.
   Identity, axis rotations and a mixed pose pass matrix-conjugation fixtures.
   See `ANALYSIS_CHECKPOINT_2026-09-22_GODOT_COORDINATE_ADAPTER.md`.
2. ~~Map exact animation controller update entry points and main-loop notification ordering.~~
   Closed for the appearance-local contract: transform-cache invalidation,
   ordered controller alter, per-substep root-motion accumulation, synchronous
   message fan-out before replacement installation, readiness/fade progression,
   and periodic inactive-detail eviction are mapped. Cross-object scene order
   remains intentionally unclaimed. See
   `ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_APPEARANCE_ALTER_ORDER.md`.

## Dynamic face textures (2026-09-22)

Sixteen effective head MGN files use the optional TRTS bridge. Their executable
loaders preserve a named blueprint texture-renderer template plus one or more
`{primitiveIndex, textureTag}` bindings; the current corpus uniformly targets
`MAIN`. Face texture composition is therefore part of the original skeletal
appearance contract. See
`ANALYSIS_CHECKPOINT_2026-09-22_MGN_TEXTURE_RENDERER_BINDINGS.md`.
