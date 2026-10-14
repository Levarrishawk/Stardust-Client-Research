# Modern directed route inspection

`DirectedPathGraphQuery` owns node positions and directed edges, builds ordinal adjacency and assigns Euclidean distance in serialized position space to each edge. Lane widths, node types and radius do not affect this inspection cost. This is an explicit modern policy; original client route cost and agent-clearance behavior remain unverified.

Dijkstra expands by (distance, node ordinal), with authored edge order, and preserves first discovery for exactly equal costs. Settled nodes and strict improvements prevent predecessor cycles on zero-length edges. Routes return owned node/edge ordinals and double total distance. Input availability is snapshotted and excludes nodes; it is not replicated cell permission. Invalid endpoints/availability width are rejected; unreachable/unavailable endpoints return null; a reachable self route has one node/zero edges. Nearest-node inspection has no original range cutoff and resolves equal distances by ordinal. Finite coordinates and one million nodes/edges bound input and arithmetic.

[Original independent agreement](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DIRECTED_ROUTE_INSPECTOR.md): 25,847 routes across 3,909 nonempty graphs match Bellman–Ford reachability/cost and explicit returned edge-chain validity. Five empty graphs have no route endpoints. This sampling does not prove all paths or original route equivalence.

## Cell graph ownership and prototype host

`PortalSceneBundleReader.Load(..., loadPathGraphs:true)` decodes the already loaded floor graph metadata and layout graph into owned models, sharing repeated floor resources and keeping parse omissions explicit. It implies floor loading under the same source budget.

`--interior-paths` adds reflected graph edges, fixed-size inspection glyphs and route endpoint controls to the named-cell inspector. All-cells selection shows the layout graph; an individual cell shows its floor graph. Glyph size is presentation-only and does not replace serialized radius zero. Line/instance/route uploads are checked against reflected source positions. Inspection limits are 4,096 nodes and 32,768 edges. Route mesh/material are reused across changes and released on teardown. No floor movement, cross-cell linking or dynamic permissions are implemented here.


Headless prototype host uses a dummy MultiMesh renderer, whose glyph transform readback is unavailable. The gate reports `unavailable_headless` and still checks CPU edge/route arrays. Actual GPU runs verify glyph instance transforms. Cross-cell associations follow the [separate contract](PORTAL_PATH_ASSOCIATION_CONTRACT.md).


Same-cell floor picks now feed the route-start inspector through an owned spatial query. All 149 original scene probes and seven reflected host-control connections pass; this does not establish legal movement. See the [surface connection](FLOOR_SURFACE_QUERY_CONTRACT.md#same-cell-surface-to-route-selection).
