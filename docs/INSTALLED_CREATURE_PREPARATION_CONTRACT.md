# Installed creature preparation

[InstalledCreaturePreparation](../../SWG-Godot-Client/poc/Stardust.Core/Assets/InstalledCreaturePreparation.cs)
derives one creature catalog entry and idle/walk/run options from an explicitly
opened [original installation](ORIGINAL_INSTALLATION_PREFLIGHT_CONTRACT.md).
No caller-provided SHA pins, preview profile, or historical argument vector is
required. Animation choices remain explicit modern presentation policy.

## API and lifetime

```csharp
public sealed record InstalledAnimationChoice(
    string TablePath, string LogicalName, IReadOnlyList<int>? ExplicitChildRoute = null);

InstalledCreaturePreparation.Create(OriginalInstallationInputs inputs, string objectPath,
    InstalledAnimationChoice idle, InstalledAnimationChoice walk, InstalledAnimationChoice run);
```

The result exposes get-only `ObjectTemplateCatalog Catalog`,
`SceneCreatureResourceOptions Options`, and
`IReadOnlyDictionary<string, AssetSourceIdentity> Sources`. Catalog, options,
routes, and source rows are independent immutable copies. The result owns no
archive handles, retains no source payloads, and has no Dispose method. It borrows
the input overlay during Create. The caller keeps the installation alive for
subsequent loaders; creation with disposed inputs fails. Metadata remains usable
after input disposal, while further loading requires a valid caller-owned resolver.

## Discovery and rejection

The implementation reads/hash-identifies the actual root bytes through the existing
owned source cache, then uses `SharedCreatureTemplateProfileReader` for inherited
SCOT/STOT/SHOT appearance selection and supported deterministic collision fields.
It does not interpret an APT as a skeletal appearance. The inherited appearance
must be an `appearance/*.sat` path and decode as the existing SMAT 0003 family.

Every LAT declared by the SAT is decoded, even when none of the three choices uses
that table. A choice must select a declared `appearance/lat/*.lat` table and one
unique serialized logical entry, under the existing ASCII-insensitive lookup
policy and trailing-space trimming. Synthetic defaults, explicit `default`, absent
names, and duplicate case-insensitive entries fail. The implementation does not
call the table reader's default-fallback lookup. Counted child routes and proxy
envelopes use the existing `LogicalAnimationProxyDecoder`. Ambiguous SSAT/SPAT
branches fail without a complete explicit supported route. Selected clip hashes
come from installed decoded bytes rather than supplied pins.

Caller paths must be bounded printable ASCII relative logical paths with no URI,
leading slash, colon, control/NUL, empty, dot, or traversal segment. Internal
backslashes and ASCII uppercase normalize through the existing path normalizer.
Object paths use `object/*.iff`; choice table paths use `appearance/lat/*.lat`.
Routes are copied before I/O, have at most eight indices, each 0 through 4,095;
the decoder further validates indices against actual counted children.

Missing/masked roots, inherited templates, SATs, LATs, or clips fail; tombstones
retain overlay masking semantics. The inherited-template reader retains its
16-resource/1 MiB/depth-16 restrictions. Preparation's outer source cache and the
resulting resource options use 256 sources and 64 MiB aggregate source bytes.
Those cache limits are checked after the resolver returns bounded TRE payloads;
they are not a pre-allocation guarantee for an individual archive member. Existing
SAT/LAT/parser limits also apply. Failure leaves the borrowed installation owned
and usable by its caller.

The one catalog entry uses normalized-path legacy CRC plus the discovered root
and SAT hashes. Options select detail-0 creature loading, albedo factors enabled,
idle plus exact walk/run bank, no face inspection, and no authored movement-input
extras. This preparation discovers identities; it does not decode clip channels,
prepare mesh geometry, or claim closure completion by itself. The existing
`SceneObjectResourceRouter.Load` performs that subsequent validation/preparation.

## Pin discovery and source identities

`Sources` contains compact path/priority/decoded size/SHA-256 identities for root,
inherited templates, SAT, every declared LAT, and the selected clips actually read.
It contains no payload bytes, endpoints, configuration contents, or physical paths.
Its paths are preparation dependencies, not a declaration of all appearance
dependencies. No installation receipt or original bytes are promoted by the tests.

Discovered pins do not create an immutable archive snapshot. A later preparation
against changed archives can derive different hashes. Earlier catalog/options
remain unchanged; existing pinned readers reject changed pinned sources. Catalog
pins cover root/SAT and choice pins cover selected clips; other later loader
dependencies retain their existing reader identity policies. Callers must keep
their installation stable and decide whether to compare additional identities.

## Successful original choice policy

The read-only human pipeline uses `object/creature/player/shared_human_male.iff`
with these explicit selections, verified against installed declared tables:

| Role | Table | Logical name | Child route |
| --- | --- | --- | --- |
| Idle | `appearance/lat/all_m.lat` | `emt_standing_idle_ambient_1_ag` | null |
| Walk | `appearance/lat/all_m.lat` | `loop_standing` | `[0,1]` |
| Run | `appearance/lat/all_m.lat` | `loop_standing` | `[0,2]` |

These choices follow the already accepted [selected proxy](SELECTED_ANIMATION_PROXY_CONTRACT.md)
and [animation bank](SKELETAL_ANIMATION_BANK_CONTRACT.md) policies. This component
does not load their old profile files or copied pins. The distinction matters:
original animation bytes are consumed through explicit modern selections.

## Reproduction and evidence

The self-contained [test project](../../SWG-Godot-Client/poc/round84-tests/Assets/Assets.csproj) runs
synthetic preparation by default. Its Python producer independently authors
TREE/SCOT/STOT/SHOT/SMAT/LATT/counted-proxy records and metadata. Synthetic clip
payloads are deliberately opaque hash fixtures, not claimed playable animations.

```powershell
& ./poc/round84-tests/Assets/Invoke-Preparation.ps1 `
  -Python '<cached-python.exe>' -Dotnet '<cached-dotnet.exe>' `
  -CachedPackages '<existing-private-NuGet-cache>'
```

Output is a fresh ignored directory under `local-private/`; use optional `-RunRoot`
to specify it. The runner clears package sources and uses copied cached packages.
For the original pipeline, add the explicit pair:

```text
-AssetRoot C:/Stardust-DEV -Config C:/Stardust-DEV/swgemu_live.cfg
```

For consolidated regression integration:

```text
python poc/round84-tests/Assets/peer.py generate <fresh-absolute-private-fixtures>
dotnet build poc/round84-tests/Assets/Assets.csproj <offline-build-options>
dotnet <built-Assets.dll> <fixtures>/fixtures.json <fixtures>/receipt.json
python poc/round84-tests/Assets/peer.py verify <fixtures>
```

The producer optionally accepts `--asset-root <root> --config <file>` together;
input admission occurs before output writes. For that mode it independently derives
the entire expected preparation metadata from installed bytes before native
execution. Native then calls Create and the existing router on the derived catalog
and options. Verification compares all preparation metadata and every loaded
closure identity against independent effective-overlay bytes. Outputs remain private.

Observed final validation: 34 rejections in both synthetic-only and original modes,
immutable route/source snapshots, caller ownership after failure, missing/masked
inputs, inheritance cycle, wrong template/SAT family, malformed LAT, undeclared
table, default/absent/ambiguous choice, counted-route/depth and inherited-source
byte limits, disposed input, changed-clip discovery, and old-root pin rejection
after actual synthetic archive mutation. Original preparation reads 11 sources /
418,866 decoded bytes; subsequent router loads 39 sources / 4,686,371 decoded bytes,
1,454 geometry positions, and prepared idle/walk/run. Independent metadata/hash
agreement, offline warning-free builds, and default Core smoke pass. One test
compile error was corrected before the passing run; no failed attempt counts as
acceptance. These are **Confirmed component/fixture observations**.

Arbitrary avatars, customization variants, terrain, complete lighting, movement
authority, server state, and visible Godot publication remain **Unverified** here.
The next action is launch composition to use these catalog/options with the selected
create message and borrowed overlay, then publish the owned router result under
its separate scene/presentation lifetime contract. No new executable analysis or
original executable execution was performed.
