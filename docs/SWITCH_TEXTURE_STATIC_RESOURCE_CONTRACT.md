# Explicit SWTS frame inspection

R8-ASSETS-01, October 2, 2026. A caller-selected, pinned SWTS frame now completes the original mission terminal's four CPU material bundles. This is a modern inspection operation. The original executable's switcher behavior, DTST timing semantics, shader equations and rendered fidelity remain unknown.

## Observed asset structure

The read-only installed overlay `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, resolves `shader/anim_mission_terminal.sht` at priority 38, 475 bytes, SHA-256 `B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62`.

Its entire top-level layout is:

| Record | Observed content |
| --- | --- |
| `FORM SWTS / FORM 0000` | One version form |
| `NAME` | `shader/defaultemis.sht`, single NUL-terminated ASCII name |
| `FORM DTST / chunk 0000` | Eight bytes: `08 00 00 00 CD CC CC 3D` |
| Eight ordered `TEXT` chunks | Four-byte little-endian tag `0x4D41494E` (`MAIN`, stored bytes `NIAM`), then single NUL-terminated name |
| Texture names in order | `texture/anim_mission_terminal_01.dds` through `_08.dds` |

**Observation / confidence:** Confirmed from original bytes and independent Python parsing. The two DTST words are exposed as raw `uint32` values `8` and `0x3DCCCCCD`. The second word can also be represented as IEEE float32 approximately 0.1; its role is **Unknown**. The first word coincides with the number of references; a count interpretation is **Tentative**, and the decoder deliberately neither uses it as a count nor requires that equality. No runtime scheduler is inferred from this asset.

The base `shader/defaultemis.sht` resolves at priority 38, 171 bytes, SHA-256 `BD33741659BD3C322AE37DC8C0F4DDADBE940502DB273538DB6A34A9FE14D5F7`. It is `SSHT/0001`, names `effect/a_emis_full.eft`, and has one `TXMS/TXM /0002` binding: 12-byte DATA `4E 49 41 4D 00 00 00 00 02 02 02 00`, named `texture/defaulttexture.dds`. `TCSS/0000` retains bytes `4E 49 41 4D 00`. These are **Confirmed** structures; the existing static parameter/effect/texture decoders retain their established interpretation limits.

**Interpretation / confidence:** The repeated MAIN references can supply an explicitly chosen replacement for the base MAIN binding, **Strongly inferred** from the matching tags and base-shader reference. **Implementation consequence:** derive an inspection description from the actual base SSHT, keep its parameters/effect and other bindings, and change only the selected matching texture path. Do not fabricate SSHT source bytes or a source hash for that derived description. This does not prove the original runtime applies identical switching or initialization behavior.

## API and modern acceptance policy

SwitchTextureShaderDecoder (external historical reference) decodes owned IFF metadata and returns base path, ordered read-only frames and the two raw DTST words. It accepts only the observed `SWTS/0000` child order `NAME`, `FORM DTST` containing one eight-byte `0000` chunk, followed by 1..256 `TEXT` chunks. Unsupported versions, extra/reordered children, empty frame lists, malformed extents, trailing bytes, invalid strings and unsupported resource kinds reject. This replacement gate is not the original executable's complete acceptance set.

Names must be single terminated printable ASCII, 2..1025 bytes including terminator. Literal `.` and `..` segments reject before normalization, including backslash-separated segments. Base names normalize beneath `shader/` with `.sht`; frame names beneath `texture/` with `.dds`. They are logical archive paths. No filesystem discovery occurs. Legacy normalization otherwise remains available for asset references. These path and resource restrictions are a **modern policy**, not a recovered original rejection rule.

`SwitchTextureFrameSelection(ShaderPath, ShaderSha256, FrameIndex)` requires an already canonical `shader/*.sht` path, 64 hexadecimal SHA-256 characters and index 0..255. All reader entry points validate this option before lookup. The actual authored frame count is checked after SWTS decoding and before base lookup. The SHA pin covers the original SWTS bytes, not the derived static description; the base and selected DDS each retain their separately measured source identity in the result.

`TexturedStaticMeshReader.Load`, `TexturedAppearanceGraphReader.Load` and SceneStaticResourceReader.Load (external historical reference) append optional `switchTextureFrame` arguments. With no selection, the [Round 7 strict/neutral contract](SCENE_STATIC_RESOURCE_CONTRACT.md) remains unchanged: SWTS rejects through the existing unsupported SSHT-envelope path. The option authorizes only a reachable SWTS at its exact logical path. It does not require that every asset graph contain that path and does not enable other SWTS files.

For a matching SWTS, verify its pin, decode references, check frame bounds, read the actual base SSHT and use the existing SSHT decoder. Nested SWTS/CSHD bases reject. Selection requires exactly one matching tag with a named base binding and source flag zero. An absent/ambiguous tag or dynamic/unnamed binding rejects. Frame tags remain individually preserved, rather than guessed from a filename.

The returned material remains keyed by the original SWTS shader path. Its `SwitchTexture` metadata retains the owned SWTS description, immutable selection snapshot and actual base source identity. `References.Metadata` is the actual owned base SSHT metadata; `References.Textures` is the explicitly derived binding list. Consumers must use the latter for selected texture dependencies. `Parameters` is decoded from the unchanged base SSHT. Unselected frame DDS files and the replaced base default texture are not loaded; other base bindings, if present, remain dependencies.

The per-load cache shares ordinary SSHT/EFCT/DDS decoding by source hash. SWTS-derived materials bypass its content-only material key and perform the path/pin/index selection per mesh. Identical SWTS bytes at a different shader path cannot inherit the selected frame. Separate calls have separate caches. This policy is validated with a synthetic resolver variant of one original mesh whose shader path is changed to an alias, with both shader paths resolving to identical SWTS bytes.

Per-mesh reads retain cloned payloads in a local path cache, preventing duplicate source-map entries when base dependencies overlap. Outer appearance/static owned caches preserve one external attempt per normalized logical path. Returned IFF metadata, geometry and decoded pixels survive mutation of all resolver-owned byte arrays. Failed calls return no resource result; cancellation and source/decoded limits keep their existing fatal behavior, including under `AllowNeutral`. Missing/masked or unsupported selected material dependencies produce the existing neutral geometry diagnostics only when explicitly permitted.

The source count/byte limits and decoded limits are unchanged; they are not a peak-memory promise. There is no timer, automatic frame progression, randomness, callback, engine mutation, prototype host change, world readiness transition or original executable execution.

## Original terminal closure and validation

The original [Round 7 terminal recipe](SCENE_STATIC_RESOURCE_CONTRACT.md) is unchanged: CRC `2907007746`, root object pin `E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281`, APT pin `74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021`. Frame indexes 0 and 1 each retain four inherited object templates and all four detail meshes, whose vertex counts are 208/276/427/496 and triangle counts 102/142/232/296 in serialized coarse-to-fine order.

Each selected frame gives 27 unique sources, 1,401,794 source bytes, four complete material meshes, five distinct shader keys, four effects, seven textures and 62 decoded mips. All external present paths are read once. The original SWTS and base SSHT are both in the exact union; seven unselected frames and `texture/defaulttexture.dds` are absent.

| Zero-based frame | Selected DDS SHA-256 | Selected mip 0 RGBA SHA-256 |
| --- | --- | --- |
| 0 (`_01.dds`) | `6BA1EC54844D2614DCAF9C37441C7373D63786F2F3B0D43B953F9A0F15BCC938` | `68761BFA88EEB273A3063B69E228ECAA49E9F6FEAE28CC3C23D567C6B2FC8C70` |
| 1 (`_02.dds`) | `226A16D33CCFFE97EA9BA07B13F13D4B08C973006FC99C9959C670CCDE382ABD` | `AD3144AE7FD5B91573B4F7E820D1089DA1027F751CC0BC6FFD6C9D07D591E4FF` |

Both selected DDS files resolve at priority 34 and are 11,088 bytes with eight mips. Distinct source and pixel hashes establish that the explicit selection changes original texture input and output. No visual renderer comparison was performed.

Native scenarios (external historical reference), driven by the isolated Round 8 project (external historical reference), pass two original strict closures and 168 rejection cases, including every truncation of a synthetic SWTS fixture, malformed versions/layout/string/DTST extent, frame bounds, binding mismatch/dynamic/duplicate tags, path restrictions, pin mismatch, missing/masked dependencies, unsupported base families, source and decoded budgets, cancellation, strict/default rejection and the identical-byte alias. Explicit neutral cases retain geometry, and exact-limit fresh loading proves failed calls do not contaminate the next load. Synthetic bytes are labeled fixtures, not recovered original assets.

Independent Python verifier (external historical reference) uses read-only archive resolution and raw IFF parsing to reconstruct the full object/appearance/geometry/shader/effect/selected-texture source union. It checks each priority/size/SHA, raw SWTS and base binding, every returned mesh geometry hash and all 62 RGBA mip hashes per frame, absent unselected dependencies and byte-identical 47-byte decoded-create re-encoding. It neither calls C# nor exports asset bytes.

The unchanged Round 7 native regression and Python audit pass 84 rejections and retain the original default outcome: 24 sources / 1,389,982 bytes, two material meshes, two SWTS diagnostics. CPU dependency completeness is therefore optional and explicit; it is not protocol, executable-function or global asset-format coverage.

Reproduce with an existing offline .NET 8 environment and local Python:

```powershell
dotnet build poc/round8-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round8-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round8/native.txt
python tools/python/verify_switch_texture_static_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round8/native.txt
dotnet poc/round8-tests/Assets/bin/Debug/net8.0/Assets.dll --r7-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round8/r7-native.txt
python tools/python/verify_scene_static_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round8/r7-native.txt
```

## Open boundary and next probe

No input executable or virtual addresses were used. Prior RTTI correlation in the [EFCT checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_EFCT_PRECALCULATED_LIGHTING.md) does not establish a SWTS factory grammar or switcher semantics. Only original asset structure and the bounded modern selection are established here. The precise future probe is static executable analysis, after verifying its documented SHA-256, of SWTS/0000 registration and DTST/0000 construction/read/update ownership to determine the two raw words' meanings and initial frame rule. That separate investigation is outside this milestone.

## Round27 static runtime closure

[Pinned SWTS/DTST runtime](SWITCH_TEXTURE_RUNTIME_CONTRACT.md) establishes authored signed count, float interval, initial zero index/accumulator, repeated catch-up/wrap and per-wrapper selector ownership over template-owned variants. Terminal count8/float bits3DCCCCCD matches its eight ordered textures. Outer clock units and first update/draw/pause/culling remain Unknown. Explicit frame0 preview remains current policy; no10Hz or automatic host animation claim.
