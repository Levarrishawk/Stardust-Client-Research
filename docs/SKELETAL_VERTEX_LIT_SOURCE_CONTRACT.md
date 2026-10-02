# Selected mouth vertex source and color outputs

R1-RENDER-01 closes the authored source boundary for the selected male human mouth. The compiled pixel equations remain in [the pixel contract](SKELETAL_VERTEX_LIT_SPECULAR_CONTRACT.md). This contract does not claim original generated vertex-bytecode identity or device precision.

## Original binding and complete include graph

The effective `shader/hum_m_mouth.sht` (priority53,577 bytes) names `effect/h_color2_specmap.eft` (priority40,2207 bytes). The unique one-pass candidate for `pixel_program/h_color2_specmap_ps20_nodot3.psh` has PVSH0000 naming `vertex_program/a_specmap_vs20_nodot3.vsh`. MAIN is UV0; MAIN material is ambient/diffuse/specular white, emissive zero and power300. This is the selected inspection candidate, not recovered capability negotiation.

| Source | Priority | Bytes | SHA256 |
|---|---:|---:|---|
| vertex_program/a_specmap_vs20_nodot3.vsh | 52 | 1346 | AD5D1F09358DA7B9F20A82E126538297C333A502E291CB9F0DD005C9B6B5E408 |
| vertex_program/include/functions.inc | 40 | 12318 | CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15 |
| vertex_program/include/vertex_shader_constants.inc | 40 | 3339 | C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9 |
| shared_program/functions.inc | 40 | 2398 | A9E9C7A2E6D41545BDBD0F08A57F14FD4D5C39899788F7FB94B510B20B85B09A |

The VSH includes constants and vertex functions by logical root path. Vertex functions include `../../shared_program/functions.inc`. Independent traversal verifies four nodes, three edges and19401 bytes, with no further includes or cycle. The three include paths/identities are shared with [the tangent family](TANGENT_LIGHTING_VARYING_CONTRACT.md).

**Observation/confidence:** Confirmed effective asset bytes, PVSH binding, include graph and source expressions. **Interpretation:** these are compiler inputs for the selected authored family; generated instructions remain Unknown. **Consequence:** retain bounded exact identities through the existing shared source cache rather than retaining original source in public fixtures.

## Color staging and channel roles

The VSH declares RGB diffuse COLOR0 and RGB specular COLOR1. It calls `calculateDiffuseSpecularLighting(false, position, normal)`. Vertex functions start diffuse with material emissive and specular with zero. With false, the primary `parallelSpecular[0]` contributes both diffuse and specular; it is not reserved for a later mouth dot3 pixel calculation. The two `parallel[]` slots contribute only diffuse. The one `pointSpecular[0]` contributes both channels; four `point[]` slots contribute only diffuse.

Directional and point specular helpers normalize the world normal, viewer and half vector, then call HLSL `lit(N.L,N.H,materialPower)`. Point helpers use the positive-distance reciprocal coefficient sum, with the backend fourth coefficient zero. World position/normal transformation precedes this lighting. The mouth VSH then adds light ambient to the diffuse sum and multiplies the complete specular sum by material specular RGB. Material ambient and diffuse colors are not multipliers in this selected expression. No output saturation appears in the VSH. Later COLOR input saturation belongs to [the pixel input boundary](LEGACY_PIXEL_COLOR_INPUT_CONTRACT.md), before the already closed mouth pixel/mask/hue equations.

**Observation/confidence:** Confirmed source order and channels, not compiled float evaluation order. **Consequence:** ambient and emissive must not leak into specular; diffuse-only slots must not acquire specular; the primary directional must participate at the mouth vertex stage. Pixel texture hue/mask and material specular remain distinct factors.

## Owned implementation and modern CPU policy

`SkeletalVertexLitLightingSources.Load` validates the unique supported pixel/PVSH binding, rejects precalculated lighting and verifies all four bounded source hashes. `SkeletalVertexLitSpecularReader` loads this identity through its existing cache and retains it in `VertexSources`. No resolver or source payload is retained by the identity object; its dictionary is read-only. Shared cache ownership, budgets and read-once semantics are reused.

`SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower` accepts world position/normal/viewer plus an owned material and recovered `CellLightingPreview` slots. It reuses existing slot/channel selection, primary inclusion and positive point attenuation, adds material emissive to diffuse, and preserves separate RGB channels. It rejects ordinary diagnostic selection, nonfinite material/inputs and zero normals. Existing finite-zero normalization, coincident/zero-denominator point omission, nonnegative coefficients and power range0..1024 are modern bounded policies. Multiplication/addition grouping follows the shared CPU helper, so this is algebraic source staging, not a recovered F32 instruction sequence.

Literal positive power300 is **modern**. Original compiled HLSL LIT exponent limits, normalize-zero, rounding, register interpolation and device precision remain **UNKNOWN**. No GPU production change is made. The existing diagnostic mouth adapter is unchanged; adopting this CPU policy in a GPU path requires a separately reviewed milestone.

## Reproduction and acceptance

The owned console is `poc/round1-tests/Rendering/Rendering.csproj`; it references Core and links the new scenario without modifying shared runners. No arguments run `RunMath()` synthetic checks. Explicit archive root/configuration arguments add original binding and source gates. The original overlay is deterministically disposed. Original bytes are never saved by the permanent verifier.

After offline restore, build with `dotnet build poc/round1-tests/Rendering/Rendering.csproj --artifacts-path local-private/round1/build --no-restore -p:UseSharedCompilation=false`. Run its built Rendering.dll with `C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg`, saving the compact output to an ignored native log. Audit with `python tools/python/verify_skeletal_vertex_lit_sources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg <native-log>`.

Original configuration SHA256 is22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. The verifier reuses the existing raw TRE index and IFF readers, resolves descending configured priorities and checks the raw SHT/EFCT/MATL/TCSS binding, includes and source expressions independently. Source missing/masked/empty/truncated/changed/oversized gates, duplicate/incorrect PVSH and precalculated-effect gates pass. Hand calculations distinguish both directional/point slot channels, ambient/emissive placement, unused material channels, baked suppression, back-facing lighting, oblique exponent2 and literal300, coincident-point policy and caller-array ownership. Independent double-precision evaluation agrees for64 supplied synthetic world cases at maximum3.4835412372480334e-6, tolerance1e-5; this tolerance measures modern CPU agreement only.

## Lead integration boundary

The new reader adds one1346-byte source to a full union that already contains the three includes. Relative to the latest full interior159-source/16642134-byte baseline, the expected union is160 sources/16643480 bytes. These full-scene totals require lead producer/union verification; the worker console verifies the19401-byte family closure directly. Earlier dated totals remain historical.

The lead owns shared smoke registration, original producer output and union-verifier updates, acceptance/status/ledger/session navigation and existing pixel/tangent contract cross-links. Register synthetic `RunMath()` independently from original `Run(resolve,effect)`, emit retained `VertexSources` in the original producer, and include the mouth VSH in the original expected source union. Preserve all previous count/hash/byte gates. No metrics, address names or packet registries change.

## Replacement GPU channel verification

[Round 2 mouth GPU contract](SKELETAL_MOUTH_LIGHTING_GPU_CONTRACT.md) tests the existing source-shaped vertex adapter over216 separate-channel/combined-pixel comparisons. It detects omitted primary, omitted diffuse slots, wrong specular channels, omitted emissive and late fragment recomputation. Original compiled LIT/device precision remains unknown; no production shader equation changed.
