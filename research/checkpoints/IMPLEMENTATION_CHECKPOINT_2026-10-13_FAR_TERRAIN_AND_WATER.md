# October 13, 2026 — stitched far terrain and visible local water

## Bounded question and stopping condition

Can the existing 320m,5m-sampled terrain window display an original-data 640m
outer landscape without overlap/cracks, while preserving fine-only grounding
and transactional streaming of terrain and authored ponds? Stop at exact edge,
area, material and ownership verification plus frozen x64/DX12 acceptance.
This is a modern visual policy, not recovered original quadtree/LOD behavior.

## Inputs and tools

- Original `terrain/tatooine.trn` SHA-256
  `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
- TGEN SHA-256
  `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
- Existing [height/material slice](../../../SWG-Godot-Client/docs/TATOOINE_REGION_DEVELOPMENT.md),
  [water input and consumer evidence](../../docs/TERRAIN_LOCAL_WATER_CONTRACT.md)
  and [residency contract](../../docs/TERRAIN_REGION_RESIDENCY_CONTRACT.md).
- .NET SDK8.0.300; Godot4.7.2.stable.mono.official.ed1daf0bf; actual Windows
  AMD64/D3D12API12_0 Forward+ on NVIDIA RTX4070SUPER.
- Original installed assets remain private. No original executable execution,
  server connection, commit, push or publication occurred.

## Observations, interpretation and consequences

**Confirmed implementation behavior:** sixteen 160m coarse chunks supply 20m
samples around sixty-four 40m fine chunks with 5m samples. Of 256 coarse 40m patches,
64 wholly inside the fine hole are omitted. The remaining 192 fans contain 1920
vertices and 1728 triangles, covering exactly 307200m². All 256 distinct 5m hole
subedges match;292 duplicated boundary vertex occurrences copy actual fine
positions, normals, ARGB and family keys. The derived fine UV frame is increasing
X and decreasingZ with 10m period. Copying equal integer UV endpoints alone would
have missed reversed texture phase between them.

**Confirmed risk and correction:** the earlier coarse probe found 289 exact
shared heights but different coarse/fine normals and up to 7.5395m interpolation
gaps at intermediate fine samples. The implementation therefore stitches actual
fine samples; it does not overlay two surfaces or conceal cracks with skirts.
The [Core helper](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainModernFarTerrainView.cs)
starts no worker and does not alter the fine grounding surface.

**Confirmed scheduler behavior:** the host builds on the existing single CPU
worker and reuses the exact unchanged coarse request. Current/candidate ownership
is at most 64 fine plus 16 coarse receipts each, total 160; existing fine-only
telemetry remains 128. GPU bounds remain 2048 fine patch visuals plus two far
visuals and eight local-water visuals. Stale coarse cache ownership is dropped
before replacement and cleared after worker drain. Hidden far/water candidates
are prepared before publication and disposed on preparation failure. Far visuals
borrow the same five-family material as fine patches and are disposed before it.

**Confirmed defects repaired:** an uncached streaming run failed the unchanged
three-successor gate; caching coarse input repaired that run. A request that left
and returned to an aborting candidate could remain idle after cancellation; the
host now retries only an idle canceled desired key differing from Current.
Independent accounting now includes exact created/disposed counts for each
unpublished generation instead of assuming every created mesh was published.

A separate integration defect clipped the authored pond to fine minimumZ2680
while its authored minimumZ2675 still lay inside visible far terrain. Fraction
area comparison found19.4444m² missing. Water now clips to the published far
source bounds when enabled, with explicit `PublishedFarOuterBounds` metadata.
The independent verifier derives those bounds from signed request coordinates
using mathematical floor, not copied output bounds. Fine-only water retains
`PublishedFineBounds` and its previous behavior.

## Verification

- Core far gate:22199 synthetic assertions;44379 with original negative pond
  inputs. Independent rational coverage/edge verification and original label
  replay pass. Ten verifier tests include original-input mutations.
- Actual synthetic GPU gate:902 checks/four rejected inputs,17 PNG-bound blend,
  asymmetric UV and backface probes; seven tamper mutations rejected. Full mesh
  attribute readback and shared material ownership are verified.
- Four original geographic CPU probes pass: Mos (98,58), Tusken (-103,152),
  pond (-135,65), baked road (-140,-162). All final triangle/normal dot guards
  remain positive. This is selected-site evidence, not full-planet coverage.
- Frozen corrected package passes 20 acceptance checks without package mutation.
  Main proof: 3412 independently checked positions,101.4278m travel, three
  successors, maximum grounding error 4.764e-7m. Independent far replay checks
  four geometry windows and46080 original source family assignments. Fine replay
  checks 36864 vertex assignments and 19964 shared positions.
- Corrected water replay checks four windows,16 vertices/eight triangles,
  original shader/MAIN identities and zero ULP differences. The pond remains
  complete across the moving fine-window boundary.
- Main cleanup balances 1626 fine, four water and four far visuals, and both
  shared material bindings. This includes 106 created/disposed visuals from a
  canceled generation. Reset passes after spawn eviction; its 3093 fine and
  nine each water/far visuals are all disposed. Cancellation, staged cancellation,
  returned-target retry, water rollback and far rollback pass independently.
- Full 32-suite regression run passes (26.NET+6Python). The four newly registered
  Python suites separately pass 43 synthetic methods; the subsequent focused
  water correction passes 19 methods (13 synthetic/six original). Current default
  inventory is36 suites, with45 synthetic methods in the four added suites and
  14 optional original methods. No monolithic36-suite replay is claimed.
- Godot builds with zero warnings/errors. The original console export wrapper
  stalled after pack creation; its failed output is retained. Re-export through
  the engine executable completed and is the accepted package.

## Reproduction and remaining uncertainty

Use the [builder](../../../SWG-Godot-Client/tools/python/build_offline_dx12_client.py) with
`--stream-region -135 65 --window-chunks 8 --modern-blend --local-water --far-view`
and explicit local toolchain, package cache, original installation, configuration
and fresh private output arguments. Run the
[export verifier](../../../SWG-Godot-Client/tools/python/verify_offline_terrain_client.py) with
`--build BUILD --output FRESH_OUTPUT --original-terrain ORIGINAL_TRN --gpu`.
Native and independent fixture commands are in the
[far contract](../../docs/TERRAIN_MODERN_FAR_RING_CONTRACT.md),
[GPU gate](../../../SWG-Godot-Client/poc/terrain-far-gpu-tests/README.md) and
[water contract](../../docs/TERRAIN_LOCAL_WATER_CONTRACT.md).

Post-publication wall intervals were31.67..62.05ms in the accepted proof;
startup/capture reached666.16ms. Proof serialization and readback contribute,
but these observations do not establish production frame pacing or physical
input acceptance. Original numeric/LOD/material/water parity, full planetary
coverage, flora, broader scene placement, swimming and live authority remain open.

The bounded slope-material readiness audit remains a no-go: original cached
FSIN result bits and ordered query-time shader integration are not yet
closed; layer89 also requires ordered FHGT, and families1/4 exceed the current
fixed five-family binding. The subsequent [endpoint probe](ANALYSIS_CHECKPOINT_2026-10-13_SLOPE_ENDPOINT_GRAPH.md)
closed the conversion/clamp/sine/store graph without claiming numerical parity.
The subsequent [producer probe](ANALYSIS_CHECKPOINT_2026-10-13_SLOPE_NORMAL_PRODUCER.md)
closes the normal grid and dirty timing. A modern cache primitive and ordered
shader adapter are the remaining implementation prerequisites.
The subsequent [modern cache primitive](../../docs/TERRAIN_MODERN_SLOPE_NORMAL_GRID_CONTRACT.md)
passes 2440 checks/39 rejections in Debug/Release and parent replay, including
independent topology and rounding witnesses. It remains outside the accepted
export and traversal. The ordered shader adapter is now the next integration.
NGE remains corroboration; final render normals must not be substituted for
the query-time normal cache. No research/function/protocol metrics changed.
