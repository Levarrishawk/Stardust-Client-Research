# ShipObjectDestruction runtime boundary

Date: 2026-09-21

## Result

This block connects the previously decoded `DSEF` asset grammar to the concrete live class `ShipObjectDestruction`. It materially narrows the runtime contract, but it does not claim exact semantic names for every DSEF scalar.

## Class identity and lifecycle

MSVC RTTI at type descriptor `0x01881820` identifies vtable `0x015DFD3C` as `ShipObjectDestruction`.

| Address | Behavioral role | Confidence |
|---|---|---|
| `0x006149E0` | install/remove registration and `ClientGame/ShipObjectDestruction` configuration boundary | Strong |
| `0x00614AF0` | construct destruction controller around the source ship, seed split/debris collections, bounds, camera-relative spin direction and initial child | Confirmed |
| `0x006155B0` | per-frame staged split/debris/effect update and final cleanup | Confirmed |

The constructor retains the source object, builds initial split-plane/debris records, derives object bounds/radius and camera-relative rotation signs, and accesses the prepared destruction-sequence data through the `0x006FD5xx` accessor family. It also handles the explicit diagnostic `ShipObjectDestruction found no planes to split with`.

## Per-frame behavior

The update method confirms a multi-phase presentation rather than a single effect:

1. rotate the retained source/split object during destruction;
2. update active split operations and collect new `DynamicDebrisObject` children;
3. test camera/distance and separation thresholds before further splitting;
4. transfer transforms and velocities into newly created debris;
5. maintain a collection of surviving destruction children;
6. periodically trigger sequence-owned effects against eligible children;
7. when no pending splits remain, compute combined bounds and emit the terminal effect;
8. release transient objects and finish the dynamics object.

The method directly uses the prepared breakpoint/split collections exposed by the DSEF runtime accessors. This confirms that `SPTL`, `SXPL`, and related records feed a live split/debris scheduler rather than being inert metadata.

## Prepared DSEF accessor map

The `0x006FD560..0x006FD5C0` family makes the prepared structure offsets explicit:

| Accessor | Structure member | Loaded records |
|---|---:|---|
| `0x006FD560` | `+0x2C` | `SPTL/BRKP` vector |
| direct member | `+0x30` | `SXPL/SEDF` vector |
| `0x006FD580` | `+0x34` | `ASNL/SDAS` vector |
| `0x006FD5C0` | `+0x38` | `SDOL/SDOE` vector application |
| `0x006FD5A0` | `+0x04` | first top-level effect/float pair |
| `0x006FD5B0` | `+0x18` | second top-level effect/float pair |

`0x006FD590` returns the top-level float at `+0x00`. The constructor uses it while seeding split planes, but the surrounding math does not yet justify calling it duration. The SDAS float participates in positioning/coupling sequence elements and should likewise not be assumed to be simple audio volume merely because the strings name loop sounds.

## What remains open

The exact labels of the top-level DSEF float and the floats paired with break/final/split/sound strings remain unresolved in this block. Their runtime access crosses several split/effect helpers; assigning duration/radius/weight/volume names from resource names alone would be speculative. The SDOE first/third strings also remain open.

## Replacement-client consequence

Implement capital-ship destruction as a data-driven scene process with owned child/debris handles, explicit cancellation and deterministic cleanup. Retain DSEF floats as named-by-offset typed fields until their consumers are individually closed. The modern implementation need not reproduce the original object layout or split algorithm exactly, but it must preserve the visible schedule, resource selection, transform inheritance and original-asset grammar.

## Confidence

- Class identity, source-object ownership, staged split/debris lifecycle and final cleanup: **Confirmed by RTTI and executable code**.
- DSEF collection participation in the runtime scheduler: **Strongly supported by prepared-sequence accessors and record-count coupling**.
- Individual DSEF scalar labels and SDOE optional strings: **Unknown; deliberately not invented**.
