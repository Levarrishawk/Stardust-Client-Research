# Analysis checkpoint: MLOD runtime contract

Scope is the verified `Stardust.exe` image only (SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`)
and the effective configured archive overlay.

## Confirmed file grammar

`LodMeshGeneratorTemplate` registers `MLOD` at `0x007F8E10`. Its factory
`0x007F8FF0` constructs the 0x30-byte template object at `0x007F9050`.
`0x007F9290` accepts only `MLOD/0000` and reads:

```text
FORM MLOD
  FORM 0000
    CHUNK INFO
      int16 meshNameCount       // negative values clamp to zero
    CHUNK NAME × meshNameCount
      zero-terminated mesh-generator asset path
```

The loader reserves the owned asset-name vector at object offsets
`+0x18..+0x20`, then appends accepted NAME values. It sizes a parallel
initially-null cached-generator vector at `+0x24..+0x2c` for the serialized
count.

The effective overlay contains 4,882 `.lmg` files, all `MLOD/0000`. NAME-count
distribution is:

| NAME count | Files |
|---:|---:|
| 1 | 86 |
| 2 | 12 |
| 3 | 193 |
| 4 | 4,405 |
| 5 | 96 |
| 6 | 68 |
| 7 | 21 |
| 8 | 1 |

## Confirmed runtime behavior

The MLOD vtable at `0x015E57D8` begins with:

| Slot | Function | Contract |
|---:|---:|---|
| `+0x00` | `0x007F8EE0` | returns `min(meshNameCount, 4)` |
| `+0x04` | `0x007F8F10` | lazy-resolves one named mesh generator and returns an added reference |
| `+0x08` | `0x007F8F50` | iterates exposed levels, resolves/preloads each, then releases the temporary reference |
| `+0x0c` | `0x007F8FA0` | releases and nulls all cached mesh generators |
| `+0x10` | `0x007DF970` | inherited base-template operation |
| `+0x14` | `0x007F9150` | deleting destructor |

Thus the file permits eight names, while this client exposes only the first
four logical levels. A replacement importer should preserve the complete list
but reproduce the four-level client policy where compatibility matters.

At install, configuration key `ClientSkeletalAnimation.skipL0` is read. When
enabled and an MLOD has more than one name, the loader deliberately skips NAME
index zero and emits `LodMeshGeneratorTemplate: skipping L0`. This is a
development/performance policy, not malformed asset data. Because names are
appended rather than written by serialized index, the skip removes L0 and
shifts later names down; it does not leave a null name slot. The exposed count
therefore becomes `min(serializedCount - 1, 4)` in this mode.

Each non-skipped NAME becomes an owned path object. Its cache entry remains
null until `0x007F8F10`; that function calls common mesh-generator-template
fetcher `0x007D1690`, stores the shared result, and increments its reference
count for the caller. Destruction releases name objects, every non-null cached
generator through `0x007DF980`, and both vectors.

## Boundary of this block

MLOD stores no distance thresholds. SAT `LDTB`, already mapped at
`0x013A4910`/`0x013A4780`, supplies the distance/hysteresis policy. Selection
of an MLOD child is therefore a higher-level skeletal-appearance decision, not
a hidden `.lmg` field. The exact consumer call site combining selected detail
with the MLOD interface remains targeted; the container grammar, level cap,
lazy load, preload, skip-L0 option, and lifetime contract are resolved.

## Replacement requirements

- Parse the signed 16-bit count and reject truncated NAME sequences.
- Retain all names, but expose a compatibility view capped at four.
- Resolve children lazily through the asset overlay and cache immutable
  templates; never serialize native pointers.
- Model `skipL0` as an optional diagnostic/performance setting.
- Keep SAT distance policy separate from the MLOD container.
