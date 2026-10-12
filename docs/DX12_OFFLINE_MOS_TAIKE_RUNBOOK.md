# Windows x64/DX12 offline Mos Taike

October 12, 2026. This entry opens the original animated human in a bounded west/central Mos Taike district. The delivery folder on this workstation is `local-private/builds/Stardust-Mos-Taike-DX12`; double-click `Stardust.Client.exe` there. Keep its EXE, PCK, adjacent `character-launch.json` and runtime directory together. No editor or SDK is needed. The original installation must remain at `C:\Stardust-DEV` with the supplied `swgemu_live.cfg`. Default launch creates no socket. Previous character and partial-terrain packages remain separate.

The district covers native X3720–3880, Z2280–2440: 160 × 160 metres, including the garage, a medium house, three filler buildings, tents, walls and other scenery. The original snapshot supplies every admitted object's position and quaternion; buildings are not snapped to modern terrain. Of 103 selected original root objects, 94 render and nine have explicit omission receipts: eight unsupported terminal/crafting appearances and one sound object. Interiors and server-spawned actors are outside this entry.

| Control | Behavior |
|---|---|
| WASD / Shift | Camera-relative walk / run |
| Right-drag / wheel | Orbit / zoom |
| P / R | Pause / reset actor and camera |
| Escape / window close | Cancel loading or close after owned resource drain |
| Focus loss | Clear held movement and stop animation |

Walk and run use modern speeds of 3 and 6 m/s with normalized diagonal input and a bounded 100 ms step. A double position accumulator avoids repeated float32 rounding at town coordinates. Feet follow the same retained triangles that are drawn. Crossing a district edge holds position. Original idle, walk and run assets use the existing prepared skeletal path.

Basic obstacles are conservative boxes around transformed original mesh vertices, including all admitted LODs. Swept movement uses a 0.35 m radius and 1.8 m body; closed buildings and some openings remain blocked. These boxes are a modern navigation policy, not recovered original collision. The camera tries shorter segments, raises its endpoint above terrain and checks the final raised segment against boxes. This does not establish continuous terrain occlusion or interior camera behavior.

All potentially applicable original height writers within the district and its 15 m sampling margin are covered. The original generator has 122 height-owning layers; an independent byte-level scope audit excludes 110 and retains layers 0, 3, 4, 5, 146, 148, 151, 153, 155, 157, 159 and 161. Inverted boundary/filter gates and source order are preserved. The unsupported family60 second-curve writer has exactly zero influence throughout this padded district; widening the region requires a new audit. The implementation rejects unsupported applicable writers rather than substituting base terrain.

Complete original numeric terrain parity remains unproved. The existing explicit coordinate, noise, precision and first-curve approximation policies remain modern controls. Five original building pads independently match their authored targets. Terrain uses the original root family's first sand texture; material blending, flora and water remain pending. Scenery uses original MAIN textures and UV0 with modern opaque, straight-alpha, cutout or additive presentation. One original fruit-stand LOD has zero normals; its unlit adapter supplies an up direction because GPU normal compression cannot retain zero. Original CPU data is retained. Original effect programs, bump/specular layers and capability selection are not translated. The window labels these limits.

## Reproduction and acceptance

Use the toolchain inputs described in the [character runbook](DX12_OFFLINE_CLIENT_RUNBOOK.md). Each producer requires a fresh ignored output directory.

```powershell
python tools/python/inspect_mos_taike_inputs.py --asset-root C:\Stardust-DEV --config C:\Stardust-DEV\swgemu_live.cfg --output $FreshInputs
python tools/python/verify_mos_taike_height_scope.py --terrain "$FreshInputs/tatooine.trn" --layers "$FreshInputs/terrain-layers.json" --output $ScopeReceipt
python tools/python/build_offline_dx12_client.py --mos-taike --dotnet $Dotnet --godot $Godot --packages $Packages --templates $Templates --asset-root C:\Stardust-DEV --config C:\Stardust-DEV\swgemu_live.cfg --output $FreshBuild
python tools/python/verify_offline_terrain_client.py --build $FreshBuild --output $FreshVerification --original-snapshot "$FreshInputs/tatooine.ws" --gpu
```

The exported verifier checks bundled runtime and frozen hashes, terrain/controller acceptance, cancellation, malformed profile/option, headless and OpenGL refusal, actual DX12 rendering, original placement bytes, displayed-triangle grounding and walk/run distance. The GPU proof injects synthetic W, Shift+W, D and focus through the same `_Process` controller. It records original clip identities and three captures. This is explicit synthetic input, not a claim that a person physically pressed keys.

All nine exported checks pass for the October 12 final2 package on NVIDIA RTX5060 Laptop, x64 Forward+/D3D12 API12_0. The complete frozen package has193 files. Final proof records541 process updates, idle/walk/run, maximum stored grounding error below9.54e−7m and displacement within0.001m of accepted walk/run time. Independent raw-snapshot verification matches94 exact original placements and nine omissions. The parent additionally replayed the package, checked five original foundation targets and visually accepted recognizable building facades, market stalls, streets, grounded human and readable HUD. All22 default regression checks pass. Other hardware and clean-machine deployment remain unverified.

Implementation boundaries live in the engine-neutral [snapshot decoder](../poc/Stardust.Core/Assets/WorldSnapshotInput.cs), [height traversal](../poc/Stardust.Core/Geometry/OriginalTerrainHeightTraversal.cs), [scene loader](../poc/Stardust.Core/Assets/MosTaikeSceneResources.cs), [obstacle boxes](../poc/Stardust.Core/Geometry/MosTaikeObstacleBoxes.cs) and [movement controller](../poc/Stardust.Core/Session/OfflineTerrainRoaming.cs). The [Godot host](../poc/Stardust.Godot/OfflineTerrainClient.cs) owns input, upload, rendering and cleanup. Exact source evidence, input identity, verification and remaining questions are recorded in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-12_DX12_OFFLINE_MOS_TAIKE.md).
