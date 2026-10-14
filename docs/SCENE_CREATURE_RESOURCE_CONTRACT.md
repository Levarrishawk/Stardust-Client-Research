# Scene create to owned creature resources

R5-ASSETS-01, October 2, 2026. This engine-neutral boundary connects a decoded `SceneCreateObjectByCrcMessage` to a caller-supplied pinned resource recipe. The catalog is modern bounded configuration. It is not a recovered legacy global CRC catalog, and the fixture create is synthetic with a real original-template path checksum.

## Catalog and selection

ObjectTemplateCatalog (external historical reference) snapshots entries into a read-only dictionary keyed by U32 template CRC. Each entry carries an object path, explicit root/appearance SHA-256 pins, and the supported `CreatureDetail0` or `StaticAppearance` recipe; the latter uses the [static resource reader](SCENE_STATIC_RESOURCE_CONTRACT.md). Strings/scalars are immutable; modifying the supplied collection or creating a changed record cannot alter the catalog.

Paths normalize through the existing `LegacyAssetPath.Normalize`, then must have the `object/` prefix, `.iff` suffix, a nonempty basename, and no remaining `.`/`..` segments. Legacy normalization also strips leading slash/relative prefixes, folds ASCII case, changes backslashes and collapses repeated separators. This is a logical archive path, not an operating-system file path. The supplied CRC must equal `LegacyAssetPath.Checksum` of that normalized path. Pins require 64 hex digits and normalize to uppercase. Invalid pins, unsupported recipes, conflicting/duplicate normalized paths and duplicate/colliding CRCs reject construction atomically. Distinct correct-checksum paths with the same CRC cannot share one catalog.

Default catalog budgets are 256 entries and 262,144 normalized path characters. Caller limits must be positive, at most 4,096 entries / 4,194,304 characters. Individual paths retain the existing 1,024-character printable ASCII bound. Empty catalogs are legal; every lookup then rejects. `Require(crc)` throws for an unknown CRC. There is no default human substitution, arbitrary path lookup or reverse-checksum search.

## Reader and ownership

SceneCreatureResourceReader.Load (external historical reference) accepts the decoded create, catalog, explicit idle selection, exact walk/run bank, resolver and cancellation token. It rejects the static recipe before lookup. The synchronous operation validates resource limits, finite create floats, catalog membership and every animation selection before any resolver call. LAT paths must normalize beneath `appearance/lat/`, end in `.lat`, and retain no traversal segments. Explicit routes are snapshotted, limited to eight indices, each 0..4,095. Clip SHA pins are mandatory. The existing logical-name and counted-route validation remains authoritative.

The reader calls the unchanged [creature descriptor reader](SKELETAL_APPEARANCE_DESCRIPTOR_CONTRACT.md) with owned skeletons, skin sources, geometry, logical tables, prepared idle, authored material/texture defaults, palettes, and the explicit [walk/run bank](SKELETAL_ANIMATION_BANK_CONTRACT.md), at mesh detail 0. Skeleton preparation uses the existing level 0 binding. It does not choose a runtime animation graph or infer a skeleton level from distance.

The accepted cache owns cloned raw payloads and retains independently parsed descriptors, pixels and prepared clips. Each normalized external source is looked up once per load, including inherited templates and shared dependencies. The caller retains ownership of the resolver and archive lifetime. No resolver, token or callback survives in the result. Caller animation dictionaries/routes are copied before the first lookup. There is no shared/static cache or published partial result on rejection.

`SceneCreatureResources` carries the immutable scalar create snapshot, selected catalog entry, owned creature bundle, external-read count and aggregate present-source bytes. Object ID, template CRC, raw hyperspace byte and finite quaternion/position float bits remain unchanged, including negative zero and subnormals. Position fields remain X/Z/Y. No engine matrix, quaternion repair, root translation, actor registration, baseline application, event dispatch or rendering occurs.

Default source budgets are 64 distinct attempts and 16 MiB aggregate present bytes; caller limits are 1..4,096 attempts and 1..128 MiB. The existing object-inheritance, IFF, geometry, animation and individual image decoder bounds still apply. A successful result additionally requires at most 32 MiB decoded texture-mip bytes. This last check occurs after the unchanged image readers decode; it is a retained-output limit, not an early aggregate allocation limit. Cancellation is checked before the load, before/after each external lookup and after descriptor preparation. CPU parsing between checks is synchronous and is not immediately interruptible.

Required missing/masked object, SAT, MLOD, MGN, skeleton, clip, shader or texture rejects. Changed root/SAT/selected clip pins reject. Other dependency identities are recorded and independently audited in the proof, but are not additional production pins. The existing optional missing-palette fallback remains part of the unchanged material reader; this milestone does not claim that every absent palette rejects.

## Evidence and confidence

The supplied original entry is `object/creature/player/shared_human_male.iff`, CRC `2937962913` (`AF1DC1A1`). Root SHA-256 is `E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243`; appearance SHA-256 is `951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C`. The configured original overlay is pinned to `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. The explicit human locomotion profile (external historical reference) supplies movement routes and clip pins; idle is the established ambient-01 selection.

Observation: the native original proof resolves five inherited object templates, SAT, two MLODs, detail 0 body/head MGNs, two skeletons, two LATs, three prepared clips and authored shader/effect/texture/palette dependencies. All 37 external paths are read once; 4,683,848 source bytes, 53 bound joints, four authored MAIN shader bindings and ten textures are retained. It preserves a synthetic create's full-width ID, real template CRC, flag 255, nonunit quaternion, negative-zero component and subnormal position bits.

Observation: 75 native rejection cases cover malformed/conflicting catalog configuration, a fixed synthetic CRC collision, budgets, unknown/empty catalog with no lookup, malformed bank/routes, cancellation, missing/masked required resources, altered pins and pinned bytes. Count/byte limits succeed exactly at the measured closure and reject one below. Mutating all 37 borrowed source buffers and caller bank/routes leaves two mesh fingerprints, ten texture mip fingerprints and three sampled poses unchanged. A fresh load succeeds after the rejection cases.

Independent Python audit (external historical reference) uses polynomial long division for checksums, validates the supplied manifest and create identity, and checks every native source's priority/size/SHA against effective original TRE bytes. It reuses closed template/SAT/MLOD and counted animation-route parsers rather than replacing their source grammar. Authored MAIN references are correlated against original shader records. These are **Confirmed implementation/fixture observations**. The caller catalog, rejection budgets and recipe selection are explicitly modern policy; no original global table completeness or runtime application claim follows.

## Reproduction and handoff

From the isolated repository root with .NET 8 and the accepted private package cache:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round5-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
New-Item -ItemType Directory -Force local-private/round5 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -Encoding utf8 local-private/round5/NuGet.Config
dotnet restore poc/round5-tests/Assets --configfile local-private/round5/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round5-tests/Assets --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round5-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json > local-private/round5/native.txt
python tools/python/verify_scene_creature_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json local-private/round5/native.txt
python tools/python/validate_organized_workspace.py .
```

Use the configured Python executable when `python` is absent from PATH. Original resources are read only from the explicitly configured overlay; original game execution and network access are unnecessary. The own console project (external historical reference) compiles existing smoke helpers and invokes only this scenario (external historical reference). Shared runner and existing readers remain untouched. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R5_CREATE_RESOURCES.md) records baseline/scope/validation.

Lead review and integration are next. A later consumer can publish the owned resource result into lifecycle/renderer state using a separately explicit transform policy. Global template discovery, appearance alternatives, arbitrary object families, engine transforms and runtime root/event policies remain outside this slice. No GPU or headless prototype host assertion is claimed.

## Optional albedo forwarding —Round10

Load now has final optional loadAlbedoFactors=false. Default37-source behavior remains; explicit true prepares the independently checked [39-source authored albedo bundle](SCENE_CREATURE_ALBEDO_RESOURCE_CONTRACT.md). Loader selection is explicit and does not change initializer requests or claim full effect/lighting fidelity.
