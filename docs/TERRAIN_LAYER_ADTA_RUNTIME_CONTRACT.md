# LAYR ADTA words and fresh runtime flags

## Bounded question and identity

Which ADTA words feed the inversion bytes used by the general layer loop?
This is a loader/setter/constructor mapping, not a complete layer evaluator,
cache activation analysis or original runtime-state capture. The original
executable pin is
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
`tools/python/verify_terrain_layer_adta.py` checks it before imports or address
decoding, then independently checks exact reads and setters. It optionally
checks one original raw layer in the same pinned Tatooine input; details stay
in a private receipt.

## Direct mapping: Confirmed

| Layer version | ADTA word0 | word1 | word2 | word3 |
|---|---|---|---|---|
| 0001 | boundary inversion | filter inversion | absent | absent |
| 0002 | boundary inversion | filter inversion | runtime byte+68 | absent |
| 0003 | boundary inversion | filter inversion | runtime byte+68 | absent |
| 0004 | boundary inversion | filter inversion | consumed/discarded | runtime byte+68 |

Each mapped word is read as four bytes and normalized with `test`/`setne`.
Every nonzero raw word becomes byte one, including words whose bits would be
negative integers or nonfinite floats. These words are not F32 arithmetic
operands. The owned raw input retains their exact words; semantic labels do
not justify normalizing the canonical input data.

Loader starts `00B64A30`, `00B64AE0`, `00B64BB0`, `00B64CA0` read2/3/3/4
words. The first two call setters `00B65FF0`/`00B66000`, which write byte
`+0x50`/`+0x51` (`00B65FF6`, `00B66006`). The third mapped word calls
`00B66010`, writing byte `+0x68` at `00B66016`. In version0004, the third
four-byte read at `00B64D15` fills a temporary overwritten by the fourth read
at `00B64D22`; only the fourth value is normalized and passed to the setter.
Its ignored raw word's original semantic purpose remains **Unknown**.

Layer constructor `00B63560` sets these bytes and distinct byte `+0x52` to
zero (`00B635B6`, `00B635B9`, `00B635BC`, `00B635CE`). Thus a **fresh**
version0001 load retains byte+68 zero. The version0001 loader does not
explicitly reset it; reloading an existing runtime instance is not proved
equivalent to constructing a new one.

The [general influence sequence](TERRAIN_LAYER_INFLUENCE_CONTRACT.md) reads
byte+50 at `00B6402D` before the boundary inversion subtraction, and byte+51
at `00B64124` before inversion of the combined boundary/filter accumulator.
This independently connects serialized words to the two arithmetic branches.
Byte+52 controls a bounding comparison path at `00B63D51`; it is a different
member from ADTA's byte+68. This probe does not establish all writers of+52.

## Unknowns and compatibility consequences

Byte+68's role remains **Unknown**. It must not be named as boundary culling,
height filtering or inherited enable from its proximity or third-word order.
IHDR local-enabled metadata is separately described in the
[TGEN input contract](TERRAIN_GENERATOR_INPUT_CONTRACT.md). ADTA inversion
does not establish ancestor/local-enabled composition or dynamic cache state.

A replacement may project supported **fresh** raw layer records into the two
known inversion booleans while preserving the raw words and unknown+68 flag.
Version0004 must not mistakenly interpret word2 as that flag or word3 as a
new independent feature. Version0001 defaults are conditional on fresh
construction. No existing raw decoder or completed arithmetic helper needs
to change to preserve this distinction.

Next probe: trace byte+68 reads/callers and separately trace+52 writes/cache
activation before adding a complete runtime layer descriptor or traversal.
## Round41 supplied height execution and CREO4 fields

[Preparation and bounds](TERRAIN_LAYER_PREPARATION_CONTRACT.md) confirm byte52 is local bounding readiness, distinct from serialized unknown68. Inverted bounds return before resetting rectangle state. No original cache capture is claimed.
