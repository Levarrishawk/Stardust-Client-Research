# Analysis checkpoint — CKAT INFO frame count

Target: all 7,771 effective `CKAT/0001` original clips, with field placement checked against verified `Stardust.exe`.

The first signed-16 value after CKAT INFO's F32 frame rate is a clip end-frame/count field. The loader stores it at template `+0x1C`, and the runtime constructor at `0x00822FC0` retains it as float playback length. Exhaustive corpus comparison across the **7,505** clips with AROT, ATRN or LOCT key tracks finds that each clip's maximum key time equals this declared value exactly: **7,505/7,505**, with zero keys later than the field. The other **266** clips have no such sampled track in this audit; this block does not assert their event-time maxima. The declared value ranges from **1 to 1,440** frames. INFO frame rate is **30.0** in 7,188 clips and **24.0** in 583 clips.

This gives importer checks for clip-time bounds and a natural duration calculation (`endFrame / frameRate`) while keeping the exact original endpoint convention subject to playback verification. In particular, do not silently reinterpret frame indices as seconds or assume 30 fps for every clip.

Evidence: `0x00825B30`, `0x00822FC0` read-only pseudocode and ignored full-corpus probe `local-private/session-2026-09-23-marathon/probe_ckat.py`. Confidence: confirmed for encoding, range and tracked-clip invariant; strongly inferred for final playback-duration formula.
