# Round67: viewport input, target clear and offline root motion

Date: October6,2026. Status: all execution gates passed; final workspace acceptance recorded in the session.

## Question and stopping condition

Can the original human/terminal scene receive actual prototype host viewport events through
the reusable point-input Node, clear target intent for three active zero-speed ticks,
retarget and adopt its queued correction, then encode selected actual local rows as
explicit caller-owned outbound root messages? Stop after headless/GPU trajectory
and wire checks, invalid hosted-clock cleanup and unchanged Version2 regression.

## Identity, evidence and implementation boundary

Accepted66 frozen input:4157 public files, manifest SHA-256
`d209e9122ef2609ba8b0b9834d206ba86a5a2368fc6ef5adcc74fdafdd96b53a`.
Original executable SHA
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
is checked only for bounded static asset evidence, never executed. Original object,
appearance, skeletal/mesh/material and diagnostic texture chains retain accepted
pinned identities. Supplied terrain is synthetic; events/packets are constructed
offline. No user/server input, packet capture, fuzzing, download or publication.

Version3`SuppliedViewportPointTargetClearAndOfflineRootMotion` retains fixed10ms,
128-update, pause5/resume8, retarget10 and queued correction20 policy. It explicitly
adds clear15/retarget18 and caller flags27,counter base1000,timestamp bitsF1234567.
These values are authored modern test policy, not recovered sender cadence/units.
Version1/2 remain accepted alternatives in the common launch/profile admission.

The [new Node](../../docs/TERRAIN_POINT_TARGET_INPUT_HOST_CONTRACT.md) receives
`Viewport.PushInput` through actual `_UnhandledInput`; the lead does not bypass it
with direct Dispatch. It borrows actual adapter/current-clock supplier. Ignored
events do not read the clock or charge queries. Successful event diagnostics retain
the exact adapter result and mark the viewport handled. Node Close releases its
references/processing before adapter/controller/picker disposal and owns none of
their resources.

The [clear operation](../../docs/SCENE_WORLD_TERRAIN_POINT_TARGET_CLEAR_CONTRACT.md)
changes owned target intent only. Exact driver result, clock, motion row and
successful-target count remain unchanged; previous target becomes non-current.
Three ordinary idle ticks ground/publish zero speed, then a third actual camera
event issues the next monotonic revision. Clearing never refunds target budget.
Queued correction still processes before the subsequent steering tick.

The [outbound builder](../../docs/SCENE_WORLD_CLIENT_ROOT_MOTION_CONTRACT.md)
consumes actual current LOCAL rows under explicit finite position/speed bounds.
It rejects authoritative rows; accepts caller flags/counter/raw uint timestamp;
converts proper native column-vector rows to a normalized quaternion and delegates
existing62-byte X/Z/Y packing. The lead samples moving, paused, cleared-idle,
post-correction and stopped rows and clears each caller-owned output buffer after
recording its private fixture. No packet is queued or sent.

## Partial effects and confidence

Lead review flagged two precise boundaries. Clock-reader caught host reentry is
checked before the adapter can pick. Late marker callback host reentry may be
observed only after actual child pick AND target commit; host diagnostics retain
that completed result while its own accepted/handled counts do not advance. The
worker tests both phases and documents the difference, with no rollback claim.

Malformed numerical rows rejected by the actual motion store or forged-row identity
checks do not dynamically cover unreachable builder numerical guards. Tests and
documents distinguish those inherited/source-inspected guards from actual lower
caller-bound rejection and current-row conversion. World `DrainChanges` only
inspects a batch; builder leaves its acknowledgement/contents unchanged.

Invalid hosted clock rejects before any picker/adapter charge or target publication.
An earlier actual controller constructor may already publish initial grounded local
motion. Whole-host failure closes world before cleanup; that initial driver prefix
is not rolled back or described as no-motion publication.

Modern fixture behavior is Confirmed where executed, bounded source guards are
source-inspected, original input/movement/pause/sender units/cadence/interpolation
and terrain association remain Unknown. No coverage-score/name/packet denominator
changes follow.

## Reproduction, validation and next action

the separate implementation record
independently intersects three captured camera rays with Fraction geometry, casts
expected targets to binary32 and calculates movement, paused/idle ticks, grounding,
speed, clock and orientation. It independently packs packet metadata and checks
wire-native XYZ/speed and quaternion basis against that calculated trajectory,
without using native pose/hit output as the expected seed. Component outbound
fixtures separately pin exact frame bytes and sign-equivalent near180 rotation.
Clock failure peer (external historical reference)
checks actual event fault, zero query/target attempts and all inner/outer owner drains.
The unchanged66 oracle remains a separate Version2 regression gate.

Lead component replays pass59 clear updates/256 checks/37 negatives, unchanged71
pause and111 unpaused updates, and nine outbound cases/93 checks/eight exact whole
frames. Rendering lead replay passes73 headless/74 GPU checks with five independent camera
projections and four binary32 target casts per mode. Assets passes58 anchors,150
negative guards,23 independent lookup checks and35 conditional original-record
witnesses. Joined headless/GPU each pass102 updates, three actual viewport inputs,
three clear-idle ticks, three paused ticks, queued correction/duplicate and five
offline root packets checked against independent geometry/trajectory/basis/wire.
Invalid clock faults before child attempts and drains all owners. Unchanged66
Version2 passes102 updates. Default smoke18 events/zero failures and normal prototype host
build zero warnings/errors pass. All30 final worker/current/main hashes match.
The first joined build found a lead nullable annotation; fixed before these runs,
failed log retained. GPU capture inspected original human/terminal on textured
diagnostic terrain; no original fidelity or joined marker-pixel oracle is claimed.
Scope/text/links and final timing are recorded in the session.

Next: finalize canonical workspace gates before another bounded round. Original static
record lookup establishes an ordered first-zero match or zero output, not resource
loading; follow its precise first unresolved caller/resource boundary separately.
