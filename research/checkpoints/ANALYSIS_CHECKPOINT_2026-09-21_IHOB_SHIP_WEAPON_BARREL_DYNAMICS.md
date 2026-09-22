# `IHOB` and `ShipWeaponBarrelDynamics`

Scope: resolve the three previously unknown `IHOB` floats by tracing record application, RTTI, update equations, and firing-path activation in `Stardust.exe`.

## Result

`IHOB` is an appearance attached to a named hardpoint with optional **ship weapon barrel recoil dynamics**. Its on-disk payload is:

```text
NUL-terminated appearance-template path
NUL-terminated parent hardpoint name
float32 maximum_recoil_distance
float32 recoil_speed
float32 return_speed
```

All fields remain 32-bit on disk.

## Construction path

`0x007008F0` parses the two strings, eagerly resolves the appearance template, and reads the three floats into record offsets `+0x18`, `+0x1C`, and `+0x20`. `ClientDataFile_applyToObject_VA_00604420`:

1. resolves the named hardpoint transform on the owning object;
2. creates the child object and applies the attachment appearance;
3. when `maximum_recoil_distance` is nonzero, allocates a 76-byte dynamics object through `0x006E9DE0`;
4. supplies the hardpoint transform plus all three floats;
5. installs that dynamics controller on the child and attaches it to the scene.

MSVC RTTI at vtable `0x015E1BF8` gives the original class name `ShipWeaponBarrelDynamics`.

## Runtime state machine

`ShipWeaponBarrelDynamics_alter_VA_006E9E70` retains:

- base hardpoint transform;
- authored maximum displacement;
- outward/recoil rate;
- return rate;
- active-recoil flag;
- current displacement.

When active, displacement increases toward the maximum by `deltaTime * recoil_speed`. Reaching the maximum clears the active flag. Once inactive, displacement decreases toward zero by `deltaTime * return_speed`. Whenever the controller was or remains displaced, it copies the stored hardpoint transform, translates it by `-current_displacement` along local unit Z, and applies the transform to the barrel child.

`0x006EA010` activates/restarts recoil by setting the active flag. `0x005363D0` recursively walks a fired weapon object's child hierarchy, RTTI-casts each installed `Dynamics` to `ShipWeaponBarrelDynamics`, and calls that activation method. Both projectile-manager firing paths (`0x00597A60` and `0x00597E30`) reach the recursive trigger through `0x0053CED0` / `0x0053CF60` after resolving the weapon attachment.

This yields the complete behavioral chain:

```text
ship weapon fires
  -> resolve weapon attachment hierarchy
  -> recursively locate ShipWeaponBarrelDynamics controllers
  -> activate recoil
  -> advance to maximum along local -Z at recoil_speed
  -> return to base transform at return_speed
```

## Corpus evidence

A precedence-correct scan found **196 IHOB records in 80 effective CDF files**. Most records use zero dynamics and therefore behave as ordinary hardpoint attachments. Nonzero examples are concentrated in turret barrels:

| Hardpoint | Distance | Recoil speed | Return speed |
|---|---:|---:|---:|
| `turretbarrel1` | `1.4` | `4.0` | `1.2` |
| `turretbarrel2` | `1.7` | `5.0` | `1.8` |

These tuples recur across YT-1300, YT-2400, YKL-37R, VT-49, and Imperial Lancer turret/cannon CDFs. This corroborates the executable equations and RTTI classification.

## Replacement-client contract

- Parse all IHOB records, including zero-valued dynamics records.
- Resolve the hardpoint before creating the child presentation object.
- Only create recoil state when distance is nonzero, matching the legacy branch.
- Trigger every `ShipWeaponBarrelDynamics` instance recursively beneath the fired weapon attachment.
- Recoil is local negative Z from the stored base hardpoint transform.
- Preserve distinct recoil and return rates and clamp at both endpoints.
- The modern implementation does not need the legacy `Dynamics` class layout; a component with the same state machine is sufficient.

## Stable provisional names

| Address | Name | Confidence |
|---:|---|---|
| `0x006E9DE0` | `ShipWeaponBarrelDynamics_ctor_VA` | Confirmed by RTTI and code |
| `0x006E9E70` | `ShipWeaponBarrelDynamics_alter_VA` | Confirmed by code |
| `0x006EA010` | `ShipWeaponBarrelDynamics_triggerRecoil_VA` | Confirmed by code |
| `0x005363D0` | `ShipObject_triggerWeaponBarrelRecoilRecursive_VA` | Strongly inferred |

## Confidence

- Original runtime class name, layout relationship, equations, axis, and three float meanings: **confirmed by code and RTTI**.
- Connection to projectile firing and recursive child activation: **confirmed by code**.
- Exact historic method names: **unknown**; behavioral provisional names are stable.
