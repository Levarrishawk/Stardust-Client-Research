# Static mesh sorted-index slice

The [SIDX research](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TOPOLOGY_SHADER_ENVELOPE.md) and [garage implementation](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SORTED_MESH_GARAGE.md) establish this bounded slice. Other topologies, old SPS/VTXA versions, inline shaders and packed color channels remain outside the current reader.

## Owned structure

MESH 0004/0005, SPS 0001 indexed triangle-list primitives 0000/0001 retain ordinary INDX and optional SIDX. INFO is U32 topology 9, U8 indexed=1 and U8 sorted-presence=0/1. Child count and ordering must agree with the flag. SIDX payload is U32 variant count, then repeated three finite LE F32 direction components, U32 index count and the version-selected index entries. Primitive 0000 entries are U32 narrowed to low U16; 0001 entries are U16. There are no nested INDX envelopes inside SIDX.

Each variant owns its direction and readonly index list. Counts must fit the exact chunk; every list must form triangles and reference available vertices. No trailing bytes are accepted. Replacement limits are 4,096 variants and three million combined base/sorted indices per primitive, in addition to the IFF input limit. Present zero-variant SIDX remains structurally represented and uses the ordinary indices under an explicit modern fallback policy; the original preparation path indexes the first variant and guards an empty vector.

## Selection

`SortedMeshIndexSelector.Select` requires finite camera displacement already expressed in primitive-local space. It selects the minimum dot product and preserves the first entry on ties, matching the strict-less comparison at VA 007707D0. Without variants it returns the original INDX list. Wider double intermediates prevent modern extreme-input overflow; ordinary values are compatible, but bit-identical x87 decisions near precision ties are not claimed. Caller transform conversion is separate. The original preparation code uses rotation transpose on world camera-minus-translation; arbitrary modern shear/scale inverse semantics are not inferred from it.

The neutral Godot inspector uploads the ordinary index list and does not claim translucent ordering or faithful original materials. A later material renderer must supply camera-local displacement and bind the selected variant for the relevant surface.

## Additional declarations

The parser now retains 0x5205 (position, normal, two 2D coordinate sets; stride 40) and 0x35305 (position, normal, two 2D sets plus a 4D set; stride 56). These extend the existing four explicit declarations. Count bits 8–11 and two-bit dimensions beginning at bit 12 determine widths. All declared components remain owned and finite; the final 4D semantic label remains inferred rather than named as confirmed tangent data.

## Independent interchange

`ComputeGeometrySha256` remains unchanged: per primitive LE U32 vertex/index counts, all declared vertex F32 values, then ordinary LE U16 indices. `ComputeSortedIndicesSha256` is separate: per primitive U32 variant count, followed by each three F32 direction components, U32 index count and narrowed U16 indices. This preserves prior geometry hashes while proving every auxiliary ordering byte.

The existing Python mesh reader independently reconstructs both hashes. `verify_component_appearance.py ... --geometry` checks all source identities, mesh/hash/count results and zero omissions for a fully supported static graph. It deliberately fails for unsupported families rather than reproducing part omission heuristics.
