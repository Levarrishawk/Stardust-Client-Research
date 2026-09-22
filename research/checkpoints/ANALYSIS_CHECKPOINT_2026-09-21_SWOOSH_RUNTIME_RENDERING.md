# Swoosh Runtime Sampling and Rendering

Date: 2026-09-21

## Result

The live `SwooshAppearance` path is a CPU-maintained, shader-rendered ribbon. It samples two endpoints over time into a bounded ring buffer, generates a dynamic vertex stream from adjacent samples, submits it through the normal shader-primitive path, and maintains DPVS visibility state.

## Runtime object and ownership

`SwooshAppearanceTemplate_createAppearance_VA_007AD260` allocates a 0x344-byte live appearance. Constructors beginning at `0x007aefb0` support several attachment modes, including no explicit attachment, an owning object, and one or two named attachment keys. They:

- retain the template;
- allocate a bounded sample array using a template/global sample-capacity value;
- initialize a shader primitive owned by the appearance;
- create a DPVS object and initially disable it;
- retain optional attachment CRC strings/references.

The destructor releases the primitive, DPVS reference, attachment objects, sample storage, shader references, and template-owned strings through the normal reference-counting paths.

## Trail sampling

`FUN_007b19e0` is the core endpoint sampler. It accepts two world-space `Vector3` endpoints and:

1. accumulates elapsed sampling time;
2. inserts samples at a fixed interval, including multiple catch-up samples when a frame spans more than one interval;
3. stores each sample in a 44-byte ring element containing an active flag, endpoint A, an orientation/normal vector, and endpoint B;
4. wraps at the configured sample capacity;
5. tracks the live sample count separately from the write index;
6. computes a motion magnitude used by later presentation logic.

The current endpoints are also written into the active slot after catch-up insertion. This design makes trail density primarily time-based rather than one sample per rendered frame.

## Dynamic ribbon build

`FUN_007afcd0` builds the renderable ribbon when at least two samples exist. It:

- locks/reserves a dynamic vertex buffer sized from adjacent sample pairs and the configured cross-section/subdivision count;
- walks active samples in ring order;
- derives tangent/side vectors from neighboring endpoint pairs, normalizing and interpolating them where necessary;
- evaluates template/PTEX color, alpha, and texture-coordinate progression;
- emits paired/section vertices containing position, packed color, and UV data;
- unlocks the buffer;
- configures the shader primitive and submits it to the shader primitive sorter.

The implementation includes degeneracy handling for coincident or near-collinear samples, clamps modulation values to 0..1, and uses the object's transform/camera state when deriving presentation vectors.

`FUN_007b0ab0` is the per-frame alter/update path. It advances sample time, lazily creates optional attachment objects, converts stored endpoints between world and object space as required by the configured mode, updates auxiliary effects, expires old trail state, and returns the normal appearance alter result.

## Architectural interpretation

The original renderer performs CPU ribbon tessellation into a transient vertex buffer. A DX12 replacement can preserve behavior with either:

- the same CPU-generated vertex stream uploaded through a frame allocator (lowest-risk compatibility path); or
- a GPU expansion path fed by the 44-byte-equivalent logical sample records (later optimization).

The logical contract is the endpoint sample sequence, timing, interpolation, color/alpha evaluation, and UV progression—not the original dynamic-buffer API or 0x344-byte object layout.

## Replacement-client consequence

- Implement the first compatible version as CPU ribbon generation; it maps directly onto the recovered behavior and is easy to validate against screenshots/captures.
- Keep sample capacity and interval bounded and validate zero/degenerate endpoint spans.
- Store samples in an engine-neutral ring buffer with explicit endpoint and frame-vector fields.
- Use the shared material/PTEX system and transparent primitive sorting.
- Defer compute/geometry-shader expansion until the CPU reference implementation produces matching results.

## Confidence

- Time-based endpoint ring, 44-byte legacy sample stride, wrapping, and catch-up insertion: confirmed.
- CPU dynamic-vertex construction and shader-sorter submission: confirmed.
- Exact historic names for every template scalar controlling width/subdivision/fade: unknown.
- Recommended CPU-first DX12 implementation: architectural recommendation based on confirmed behavior.
