# Common ClientObject View-6 Opaque Field

## Outcome

The inherited `ClientObject` view-6 U32 cannot be assigned a defensible product
name, but its compatibility boundary is now closed: it is a passive replicated
scalar in this executable, not a missing behavioral callback.

## Executable evidence

`ClientObject` constructor `0x00554240` creates the field at object offset
`+0x230`, installs vtable `0x015DE08C`, and registers it as the sole common
view-6 field. MSVC RTTI identifies the type only as
`Archive::AutoDeltaVariable<unsigned long>`. Snapshot `0x00542DA0` and delta
`0x00542DC0` read exactly one U32. The constructor leaves the associated owner
and callback slots zero, so delta application does not invoke a semantic
`ClientObject` notification in this build.

## Server correlation

Current Stardust emits different constants by object family, including 0x76
for Tangible, 0x3D in Creature composition, 0x44 for Static and 0x95 for Cell.
The bytes are therefore not a universal Boolean or pointer. Existing source
comments do not supply trustworthy semantics, and naming it from the constants
would be invention.

`TANO:6` adds the defender vector after this scalar. `PLAY:6` adds its privilege
byte. `SCLT:6`, `STAO:6`, and `ITNO:6` include characterized legacy trailing
payloads not represented by registered fields in this client.

## Replacement rule

Decode and retain the U32 as `commonView6Opaque` keyed by object tag/view. Do
not expose gameplay behavior from it unless later endpoint evidence establishes
a meaning. Keep narrowly keyed tail allowances for the known tag/view pairs;
do not turn this observation into general acceptance of trailing bytes.

## Confidence

- Type, offset, wire width, registration, decoder and null callback ownership:
  **Confirmed by code**.
- Current constants by family and composed order: **Confirmed by server code**.
- Historical semantic name: **Unknown** and intentionally not guessed.

