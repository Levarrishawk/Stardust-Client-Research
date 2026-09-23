# CPSA change-posture action (overnight block 6)

The effective overlay contains five `CPSA/0000` actions in `change_attacker_posture`, `change_posture_align`, `creature_throw`, `hoth_snow_ball_throw`, and `throw_grenade` scripts. Their six-byte `INFO` stores a 16-bit actor selector (0 or 1 observed) and a four-byte tag (`SPEA`, `SPED`, or raw ` SPE`).

`0x00600210` installs `ChangePostureActionTemplate`. Factory `0x00600370` allocates a 0x10-byte template; `0x006003D0` dispatches `0000` to `0x006004F0`, which reads the selector and tag. Template virtual method `0x006002F0` resolves that playback actor, dynamically checks it as `CreatureObject`, looks up the tag in the playback **integer** variable map (`0x00408080`, default local value zero), and if the resulting value is nonnegative calls already identified `creature_posture_apply_VA_004328D0`. This directly links typed combat-manager `INT` bindings such as `SPEA`/`SPED` to a client creature posture mutation.

The `raw ' SPE'` tag in `change_posture_align.pst` should not be silently normalized to `SPEA`; if absent from the integer map, the observed lookup helper initializes its output to zero. Exact desired behavior for that case and whether the action emits events beyond posture application remain untested. Server authority over creature posture is a separate protocol/replication concern; this playback action is an authored client-side reaction.

Provisional names: `0x006003D0` `parse_CPSA_template`; `0x006004F0` `parse_CPSA_0000_actor_and_int_tag`; `0x006002F0` `apply_CPSA_creature_posture_from_playback_int`.
