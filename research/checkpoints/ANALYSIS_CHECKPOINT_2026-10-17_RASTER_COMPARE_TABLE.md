# Original raster comparison table — October 17, 2026

## Bounded question and stopping condition

Does the original raster module translate logical depth comparison index 1 to
Direct3D LESS (2), or does the conversion introduce that value? Stop after
recovering the original table and its consumers, separating this numeric mapping
from shadow geometry, device execution and any diagnostic trial.

This closes the comparison-table element question retained in the
[rendering architecture](../../docs/RENDERING_ARCHITECTURE.md#exact-core-render-state-map).
It extends the [PASS compiler investigation](ANALYSIS_CHECKPOINT_2026-09-12_PASS_D3D9_COMPILER.md)
and the later [blend/write-mask correlation](IMPLEMENTATION_CHECKPOINT_2026-10-01_INLINE_FACE_EFFECTS.md).
No original binary is executed, no live contact or new capture is performed,
and no implementation behavior changes in this research work.

## Input identity and address domain

- Input: `C:\Stardust-DEV\s207_r.dll`, 1,634,389 bytes, x86 PE32.
- SHA-256: `D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D`.
- Preferred image base: `0x62A00000`.
- Orientation-only primary input: `Stardust.exe`, independently rechecked SHA-256
  `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
  No primary-executable address is needed for this result.

Every address below belongs to **s207_r.dll**, not Stardust.exe. An RVA is
relative to this DLL's preferred image base; the preferred VA is base plus RVA,
not an observed runtime load address. PE section mapping establishes the listed
file offsets; equality between these RVAs and file offsets is specific to these
sections in this input and is not a general PE rule.

| Evidence | Module RVA | Preferred VA | File offset |
|---|---:|---:|---:|
| Comparison table, eight U32 entries | `0x0015143C` | `0x62B5143C` | `0x0015143C` |
| Depth-index load from logical PASS `+0x1C` | `0x0000B504` | `0x62A0B504` | `0x0000B504` |
| Depth indexed table read | `0x0000B507` | `0x62A0B507` | `0x0000B507` |
| Depth pair's state identifier 23 | `0x0000B519` | `0x62A0B519` | `0x0000B519` |
| Alpha-index load / table read | `0x0000B662 / 0x0000B665` | `0x62A0B662 / 0x62A0B665` | `0x0000B662 / 0x0000B665` |
| Front-stencil-index load / table read | `0x0000B719 / 0x0000B71C` | `0x62A0B719 / 0x62A0B71C` | `0x0000B719 / 0x0000B71C` |
| CCW-stencil-index load / table read | `0x0000B88E / 0x0000B891` | `0x62A0B88E / 0x62A0B891` | `0x0000B88E / 0x0000B891` |

## Observations and interpretation

**Confirmed observation:** the table contains eight little-endian U32 values:
`1, 2, 3, 4, 5, 6, 7, 8`. At RVA `0xB504`, the compiler reads a U32 index from
logical PASS `+0x1C`. At `0xB507`, it reads the table at that index times four.
The following pair construction stores the selected value alongside state
identifier `0x17` (23, ZFUNC). The existing state-vector application boundary
then submits compiled state/value pairs. This is a lookup from a logical runtime
PASS member; `+0x1C` is not a serialized asset byte offset.

The same table is read for logical PASS `+0x38`, `+0x54` and `+0x64`, corresponding
to alpha compare, front stencil compare and counterclockwise stencil compare in
the already established render-state map. The bounded lookup instructions do
not establish malformed-index validation in callers or loaders.

| Logical index | Original numeric D3DCMPFUNC | Meaning |
|---:|---:|---|
| 0 | 1 | NEVER |
| 1 | 2 | LESS |
| 2 | 3 | EQUAL |
| 3 | 4 | LESSEQUAL |
| 4 | 5 | GREATER |
| 5 | 6 | NOTEQUAL |
| 6 | 7 | GREATEREQUAL |
| 7 | 8 | ALWAYS |

**Confirmed interpretation:** index 1 means LESS, not LESSEQUAL. Index 3 means
LESSEQUAL. The conversion's current numeric table agrees with the recovered
original table. Source enum names at indices 5/6 do not override these numeric
observations and are not evidence for changing the table.

Separate source-conversion reconciliation finds the effective two-sided shadow
PASS0009 depth index is 1 and the three sampled edge/front-cap/composite stages
agree with all 20 compared declared fields. Both sampled pixel-program
fingerprints also match their effective program bytes. These are supporting
asset/source/wrapped-device observations, not original raster execution. Their
runtime identities and trial receipts belong to the separate conversion record;
they are not new research coverage or an accepted shadow repair here.

## Reproduction and validation

Tools: the existing [bounded disassembler](../../tools/python/disasm_range.py),
Capstone x86-32 and pefile from the configured private static-analysis package
directory, plus Python standard-library SHA-256, PE-header reads and little-endian
U32 unpacking. This worker independently rechecked both hashes, section mapping,
the 32-byte table and the four indexed consumer sites. Only compact independently
written observations are retained publicly; original binaries and detailed
private evidence remain outside the curated record.

From the repository root, using the configured Python interpreter:

```powershell
python tools/python/disasm_range.py C:\Stardust-DEV\s207_r.dll 0x62a0b340 0x62a0b9b0
```

Before interpreting those preferred VAs, verify the DLL SHA-256 above. Read 32
bytes at PE-mapped RVA `0x15143C` and unpack `<8I`; require exactly the eight
values listed above. As an independent raw-file check for this pinned input,
the same 32 bytes occupy file offset `0x15143C`. The depth load/read instruction
bytes at file offset `0xB504` are `8B 57 1C 8B 04 95 3C 14 B5 62`.

Workspace/link validation passes: 1,973 Markdown files, zero broken links, zero
private links, 2,626 unique provisional addresses with zero duplicates, and 345
packet rows. Focused whitespace/diff inspection passes; existing unrelated
dirty work is preserved. No original dynamic execution, GPU test or source build
is asserted by these static checks.

## Consequences, remaining uncertainty and next probe

Preserve the recovered numeric mapping when interpreting original PASS assets.
Do not globally substitute LESSEQUAL for LESS to explain the observed shadow
artifact: the sampled value agrees with the effective asset and original table.
An isolated front-cap comparison experiment would be an authored diagnostic
deviation, with its own identity and restoration receipt, not recovered parity.

The shadow fault remains Unknown. Wrapped render-state receipts do not measure
actual matrix constants, equality of visible-mesh and volume projection,
extrusion vertices, cap closure/winding, per-caster identity, stencil contents
or downstream wrapper/driver execution. Next: select one bounded same-caster
geometry/matrix or equal-depth discriminator, preserve the baseline, and keep
any result separate from this closed table question. No new primary-registry
entry, semantic-address count, packet count or coverage/readiness score follows.
