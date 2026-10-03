# Round28 SWTS frame input bundle — 2026-10-03 UTC

## Question and stopping condition

Can every authored frame of the pinned mission SWTS become an immutable bounded CPU input bundle with exact path/source/mip accounting, without loading overwritten defaults/effects or depending on world/engine/cursor? Stop after synthetic positive/negative/ownership/alias gates, ten pinned original inputs/all64 mip hashes, prior switch/DDS regressions and exact scope checks.

Orientation: START/latest accepted27/session28, status/ledger/UNKNOWN and updated SWTS terminology, accepted switch static/runtime contracts/checkpoints, existing SWTS/SSHT/DDS/cache/readers and Python byte/pixel verifiers. Actual static reference decoder is the accepted `StaticShaderReferences` class. No new executable input/VA claims, downloads, live contact or original execution.

## Input identity and scope

Authoritative Round28 snapshot:2,666 public files; manifest SHA-256 `c6945d7365f1c6d2b3585aebeec57d10a29fb9c0fcbd7b0e586a0d58125ab654`; ZIP `e0eb172e1b2429de49c55857bc14baa0cbe75f5aa825bfcba88762b8483ca011`. Complete unique member set, all size/hash and archive identity, lexical/resolved containment and archive/target/ancestor symlink/Windows-reparse absence preflighted before overlay. All member hashes reverified. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`, snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`, empty index and unlisted/private files preserved.

Readonly root `C:/Stardust-DEV`; configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Exact original standalone bundle input union:

| Path | Priority | Bytes | SHA-256 |
|---|---:|---:|---|
| `shader/anim_mission_terminal.sht` | 38 | 475 | `B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62` |
| `shader/defaultemis.sht` | 38 | 171 | `BD33741659BD3C322AE37DC8C0F4DDADBE940502DB273538DB6A34A9FE14D5F7` |
| `texture/anim_mission_terminal_01.dds` | 34 | 11088 | `6BA1EC54844D2614DCAF9C37441C7373D63786F2F3B0D43B953F9A0F15BCC938` |
| `texture/anim_mission_terminal_02.dds` | 34 | 11088 | `226A16D33CCFFE97EA9BA07B13F13D4B08C973006FC99C9959C670CCDE382ABD` |
| `texture/anim_mission_terminal_03.dds` | 34 | 11088 | `87D602AF5538E04B55B71570F3736AAE4782C953A6E6F7D2CD2BBD3CE60C7B95` |
| `texture/anim_mission_terminal_04.dds` | 34 | 11088 | `347572A7A980F2A29775300C2D68AD72986C7E3264E6347B2E769D49B807C897` |
| `texture/anim_mission_terminal_05.dds` | 34 | 11088 | `829D371B0AFE48C607F2CA4AD0CD0A9B1E46372364896637072491A47B1A5886` |
| `texture/anim_mission_terminal_06.dds` | 34 | 11088 | `698E795675D992628CB5F92441E5508057AA7F479EF9BA2DA5DBDBE0CD27716C` |
| `texture/anim_mission_terminal_07.dds` | 34 | 11088 | `913AB9059C0F1EEF20F5EC4573F564490418ADAE517742B117AC52736F588E95` |
| `texture/anim_mission_terminal_08.dds` | 34 | 11088 | `828EB4C1CB4590F9BDFD45992BA117B63D323791AC3DE1E8560A3FB032B36652` |

Ten sources /89,350 bytes, eight ordered MAIN frames, DTST words8/`0x3DCCCCCD`. BC2 frames each128x64 at mip0/eight mips/43,692 RGBA bytes; totals64 mips/349,536 bytes. Shader/base/count/raw-binding identities agree independently. All original-derived detailed mip hashes/receipts stay private; no assets or proprietary pixel corpus are promoted. No overwritten default or effect path is read by this bundle. Prior material regression's larger27-source per-selected-frame union is separate.



| Frame | Exact authored path | Mip0 dimensions | Mips | RGBA mip bytes |
|---:|---|---:|---:|---:|
| 0 | `texture/anim_mission_terminal_01.dds` | 128x64 | 8 | 43,692 |
| 1 | `texture/anim_mission_terminal_02.dds` | 128x64 | 8 | 43,692 |
| 2 | `texture/anim_mission_terminal_03.dds` | 128x64 | 8 | 43,692 |
| 3 | `texture/anim_mission_terminal_04.dds` | 128x64 | 8 | 43,692 |
| 4 | `texture/anim_mission_terminal_05.dds` | 128x64 | 8 | 43,692 |
| 5 | `texture/anim_mission_terminal_06.dds` | 128x64 | 8 | 43,692 |
| 6 | `texture/anim_mission_terminal_07.dds` | 128x64 | 8 | 43,692 |
| 7 | `texture/anim_mission_terminal_08.dds` | 128x64 | 8 | 43,692 |

Each row is computed from its independently decoded mip records, rather than inferred from the aggregate. Each chain is 128x64, 64x32, 32x16, 16x8, 8x4, 4x2, 2x1, 1x1; byte counts are 32,768 + 8,192 + 2,048 + 512 + 128 + 32 + 8 + 4 = 43,692. No terminal bytes are retained. The unchanged source identities and current native/independent per-mip agreement establish these sizes.

Seven additions: [bundle](../../poc/Stardust.Core/Assets/SwitchTextureFrameBundle.cs), [scenario](../../poc/Stardust.Core.SmokeTests/SwitchTextureFrameBundleScenarios.cs), [contract](../../docs/SWITCH_TEXTURE_FRAME_BUNDLE_CONTRACT.md), this checkpoint, [project](../../poc/round28-tests/Assets/Assets.csproj), [entry point](../../poc/round28-tests/Assets/Program.cs), [oracle](../../poc/round28-tests/Assets/verify_frame_bundle.py). No existing decoder/loader/world/Godot/frozen/shared Program/ledger edits; lead owns stage integration.

## Observations, policy and compatibility consequence

**Observation / Confirmed:** original SWTS/base grammar and eight references match prior evidence. Header bytes identify BC2; the initial Python probe's assumed BC1 assertion failed and was replaced by FourCC-derived format selection before native implementation verification. Independent pixel decoding confirms all64 hashes. The standalone result's exact union is shader/base/unique authored DDS only.

**Prior runtime evidence / Confirmed:** signed authored count and float interval, local initially zero selector and repeated update/wrap behavior are established in [Round27 runtime](../../docs/SWITCH_TEXTURE_RUNTIME_CONTRACT.md). This task reuses that durable conclusion without new VA analysis. Count/reference equality and finite positive interval are not recovered original reader rejection rules.

**Implementation / Confirmed modern policy:** require positive signed count equal to actual references and finite positive interval. Validate every raw whole path before legacy normalization can alter it, preflight complete unique-source count before base lookup, and validate every selected tag/named source-flag-zero base binding before DDS reads. Load each unique present path once; repeated paths share immutable image/charge, equal-byte distinct paths remain separate. Base descriptions are metadata, not complete scene/material closure.

**Ownership/accounting / Confirmed:** new private-array mip/image wrappers prevent caller mutation through existing DDS ReadOnlyMemory exposure. CopyImage and copied shader descriptions return independently mutable copies. Ordered frames preserve raw tags/words and canonical authored path spelling. Unique decoded bytes include retained terminal tail; mip count is unique-path count. Remaining decode budgets narrow DdsReadLimits before decoding, header count preflights remaining mip budget, and tail charge is checked before immutable retention. No partial bundle is published after late input/budget/cancel/callback/reentry failure. Existing cache handles source snapshots and aggregate bounds; callbacks/effects outside the method are not rolled back.

**Compatibility consequence:** use this bundle as an explicit engine-neutral frame input stage. A future adapter must separately validate/materialize runtime texture resources and join an independent cursor/clock policy. No scheduler/time-unit/x87 precision or first-visible-frame behavior is claimed here. No residual arithmetic is implemented or clamped in this stage.

## Validation and reproduction

Offline cached .NET8 restore/build, task APPDATA, cleared private NuGet sources. Final build zero warnings/errors. Synthetic corpus210 fixtures:8 positive/202 rejected. Exact positive budget is2 frames/4 sources/544 source bytes/16 decoded bytes/3 mips/144 shader bytes; per-DDS dimension2/input140/decoded12. Six bundle and three DDS one-less budgets, retained-tail exact184/one-less183, positive subnormal interval, different tags, alias and equal-byte distinct identity,1,024-character frame path, invalid count/interval/whole paths/strings/bindings, missing/masked/pin errors, late DDS/base truncation/tail and all128 truncated SWTS prefixes pass.

Fourteen controlled checks cover aliases, distinct equal-byte images, borrowed-buffer clearing/reuse, pixel/image/metadata copy mutation, read-only collections, later callback exception identity, unswallowed/swallowed reentry, pre/first/last cancellation, guard recovery/prior-result stability and independent callback thread. Thirty-two concurrent loads pass. Native original gate pins each of ten priority/size/SHA identities and exact budgets; independent configured-overlay/IFF/Pillow decoder checks every source/ordered frame/raw word and64 mip hashes. Archives close and caller source arrays clear before receipt emission.

Existing DDS gates pass opaque BGR, BC1/2/3/BGRA, BC7, typed DX10/ATI, ownership/mip/malformed/unsupported policies. Prior switch material regression passes168 rejections and two selected original closures; independent original source-union/create/mip audit passes. No GPU or scene/clock integration is performed by this milestone.

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round28-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
# Private nuget/NuGet.Config: <configuration><packageSources><clear/></packageSources></configuration>
& $python poc/round28-tests/Assets/verify_frame_bundle.py probe C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round28/original-probe.json
& $python poc/round28-tests/Assets/verify_frame_bundle.py generate local-private/round28/fixtures.json
dotnet restore poc/round28-tests/Assets/Assets.csproj --configfile local-private/round28/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round28-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round28-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round28/fixtures.json > local-private/round28/native.txt
& $python poc/round28-tests/Assets/verify_frame_bundle.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round28/native.txt
dotnet poc/round28-tests/Assets/bin/Debug/net8.0/Assets.dll dds-regression
dotnet poc/round28-tests/Assets/bin/Debug/net8.0/Assets.dll switch-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round28/switch-regression.txt
& $python tools/python/verify_switch_texture_static_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round28/switch-regression.txt
& $python tools/python/validate_organized_workspace.py .
```

Final scope/link/whitespace/EOF review verifies exactly seven additions, all2,666 baseline files unchanged, unchanged HEAD/empty index, frozen APIs preserved, ignored private/build output and zero broken/private links. No original assets/raw pixel corpus are staged or curated.

## Resume point

Standalone immutable frame inputs are complete. Next join a reviewed bounded cursor and explicit adapter outside this scope; base material/effect completion and world publication remain separate. Preserve raw interval bits and avoid assuming persistent float32 residual is always below interval in the separate cursor implementation. Original numeric/outer-clock/rendering parity requires its own evidence.
