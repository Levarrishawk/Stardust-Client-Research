# APPR hardpoint contract

## Grammar and ownership

APPR/0003 can contain an optional FORM HPTS. It contains zero or more HPNT chunks directly, with **no serialized count or version form**. Each HPNT contains twelve little-endian F32 values in the same row-major 3x4 transform convention as CMPA, followed by the name occupying the remaining payload bytes, including its terminating NUL. There is no name length prefix. Earlier count/length-prefix descriptions in the asset overview were incorrect.

`AppearanceHardpointDecoder.DecodeBase` owns authored records with exact names, CRC values, transforms and original indices. It accepts printable ASCII names including empty names, up to 255 bytes plus NUL, and finite transforms. It rejects embedded NUL/trailing name bytes, unexpected children, duplicate HPTS forms and more than 4,096 records. These bounds are replacement policy. Mesh/detail/component descriptions expose the table; APT graph templates expose their terminal appearance's table. The raw APPR tree remains retained for unimplemented records.

## Ordering and query

The original hardpoint constructor copies names without case folding and computes their non-reflected CRC32. Sorting compares unsigned CRC first, then exact string bytes. The portable table reproduces that comparison, with authored index as an explicit stable tie policy. `FindAll` returns every exact match. `TryFindUnique` returns false for missing or ambiguous names; it never silently selects an unverified duplicate tie. Neither operation performs logical asset-path normalization.

The original template search returns the first element of a comparator-equal range. Its small insertion-sort path is stable; the complete larger-sort tie order has not been established. Ninety-one deployed duplicate-name groups have different transforms, so the portable stable tie must not be represented as original duplicate attachment fidelity. Runtime appearance scaling, component/LOD virtual lookup overrides and parent-object traversal are separate from this template table.

## Evidence

Confirmed for the baseline executable SHA: loader 00B2CD40 enters HPTS, obtains physical child count through 00A94B00 and iterates HPNT. Transform reader 00A95080 returns twelve floats. At 00B2CF2D it asks 00A94EE0 for bytes remaining, then 00A94F00 copies that many bytes to the name buffer. Constructor 00B49CD0 copies the transform at hardpoint+0x10 and constructs its name through 00AA4050 with folding disabled. CRC compute is 00AA4790; comparator 00AA2F10 orders unsigned CRC then exact bytes. Sort path 00B2DCD0 and template lookup 00B2D190 use that comparator; lookup returns the first equal-range index. Base appearance lookup virtual 00B2C550 applies appearance scale before copying a result. Loader resets/releases previous table contents and template destructor 00B2C980 releases all records.

## Corpus and reproduction

`verify_appearance_hardpoints.py ROOT CONFIG PRIVATE_OUTPUT --core-dll CORE_DLL` independently discovers effective `.msh/.lod/.cmp` winners from configured TRE indexes. Zero-size records mask lower-priority data. It compares source SHA/priority, every authored name/CRC/transform bit, sorted index and duplicate name list with one native process. All 31,151 APPR/0003 records agree, containing 13,253 hardpoints/159,036 float fields. Only 2,716 HPTS tables are nonempty. There are 1,118 extra duplicate-name records, 91 groups with different transforms; maximum observed name length is 34 bytes.

The discovery separately reports 22 masks, one non-IFF `.lod` suffix and three legacy outer layouts outside this slice. No malformed IFF is silently skipped. This is the current effective static-family denominator, not all skeletal or character hardpoint coverage. Proprietary names/transforms and detailed inventories remain private. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_HARDPOINTS.md).

## Composed static instance annotations

`AppearanceHardpointSceneQuery.Collect` enumerates every loaded occurrence, including inactive LOD branches, in preorder. Each placement retains instance ordinal, owned child-index chain, owner path, authored record and parent/local composed transform. Optional root transform is engine neutral. Limits: 4,096 instances, 32,768 records, depth 32; cycles/nonfinite composition and exhaustion fail. Instance-scoped exact name queries retain duplicate ambiguity. This does not implement original appearance scaling or virtual attachment lookup.

Independent YT-1300/garage graphs match 26 placements/312 float fields, with floating-point tolerances stated in the [instance checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_HARDPOINT_INSTANCE_INSPECTION.md). `verify_hardpoint_scene.py` retains detailed output privately. Godot `-InspectHardpoints` adds reflected axes, a visibility toggle and one selected label to component/interior inspectors; annotations follow cell/LOD visibility and are excluded from camera geometry bounds. The UI lists up to 512 records; model limits remain separate.
