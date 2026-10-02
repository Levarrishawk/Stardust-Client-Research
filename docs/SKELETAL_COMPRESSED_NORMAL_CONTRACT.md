# Skeletal compressed-normal boundary

## Owned program/material policy

`loadCompressedNormals` requires validated albedo factors. The reader selects exactly one `_cbmp_ps20.psh` inspection candidate for each material with CNRM, verifies opaque depth/factor/stage0 MAIN/stage1 CNRM identities and NRML UV0, and requires legacy BC3. Materials without CNRM keep an explicit geometric-normal fallback. This selected family is not recovered hardware capability negotiation.

The owned PSHP0000 decoder validates PSRC/PEXE, PS2.0 header and bounded leading COMMENT extents. After comments, complete instruction hashes identify the two inspected programs; altered instructions reject. No copied program corpus is embedded. Recognized program semantics remain separate from full original lighting translation.

| Original program | Source SHA256 | Instruction SHA256 after comments |
|---|---|---|
| h_color2_specmap_cbmp_ps20.psh | AA120FB501E6BADCF9573A736AACD152AC8EA48A12F9240A042086FB7A96B36F | B53EC0E8F576B033748323AAB8F01C355F3300D6EB51217765179FBD796B39E0 |
| h_specmap_cbmp_ps20.psh | 9EC70AA5D493564432CFB876144881493101002F0726DC98C4B0E62F66C1ED19 | 3DA984253F860A45C6B0915FDAD4DE9DDB4685EC45FDDDD8B0E4108EF5F0B0B3 |

## Sample and tangent semantics

The compiled normal block samples alpha for X and green for Y, subtracts0.5 and doubles, forms1−X²−Y², then RSQ/RCP reconstructs Z and NRM normalizes XYZ. **Confirmed:** exact operand swizzles, negate modifier and operation sequence independently decoded from original PEXE. RSQ takes the absolute value of its input, so the inspected program reconstructs `sqrt(abs(1−X²−Y²))`; source HLSL alone writes sqrt without that absolute operation. The compiled behavior is authoritative for this inspection family. [Microsoft RSQ reference](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/rsq---ps), [instruction token widths](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/instruction-token) and [NRM semantics](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/nrm---ps) support opcode interpretation.

`SkeletalCompressedNormalEvaluator.Decode` consumes finite normalized filtered RGBA and applies this math under float32 modern policy. Red/blue are ignored by normal reconstruction. Filtering precedes decoding. Driver precision/bit identity is unproven.

Original vertex include builds T from DOT3.xyz, N from the vertex normal and B=`cross(N,T)*DOT3.w`; object-space basis does not normalize these inputs. The independently authored `ToObject` preserves this raw basis. Source include priority40/12,318 bytes/SHA CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15 establishes this **source observation**; complete original runtime vertex compilation remains outside the claim. Shared helper priority40/2,398 bytes/SHA A9E9C7A2E6D41545BDBD0F08A57F14FD4D5C39899788F7FB94B510B20B85B09A corroborates channel selection. Z reflection requires negating tangent handedness to preserve the reflected B direction; synthetic nontrivial sign fixtures pass. Godot packing may normalize directions at its explicit adapter boundary.

## Original occurrence and verification

Three normal materials/body/head/face, two PS20 programs and six maps (body/head plus four age choices) are owned over42 sources/5,728,471 bytes. All13 textures/110 mips, selection/factor/source audits agree.852 sampled normal vectors independently agree at max2.548806524449443e-7 with2e-6 tolerance.78 original decoded texels lie outside the unit disk; per-mip selected outside-disk cases are included. This compiled/source difference therefore occurs in the selected corpus.

Native `--skeletal-compressed-normals` uses existing pinned object/SAT/configuration arguments. `verify_skeletal_descriptors.py` invokes independent stage/instruction/Pillow/sample checks. Every original program truncated prefix, altered instruction, missing/masked source and portable sample/program/option/tangent fixtures pass. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_COMPRESSED_NORMALS.md). [Normal diffuse and live detail adapter](SKELETAL_NORMAL_DIFFUSE_PREVIEW_CONTRACT.md) now verifies reflected tangent GPU upload, diagnostic diffuse shading and live age selection; original specular/bloom/fog remain separate.
