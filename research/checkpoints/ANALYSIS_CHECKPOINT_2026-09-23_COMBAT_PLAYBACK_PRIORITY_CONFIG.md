# Combat playback priority configuration and corpus (overnight block 3)

The earlier priority-provenance checkpoint left the local-player adjustments and base source open. Both are now bounded by executable code and the effective combat-manager asset.

`0x00466F40` parses each `SNGL` leaf's `NAME` chunk as a NUL-terminated `.pst` path followed by **one byte**. It passes that byte to `0x00467090`, which stores it at selector `+0x20`. `0x00467370` reads this as the base playback priority, conditionally adds a local-player adjustment, and passes it into `0x00404200` → `0x00408790`, where it becomes playback instance `+0x68`. This is the full asset-to-arbitration priority route for `SNGL`; it is not a network field.

An effective-overlay census of `combat/combat_manager.iff` finds 2,641 `SNGL` NAME suffix bytes: base priorities 1 (484), 5 (681), 6 (6), 7 (51), 8 (6), 10 (275), and 20 (1,138). These counts are SNGL leaves, not unique combat actions or executed events. The reader `0x00466F40` loads the byte and does not normalize it; no value above 20 occurs in this asset. The census is reproducible with ignored scratch `local-private/session-2026-09-23-marathon/selector_priority_census.py` and the existing configured-TRE reader.

`ConfigClientGame::install` (`0x00503600`) populates the two adjustment globals from `ClientGame` integer keys `playerAttackerCombatPriorityBoost` (default 50) and `playerDefenderCombatPriorityBoost` (default 49). `0x00467370` adds the former when the local player is the attack actor and context field `[8] == 1`; it adds the latter when the local player is the defender actor under that same context flag. Therefore a base priority of 20 can become 70 or 69 under defaults. Runtime configuration can override the defaults. The exact gameplay meaning of context `[8] == 1` remains open.

`ConfigClientAnimation::install` (`0x0040A980`) reads `ClientAnimation.disableCombatTrumping` (default false) into the arbitration bypass byte `DAT_01907EE0` and registers it as a debug flag. Thus when true, `0x0139AD30` directly inserts playback without actor-conflict preemption. `ClientAnimation.logPriorityHandling` (default false) is a separate debug flag. This resolves the global flag's source and intended label. It does **not** resolve the distinct per-call bypass boolean passed by `0x00469550`.

Provisional names: `0x00466F40` `parse_SNGL_name_path_and_base_priority`; `0x00467090` `construct_SNGL_selector_with_base_priority`; `0x0040A980` `install_ClientAnimation_priority_flags`; existing `0x00467370` and `0x0139AD30` names remain valid.
