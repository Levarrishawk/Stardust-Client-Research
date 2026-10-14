# Fresh configured offline preview witness

## Scope and admission

Round81 adds `RecordOfflineFreshPreviewState()` to the configured Version13 host. The single call is at the end of `BeginPointTarget`, after prepared input/camera/frame/root helpers and before its first update. Other versions return without a report. This is replacement-host evidence, not recovered legacy client behavior or a protocol coverage metric.

The partial [implementation](../../SWG-Godot-Client/poc/Stardust.Godot/ScenePumpedOfflineFreshState.cs) checks actual borrowed and owned objects before serializing a primitive-only `offline_fresh_preview_state` JSON record. It does not retain nodes or sessions in the record. `ReferenceEquals` establishes the point/controller and root/transport ownership relationships. Godot instance identifiers support same-process child distinction; identifiers from separate processes may repeat.

## SchemaVersion 1

| Fields | Meaning and initial invariant |
| --- | --- |
| SchemaVersion | Integer 1 |
| PreviewInstanceId, CameraInstanceId, SceneGeneration | Positive integer identities/generation; scene generation is local to this child |
| OwnedPoint, OwnedController, OwnedRoots, OwnedTransport | Actual identity relationships, all true |
| CompletedUpdates, AttemptedUpdates, PointIndex, Speed | Zero; point Mode is `Idle` |
| LocalClock, OfflineStartClock, DriverClock | Equal nonnegative integers; absolute value is not prescribed |
| ManualHeld, RunHeld, ManualX, ManualZ | Zero |
| Running, Paused, FinishRequested, TargetActive | False |
| ClickAttempts, ClickRemaining | Zero attempts; remaining equals supplied MaximumClickAttempts |
| FramePlans, ClampedMicroseconds, FrameSteps, ReservedSteps, RemainderMicroseconds, DroppedMicroseconds | Zero; reservation slots are not completed updates |
| OrbitYaw, OrbitPitch, OrbitDistance | Actual configured pose: IEEE remainder yaw, clamped pitch/distance |
| OrbitSeedYaw, OrbitSeedPitch, OrbitSeedDistance | Supplied profile values, retained as numeric primitives |
| OrbitAccepted, OrbitHandled | Zero; camera revision/pose-attempt counters are deliberately not inferred |
| RootSamples, RootQueued, RootDrained, RootNextSequence, RootRelativeMicroseconds | Zero |
| RootLastNowMilliseconds | -1 initial sentinel |
| QueueCount, QueueBytes, OutboundStepCount, PendingMessages, RetainedPackets, RunDatagrams | Zero |
| TransportState, SocketCreated | `Established`, false; existing in-memory synthetic transport, no socket |

The initialization contract comes from configured helper preparation and their initial states, not a native log. The observed fixture camera starts at yaw 0, pitch 0.6 and distance 18. A fresh camera does not imply zero pitch or distance.

Configured Version13 admission reads both SoeConnectionId and SoeSeed through GetUInt32 before preparation, allowing supplied synthetic generation identities. Earlier versions retain their fixed F00D00DB/A1B2C3D4 fixture pins. Maximum datagram 496 and CRC width 2 remain required. Frame agreement and generation byte validation belong to the transport verifier; this admission change makes no live identity claim.

## Independent verifier and runner

[verify_fresh.py](../../SWG-Godot-Client/poc/round81-tests/Runtime/verify_fresh.py) exports:

- `expected(profile)` returns independently calculated initialization expectations for frozen Version13 policy `SuppliedConfiguredOfflinePlayground`.
- `records(log)` extracts newline `key=JSON` records.
- `verify_runs(logs, profiles, require_distinct_instances=False)` accepts paired strings and profile dictionaries. Pass already segmented one-child logs. It requires one fresh witness and completion report per segment, checks fresh counters, pose, ownership and clocks, then checks configured owner disposal, closed input/orbit hosts and empty queue/transport buffers. Same-process persistent runs can require independently distinct preview and camera identifiers; separate-process runs must not require that.

Results include actual input-intent, speed-intent and noninitial orbit record counts. Those counts do not establish final held masks or camera pose after retirement: the frozen cleanup schema does not expose them. Missing cleanup fails validation. A fault reason is accepted only with the required cleanup evidence. This verifier does not validate root packet bytes or server protocol semantics.

CLI examples, with explicit private inputs and a fresh output under this checkout's ignored `local-private`:

```powershell
python poc/round81-tests/Runtime/verify_fresh.py generate --profile <profile.json> --out <fresh-private-expected.json>
python poc/round81-tests/Runtime/verify_fresh.py verify --profile <profile.json> --log <console.log> --out <fresh-private-receipt.json>
python poc/round81-tests/Runtime/test_verifier.py
python poc/round81-tests/Runtime/run_headless.py --godot <Godot_console.exe> --args <prepared-argv.json> --console-out <fresh-private-console.log>
```

The runner takes a JSON string array of explicit Godot arguments. It requires `--headless`, a private prepared `--path`, and a fresh private `--log-file`; timeout is 45 seconds. Prepare a private copy of the frozen Godot project and sibling preview-profiles, reference the frozen Core project, and build using exact locked offline packages. Preserve the full frozen resource/peer/profile arguments. It does not download packages or fabricate missing original resources. Output guards reuse the existing symlink/reparse-aware private path validator.

## Verified result and limits

Confirmed on 2026-10-06: warning-free full-host build; original-resource headless short fixture completed 3/3 updates; original-resource strict terrain-gap fixture faulted at 225/226 completed/attempted updates. Both passed fresh-state and cleanup verification. The fault fixture recorded two manual intents and two ShiftPressed speed intents, with no noninitial orbit record. Closed cleanup was observed, but final held masks were not reported. Authored synthetic tests cover repeated normal/fault segments and 33 rejection cases; these are parser tests, not native input evidence.

The initial restore failed with missing BCnEncoder and mismatched Godot locked hashes. Exact previously validated offline packages resolved this without changing locks. The first native attempt failed because the isolated project lacked its sibling preview-profiles; copying those public inputs resolved it. Godot also emitted a root-certificate-store diagnostic; no network operation was requested.

Next probe: lead-owned persistent full-host runs should feed individual child log segments into `verify_runs(..., True)`, including held-input retirement and camera perturbation evidence if the frozen cleanup schema supports it. No Rendering81 lifecycle API is imported here.
