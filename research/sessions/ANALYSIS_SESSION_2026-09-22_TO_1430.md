# Analysis session report — 2026-09-22 to 14:30

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

This session resumed at 13:16 after relocating the organized research workspace. It performed static executable, configured-TRE corpus and server-source correlation only. No server/client product code was changed, and no commit or push was performed.

## Completed blocks

| Block | Material result | Confidence |
|---|---|---|
| Interior layouts | Exact INLY/0000 node grammar, client manager policy, cell binding, local object creation and complete effective corpus | Confirmed |
| Component appearances | CMPA 0001-0005 dispatch, transformed part grammar, owned runtime hierarchy, effective dependency graph and failure policy | Confirmed except RADR label |
| Sprite/quad appearances | Shared SPRT/QUAD v0000 schema, bounds/primitive creation and complete effective corpus | Confirmed; outer-tag policy label unknown |
| Beam appearance | BEAM/0000 schema, shader-instance construction, four-vertex runtime primitive and absence from effective overlay | Confirmed except field labels; no corpus sample |
| Scene-worker retirement | Reconciled join-vs-discard behavior by ownership: scene terrain joins; global async assets cancel publication; file reads finish | Confirmed architectural policy |
| Documentation reconciliation | Removed stale terrain-mask, PTEX Boolean, mesh-tombstone, SPS and PASS-open wording | Confirmed against later checkpoints |

## Interior-layout findings

The earlier 411 `.ilf` count represents archive copies. Overlay-aware parsing finds 297 effective live `INLY/0000` layouts, one tombstone and 87,937 nodes with zero errors. A node contains object-template path, portal-cell name and twelve little-endian F32 transform values inside the big-endian IFF envelope.

`client_interior_layout_manager_apply_building_layout_VA_0059B7B0` resolves the cell, creates the local object, applies its cell-relative transform and inserts it into the world; failure is node-local. The frame-scanning lazy path and the separate `useInteriorLayoutFiles`/`disableLazyInteriorLayoutCreation` controls are distinguished. ILF furnishings are presentation objects and do not replace server-spawned gameplay entities.

## Component-appearance findings

The executable accepts CMPA 0001-0005; the effective corpus contains 165 winners (13 v0003, six v0004, 146 v0005), 779 parts and no parse errors. Each common PART is an appearance-relative child path plus a 3x4 transform. Runtime code owns child object/appearance instances beneath one visibility parent and composes per-part transforms while forwarding lifecycle and query operations.

References comprise 640 LOD, 87 particle, 26 mesh, 16 sprite and ten nested CMPA parts. Namespace-normalized graph checking finds no nested cycles. Eight legacy laser-bolt/bluebolt mesh children do not resolve, while sibling sprite children remain live; unresolved parts must therefore be skipped and logged rather than invalidating the whole CMPA. V0005 `RADR/INFO` remains a bounded U32 policy whose historical semantic label is unknown.

## Sprite/quad findings

`SPRT` and `QUAD` share a factory and v0000 layout: Vector3 center/offset, F32 size/radius, Vector4 RGBA and shader path. The configured overlay has 469 effective winners (369 QUAD, 100 SPRT), 385 shaders and zero parse failures. All deployed RGBA values are white, but centers and sizes vary. Runtime code applies the legacy `sqrt(2)` size conversion, creates the shader-backed quad and explicit bounds, and installs normal visibility ownership.

## Replacement implications

- Model ILF as building-scoped, client-local furnishing instances keyed by portal-cell name.
- Model CMPA as immutable decoded child-template/transforms plus mutable per-instance child nodes.
- Use cycle-safe resource loading and tolerate a failed component child locally.
- Implement SPRT/QUAD with engine-native billboard/quad primitives while preserving authored bounds, shader identity and tag-driven texture lookup.
- Keep IFF container endian rules separate from little-endian numeric payload decoding.
- Retain dormant BEAM compatibility with two dimensions, opaque Vector4 shader parameter and engine-native four-vertex geometry.
- Join scene-owned workers, but keep process-global asset/VFS workers alive across zone changes and suppress stale publication through cancellable owner/generation handles.

## Beam findings

`0x00798240` registers BEAM and `0x00798460` accepts only version 0000. The decoder reads two F32 dimensions, a Vector4 and a shader-template path. Runtime construction at `0x00792EC0 -> 0x007928D0` creates a shader-backed four-vertex primitive, regenerates its geometry and derives its bound from the two dimensions. The Vector4 is supplied to shader-instance creation, but its historical semantic label is not established. No effective configured archive asset uses BEAM, so this closes the executable contract without claiming deployed value-range validation.

## Scene-worker retirement findings

The old “join or discard” question is ownership-specific. The scene-owned procedural-terrain worker is stopped, signalled and joined before its queues and terrain owner are destroyed. The process-global asynchronous asset loader remains alive across a scene change; cancellation zeroes an exact callback/context identity, main-thread publication is suppressed, and produced resources are still cleaned. File-streamer requests have no scene callback publication path and finish synchronously for their owners. The replacement should preserve these scopes and add generation-safe handles rather than rebuilding global asset services at every zone transition.

## Progress and registries

- Implementation-grade replacement research: **96%**.
- Comprehensive whole-client research: **90%**.
- Packet inventory: **345 documented / 0 reachable gaps**.
- Stable provisional-name registry: **2,171 distinct addresses / 0 duplicate rows**.

The rounded percentages remain unchanged. Four asset/runtime families moved from tentative/broad coverage to implementation-grade specifications, but this does not justify inflating the whole-client denominator.

## Recommended resume point

Continue the rare effective asset/runtime sequence with one of the remaining registered appearance families that lacks a complete corpus-plus-runtime contract, alternating with a lifecycle/ownership or UI/gameplay breadth block. Do not reopen ILF, CMPA, SPRT/QUAD, PTEX, terrain vertex payload or SPS topology without contradictory evidence.
