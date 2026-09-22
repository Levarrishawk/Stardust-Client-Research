# SWG-to-Godot coordinate adapter fixtures

Date: 2026-09-22

## Evidence boundary

Executable analysis establishes that the client transform basis is Y-up and
uses the K/+Z basis vector as forward (the spatial-audio bridge forwards K as
listener facing and J/+Y as up). Quaternion storage is `[w,x,y,z]`; the axis-
angle constructor and quaternion-to-basis builder independently confirm this.
Wire encoders often serialize translation as x/z/y, but that is field order,
not a different in-memory coordinate system.

Godot is also Y-up but treats -Z as forward. The smallest explicit adapter is
therefore the Z reflection `S = diag(1,1,-1)`.

## Adapter

| Value | SWG -> Godot |
|---|---|
| position or direction | `(x, y, z) -> (x, y, -z)` |
| quaternion memory `[w,x,y,z]` | `(w,x,y,z) -> Godot Quaternion(-x,-y,z,w)` |
| rotation basis | `R_g = S * R_s * S` |
| triangle indices | reverse one winding edge/order once at import |
| normal | transform as direction `(nx,ny,-nz)` |
| tangent | transform xyz as direction and invert tangent handedness sign |

The quaternion rule follows from conjugating the rotation basis by the
reflection: quaternion vector components transform as an axial vector,
`det(S) * S * (x,y,z) = (-x,-y,z)`. It is not merely a component reorder.

## Numerical fixture result

`coordinate_adapter_fixture.py` tests identity, +90-degree yaw, pitch and roll,
plus a normalized mixed quaternion. For every fixture, the matrix created from
the adapted quaternion equals `S * R_swg * S` within `1e-6`. The script also
prints the explicit Godot `[x,y,z,w]` constructor values and exits nonzero on
failure. All fixtures passed on 2026-09-22.

## Required use boundaries

- Decode files into an engine-neutral SWG-space representation first.
- Apply this adapter once when publishing transforms/geometry to Godot; do not
  scatter sign changes among individual asset readers.
- Keep network translation decoding separate: read wire x/z/y into logical SWG
  `(x,y,z)`, then apply the same host adapter if/when publishing to the scene.
- Compose complete parent/child transforms in one coordinate convention. Since
  `S(AB)S = (SAS)(SBS)`, converting each local basis is composition-safe.
- Root-motion translation and QCHN/LOCR rotation use the identical adapter.

## Confidence

- SWG `[w,x,y,z]`, Y-up, +Z/K forward, and wire x/z/y distinction: **confirmed
  by executable code**.
- Godot conversion equations and fixture invariants: **confirmed
  mathematically and by numerical tests**.
- Whether a future custom DX12 renderer should retain SWG +Z-forward internally
  instead of applying this adapter: **architecture choice**, not a compatibility
  unknown.

