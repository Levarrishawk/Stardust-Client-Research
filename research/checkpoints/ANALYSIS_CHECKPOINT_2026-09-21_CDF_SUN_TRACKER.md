# CDF Sun-Tracking Installation Block

Date: 2026-09-21

## Result

This block reconstructs the rare `SUNT` record used by solar/resource installations to build a two-axis sun-tracking visual assembly.

## Schema

`SUNT` is a singular leaf chunk stored at `ClientDataFile +0x3C`. `FUN_00606250` parses:

1. turntable/base appearance-template path string, eagerly resolved;
2. turntable attachment hardpoint string;
3. `float32` first angular parameter, converted to internal angular units;
4. disk/panel appearance-template path string, eagerly resolved;
5. disk attachment hardpoint string;
6. three additional `float32` angular parameters, converted to internal angular units.

The runtime record sets a final loaded/enabled flag after all resources and angles have been resolved. Duplicate SUNT records are skipped.

## Effective asset evidence

Only two effective CDFs contain SUNT: the organic flora farm and solar power generator. The flora farm record uses:

- turntable `appearance/ins_all_flora_farm_turntable_s01_u0.apt` at `hardpoint`, angle `45` degrees;
- disk `appearance/ins_all_flora_farm_disk_s01_u0.apt` at `hardpoint1`, angles `-10, 10, 10` degrees.

The tag, two-axis hierarchy, asset names, and angular constraints confirm that this is a sun-tracking installation controller rather than a generic attachment.

## Replacement-client consequence

The world/environment system must expose the current directional-light/sun vector to presentation controllers. SUNT can then aim its turntable and panel nodes within original data-defined angular limits. A modern implementation may use quaternions and explicit yaw/pitch constraints internally, while retaining the original four 32-bit degree values on disk.

This is low priority for the network proof of concept and first rendered scene, but required for faithful installation animation.

## Confidence

- Field order, resource types, and angle conversion: confirmed.
- Sun-tracking purpose and two-axis hierarchy: confirmed by corpus/tag/resource agreement.
- Exact association of each of the four angular scalars with yaw/pitch speed/limit: not yet proven.

