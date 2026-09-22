# Incapacitation UI Workflow

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`, correlated with current Stardust server

## Client behavior

`SwgCuiIncap` RTTI resolves to vtable `0x015F6428`. Constructor `0x00F4A0E0`
binds required UI objects for the displayed text and `valueBar`; absence is a
fatal asset/configuration error, confirming that the original page contract is
mandatory.

Per-frame update `0x00F4A310` is presentation-only:

1. obtain the current player creature;
2. if the creature is no longer incapacitated, deactivate/close the mediator;
3. read the replicated incapacitation recovery time from the creature;
4. maintain a local elapsed-time accumulator between authoritative changes;
5. derive a nonnegative countdown;
6. update `Incapacitation Time: %d` only when the displayed second changes; and
7. set the value-bar proportion against the retained maximum observed duration.

The mediator sends no recovery request. Posture/state and the replicated timer
are authoritative; local frame time only smooths the countdown between updates.

## Server correlation

Current Stardust writes `CREO:3` index `0x07` with
`CreatureObjectDeltaMessage3::updateIncapacitationRecoveryTime(uint32)` after
calculating the timer. It sets posture to `INCAPACITATED`, clears combat/feign
death state, and schedules `PlayerIncapacitationRecoverTask` for the same number
of seconds. Recovery validates current lifecycle state, clears cloning, clamps
negative HAM pools to one, removes feigned death, sets upright posture and emits
the revived observer event.

This corroborates the client field semantics and proves the UI is a projection
of replicated state plus local interpolation—not a second recovery authority.

## Replacement contract

- Decode the recovery field as protocol U32 regardless of x64 internals.
- Activate the incap overlay from current-player posture/incapacity state.
- Rebase the local countdown whenever authoritative duration changes.
- Never let local countdown completion mutate authoritative posture.
- Close when replicated/model state says the player recovered or left scene.
- Preserve the original page's text/value-bar bindings through the UI adapter.

## Confidence

The timer source, display calculation, close condition and server scheduling are
**Confirmed**. The exact original names of internal accumulator fields are not
needed for compatible behavior.
