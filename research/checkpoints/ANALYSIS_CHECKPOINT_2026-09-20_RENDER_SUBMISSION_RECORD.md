# ShaderPrimitiveSorter submission and draw-record ABI

Date: 2026-09-20

## Scope

This block follows a renderable from the public sorter submission entry points into its semantic phase queue, then through the phase draw loop. It complements the phase-profile table without inventing names for unresolved virtual methods.

## Submission routing

The principal entry `shader_primitive_sorter_submit_VA_00773870` receives a render primitive pointer. It:

1. invokes the installed resolver callback at `0x019234A4` (default `0x00773F50`) to obtain the associated shader/material-like object;
2. reads that object's phase index at `+0x14`;
3. optionally applies the installed phase-override predicate and replacement indices;
4. computes `phaseBase + phaseIndex * 0x24`;
5. calls a shader/material virtual predicate through `0x0076A4C0`;
6. enqueues via `0x00771D00` into one of two per-environment bitmask groups (`0x0192351C` or `0x01923520`).

The explicit-index entry `0x00773910` follows the same route without phase inference. `0x00773970` has special handling for phase indices 2 (`MAIN`) and 5 (`ALPH`) and can attach an additional float; `0x00773AE0` enqueues using the shader/material's phase directly.

This proves that the phase is a property of the resolved shader/material object, not of the primitive alone, while submission may override it for specialized draws.

## Fixed draw record

Each phase owns a vector of 0x28-byte records. Construction in `0x00771D00` / `0x00771E60`, profile preparation, comparators, and consumption establish:

| Offset | Size | Role | Confidence |
|---:|---:|---|---|
| `+0x00` | 4 | primitive/renderable pointer | Confirmed |
| `+0x04` | 4 | resolved shader/material-like pointer | Confirmed broad role |
| `+0x08` | 1 | queue/visibility-group flag | Confirmed storage; exact enum unknown |
| `+0x09..0x0b` | 3 | padding/unused in observed paths | Strongly inferred |
| `+0x0c` | 4 | per-entry float, default chosen from one of two global constants | Confirmed value type; exact semantic label unknown |
| `+0x10` | 4 | profile-2 key from primitive `+0x20` | Confirmed source |
| `+0x14` | 4 | profile-2 key from primitive `+0x08` | Confirmed source |
| `+0x18` | 4 | profile key from shader/material virtual `+0x18` | Confirmed source |
| `+0x1c` | 4 | profile key from primitive `+0x1c` | Confirmed source |
| `+0x20` | 4 | profile-3 float from shader/material virtual `+0x14` | Confirmed source/type |
| `+0x24` | 4 | mutable per-entry state passed to the mask/application helper | Confirmed use; exact type unknown |

Profile 2 compares `+0x10,+0x14,+0x18,+0x1c` lexicographically. Profile 3 compares `+0x18,+0x1c` in its primary comparator and uses `+0x20` in the depth-oriented sorting implementation. This is stronger than merely classifying phases as opaque/translucent, but the executable does not expose original field names.

## Draw consumption

`shader_primitive_phase_draw_entries_VA_00772B30` iterates the active slice of 0x28-byte records. For each entry it:

1. applies the entry's mask/state through `0x007736D0`;
2. invokes a virtual method at `shader/material vtable + 0x1c`;
3. sends the byte/float pair at `+0x08/+0x0c` to graphics wrapper `0x00755E60`;
4. obtains an iteration count from primitive offset `+0x18`;
5. for every iteration calls graphics wrapper `0x00755910(primitive,index)` and then `shader/material vtable + 0x20`.

After the phase, it resets the byte/float graphics state to `(0,1.0)`. The wrapper functions ultimately dispatch through the installed graphics backend vtable, confirming that ShaderPrimitiveSorter is API-independent orchestration above the Direct3D 9 backend.

## Mask-group lifecycle

Objects registered through `0x00773590` receive a compact index at object offset `+0x120` and contribute bits based on flags at `+0x11c/+0x11d`. `0x00773670` removes those bits. `0x007736D0` materializes the selected bit group into the active graphics-side list before a record is drawn. The implementation supports at most 32 directly addressable bits and diagnoses out-of-range indices.

For a replacement renderer this is a behavioral input-selection/light-influence mechanism, not a reason to reproduce the 32-bit mask or object offsets internally. Its externally visible visual behavior should be retained while the representation can become an unbounded handle set or compact GPU data structure.

## Confidence and remaining gap

- Routing, phase lookup, two queue groups, record size/layout sources, comparator fields, and draw-loop call sequence: **Confirmed by code**.
- The `+0x20` float as camera/depth distance and the two bit groups as exact light/visibility categories: **Strongly inferred**, not assigned as original names.
- Concrete RTTI names for the object at record `+0x04` and the virtual methods at `+0x14/+0x18/+0x1c/+0x20`: **Unknown**.

## Evidence

- submission routes: `0x00773870..0x00773B0C`
- record builders: `0x00771D00`, `0x00771E60`
- profile preparation: `0x00772AD0`, `0x00772AF0`
- draw loop: `0x00772B30`
- mask registration/application: `0x00773590`, `0x00773670`, `0x007736D0`
- graphics wrappers: `0x00755910`, `0x00755E60`
