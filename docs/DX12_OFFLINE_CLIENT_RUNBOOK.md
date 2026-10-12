# Windows x64 Direct3D 12 offline character build

October 12, 2026. The existing engine-neutral .NET 8 core and Godot host now produce a self-contained Windows x64 executable which opens the original animated human directly. The delivered folder on this workstation is `local-private/builds/Stardust-DX12`; double-click `Stardust.Client.exe`. Keep its PCK, `character-launch.json` and `data_Stardust.Godot_windows_x86_64` directory together. An editor, SDK and separately installed .NET runtime are unnecessary at launch.

The profile references the user's existing `C:\Stardust-DEV` installation and its explicit archive mount file `swgemu_live.cfg`. The top-level `swgemu.cfg` contains includes and is not this reader's mount input. Assets remain in the original installation; the package does not redistribute them. Moving the package does not move that installation. The profile uses the existing strict [launch contract](REPLACEMENT_CLIENT_BOOTSTRAP_CONTRACT.md) and installed-resource preparation; default offline entry creates no socket.

## Controls and supported presentation

Drag with the left mouse button to orbit, use the wheel to zoom, and press R to reset the front three-quarter camera. **Play idle** pauses or resumes; the slider seeks the original clip. Losing focus pauses playback. Escape or window close retires the preview; a close during preparation waits for its owned archive scope before exit, and a close during visual loading requests cancellation. Invalid installation/profile/device admission displays a readable failure panel for normal interactive launches.

This bounded display supports the original male human, original idle clip, authored texture/palette defaults, and existing normal/specular translation. It uses diagnostic lighting. Generated face compositing and equipment are absent; the neutral face is visible. There is no terrain/interior scene, live login, character selection or general gameplay in this entry. Existing Round93 and offline playground entries remain separate and safe. This launch completes the runnable original-character part of the [x64/DX12 milestone](NGE_SOURCE_ACCELERATION_PLAN.md); broader representative material, terrain, resize/endurance and hardware-matrix acceptance remains open.

## Build and verify

Use matching official Godot **4.7.2 stable .NET** editor and export templates, a .NET 8 SDK, and a populated offline package cache. The October 12 build used SDK 8.0.425 and bundled runtime 8.0.31. The Godot SDK/package version remains 4.7.2. Three host-lock content hashes were regenerated because the prior lock did not match packages inside the SHA256-verified official engine archive. BCnEncoder and CommunityToolkit dependency pins remained unchanged.

Supply explicit inputs to the [builder](../tools/python/build_offline_dx12_client.py); output must be a fresh directory under ignored `local-private`. The script stages current host/core sources, hashes them, uses the independent installed-resource oracle, creates the required two-project solution, restores from bundled packages, imports and exports with explicit x64 templates. Export errors are rejected even if Godot returns exit zero. PE machine, PCK and .NET data presence are checked. Source identities are checked again after export. A build receipt is explicitly not a runtime pass.

```powershell
python tools/python/build_offline_dx12_client.py --dotnet $Dotnet --godot $Godot --packages $Packages --templates $Templates --asset-root C:\Stardust-DEV --config C:\Stardust-DEV\swgemu_live.cfg --output $FreshPrivateBuild
python tools/python/verify_offline_dx12_client.py --build $FreshPrivateBuild --output $FreshPrivateVerification --gpu
```

`$Templates` is the directory containing `windows_release_x86_64.exe`. The [verifier](../tools/python/verify_offline_dx12_client.py) launches the exported executable from a different working directory with portable SDK paths and environment overrides removed. It verifies frozen package hashes and self-contained runtime libraries. Five default cases cover original headless poses, cancellation with zero publication, explicit headless DX12 rejection, an unsupported option and an invalid profile version. `--gpu` adds actual OpenGL fallback refusal, original GPU pose/material fixtures, and wall-clock animation with two PNGs and a receipt. Expected negative exits remain exit 1 alongside explicit expected-outcome evidence. Omitted GPU verification remains unverified.

The proof mode `--offline-animation-proof <fresh existing absolute evidence directory>` is for bounded automation. It records a synthetic focus override, real `_Process` pose updates, measured elapsed wall time, selected original clip identity and actual backend. Normal no-argument launch retains real focus policy. GPU fixture mode `--capture <absolute PNG>` tests 480 pose updates, two loops, pause/focus/seek and material probes before capture and exit. These are offline implementation tests, not original-client execution or server interoperability.

## Backend admission and evidence

The checked-in project selects Forward+ and Windows D3D12. Runtime admission requires AMD64, eight-byte pointers, a non-headless window, actual `d3d12`, `forward_plus` and a global RenderingDevice. The package refuses a fallback; project settings or an engine banner alone are not acceptance. The APIs are documented in [Godot's RenderingServer reference](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html#class-renderingserver-method-get-current-rendering-driver-name).

The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-12_DX12_OFFLINE_CLIENT.md) records the verified GPU, original input hashes, counts, export identity and limits. Modern binaries, private receipts, logs, captures and toolchains remain ignored. No legacy executable, real UDP, DNS/server contact or publication was used.
