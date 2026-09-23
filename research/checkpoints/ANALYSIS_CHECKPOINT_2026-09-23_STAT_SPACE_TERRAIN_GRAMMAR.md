# STAT space-terrain loader and effective corpus (overnight block 20)

The configured overlay has **16** effective `terrain/space_*.trn` files with `STAT/0000`; the same extension also has 54 ground `PTAT` files and one zero-length winner. `Stardust.exe` loader `0x00B61290` accepts `STAT/0000` only and calls the version-0 reader `0x00B61360`. Every observed immediate child is a `FORM <tag>` holding a `0000` data chunk. The reader dispatches all eleven observed tags; an unknown tag takes its IFF skip/error path rather than being mapped to a known field.

| Child form | Effective records | Confirmed reader shape / state effect |
| --- | ---: | --- |
| `CLEA` | 16 | Three F32 values converted to U8 clear-color components at template `+0x20..+0x22`. |
| `SKYB` | 16 | U8 enable/mode converted to Boolean plus NUL-terminated logical name stored at `+0x74`. This is an **active STAT parser**, unlike the ground ENVM extension skip case. |
| `AMBI` | 16 | Four-float color via shared color reader, stored at `+0x24..+0x30`. |
| `PARA` | 32 | Repeated 0x30-byte runtime records: one Boolean, two four-float colors and one three-float vector. The two records per file are preserved in order. |
| `ENVI` | 16 | NUL-terminated environment name stored through a string setter at `+0x38`. |
| `FOG ` | 16 | Four-float color converted/packed through `0x00ABA230`; enables fog and stores color at `+0x50`. |
| `STAR` | 16 | NUL-terminated name plus F32 scalar, stored at `+0x58` and `+0x54`. |
| `DUST` | 16 | Two F32 values stored at `+0x68/+0x6c`. |
| `PLAN` | 63 | Repeated record: resource name, three-float vector, then five F32 values. Four of those F32 values are multiplied by `pi/180` before storage, confirming degree angles. |
| `DIST` | 20 | Repeated record: resource name, three-float vector and three F32 degree angles converted to radians. These occur only in Kashyyyk/light/heavy variants. |
| `CELE` | 237 | Repeated record: two resource/name strings, two F32 scalars and one three-float vector, retained in a 0x34-byte x86 vector element. |

The counts are from all 16 effective files, not a sample. `STAT` is therefore a composite space backdrop/environment resource with named sky, star, planet and distant-object records, not a heightfield. The parser uses typed paths and transforms; a replacement should decode into an engine-neutral record graph and preserve ordering/multiplicity. The observed `PLAN`/`DIST` vector3 byte order and actual coordinate-frame application should still be verified against the runtime consumer before assigning X/Y/Z semantic labels; prior server source corroborates `DIST` as a distant-object appearance with position and rotations, but this checkpoint's primary field evidence is the executable.

Confidence: **confirmed** for versions, form dispatch, field widths, record counts, degree conversion, and storage multiplicity; **strongly inferred** for visual roles from tags, strings and class context; **unknown** for exact rendering/visibility, per-frame skybox behavior, and whether all stored records instantiate in every space scene. Provisional name: `0x00B61360` `load_STAT_v0000_space_environment`.
