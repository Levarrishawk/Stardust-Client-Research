# Round18 local input and offline motion integration — 2026-10-02

## Bounded question and inputs

Can the accepted original-resource world host one native fixed-step local driver,
timestamped Godot input, idle/walk/run presentation and queued authoritative
motion without replaying suspension, undoing corrections or reloading resources?
Stop after original headless/GPU/cancellation and independent clock/queue receipts
agree. No original prediction, interpolation, floor collision or parent-world
conversion is claimed.

Round18 baseline:2,310 files, manifest SHA256
023b090af15474a13a76ba1836d59cfc3800baab6dc27057436a165608e8a910;
ZIP e2fe55b4f1a8c4c21da802d28dc922930f8963d002ee38b5c9f71129145fc43d.
Input root C:/Stardust-DEV; configuration SHA256
22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524.
Reuse catalog, explicit human locomotion bank, selected idle and terminal frame0
inspection from Round17. Native motion fixtures are independently handpacked;
keys/events/time in the integration route are labeled synthetic. No new VAs.
Original EXE is neither probed nor run; no live contact.

## Observations and findings

**Confirmed implemented behavior:** [driver](../../docs/SCENE_WORLD_LOCAL_MOTION_CONTRACT.md)
uses the accepted integer scheduler/timeline, native planar Y-yaw and current
creation incarnation. A newly observed authoritative pose replaces its owned
pose before subsequent local ticks while keeping the input clock. Driver borrows
world/store; input host owns the driver; pump owns presentation/world close.
[Input/locomotion adapter](../../docs/SCENE_WORLD_LOCAL_INPUT_CONTRACT.md)
runs from BeforeStep, releases held keys on pause/focus loss and selects supplied
walk/run clips with the existing animation clock. Diagnostic clip selection
finishes its retained crossfade without advancing clip phase.

**Confirmed implemented behavior:** [router](../../docs/SCENE_WORLD_MOTION_INGRESS_CONTRACT.md)
is the pump's optional synchronous applicationReceiver. Pump keeps queue copying,
bounds, stale-generation retirement and failure/drain policy. Receiver borrows
bytes only during the call; dequeued arrays are cleared afterward. Default
receiver remains world.Accept. Router checks current finalized world target,
parent-space rejection, callback freshness/reentry and store serial ordering.
The preview supplies explicit modern zero-yaw/native-position/nonnegative raw
speed projection; this is not recovered original direction/speed semantics.

The original join starts with human39 sources/terminal27, queues Height and
motion, destroys/recreates only the terminal, then performs nine observed stages:
walk, run, pause, correction while paused, resumed run, idle, focus loss,
focus resume and reset. Native Z is reflected once into Godot; Height1.4 composes
once; human resource/GPU identities remain unchanged; total loads/closed archives
stay3/3. Correction is consumed by ingress after a driver update, observed on
the next update, and subsequent local motion starts there. Pause/focus time adds
exact suspended microseconds and no dropped time. Reset restores raw create pose
without changing global motion revision/counter ownership.

**Confirmed source/material configuration, Strongly inferred symptom cause:**
[head finding](../../docs/SCENE_HUMAN_HEAD_MATERIAL_GAP.md) establishes omitted
generated-face preparation in the exact39-source route. Face authored MAIN is
pale with white factors; body and head cap already share the authored skin factor.
Independent DDS hashes agree. Original player's appearance values and expected
complexion remain Unknown. This round diagnoses the gap; it does not fix it.

## Validation and review corrections

- Native local driver90 checks; shared native run also90; prior regression retained.
- Ingress standalone160 checks and independent six-serial/17-gate oracle.
  Shared async smoke context exposed Task.Run owner-thread probe inlining; lead
  replaced it with a dedicated thread and bounded join. Shared ingress161 checks
  and independent oracle now pass. Production router behavior was unchanged.
- Exact original head producer,39-source identities/four MAIN decoded hashes,
  five tampered-receipt negatives and read-only cleanup pass.
- Rendering worker real dispatched key/focus/pause, clip/Core-clock agreement,
  headless/GPU/cancel, blend/reset and owner/disposal gates pass.
- Combined original headless/GPU nine-stage clock/pose/clip/resource oracle,
  continuous frame-budget/lifecycle oracle, cancellation/drain and inspected
  fresh GPU capture pass. Godot build has zero warnings/errors.
- Main native suite runs before registered --round18-local/--round18-ingress
  probes; both pass. Repository gate and exact scope are in the session report.

One lead test sequencing correction waits for the next driver observation after
queue delivery. Resume explicitly anchors the input host at the current timestamp
before advancing fresh time, respecting the scheduler's first-resume no-replay
policy. Default Round17 route remains separate; canceled local mode prints only
its cancellation proof.

## Reproduce and resume

Use [continuous launcher](../../../SWG-Godot-Client/poc/round16-tests/Integration/Launch.ps1) with
-GodotPath cached Godot, -ArchiveRoot original root, -Configuration pinned config,
-FixturePath output of Round16 Integration/Generate-Peer.py, -CatalogProfile
poc/preview-profiles/scene-object-catalog.json, -MotionPeer output of Round17
Integration/Generate-Motion.py and -LocalInput. Default is headless; add -Gpu and
fresh private -CapturePath for GPU; separate -CancelLoad for pending-load cleanup.
Run [independent oracle](../../../SWG-Godot-Client/poc/round18-tests/Integration/verify_local.py) on
normal log and prior Round16 verify_preview.py on normal/cancel logs.
Worker contracts give native producer/verifier commands.

Next bounded milestone: explicit generated-face inspection route with supplied
appearance values, requested-extension validation/accounting, actual material
texture readback and face-region proof. Preserve default route; original
replicated customization semantics remain open. Interior floor/door hosting and
original direction/interpolation are separate next integration risks.
