# Pre-CU attribute index adapter

Date: October14,2026. A semantic read adapter for the NGE source client; the legacy replicated arrays retain their original order and values.

## Observation and confidence

Original research records nine HAM attributes in the stat migration constructor00DB8960 and reset00DB9EC0; [replication schema](REPLICATION_SCHEMA_MAP.md) retains indexed creature current/max/wounds arrays. Core3 BuffAttribute.h orders health/strength/constitution/action/quickness/stamina/mind/focus/willpower. Source swgSharedUtility/src/shared/Attributes.cpp itself retains that nine-name table, but Attributes.def exposes only health0, constitution1, action2, stamina3, mind4 and willpower5. Source CreatureObject getters index the incoming vectors directly. **Confirmed named source/server ordering mismatch** and original nine-row research; raw decoder widths are not changed here.

The live HUD before adaptation shows shortened secondary pool bars. Reading source action2/mind4 from the nine-element wire arrays obtains constitution/quickness, rather than action3/mind6. This establishes incorrect value selection, but does not establish the correct bar-width policy. The original stat reset probe alone does not prove every current-max/wound accessor or arithmetic rule.

## Compatibility policy

The engine-independent PrecuAttributeProfile maps the existing six source semantics to wire positions0,2,3,5,6,8. Source CreatureObject captures the explicit Pre-CU replication profile at construction, sizes initial current/max/unmodified/bonus arrays to nine for that profile, and maps the existing semantic getters and offline reset producer. False retains six-element initialization and identity indices. The shared localization helpers apply the same mapping for Pre-CU labels.

Strength, quickness and focus remain present in the wire arrays but are not newly invented source enum names or exposed as six-model fields. A future original nine-row attribute UI needs an explicit raw-nine presentation adapter. Values outside the six semantic indices remain unchanged by the pure mapping helper; it is not a wire validation or malformed-state recovery layer. Existing wounds, maximum/buff/level arithmetic and damage trails are not changed or declared equivalent.

## Validation and next gate

Native fixtures decode a synthetic production nine-value AutoDeltaVector with distinct values and assert independent pool/secondary semantic positions, preserved nine entries, six-model identity and profile counts. Full getter/UI lifecycle and wounded/buffed presentation need live evidence. Next launch: same stationary character, verify independently constrained Health/Action frames and the intentional absence of Mind. [Daytime checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md).

## Stardust presentation requirement

The user explicitly clarifies that Stardust intentionally hides Mind. The replicated Mind value/index and optional original widget remain supported, while ClientUserInterface.showPrecuMindBar defaults false and is explicitly false in the authored profile. Other projects can opt into its display. This presentation choice does not discard the data or disable server mechanics. The earlier missing-Mind investigation is superseded; its unbuilt diagnostics are removed. True-option UI fidelity is not newly live-verified.


The later [original sheet bridge](NGE_PRECU_CHARACTER_SHEET_CONTRACT.md) exposes a separate raw-nine accessor and independent encumbrance projection. Core3 emits three encumbrance values, so requiring equal HAM/encumbrance lengths incorrectly blanked Action and later rows. The corrected display validates all six authored rows while preserving Mind's intentional omission. This does not widen the enum, change the wire vectors or claim full original wounds/modifier arithmetic.

## Independently constrained HUD frames — October14 correction

**Observation:** the user supplies the original Stardust HUD screenshot dated09:40:48 and explicitly states that each bar has a constrained width. Source SwgCuiStatusGround passes the largest pool maximum as scaleMax; the previous PrecuHam::project divided each pool's normal/current values by that shared maximum. A full Action pool therefore shrank when Health had a larger maximum. **Confirmed source coupling and explicit target requirement.** The screenshot alone does not establish wound/trail timing or every resize rule.

**Implementation consequence:** the pure Pre-CU projection now takes only that bar's normalMax/currentMax/current. Positive normalMax occupies the full authored frame; fill is current/normalMax, capped by currentMax/normalMax. Zero/negative denominators and values remain bounded. The adapter preserves each widget's existing frame/orientation/margins; it adds no common hardcoded pixel width. NGE rendering, semantic index mapping and hidden Mind are unchanged. Wound arithmetic remains the existing bounded policy, not newly recovered original parity.

Unequal full2500/2000 pools, changed Health5000 with unchanged Action, half-full Action, wound caps, invalid denominators and integer limits pass the native suite:10847 checks/2997 malformed rejections plus five isolated cases. The Release HUD library compiles. Executable relink/deployment and live visual verification remain pending because the prior source client is still running and computer use reports the earlier physical Escape stop despite renewed user authorization. No UI retry or forced closure follows that stop. The user also flags radar/minimap resizing for a later separate acceptance gate; it is recorded, not fixed here.


Live correction acceptance: after computer use resumes, the constrained-width client is relinked/deployed. Its first retry fails in a distinct spatial-chat codec. After that researched correction, buildB78D8649F02B17AB05DBB0F421008B8D40F5053D284F406CF228E87E5C7444D1 renders full Health/Action frames with different real maxima confirmed in the original Character sheet; Mind remains hidden. This establishes the requested full-pool independent width behavior, not wounded/trail/resize parity.
