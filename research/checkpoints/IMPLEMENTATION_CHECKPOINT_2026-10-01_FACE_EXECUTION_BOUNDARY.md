# Face execution boundary — 2026-10-01

## Question and stop condition

What camera, sampler, scratch target and programmable output constrain the selected original human face's five triangle-fan layers? Stop at direct executable/program-byte evidence and independently verified owned pixel-program dependency; CPU/GPU raster parity is not claimed.

## Inputs and tools

Baseline Stardust.exe SHA256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150; s207_r.dll SHA256 D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D, base62A00000; both reverified this run. Original BTRT SHA256275C45C62CBF25982F20AD45C6986C69F335BF0C3385E3036900E7A78756DA2F, unmodified configured overlay. Static PE disassembly, existing curated loader research, owned .NET decoder and independent Python original-chain verifier. Read-only Ghidra Java script loading failed; no decompiler output from those attempts is used as verification. Existing disassembly and prior evidence provide the observations below.

## Observations, interpretation and consequence

| Observation | Interpretation/confidence | Consequence |
| --- | --- | --- |
|0086C400 reads PCAM scalar;0086C380 sets viewport0,0,W,H and00768620 range0,0,scalar,scalar;0086E240 constructs camera0086B410. Camera vtable015E7AC4 +68→00767700.|Confirmed parallel camera setup; orthographic branch00767A8C computes clip X=2*x/scalar−1, Y=1−2*y/scalar.|Top-left origin with increasing Y downward. Hardware pixel-center/subpixel precision remains separately unverified.|
|Original five VTXA arrays have four position/UV vertices, Z3: base/freckles/mask full0..1 squares; eyebrow X~.4975..9975,Y~.2325..3575; beard X~−.00407..99593,Y~.49346..99346.|Confirmed byte structure and layer rectangles.|Clip beard at viewport left; do not stretch smaller overlays over the entire destination.|
|00781A20 TXM0002 consumes tag, source flag, three address bytes, mip/min/mag filters and anisotropy. Original copy addresses0,0,0; alpha2,2,0; both filters1,2,2. PTXM0000 addresses5,5,5/filters6,6,6 inherit.62A0E570 merges/ translates with tables62B514E8/62B51500.|Confirmed wrap for copy, clamp for alpha, point mip selection and linear min/mag.|Preserve inherited sampler values; reject unsupported sampler/program patterns in the bounded preview.|
|Actual DEST has count4, IDs4,3,5,6. Direct table015E7A58 maps these to internal4,1,3,0.|Confirmed RGB888,ARGB4444,XRGB8888,ARGB8888, in order.|Historical selected-template format summary was wrong. Capability-dependent destination conversion is not a universal RGBA8 claim.|
|00869CD0 creates one-mip, noncube, non-render-target destination via007650E0;008706A0 sets it through00755750→62A04060→62A0AF40. Managed branch uses existing ARGB8 scratch render target; prior62A0B120 trace reads back and converts.|Confirmed scratch-target path; final format selection remains capability-dependent.|Blend into ARGB8 scratch before optional destination conversion. Modern RGBA8 preview can retain scratch alpha with explicit policy.|
|Original pixel_program/texren_copy_c1a1.psh, priority40,158 bytes,SHA2569162152E5DB23B15F06AC8DE9E4DAF223614D8226BEF6C6183266D7B9CB796EB: PSHP0000/PSRC/PEXE44 bytes. Shader1.1 TEX t0; MUL r0.rgb,t0,c3; coissued MOV r0.a,t0.a; END.62A0E9D0 normalizes MAIN factor;62A0EF30 binds compiled factors at c3.|Confirmed sampled RGB×factor; source alpha unchanged by factor alpha.|Validate program bytes before CPU interpretation. This is a small semantic recognizer, not arbitrary original-code execution.|
|62A05620 normal branch applies cached blend/mask, while global DAT62B82FD1 enables an alternate opacity path set by62A04EF0.|Confirmed branch; blueprint entry's inherited global mode unknown.|Use explicit normal-opacity preview policy; do not assert original runtime bit parity.|

Blend factors established in the [inline-effect checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_INLINE_FACE_EFFECTS.md): ADD, SRCALPHA and INVSRCALPHA, all channels writable. Under the normal branch the alpha channel uses the same factors: As*As+Ad*(1−As). No separate-alpha override is established for this slice.

## Implementation and validation

`BlueprintFacePixelProgramDecoder` owns bounded PSHP0000 metadata and recognizes exactly the observed shader1.1 instruction/mask/register/swizzle pattern. Unsupported patterns, envelopes and truncation reject. Optional `loadFacePixelPrograms` requires face resources; default resource closure is unchanged. Program dependencies share the original source cache and deduplicate by normalized path. Vertex source/includes are not yet an owned program closure.

Native `--skeletal-face-programs` uses the pinned original object/SAT/config path. 51 sources/2,283,555 bytes; one shared pixel program. Independent verifier derives the required PPSH name from original shader bytes, compares instruction bytes/register semantics and the exact source union, and retains prior geometry/texture/palette/preparation audits. Portable source ownership/every-truncated-prefix/every-altered-token rejection and original missing/masked program cases pass. Godot build: zero warnings/errors. Workspace/link and diff checks recorded in the session.

## Remaining uncertainty and next probe

Implement bounded engine-neutral raster/composition with explicit pixel-center, LOD, quantization and normal-opacity policies; independently compare original prepared layers and deterministic hand-calculated blend/raster fixtures. Then upload generated MAIN through MGN TRTS binding. Hardware format negotiation, alternate opacity, vertex includes and exact legacy subpixel/rounding parity remain separate open questions.
