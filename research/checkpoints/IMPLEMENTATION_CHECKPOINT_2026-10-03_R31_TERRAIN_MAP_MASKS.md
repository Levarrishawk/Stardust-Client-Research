# Round31 terrain map inputs — 2026-10-03

## Question and stopping condition

Can the conceptual MAPS object's empty0000 and DATA/WMAP/SMAP0001 version forms become immutable bounded engine-neutral inputs, preserving raw metadata and both LSB-first grids? Stop after synthetic malformed/ownership/cap/empty/query gates, one pinned readonly Tatooine sample with independent raw mask/schema/bit oracle, and exact scope/link/whitespace review. No PTAT/TGEN generation, world-coordinate mapping, active SMAP inference or corpus sweep.

## Input identity and orientation

Round31 snapshot:2,754 public files; manifest SHA-256 `093a193c288ed5889c62317c57028a26f0afffa9c710b104a9c5f9bd2163b0fb`; ZIP `d80bb36fd1d71a63ff31d2eaf28a5d9dc7c90de197b5e0abd7bd2bf29f86dc70`. Full archive/member/size/hash/containment and archive/target/ancestor symlink/Windows-reparse preflight before listed-byte overlay, then all2,754 hashes verified. Private baseline preserved. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`, snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`; index/unlisted/private preserved.

Read START/latest accepted30/session31 assignment, current status/ledger/UNKNOWN, [terrain architecture](../../docs/WORLD_TERRAIN_ARCHITECTURE.md), [prior map evidence](ANALYSIS_CHECKPOINT_2026-09-12_PTAT_WMAP_SMAP.md), accepted IFF/overlay and existing raw schema tools/curated searches. No new address/registry/protocol metric claims; original executable is not required or run. Existing prior executable evidence supplies metadata/bit semantics; this round adds byte/implementation proof.

Readonly root `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

| Identity | Observation |
|---|---|
| Effective terrain path/priority | `terrain/tatooine.trn` /42 |
| Full bytes/SHA | 1,188,446 / `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427` |
| Direct map offset/bytes/SHA | 139,818 /1,048,628 / `D3F7B55C110D6454E328507743FA0CF4A126CD6D8A802DC040752543CC18AD9D` |
| World size/meters per bit | 16384.0 (`0x46800000`) /8.0 (`0x41000000`) |
| Bytes per row/bit dimension | 256 /2048 |
| WMAP bytes/SHA | 524,288 / `B893659B5BCE74ACFC851BF4613F6B25A8DEB9EC1DEE27D7103910376743F3FF` |
| SMAP bytes/SHA | 524,288 / `D14368C40FDAB9F37C197F9BA0F6C92E7AE236579AC6B3BF6D3FA0E14CB114D9` |

## Scope and API

Seven additions only: [decoder](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/TerrainMapMasks.cs), [native scenarios](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainMapMasksScenarios.cs), [durable contract](../../docs/TERRAIN_MAP_MASKS_CONTRACT.md), this checkpoint, [project](../../../SWG-Godot-Client/poc/round31-tests/Assets/Assets.csproj), [entry point](../../../SWG-Godot-Client/poc/round31-tests/Assets/Program.cs), [fixture producer/oracle](../../../SWG-Godot-Client/poc/round31-tests/Assets/verify_masks.py). No existing production/shared Program/global/world/Godot changes. Lead owns historical framing reconciliation.

`TerrainMapMasks.Decode(ReadOnlySpan<byte> versionForm,TerrainMapMaskLimits? limits=null)` exposes get-only version/empty/raw float bits/float values/U32 row/dimension/mask length, cloned mask/Data inspection bytes, and bounded raw integer-grid `GetBit`/inclusive clipped `AnyBit`. `TerrainMapMaskKind` distinguishes Wmap/Smap. Exact ranges/errors/query policy are in the contract. No float-coordinate mapping or mutation API is added.

## Findings versus interpretation

**Observation / Confirmed original bytes:** PTAT has one0014 form whose relevant children are TGEN then a direct0001 form containing DATA/WMAP/SMAP. There is no literal MAPS wrapper. The initial probe followed the old `FORM MAPS` diagram and failed to find one; direct child inspection established the actual version-form extent. Lead accepted this framing correction and will append the historical/global correction. The conceptual object name remains MAPS; decoder rejects an unsupported literal wrapper.

**Prior evidence / Confirmed:** two little-endian F32 and two U32 fields, LSB-first row addressing, empty0000 and opaque dual masks are closed by prior executable/asset evidence. This task does not reanalyze VAs or implement the old checkpoint's world-coordinate mapping. Active SMAP use remains unobserved.

**Modern implementation / Confirmed:** finite positive floats, positive bounded row/dimension, checked row capacity/multiplication, exact two lengths, IFF node/depth/input ceilings and strict tag/version/order/tail rejection. These stricter modern eligibility rules are not original parser rejection claims. Raw float bits/padded mask bytes remain unchanged. No relationship between worldSize/metersPerBit and dimension is invented.

**Ownership / Confirmed:** accepted IFF parser snapshots input; decoder retains private arrays only. Source clearing, mask-copy clearing and Data-copy clearing preserve state; independent clones/get-only metadata prevent mutation. Empty copies/queries behave deterministically. Logical caps do not estimate peak parser/copy/CLR memory.

**Queries / Confirmed modern policy:** raw grid point outside returns false. Rectangle corners normalize, clip, and preflight all clipped cells before searching; early hit does not bypass work cap. Extreme signed corners are safe after clipping. No centered/world-unit/negative float mapping or active gameplay query claim follows.

**Compatibility consequence:** retain both map inputs faithfully for a future terrain implementation; a reviewed owner can inspect raw grid bits without a renderer. Metadata/world conversion, PTAT DATA, procedural rules, topology-to-cell mapping and runtime consumers remain separate boundaries.

## Validation and reproduction

Final cached offline .NET8 build:zero warnings/errors. Native and independent Python PASS:143 fixtures (five accepted/138 rejected),18 grid points,14 normalized/clipped rectangles, five query rejections, four ownership checks. Covers exact/one-less caps, empty/zero-mask ceilings, positive subnormals, single/padded/non-byte-aligned grids, nonfinite/nonpositive fields, zero/high-bit/impossible dimensions, bad metadata/mask extents, tags/order/forms/versions/extra/tail, all92 truncated prefixes and invalid settings. One original effective sample only; archive closes and original input buffers clear before returned mask receipt. Independent raw IFF extents/metadata/mask SHA and sampled points match native results. Detailed original sample receipts remain ignored, not proprietary fixtures in public data.

The initial literal MAPS-wrapper probe failure led to the evidence-backed framing correction above. Implementation native/oracle gates passed on their first run; final receipt adds explicit original float values alongside raw words and passes again. No additional corpus or optional expanded test campaign.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round31-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
$python='C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
# Private nuget/NuGet.Config clears packageSources; cached inputs only.
& $python poc/round31-tests/Assets/verify_masks.py generate local-private/round31/fixtures.json
dotnet restore poc/round31-tests/Assets/Assets.csproj --configfile local-private/round31/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round31-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round31-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round31/fixtures.json > local-private/round31/native.txt
& $python poc/round31-tests/Assets/verify_masks.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round31/native.txt > local-private/round31/python-audit.json
& $python tools/python/validate_organized_workspace.py .
```

Private task logs/receipts: original-probe.json, native.txt, python-audit.json, fixtures.json, baseline.json, changed-files.json, baseline-relative.diff, scope-review.json, workspace-validation.txt, git-status.txt. Final scope verifies seven additions/zero existing edits, all2,754 baseline files unchanged, HEAD/empty index preserved, private/build ignored, clean whitespace/EOF and zero broken/private links. No stage/commit/push or original execution/live contact/download occurs.

## Exact next action

Lead replay/acceptance and historical MAPS diagram correction. Then a separately bounded PTAT DATA/procedural-input milestone may consume these immutable masks. Do not infer world mapping, active SMAP use or playable outdoor terrain from this decoder. Final overnight worker milestone is stopped here.
