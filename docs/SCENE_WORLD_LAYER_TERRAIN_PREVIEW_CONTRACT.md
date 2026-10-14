# Supplied layer heights in the original-actor preview

Round41 adds an explicit synthetic version2 terrain fixture to the existing
scene-pumped preview. It uses the [supplied executor](TERRAIN_SUPPLIED_LAYER_HEIGHT_CONTRACT.md),
not a recovered complete terrain generator. Original actor loading remains the
object/appearance/mesh route used by the accepted preview.

## Boundary and ownership

Four fixed 8-meter requests produce 100 padded samples each. Owned circle and
rectangle inputs produce split-phase queries; explicit supplied filter values
and incoming weights feed the local layer combiner. Three ordered AHCN rules
operate on each supplied initial height. All phase policies are explicit.
Finite results are assembled into16 immutable patch queries and16 owned Godot
visuals. Normals and colors are supplied debug channels. No original generated
normals, materials, complete recursive traversal or active precision is claimed.

Version1 supplied-slope fixtures remain accepted. Version2 requires exactly nine
bounded pose inspection expectations,400 initial heights, three typed affectors
and four ordered requests. Expected geometry is never read by the native host.
An independent Python peer reconstructs all400 height/local/product words,
operation visits,16 patch geometries/indices and nine actor grounding results.
Pose expectation values are test assertions, not movement inputs.

The owned terrain driver preserves actor resource publication, animation and
one engine Z reflection. Correction while paused retains the raw correction;
subsequent movement grounds it. Shutdown clears queries, visuals and expectations.
Failure in the fourth chunk disposes all12 already allocated visuals before
the outer host failure cleanup. Cleanup is idempotent.

## Verification and limits

Main headless and GPU runs pass nine grounded states,16 patches/128 triangles,
exact16 visual disposal, full CREO6/TANO6 state/event checks and14 terminal clock
states/all8 selected mip identities. A fresh GPU capture was visually inspected.
Late malformed initial-height data rejects and drains12 partial visuals.
The prior version1 headless mode is replayed separately.

The independent [host peer](../../SWG-Godot-Client/poc/round41-tests/Integration/layer_host_peer.py)
regenerates its fixture before verification. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R41_SUPPLIED_HEIGHT_AND_CREO4_FIELDS.md)
records failures and acceptance. No original executable or live server runs.

## Round43 integration

Round43 retains this exact supplied three-rule terrain in fresh combined private-state headless/GPU checks. The separate [FHGT two-layer join](TERRAIN_SUPPLIED_HEIGHT_FILTERED_SEQUENCE_CONTRACT.md) is native-only; no GPU/full original terrain extension is claimed.
