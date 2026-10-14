# Original lower-detail single-color specular material

## Selected owned boundary (October1,2026)

Original explicit human mesh details2/3 resolve through object -> SAT -> MLOD -> selected body/head MGN to `shader/hum_m_body_sm.sht`, `effect/h_specmap.eft` and `texture/hum_m_sm.dds`. Both levels have zero tangent records and share one material. The existing reader succeeds with palette defaults, albedo factors and vertex-lit specular enabled, compressed normals disabled. No Core defect or production change was demonstrated. See the [lower-detail geometry contract](SKELETAL_LOWER_DETAIL_CONTRACT.md) and [Round3 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R3_LOWER_DETAIL_SHADING.md).

| Closure | Resources | Source bytes |
|---|---:|---:|
| Explicit detail2 shading | 22 | 183,408 |
| Explicit detail3 shading | 22 | 143,090 |

Compared with Round2 authored material defaults, each closure adds the skin palette, PS1.1 albedo inspection program, four pinned vertex-source/include nodes and PS20 nodot3 program: seven resources/23,245 bytes. Each successful source resolves externally once per bundle. MAIN and SPEC use the same owned decoded256x256 BC3 image and nine mips; the decoder resolves it once. These counts are separate from animation closures and research scores.

## Exact bindings, material and factors

The EFCT has five IMPL/0005 implementations/six PASS/0009 passes. Three one-pass programmable candidates bind:

| Vertex program under `vertex_program/` | Pixel program under `pixel_program/` |
|---|---|
| a_spec_pp_vs20_for_ps20.vsh | h_specmap_pp_ps20.psh |
| a_specmap_vs20_nodot3.vsh | h_specmap_ps20_nodot3.psh |
| a_specmap_vs11_for_ps11.vsh | h_specmap_ps11.psh |

Two further implementations use fixed vertex/pixel stages, with two passes and one pass respectively. The audit preserves all capability/option/phase/flag/pass records and fixed-stage metadata. Only the explicit second programmable candidate is executed in this CPU proof; hardware capability negotiation and runtime fallback selection remain Unknown.

The selected pass binds sampler0 MAIN and sampler1 SPEC, both to `texture/hum_m_sm.dds`. PTXM/0002 records contain stage/tag only; SSHT TXM/0001 records have11-byte DATA, preserving six raw sampler bytes `(0,0,0,2,2,2)` after tag/source flag. No anisotropy override byte is present in this version. Point texel fixtures do not emulate runtime filtering or addressing. TCSS maps MAIN to UV0. Pass factor0x40 and material0x7C are MAIN; factor0x44 is zero. Depth/test/opaque policy fields and all remaining raw pass fields agree independently.

MATS MAIN has ambient/diffuse RGBA `(1,1,1,1)`, emissive `(0,0,0,0)`, specular `(0.2,0.2,0.2,1)` and power20. These are independently decoded from original17-float packing. The authored `index_color_skin` palette factor is shared (raw private flag0), default index0, from `palette/pc_skin_hum.pal`; the palette-selected opaque ARGB is4293048239. The PS1.1 albedo inspection candidate supplies the single MAIN factor policy; there is no HUEB factor or MAIN-alpha hue interpolation in the selected PS20 family.

The existing [vertex source contract](SKELETAL_VERTEX_LIT_SOURCE_CONTRACT.md) pins the same VSH/include graph: four nodes/three edges/19,401 bytes. This is authored source identity, not generated vertex-bytecode/LIT precision.

## Source identities

| Source | Priority | Bytes | SHA-256 |
|---|---:|---:|---|
| shader/hum_m_body_sm.sht | 53 | 501 | 16471F99D8960C6D481606E477C397C3729B07299FF450AE5D8E760EA462178D |
| effect/h_specmap.eft | 40 | 1,744 | ED4CE5EADAC3CA080A5E21DF5BABC95739634C41228A0DCDFDA167E57A5E4C4C |
| texture/hum_m_sm.dds | 55 | 87,536 | 93C9063BBA30A4325B87FFC43C367B5F463967FF9ECD9942D4C8498393F7B51E |
| palette/pc_skin_hum.pal | 53 | 1,048 | 09443BDC0DE66A85D0DEB15813CCB9FCBFE6F5F8A6E417CF6D46DC910A494160 |
| pixel_program/h_specmap_ps11.psh | 40 | 1,124 | 4A3155359640270C2274495AFD1E8E68E6139CCBC358DDA14B66D50BAF8B340C |
| pixel_program/h_specmap_ps20_nodot3.psh | 40 | 1,672 | C7A5218B031D9C0CD80C33C7D31611CAC4AFD7A241C794354879070DFD7DDB85 |

PS20 complete PEXE hash is FCD1A420FB005FEC90FE3917139EB5E754E7C6A2488BD76B3B8C626AD0AF3905; instructions after comments hash D794150290AC181EEF093C4FD974832CFF55039DA606FE749C6BC4A8E94212F2. Original object/SAT/configuration pins are unchanged from the prior contract and enforced.

## Confirmed compiled pixel equations

For normalized source-encoding MAIN RGB `T`, palette factor RGB `F`, SPEC alpha `m`, supplied vertex diffuse `D` and supplied vertex specular `V`:

- `S = V * m`
- `RGB = T * F * D + S * (bloom ? 0.5 : 1)`
- `alpha = fade ? opacity : dot(S,(0.3,0.59,0.11)) * (bloom ? 0.65 : 1)`

MAIN/SPEC sample the same texture in these fixtures; alpha masks specular and does not blend hue. Bloom scales RGB specular, while luminance uses unscaled S. No final clamp is added. The existing compiled-token interpreter independently executes original PEXE for64 distinct original point texels with varied supplied colors and all four bloom/fade combinations, shared across both details. Max output component error8.869665502064805e-8 at1e-5 tolerance. Wrong unscaled-bloom RGB, red-channel mask, prematurely scaled alpha input and ignored fade differ by at least0.1717647,1.1336471,0.03894494 and0.992790588 in their strongest fixtures, respectively. These are fixture discriminators, not observed original scene colors.

## Rejection, fallback and ownership

Missing/masked shader, effect, texture, PS1.1/PS20 programs or pinned vertex/include sources reject. Changed selected PS20 instructions or pinned source bytes, oversized/truncated programs/sources, unsupported TFNS version, missing MAIN material tag, nonzero MAIN coordinate set, changed SPEC sampler tag, aggregate count/byte exhaustion and invalid factor/sample/mask/color/fade inputs reject. Missing/masked palette deliberately retains the existing diagnostic opaque-cyan fallback; malformed present palette rejects. An absent base MAIN TFNS entry can be supplied by the authored palette and is not itself invalid.

Clearing all resolver byte arrays preserves geometry hashes, decoded mip hashes, owned PEXE hash and evaluator results. Overlays dispose deterministically. Both closures fit explicit64-resource/16MiB limits; no limit or parser changes are needed.

## Reproduction and limits

Use [the isolated console](../../SWG-Godot-Client/poc/round3-tests/Assets/Assets.csproj) and [independent audit](../../SWG-Godot-Client/tools/python/verify_skeletal_lower_detail_shading.py), with task-specific APPDATA, existing offline package cache and UTF8 Python:

Prerequisites are the .NET8 SDK, cached BCnEncoder.Net2.3.0 and its locked dependencies (including CommunityToolkit.HighPerformance8.4.0), the pinned private original assets/configuration, and the existing Python audit dependencies. The commands below create the ignored task configuration; no pre-existing round3 configuration file is required.

```powershell
$env:APPDATA = Join-Path $env:TEMP "stardust-assets-round3-appdata"
$env:NUGET_PACKAGES = "C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages"
$env:PYTHONUTF8 = "1"
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round3 | Out-Null
"<configuration><packageSources><clear /></packageSources></configuration>" | Set-Content -Encoding utf8 local-private/round3/NuGet.Config
dotnet restore poc/round3-tests/Assets/Assets.csproj --configfile local-private/round3/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round3-tests/Assets --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round3-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round3/native.txt
python tools/python/verify_skeletal_lower_detail_shading.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round3/native.txt
```

Observation/confidence: original source structures/identities, material/factor/binding values and selected compiled pixel agreement are Confirmed. Supplied vertex colors are modern fixtures. Original compiled vertex LIT, hardware candidate selection/filtering, automatic LOD and GPU rendering remain Unknown or unrun. This milestone does not choose or integrate a renderer.
