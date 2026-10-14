# R17 native motion integration - October2,2026

## Question, identity and method

Can exact compact movement bytes drive current native poses through the continuous
original scene without reloading assets, compounding Height or retaining stale
incarnations? Stop after independent scoped review and joined wire/native/body/
Prototype host/cleanup gates. [Round17](../sessions/PARALLEL_DEVELOPMENT_ROUND_17_2026-10-02.md)
records the2,272-file baseline, identities, assignments and acceptance.

Original root/config and explicit human/terminal recipes are unchanged from16.
No executable addresses are newly relied upon or original executable run.
Local server serializers correlate known wire layouts; they do not prove client
yaw, speed units, interpolation or physical participation.

## Evidence and conclusions

- [Compact codec](../../docs/COMPACT_SCENE_TRANSFORM_CONTRACT.md) preserves
  exact26/34-byte branches, count8, signed raw coordinates/speed/direction,
  counter/IDs and parent-zero distinction.522 independent positive fixtures and
  19,324 malformed probes pass standalone and shared, with bit-exact native
  coordinate axes/scales. Original yaw/physical-speed interpretation is deferred.
- [Motion store](../../docs/SCENE_WORLD_OBJECT_MOTION_CONTRACT.md):280 native
  checks and64 serial-boundary pairs pass standalone/shared. Native rigid poses,
  exact current incarnation, bounded rows, independent replication/resource
  lifetime, global monotonic motion revisions and modern half-range U32 freshness
  are verified. It borrows the world and performs no asset load.
- [Body bridge](../../docs/SCENE_CREATURE_BODY_ENVELOPE_CONTRACT.md): eight
  samples/36 negatives and independent float32/provenance oracle pass. Original
  human profile5 sources/8,290 bytes already present in the39-source closure;
  no further reads or decoding. Local center/radius at Height1.4 are(0,1.4,0)/0.7.
  It does not select original active collision participation or implement physics.
- [Visual adapter](../../docs/SCENE_WORLD_OBJECT_MOTION_VISUAL_CONTRACT.md):
  original standalone native/GPU/headless/cancel, success-only ownership,
  detached-parent/near-shear/current-invalid/state-reset/recreate/cleanup gates
  pass in main. Five roots allocate/dispose. Independent image/state oracle
  finds21,158 human and30,705 terminal motion pixels while resource identities
  persist; original Height-only regions change14,333/0, recreation identical.
- Lead optional --motion-peer path in the continuous preview joins all four.
  Independent synthetic quarter-unit messages yield native(-0.5,0,1), then
  (-1.5,0,-1) across counterFFFFFFFE to0; FFFFFFFF after0 rejects. Clearing motion
  restores create placement; newer local(-0.75,0,0.5) resumes. Engine reflects Z
  once, Height1.4 composes once, raw create/resource identity persists, terminal
  alone reloads, and the body receipt matches the loaded profile. Headless/GPU
  receipts and real archive cancellation pass. A fresh private GPU capture was
  inspected; known face/material limitations persist.
- prototype host build has zero warnings/errors; shared baseline suite passes. Prelaunch
  negative verifier rejects five modes/destinations without process/capture.

Observation: deterministic native/engine/body receipts and original resource
closures agree with independent authored bytes and numeric checks.
Interpretation: reusable selected-world motion presentation works under explicit
modern immediate-pose policy. Confidence: Confirmed bounded implementation.
Consequence: wire/asset preservation can coexist with independent local poses.
This is not original interpolation, live connectivity, full world ingress,
locomotion switching or interior traversal.

## Review correction and limits

Clear originally reset same-incarnation revisions to1, which could be ignored by
a visual adapter guarding a later revision. The store now retains one checked
store-wide sequence across Clear/Prune and advances only on successful commit.
Native checks and joined clear/reset/local-resume verify the correction.
Existing transform validator already rejects near shear; no duplicate production
orthogonality check was required. Constructor verifies live attachment before
ownership; rejected inner remains caller-owned.

The lead fixture explicitly chooses zero yaw and uses the raw positive speed
as a diagnostic value, without claiming recovered speed units. Parent-relative
messages decode structurally but are not resolved/applied here. Movement is fed
separately to the store; baseline world.Accept still rejects these opcodes.
Current native store finite bounds differ from renderer origin limits; adapter
guards remain authoritative at that engine boundary. No original physics claim.

## Reproduction and next action

Worker checkpoints give exact commands. Runtime runner takes accepted classic
peer/export; Assets runner takes root/config/catalog/bank then body oracle LOG;
Networking peer.py generates and verifies native runner peer/export. Shared flags
--round17-motion, --round17-body and --round17-transform preserve these inputs.
Rendering own generator/native runner/oracle and Launch/verify_preview/
Verify-Launch commands are in its checkpoint.

Lead: generate round16 Integration peer, then round17 Integration/Generate-Motion.py
BASE_PEER MOTION_PEER. Existing round16 Integration/Launch.ps1 accepts -MotionPeer MOTION_PEER, optional -Gpu/-CapturePath fresh private PNG.
verify_motion.py LOG MOTION_PEER checks wire/native/engine/body/reset observations;
accepted16 verify_preview.py LOG checks continuous budgets/lifecycle/drain.
Separate -CancelLoad remains original job-cancellation proof.

Next: add bounded input/fixed-step local movement, locomotion switching and unified
offline ingress before interior binding. Investigate remaining face/material gap
on this exact sample rather than expanding the corpus. Keep original/live policy
and independent research/function/protocol denominators separate.
