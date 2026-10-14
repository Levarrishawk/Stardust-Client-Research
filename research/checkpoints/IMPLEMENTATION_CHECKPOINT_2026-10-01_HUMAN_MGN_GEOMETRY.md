# October 1, 2026 — Original human MGN shader geometry

## Question and stop

Can the object-root original L0 body/head MGN decode shader-local vertex/UV/triangle maps and retained generated-texture binding with independently matching geometry? Stop before rendering, active occlusion or skinning.

## Evidence and implementation

Read prior corpus/TRTS specifications and current skin-source contract; used existing read-only Ghidra project on verified baseline007FCB50,007FD900,007FB380,007FBA00. PSDT reads shader NAME, counted PIDX, optional per-vertex NIDX/DOT3/VDCL, TXCI dimensions/TCSD arrays and counted PRIM. ITL reads U32 count/threeU32 indices; OITL reads leading I16 per triangle then threeU32. Both resolve indices through PIDX into POSN and omit exact zero-area mapped triangles. Weight helper007FD900 simply stores two4-byte fields; it does not independently establish weighted skinning semantics.

Implemented owned bounded shader geometry with stable source IDs, retained serialized versus nondegenerate triangles, optional maps/coordinates/raw colors and typed TRTS binding records. Modern double exact-zero filtering is declared separately from original floating arithmetic. No copied decompiler pseudocode. Existing core IFF/source cache is reused. Bundle optional geometry creation requires owned skin-source loading before lookup; unsupported hierarchy/options reject.

## Original results and gates

The twelve-source329,296-byte original closure is unchanged. Body L0:1,099 shader vertices/1,750 triangles/two shaders/900 global DOT3 records. Head L0:691 vertices/1,060 triangles/three shaders/542 DOT3 records/one texture-renderer record. Total1,790 vertices/2,810 triangles/five shaders/zero degenerates. Head renderer path texturerenderer/hum_m_face.trt retains local shader0/MAIN; no generated texture is rendered.

Full native smoke passes owned input mutation, ITL/OITL source/occlusion distinction, positive degenerate filtering, UV/reference retention and all truncated prefixes; invalid triangle/index/UV/renderer target/capacity rejects. Original `--skeletal-geometry` passes. Independent geometry verifier (external historical reference) rereads effective original source priority/size/hash and reconstructs a canonical geometry stream containing global tangents, every shader path/map/UV/color record, serialized source IDs/occlusion/index records, retained source IDs and TRTS records. Both native hashes agree exactly. No material, animation or character rendering claim follows.

Reproduce with prior pinned object/SAT options plus `--skeletal-geometry`; verifier ROOT CONFIG NATIVE_OUTPUT `--configuration-sha256 CONFIG_PIN`. [Durable contract](../../docs/SKELETAL_MESH_GEOMETRY_CONTRACT.md). Canonical stream encoding is independently implemented by native fixture helper and verifier; detailed output remains ignored/private. Research scores and named-function counts unchanged.

Next: neutral original bind-pose preview with coordinate/winding/bounds inspection. Separate animation, weighted generation, customization, occlusion and BTRT face rendering remain named follow-ups.
