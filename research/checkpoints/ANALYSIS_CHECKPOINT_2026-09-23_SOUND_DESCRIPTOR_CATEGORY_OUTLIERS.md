# Analysis checkpoint — two malformed sound category outliers

Target: verified `C:\Stardust-DEV\Stardust.exe` and configured effective TRE overlay. Static read-only inspection; no playback test was performed.

An audit of the `SD2D/0003` scalar inventory found two extreme `+0x44` categories that had not been called out beside the known category-13 defect:

| Effective descriptor | Archive | Client-decoded `+0x44` | Other shifted fields |
|---|---|---:|---|
| `sound/taan_sunrise_a.snd` | `mtg_planets.tre` | 2048 | `+0x2C/+0x30 = 256`, `+0x84 = 2304` |
| `sound/taan_sunset_a.snd` | `mtg_planets.tre` | 524288 | `+0x2C/+0x30 = 65536`, `+0x84 = 589824` |

The raw records are well-framed `SD2D/0003` IFF chunks of 141 and 140 payload bytes. They contain one `.mp3` sample path followed by apparent extra zero padding before the expected scalar values. The observed `08` and `09` bytes resemble ordinary music-category and priority values shifted by one or two byte positions, respectively. This pattern is **strong evidence of malformed authored scalar alignment**, not evidence that 2048 or 524288 are intended category enumerators. However, the client's actual sequential parser does not skip that apparent padding: `0x0041DB30` reads the sample string then exactly 28 four-byte fields, and `0x00A951B0` advances only through the terminating NUL with no alignment. Therefore the huge category values are **confirmed client parser outputs** under the observed bytes. The older fixed-tail scalar CSV did not invent them.

The gain accessor `0x0040C780` indexes a 13-element table without a category bound check. If one of these malformed descriptors reaches gain lookup, it presents a much more severe out-of-bounds risk than category 13. A configured-overlay scan of decompressed payloads found **no literal reference** to either basename outside the two descriptors. That makes ordinary use unproven, not impossible: dynamic lookup or externally supplied names were not excluded.

Replacement policy: preserve the original asset bytes for forensic fidelity, validate the decoded category before indexing a mixer table, and log/substitute a safe explicit gain for invalid values. A compatibility importer may expose a separately labeled repair heuristic for these two assets, but must not silently rewrite their serialized values or claim that Stardust.exe itself normalizes them. Do not infer intended categories without an authored-format source or runtime fixture.

Confidence: bytes, sequential parser and out-of-range category are **confirmed**; accidental padding cause is **strongly inferred**; live encounter frequency and intended corrected values are **unknown**.

Evidence: ignored `probe_taan_sound.py`, `scan_taan_refs.py`, effective sound overlay and scalar CSVs, and read-only decompilation of `0x0041DB30`/`0x00A951B0`.
