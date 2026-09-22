# PTMH mesh-particle specialization checkpoint

Date: 2026-09-13

Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Outcome

`PTMH/0000` is now structurally and behaviorally mapped. It reuses the common PTCL lifetime/resource framework, names a general appearance resource (`.apt`, `.msh`, or `.sat` in the observed corpus), and adds four waveforms: uniform scale followed by three authored rotation axes. Runtime creates a normal appearance object through the shared factory, attaches it to the particle, updates its transform every frame, and samples scale/rotation from normalized particle age.

## Object and loader structure

`ParticleDescriptionMesh` is a 0x120-byte legacy object with vtable `0x015E51F0`. Its constructor `0x007C1A90` constructs four WVFM members at `+0xAC`, `+0xC4`, `+0xDC`, `+0xF4`, clears cached appearance-template pointer `+0x10C`, and constructs the resource-name object at `+0x110`.

`0x007C2080` enters `FORM PTMH`, loads common PTCL through `0x007BF140`, accepts specialization chunk `0000`, and calls `0x007C2200`. The detail loader reads a NUL-terminated resource name from chunk 0000 through `0x007C1FB0`, then loads four WVFM forms. Save function `0x007C2320` writes the same order.

Important framing detail: unlike PTQD, PTMH's specialization version marker is a `0000` chunk, not `FORM 0000`; the four WVFM forms follow it as PTMH siblings.

## Runtime materialization

In `0x007A4000`:

1. `0x007C2070` returns the PTMH resource-name object.
2. The resource system resolves the name to an appearance template and constructs an appearance instance.
3. The instance is retained by the particle, registered with the containing render/world object, and receives the particle transform.
4. The four authored waveforms initialize per-particle evaluator state at `+0x74`, `+0x7C`, `+0x84`, `+0x8C`.
5. Common random-orientation policy initializes additive starting angles at `+0x94`, `+0x98`, `+0x9C`.

During `0x007A4840`, the PTMH branch is selected by particle-description type 1. Waveforms 2-4 are evaluated, added to their initial angles, converted to radians and applied through three distinct transform-axis rotation helpers. Waveform 1 is evaluated and multiplied by the emitter/global scale; the result is copied to all three components before the appearance scale setter is called. This proves uniform scale rather than three-axis nonuniform scale.

| PTMH field | Runtime meaning | Confidence | Evidence |
|---|---|---|---|
| `+0xAC` | uniform scale over lifetime | Confirmed | `0x007A4FE3` evaluates it, multiplies global scale, replicates scalar into XYZ, calls appearance scale setter |
| `+0xC4` | rotation axis 1 over lifetime | Confirmed role / Strong axis label | `0x007A4E9C`, additive initial angle, helper `0x00AB8EA0` |
| `+0xDC` | rotation axis 2 over lifetime | Confirmed role / Strong axis label | `0x007A4EEB`, additive initial angle, helper `0x00AB8E10` |
| `+0xF4` | rotation axis 3 over lifetime | Confirmed role / Strong axis label | `0x007A4F3A`, additive initial angle, helper `0x00AB8F30` |
| `+0x10C` | cached appearance-template pointer | Strong | replaced/released by resource-name assignment and destroyed through template release path |
| `+0x110` | appearance resource name | Confirmed | consumed by the shared appearance factory; effective suffixes are `.apt`, `.msh`, and `.sat` |

Constructor reset `0x007C1DA0` makes scale a constant 1 curve and all three rotations constant 0 curves, independently reinforcing the mapping.

## Corrected effective corpus census

A corrected context-aware traversal of 2,114 live effective `.prt` winners (9 tombstones, zero expansion/framing errors) gives:

| Form/detail | Version | Occurrences |
|---|---:|---:|
| PTCL | 0000 | 1 |
| PTCL | 0002 | 1,803 |
| PTCL | 0003 | 4,569 |
| PTMH | 0000 | 399 |
| PTQD | 0000 | 8 |
| PTQD | 0001 | 5,965 |

These are nested description occurrences, so their count legitimately exceeds the number of top-level particle assets. The previous checkpoint's lower PTCL count was incomplete because its scanner advanced over an assumed external pad byte after odd-sized chunks. SWG IFF item sizes in these assets directly delimit the next item; no such external pad is present. `scan_particle_description_versions.py` and `particle_description_versions.csv` now embody the corrected rule.

## Replacement-client implications

- Mesh particles should instantiate ordinary appearance resources through the same factory used by world objects, not a particle-only mesh loader. The effective set contains 275 `.apt`, 79 `.msh`, and 45 `.sat` references.
- Particle simulation owns sampled uniform scale, orientation and transform; renderer backends consume the resulting appearance instance/state.
- The new client may pool or instance repeated mesh-particle appearances, but observable per-particle transform, scale, color/alpha/common PTCL behavior and attachment lifetime must remain compatible.
- Preserve `.apt`, `.msh`, and `.sat` indirection and downstream appearance-factory behavior; resolving directly to one guessed mesh would break composite, skeletal, or indirect appearances.

## Remaining narrow questions

1. Confirm human-readable X/Y/Z labels for the three transform helper orderings; their independent three-axis roles are already confirmed.
2. Downstream loading is now bounded to 152 unique resources across `.apt`, `.msh`, and `.sat`; individual transitive dependencies remain part of those already-documented appearance pipelines.
3. The direct mesh update path does not evaluate or forward PTCL CLRR/alpha. These are quad-render channels in this executable; arbitrary PTMH appearances retain their own material behavior.

Overall weighted client-research progress remains approximately 74%. This block closes the principal PTMH loader and runtime path but does not alone warrant a full percentage-point increase.
