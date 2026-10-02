# Scene create to static object resources

R7-ASSETS-01, October 2, 2026. This bounded engine-neutral slice connects a decoded create and explicit caller catalog entry to original static object inheritance and the existing textured appearance graph. It preserves raw create identity and returns owned descriptors, geometry and available material bundles. It does not place a world object, publish readiness or instantiate an engine visual.

## Recipe and shared-template boundary

The existing [catalog](SCENE_CREATURE_RESOURCE_CONTRACT.md) retains `CreatureDetail0=0` and adds `StaticAppearance=1`; both still require exact normalized legacy path CRC and explicit object/appearance SHA-256 pins. The [creature reader](../poc/Stardust.Core/Assets/SceneCreatureResourceReader.cs) now rejects static entries before lookup. The [static reader](../poc/Stardust.Core/Assets/SceneStaticResourceReader.cs) rejects creature entries and unknown CRCs before lookup. There is no fallback family or global catalog claim. No optional creature albedo flag was added.

[SharedObjectAppearanceProfileReader](../poc/Stardust.Core/Assets/SharedObjectAppearanceProfileReader.cs) reuses `SharedObjectTemplateDecoder`, follows the full bounded inheritance chain and resolves the nearest SHOT `appearanceFilename`. It preserves root/path, selected source/family/version, read-only layer/parameter evidence and each source identity. A nearer missing field or mode0 inherits; mode1 defines the nearest value, including an explicit empty value that rejects rather than falling back to a farther nonempty ancestor.

Only string mode0 (one zero byte) and mode1 (byte1 plus a single terminated printable ASCII string) are accepted. All encountered appearance fields are validated, including farther fields after a winner is found. A farther literal empty string is structurally valid and may be shadowed, as the original base object demonstrates. Random/unknown modes, malformed strings and unresolved/nonempty-required selection reject. Unrelated parameter payloads remain opaque.

References are logical archive paths. Literal `.`/`..` segments are rejected before legacy normalization, including raw DERV strings that the existing decoder would otherwise normalize. Parent paths must normalize to nonempty `object/*.iff`; selected nonempty appearance strings must normalize beneath `appearance/`. Repeated normalized parent paths reject as cycles; missing/masked parents and unsupported versions reject. Leading slash, case/backslash and duplicate separator normalization remain the existing logical-path convention, with no operating-system lookup introduced.

The only structural decoder change is adding SHOT/0008 to its observed replacement gate. Original terminal bytes below independently establish STOT/0007 + SHOT/0008 with unchanged DERV/PCNT/XXXX framing. This is not the original executable's complete factory version set. The older [creature template contract](OBJECT_TEMPLATE_COLLISION_PROFILE_CONTRACT.md) describes its prior version gate; lead reconciliation of that listing belongs to acceptance.

Profile defaults bound 16 templates and 1 MiB aggregate source bytes. Direct caller limits allow depth 1..16 / bytes 1..16 MiB; per-template IFF remains 1 MiB, 4,096 nodes, depth 16, 512 parameters per layer. The static reader uses at most 16 inheritance levels and 1 MiB template bytes within the caller's smaller overall bounds.

## Static resource preparation and policies

`SceneStaticResourceReader.Load(create, catalog, resolver, materialPolicy, token, limits, maximumDecodedBytes)` validates nulls, explicit material policy, finite raw transform fields, limits and recipe family before resolving anything. It obtains the object profile, then prepares `TexturedAppearanceGraphReader` through pinned appearance -> APT redirect -> supported DTLA/CMPA/MESH -> existing material/effect/texture readers. No appearance filename is guessed.

One outer owned source cache covers object, appearance, geometry and material lookups. All normalized external paths are attempted once per call; present payloads are cloned before decoding. Inner unchanged readers may make their own bounded copies. The result retains a distinct immutable raw-create scalar snapshot, recipe, profile, textured graph, read-only union source identities, external-attempt count, aggregate present bytes and selected policy. Failed calls publish no partial result; no global cache, resolver, archive or cancellation ownership transfers.

`RequireComplete` rejects any unavailable material bundle or missing material mesh. `AllowNeutral` explicitly retains supported geometry with the existing `UnavailableMaterialBundle` diagnostics; whole failed mesh-material bundles are omitted, even if earlier shader/texture pieces were decoded. Neither policy tolerates missing required geometry: new validation rejects the existing CMP part-omission diagnostics and requires at least one supported mesh. Complete returned material bundles are not a promise of rendered shader fidelity.

Overall defaults are 64 source attempts / 16 MiB present bytes / appearance depth 32 / 4,096 edges. Caller ceilings are 4,096 attempts / 128 MiB bytes / depth 32 / 4,096 edges. The decoded-image limit is 4 bytes..128 MiB, default 128 MiB, enforced by unchanged readers after bounded individual decoding; these limits are not a peak-allocation promise. Budgets remain fatal through neutral material handling. Cancellation is checked before preparation, before/after each external lookup, on cached read requests and before returning. CPU parsing/decoding between checks is synchronous.

Raw ID, template CRC, raw flag and seven float fields remain byte-identical to the decoded create, including signed zero/subnormal/nonunit values. No coordinate conversion, quaternion normalization, parent/container choice or root translation occurs. A later consumer can explicitly combine this result with [placement](SCENE_CREATE_PLACEMENT_CONTRACT.md); that is not done here.

## Original proof and material limit

Configured overlay SHA-256: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

| Input | Priority / bytes | SHA-256 or interpretation |
|---|---|---|
| `object/tangible/terminal/shared_terminal_mission.iff` | 39 / 1,469 | `E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281` |
| `appearance/ksk_all_mission.apt` | 31 / 67 | `74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021` |
| Static template CRC | `2907007746` | Exact existing normalized path checksum; caller-supplied mapping |
| Inheritance | Four templates / 4,975 bytes | terminal -> base terminal -> tangible base -> base object |
| Appearance | APT/0000 -> DTLA/0007 | `appearance/lod/ksk_all_mission.lod`, four serialized children |

The root's nearest appearance has SHOT/0008 provenance. The original base terminal repeats that value, tangible base inherits with mode0, and base object has a shadowed literal empty string. Native evidence and independent raw parsing agree on all layer versions/parents/string payloads and identities.

Geometry is owned at all four serialized levels, ordered coarse to fine:

| Mesh level | Vertices | Triangles | Returned material bundle |
|---|---:|---:|---|
| 3 | 208 | 102 | Available |
| 2 | 276 | 142 | Available |
| 1 | 427 | 232 | Unavailable: unsupported static shader envelope |
| 0 | 496 | 296 | Unavailable: unsupported static shader envelope |

The 24 read-once original sources total 1,389,982 bytes. Levels 3/2 retain one shared shader/effect/texture set, including all nine texture mips. The union also records source identities attempted while preparing levels 1/0; those attempts do not make their discarded material bundles available. Independent audit identifies `shader/anim_mission_terminal.sht` as `SWTS`, unsupported by the unchanged static shader reader. Full texture coverage is **not** claimed. Strict material mode rejects this original result; explicitly selected neutral mode returns two precise diagnostics. No SWTS/shader/renderer change is included.

## Validation, confidence and reproduction

[Native scenario](../poc/Stardust.Core.SmokeTests/SceneStaticResourceScenarios.cs) passes 84 rejection cases: malformed string modes/paths, unsupported versions, traversal/cycles, provenance/nearest-empty behavior, source/pin failures, budgets, wrong recipe with zero lookups, cancellation after one resolver call, required geometry failures and strict material failures. Four tiny synthetic profile positives and separate synthetic complete/neutral CMP fixtures exercise boundaries without claiming original content. Clearing all 24 borrowed original buffers preserves profile parameters, four geometry fingerprints and the available texture's mip fingerprints. Exact measured count/byte budgets succeed; one below fails. A fresh load after rejection succeeds.

[Independent auditor](../tools/python/verify_scene_static_resources.py) reuses closed TRE/IFF/template/DTLA/MESH/DDS parsers. It checks actual effective source priority/size/SHA, full inheritance/provenance, checksum/synthetic 47-byte create identity, all four geometry hashes/counts, attempted source closure and returned/omitted material counts. It verifies every mip of the retained texture against original bytes and identifies the actual unsupported shader family. These are **Confirmed static byte/implementation observations**. Recipe catalog, path guards, failure/neutral policy and object preparation are modern composition; original runtime factory/world readiness remains unknown.

The unchanged original creature proof still passes all 75 negatives and its independent 37-source / 4,683,848-byte audit. Its sole scenario edit changes the unsupported enum fixture from 1 to 255 because 1 is now a valid static recipe. The optional albedo expansion was omitted.

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round7-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
New-Item -ItemType Directory -Force local-private/round7 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -Encoding utf8 local-private/round7/NuGet.Config
dotnet restore poc/round7-tests/Assets --configfile local-private/round7/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round7-tests/Assets --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round7-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round7/native.txt
python tools/python/verify_scene_static_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round7/native.txt
dotnet poc/round7-tests/Assets/bin/Debug/net8.0/Assets.dll --creature-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json > local-private/round7/creature-regression.txt
python tools/python/verify_scene_creature_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json local-private/round7/creature-regression.txt
python tools/python/validate_organized_workspace.py .
```

Use the configured Python executable if absent from PATH. The own [console project](../poc/round7-tests/Assets/Assets.csproj) compiles accepted helper scenarios but invokes only the requested static or explicit creature regression proof. Failures are printed with exit code 1 rather than left in a Windows unhandled-exception dialog. No new dependencies, toolchain downloads, original executable, GPU/headless engine or network operations are needed.

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R7_STATIC_RESOURCES.md) records scope and final gates. Lead review/integration, shared probe registration and version-list reconciliation are next. Complete SWTS presentation, other static families/global CRC catalog, asynchronous publication and world/readiness behavior remain open.


## Explicit selected-frame extension — Round8

The default result above is preserved. An explicit pinned [SWTS frame selection](SWITCH_TEXTURE_STATIC_RESOURCE_CONTRACT.md) can now prepare all four original terminal material meshes. It records original SWTS/base/DDS identities and derived references, without runtime switching or readiness claims.
