# Bounded Tatooine local-water geometry admission

Date:2026-10-13. Question: can the replacement display an actual authored pond
without guessing Pre-CU fields or inventing a global Tatooine water plane?
Stopping gate: pinned four-pond admission, bounded modern clipping/geometry,
independent fixtures and an actual first-pond ground-height query. Host shader
translation and visual acceptance are separate work.

## Inputs and tools

Original TRN SHA256
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`,
TGEN SHA256
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`;
target executable SHA256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
checked before VA inspection. Existing BPOL/PTAT codecs and independent
Round36/37 IFF/TRE readers were reused. Static observation used the existing
local PE/capstone runtime and the earlier retained0084AE30 decompilation as
a navigation lead. No original executable, network or server was executed.
NGE checkout HEAD949451032647e45e42c3aaef3f41b132c8af36e3 was checked and
its BoundaryPolygon loader/buildLocalWaterTable used only as corroboration.

## Observation and interpretation

- **Confirmed:** PTAT global-water flag0, height0, shader size2, empty shader;
  a global plane would be invented for this original occurrence.
- **Confirmed:** four BPOL0005/IHDR0001 records at TGEN42573/42730/42887/43044,
  enabled boundary/ancestor chain0→53→56; all extension flags1, sizes2,
  nameswter_dsrt_bright, plane heights0/1/0/0. Each has four convex points.
- **Confirmed target chain:**0084AE30 checks enabled/type2/flag00B73C60(+38),
  copies points+20/count+24;0084AF95/98/A1 takes+3C/+40/+44 into
 008637D0 through0084AFFD.00863922/29/31/34/37 maps point pairs and passed
  height into XYZ;008639CA calls primitive00862CA0. Thus the previously
  neutral BPOL extension storage has direct local-water consumer evidence.
- **Confirmed assets:** shader/wter_dsrt_bright.sht, priority38,672 bytes,
  SSHT0001/effect water with named MAIN texture/wter_aqua.dds; priority37,
  256×256 DXT5. Exact hashes and complete relevant bindings are in the
  [durable water contract](../../docs/TERRAIN_LOCAL_WATER_CONTRACT.md).
- **Modern implementation choices:** up-facing convex fans, world-XZ/size UVs,
  rectangle clipping and a supplied MAIN texture policy. These deliberately
  make no original tessellation, ALP, reflection, normal/time shader or swimming
  claim. The neutral existing codec remains unchanged.

## Implementation and gate

[Semantic collection](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/OriginalTatooineLocalWater.cs)
pins source identity, version/order/activation and retains immutable occurrence
provenance. [Geometry](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainModernLocalWaterPolygon.cs)
preserves authored input/height/bounds, clones exports and clips to the current
ground window with at most eight vertices/six triangles per pond. Outside or
tangent intersections are empty; unsupported/malformed profiles reject.

Native synthetic270 assertions and optional original311 pass. The original
receipt records four full meshes and24 clips. A clean isolated artifact build
passes after the initial shared-output invocation failed a newly added input-cap
test; no success claim is based on that failed invocation. The correct documented
reproduction restores with explicit NuGet.Config then runs without restore.

Actual generated ground at first-pond centroid(-5242.25,2765.5) is
−0.515142560005188, below its authored0m surface. The320m ground request is
(-135,65,8,8), retaining5m terrain samples. This closes the necessary visible
water prerequisite at one sample; submerged area, texture pixels and camera
presentation remain separate evidence.

## Remaining work and exact next gate

The [independent IFF/Fraction verifier](../../../SWG-Godot-Client/tools/python/verify_terrain_water_render.py)
passes the original receipt's24 clips with zero ULP differences; its16 mutation
tests pass. Source identities, all points/heights, upward indices, anchored UVs
and clipping are compared separately from production decoding. Workspace
validation passes1948 Markdown files with zero broken/private links; diff checks
pass. The adapter must positively decode
the original MAIN texture, draw only meshes clipped to the committed ground
window, demonstrate the actual first pond on DX12, preserve grounded movement
and replace/dispose bounded water visuals atomically. Full original reflective
shader execution, general polygons/river tessellation, global water, physics,
underwater transitions and swimming remain outside this completed core slice.
