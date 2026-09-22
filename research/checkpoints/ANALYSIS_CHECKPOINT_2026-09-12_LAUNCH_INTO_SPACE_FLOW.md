# `launchIntoSpace` client/server flow

Scope: static analysis of `C:\Stardust-DEV\Stardust.exe` correlated with `LaunchIntoSpaceCommand.h` in the current Stardust server tree.

## Confirmed client command construction

`SwgCuiTicketPurchase` event handler `0x00D848D0` contains a distinct space-travel branch beginning at `0x00D84BC3`. Its retained NetworkIds are:

- mediator `+0x110/+0x114`: the space-terminal ID used as the ordinary command target;
- mediator `+0x118/+0x11C`: the selected ShipControlDevice ID placed first in the arguments.

The two setters are `0x00D86BE0` and `0x00D86C10`. Their provenance is visible in the selection mediator at `0x00DF6BD0`: it validates the selected world object as game-object type `0x400B`, stores that object's ID as the terminal target, resolves the selected ship/device object, and stores its NetworkId separately.

The client constructs this Unicode argument sequence:

`shipControlDeviceId groupSize groupMemberId... arrivalPlanet arrivalPoint`

Specifically, it:

1. formats the selected ID at `+0x118`;
2. reads the shared launch-member list rooted at `0x0196A974`, appends its count, then every member NetworkId;
3. appends the currently selected arrival planet;
4. appends the selected arrival point after the same UI/token normalization used by ticket purchase;
5. queues command name `launchIntoSpace` through `0x0046E620`, passing `+0x110` as target;
6. closes/deactivates the mediator.

The same global member list and command construction also appear in the ship-selection mediator at `0x00DF6BD0`, showing that this is shared launch-party state rather than ticket-map state.

## Confirmed server behavior

`LaunchIntoSpaceCommand` parses exactly the client sequence:

- U64 ShipControlDevice ID;
- U32 group count;
- that many U64 member IDs;
- optional arrival planet token;
- optional arrival point token, with underscores restored to spaces.

It requires a valid space-terminal target, owned/controllable ship and pilot certification.

Two modes then diverge:

- No arrival planet: schedule `LaunchShipTask` after 100 ms to launch the ship and supplied members into space.
- Arrival planet supplied (Stardust JTL fast travel): require reactor and engine, validate the destination zone and interplanetary travel point, randomize each valid member's landing coordinate within the point's landing range, switch members and pilot to the ground zone, then store the ship-device location state.

The server deliberately treats the client group list as claims: each ID is resolved and must be a player creature; invalid entries are skipped. Ship ownership, certification, terminal type, components and destination remain authoritative.

## Replacement-client boundary

The protocol implementation only needs to reproduce the ordinary command target and token sequence. Party/member selection is a higher-level service. The x64 client must retain U64 object identities and must not serialize native pointers. Arrival-point display strings containing spaces must be token-safe in the legacy command grammar (the legacy client normalizes them and the server reverses underscores).

## Confidence

- Field identities, command name, target, argument ordering and shared member-list traversal: **Confirmed by executable code**.
- Server parse and both operational branches: **Confirmed by server code**.
- Individual legacy widget/membership-list helper class names: not required for compatibility and remain provisional.
