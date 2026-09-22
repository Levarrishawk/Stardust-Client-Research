# Ground Group UI Runtime

Date: 2026-09-22

## Result

`SwgCuiGroup` is the ground-party projection and interaction shell over the
already recovered `GRUP:6` replicated object plus out-of-range
`ClientMfdStatusUpdateMessage` presentation. It introduces no new membership
protocol; leader/member actions use ordinary queued commands.

## Client evidence

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00EF1780` | `swg_cui_group_construct_VA_00EF1780` | constructs reusable member rows and subscribes group/member/target/loot events | Confirmed |
| `0x00EF2800` | `swg_cui_group_rebuild_members_VA_00EF2800` | rebuilds member status mediators from replicated group membership and local player state | Strongly inferred |
| `0x00EF2CD0` | `swg_cui_group_context_action_VA_00EF2CD0` | handles member context actions including dismiss, disband and leave through exact queued commands | Confirmed |
| `0x00EF3110` | `swg_cui_group_layout_changed_VA_00EF3110` | marks member layout dirty when a child status row changes size/location | Strongly inferred |

RTTI confirms subscriptions to `GroupObject::MembersChanged`, `MemberAdded`,
`MemberRemoved`, `CreatureObject::GroupChanged`, `LookAtTargetChanged`, and
`GroupObject::LootMasterChanged`/`LootRuleChanged`.

## State flow

```text
GRUP:6 snapshot/deltas
  -> transactional GroupObject commit
  -> aggregate/granular member and loot events
  -> SwgCuiGroup row creation/removal/reordering

near member: ordinary replicated creature status
far member: ClientMfdStatusUpdateMessage augmentation
  -> same member status presentation
```

Each replicated member entry is `NetworkId + ASCII member name`; master-looter
ID and loot rule are separate `GRUP:6` fields. The UI also tracks the local
look-at target for row highlighting/selection.

## Authoritative actions

The context handler resolves these exact command names:

- `dismissGroupMember` targeting a selected member;
- `disband` for the local group leader;
- `leaveGroup` for the local member.

Current Stardust registers all three handlers and performs membership mutation
through `GroupManager`. The UI's leadership/visibility checks are advisory;
server validation and resulting `GRUP:6` replication remain authoritative.

## Replacement-client requirement

Model group state independently from widgets. Apply complete `GRUP:6` changes
transactionally, then emit typed aggregate/granular events. Resolve members by
64-bit object ID, merge near-object status with far-member MFD status, and use
queued commands for mutations. Never optimistically author membership.

## Residual uncertainty

Exact row spacing, portrait/status style and context-menu visibility nuances
are presentation fidelity. Membership, loot state, target coupling, far-member
augmentation and mutation commands are closed.
