# Original static terminal frame preview

R9-RENDER-01, October 2, 2026. This bounded Godot inspector consumes the accepted [static object reader](SCENE_STATIC_RESOURCE_CONTRACT.md) and [explicit SWTS selection](SWITCH_TEXTURE_STATIC_RESOURCE_CONTRACT.md). Frame and LOD choices are modern manual inspection policy. Original switch timing, emissive effect fidelity, and automatic LOD behavior are not claimed.

## Original resource preparation

One `OwnedBackgroundAssetLoad` worker opens its own configured overlay, prepares frame0 and frame1 sequentially through `SceneStaticResourceReader.Load` with `RequireComplete`, then closes the overlay before main-thread transfer or GPU allocation. Each call begins at the pinned terminal object and catalog, follows four inherited templates, the effective APT redirect and DTLA, and prepares all four meshes and their complete CPU material dependencies. No direct mesh path bypasses this pipeline.

Original inputs: `C:/Stardust-DEV`, `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; object `object/tangible/terminal/shared_terminal_mission.iff`, CRC2907007746, SHA-256 `E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281`; effective APT pin `74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021`. Explicit selection pins `shader/anim_mission_terminal.sht` to `B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62` and permits only zero-based frame0/1 of the authored eight-frame list. The base SSHT remains `shader/defaultemis.sht`.

Each frame owns 27 read-once sources /1,401,794 bytes, four material meshes, seven textures and62 decoded mips. The selected DDS alone changes between closures. Mesh vertex counts are208/276/427/496 and triangle counts102/142/232/296 in serialized detail order. The fixture's47-byte create is independently authored diagnostic identity, retains negative zero and smallest-subnormal position bits, and is not a captured packet. Its position/quaternion do not place this asset inspector; the appearance is shown under an identity engine root.

The worker snapshots configuration bytes, passes cancellation through the accepted reader, and retains its existing per-call64-source/16MiB source and128MiB decoded-image limits. Two fixed bundles are a bounded inspection set, not a peak-memory promise. There is no loading or frame timer after transfer. Cancellation during the first resolver call closes the archive and publishes no GPU resources; normal shutdown cancels/observes any unfinished worker.

## GPU material boundary and retained selection

Two unchanged `GeometryAppearancePreview` adapters preload all four LOD meshes each. Each adapter owns seven uploaded textures; together they retain14 textures and8 mesh instances. Manual frame selection toggles roots. Manual serialized LOD selection chooses the authored band's midpoint through the existing selector, verifies the visible mesh path, and does not call camera-driven LOD selection. `ValidateChoice` rejects frame outside0..1 or detail outside0..3 before changing visibility.

The existing opaque MAIN adapter rejects `effect/a_emis_full.eft` on the SWTS screen surfaces. The inspector requires exactly those two diagnostics per frame, identified by exact mesh/shader/reason strings, and exactly two neutral surfaces. Any other geometry/material gap rejects. It then installs one **Godot built-in unshaded MAIN inspection material per frame**, shared by the two exact SWTS surfaces. Binding requires the pinned SWTS selection, `a_emis_full.eft`, selected named MAIN texture, source flag0 and UV0. No other surface is overridden. The texture comes from the adapter's existing upload cache.

The two override materials are owned/disposed by this inspector. Body materials retain the adapter's accepted MAIN policy. No handwritten shader or existing adapter/material code changes. UI explicitly labels the built-in unshaded SWTS policy and unknown effect/timing. Complete CPU material dependencies and visible authored textures do not establish original blending, emissive equations, lighting or full GPU effect support.

64 frame/LOD selections preserve root/mesh/material/shader/texture identities, uploaded original mip bytes and geometry. Four invalid choices preserve the same fingerprint. Fixed texture/mesh counts remain14/8; the two built-in override materials are constructed once and reused. The existing adapter checks all uploaded source textures; the fingerprint also samples effective MAIN textures. Teardown frees roots, disposes adapters and overrides, releases parsed bundles and observes the load. Normal interactive window close awaits shutdown; forced tree/process exit cannot guarantee an awaited drain.

## Capture evidence and confidence

Confirmed replacement execution: native, headless and GPU exports agree for both full closures. The independent Python audit reopens effective archives and verifies every source priority/length/SHA, the four-template inheritance and APT/LOD route, all four original mesh geometry hashes and every decoded texture mip hash. Mutating native resolver-owned buffers after preparation preserves the exported results. Seven native negatives cover choice and invalid pinned-frame preflight; invalid selection options perform zero resolver calls.

Source/pixel distinction: frame0 DDS SHA `6BA1EC54844D2614DCAF9C37441C7373D63786F2F3B0D43B953F9A0F15BCC938`, mip0 RGBA SHA `68761BFA88EEB273A3063B69E228ECAA49E9F6FEAE28CC3C23D567C6B2FC8C70`; frame1 DDS SHA `226A16D33CCFFE97EA9BA07B13F13D4B08C973006FC99C9959C670CCDE382ABD`, mip0 RGBA SHA `AD3144AE7FD5B91573B4F7E820D1089DA1027F751CC0BC6FFD6C9D07D591E4FF`. These are original static-byte/decoder observations, separate from renderer output.

The GPU capture pair fixes camera transform, finest serialized LOD3, environment and resource fingerprint. It compares only rows180..719, below the UI. The independent PNG comparison reproduces6,934 changed pixels, observed rectangle `[615,316,717,393)` inside the terminal screen; the complete terminal is visible in both inspected images. A duplicated rendered image pair rejects despite valid source/lifecycle evidence. Thus changing a UI label alone cannot satisfy the difference check. This proves a visible inspection-frame difference, not original shader fidelity.

Headless, real first-lookup cancellation, teardown and eight invalid launcher/capture-pair cases pass. Builds have zero warnings/errors. Known Godot certificate-store startup diagnostics remain nonfatal in passing logs. No original executable addresses/execution, live contact, timer interpretation or research/protocol/function coverage increase is claimed.

## Reproduction

From the isolated checkout with existing .NET8/Godot4.7.2 .NET caches and original inputs (use the configured Python executable if `python` is absent):

```powershell
New-Item -ItemType Directory -Force local-private/round9/appdata | Out-Null
$env:APPDATA=Join-Path (Get-Location) 'local-private/round9/appdata'
Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet build poc/round9-tests/Rendering/Rendering.csproj --configfile local-private/round2/NuGet.Config -p:UseSharedCompilation=false -p:UseAppHost=false -p:NuGetAudit=false
dotnet poc/round9-tests/Rendering/bin/Debug/net8.0/Rendering.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round9/native.txt
$engine='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
tools/powershell/Invoke-StardustStaticSwitchTexturePreview.ps1 -GodotPath $engine -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -Headless
tools/powershell/Invoke-StardustStaticSwitchTexturePreview.ps1 -GodotPath $engine -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -CancelLoad
tools/powershell/Invoke-StardustStaticSwitchTexturePreview.ps1 -GodotPath $engine -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -CapturePath (Join-Path (Get-Location) 'local-private/round9/fresh-terminal.png')
```

Build Godot first; the console references its actual Debug adapter DLL and managed GodotSharp. `GodotManagedReference` can override that managed DLL path. Godot uses the default already-restored SDK resolver with `NUGET_PACKAGES` unset; native uses the accepted offline cache/sources-cleared configuration. No downloads or uncached restore are promised.

The launcher prints each generated log path. `CapturePath` writes frame0 there and frame1 to the same stem plus `-frame1.png`; both must be fresh PNGs in an existing checkout private directory. Automated runs are hidden and bounded to45 seconds. `-Interactive` explicitly opens frame0/1, LOD0..3 and orbit/zoom/reset controls.

Run [verify_preview.py](../../SWG-Godot-Client/poc/round9-tests/Rendering/verify_preview.py) with root, configuration, native log, GPU log, headless log, cancellation log, frame0 PNG, frame1 PNG, in that order. Run [verify_options.ps1](../../SWG-Godot-Client/poc/round9-tests/Rendering/verify_options.ps1) with the three launcher inputs and `-ExistingCapture` (a previously produced frame0 image). The checkpoint records the exact final log/image names. The prior [SWTS verifier](../../SWG-Godot-Client/tools/python/verify_switch_texture_static_resources.py) remains the accepted raw-format investigation; new evidence is the bounded Godot selection boundary.

See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R9_SWITCH_PREVIEW.md). Stop after this milestone for lead acceptance before Round10.
