# 2026-09-30 connected lower-floor host

## Question and evidence

Does optional connected approach selection preserve existing host controls on the black-sun lower-floor 2/0 ↔ 3/0 doorway? Input SHA-256 pins and independent actor results are in the [connected approach checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_APPROACH.md). Tools: prototype host 4.7.2 .NET x64, bounded offline preview launcher.

`-ConnectedDoorApproach` requires local step coverage, step movement and precise door movement. Forward headless and reverse GPU runs pass manual controls, retreat/reset/contact, two owner poses/reflection, fixed-step pause/focus/stall, physical timestamped keys and diagonal owner round trips. Each direction checks 20 manual transfers; manual step counts are 48/50. Private reverse capture inspected for consistent lower-floor actor/leaf placement. Build passes zero warnings/errors. Known nonfatal certificate-store startup diagnostic persists.

Confidence: Confirmed bounded host selection. Original navigation/spawn policy and arbitrary world collision remain Unknown. No new VA names or protocol rows. Modern fixture definitions remain separate.

## Next action

Implement a bounded engine-neutral persistent actor scene with multiple explicitly paired single leaves. Verify three-cell/two-door traversal, per-direction permission, retained contact teardown, reset/dispose and callback-failure atomicity before an original multi-door probe.
