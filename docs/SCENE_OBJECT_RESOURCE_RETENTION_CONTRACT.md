# Scene object resource retention contract

## Bounded result

The accepted modern generic `SceneWorldCoordinator` retains an exact resource publication across state-only CREO:3 Height and opaque TANO updates. Destroy/recreate of the terminal requires a distinct publication ticket, even when its raw create and encoded source assets are identical. The human publication remains current and retains the exact resource reference. These are **Confirmed modern fixture observations**, not claims about legacy executable runtime or rendering.

Evidence: [R15 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R15_RESOURCE_RETENTION.md), [native fixture](../poc/Stardust.Core.SmokeTests/SceneObjectResourceRetentionScenarios.cs), [independent oracle](../poc/round15-tests/Assets/verify_retention.py). Existing boundaries remain in [mixed world](SCENE_MIXED_WORLD_CONTRACT.md) and [pure resource validation](SCENE_OBJECT_RESOURCE_VALIDATION_CONTRACT.md).

## Sequence and observations

The fixture authors synthetic messages for IDs101/202 and uses the original pinned human/terminal catalog recipes. It loads owned resources through the existing router, clears borrowed resolver arrays, closes each readonly overlay, and passes the existing pure validator before transferring results through controlled `TaskCompletionSource` jobs. No new asset reader or protocol codec is introduced.

1. CREO:3 baseline seeds Height4.5. Terminal TANO baseline retains two opaque bytes. Both objects end baselines, cross the minimum barrier and publish independently. Initial loader calls:2.
2. CREO scalar Height becomes -1.75 (`BFE00000`), then negative zero (`80000000`). Each step also appends a two-byte opaque TANO delta. Both batches acknowledge successfully. Loader calls stay2; raw 47-byte creates, complete decoded-resource fingerprints and exact publication/resource references remain unchanged. Height does not rewrite position or quaternion. The terminal retains opaque history; the fixture assigns no meaning to its payload bytes.
3. Terminal destroy immediately invalidates its old ticket while the human stays current. Recreating the same ID with the same create and ending baselines causes exactly one terminal load. A newly loaded owned resource has the same source/mip/decoded fingerprints but a distinct publication ticket/reference. The old ticket remains invalid; the human ticket/reference remains exact and current.
4. Valid original create/options still pass the pure gate after state updates. Altered position and swapped recipe options reject for both objects; altered creature albedo and static frame options reject. Six focused negatives pass. Close drains completed work and invalidates current publications.

The original human closure is39 source reads/4,686,371 bytes,10 textures/80 decoded mips. The terminal is27 reads/1,401,794 bytes,7 textures/62 mips. Initial combined closure:66 reads/6,088,165 bytes/142 mips. One terminal reload makes93 reads/7,489,959 bytes; its62 repeated mips match the initial terminal. Each path is read once per load. Config SHA-256: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

## Implementation consequence and limits

A presentation consumer must preserve the separation between resource ticket currentness and replication revision. State updates may update presentation values without reacquiring resource closures. Recreated objects require fresh ticket checks even when resource hashes match. Pure descriptor validation alone cannot establish generation/incarnation/currentness; the coordinator provides those checks.

The Python oracle independently checks exact synthetic create bit layouts, expected counters, unique source paths, source-byte sums, hash syntax, state bit expectations and matching terminal manifests. C# checks reference identity, currentness, typed state, opaque history, acknowledgements, ownership regression and gate rejection. The oracle does not independently execute those C# semantics. No original decoder campaign is repeated: this milestone asserts retention/equality against the accepted readers, not a new pixel-decoding claim. No GPU, legacy process, server or network execution occurs. No whole-corpus, renderer scaling, deep exclusive ownership or encoded-origin claim is made.

## Registration and reproducibility

Lead registration is an explicit call to `SceneObjectResourceRetentionScenarios.Run(new[]{root,config,catalog,bank})` in the accepted smoke host. The fixture needs the readonly installed overlay and the existing scene-object catalog/human-locomotion profiles. Its own runner accepts exactly those four arguments and prints `retention_receipt=` JSON followed by a PASS line. The oracle takes the captured native output path. Full commands and input identity are in the checkpoint. No shared host or product file is edited here.
