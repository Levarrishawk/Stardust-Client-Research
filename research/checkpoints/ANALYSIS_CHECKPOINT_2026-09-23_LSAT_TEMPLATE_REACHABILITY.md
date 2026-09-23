# LSAT blade assets referenced by weapon templates

Date: 2026-09-23. Effective configured overlay.

## Result

The LSAT family is not an isolated cache-only experiment. A read-only scan
of effective original assets found **208 object/weapon `.iff` templates**
with literal `.lsb` appearance references, covering **42 of the 45** live
LSAT files. All 42 referenced `.lsb` paths resolve. The three other live
files are:

- `appearance/wp_mle_lance_lightsaber_blade_gen5.lsb`
- `appearance/wp_mle_lightsaber_blade_mandalorian.lsb`
- `appearance/wp_mle_sword_lightsaber_gcw_2h.lsb`

Those three are not directly named by an effective `object/` template in the
scanned corpus; they remain valid assets and may have other dynamic or future
uses. Cache manifests contain 45-path inventories but are not a substitute
for template reachability evidence.

## Consequence

The importer must connect original shared weapon-template appearance fields
to the `LSAT` factory, then from LSAT to its BASE/ASND/SHDR dependencies.
Loading a saber as only a base APT would omit the blade, its transitions,
light and sound. Conversely, do not infer that all 208 templates are spawned
on the current shard merely because they are present in installed assets.

Counts and path resolution are **confirmed by the configured-overlay corpus**;
actual live server spawn frequency is **unknown**. Scratch scan:
`local-private/session-2026-09-23-marathon/scan_lsb_references.py`.
