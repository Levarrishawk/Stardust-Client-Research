# Owned deployed path graph contract

`PathGraphDecoder` consumes owned PGRF/0001 with exactly META, PNOD, PEDG, ECNT and ESTR. Original other versions remain deferred; all currently effective floor/layout graphs are 0001. Bytes are little endian within IFF.

## Fields

META is one I32 raw graph type. PNOD starts U32 count and repeated 32-byte records: I32 authored ID, I32 field04, I32 global key, I32 raw node type, three F32 serialized position components and F32 radius. Positions retain original vector order; no server axis-name rearrangement or radius-zero substitution is applied. PEDG starts U32 count and 16-byte records: I32 from/to node ordinals and two F32 lane-width fields. Exact left/right labels are server correlation; raw order remains authoritative.

ECNT/ESTR each start U32 count and I32 values, with one entry per node. Positive counts require bounded nonnegative starts; zero counts permit -1 or a bounded nonnegative start. Every referenced edge must originate at that node ordinal, each edge is covered exactly once, and destinations are bounded. IDs are retained separately from ordinals even though all current IDs equal their ordinals. Duplicate/global/type keys are not implicitly unique. Unknown raw types are retained. Finite nonnegative radius/width is the replacement's strict bounded policy.

## Direct executable chain

SimplePathGraph version dispatch and deployed loader 012E2120 reads META then delegates counted arrays to 012E2730/012E2880/012E29F0. Node callback 012E3B70 reads four 4-byte fields, vector via A95810 and trailing float. Edge callback 012E39A0 reads four four-byte fields. The adjacent 012E21D0 path is a writer, not the reader. Client loader/factory lifecycle remains separate from this field decoder. Server correlation: `templates/appearance/PathGraph.cpp::readObject`, `PathNode.h::readObject`, `PathEdge.h::readObject`; server zero-radius-to-0.5 behavior is not adopted as client evidence.

## Agreement and limits

[Original checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PATH_GRAPH_READER.md) verifies all 3,914 graphs, 39,647 nodes and 86,964 edges. All have graph type 3; raw node types 0/1/3/4/5 occur. 1,471 empty ranges use start -1. Native normalized owned-field hashes match independently inspected bytes; exact widths/positions/ranges are included. One million records per family and existing IFF budgets bound allocation. Tests cover ownership, exact directed edges, nonsequential authored IDs, empty ranges and malformed count/width/index/float/table inputs.

This decoder does not establish traversability, portal-to-floor ownership, dynamic permission, original route cost or FloorLocator path-walk equivalence. Any modern route inspection must name its own cost/policy explicitly.


Factory registration 012E1060 → setter 0128DF20 installs 012E10A0, retrieved by 0128DF30. PGRF construction invokes reader 012E1C40, which dispatches 0001 to 012E2120; 0000 is supported in the client but absent from current effective graphs. Modern routes follow the [separate contract](DIRECTED_PATH_ROUTE_CONTRACT.md).
