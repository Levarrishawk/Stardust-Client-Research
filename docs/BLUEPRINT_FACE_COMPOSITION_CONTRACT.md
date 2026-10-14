# CPU blueprint face composition

## Boundary and policy

`BlueprintFaceCompositor.Compose(blueprint,resources,preparation,limits,cancellation)` returns an owned one-mip RGBA8 image and ordered layer diagnostics. Input comes from original BTRT and explicit prepared name/Boolean customization values. The core is engine-neutral. Original shaders/programs are parsed as data; no original executable code runs.

Policy identifier: `parallel-top-left-half-pixel-rgba8-normal-opacity-v1`. PCAM projects coordinates into a top-left viewport; pixel samples use x+.5,y+.5. Triangle fans use a top-left shared-edge rule, clockwise screen winding and planar finite Z within the camera range. Degenerate triangles skip; opposite winding/nonplanar data reject. Viewport clipping affects coverage without stretching UVs. Default limits:1024-pixel dimensions,1024 triangles and33,554,432 bounding-box pixel tests; cancellation is checked during preparation and each raster row. Output publishes after complete composition.

This preview policy is explicit. Exact original D3D9 pixel centers/subpixel precision, hardware rounding, capability-dependent destination conversion and inherited alternate global opacity are not claimed. Ordered original formats remain metadata; RGBA8 scratch alpha is retained in the preview. See [camera/sampler execution boundary](BLUEPRINT_FACE_DESCRIPTION_CONTRACT.md#camera-sampler-and-execution-boundary).

## Supported shader and sampler

Exactly one programmable single-pass candidate is required per rendered shader slot. PASS0007 must disable depth writes/test, alpha test and stencil; enable all channel writes; use ADD/SRCALPHA/INVSRCALPHA and MAIN factor with no second factor. Pixel stage0 references MAIN; its PTXM0000 inherits address/filter values. TXM0002 supports wrap or clamp U/V, wrap W, point mip/linear min/linear mag and zero anisotropy override. MAIN maps to UV0. Vertex program is the documented texren copy transform path; original source/include compilation is outside this CPU policy.

The referenced owned PSHP0000 must pass `BlueprintFacePixelProgramDecoder`'s exact sampled-texture/factor/copied-alpha semantic recognizer. Pixel RGB is textureRGB×factorRGB/255. Factor alpha does not alter source alpha. Copy writes all channels. Normal alpha blend writes RGB=sourceRGB×As+destinationRGB×(1−As), alpha=As×As+Ad×(1−As). Both alpha terms use normalized source alpha. No gamma conversion or premultiplication occurs.

Affine UV derivatives determine maximum axis texture footprint; point LOD uses floor(log2(max(1,footprint))+.5), clamped to declared mips. Each chosen mip uses bilinear U*width−.5,V*height−.5 sampling with independently addressed neighbors. Undeclared retained terminal blocks do not become selectable mips. Every layer writes RGBA8 via floor(value+.5) with byte clamping. Layer diagnostics retain shader/vertex-array indices, selected source, triangle mip levels, written pixel count and cumulative RGBA SHA256.

## Evidence and verification

Native `--skeletal-face-compose` follows the pinned original object→SAT→LMG→MGN→TRTS→BTRT/effects/DDS/palettes/pixel-program chain:51 sources/2,283,555 bytes. All assets remain unmodified. Three explicit profiles (all variable values0,INT_MIN,INT_MAX) each produce five layers/256×256 RGBA8. Base/freckles/mask each write65,536 pixels, eyebrow4,096 and clipped beard32,640. Base chooses declared mip2; overlays mip0.

The independent Python verifier derives rectangular coverage/UVs directly from original VTXA bytes, reads original shader blend/sampler bytes and independently decodes selected DDS mips with Pillow. Every cumulative layer digest and final262,144-byte image agrees for all three profiles. This independently checks the bounded preview algorithm; it does not establish legacy-driver bit parity.

Portable hand-calculated fixtures cover source factor/unchanged alpha, all-channel blend, shared fan edges, clipping, degeneracy, wrap/clamp, point LOD and owned output. Missing program/binding, invalid pixels/sampler, opposite winding/nonplanar/nonfinite coordinates, dimensions/triangle/work budgets and cancellation reject. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CPU_FACE_COMPOSITION.md).

## Generated skeletal binding and preview

`BlueprintGeneratedFaceBuilder.Create` consumes owned geometry/blueprints/resources and exact supplied values. It checks reference closure, MAIN shader-index bounds and conflicting destinations. Binding keys are mesh path/shader index/tag; renderer-path outputs are shared. Limits:64 meshes,16 outputs,4,096 values/bindings and16,777,216 aggregate output pixels. Cancellation propagates into composition. Strict version1 JSON `BlueprintFaceCustomizationProfile` supplies variableName/factoryFlag/int32 rows with no implicit defaults; duplicate/unknown fields/keys/types and size/count violations reject.

Prototype host `--creature-face-profile <path>` requires material defaults. The launcher exposes `-CreatureFaceProfile`; `poc/preview-profiles/human-face-zero.json` is an explicit authored inspection profile. The CPU worker loads and composes before owned main-thread transfer. Combined original material/palette/idle/face closure requires an explicit128-source/32MiB encoded limit, leaving ordinary reader defaults unchanged. Observed72 sources/5,924,515 bytes; one raw head TRTS MAIN binding independently agrees.

Per-mesh generated materials override only that destination. GPU checks verify exact RGBA bytes and the actual material texture parameter identity; pose/animation updates retain material resources. Original480 idle updates/two loops, diagnostic reset, focus/pause/seek/play/scrub, CPU worker cleanup and zero-publication cancellation pass. Generated face is visually present on the original head. Full palette/body/eye shading and interior integration remain next. [Preview checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_GENERATED_FACE_PREVIEW.md).
