# Space HUD Widgets and Player Formation Checkpoint

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Summary

This block closes four remaining high-value space presentation families:
player-ship formation assignment, missile/countermeasure counts, component
fly-out status, and the ship reticle.

## Player formation assignment

`SwgCuiSpaceAssignPlayerFormation` constructor `0x00DCC700` binds member and
formation volumes, formation-ship diagram, formation name, formation-name
sample button and player/ship row samples. Its lifecycle subscribes to group
member, group-changed, member-ship and formation-member add/remove events.

The submit routine `0x00DCDC70` is confirmed end-to-end on the client:

1. queue `setFormation` with the selected formation name and invalid target;
2. iterate assigned players;
3. determine each member's selected slot;
4. queue `setFormationSlot` against that member's NetworkId with the decimal
   slot index as Unicode arguments.

Both use the ordinary command queue (`0x0046E620` -> controller `0x116`). The
current Stardust source contains command localization entries but no registered
`setFormation`/`setFormationSlot` handlers. Thus this client feature's current
server endpoint appears absent. That is a server compatibility gap to verify,
not an unknown client action.

## Missile count

`SwgCuiSpaceMissileCount` constructor `0x00EDE4E0` binds two missile launcher
name/count pairs and a countermeasure name/count pair. Update `0x00EDE840`
scans the 112 ship-component slots, locates eligible missile/countermeasure
components, refreshes each pair, and hides the mediator when none exists. It is
a projection of replicated ship component/ammunition state and sends nothing.

## Component fly-out page

`SwgCuiSpaceFlyOutPage` constructor `0x00EE4A60` creates buttons for armor,
capacitor, engine, weapon, shield and reactor categories and a pop-out page. It
subscribes to ship-damage and weapon-group-change events. Its retained per-slot
display cache is dirtied by the component event routine `0x00EE53F0`, which
forces a refresh. No new protocol is introduced.

## Ship reticle

`SwgCuiShipReticle` constructor `0x00EE74A0` binds speed, weapon and booster
gauges plus the reticle group and registers the
`disableReticleInThirdPerson` preference/debug flag. Update `0x00EE7AA0`
derives:

- current/max speed and controller-scaled throttle;
- weapon/capacitor state;
- booster active/available state;
- reticle visibility from camera mode and player-ship/controller flags.

All values are local render projections of the ship/controller model.

## Replacement-client implications

These should become lightweight view models over a common ship-state snapshot.
Do not reproduce the 32-bit mediator inheritance or global display caches.
Preserve original component slot numbering, formation identifiers, command
strings, UI imagery/styles and ship data definitions. Formation commands need
a deliberate Stardust server decision before they can be considered supported.

## Confidence

- Widget bindings, event dependencies, formation command sequence and reticle
  source values: **confirmed**.
- Missile slot classification and fly-out refresh role: **strongly inferred to
  confirmed**, with anonymous type predicates still unnamed.
- Absence of current formation handlers: **confirmed by repository search**,
  subject to runtime/plugin code outside the inspected tree.

