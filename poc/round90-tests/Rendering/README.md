# Original-resource Godot client-host proof

The production [host](../../Stardust.Godot/ReplacementClientHost.cs) has actual
login, password, character list, selection, movement and close controls.
[Contract](../../../docs/REPLACEMENT_GODOT_CLIENT_HOST_CONTRACT.md) defines transfer
and completion ownership. The standalone scene requires explicit caller
configuration; it starts no connection by itself.

This harness injects authored offline peers and native Godot control/key events.
It does not use physical OS input, sockets, DNS or the original executable.
Original human/animation/texture/PTAT resources come only from explicit local
installation inputs. No full procedural terrain or gameplay claim follows.

```powershell
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' ./poc/round90-tests/Rendering/replay.py --dotnet 'C:/Program Files/dotnet/dotnet.exe' --godot 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --packages 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round49/godot-packages' --asset-root 'C:/Stardust-DEV' --config 'C:/Stardust-DEV/swgemu_live.cfg' --movement-source 'C:/Users/ancie/Documents/GitHub/Stardust-Client/poc/Stardust.Core/Session/ReplacementControlledMovement.cs' --movement-sha256 af703ee74bd028242e73f5d79e41b328119f00279132b2ef7240eadddf7cbf79 --output "$PWD/local-private/client-host90-new"
```

Choose a fresh ignored output each time. Movement source and its SHA-256 are an
explicit reviewed Runtime90 dependency, copied only to the private source stage.
The worker never imports it into its public Core tree. `SourceHashes` records own
and frozen public dependencies; `BorrowedDependencies` records that separate
pacer pin. The launcher verifies both remain unchanged during execution. The
lead integrated pacer may already be part of its public source inventory.

The launcher authors independent fixtures before building the staged actual
Godot/Core sources, with clear-only local bundled SDK and cached NuGet packages,
warnings-as-errors and private build outputs. All processes use bounded180s
descendant-owned commands. No toolchain or global cache is modified.

`verification.json` contains Passed, SourceHashes, BorrowedDependencies, Commands
with actual statuses and Receipts with Passed/Clean/OriginalResources. Required
accepted89 modes remain success, cancel, terrain-failure, send-error and
send-timeout; stop, window-close and retained-node native-exit are additional
host cases. Clean means actual ownership safely finalized with no outstanding
jobs/ports/native actor. The provider-fault case separately records unsuccessful
drain and its observed fault. A successful test of that negative does not turn
the provider task into a successful completion.

Success uses actual login fields/authenticate/select buttons, unsupported
selection rejection, a disabled/guarded movement-button attempt before received
ready, then a Godot-injected W event. Stop and close buttons are actual production
signals. Window-close invokes the production notification and remains alive until
the composition owner observes host Completion and quits. Native-exit removes
the host while retaining nodes and the engine loop, waits completion, then frees.
Immediate forced freeing of a live hierarchy is explicitly outside successful
graceful-close evidence and retains observed cleanup faults.

Native peer comparisons verify actual protected wires against pre-native Python
expectations. Python verifies actor float32 position bits and >=400ms accepted
root spacing, same native actor identity, original preparation and every loaded
closure-source/terrain hash. No native output seeds the expected bytes.

Add `--gpu` for an explicitly authorized visible success case, original upload
verification and `client-host.png`; omitted GPU remains unverified. Worker
verification is headless. Live compatibility, complete server baseline support,
floor/validated-state provenance and server-observed disconnect remain unverified.
