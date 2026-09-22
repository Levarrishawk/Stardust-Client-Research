# Stardust.exe checkpoint: effective appearance/animation corpus and optional forms

Target: `C:\Stardust-DEV\Stardust.exe`, SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Asset source: the 48 search-tree archives configured by
`C:\Stardust-DEV\swgemu_live.cfg`, resolved in client overlay order.

## Outcome

An effective-overlay census now covers 38,440 skeletal appearance, skeleton,
mesh-generator, logical-animation and clip resources. This replaces
sample-based version assumptions with corpus-wide evidence and identified
rare optional chunks that a first importer would otherwise miss.

## Effective resource/version matrix

| Extension | Effective records | Outer/version distribution |
|---|---:|---|
| `.sat` | 4,926 | 4,925 `SMAT/0003`; one zero-byte overlay record |
| `.skt` | 270 plus one path named `appearance/skeleton/.skt` | 113+1 `SKTM/0002`; 157 `SLOD/0000` |
| `.mgn` | 19,575 | 368 `SKMG/0003`; 19,206 `SKMG/0004`; one zero-byte record |
| `.lmg` | 4,882 | all `MLOD/0000` |
| `.lat` | 195 | all `LATT/0000` |
| `.ans` | 8,591 | 7,771 `CKAT/0001`; 817 `KFAT/0003`; three zero-byte records |

The executable accepts older versions already documented, but none is
selected by this installed effective overlay. The six zero-byte records are
overlay entries rather than malformed IFF trees: one SAT, one MGN and three
ANS paths are apparent content removals, while the odd empty-extension
skeleton path is a valid 167-byte SKTM. The VFS must distinguish a present
zero-length overlay member from a valid IFF and never parse it as a truncated
normal asset.

## Skeletal appearance `SMAT/0003`

Every nonempty SAT begins with mandatory `INFO/MSGN/SKTI`:

- `INFO`: LE U32 mesh-generator count, LE U32 skeleton-template count, and a
  Boolean byte stored at template offset `+0x28`;
- `MSGN`: that many NUL mesh-generator paths;
- `SKTI`: for each skeleton, a NUL skeleton path and NUL attachment-transform
  name, followed by skeleton-template resolution.

Optional form distribution:

| Suffix | Count | Confirmed structure/consumer |
|---|---:|---|
| none | 4,139 | mandatory data only |
| `LATX` | 581 | U16 count then skeleton-path/LAT-path string pairs |
| `LATX + LDTB` | 196 | logical-animation associations plus LOD distance table |
| `LATX + SFSK` | 6 | one Boolean to `+0x5D`; queried by skeletal LOD/update selection |
| `LATX + APAG` | 3 | one Boolean to `+0x5E`; queried by periodic animation/action generation |

SFSK occurs only on banner/flag appearances and its getter is called at
`0x007C8D62`. In the skeletal update selector, near distance still chooses
detail index 0, but when SFSK is true any distance beyond the near threshold
forces detail index 2 and bypasses the ordinary intermediate-index-1 test.
APAG occurs only on three distant-ship controller appearances and its getter
is called at `0x0083A0D9`; it makes the periodic animation/action generator
immediately eligible instead of requiring the ordinary last-update age test.
The historical acronym expansions remain unknown, but both runtime effects
are closed. Preserve them as typed policies rather than guessed names.

`hum_f.sat` demonstrates LDTB: U16 count 4 followed by four float32 intervals
`(-1,10)`, `(9,20)`, `(19,40)`, `(39,10000)`. Intervals overlap at LOD
boundaries, so they are not merely four adjacent cutoff scalars. Loader
`0x013A4910` stores the square of both values. Selector `0x013A4780` clamps a
current-index hint, multiplies that interval's squared bounds by a supplied
scale, and scans downward or upward only when squared distance leaves the
current interval. The one-unit overlap is deliberate hysteresis that prevents
LOD oscillation. Writer `0x013A4840` square-roots the stored values back to the
disk representation.

The v3 reader is `0x007DB3A0`; dispatcher `0x007DA790` accepts SMAT 0001-0003.
SFSK/APAG getters are `0x007D9E70` and `0x007D9E80`.

## Skeleton and mesh structural breadth

- All 114 effective basic skeletons are `SKTM/0002` with the sequence
  `INFO/NAME/PRNT/RPRE/RPST/BPTR/BPRO/JROR`.
- `SLOD/0000` contains 1-7 nested SKTM forms; consume all safely even though
  runtime active-detail selection is narrower.
- `MLOD/0000` contains 1-8 NAME chunks; four names dominate (4,405 files), but
  this is a count-driven list, not a fixed four-element record.
- SKMG shares `INFO/SKTM/XFNM/POSN/TWHD/TWDT/NORM` and one or more `PSDT`
  forms. Optional families are `BLTS`, `HPTS`, `OZN `/`FOZC`/`OZC `/`ZTO `,
  `DOT3`, and `TRTS`.
- 8,815 MGN files carry DOT3, 259 carry HPTS, and 16 carry TRTS. These rare
  paths require compatibility fixtures.

## Newly exposed animation chunk: QCHN

Thirty-two CKAT/0001 clips contain a final `QCHN`, including turn/rotate
animations. The tag also names every animated-rotation channel nested beneath
`AROT`. The CKAT reader at `0x00824870` consumes an exact compressed payload:
signed U16 key count, three channel/quantization bytes, then `count` records of
U16 frame index plus U32 packed quaternion (six bytes per record). It expands
each to a 12-byte runtime key and finalizes the channel. The KFAT/0003 reader
at `0x0081BAB0` consumes U32 key count followed by U32 frame index and an
uncompressed quaternion value, building 24-byte runtime keys. A final CKAT
QCHN is the locomotion/root-rotation channel; ordinary QCHNs under AROT are
joint rotation channels. Decoder `0x00AC0D90` closes the packed word: bits
31..21 hold a sign plus 10-bit magnitude, bits 20..10 hold a sign plus 10-bit
magnitude, and bits 9..0 hold a sign plus 9-bit magnitude. The three header
bytes select per-component quantizer lookup families generated by
`0x00AC0E20`; they are encoded selectors, not literal bit counts. The fourth
component is reconstructed as nonnegative `sqrt(1-a*a-b*b-c*c)`. Only the
decoded result is definitively stored in engine quaternion order `[w,x,y,z]`:
the reconstructed nonnegative component is `w`, while packed fields 31..21,
20..10 and 9..0 are respectively `x`, `y` and `z`. Wrapper `0x00AC13E0`
passes output addresses in that order, and axis-angle constructor `0x00ABA490`
independently proves the same memory convention by writing
`[cos(a/2), axis.x*sin(a/2), axis.y*sin(a/2), axis.z*sin(a/2)]`. Matrix builder
`0x00ABA530` supplies the matching quaternion-to-basis conversion. Host-engine
coordinate conversion still belongs at the explicit SWG-to-renderer boundary,
but QCHN component identity itself is no longer unknown.

## Replacement consequences

1. Use version-explicit, bounds-checked readers; never infer counts from the
   most common deployed record.
2. Preserve optional chunks and flags in engine-neutral IR.
3. Maintain fixtures for every version and rare family above, including
   zero-length overlay tombstones.
4. Preserve QCHN `[w,x,y,z]` semantics and test the explicit SWG-to-host-engine
   coordinate adapter with pose fixtures.

Reusable census: `appearance_overlay_inventory.py`; row-level output:
`appearance_overlay_inventory.csv`.
