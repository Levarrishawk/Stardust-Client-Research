# Fresh selected-human baseline admission

Round90 establishes no additional field-order blocker for an outdoor selected human whose values fit the prepared application policies. This is static source correlation, not admission proof for an arbitrary persisted character or live session.

## Evidence

Server paths below are relative to Stardust-3, HEAD `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`, under `MMOCoreORB/src/server/zone/`.

- `objects/creature/CreatureObjectImplementation.cpp:285` sends the owner start-scene sequence and chooses root-parent or direct creature send. `objects/scene/SceneObjectImplementation.cpp:363` sends create, link, baselines, children and optional close. Link returns when parent is absent. The bounded outdoor case has no root parent and requires `doClose=true` for selected EndBaselines.
- `objects/creature/CreatureObjectImplementation.cpp:467` emits owner CREO1/3/4/6. Child sends can interleave before selected close.
- `packets/scene/SceneObjectCreateMessage.h` writes object ID, quaternion XYZW, position XZY, template CRC and hyperspace flag. Normal flag zero is accepted by the raw-flag create codec; the existing fixture flag one is not a prerequisite.
- `packets/creature/CreatureObjectMessage3.h` extends tangible3 with posture, faction rank, link ID, height, shock wounds, state mask and wound vector. Its declared18-field sequence matches the accepted CREO3 reader, including name/customization lengths and vector count/counter ownership.
- `packets/creature/CreatureObjectMessage4.h` emits acceleration pair, encumbrances, skills, speed pair, listen ID, six locomotion floats and mission ID pairs. `objects/scene/variables/DeltaVectorMap.h:140` emits skill count/counter and an ADD byte per baseline entry; `objects/creature/variables/SkillModEntry.h` serializes two signed integers. These agree with the accepted CREO4 grammar.
- `packets/creature/CreatureObjectMessage6.h` extends tangible6 with level, performance/mood strings, IDs/counters, guild/target, performance fields, HAM vectors, wearables, alternate appearance and frozen flag. Declared22 with18 physical fields is already handled. `objects/scene/variables/DeltaVector.h:219` emits count/counter then values; `objects/creature/variables/WearablesDeltaVector.h:107` emits customization, arrangement, object ID and CRC per wearable.

Confidence: **Strongly inferred** source/client layout compatibility. Source order and local admission conditions are direct observations. Server source alone does not prove dated executable behavior; existing typed codec contracts provide client correlation. This round introduces no executable-address claim.

## Readiness dependency

[Selected ingress](SELECTED_PLAYER_SERVER_INGRESS_PROFILE.md) accepts selected CREO3/4/6. `ReplacementSelectedPlayerIngress.cs:48–60` defers other object IDs and selected views outside3/4/6, and accepts selected EndBaselines with mask7. CREO1 supplies no mask bit; separate PLAY traffic is deferred by object ID. The minimum resource/barrier path does not require either snapshot. Their deferral therefore establishes no minimum visible-player/readiness blocker. Broader gameplay and avatar parity remain open.

## Concrete conditional rejection boundary

`poc/Stardust.Core/Session/ReplacementClientApplication.cs:64–66` sets CREO3/4/6 payload limits of1,024 bytes. CREO4/6 readers reject envelopes beyond their configured budget. Server variable-length skill, customization and wearable serialization establishes no such ceiling. A larger selected typed baseline is a concrete rejection boundary, but this investigation did not observe an actual chosen character crossing it.

Implementation consequence: preserve selected create/3/4/6/finalization order; do not add CREO1/PLAY readiness prerequisites based on deferral alone. Before controlled-server validation, measure the chosen character's baseline payload sizes and check finite values, string/collection limits and resource resolution. If limits must change, implement and test that bounded policy in a subsequent assigned round. Missing original resources may independently prevent visibility; this comparison validates no particular appearance or template CRC.

No production code, live contact, capture or general baseline coverage claim is added.
