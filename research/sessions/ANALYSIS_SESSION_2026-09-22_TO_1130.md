# Stardust client analysis session — through 11:30 checkpoint

Date: 2026-09-22  
Canonical target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Mode: static executable analysis and configured-asset correlation only

## Outcome

This continuation completed eight coherent blocks. It did not implement the
replacement client, alter the executable/assets, publish anything to GitHub,
or issue a shutdown command.

Implementation-grade research remains conservatively **96%**. The comprehensive
whole-client estimate advances from **89% to 90%**: the gain comes from closing
appearance-local animation order, the Godot transform boundary and several
specialized but observable presentation paths, not from pretending that every
compiler helper or historical source name has been recovered.

The provisional-name registry contains **2,118 distinct executable addresses**.
The server-correlated packet inventory remains **345 documented / 0 reachable
gaps**.

## Completed blocks

| Block | Result | Confidence |
|---|---|---|
| Dormant BTRT graph | recursive TRUE/FALS/OR/AND/CVTP conditions, SST2/STF/STFA/NOP and CFBC/RTLI behavior mapped | Confirmed; CVTP historical intent unknown |
| Skeletal appearance alter order | cache invalidation -> ordered controllers -> physical substeps/root motion -> synchronous messages -> replacement -> readiness/LOD maintenance | Confirmed locally |
| Godot coordinate adapter | SWG position reflects Z; `[w,x,y,z]` becomes Godot `[-x,-y,z,w]`; pose fixtures pass | Confirmed mathematically and numerically |
| Nameplate frame cache | integer pairs are current/previous layout displacement and projected screen-Y; visibility query is separate | Confirmed/Strong labels |
| SUNT runtime | first/fourth scalars are yaw/pitch slew rates; middle limit pair is retained but dormant | Confirmed rates/non-use; Strong limit labels |
| STFP Boolean | factory identity discriminator keyed with customization name, not render state | Confirmed behavior; historic domain label unknown |
| CFBC fields | exact color/depth/stencil enable-value order through D3D9 Clear boundary | Confirmed |
| BTRT DEST formats | dimensions, file-to-runtime format map and ordered acceptable-format delivery into texture creation | Confirmed/Strong; enum labels unknown |

## Principal findings

### Animation events have a precise synchronous boundary

`SkeletalAppearance2::alter` invalidates its transform cache before advancing
controllers. Each physical-track substep advances the outgoing animation,
extracts and accumulates root motion, and synchronously fans out messages before
installing any returned or queued replacement. This ordering is a compatibility
contract for gameplay effects and sound triggered by animation messages.

### Godot can consume the original transform data through one adapter

The executable uses Y-up, +Z/K-forward SWG space and stores quaternions as
`[w,x,y,z]`. Godot's Y-up/-Z-forward space is reached by one Z reflection.
Geometry winding and tangent handedness must flip once at import. Network x/z/y
is only serialization order: decode it into logical SWG `(x,y,z)` before the
scene adapter. This removes a major risk from the original-asset requirement.

### BTRT is closed at its behavioral command boundary

All executable-supported conditions and prepare/render operations now have
bounded behavior. The STFP Boolean distinguishes otherwise same-named variable
factories and has no palette/shader effect. CFBC is ordinary optional color,
depth and stencil clear state. The remaining BTRT uncertainties are historical
labels, not unimplemented command mechanics.

### Specialized presentation findings remain separable

Nameplate temporal caches and sun tracking are observable presentation rules,
but neither contaminates authoritative object/network state. The modern client
can encapsulate or redesign their internals while retaining deterministic
inputs and effects. The original sun tracker notably stores `-10/+10` panel
limits but does not enforce them; enforcing them would be an opt-in correction.

## Remaining focus

- Continue long-tail optional UI/gameplay mediator and specialized presentation
  coverage using the uncertainty ledger, prioritizing observable asset behavior.
- Audit stale historical “unknown” text so resolved entries do not overstate
  remaining risk.
- Preserve the external public TCP gateway question separately from the fully
  mapped direct UDP/SOE developer-server path.
- Continue function-level work where it changes assets, protocol, lifecycle or
  user-visible behavior; do not inflate the denominator with template/compiler
  scaffolding that has no replacement-client consequence.

## New checkpoints

- `ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_DORMANT_PATHS.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_APPEARANCE_ALTER_ORDER.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_GODOT_COORDINATE_ADAPTER.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_NAMEPLATE_FRAME_CACHE.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_SUN_TRACKER_RUNTIME.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_STFP_FACTORY_FLAG.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_BTRT_CLEAR_FIELDS.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_BTRT_DEST_FORMATS.md`
