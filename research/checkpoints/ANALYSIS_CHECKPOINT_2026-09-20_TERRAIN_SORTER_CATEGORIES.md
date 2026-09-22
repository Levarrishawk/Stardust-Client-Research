# Terrain sorter category closure

Date: 2026-09-20  
Target: `C:\Stardust-DEV\Stardust.exe`  
Scope: `0x0084E510`, `0x0084EBA0`, `0x0084EDB0`, `0x008612B0`, `0x008618D0`, `0x00865DA0`, `0x008661A0`

## Result

The five top-level queues owned by the procedural-terrain sorter are **not** the global shader primitive render phases. They are terrain batching categories keyed by the material-complexity value stored in each generated terrain primitive.

The generator establishes the category as follows:

1. `terrain_build_cell_material_blend_descriptor_VA_008612B0` samples the 3-by-3 material neighborhood, constructs a sorted set of relevant material/shader entries, and writes the relevant-entry count to output offset `+0x08`.
2. `client_procedural_terrain_build_chunk_primitives_VA_008618D0` reads that output member and adds one.
3. `terrain_shader_primitive_construct_VA_00865DA0` stores the resulting value at primitive offset `+0x108`.
4. `terrain_sorter_submit_primitive_VA_0084EDB0` reads `primitive+0x108` and uses it directly as the index into the five-element outer batch vector.

The executable allocates exactly five category entries at install time. Consequently, the generator invariant constrains its stored category to indices `1..4`; index `0` is a reserved/default category not selected by this observed generated-primitive path. The exact historical source names for the four nonzero categories are unavailable. The behavioral meaning is nevertheless closed: increasing category tracks increasing relevant material/blend complexity, rather than a renderer pass identity.

## Submission and batching behavior

`client_procedural_terrain_submit_visible_primitives_VA_008661A0` performs frustum/plane visibility testing and calls the terrain sorter for the ordinary terrain sorter plus optional additional sorter handles supplied by its caller. Therefore one visible primitive can be entered into more than one terrain pass, but each entry retains the primitive's category from `+0x108`.

Within a category, `terrain_sorter_submit_primitive_VA_0084EDB0` additionally reads `primitive+0x120` as the material/shader identity, locates or creates the corresponding batch, and appends the primitive subject to the batch's `0x40000` capacity threshold. `terrain_sorter_flush_batches_VA_0084EBA0` then walks the categories and emits the grouped primitive lists.

This gives the conceptual key:

```text
terrain pass/sorter instance
  -> material-complexity category [0..4]
     -> material/shader identity
        -> capacity-bounded primitive batch
```

## Confidence

- **Confirmed:** the outer vectors contain exactly five entries.
- **Confirmed:** primitive offset `+0x108` is used directly as the outer category index.
- **Confirmed:** the chunk builder stores descriptor count plus one into that field.
- **Confirmed:** material/shader identity at `+0x120` supplies the next grouping level.
- **Strongly inferred:** category zero is reserved/default and generated terrain primitives occupy categories 1 through 4. This follows from the five-entry allocation, direct unchecked indexing, and the count-plus-one producer invariant.
- **Unknown:** original human-readable names for category indices 0 through 4.

## Replacement-client consequence

Do not map these values onto ENVM/TERR/MAIN/etc. phases. Preserve this as an internal procedural-terrain batch key. A replacement may redesign the batching implementation, but it must retain equivalent grouping by material/blend complexity and material identity so original multi-layer terrain shaders and their pass duplication remain representable.
