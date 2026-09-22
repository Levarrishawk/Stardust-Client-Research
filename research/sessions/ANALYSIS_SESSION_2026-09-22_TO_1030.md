# Stardust client analysis session — through 10:30 checkpoint

Date: 2026-09-22  
Canonical target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Mode: static analysis, launcher inspection, configured-asset inspection and current-server correlation only

## Outcome

This continuation completed six bounded integration/asset blocks. It did not
implement a replacement client, alter Stardust.exe or original assets, publish
to GitHub, or issue a shutdown command.

The implementation-grade estimate remains conservatively **96%**: these blocks
retired important risks inside already-known subsystems but did not justify
rounding the final four percent away. The comprehensive whole-client estimate
advances from **88% to 89%**, principally because the deployed transport choice,
skeletal detail composition and dynamic face-texture asset chain are now based
on direct evidence rather than subsystem labels.

The stable provisional-name registry now contains **2,070 distinct executable
addresses**. The authoritative server packet-source inventory remains **345
documented / 0 reachable gaps**.

## Completed blocks

| Block | Principal result | Confidence |
|---|---|---|
| Deployed transport selector | installed launcher starts Stardust.exe without arguments; executable defaults to framed TCP while checked-in server endpoints are UDP/SOE, exposing a missing deployment-edge component | Confirmed local facts; external edge unknown |
| Launcher GPU archive gate | priority 54 switches between a 208-record NVIDIA archive and a byte-identical 26-record AMD subset; 179 Mustafar resources disappear without fallback under AMD | Confirmed |
| Skeletal LOD composition | one logical SAT detail index drives all mesh generators; MLOD/basic SKMG share the fetch interface and adjacent availability fallback | Confirmed |
| MGN TRTS bindings | sixteen head meshes bind named face texture-renderers to a per-LOD primitive index and `MAIN` texture tag | Confirmed grammar/corpus; pair labels Strong |
| Blueprint texture-renderer envelope | all eight effective TRT assets are referenced BTRT/0001 graphs; factory, versions and ordered resource/command sections are bounded | Confirmed envelope; command labels open |
| Blueprint texture-renderer commands | live face graphs reduce to unconditional SSTC/SST1/STFP setup and SRSC/RTFC shader-selected triangle fans | Confirmed dispatch/RTTI/corpus; operand labels open |

## Principal findings

### The public package and developer server expose different transport edges

`shared_network_config_install_VA_012BBA30` defaults `useTcp=true`, and
`game_connection_base_ctor_VA_012B6150` selects TCP when that flag is nonzero.
Managed-launcher IL shows `ProcessStartInfo("Stardust.exe")` with no Arguments.
Current login and zone server source instantiate datagram services. Therefore a
replacement should implement two explicit profiles: direct UDP/SOE for the
provided server source, and framed TCP only when the absent public gateway or
proxy boundary is identified. The TCP launch fact must not be misread as proof
that Core3 itself accepts TCP.

### GPU selection changes the effective namespace, not payload variants

The launcher atomically rewrites one search-tree entry. The AMD archive's 26
records are byte-identical to records in the NVIDIA archive; it adds no AMD-only
payload. Selecting it removes 179 names, mainly Mustafar ribbon/affector assets,
and only three names fall back to lower archives. Effective manifests and asset
tests must therefore include launch-time GPU selection.

### Skeletal LOD is a coordinated appearance-level decision

The appearance computes one selected detail with hysteresis and caps, asks each
SAT mesh-generator template for that level, creates the resulting mesh and
aggregates all components. Missing generated/dependent content triggers an
adjacent-index search. A new importer must not select independent body/head LODs
or assume MLOD embeds distance thresholds.

### Original head rendering requires dynamic texture composition

The sixteen rare TRTS-bearing MGN files are head meshes. Their exact optional
grammar names a `.trt` resource and supplies one or more pairs that the current
corpus strongly identifies as `{primitiveIndex, textureTag}`. Every live pair
targets `MAIN`, while primitive indices vary between assets and sometimes LODs.

All eight referenced `.trt` files are `BTRT/0001`. The executable parses a
destination definition, shader and texture tables, vertex/index data, camera,
render commands and prepare commands. This makes facial appearance an
asset-authored dynamic render product, not a static skin texture. The modern
client can translate this to renderer-neutral IR, but cannot omit it and still
claim original-asset character compatibility. The live executor subset is now
bounded further: 67 unconditional prepare lists use only constant texture,
one-dimensional texture and palette-derived factor operations; 37 render sets
use triangle fans. Conditional branches and the other supported command types
are absent from all eight effective templates. Runtime vtable correlation also
confirms the operand roles: constant texture selection, customization-indexed
1D texture selection, palette-color factor selection, per-pass shader
iteration and vertex-buffer-indexed triangle-fan drawing.

Destination resolution is also corpus-bounded: 128x256, 256x256 and 512x512
are all live, so a replacement must honor authored dimensions rather than
hard-code one face-texture size. The five DEST attribute IDs and executable
remap are recorded even though their historical friendly channel names remain
unknown.

## Remaining focused work

- Decode only the dormant BTRT operations/conditional forms and the remaining
  destination/STFP policy fields; the complete live face path is now closed.
- Continue long-tail appearance/animation runtime and failure/fallback paths.
- Retire remaining presentation semantics that materially affect original
  assets, while leaving non-behavioral compiler helpers out of the denominator.
- Preserve the missing public TCP deployment edge as an external-state question
  and do not invent its handshake.

## New checkpoint files

- `ANALYSIS_CHECKPOINT_2026-09-22_DEPLOYED_TRANSPORT_SELECTOR.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_GPU_TRE_OVERLAY_SELECTION.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_LOD_COMPOSITION.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_MGN_TEXTURE_RENDERER_BINDINGS.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_ENVELOPE.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_COMMANDS.md`
