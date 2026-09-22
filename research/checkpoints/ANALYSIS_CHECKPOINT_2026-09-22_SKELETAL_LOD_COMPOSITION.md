# Skeletal LOD selection and mesh composition

Date: 2026-09-22  
Target: verified `Stardust.exe`

## Result

The previously missing integration between SAT distance selection and MLOD
children is now address-backed.

`skeletal_appearance_update_and_select_detail_VA_007C8B60` computes camera
distance, preserves the current detail as a hint, and calls
`skeletal_select_detail_index_VA_007CB240`. The selector rejects an appearance
whose maximum detail is negative, honors a forced-detail state, applies the
configured distance scale, begins at the prior hint, walks interval pairs only
while outside the current range, caps ordinary selection at detail 3 and at the
appearance maximum, and applies existing global/anti-thrash policies.

`skeletal_find_available_detail_VA_007CBBD0` starts at the desired detail and
alternates outward to adjacent indices, rejecting a detail when a generated
mesh or dependent appearance reports unavailable. Missing/streaming content
therefore degrades to a neighboring detail instead of hiding the appearance.

## SAT to MLOD bridge

`skeletal_populate_detail_meshes_VA_007CA9B0` is the exact bridge:

```text
for each SAT MSGN path:
  fetch MeshGeneratorTemplate
  if selectedDetail < template.virtualLevelCount():
    levelTemplate = template.virtualFetchLevel(selectedDetail)
    if levelTemplate:
      generatedMesh = levelTemplate.virtualCreateMesh()
      apply retained customization/environment binding
      append generatedMesh to SkeletalAppearance2[detail]
      release levelTemplate
  release MeshGeneratorTemplate
```

For MLOD, virtual slot `+0x00` is the exposed-level count and slot `+0x04` is
`lod_mesh_generator_template_fetch_level_VA_007F8F10`. Basic SKMG uses the
same interface with one level. SAT owns the ordered generator list, each
template independently consumes the same logical detail index, and meshes are
aggregated into one per-detail appearance vector.

`skeletal_get_or_build_detail_meshes_VA_007CAB40` populates lazily only when a
detail vector is empty. Availability checks traverse that vector plus dependent
or worn appearance records. The four-level MLOD cap and `skipL0` compaction feed
directly into the shared selection index for every SAT mesh component.

## Separate policies and replacement consequence

General appearance detail uses runtime interval pairs plus neighboring-resource
fallback. SAT `LDTB` remains a separate squared-interval policy through
`0x013A4780`; the two decisions must not be conflated. SFSK still forces the
coarse branch for its six banner/flag assets.

Normalize MLOD and basic SKMG behind one template interface; cache generated
meshes per appearance/detail/customization generation; use one logical detail
for every SAT component while tolerating missing levels; search neighboring
available details; preserve hysteresis and caps; and invalidate only affected
detail caches on wearable/customization changes.

## Confidence

- Main update, selector, neighboring fallback and SAT/MLOD bridge: **Confirmed by code**.
- Historical names of tuning fields and anti-thrash counters: **Unknown/naming-only**.

