# Implementation checkpoint: movement-linked interior —2026-10-01

Question: can accepted original-scene movement drive retained idle/walk/run playback while collision remains authoritative? Stop at one original interior with all live controls/resources and failure gates preserved.

## Inputs and observations

Same verified executable/config/object/SAT/LATT/clip pins as [bank checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_ANIMATION_BANK.md); same pinned original mining-outpost POB/scene/door table as [mouth preview](IMPLEMENTATION_CHECKPOINT_2026-10-01_VERTEX_LIT_SPECULAR_PREVIEW.md). [Durable movement contract](../../docs/MOVEMENT_SKELETAL_PREVIEW_CONTRACT.md) describes exact explicit policy, ownership and consumer.

Observation: effective full content147 sources/16,611,843 bytes, each external source resolved once; original2880-tick/24-transfer/182-contact route remains independently verified. Confidence confirmed for original source/geometry/route assertions; dynamic legacy doors are not independently reimplemented by the Python checker. Interpretation: movement presentation can share the existing owned cache without changing compatibility movement outcomes.

Observation: accepted per-tick displacement selects gait and controls phase; blocked ticks select idle. Visual heading reflection, root-feet placement, actor/door state independence and480 retained clip updates pass. Confidence confirmed for replacement behavior. Explicit threshold3 and movement inputs1.6/5.2 are modern preview policy, not recovered legacy graph semantics. Root tracks/messages remain inert.

## Validation and failures

Full native core including synthetic phase/subdivision/reversal/pause/reset/negative cases passes. Native `--original-interior-locomotion-character` plus independent checker verifies147-source union, selected table routes, material/face/shape/normal/specular boundaries and2880 route ticks. Build zero warnings/errors passes.

First headless run failed the new synthetic pause proof because directly emitting Toggled does not change ButtonPressed. Tick policy now reads the existing control handler's Playing state, shared by real UI and emitted-signal proofs. Final GPU log d25b5e53dbc04df8b0e4c986fd04ff06, headless c9b0a1faafe241a6b3a1fdc5c1fb9130 and cancellation7e474aa74b1d483888e197b26ea263f8 pass. GPU original controls and20 specular/3 normal/3 albedo fixtures pass. Private final capture inspected. No private output staged; workspace/link/diff checks run at closure.

## Limits and next action

Hard clip switching remains visible; root/event scheduling, original speed graph and lighting are not claimed. Next: engine-neutral finite joint-pose transitions with retained GPU updates, including interrupted transition continuity and shape/material interaction. Research89%/94%,2,599 names/345 packets unchanged.
