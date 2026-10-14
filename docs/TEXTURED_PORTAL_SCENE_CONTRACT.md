# Textured portal scene inspection contract

## Boundary and ownership

`TexturedPortalSceneReader` attaches supported immutable static material/effect/DDS bundles to the existing shared cell geometry. One outer owned lookup cache covers every attempted geometry, floor, graph and material dependency. The default limits remain 256 attempted resources/128 MiB encoded bytes plus 128 MiB unique decoded RGBA pixels. Repeated source paths resolve once; decoded material/effect/texture content shares by SHA. Intermediate owned buffers may coexist: these budgets do not guarantee peak memory. Root pins and aggregate budget failures are fatal; unsupported material bundles retain their geometry with explicit diagnostics. A failed bundle may have read/decoded dependencies before encountering its unsupported shader; sources include these attempts, while material meshes contain only complete bundles.

## Older baseline effect versions

EFCT0001 now accepts inline IMPL0004/0005 with PASS0008/0009. All other versions/references retain rejection. IMPL0004 DATA is five bytes: U8 pass count + LE U32 phase tag. IMPL0005 adds two U8 flags. The description exposes the implementation version and nullable flags; missing serialized flags are not fabricated. Original loader 0075D730 reads the five bytes at 0075D86D–0075D89F, then computes runtime flags via 0075E4E0 at 0075DA2F. Original capability/option selection is not reproduced by this structural decoder.

PASS0008 DATA is 51 bytes; PASS0009 is 56. Original loaders 007613C0 and 007619D0 agree on the common serialized ordering documented in [the asset pipeline](ASSET_PIPELINE.md). Version0008 omits runtime offsets **4D,64,68,6C,70**. Its read stream jumps directly from 4C to 50 (0076158E–007615A9) and from 60 to 74 (007615F6–0076160C). Those keys are absent in the raw-field dictionary; no constructor default is asserted. Widths, versions and exact payload extents are enforced; raw enum bytes are preserved rather than normalized into runtime booleans.

## Host policy and known omissions

`--asset-interior --interior-textureshistorical prototype identifier (omitted)--main-texture-only` explicitly selects unlit, opaque named MAIN/UV0 inspection and permits omission of original precalculated vertex lighting. Strict inspection still rejects that lighting requirement. Unsupported shader families, nonopaque material candidates and missing MAIN/UV0 retain neutral surfaces. Packed vertex color, original programs, lighting, customization, portal culling and movement are not translated.

The pinned eight-cell YT-1300 has **77 attempted present sources, seven complete textured mesh bundles, 23 textures and 221 mip levels**. Four exterior LOD material bundles encounter CSHD and remain neutral. CPU source/pixel agreement includes all attempted sources; the successful effect bundles agree on 18 implementation/pass records. GPU MAIN inspection renders 78 textured/ten neutral surfaces, seven textured/four neutral templates, and shares 23 uploaded textures. Three surface fallbacks are one cockpit collision material and two grate materials; four exterior template fallbacks account for seven further neutral surfaces. Strict headless inspection reports zero textured/88 neutral surfaces with its explicit lighting diagnostics; this does not negate the successful decoding or actual GPU upload verification.

## Reproduction and next probe

Run `verify_portal_scene.py ROOT CONFIG MEMBER SHA CORE_DLL --textureshistorical prototype identifier (omitted)Invoke-StardustAssetPreview.ps1 -Interior -InteriorTextures -MainTextureOnly -InteriorFloors -InteriorPaths -Cell 1` with the pinned inputs and a fresh private capture path. The host verifies all uploaded texture mips and mesh arrays; the GPU coordinate fixture is separate from headless checks.

Next: trace CSHD0001 inline static-shader ownership and customization records before allowing an explicitly labeled default customization inspection. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TEXTURED_PORTAL_SCENE.md).

The later [authored texture policy](AUTHORED_CUSTOMIZATION_TEXTURE_CONTRACT.md) optionally closes the four CSHD exterior bundle omissions; strict default rejection remains intact.

The opt-in [original room-material adapter](STATIC_ROOM_LIGHTING_CONTRACT.md) now translates three selected baked/specular/emissive variants in the bounded scene. Existing texture-only inspection remains available; original capability selection and door normal mapping remain separate.

Selected static doors now have an opt-in [retained normal/specular adapter](STATIC_NORMAL_MATERIAL_CONTRACT.md), sharing original LOD templates and geometry textures.
