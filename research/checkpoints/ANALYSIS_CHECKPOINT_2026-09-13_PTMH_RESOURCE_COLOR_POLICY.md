# PTMH resource types and common-color policy

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Effective resource census

A zero-error traversal found all 399 effective `PTMH/0000` records and 152 unique referenced appearance resources. The field is not restricted to `.apt`:

| Suffix | Records |
|---|---:|
| `.apt` | 275 |
| `.msh` | 79 |
| `.sat` | 45 |

No resource name is empty. Paths use both slash styles. Common values include `appearance/frn_all_glow_torch_s1.apt` (69), `appearance/mesh/mesh_grenade.msh` (18), `appearance/distant_tie_fighter.apt` (16), and skeletal `.sat` assets such as bounty-hunter armor and ambient creatures. The replacement must route the string through the general appearance factory so the suffix selects the correct downstream loader; hard-wiring PTMH to a mesh or APT decoder is incorrect.

## Color and alpha behavior

The mesh branch of `particle_emitter_simulate_particles_VA_007A4840`, selected by particle-description type 1 at `0x007A4D18`, updates referenced appearance position/orientation, uniform scale, culling extent and attachment position. It does not evaluate the common PTCL `CLRR` or alpha waveform and does not pass a particle tint/alpha to the appearance. Those channels are evaluated in quad preparation `0x007A2660`.

Therefore the deployed client treats PTCL color/alpha as quad-render data, not a universal tint contract for arbitrary PTMH appearances. This is **Confirmed for this executable's direct mesh update path**. A downstream appearance may still animate its own material independently, but it does not receive the PTCL particle color from this branch.

## Replacement contract

Instantiate PTMH resources through the ordinary appearance system, preserve `.apt`, `.msh`, and `.sat` indirection, normalize slash direction only at resource lookup, and apply the four PTMH scale/rotation waveforms to the appearance transform. Do not impose PTCL quad tint/alpha on mesh particles unless later dynamic evidence proves a separate route.

The complete inventory is retained as `ptmh_resources.csv`.
