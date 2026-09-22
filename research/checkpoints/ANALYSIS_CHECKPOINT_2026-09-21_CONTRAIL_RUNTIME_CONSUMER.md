# Contrail runtime consumer and width

Date: 2026-09-21

## Result

The `ClientDataFile +0x68` contrail vector has exactly one executable accessor consumer: `ShipObject_buildContrailsRecursive_VA_006EDF20`. The function first recurses through child objects, then processes the current object's CDF records against live appearance hardpoints and a caller-owned selection map.

## Field use

For each `ContrailData` record the consumer:

1. uses string A as a map lookup/selection key;
2. resolves string B as the first/start hardpoint;
3. resolves nonempty string C as a second/end hardpoint, otherwise uses the single-ended swoosh constructor;
4. creates the cached `.swh` appearance through `0x00700F30`;
5. uses string D when present in the parent/component remapping path;
6. writes the final float through `SwooshAppearance_setWidth_VA_007B19D0`.

The swoosh vertex builder at `0x007AFCD0` reads that field, multiplies it by `0.5`, and offsets paired vertices by positive and negative half-width along the computed ribbon normal. This proves the file scalar is full ribbon width, not alpha, lifetime or an arbitrary scale.

## Replacement-client contract

Resolve contrails after the ship/component hierarchy exists. Select records through A, resolve start/end hardpoints B/C, apply optional parent remapping D, instantiate the original SWH template, and set full width. Preserve recursive child traversal and avoid creating duplicate trails when an existing selected record is reused.

## Superseding override-key trace

`ANALYSIS_CHECKPOINT_2026-09-21_CONTRAIL_OVERRIDE_KEYS.md` closes A and D. Child-first recursion inserts D into a rebuild-local set, then skips a record when its A is already present. A is therefore the overrideable contrail name/key and D is the name/key overridden by the child record; neither is a hardpoint remapping field.

## Confidence

- Sole accessor consumer, recursive traversal, B/C hardpoint use, SWH construction and width: **Confirmed by executable code**.
- A lookup-key role: **Confirmed**; exact public domain name remains unknown.
- D parent/component remap role: **Strongly inferred from branch and corpus pairing**.
