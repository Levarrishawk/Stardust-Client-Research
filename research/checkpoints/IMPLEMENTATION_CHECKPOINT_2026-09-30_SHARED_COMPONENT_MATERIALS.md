# Shared component materials checkpoint — 2026-09-30

## Question and stop

Can composed static appearances retain shared owned material/pixel resources and expose supported MAIN textures without replacing an entire mesh when one material is unsupported? Stop after deterministic sharing/failure fixtures, independent original mip/source agreement and real GPU sharing/readback checks.

## Inputs

Same verified executable/configuration baseline as the [garage checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_SORTED_MESH_GARAGE.md): executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; configuration `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; 48 TREE0005 mounts. Root `appearance/component/mun_all_garage_s01_u01.cmp`, priority 30, SHA-256 `1FE0DF11A1A1B6506AA18B1672F87A03F6C892D48C91A72D63E4D3B170D64B12`.

Tools: owned .NET core and smoke fixtures, existing independent IFF/TRE/Pillow readers extended through `verify_component_appearance.py --textured`, Godot 4.7.2 .NET x64 headless and host OpenGL GPU. Original executable and live sockets remain unused.

## Result and confidence

**Confirmed implementation:** a per-load decode cache shares immutable shader/material descriptions, effects and DDS images by SHA-256 content identity. Two independent mesh paths in synthetic fixtures refer to the same decoded material/effect/image objects; each logical dependency is resolved once. Missing effect bundles retain geometry with explicit material diagnostics. Aggregate decoded limits are fatal, not swallowed as neutral fallback. APT-to-MESH templates retain their terminal mesh source path separately from the requested graph identity.

**Confirmed original agreement:** the complete garage bundle has 50 sources, nine material meshes, eleven shaders, eight effects, seventeen textures and 151 mip levels. The independent reader discovers dependency paths from original IFF bytes, verifies every source identity and matches every decoded RGBA mip hash. The nine geometry hashes and 400 sorted variants continue to match independently.

**Confirmed Godot boundary:** all nine templates upload with 35 textured surfaces and ten explicitly neutral surfaces across all loaded LODs. A shared GPU cache owns seventeen unique ImageTextures rather than duplicating them per mesh. All uploaded geometry/pixel readback checks, LOD midpoint switches and the existing asymmetric GPU UV/front-face fixture pass. Headless and hidden GPU runs exit zero. The captured textured garage was visually inspected.

The first GPU attempt replaced six complete mesh templates because one shader in each failed the opaque MAIN policy. The corrected opt-in component mode substitutes a neutral material only for the rejected shader batches and reports their paths/reasons. Standalone preview remains strict by default. Premultiplied-alpha eligibility is checked for the selected MAIN material, while other source images remain unchanged and uploadable as data.

## Limits and consequences

This is explicitly unlit MAIN inspection. Original programs, multi-stage blending, translucent ordering and lighting are not translated. Neutral diagnostic surfaces preserve geometry; they do not claim original visual behavior. SIDX remains in core but this inspection adapter draws ordinary INDX.

Per-load source/graph limits and the 128 MiB unique decoded/GPU RGBA budgets are retained. The decoded aggregate is checked after one bounded image decode, so temporary allocations can exceed the retained aggregate. These are resource policy limits, not measured peak-memory/VRAM guarantees. GPU cache lifetime belongs to one component preview; adapters release their resources before the cache. No global cross-corpus cache or background resource mutation is introduced.

Canonical behavior is consolidated in the [component contract](../../docs/COMPONENT_APPEARANCE_CONTRACT.md). Reproduce using the existing launcher with `-Component -ComponentTextures -MainTextureOnly` plus explicit root/configuration/member/hash. The independent verifier uses `--textured`, which also checks the supported static geometry graph.

## Validation and next probe

Native smoke suite passes; Godot build has zero warnings/errors. Independent 151-mip checks and actual seventeen-texture GPU sharing/readback pass. The pre-existing Godot certificate-store warning persists with successful offline exits. Research scores, address-name count and protocol registries do not change.

Next: identify the virtual local offset consumed by DTLA PIVT=0 and decode its APPR extent source. Implement camera/world distance only after that offset is proven; keep explicit offline selection as a reproducible inspector control.
