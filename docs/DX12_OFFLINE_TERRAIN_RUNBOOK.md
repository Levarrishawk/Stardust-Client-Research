# Windows x64/DX12 offline terrain roaming

October12,2026. The delivery folder on this workstation is `local-private/builds/Stardust-Terrain-DX12`; double-click `Stardust.Client.exe` there. Keep its PCK, adjacent `character-launch.json` and .NET data directory together. The executable includes its runtime; no editor or SDK is required. Original resources remain in `C:\Stardust-DEV`, mounted through `swgemu_live.cfg`. Default launch creates no socket. The earlier `Stardust-DX12` character package remains separate.

The original animated male human now moves across a256×256metre Tatooine terrain region. This is **Tatooine base terrain — partial generator**, as labeled in the window. The retained original TRN supplies the root AHFR/family1 base and the already verified layer3 BCIR/FFRA/AHFR/family4 detail. Detail priors are the actual computed base heights; no synthetic height field, plane or warm priors are supplied. Other geographic layers are omitted, so this is not the complete original location or its final elevations. Full terrain material blending, flora, water, buildings, nearby actors and live gameplay remain open.

| Control | Behavior |
|---|---|
| WASD | Camera-relative walk |
| Shift | Run |
| Right-drag / wheel | Orbit / zoom |
| P | Pause / resume |
| R | Reset actor to grounded region origin and reset camera |
| Escape / window close | Cancel loading or close after owned worker drain |
| Focus loss | Clear held input and stop movement/animation |

The modern controller uses3m/s walk and6m/s run, normalized diagonal input and at most100ms of frame time. These speeds are offline policy, not recovered server or Pre-CU movement settings. It grounds feet on the same retained triangles rendered by the host. Candidate endpoints outside the region hold position. There is no swept collision, slope restriction, obstacle collision or streaming. The camera follows the actor with a modern fixed-distance orbit and does not implement camera/terrain collision.

The original root ASCN selects the base shader family. Its first entry supplies original `tatt_sand_bumpy` MAIN texture. First-entry selection, opaque modulation, nearest-repeat sampling, UV scale and diagnostic lighting are modern presentation policies; original random family selection, texture blending, effect execution and final shading parity are unproved. Original idle, male walk and run clips play through the existing prepared skeletal path with a150ms pose transition. Generated face/equipment and exact root-motion/speed parity remain open.

## Build and acceptance

Use the same local toolchain inputs as the [character runbook](DX12_OFFLINE_CLIENT_RUNBOOK.md), adding `--terrain` to the existing [builder](../tools/python/build_offline_dx12_client.py). Supply a fresh output under ignored private storage. The character package is a separate entry and need not be overwritten.

```powershell
python tools/python/build_offline_dx12_client.py --terrain --dotnet $Dotnet --godot $Godot --packages $Packages --templates $Templates --asset-root C:\Stardust-DEV --config C:\Stardust-DEV\swgemu_live.cfg --output $FreshTerrainBuild
python tools/python/verify_offline_terrain_client.py --build $FreshTerrainBuild --output $FreshTerrainVerification --gpu
```

The [terrain verifier](../tools/python/verify_offline_terrain_client.py) runs from a different working directory with toolchain environment overrides removed. It checks frozen package hashes, bundled runtime, headless terrain/controller acceptance, cancellation before publication, headless device refusal, unsupported option/profile, actual OpenGL fallback refusal and actual DX12 GPU movement. The proof drives the same `_Process` controller through synthetic W, Shift+W and idle phases, exports three captures and retained geometry, then independently checks barycentric grounding and travelled distance against accepted time. Synthetic focus/input in this proof is explicit; it does not claim physical keyboard/mouse acceptance. Expected negative exits remain1.

All8checks pass for the October12package on NVIDIA RTX5060Laptop/D3D12API12_0;193exported files include the runtime. Final GPU proof records536updates, original walk/run/idle and a maximum stored-grounding error below4.8e−7m. Capture review confirms readable HUD and visible sand detail after reducing diagnostic exposure; highlight-clipping/HUD-background guards now accompany geometry checks. Other hardware, clean-machine deployment and sustained traversal remain unverified.

The engine-neutral [region](../poc/Stardust.Core/Geometry/OriginalTerrainRegion.cs) builds16padded64m chunks at8m sample spacing through existing coordinate/noise/height/normal/patch components. Strict shared height and normal admission checks234sample comparisons across24edges and18corner pairs before any GPU allocation. The [controller](../poc/Stardust.Core/Session/OfflineTerrainRoaming.cs) retains the actual aggregate surface query. The [host](../poc/Stardust.Godot/OfflineTerrainClient.cs) owns texture,256patch adapters, actor, camera and cancellation. Unsupported generator identity rejects without a synthetic fallback. The exact effective input and uncertainty are recorded in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-12_DX12_OFFLINE_TERRAIN.md).

The source-guided next action is to admit the remaining enabled height layers in original traversal order, with independent fixtures for their boundaries, filters and affectors; then compare representative original geographic locations. The partial surface cannot support live authoritative placement acceptance.
