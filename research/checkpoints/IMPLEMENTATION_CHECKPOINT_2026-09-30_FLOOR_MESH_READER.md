# Owned floor mesh reader — 2026-09-30

## Question

Can the replacement preserve every deployed FLOR geometry/adjacency field and optional metadata, with independent all-corpus agreement and malformed-input gates? Stopping condition met.

## Identity and tools

Static executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` verified before this boundary's disassembly. Config SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 configured TREE0005 archives. Python Index/raw IFF inspection, static Capstone, native .NET smoke and one-process pinned manifest comparison. Member-list SHA-256 `10D0354A1C7125533A7ED9804C9CD6C38D6AC0B2C201408F808F58270F656604`.

## Observation and interpretation

Direct version loader 012751E0 reads VERT/TRIS counts, calls 012A2850 for the 60-byte disk triangle and 012A2570 for its leading fields. Detailed field/optional reads and implementation requirements are in the [canonical contract](../../docs/FLOOR_MESH_DECODER_CONTRACT.md). Server `FloorMeshTriangleNode::readObject` agrees on field order; semantic edge labels remain correlation. Original post-load normals are flipped; authored storage is preserved instead of silently rewriting them.

All 4,730 live effective files agree independently: version 0003 ×472, 0004 ×177, 0005 ×366, 0006 ×3,715. Forty-six masks are reported separately. Totals: **414,301 vertices, 604,429 triangles, 189,927 boundary records**, zero duplicate-ID diagnostics. Native-owned fields reconstructed into canonical byte hashes match independent raw bytes, including normals, adjacency and raw flags; opaque optional payload hashes also match. Edge flag counts: 0 ×195,237; 1 ×1,616,990; 2 ×1,060. Non-solid bytes: zero ×570,284; one ×34,145. Portal IDs range -1 to 14; boundary edge integers range 0–2. These are Confirmed corpus observations, not full runtime equivalence.

## Validation and next probe

Native versions/ownership/exact fields and every truncated fixture prefix pass. Invalid vertex/neighbor indices, count mismatch, nonfinite fields, unsupported version and trailing bytes are rejected. Optional omissions are accepted. Independently authored structs retain original data outside engine adapters. Proprietary details and manifest output remain ignored private data.

Next: deterministic engine-neutral acceleration and surface query, verified independently against an exhaustive reference. Then attach floor resources to the named-cell inspector. Navigation, portal transitions and live permission remain separate.
