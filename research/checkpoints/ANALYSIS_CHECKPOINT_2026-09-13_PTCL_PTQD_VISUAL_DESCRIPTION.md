# PTCL/PTQD particle visual-description checkpoint

Date: 2026-09-13

Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

Scope: common particle description (`PTCL`), quad specialization (`PTQD`), mesh boundary (`PTMH`), animated texture descriptor (`PTEX`), and runtime consumers. Disassembly is reconstructed evidence, not original source.

## Outcome

The nested particle visual layer now has a renderer-neutral compatibility model. `PTCL` supplies color, alpha, velocity modulation, optional three-axis rotation, resource/policy data and attachments. `PTQD` adds rotation, two dimensions, an optional dimension lock and a sprite-sheet texture descriptor. Runtime evaluates curves against normalized particle age; emitter simulation owns motion and lifetime, while rendering consumes sampled state.

## Factory and version dispatch

- `0x007BE8C0`: closed factory; 0 creates Quad, 1 creates Mesh, other values return null.
- `0x007B7B70`: PTQD loader. Modern files load common PTCL then detail 0000 (`0x007B8140`) or 0001 (`0x007B81A0`); legacy direct envelopes use `0x007B7DC0`/`0x007B7FD0`.
- `0x007BF140`: common PTCL versions 0000-0003 via `0x007BF2F0`, `0x007BF4E0`, `0x007BF710`, `0x007BFA10`; alpha is clamped to `[0,1]` after load.
- `0x007C2080`: PTMH, common PTCL, then mesh detail 0000 through `0x007C2200`.
- `0x007B7220`: PTEX; only 0000 is accepted. `0x007B74E0` writes it.

## PTCL evolution and corpus

0000 contains the resource/policy prefix, color, alpha and velocity data. 0001 adds a second prefix string with unresolved editor meaning. 0002 adds repeated 0x24-byte attachment descriptions. 0003 adds three rotation waveforms and marks their presence.

Corrected context-aware traversal finds 1 PTCL/0000, 1,803 PTCL/0002 and 4,569 PTCL/0003 occurrences across 2,114 live `.prt` winners (9 tombstones, zero expansion/framing errors). No PTCL/0001 occurrence was observed. It also finds 8 PTQD/0000, 5,965 PTQD/0001 and 399 PTMH/0000 specializations. These are nested descriptions, so totals exceed the top-level asset count. The initial lower figures assumed an external even-byte pad after odd-sized items; these SWG IFF assets do not use that traversal rule.

## Runtime channels

| Field | Meaning | Confidence |
|---|---|---|
| PTCL `+0x04` | RGB color ramp | Confirmed |
| PTCL `+0x1C` | alpha curve, clamped 0..1 | Confirmed |
| PTCL `+0x34` | velocity multiplier | Confirmed |
| PTCL `+0x4C/+0x64/+0x7C` | three authored rotation axes | Strong |
| PTCL `+0x94` | three-axis data present | Strong |
| PTCL `+0xA8` | attachment-description vector | Confirmed |
| PTQD `+0xAC` | quad rotation | Confirmed |
| PTQD `+0xC4` | primary quad dimension | Confirmed |
| PTQD `+0xDC` | secondary quad dimension | Confirmed |
| PTQD `+0xF4` | lock secondary dimension to primary | Confirmed |
| PTQD `+0xF8` | PTEX descriptor | Confirmed |

`0x007A5F17` evaluates quad rotation, adds initial particle rotation `+0x8C`, then performs angle conversion. `0x007A28E7` and `0x007A291F` evaluate the dimensions; `0x007A28FE` bypasses secondary evaluation and reuses primary size when locked. Full evidence is in `particle_description_channel_map.csv`.

## PTEX/0000

PTEX reads a shader resource string, six 32-bit fields at legacy offsets `+0x14..+0x28`, then a Boolean at `+0x2C`. `+0x18/+0x1C` are inclusive first/last frames. Layout values 1, 4 and 16 map to 1, 2 and 4 frames per row; `+0x20` is reciprocal UV-cell extent. `0x007B7100` divides frame index by frames-per-row and emits four UV boundaries. `+0x28 == -1.0` chooses normalized-age selection; positive values are frames per second. The final Boolean enables shader lookup/rendering. See `ANALYSIS_CHECKPOINT_2026-09-13_PTEX_SPRITE_ANIMATION.md`.

## Replacement contract

Deserialize each version field-by-field into engine-neutral records; never mirror pointer-bearing 32-bit objects. Simulation/render preparation should publish `{transform, rgb, alpha, sizeX, sizeY, rotation, uvRect, resourceId}`. A DX12, Vulkan or Godot backend may batch that freely. Attachments and mesh particles must remain represented even if an early renderer milestone supports only quads.

## Remaining unknowns

1. Editor label for the PTCL/0001 extra prefix string and several prefix scalars.
2. PTCL `+0x98` is now confirmed as an editor description/name; PTEX `+0x04` is the shader logical path.
3. PTEX is closed separately in `ANALYSIS_CHECKPOINT_2026-09-13_PTEX_SPRITE_ANIMATION.md`.
4. PTMH is closed separately in `ANALYSIS_CHECKPOINT_2026-09-13_PTMH_MESH_PARTICLES.md`.

These are field/detail unknowns, not unknown control flow. Overall weighted client-research progress remains approximately 74%; this block deepens the asset/particle slice but alone does not justify a full percentage-point increase.
