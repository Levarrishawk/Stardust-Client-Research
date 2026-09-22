# Analysis checkpoint — LEFX lightning appearance

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The LEFX loader, deployed corpus and runtime rendering boundary are reconstructed to replacement depth.

| Stage | Address | Result | Confidence |
|---|---:|---|---|
| install | `0x007B3570` | registers tag LEFX and factory `0x007B3500` | Confirmed |
| dispatch | `0x007B3630` | accepts only 0000, 0001 and 0002 | Confirmed |
| v0000 | `0x007B37C0` | PTEX plus legacy scalar/string record | Confirmed order |
| v0001 | `0x007B3B80` | PTEX, two WVFM records, scalar/string record and discarded legacy byte | Confirmed order/use |
| v0002 | `0x007B3E90` | PTEX, two WVFM records and expanded scalar/string record | Confirmed order |
| runtime | `0x007A9BE0` | local shader primitive, five fixed branch work records, procedural dynamic geometry | Confirmed broad behavior |

## Corpus

The configured effective overlay contains 49 live `.ltn` winners, no tombstones and zero structural/field-count errors:

| Version | Count | Child signature |
|---|---:|---|
| 0000 | 2 | `PTEX/0000 / 0000` |
| 0001 | 6 | `PTEX/0000 / WVFM/0001 / WVFM/0001 / 0000` |
| 0002 | 30 | `PTEX/0000 / WVFM/0001 / WVFM/0001 / 0000` |
| 0002 | 11 | `PTEX/0000 / WVFM/0002 / WVFM/0002 / 0000` |

SWG IFF chunks are packed exactly to declared size; odd-sized chunks do not receive RIFF pad bytes. This was verified across all 49 variable-length records.

The embedded PTEX records use thirteen unique `.sht` resources. Every shader and every non-empty start, end and sound reference resolves in the same configured overlay. There are no missing LEFX dependencies.

## Scalar/string contract

Each final chunk begins with one F32 and three zero-terminated logical resource paths. Corpus identities establish their roles:

1. start appearance (`.prt`, often `*_start*`);
2. end/target appearance (`.prt`, often `*_end*`);
3. optional sound (empty in 48 records; `sound/shp_wep_tractor_beam.snd` in the tractor-beam record).

The remaining ordered payload is:

- v0000: fifteen further F32 values (16 F32 total);
- v0001: one F32, two U32 attachment-transform flags, one discarded legacy byte and fourteen further F32 (16 F32 total);
- v0002: two U32 flags and eighteen further F32 (19 F32 total).

Both U32 fields use only values 0/1 in the effective corpus. Runtime call sites `0x007AB412` and `0x007AB4AA` test them independently before applying full transform/orientation handling to the start-side and end-side attachment objects. The v0001 byte is read into a stack temporary but never copied into the template or consulted; all six effective values are zero. It is confirmed legacy compatibility data, not an active Boolean option. The loaders map the remaining values into width/color/waveform/branch-control state, but many original editor labels are not recoverable. The compatible strategy is a versioned ordered structure with known runtime consumers rather than invented names.

## Runtime and replacement

The runtime appearance is large because it embeds five fixed branch work records. It owns a local shader primitive, endpoint state, dynamic vertex preparation and visibility ownership. Rendering samples PTEX, color/scalar channels and the two waveform records, then emits procedural lightning geometry proportional to active branches.

A modern renderer may replace CPU-expanded geometry with a GPU ribbon/strip implementation. It must preserve:

- PTEX material and atlas animation;
- both WVFM channels;
- five-branch maximum behavior;
- start/end attachment appearances;
- optional sound;
- versioned field order and defaults.

Exact visual equations for every scalar remain a fidelity refinement, not an asset-container blocker.
