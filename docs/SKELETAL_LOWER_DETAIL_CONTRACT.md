# Explicit lower skeletal mesh details and sampled deformation

## Confirmed original boundary (October1,2026)

The original human shared object resolves both body/head MLOD index0..3 paths explicitly, using the same pinned inherited object/SAT chain as the [Round1 contract](SKELETAL_LOD_SELECTION_CONTRACT.md). No production change was necessary. The implementation keeps both body/face skeleton selections at level0; this milestone does not recover automatic mesh/skeleton coupling, distance scheduling or runtime fallback. [Round2 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R2_LOWER_DETAILS.md) records baseline and reproduction.

| Selected detail | Sources | Source bytes | Positions | Weights | Normals | Tangents | Retained triangles | Materials/textures/mips |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| 0 | 29 | 4,271,530 | 1,454 | 2,170 | 1,518 | 1,442 | 2,810 | 4/10/80 |
| 1 | 29 | 4,167,914 | 968 | 1,382 | 1,064 | 1,094 | 1,904 | 4/10/80 |
| 2 | 15 | 160,163 | 386 | 542 | 440 | 0 | 756 | 1/1/9 |
| 3 | 15 | 119,845 | 101 | 103 | 115 | 0 | 190 | 1/1/9 |

The source-byte aggregates include authored material defaults. Details2/3 contain no tangent records and use a smaller material closure. Preserve that absence; do not infer a normal/tangent material family from detail0. Palette evaluation, face generation, optional choices and GPU shading remain separate.

Selected MGN files under `appearance/mesh/` (effective priority53):

| File | Bytes | SHA-256 |
|---|---:|---|
| hum_m_body_l0.mgn | 161,413 | 93378D74AA75B1C6673C9008914FFB51CF7DC380FF18459DBBFC26EF016A59B0 |
| hum_m_head_l0.mgn | 148,489 | 84DFF58013C8374D80D691549A67E009A84344649010C9A4365552C9FCE48F19 |
| hum_m_body_l1.mgn | 119,643 | D477E22985A49F7F2FBA51828351435D41F74E277A186AF177F54FFE5D86E863 |
| hum_m_head_l1.mgn | 86,643 | E544585B9E206E9537BAC02967722F67D3768595C9E2E82EEE3B189798C74D8A |
| hum_m_body_l2.mgn | 41,324 | 4227C5E64B5C490B4A2E0A7D106AEAB5A4F8E84715BDB0D6E7E210114A94EED3 |
| hum_m_head_l2.mgn | 9,664 | 3F4F4833DC4294D0F0EFBAE8322817357B1FAD4BF0BF1523E1DF92EDA9F6B860 |
| hum_m_body_l3.mgn | 7,869 | 6128DE248A97B3C57F89E2DA883DBF469B0CF1FBDF0EC53B77F5BDD3D62A49C9 |
| hum_m_head_l3.mgn | 2,801 | 309FFBE0DF0ED8D9AD23AF3F87B0B9C6CEA94221329A581840956CFDCED9E0F1 |

Independent effective archive bytes establish source priority/length/hash, inherited appearance selection, SMAT/MLOD paths, skin positions/names/ranges/ordered weights/normals, canonical geometry including triangle/tangent maps, scoped joint indices, authored shader/effect/texture references and every decoded mip hash. Every successful source resolves externally once per bundle. Material closures fit the existing64-source/16MiB defaults; explicit verification allowance128-source/32MiB is not a production limit change.

## Original movement samples with explicit level0 skeletons

The existing pinned human locomotion profile (external historical reference) selects `appearance/lat/all_m.lat` / `loop_standing`, with walk route[0,1] and run route[0,2]. Walk asset `appearance/animation/all_b_loc_walk_male.ans` has SHA-256 FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6; run `appearance/animation/all_b_loc_run.ans` has SHA-256 72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4. Both use30fps; tested frames are walk0,0.5,16.5,33 and run0,0.5,11,22.

For each detail1..3 the animation-only closure has17 sources: respectively635,870 /480,572 /440,254 bytes. This closure adds LATs, pinned idle and walk/run clips; it omits material defaults. The verifier checks the exact union and read-once counts separately from the material closure above.

Independent raw CKAT sampling uses the hash-verified original executable's static quaternion table, shortest-arc interpolation and translation channels. The oracle reuses existing skeleton/bind/matrix and route audit helpers, composes local/world matrices in double precision and computes each position using all original ordered weights. It compares1,272 joint matrix rows and11,640 weighted positions. Maximum matrix component error9.806433240999723e-7; maximum position component error5.277724678121043e-7, against2e-5 tolerance. Largest motion from bind0.6813496148476299 units. These are Confirmed replacement arithmetic agreements for these samples; they do not establish original execution/device precision.

`hold_l` remains at bind pose under the existing explicit missing-transform policy; `polearm1_polearm` and `l0` remain unused animation names. Attached face hierarchy follows its animated parent. Root locomotion translation, event dispatch, blends and GPU conversion are not applied.

## Rejection, ownership and reproduction

For details1..3, missing/masked selected mesh, skeleton, shader, effect or texture rejects; malformed selected MGN, unavailable singleton detail, invalid detail before lookup and exhausted aggregate budgets reject. Invalid sample frame, truncated palette, unresolved joint and missing/masked walk/run clip reject. Failed samples leave retained poses unchanged. Clearing all resolver byte arrays preserves prepared poses, geometry hashes and decoded texture mip hashes. Overlay handles use deterministic `using` disposal.

Round2 console (external historical reference) calls the new scenario; independent lower-detail verifier (external historical reference) extends the existing closure verifier through0..3. Round1 console/audit retain their default0/1 mode.

```powershell
dotnet restore poc/round2-tests/Assets/Assets.csproj --configfile local-private/round2/NuGet.Config -p:NuGetAudit=false
dotnet run --project poc/round2-tests/Assets --no-restore -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round2/native.txt
python tools/python/verify_skeletal_lower_details.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round2/native.txt
```

Use the existing offline package cache/task-specific APPDATA and UTF8 Python. Shared object/SAT/configuration pins are unchanged from Round1. The independent table reader verifies Stardust.exe SHA-256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before using addresses. Generated original-derived rows remain ignored. No original executable is run.

## Round3 integration follow-up

Selected shading closure is now independently verified in [the lower-detail shading contract](SKELETAL_LOWER_DETAIL_SHADING_CONTRACT.md); explicit GPU inspection is in [the detail preview contract](SKELETAL_DETAIL_PREVIEW_CONTRACT.md). Automatic LOD remains open.
