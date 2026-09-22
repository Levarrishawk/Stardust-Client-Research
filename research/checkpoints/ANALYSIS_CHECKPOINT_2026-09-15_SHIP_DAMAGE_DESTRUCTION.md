# Analysis checkpoint: ship damage and destruction presentation

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`

## Outcome

The active Stardust ship-damage presentation family is closed across three
messages:

- `OnShipHit`, ObjectController subtype `0x432`;
- `DestroyShipComponentMessage`, opcode `0x3871D784`;
- `DestroyShipMessage`, opcode `0x5C680884`.

These messages do not own authoritative damage state. SHIP baseline/delta
fields carry health, shields and component condition. This family supplies
impact location/type, component breakage presentation, explosion presentation,
and the local-player death sequence.

## `OnShipHit` / ObjectController subtype `0x432`

Outer ObjectController framing is operand count `5`, opcode `0x80CE5E46`, U32
flags `0x1B`, U32 subtype `0x432`, U64 ship object ID and U32 zero. The nested
payload is:

| Field | Type | Meaning |
|---|---|---|
| hit direction/location X,Y,Z | 3 x F32 | each server-clamped to ship bounding radius |
| hit type | S32 | 0 shield, 1 armor, 2 component, 3 chassis |
| new percent | F32 | post-hit condition fraction |
| old percent | F32 | pre-hit condition fraction |

The ship controller dispatch `0x00618850` recognizes subtype `0x432`, casts
the queued payload to RTTI type `MessageQueueShipHit`, and calls handler
`0x005717F0`. The handler rejects hit types outside 0..3 and does not generate
an effect when `newPercent >= oldPercent`.

For a valid damaging event it:

1. maps old/new percentages into damage-severity bands;
2. converts the supplied ship-local hit vector into the appropriate attachment
   and world-space effect transform;
3. chooses an effect from a table indexed by hit type and severity;
4. creates and scales the client effect;
5. selects/plays a corresponding impact sound.

The effect and sound matrices are loaded from original assets
`datatables/space/ship_hit_effects.iff` and
`datatables/space/ship_hit_sounds.iff`. Invalid hit types and missing table
entries are diagnostic no-ops. Stardust also broadcasts a separate
`PlayClientEffectLoc` for projectile collision, so a replacement must preserve
both routes rather than assuming `OnShipHit` is the only visual impact event.

## `DestroyShipComponentMessage` / `0x3871D784`

Application framing is U16 operand count `0x12`, U32 opcode, then:

| Field | Type |
|---|---|
| ship object ID | U64 |
| component slot | S32 |
| effect scale | F32 |

The GroundScene branch at `0x0051D89A` parses through `0x00AD2AE0`, resolves
the object, converts it to a ship through virtual slot `+0x78`, and calls
`0x0053E260(slot, scale, true)`. Accessors are `0x00AD2C20` (ID),
`0x00AD2C30` (slot), and `0x00AD2C40` (scale).

`0x0053E260` validates that the slot is installed, informs the ship component
appearance/effects layer, and—with the true destruction flag used by this
message—removes and destroys the matching attached component appearance. It
also updates the ship's component attachment bookkeeping and diagnostic event
state. The server emits this message only when a component hit reduces that
component to zero; otherwise it emits `OnShipHit`.

## `DestroyShipMessage` / `0x5C680884`

Application framing is U16 operand count `0x12`, U32 opcode, U64 ship object
ID, then F32 random scale. Parser `0x00AD2840` exposes ID through `0x00AD2950`
and scale through `0x00AD2960`.

The GroundScene branch at `0x0051D99D` resolves and type-checks the ship, then
forks:

- Remote/non-player-controlled ship: call `0x0053DE50(scale)`. This disables
  relevant ship presentation/state, creates the destruction effect/dynamics
  path where available, and otherwise falls back to the ship's general
  destruction routine.
- Local controlled ship: allocate the special sequence constructed by
  `0x005866A0`. Embedded evidence names this `SpaceDeath`; it uses
  `sound/music_space_player_death.snd` and the dedicated player-death client
  effect `clienteffect/combat_player_ship_hit_death.cef`.

The local sequence is therefore not equivalent to immediately deleting the
ship object. It owns death presentation while the server's delayed lifecycle
continues.

## Server lifecycle correlation

`DestroyShipTask` first closes wings/stops boost, broadcasts
`DestroyShipMessage`, and either destroys a transient unpiloted ship immediately
or sets `DESTROYING` and waits five seconds. On its second iteration it clears
`DESTROYING`, resets orientation/faction, relocates the ship near the closest
station, and rebroadcasts transform plus SHIP:3 and SHIP:6 baselines. Players
aboard are transferred after roughly 6-8 seconds. A piloted ship is eventually
stored through `StoreShipTask`; an unpiloted transient is destroyed.

This establishes an important client contract: the explosion message begins a
presentation phase. Authoritative relocation, restoration and removal arrive
through later ordinary scene/replication messages.

## Replacement-client requirements

1. Preserve the three message roles; never derive authoritative damage solely
   from their presentation payloads.
2. Consume the original `ship_hit_effects.iff` and `ship_hit_sounds.iff` tables.
3. Maintain hit-type values 0..3 and percentage-driven severity selection.
4. Keep ship/component IDs as wire U64 and component slot as wire S32,
   independent of native pointer width.
5. Make component attachment removal idempotent because the associated SHIP
   deltas and attachment callbacks may converge on the same visible result.
6. Model player death as a timed game-state/presentation sequence, not immediate
   world-object deletion.
7. Permit a headless networking client to parse and log all three without
   loading visual/audio assets.

## Confidence and bounded unknowns

| Finding | Confidence |
|---|---|
| All three wire layouts/opcodes/subtype | Confirmed |
| Client parsers, lookup and principal handler routes | Confirmed |
| Hit type mapping and percentage severity use | Confirmed |
| Original hit effect/sound table dependency | Confirmed |
| Local versus remote whole-ship destruction fork | Confirmed |
| Stardust delayed relocation/storage lifecycle | Confirmed |
| Every effect-table column label and exact particle timing | Unknown/bounded |
| Every internal field of the local `SpaceDeath` sequence | Unknown/bounded |

The bounded presentation details matter to later visual fidelity, but no longer
block protocol, object-lifecycle, asset-pipeline or architecture decisions.
