# CDF Turret and Flag/Banner Block

Date: 2026-09-21

## Result

This block resolves two specialized but architecturally useful CDF records: installation turret composition (`TURR`) and cloth/banner presentation (`FLAG`).

## TURR installation turret

`TURR` is a single leaf chunk parsed inline by `FUN_00606250` and retained at `ClientDataFile +0x40`. The record is not merely presentation: it resolves a referenced object template and verifies it is a `ClientWeaponObjectTemplate` using RTTI.

Confirmed byte order:

1. turret appearance-template string;
2. turret/base hardpoint string;
3. `float32` turret angular parameter, converted to internal angular units;
4. barrel appearance-template string;
5. barrel hardpoint string;
6. three `float32` barrel/turret angular-limit parameters, converted to internal angular units;
7. weapon object-template path string, dynamically checked as `ClientWeaponObjectTemplate`;
8. muzzle hardpoint string;
9. `float32` projectile speed;
10. `float32` projectile lifetime;

The effective large acid turret gives concrete values:

- turret appearance `ins_all_def_turret_lg_s01_turret.apt` at hardpoint `turret`, angle `180` degrees;
- barrel appearance `..._barrel.apt` at hardpoint `barrel`, limits `-45, 45, 45` degrees;
- weapon template `object/weapon/ranged/turret/shared_turret_acid.iff`;
- muzzle hardpoint `muzzle`;
- trailing values `60.0` and `10.0`.

`TurretObject::alter` creates the weapon-template projectile at the muzzle transform, passes the first trailing float to the projectile speed/velocity setter, and passes the second to the lifetime setter. The example therefore launches at speed `60.0` with lifetime `10.0`. The appearance/hardpoint hierarchy, angular limits, weapon-template bridge, muzzle definition and projectile travel parameters are confirmed.

## FLAG banner/cloth record

`FUN_006FE820` allocates a 44-byte polymorphic flag record and delegates parsing to `FUN_006FEA90`. `FLAG` supports unordered child chunks and safely skips unknown extensions.

Confirmed child grammar:

- `APPR`: skeletal appearance-template path;
- `HPNT`: attachment hardpoint name;
- `WSCL`: `float32` width/world scale, default `1.0`;
- `MASP`: `float32` maximum animation/wind speed-like parameter, default `10.0`.

The effective Imperial battle banner uses:

- `appearance/mun_all_impr_pvp_banner_s01.sat`;
- hardpoint `banner`;
- WSCL `1.0`;
- MASP `10.0`.

The resource type `.sat` and parameter names inferred from tag mnemonics strongly support a wind/cloth banner controller, but the exact MASP expansion is not embedded and remains strongly inferred.

## BANR structure banner variant

`BANR` is a closely related 48-byte polymorphic banner record constructed by `FUN_006FE370` and loaded by `FUN_006FE5E0`. It supports the same `APPR`, `HPNT`, `WSCL`, and `MASP` chunks plus:

- `LIFT`: one `float32` converted through the engine's linear-unit scale.

Defaults are WSCL `1.0`, MASP `10.0`, and an engine-global default lift value. All 22 effective BANR files contain LIFT. The Aurilia structure banner uses APPR `mun_all_imprv_banner_s01.sat`, HPNT `banner`, WSCL `1.0`, LIFT `30.0`, MASP `10.0`, followed by a CSSI customization selecting `/private/index_texture_1 = 11`.

This demonstrates that structure banners combine cloth parameters with ordinary appearance customization. `FLAG` and `BANR` should be represented as related typed records, with BANR adding lift/vertical-force behavior rather than being treated as an alias.

## Replacement-client consequence

Turrets should be assembled as a small scene hierarchy with independently rotatable base and barrel nodes, a typed weapon-template reference, and a muzzle transform. Network combat state can drive aim/fire while the asset record supplies visual composition and constraints.

Flags require skeletal/cloth animation support or a fidelity-preserving compatibility approximation. Their original CDF grammar should be preserved even if a modern GPU cloth implementation replaces the original controller.

## Confidence

- TURR field order, resource types, and angular conversion: confirmed.
- TURR projectile speed and lifetime fields: confirmed by `TurretObject` and projectile runtime setters.
- FLAG chunks, defaults, and field widths: confirmed.
- BANR inheritance-like relationship and added LIFT scalar: confirmed.
- FLAG wind/cloth purpose: strongly inferred from assets and tags.
