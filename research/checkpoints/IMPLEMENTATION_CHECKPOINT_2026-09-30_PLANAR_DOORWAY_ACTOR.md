# 2026-09-30 — Planar doorway movement

## Question and inputs

Can normalized native planar axes drive lateral and diagonal movement through the selected original doorway, including transformed owners and explicit floor stops? Close after native, independent original and headless/GPU key-routing checks.

Executable identity remains SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rechecked this run. No new addresses. Original root/configuration, 48 mounts and six source pins are unchanged from the [instance checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md); configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

Observation: native up-cross-forward right and normalized axes agree with an independently calculated original lateral/diagonal trajectory. Confidence: Confirmed replacement mapping and fixture behavior. Original control/camera policy remains Unknown. Consequence: keep mapping engine-neutral and preserve reflection at the prototype host boundary. The [durable contract](../../docs/PLANAR_DOORWAY_ACTOR_CONTRACT.md) explicitly limits floor navigation to a feet query and door clearance to actor boxes.

## Verification

> Historical prototype-specific command or code example omitted from the research-only documentation.


Full core smoke passes; prototype host builds with zero warnings/errors. Independent original comparison: 268 ticks, four transfers, two owner poses, six phases, zero discrepancies. Synthetic 100 axis/basis combinations plus invalid-input and transformed floor-boundary gates pass. An extreme-basis test exposed intermediate float overflow; promoting multiplication before addition to double fixes it and the negative case remains covered.

Prior bounded launcher with `-DoorWalk` passes headless/GPU W/A/S/D cancellation, normalized diagonal, timestamped events, two full diagonal square round trips and reflected actor placement. Capture inspected; detailed proprietary-derived traces remain private. Known nonfatal certificate-store diagnostic remains. No original executable execution, live server or publication.

## Next probe

Inventory installed door style variants and their authored primary/secondary/flip/force-field requirements. Select a second original normal style with a matching POB pair to test generalization before expanding unsupported variants. Moving-door overlap, full world collision and legacy actor order remain open. Scores 89% / 94%, names 2,599 and packets 345 unchanged.
