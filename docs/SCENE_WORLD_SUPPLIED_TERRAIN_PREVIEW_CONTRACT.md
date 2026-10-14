# Original actor preview on supplied terrain

## Explicit boundary

The optional `--terrain-peer` / launcher `-TerrainPeer` path joins original
human/terminal resources with **synthetic supplied terrain channels**. It is
an offline compatibility prototype. Complete original terrain generation,
legacy floor/collision behavior, normal/material generation and live gameplay
are not established by this preview.

The fixture producer `poc/round39-tests/Integration/generate_terrain_peer.py`
independently defines four padded width8/tile2/margin3 chunks under four
explicit Binary24 policies. Each supplies 100 height/normal/resolved ARGB
samples. Height is exactly native Z divided by four, normals are supplied up
vectors and the active display domain is `[-8,8]` in X and Z. The host does not
read the fixture's Expected section when constructing patches.

## Ownership and engine boundary

`ScenePumpedTerrain.cs` bounds fixture bytes, depth, version, synthetic marker,
policies, request order, channels and seam/UV parameters before construction.
Accepted coordinate/patch APIs create sixteen immutable patches and query
objects. Each debug prototype host adapter owns three resources, verifies mesh readback,
and reflects native Z once. All adapters and query references are released on
successful finish, failure and tree exit. This debug shader is inspection
presentation, not a recovered terrain material.

Local input uses the engine-neutral supplied-terrain driver. Native X/Z,
integer clock, speed and yaw preserve the existing local motion policy; local
tick candidates acquire surface Y. Authoritative correction remains its raw
pose until a local tick grounds it. The original creature's height scale is
still composed once and is independent of ground Y. Original actor resource
identity and archive-read counts must remain unchanged during movement.

## Acceptance requirements

The independent host oracle `poc/round39-tests/Integration/verify_terrain_host.py`
checks sixteen patch bit patterns/topologies, nine motion/correction/pause/
focus/reset states against the analytic slope and integer schedule, engine
reflection, retained original actor resources, and exact sixteen-allocation/
sixteen-disposal cleanup. The complete CREO6/TANO6/animation/name/customization
host oracle must also pass in headless and GPU modes. A private GPU capture
must be inspected. Leaf acceptance and these executed joins are required
before reporting this feature accepted; implementation preparation alone
does not establish them.

See [terrain motion](SCENE_WORLD_TERRAIN_MOTION_CONTRACT.md),
[padded patch input](TERRAIN_COORDINATE_HEIGHT_PATCH_CONTRACT.md) and the
[Round39 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_39_2026-10-03.md)
for evidence and final execution results.
