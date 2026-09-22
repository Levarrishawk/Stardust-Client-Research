# Installation turret projectile runtime

Date: 2026-09-21

## Result

This block closes the final two unknown `TURR` floats and identifies the runtime owner through RTTI as `TurretObject`.

## Construction

`ClientDataFile_applyToObject_VA_00604420` creates a `TurretObject` through `0x0061D700`, attaches its base and barrel appearances, installs barrel angular constraints with `0x0061D930`, then calls `0x0061D970` with:

- the validated `ClientWeaponObjectTemplate`;
- resolved muzzle transform;
- first trailing TURR float;
- second trailing TURR float.

The setter retains these at `TurretObject +0xE8/+0xEC`.

## Runtime firing path

`TurretObject_alter_VA_0061DA90` tracks a target, rate-limits base yaw and barrel pitch against the configured angular parameters, validates forward-facing aim, constructs a projectile through the retained weapon template, and then:

- sends `TurretObject +0xE8` to `Projectile_setSpeed_VA_006F5D00`;
- sends `TurretObject +0xEC` to `Projectile_setLifetime_VA_006F5CF0`.

The speed setter stores the scalar at projectile `+0xC0`, multiplies the projectile forward vector by it and installs the resulting velocity. The lifetime setter stores at `+0xC8`; projectile alter accumulates elapsed time at `+0xCC` and expires when elapsed reaches that value.

Thus the acid-turret values `60.0, 10.0` are projectile speed and lifetime respectively, not range and cooldown.

## Replacement-client contract

TURR should decode to a turret presentation/controller record containing base/barrel resources and limits, typed weapon-template reference, muzzle attachment, projectile speed and projectile lifetime. Retain fixed float32 file data; use engine-native transforms and object handles internally.

## Confidence

- `TurretObject` identity: **Confirmed by RTTI**.
- Field flow into projectile speed and lifetime setters: **Confirmed by executable call graph and update equations**.
- Exact physical unit labels: **Unknown**; preserve original numerical behavior rather than assuming meters/seconds without broader world-scale validation.
