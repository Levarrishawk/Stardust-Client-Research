# CPU face composition — 2026-10-01

## Question, inputs and stopping condition

Can a bounded engine-neutral compositor reproduce the selected original five prepared face layers under an explicit modern raster policy? Stop when hand-calculated fixtures and an independent raw-asset reference agree on coverage, mips and every layer's RGBA digest; GPU binding is next.

Inputs: documented executable/DLL identities reverified this run; pinned original human object/SAT/config and BTRT from the [execution boundary](IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_EXECUTION_BOUNDARY.md). Original asset bytes are unchanged. Tools: .NET native smoke/optional object-root probe, Python/NumPy/Pillow raw rectangular reference, prototype host build. No original binary execution/server contact.

## Findings and confidence

Confirmed implementation: `BlueprintFaceCompositor` uses explicit prepared slots and verified pixel-program semantics, ordered triangle fans, viewport clipping, sampler inheritance, point LOD/linear filtering and normal-opacity all-channel blend. Its policy, limits, cancellation, quantization and rejection boundary are specified in the [durable contract](../../docs/BLUEPRINT_FACE_COMPOSITION_CONTRACT.md). This is bounded preview compatibility, not hardware bit parity.

Observation: native and independent original0/INT_MIN/INT_MAX profiles agree on all15 cumulative layer hashes and final images. Each image256×256/262,144 RGBA bytes. Layer coverage65,536/4,096/32,640/65,536/65,536; triangle mips `[2,2]` for base,`[0,0]` for all overlays. Independent coverage uses original rectangle boundaries rather than copying the core triangle implementation. Zero profile finalSHA256 A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808; minimumBD17F0717182AE3E9DF7F778FAA9EE4DB3F2D0189FBBFBF09C28B434C033B2FF; maximum63BEF60E9EAE508996944042FCDA26D74E8DB40D099DD19DEEB5BCEFEA5A2E57. These digests characterize this stated policy, not an original client render capture.

Interpretation/Strongly inferred: authored smaller rectangles plus texture dimensions explain base minification to mip2 and differently sized overlay footprints. Confirmed raw sampler/program observations remain in the execution checkpoint. Global alternate opacity, driver rounding and final negotiated texture format remain unknown.

## Validation and reproduction

Run native `--skeletal-face-compose` with the established pinned original object/SAT/config options; run `tools/python/verify_skeletal_descriptors.py` over its ignored output with the configuration SHA pin. Exact51-source/2,283,555-byte union and prior source/geometry/texture/palette/program/preparation audits pass, followed by independent composition. Producer/verifier outputs remain private.

Portable smoke passes hand-calculated blend/copied-alpha, shared-edge/clipping/degenerate/linear-address/LOD/ownership and missing-resource/sampler/pixels/winding/planarity/nonfinite/budget/cancellation gates. Prototype host build passes with zero warnings/errors. Workspace/link and diff checks recorded in the session; no staging/publication.

## Consequence and exact next action

Generated output can now be uploaded through the original MGN TRTS MAIN binding. Add explicit preview customization profile selection, worker-owned preparation/composition, GPU byte readback and animated-character retention checks. Do not promote the preview policies to recovered hardware semantics.
