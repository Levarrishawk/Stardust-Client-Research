# October 13: bounded streamed terrain and modern material blending

## Question and stopping condition

Can the native x64/DX12 host retain full five-metre terrain sampling over a
320m moving window, reuse overlapping chunks, and interpolate the admitted
original family labels without breaking grounding, shared edges or cleanup?
Acceptance requires an exported runtime, independent original-byte association,
GPU attribute/pixel evidence, motion/ownership replay and failure/reset cases.

## Inputs and authority

Original Tatooine TRN SHA256:
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
TGEN SHA256:
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
The [surface contract](../../docs/TERRAIN_SURFACE_PREVIEW_CONTRACT.md) records
the direct Pre-CU evidence and exact eight admitted ASCN layers. Effective
archive lookup supplies five original first-entry MAIN textures. NGE source
at `949451032647e45e42c3aaef3f41b132c8af36e3` is corroboration only.

Host: prototype host4.7.2 .NET x64, D3D12 Forward+, RTX4070 SUPER. Official export
template archive SHA256 is
`92F8681E349EF1F90891B792DA95E3B2B0BD1ED610B78018C58FEB2D87E15A9D`.
The package includes its .NET runtime and reads the installed original assets;
assets, receipts and generated packages remain ignored private output. No
original executable, socket, live server, commit or publication was used.

## Observations and interpretation

- Explicit64 capacity retains40m chunks and5m sampling:64 chunks,1024 patches,
  8192 triangles per displayed window. Default16 remains available. Initial
  generation-zero adoption avoids rebuilding an already admitted window.
- CPU preparation exposes a lease while old terrain remains published. Hidden
  successor GPU patches are staged with a soft2ms admission deadline and at most
  eight indivisible patch operations per frame. Evictions retire before another
  successor starts. Shared materials remove per-patch shader/material creation.
- The initial staging-only run failed to publish enough useful successors; that
  receipt remains failed. Adoption plus shared materials and bounded staging
  passed later runtime gates. Timing results apply to the combined changes.
- The engine-neutral blend maps families3/5/6/8 to four independent one-hot
  attributes; family9 is the remaining weight. Prototype host CUSTOM0 retains all36
  float components per patch. Geometry, vertex diffuse COLOR and UVs retain
  their previous meaning. Texture child selection remains modern first-entry.
- A synthetic actual-GPU probe caught automatic colour conversion of numeric
  diffuse uniforms. Vector4 upload preserves their numeric meaning. All five
  actual selected terrain materials use unit multipliers, so the existing
  center-material path was unaffected by that issue.

The implementation and measured behaviour above are Confirmed by fixtures and
actual runtime receipts. **Original blend/numeric/shader parity is not claimed.**
This is modern interpolation of a partial original categorical classification;
46 ASCN layers, slope assignment and original ALP/rotation composition remain
outside the admitted policy.

## Validation

The native blend suite passes3394 assertions with165 independent Fraction
samples, including negative-coordinate seams, reversed edges, pure families,
all existing topology patterns, ownership and malformed inputs. All30 default
offline regression suites pass; optional original/live suites remain separate.

The synthetic DX12 blend fixture passes36 checks and7 rejection cases,63
vertices/252 exact CUSTOM0 components and balanced7 visual/1 binding cleanup.
An independent checker binds14 reported pixels to the actual PNG, recomputes
their expected colour transfer, and rejects six altered receipts. Maximum RGB
error is0.001898 against the declared0.015 tolerance.

Actual Tusken160m proof:707 independently grounded positions,2304 independently
classified vertex labels/weights,1215 shared-position comparisons and61 mixed
patches. Actual Tusken320m streaming proof:3146 positions over101.407m, four
successor windows,46080 vertex assignments,24955 shared-position comparisons and
780 mixed patches across five windows. All1536 visuals and one blend binding
are disposed. Captures were inspected.

The self-contained blended320m package passes15 checks:3181 motion updates over
101.493m, four successors, independent original material association, strict
CPU/GPU bounds, backend/input refusal, rollback, CPU cancellation, staged
cancellation and evicted-spawn reset. The package remains unchanged through
verification. Wall-clock post-publication intervals are12.28–24.53ms in this
run; startup/capture intervals reach641.56ms. Engine delta and monotonic wall
intervals are recorded separately. Whole-frame pacing and physical input remain
unverified; synthetic controls drive the proof.

## Reproduction and next probe

Use the region runbook (external historical reference),
[residency contract](../../docs/TERRAIN_REGION_RESIDENCY_CONTRACT.md),
[upload contract](../../docs/TERRAIN_GPU_UPLOAD_STAGING_CONTRACT.md),
[blend definition](../../docs/TERRAIN_MODERN_FIVE_FAMILY_BLEND_CONTRACT.md) and
[GPU boundary](../../docs/TERRAIN_FIVE_FAMILY_GPU_BLEND_CONTRACT.md).
The export builder's `--stream-region 98 58 --window-chunks 8 --modern-blend`
selects the accepted moving-window configuration. Developer launches accept
`--terrain-modern-blend` after region/streaming options. The older center mode
remains available without that explicit selection.

Next bounded feature is original local-pond geometry and explicit modern MAIN
presentation, subject to original-byte and actual-GPU checks. A private coarse
view-distance probe already established that width160/4x4 chunks can surround
the accepted fine window:289 common heights match bit-for-bit, but all289
normals differ and192 fine boundary vertices differ from coarse triangle
interpolation by up to7.5395m. Do not overlay those grids as completed LOD:
overlap removal,4:1 edge transitions and normal treatment are required first.
No research, function or protocol denominator changes follow from this work.
