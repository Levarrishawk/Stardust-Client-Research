# Combat and Damage Presentation Checkpoint (2026-09-22)

## Outcome

This block closes the architectural boundary between authoritative replicated combat state and transient combat presentation. `CuiDamageManager` does not parse a dedicated ground-damage network packet. It subscribes to model events emitted after replicated creature/tangible fields change. `CuiCombatManager` separately owns combat action state, target/incapacitation reactions, combat-spam publication, and user presentation preferences.

## Damage manager

Install routine `0x009566C0` creates two typed listeners:

- `CreatureObject::Messages::DamageTaken` -> `cui_damage_handle_creature_damage_VA_00956A10`.
- `TangibleObject::Messages::DamageTaken`, payload `pair<TangibleObject*, int>` -> `cui_damage_handle_tangible_damage_VA_00956BB0`.

The creature route ignores presentation when no valid scene is active or a suppressing UI/game state is set. It reads three current damage values, rejects an all-nonpositive event, distinguishes the local player from others, applies `showDamageOverHeadSelf` / `showDamageOverHeadOthers`, allocates a timed damage presentation record and queues it for update/render.

The tangible route rejects objects with a suppressing state flag, checks the same global presentation suppression, requires an appearance where appropriate, honors a shared feature/visibility gate, and creates the tangible damage presentation.

Preferences are loaded by `CuiCombatManager` under section `CuiCombatManager`:

- `showDamageOverHeadOthers`;
- `showDamageOverHeadSelf`;
- `damageOverHeadSizeModifer` (original misspelling preserved in configuration).

The 32-bit listener objects are MessageDispatch plumbing. Their addresses, object sizes and stored pointers are not portable ABI requirements.

## Combat manager

`cui_combat_manager_install_VA_008C74A0` installs a scene-change listener, a combat-manager action object, multiple action queues, and callbacks for local-creature `IncapacityChanged`, `LookAtTargetChanged`, and `LookAtTargetLost` events. Each callback verifies that the event belongs to the current player creature before altering combat presentation/state.

The manager owns `CuiCombatManager::Messages::CombatSpamReceived`, whose payload is `pair<UnicodeString, int>`. The already mapped object-controller `0x134` formatter produces this event; chat-window subscribers consume it. This text path is distinct from the overhead numeric damage path.

Likewise object-controller `0xCC` (`CombatAction`) drives animation/presentation after resolving attacker, weapon and defender objects. Neither `0x134` nor `0xCC` is the authoritative store for hitpoints, action pools, condition or incapacitation; those remain baseline/delta/model state.

## Space damage

`CuiDamageManager::Messages::ShipDamage` is a separate typed transceiver carrying `ShipDamageMessage`. Confirmed consumers include the space HUD, fly-out page and both spherical radar implementations. Its publish/listener infrastructure is independent of the ground creature/tangible `DamageTaken` subscriptions. Detailed authoritative/presentation behavior remains in `ANALYSIS_CHECKPOINT_2026-09-15_SHIP_DAMAGE_DESTRUCTION.md`.

## Replacement architecture

Use four explicit layers:

1. Protocol decoders parse combat action, combat spam and replication deltas.
2. The object model applies authoritative pool/condition/posture/incapacitation changes.
3. Typed domain events report state transitions without exposing pointers.
4. Presentation systems independently create animation, combat-log text, overhead numbers and ship HUD effects.

This permits a headless proof-of-concept to validate combat and replication traffic without loading fonts, effects or scene objects. In x64 code, subscribers should use stable entity handles/IDs or lifetime-safe references, never captured raw pointers modeled after the original 32-bit MessageDispatch objects.

## Confidence

- Ground damage event types, install function, listeners and preference keys: **Confirmed by executable RTTI/code**.
- Current-player filtering and separate self/other visibility policy: **Confirmed by executable code**.
- Separation among replicated state, `0xCC` animation, `0x134` text and overhead numbers: **Confirmed across executable routes**.
- Exact easing, duration, font scaling and screen-placement coefficients of every damage fly-out: **Not fully reconstructed; bounded presentation fidelity**.

