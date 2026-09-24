# Space HUD: first-level action disposition for all 12 registered names

Date: 2026-09-24. Scope W10. Target original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All addresses are provisional; Ghidra decompilation is reconstructed pseudocode. This matrix closes **first-level routing**, not every downstream mediator or simulation function.

`SwgCuiHudActionSpace` constructor `0x00E21060` registers 12 action names. Callback `0x00E211D0` compares all 12 original string globals, handles the matching branch, then invokes common base dispatcher `0x00EDBAA0`. The reproducible identifier extraction is `tools/python/map_action_registration_strings.py`; its 98 total names (base+ground+space) are not packet opcodes.

| Space action | First-level observed effect | Direct network effect in inspected branch | Remaining boundary |
|---|---|---|---|
| `spaceConversation` | Opens `WS_SpaceConversation` | None | Presentation/state previously mapped |
| `dogfightTauntPlayer` | Parses args, updates space conversation and synthesizes local spatial-chat presentation | None | Trigger/argument meanings; distinct from inbound `0xCA2A548B` |
| `autopilotLevelShip` | Clears prior autopilot, selects local ship flight mode 1 | None | Later movement replication and detailed physics |
| `boosterToggleDown` | Gates input and queues `boosterOn` in network mode, local slot-state alternative | Existing `0x116` command envelope | Rejection feedback/local-mode ownership |
| `boosterToggleUp` | Gates input and queues `boosterOff` in network mode, local slot-state alternative | Existing `0x116` command envelope | Rejection feedback/local-mode ownership |
| `hyperspaceMap` | With ship/controller available, opens `WS_HyperspaceMap` via `0x00E22270` | None here | Map mediator's separate selected-point command was mapped earlier |
| `shipComponentDetail` | Parses object ID, resolves ship, opens `WS_ShipComponentDetail` and supplies it | None here | Detail mediator separately mapped; exact argument grammar partial |
| `weaponGroupAssignment` | With ship available opens `WS_WeaponGroupAssignment`; otherwise warning | None here | Group-edit persistence/command path separately mapped |
| `targetLockDown` | Cockpit-camera/timestamp-gated local latch set | None here | Target-lock consumer and authority |
| `targetLockUp` | Cockpit-camera/timestamp-gated local latch clear | None here | Target-lock consumer and authority |
| `spaceTargetStationAndComm` | Selects candidate space object, updates local target and enqueues `comm` | Existing `0x116` command envelope | Exact selection algorithm; current server command has no target-specific action |
| `onPointerToggled` | Reconciles cursor position and publishes local input event when count gate permits | None | Exact count/event labels |

The new evidence in this matrix includes direct helpers `0x00E22220` (component-detail mediator) and `0x00E22270` (hyperspace-map mediator), and ground/space UI action distinctions already documented in the linked topical checkpoints. The existing [ship UI](ANALYSIS_CHECKPOINT_2026-09-22_SHIP_SELECTION_COMPONENT_UI.md), [space navigation](ANALYSIS_CHECKPOINT_2026-09-22_SPACE_NAVIGATION_UI.md) and [weapon group](ANALYSIS_CHECKPOINT_2026-09-22_SHIP_WEAPON_GROUPS.md) reports remain the downstream specifications; this matrix does not recertify their internals.

Confidence: **Confirmed by executable** for 12 names and first-level branch effects; network effects are **Confirmed** only where the branch reaches the known command queue. `No direct send` is scoped to the inspected branch, not a proof that all later UI/simulation is offline. No new opcode or whole-client percentage change.
