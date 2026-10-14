# Optional diagnostic current object names

[SceneWorldObjectNameVisual](../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldObjectNameVisual.cs) implements `ISceneWorldObjectPresentation` and its optional drain interface. Constructor: `(SceneWorldCoordinator world, ISceneWorldObjectPresentation inner, SceneWorldObjectVisual basis, Func<CreatureView3NameState,string> nameResolver, int maximumTextUnits=256)`. It owns the supplied inner and one Label3D after successful construction, borrowing world/base/resolver. Default world presentation is unchanged; the caller explicitly opts into a diagnostic label.

## Ownership and freshness

The base must be a live published typed creature, matching the exact inner publication, current incarnation, resource/create/envelope/replication freshness guards. Creation/resolution/updates/disposal require Godot's main thread. The label is a child of the existing creature root; it inherits root visibility, Height scale and root motion without changing appearance resources. It uses Godot's diagnostic default font, gold text, font32/pixel size0.006 at localY2.05; no original font/UI/nameplate parity is claimed.

Constructor validation and initial resolver success precede label allocation/inner ownership transfer. Failures retain caller ownership. Apply rejects stale snapshots; Tick/Apply refresh current Names. The resolver is skipped only when the same owned immutable `CreatureView3NameState` reference is retained. Callback output is bounded to the supplied UTF16-unit limit1..1024, default256; null, control characters, lone surrogates and over-limit text reject. Empty text is allowed. Raw model strings/units remain unchanged; display rejection does not repair them.

The supplied resolver must be pure and perform no synchronous asset I/O. The wrapper takes non-consuming change-journal observations, checks mutation revision/pending identities, current owned Names, root transform/visibility and reentry before publishing text. No batch is acknowledged. Caught reentry attempts also reject. These checks do not sandbox arbitrary external callback side effects; purity/no-I/O remain caller contracts. Resolver errors, lost labels, stale lifetime or inner retirement release label and inner once, drop borrowed/callback/name references, and retain the inner's drain task after disposal. A failed wrapper must be replaced rather than retrying a failed callback every frame.

## Independent diagnostic proof

The [standalone preview](../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldCreatureCustomizationPreview.cs) adds separate `--customization-name-race`, mutually exclusive with the other race modes. It uses independently authored **complete baseline reseeds**, not named deltas or unfinished Round25 APIs. The accepted store permits reseeding; this replaces historical baseline/current typed state but retains incarnation/publication. Other fixture fields remain identical so pose/resources can be compared. The actual named-wire/current-overlay join remains separate.

The supplied synthetic resolver checks bounded inputs (file/key32 bytes, custom64 units), prefers nonempty printable ASCII custom units, otherwise maps exact `diagnostic/alpha` to `FIRST CAPTION`, `diagnostic/beta` to `SECOND CAPTION`; empty original names display `BASE CAPTION`. This is an explicit diagnostic policy, not recovered localization priority. The latest fixture retains beta/StringId spacer `0xF1234568` and raw custom UTF16 `LATEST CAPTION`. The first uses alpha/spacer `0xF1234567` and empty custom units.

While appearance CPU revision2 is held and pending3/4 coalesce, the first name reseed changes the caption, then the latest reseed displays `LATEST CAPTION` before GPU commit. Revision4 appearance commit retains exact raw Names object, resolver count, label instance/text, pose/normals/topology/UVs, root transform, paused walk frame3/Height1, eight-key source revision4 and appearance ownership. Peak active composers1. Visibility0 hides both body and label; showing the latest complete baseline restores the exact committed image without archive reload. Final held revision23 starts hidden, then generation or destroy retires the owner and drains after release, leaving accepted revision22 and root1/1/archive1 balanced.

Host probes exercise null/over-limit/control/unpaired text and thrown constructor callbacks without ownership transfer, constructor world mutation, same-reference caching, stale Apply, late resolver failure, caught reentry, callback label reparenting and double disposal. Probe labels are removed while borrowed base remains live. The [native scenario](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldObjectNameScenarios.cs) passes21 raw fixture assertions, baseline round trips/ownership/aliases, exact file/key/spacer/units and visibility; prior container226/scalar134/source97/CPU queue regressions pass.

## Executed GPU evidence

Generation headless and destroy GPU hosts plus [independent audit](../../SWG-Godot-Client/poc/round25-tests/Rendering/Verify.py) pass. It independently parses complete StringId/raw UTF16/tail bytes, derives diagnostic captions, compares all five receipts and verifies original-byte face preparation/five-layer composition. Original closure76 reads/5,940,390 bytes. GPU MAIN readback matches zero SHA-256 `A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808` and varied `60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5`.

Name change affects6,360 caption pixels with identical body pixels below the explicitly fixed camera's caption band. Appearance commit changes14,497 body pixels with identical latest-caption band and body silhouette/background. Hidden PNG is uniform background; shown equals the committed PNG exactly. Actual Label3D text and retained label ID are checked independently of pixels. The committed caption/body capture was visually inspected. Accepted R24 visibility and R23 container headless hosts/independent audits pass. Godot build has zero warnings/errors. There is no live name/player/STF/locale/cache or original UI coverage claim.

## Reproduction and next action

Use existing Python/.NET8/Godot4.7.2Mono, private APPDATA/populated caches and own no-source NuGet config. No downloads/original executable execution:

```powershell
python poc/round25-tests/Rendering/Generate-Peer.py local-private/round25/peer.json
dotnet restore poc/round25-tests/Rendering/Rendering.csproj --configfile poc/round25-tests/Rendering/NuGet.Config -p:NuGetAudit=false
dotnet run --project poc/round25-tests/Rendering/Rendering.csproj --no-restore -p:UseSharedCompilation=false -- local-private/round25/peer.json poc/preview-profiles/scene-object-catalog.json
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false
```

[Launcher](../../SWG-Godot-Client/poc/round25-tests/Rendering/Launch.ps1) takes GodotPath, ArchiveRoot, Configuration, FixturePath, Retirement generation|destroy and optional fresh private CapturePath. Run generation headless and destroy GPU. Extra PNG suffixes: `-first`, `-latest-held`, `-latest-committed`, `-hidden`, `-shown`, alongside ordinary base/varied pair. Audit `Verify.py EXPORT PEER LOG [CAPTURE_STEM]`; omission of stem checks bytes/receipts/original face without pixel claims. All exports/captures stay ignored.

The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R25_OBJECT_NAME_PRESENTATION.md) records identity/scope. Lead should join actual named ingress and the separately reviewed original-STF resolver with this explicit consumer, preserving current Names/TangibleScalars through older ingress. Original locale fallback/cache and UI/font choices remain separate.
