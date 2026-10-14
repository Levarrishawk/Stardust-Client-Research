# Palette-only scene creature customization

R11-ASSETS-01, October 2, 2026. SceneCreaturePaletteCustomization (external historical reference) describes and applies explicit palette values to an accepted owned [albedo-enabled scene creature](SCENE_CREATURE_ALBEDO_RESOURCE_CONTRACT.md). It requires no face generation. It returns a new resource/creature/material view while sharing the unchanged owned asset closure. Existing readers, shaders, profiles and full [SkeletalAppearanceCustomization](SKELETAL_CUSTOMIZATION_SELECTION_CONTRACT.md) remain unchanged.

## Controls, namespace and modern range policy

`DescribeControls(resources, cancellation)` obtains keys solely from owned `MaterialBundle.PaletteDefaults` shader palette factors. Key identity is the exact ASCII variable name plus `Private = RawPrivateFlag != 0`. The established namespace mapping is true/private and false/shared-owner; names, case and domains remain distinct. No face-operation, texture-choice, shape or wire AppearanceData keys are inferred.

Each variable's range is `0..min(palette entry counts)-1` across all factors referring to that key. The returned read-only `SkeletalCustomizationControl` list sorts by ordinal variable name then private Boolean. This is **modern strict acceptance policy**: the underlying CSHD palette query supports invalid-index fallback to zero, but this operation rejects values outside the intersected range rather than applying that fallback.

Required input is an owned material bundle with nonempty palette defaults and albedo factors. Material count and palette-material count are bounded by64; albedo material keys must equal material keys, and palette keys must equal the materials with customization descriptions. Total palette factor visits and required variable keys are bounded by4,096. Per palette bundle, factor/palette/missing-path collection counts are bounded by4,096. Variable names are1..256 printable nonspace ASCII characters; owned palette ranges must have1..1,024 entries. Empty/oversized/missing palette descriptions reject. At least one control is required. These bounds do not recover the original runtime's rejection policy or authenticate caller-constructed descriptor graphs.

The accepted original human exposes exactly:

| Variable | Private | Inclusive range | Used by |
| --- | --- | --- | --- |
| `index_color_skin` | false | 0..255 | body/head MAIN |
| `index_color_2` | true | 0..255 | mouth MAIN |
| `index_color_3` | true | 0..1 | body/mouth HUEB |

**Confirmed observation:** original CSHD factors refer to `palette/pc_skin_hum.pal` and `palette/pc_eye_hum.pal`, each256 entries, and `palette/white.pal`, two entries. Five factor references collapse to three exact keys. Counts and namespace flags are independently checked from original effective bytes. Describing palette controls does not require the full eight-key face/style union.

## Apply, ownership and cancellation

`Apply(resources, IEnumerable<KeyValuePair<ShaderCustomizationKey,int>> values, cancellation)` accepts an enumerable so duplicates can be detected instead of silently collapsed by a caller dictionary. It snapshots at most4,096 supplied pairs, checks duplicate keys, exact required-key equality and every inclusive range before creating result records. Missing, extra, wrong-domain, wrong-case, duplicate, negative, excessive and out-of-intersection values reject with `InvalidDataException`. An endless caller enumeration stops on pair4,097 and its enumerator is disposed. Cancellation is checked before describing, during factor/value/selection loops and immediately before returning; synchronous work between checks is cooperative.

Selection reuses `CustomizableShaderPaletteQuery.Select` on the existing owned palettes. Every returned selection must be explicit (`UsedAuthoredDefault=false`), nonempty and have `SelectedIndex==RequestedIndex`. No palette-index fallback is permitted. Missing palettes retain the existing owned cyan fallback at its only legal index0; asset diagnostics remain. Prior index-fallback/empty-selection diagnostics are removed when recomputing a valid selection. Existing authored defaults and immutable shader metadata remain unchanged.

All four factor records are recomputed with the accepted `SkeletalAlbedoFactorReader.WithPalette`; validated program/tag/mask identity is preserved while ARGB colors can change. New read-only palette/factor dictionaries, selected-factor records and `Creature.CustomizationValues` snapshot are owned. The historical `AuthoredFactors` property stores explicit selections here, just as in the accepted full customization operation.

The outer resources, creature and material records are new. Raw Create and Recipe records, source identities, original external-read/byte totals, images/mips, material/effect descriptions, geometry, skeletons, skin sources, logical tables, prepared idle/walk/run and pixel-program metadata are shared. The operation takes no resolver, performs no archive/file/network/engine access and stores no caller enumerable, stream, callback or token. It neither mutates its input nor changes retained texture pixels. Failed calls publish no result.

Face resources are not required or generated. The accepted scene input has no face blueprint/resources/generated outputs. If a caller supplies an existing face-bearing view, this operation leaves those outputs unchanged; its returned customization snapshot describes palette keys only. It is not a replacement for coordinated full face/style regeneration.

## Authored JSON profile

`LoadProfile(Stream)` forwards to the existing `SkeletalAppearanceCustomization.LoadProfile`, which reuses the accepted strict version1 Blueprint profile parser. The parser itself does not require face data. Root properties are exactly `version` and `values`; entries are exactly `variableName`, Boolean `factoryFlag` and int32 `value`. FactoryFlag maps directly to Private. Existing byte65,536/depth4/value-count4,096/name256 limits, duplicate decoded-property/key, unknown/missing field, UTF-8/type/version/truncation rejection apply unchanged. The caller retains stream ownership. Parsing checks syntax and generic key/value shape; `Apply` additionally checks the actual owned control set and ranges.

human-scene-colors.json (external historical reference) independently chooses shared-owner skin75, private eye5 and private HUEB1. These are inspection choices, not recovered character defaults or a palette corpus. File SHA-256 is `72BE9FC64AEA1A0EB15DAA2B43E31CBD272BB01CBA61E78C5F190E04D3556726`. A second zero choice is constructed independently in the scenario. No original palette/asset bytes are included in the public profile.

## Original proof and limits

The input uses the accepted catalog human CRC2937962913, object SHA `E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243`, appearance SHA `951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C`, idle and the separate implementation record selections. Explicit read-only original overlay is `C:/Stardust-DEV`, `swgemu_live.cfg` SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; scene loading selects `loadAlbedoFactors:true`.

**Confirmed implementation/source observation:** both color choices preserve39 read-once sources /4,686,371 bytes, ten textures /80 mips, two program sources and all existing geometry/animation/source identities. The native suite checks50 reference-sharing relationships across the two returned views, input factor fingerprints before/after, caller value/profile-byte mutation, all39 borrowed source-array mutations and read-only value-dictionary enforcement. No additional source calls occur.

**Confirmed selected-color observation:** five selected palette records per choice independently match original RIFF entries/indexes/raw flags. All four material factors per choice independently match original TFNS/CSHD/pass/PSHP evidence via the accepted albedo verifier. Eight actual original MAIN mip0 pixels are decoded and their source-encoding factor equations reproduced within2e-7. Body and head sampled colors differ between zero and authored profile. The chosen face sample remains white-factor unchanged; mouth pixel0 selects its HUEB branch and is unchanged between these particular choices. Different requested palette indexes are not assumed to imply different RGB entries.

**Confirmed negative/cleanup observation:**38 checks cover missing/range/namespace/case/duplicate values, int32 extremes, absent albedo resources, canceled application, bounded endless enumeration/disposal, synthetic palette-range intersection and empty/oversized ranges,11 malformed raw profiles, immutable output and the unchanged full customization API's face requirement. Synthetic descriptor colors/counts are independently authored, explicitly separate from original-source claims. Fresh valid results remain usable after the failures.

The existing initializer accepts the customized owned closure at exact39-resource /4,686,371-byte limits, preserves result identity, transfers once and has zero outstanding jobs. Dispose/drain and registry Close complete. This is accepted initializer composition, not added optional-factor semantic validation, world publication or wire customization handling.

Independent verifier (external historical reference) checks every original source priority/size/SHA, raw-create byte identity, controls/range intersection from original factor/palette records, both value snapshots, all explicit palette selections and factor/program identities,80 unchanged mip hashes and eight sampled colors. Its separate bounded JSON parser agrees on the malformed profile fixtures. Native-only enumeration/alias/cancellation/initializer observations are identified through the recorded receipts, not claimed as independently re-executed behavior. **Confidence:** Confirmed bounded finite-fixture agreement. Full original lighting, gamma/display, face regeneration and network AppearanceData/customization decoding remain outside scope.

## Reproduction and next action

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round11-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
New-Item -ItemType Directory -Force local-private/round11 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round11/NuGet.Config
dotnet restore poc/round11-tests/Assets/Assets.csproj --configfile local-private/round11/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round11-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round11-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json poc/preview-profiles/human-scene-colors.json > local-private/round11/native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/verify_scene_creature_palette_customization.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-scene-colors.json local-private/round11/native.txt > local-private/round11/python-audit.json
python tools/python/validate_organized_workspace.py .
```

Use the own runner (external historical reference); restore is offline with existing cached packages. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R11_SCENE_COLORS.md) records baseline/scope/validation. Lead acceptance/integration is next. A future explicitly dispatched consumer can present these owned palette-only factors; no default/profile discovery or host-rendering change is introduced here. Stop idle pending Round12 dispatch.
