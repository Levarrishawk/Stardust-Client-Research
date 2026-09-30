# Owned floor mesh contract

`FloorMeshDecoder` accepts effective FLOR versions 0003, 0004, 0005 and 0006. Original supported but absent 0000–0002 remain deferred. All integers/floats in payloads are little endian; IFF envelopes use the existing unpadded reader.

## Geometry

VERT stores raw XYZ float triples in 0003–0005; 0006 prefixes U32 count. TRIS likewise stores raw 60-byte records or a 0006 U32 count. Record order: three I32 vertex indices, U32 authored ID, three I32 neighboring triangle ordinals, three F32 authored normal components, three U8 edge flags, U8 non-solid flag, I32 tag, three I32 portal IDs. Indices must be in range; neighbors permit -1. Finite vectors, exact count/stride and one million records per family are enforced before allocation. Authored triangle IDs remain separate from array ordinals. Duplicate IDs are retained with diagnostics. Non-solid bytes and edge flags retain raw values.

## Optional structures

0004 retains optional ordered PNOD/PEDG chunks. 0005/0006 retain optional ordered BTRE, BEDG and PGRF. BEDG has a U32 count and nine-byte records: I32 triangle ordinal, I32 edge integer and U8 raw flag. The client consumes that edge integer modulo three; the reader preserves it, rejects negatives and validates triangle range. Current corpus only uses 0–2. Bounding tree/path graph bytes remain owned opaque metadata. Decoding does not validate their inner acceleration/navigation semantics.

## Evidence and behavior limits

Original 012751E0 consumes counted geometry and dispatches triangle decoding to 012A2850; its 012A2570 helper consumes indices/ID/neighbors, followed by vector, edge/non-solid bytes, tag and portal IDs. BEDG field reads are at 01275536–01275564 and modulo-three selection at 0127557B–01275583. Client post-load normal flips remain separate from authored storage. Server correlation is `MMOCoreORB/src/templates/appearance/FloorMesh.cpp`, `FloorMeshTriangleNode::readObject`; it is corroboration, not the sole client evidence.

[Full effective agreement](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_MESH_READER.md): 4,730 floors, 414,301 vertices, 604,429 triangles and 189,927 BEDG records. Every native owned-field hash and optional payload hash agrees independently. These are format acceptance counts, not movement or navigation coverage. Reproduce with native smoke then `verify_floor_mesh.py ROOT CONFIG PRIVATE_OUTPUT --core-dll DLL`.

Floor movement remains stateful adjacency/path walking, dynamic cell permission and owner/portal transitions. A modern ray query may inspect surfaces but cannot stand in for that runtime contract.
