# Creature native local body envelope

## API and policy

[SceneCreatureBodyEnvelope.Create](../../SWG-Godot-Client/poc/Stardust.Core/Assets/SceneCreatureBodyEnvelope.cs) accepts an already loaded `SceneObjectResources`, `float height` and optional `bool specialDimensions=false`. It returns a sealed get-only `SceneCreatureBodyEnvelopeReceipt`. The receipt contains native `LocalCenter`/`Radius`, Height/mode, scaled StepHeight/CollisionHeight/Length/WarpTolerance, root/appearance paths and source identities, plus detached read-only resolved-field/profile-source provenance.

The helper is pure: no I/O, resolver, decoder, engine, solver, motion source or registry is called. It retains no input resource/profile reference or payload array. The only copies are small immutable provenance records/maps. Caller access must be serialized over passive owned descriptors; concurrent mutation and hostile collection callbacks are outside the contract. Callers apply full [resource validation](SCENE_OBJECT_RESOURCE_VALIDATION_CONTRACT.md) and publication-currentness guards separately.

Height must be finite in inclusive `[0.25,2]`, matching the accepted modern [visual Height policy](../../SWG-Godot-Client/poc/Stardust.Godot/SceneCreatureStatePresentation.cs). This is a modern guard, not the original scalar codec's acceptance range. Null input throws `ArgumentNullException`; invalid Height throws `ArgumentOutOfRangeException`; incomplete/static/mismatched profile metadata or invalid/unrepresentable dimensions throw `InvalidDataException`. No invalid receipt escapes.

## Geometry and identity

The helper calls `resources.Creature.Creature.Profile.Collision.CreateFallbackSphere(height,specialDimensions)` directly. The [recovered fallback contract](OBJECT_TEMPLATE_COLLISION_PROFILE_CONTRACT.md) and [original evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_CREATURE_COLLISION_FALLBACK.md) define ordinary local center `(offsetX*h,(stepHeight+radius)*h,offsetZ*h)` and radius `radius*h`. Special mode substitutes step1/radius0.5 before scaling. Receipt StepHeight is the **effective** step, including that substitution; CollisionHeight, Length and WarpTolerance scale their authored profile values in both modes. Special mode preserves authored offsets. It does not reinterpret collisionHeight as the sphere's vertical center.

Finite nonnegative step/radius/warp, finite signed offsets and finite positive collisionHeight/length are required. The resulting radius and scaled collisionHeight/length must be finite and strictly positive; scaled step/warp must be finite/nonnegative. Float overflow and radius underflow to zero reject. Ordinary zero radius rejects; special mode may accept authored zero radius because its recovered substitute produces a positive sphere. These positive-volume constraints are explicit modern policy beyond the deterministic profile reader's nonnegative-field grammar.

Only CreatureDetail0 results are accepted. Create CRC and recipe CRC, profile root and recipe root, common root/appearance source pins/paths and profile source identities must agree. All five original inherited sources remain provenance, not appearance data. The eight required field provenance entries must resolve within profile sources with SCOT/SHOT family and four-character version metadata. These are metadata consistency checks, not independent encoded-origin attestation or a second template parser.

The receipt excludes world orientation and translation, parent/cell transforms, floor/door contacts and active physical participation. Later callers apply an authoritative native pose. Local Z is preserved with its authored sign; no ad hoc Z reflection is performed. Creating the receipt does not replace the current actor volume or modify the solver. Original controller participation remains **Unknown**.

## Bounded evidence

[Native scenario](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneCreatureBodyEnvelopeScenarios.cs), [independent float32 oracle](../../SWG-Godot-Client/poc/round17-tests/Assets/verify_body_envelope.py) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R17_BODY_ENVELOPE.md) establish **Confirmed modern behavior** from the accepted original39-source human detail0/albedo closure. Its loaded profile has five inherited sources/8,290 bytes. No additional resolver reads occur after archives close and borrowed buffers clear; the original complete resource fingerprint remains unchanged.

| Height | Ordinary center / radius / effective step | Special center / radius / effective step |
| ---: | --- | --- |
| 1 | `(0,1,0)` / `0.5` / `0.5` | `(0,1.5,0)` / `0.5` / `1` |
| 1.4 | `(0,1.4,0)` / `0.7` / `0.7` | `(0,2.1,0)` / `0.7` / `1.4` |

Values in this table are readable decimal forms of tested float32 results. The original collisionHeight1.8, length1.5 and warp17 scale by Height. All eight combinations of heights1/1.4/0.25/2 and ordinary/special mode pass. Dedicated worker results match the owner thread. Changing create rotation/translation has no effect. A synthetic signed-offset profile produces native X/Z `(2*h,-3*h)` unchanged in sign.

Thirty-six negatives cover null/static/incomplete inputs, seven invalid Heights, root/appearance/CRC/pin/provenance disagreement, invalid dimensions and represented overflow/underflow, plus mutation attempts on receipt provenance maps. Small copied profile dictionaries may be cleared after creation without altering the receipt; all receipt properties are get-only. Internal sealed result constructors are invoked by reflection only in the fixture, following the accepted R14 pattern; no production construction or mutation API is added.

The Python oracle independently defines float32 arithmetic and known original profile values/provenance, verifies eight sample receipts and expected negative/accounting metadata, and reads no assets. It does not independently execute native reference ownership or exceptions. Original parsing/provenance authority remains the accepted deterministic profile fixture; no corpus or parser campaign is repeated.

## Integration point

Lead may register `SceneCreatureBodyEnvelopeScenarios.Run(new[]{root,config,catalog,bank})` and use `SceneCreatureBodyEnvelope.Create(currentResources,currentHeight)` in a separately reviewed native movement/body adapter. Frozen motion interfaces and the solver are unchanged. The next integration must select current resource/incarnation and apply native pose under an explicit modern participation/contact policy; matching this local receipt alone proves neither controller behavior nor active original collision.
