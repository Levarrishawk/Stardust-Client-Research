# Predawn multi-block analysis session

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
Server correlation: local Stardust/Core3 source tree  
Scope: static analysis, asset-corpus correlation, documentation reconciliation; no replacement-client implementation

## Outcome

This session completed 75 bounded checkpoint documents and then performed a formal reconciliation pass. The work expanded the whole-client picture beyond the already mature transport, replication, renderer, terrain and asset foundations. It did not equate packet closure with whole-client closure.

The conservative comprehensive estimate advances from approximately **78% to 82%**. The server-source packet matrix is independently complete at **345 documented rows and 0 reachable gaps** for the current source inventory. These are separate denominators.

## Material closures

### Platform, lifecycle and runtime services

- reconstructed CUI mediator activation/deactivation ownership and enumerated mediator classes;
- mapped camera families and free/chase-camera behavior;
- closed input dispatch, command-table/queue policy, console grammar and toolbar actions;
- documented crash/reporting policy, memory manager/pools, and thread/scheduler topology;
- closed localization/STF lookup, grammar/string-variable substitution, language selection and macro/social catalog loading.

### Gameplay-facing managers

- recovered region, planet-map, quest-journal, skill-progression, attribute/modifier, activity/permission and draft-schematic bridges;
- mapped mount/saddle presentation, combat-special-move visuals, object attributes, client effects, client events and animation callbacks;
- separated collision callbacks from gameplay state mutation and documented terrain dirty-region callers.

### Rendering, terrain and presentation

- narrowed terrain work scheduling, seam payloads, renderable dispatch, vertex/blend data, collision policy and flora residency/randomness;
- closed local-water shader contracts, switch shaders, swoosh template/runtime rendering, projectile management and ship projectile subclasses;
- reconstructed client weapon presentation and ship weapon-group behavior.

### ClientDataFile and original assets

The largest late-session block recovered the `ClientDataFile` dispatcher, application order and principal record families using executable control flow plus configured-overlay TRE/IFF corpus evidence. Covered families include:

- child-object composition and transforms/hardpoints;
- damage appearances, destruction effects and ship destruction sequences;
- events, customization entries and skeletal wearables;
- animated ship wings, contrails, turrets, flags and sun tracking;
- glow/on-off attachments and point/flicker lights;
- clear-flora exclusion circles;
- ship-weapon barrel recoil dynamics;
- discrete and continuously interpolated engine audio.

The final asset-runtime block additionally closes the seven GLOW floats as RGBA plus an asymmetric three-point angular size profile, including camera-side shader selection and billboard-quad generation.

The final wing-runtime trace confirms the second `WING` scalar is transition duration in seconds and recovers the state-change, one-shot sound and `elapsed / duration` interpolation behavior.

The child-controller trace resolves the common 68-byte child payload's motion fields through exact RTTI classes and update equations: continuous `RotationDynamics`, angular `SeesawDynamics`, and positional `SpringDynamics`.

The turret-runtime trace closes the final TURR scalars as projectile speed and lifetime and identifies the live controller as `TurretObject` through RTTI.

The final destruction-runtime trace identifies `ShipObjectDestruction` through RTTI and confirms the staged split, dynamic-debris, effect and terminal-cleanup lifecycle. It deliberately leaves individual DSEF scalar labels open where the helper chain does not yet justify names.

The contrail-runtime trace closes B/C as start/end hardpoints and the final scalar as full Swoosh ribbon width; only the exact domain label for selector A and parent-remap field D remains open.

This materially strengthens the non-negotiable requirement that a replacement consume original SWG assets directly. The recommended architecture remains a typed, fixed-width file decoder feeding engine-neutral presentation records rather than embedding native pointers or renderer objects in serialized structures.

## Documentation and hygiene reconciliation

- Corrected the canonical packet count from the stale 284/61 snapshot to 345/0.
- Normalized `provisional_names.csv` to one row per address: **1,597 distinct addresses**, **43 duplicate rows removed**. The newest observation wins; the chronological Markdown ledger remains intact for auditability.
- Corrected the CSV header to expose the evidence column explicitly.
- Updated the comprehensive estimate and entry-point documentation to 82% while retaining 93% as the narrower replacement-decision readiness estimate.
- Corrected stale network prose that still described the live socket mechanism as unknown despite later UDP call-graph proof.
- Preserved raw corpus inventories, scripts and decompiler outputs as reproducibility evidence. They are not primary review documents, but deleting them would force rediscovery.
- The final destination-side audit caught and corrected a stale canonical packet CSV (284/61) and three checkpoint files omitted by a date-only sync. A full top-level missing/newer reconciliation leaves the canonical D: workspace at **345 documented packet rows**, **75 dated 2026-09-21 checkpoints**, and no stray `stage_*` files.

## Remaining material unknowns

The remaining work is not a single list of 18% of raw functions. It consists chiefly of:

1. exact semantics for rare presentation scalars and extension fields, including some glow, contrail, turret, child-controller and ship-destruction values;
2. long-tail UI mediator and gameplay workflows not required by the narrow networking proof of concept;
3. exhaustive ownership, cancellation and failure-path audits around asynchronous loading and scene transitions;
4. rare or tolerated asset extensions that this executable skips or only partially consumes;
5. further specialized simulation and presentation behavior, especially space and animation exceptions.

Exact historical class/member spelling often cannot be recovered from a stripped optimized executable. A behaviorally exact contract with a stable provisional name is considered a valid closure; invented original names are not.

## Resume recommendation

Continue with the remaining rare `ClientDataFile` runtime consumers, prioritizing fields whose values affect visible behavior. Then perform workflow-based UI/gameplay closure and a final cross-document contradiction audit. Do not begin a full client build solely because the packet inventory is closed. Engine selection and the narrow x64 networking proof of concept should follow only when the user explicitly returns to the build decision.
