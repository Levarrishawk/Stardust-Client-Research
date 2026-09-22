# Contrail override-key semantics

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
Boundary: CDF `CONT` strings A and D

## Result

The final two contrail fields are behaviorally closed. They do not remap hardpoints. They implement child-before-parent contrail override suppression.

`ShipObject_buildContrailsRecursive_VA_006EDF20` recursively visits child objects before processing the current object's CDF. It receives a caller-owned ordered string set that starts empty for each rebuild.

For every contrail record:

1. if string D is nonempty, `0x004BC5F0` inserts D into the ordered set;
2. `0x006F1F70` searches that set for string A;
3. if A is present, this contrail record is skipped;
4. otherwise the record proceeds through hardpoint resolution and trail creation/reuse.

The record's A value is never inserted merely because the record was created. It is therefore an **overrideable contrail name/key**, not a uniqueness key. Multiple records may share A (for example multiple `booster` trails) without suppressing one another. D is the **contrail name/key overridden by this child record**.

## Corpus confirmation

Component weapon records commonly contain:

```text
A = ""
B = "contrail1"
D = "wingtip_neg_1"
```

The corresponding parent wing/ship record contains:

```text
A = "wingtip_neg_1"
B = "contrail1"
D = ""
```

Because recursion processes the weapon child first, the child inserts `wingtip_neg_1`; the later parent record with that A is suppressed. The child's own empty A is absent from the set and its trail is created. The same pairing occurs for `wingtip_neg_2` and across multiple X-wing/component variants.

## Final `CONT/INFO` field contract

| Order | Field | Meaning |
|---:|---|---|
| 1 | A | overrideable contrail name/key |
| 2 | B | start hardpoint |
| 3 | C | optional end hardpoint |
| 4 | resource | SWH appearance-template path |
| 5 | D | contrail name/key overridden by this record |
| 6 | float32 | full ribbon width |

## Replacement consequence

Rebuild contrails recursively from children to parent with one rebuild-local set of overridden names. Insert D before testing A. Do not treat A as a unique ID and do not translate D into a hardpoint. This preserves component replacement behavior in original ship CDF assets.

## Confidence

- Set insertion, lookup ordering and skip branch: **Confirmed by raw x86 and decompilation**.
- A/D behavioral roles: **Confirmed by control flow and matching deployed corpus pairs**.
- Suggested public names `name` and `overrideName`: **Strong semantic names; original editor spelling is not present in the executable**.

