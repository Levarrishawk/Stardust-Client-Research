# Round74: owned integer frame scheduling slots

Date2026-10-06. Question: can a bounded engine-neutral planner carry exact fractional elapsed time, reserve limited fixed-step slots and explicitly drop excess whole steps without confusing reservations with completed motion or refunding correction slots? Stop after independent long frame sequences and final sealed handoff; IDLE until explicit next assignment.

## Orientation, inputs and tools

Trusted sync verifies4404 frozen public paths and preserves HEAD/index/private. First read during pending sync could not yet find latest73 session; after sync completed, reread final accepted73 session. Root instructions, START_HERE/status/ledger/unknown, fixed-step/session-budget durable docs, prior73 checkpoint, relevant timing tools/curated discovery and main74 assignments/allowlist orient this work. Manifest SHA-256 `85118db10a4fc52080f514e0262201a8bdedc3200f6c7809b1b2d103e2040f1e`; archive `3ffc59188b6813da65e21b078f78fc71cbac75142f0787877f0e3a8672c6bbc6`. No original executable/VA dependency.

Eight additions: clock/scenarios/contract/checkpoint and four own runner files. All4404 frozen files, existing schedulers/session budget/core components/shared projects and sibling74 source remain unchanged. Cached Python/.NET/offline packages only; certificate generation disabled. No stage/commit/publication/live/capture/fuzz/download/external mutation/original execution.

Own authored integer fixture needs no private asset/terrain input. Producer computes expected receipts before native and rederives them for verification. Generated inputs/expected/native outputs remain private. No native time/state seeds expectations.

## Evidence and implementation consequence

Confirmed source/native: [clock](../../../SWG-Godot-Client/poc/Stardust.Core/Session/TerrainOfflineFrameClock.cs) admits0..configured frame bound; exact remainder+elapsed division reserves min(due, frame limit, remaining total), drops all excess whole steps, retains fractional modulo. At total cap, zero Steps continues explicit drop/fractional carry. Immutable receipts preserve historical values. Consequence: no catching up dropped time or rearming on correction/host failure.

Confirmed accounting:7,759 frames across six long cases produce12,107 aggregate reserved slots and66,240,000 dropped microseconds across separate clocks. Exact conservation checks supplied elapsed against reserved*step+drop+fraction.100Hz/60Hz integer partition, zero/mixed fraction,250ms late/eight-slot drop, seven-slot cap, one-slot frames and maximum100ms/6000-cap all pass. Simulated caller skipped slots are not refunded; caller-completed counts remain separate. No actual movement/presentation completion claim follows from reservations.

Confirmed rejection/lifecycle:15,724 checks/92 negatives cover malformed construction/input, negative/oversized/long.MaxValue delta, pre-cancel, owner getter/mutation/disposal, terminal Advance rejection and historical State/Exhausted survival. Checked cumulative overflow preflight is source-inspected: impractically many bounded calls are needed to exhaust signed64; no private reflection/state hook introduced. Reentry/post-check cancellation/rare allocation paths also remain source-inspected. Candidate/token checks precede owned replacement; no rollback.

Modern policy: exact integer microseconds enter the planner; floating Godot delta conversion/clamping is caller work. Caller-clipped time is distinct from planner dropped whole steps. Correction-only/unused final slots stay reserved, while completed driver/observer/follow counts are recorded elsewhere. The planner owns no engine/row/network/history and changes no per-step limits. Original timer/control/gameplay/cadence parity remains Unknown.

## Validation and resume

[Scenarios](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainOfflineFrameClockScenarios.cs) and [independent reference](../../../SWG-Godot-Client/poc/round74-tests/Runtime/verify_frames.py) pass on first native/numeric execution. One offline build zero warnings/errors. No optional actual driver/observer/follow join or unrelated broad/default/Godot/GPU/live/original executable run is claimed. Main will perform the caller join independently.

Replay: generate into fresh private input/expected files; offline restore/build [runner](../../../SWG-Godot-Client/poc/round74-tests/Runtime/Runtime.csproj); execute actual DLL into fresh output; independently verify a fresh receipt. Final private handoff records eight hashes, exact commands/UTC/receipt and4404 frozen SHA/length/path inventory plus UTF8/AST/XML/PowerShell/links/diff/validator/HEAD/index/private-ignore gates.

Next probe: lead reviews/replays stable API and supplies integer frame sequence0/5000/15000/33000/250000 in deterministic headless mode or explicitly converted/clamped measured render delta in GPU mode. Consume reservations through existing exact-step components while separately counting correction-only/unused slots and completed core work; preserve owner cancellation and scene teardown. Worker IDLE after74; no75 without explicit assignment. Research/readiness/function/protocol denominators remain separate and unchanged.
