# CDF Ship Contrail Data

Date: 2026-09-21

## Result

The `FORM CONT` family is explicitly identified by an executable diagnostic as `ClientDataFile::ContrailData`. All 174 effective records use `FORM CONT/FORM 0000/INFO` and share one exact schema.

## Serialized contract

`INFO` contains, in order:

1. NUL-terminated contrail selection key/logical name A;
2. NUL-terminated start hardpoint B;
3. NUL-terminated optional end hardpoint C;
4. NUL-terminated contrail appearance-template path;
5. NUL-terminated optional attachment string D;
6. little-endian `float32` full ribbon width.

`FUN_00700bb0` constructs storage for the five strings plus the scalar and cached appearance-template pointer. `FUN_00700de0` accepts only version `0000`, warning on any other child form. `FUN_00700e80` loads the fields and resolves string 4 through the appearance-template loader.

The sole runtime consumer uses A as a lookup/selection key, resolves B and optional C as hardpoint transforms, resolves the appearance, and applies the scalar through `SwooshAppearance +0x2E4`. The swoosh vertex builder multiplies it by one half and offsets vertices in both directions along the ribbon normal, proving it is full ribbon width. Optional D participates in parent/component hardpoint remapping; its exact original label remains provisional.

## Corpus evidence

A complete configured-overlay scan found exactly 174 records in 65 CDFs. Every resource field observed is `appearance/sw_ship_contrail_white.swh` or its `_long` variant. Representative records are:

```text
wingtip1 | contrail1 | ""           | ...white_long.swh | ""            | 0.15
booster  | exhaust_in1 | exhaust_out1 | ...white.swh      | ""            | 0.40
""       | contrail1 | ""           | ...white.swh      | wingtip_neg_1 | 0.40
```

The scalar range represented in the corpus includes 0.15, 0.16, 0.17, 0.20, 0.21, 0.23, and 0.40. Component CDFs often leave selector A empty and use optional D to bind the component-local trail back to a parent wingtip. Booster records can populate both B and C (`exhaust_in`/`exhaust_out`), which strongly indicates a two-point trail definition. This interpretation is strong but the exact public field names remain unknown.

Contrail records are retained at `ClientDataFile +0x68`; the three-instruction getter at `0x00609e40` returns that collection. They are not created by the generic `applyToObject` phase, consistent with ship code consuming them when ship/component attachment state is known.

## Replacement-client consequence

- Parse and retain all five strings even when most deployed records leave some empty.
- Resolve `.swh` through an appearance/trail resource factory, not as a static mesh.
- Instantiate trails in the ship/component presentation layer after component hardpoint resolution, rather than during generic object composition.
- Support two-point booster trails and parent/component hardpoint remapping.
- Preserve the full ribbon width as float32 file data; the runtime derives half-width only while building vertices.

## Superseding override-key trace

`ANALYSIS_CHECKPOINT_2026-09-21_CONTRAIL_OVERRIDE_KEYS.md` closes A and D as child-before-parent override keys rather than component hardpoint remapping. D is inserted into a rebuild-local suppression set before A is tested.

## Confidence

- ContrailData identity, version, five-string-plus-float layout, and appearance caching: confirmed.
- Corpus count and field population patterns: confirmed.
- B/C two-point hardpoint interpretation and full-width scalar: confirmed by the runtime consumer and swoosh vertex builder.
- A is confirmed as a lookup/selection key; exact domain terminology is unknown.
- D parent/component remap role: strongly inferred; exact historic label unknown.
