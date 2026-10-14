# Explicit original human skeletal detail selection

## Verified boundary (2026-10-01)

`CreatureAppearanceDescriptorBundleReader.Load(meshDetail:1)` follows the pinned shared human male object inheritance to SMAT/0003, both MLOD/0000 generators, serialized/exposed index1 MGN files, owned skeletons, skin prefixes, bind geometry and authored material defaults. This is explicit selection; it does not implement a distance selector, skip-L0 configuration, runtime fallback or a GPU preview. See the [descriptor contract](SKELETAL_APPEARANCE_DESCRIPTOR_CONTRACT.md), [skin contract](SKELETAL_MESH_SKIN_SOURCE_CONTRACT.md) and [Round 1 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R1_SKELETAL_LOD.md).

Observed original-byte and native results agree independently (Confirmed):

| Measure | Detail0 | Detail1 |
|---|---:|---:|
| Sources | 29 | 29 |
| Source bytes | 4,271,530 | 4,167,914 |
| Positions | 1,454 | 968 |
| Transform weights | 2,170 | 1,382 |
| Normals | 1,518 | 1,064 |
| Tangents | 1,442 | 1,094 |
| Retained triangles | 2,810 | 1,904 |
| Materials / textures / decoded mips | 4 / 10 / 80 | 4 / 10 / 80 |

Selected source identities:

| MGN suffix under `appearance/mesh/` | SHA-256 |
|---|---|
| hum_m_body_l0.mgn | 93378D74AA75B1C6673C9008914FFB51CF7DC380FF18459DBBFC26EF016A59B0 |
| hum_m_head_l0.mgn | 84DFF58013C8374D80D691549A67E009A84344649010C9A4365552C9FCE48F19 |
| hum_m_body_l1.mgn | D477E22985A49F7F2FBA51828351435D41F74E277A186AF177F54FFE5D86E863 |
| hum_m_head_l1.mgn | E544585B9E206E9537BAC02967722F67D3768595C9E2E82EEE3B189798C74D8A |

The independent audit reconstructs inheritance and SAT/MLOD selection from effective archive bytes, checks every source priority/length/hash, skin-prefix hashes (positions, names, ordered ranges/weights, normals), complete canonical geometry hashes (including tangent records/maps and triangles), scoped joint mappings and authored shader/texture references plus decoded mip hashes. The exact source union contains only inherited object templates, SAT, MLODs, selected MGNs, skeletons, shaders, effects and named authored textures. Each successful source resolves externally once per load.

## Policy and compatibility consequence

The verification deliberately keeps both skeleton selections at level0 while changing mesh detail; `meshDetail` does not select skeleton detail. Native bind palettes preserve positions within 1e-5. This identity check validates the modern bind/deformation implementation, not legacy runtime animation or automatic shared skeleton-detail policy.

The harness permits 128 sources/32 MiB explicitly, then separately verifies the same detail1 closure using the reader defaults (64 sources/16 MiB). This bounded authored-material closure fits the defaults; no production limit increase is needed. Decoded textures have their existing separate decoder budgets. Palette evaluation, all texture choices, generated face resources, compiled shader programs, animation and rendering are outside this closure.

Missing or masked selected MGN, skeleton, shader, effect or texture rejects. Malformed selected MGN, unavailable detail1 in a singleton MLOD and exhausted aggregate source-count/byte budgets reject. Out-of-range detail (-1 or4) rejects before lookup. A successful bundle survives clearing all resolver-owned byte arrays, including geometry and decoded pixels. No reader defect was demonstrated, so production code is unchanged. Preserve explicit detail requests and fail strictly rather than choosing detail0 implicitly.

## Reproduction

Use [the isolated console project](../../SWG-Godot-Client/poc/round1-tests/Assets/Assets.csproj) and [the independent audit](../../SWG-Godot-Client/tools/python/verify_skeletal_lod_selection.py). The project references Core and links existing smoke helpers, leaving shared runners unchanged.

```powershell
# Use an existing offline NuGet package cache and task-specific APPDATA.
dotnet restore poc/round1-tests/Assets/Assets.csproj --configfile local-private/round1/NuGet.Config -p:NuGetAudit=false
dotnet run --project poc/round1-tests/Assets --no-restore -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round1/native.txt
python tools/python/verify_skeletal_lod_selection.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round1/native.txt
```

Input pins: object E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243; SAT 951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C; configuration 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. These pins are enforced by both verification paths. No executable addresses are used.

## Remaining details and sampled deformation

[Round 2 lower-detail contract](SKELETAL_LOWER_DETAIL_CONTRACT.md) independently closes details2/3 and samples movement poses at details1..3 with explicit level0 skeletons. Detail2:386 positions/756 triangles;detail3:101 positions/190 triangles. Both have15 source identities and no tangents. The Round1 default verification mode remains0/1.
