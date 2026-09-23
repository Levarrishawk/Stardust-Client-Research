# Analysis checkpoint — CKAT frame rate and LOCT speed invariant

Target: all effective `CKAT/0001` clips, with reader field placement checked against verified `Stardust.exe`. This resolves two formerly unlabeled disk scalars by an exact cross-field corpus relationship, not by tag names alone.

The first four bytes of CKAT INFO are an IEEE-754 F32 frame-rate value; the loader retains those bits at template offset `+0x18`. For every one of the **1,354** clips with LOCT, the leading four bytes of LOCT are also F32 and satisfy:

```text
LOCT leading scalar = length(lastPosition - firstPosition)
                    * INFO frameRate
                    / (lastKeyFrame - firstKeyFrame)
```

All **1,354/1,354** deployed tracks match within `1e-4`; the worst absolute mismatch is about `2.6e-6`. This identifies the LOCT scalar as the track's net average locomotion speed in position units per second, assuming the INFO float is frames per second. The corpus includes both 24 and 30 fps examples, and using a hardcoded 30 fps fails on 70 clips. The loader stores the leading scalar at template `+0x68`; a direct playback read of that field was not found in the compressed-animation module, so its consumer remains open. The replacement importer should retain it and can use the equation as a consistency check, but should not overwrite authored values with recomputation without a compatibility test.

Evidence: `0x00825B30` read-only pseudocode and exhaustive ignored scratch probe `local-private/session-2026-09-23-marathon/probe_ckat.py`; sample `all_b_loc_walk_fmle.ans` (30 fps) and `atpt_loc_walk.ans` (24 fps). Confidence: confirmed for raw F32 encoding and corpus equation; strongly inferred for physical unit/label; unknown for downstream scalar consumer.
