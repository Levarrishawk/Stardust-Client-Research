# Analysis checkpoint — CKAT translation evaluation

Target: verified `Stardust.exe`; read-only Ghidra output is reconstructed pseudocode.

The eight translation evaluator variants selected by XFIN mask bits combine three per-axis storage objects. Static axes read a scalar once from the INFO-indexed `STRN` float array. Dynamic axes hold a pointer to a 12-byte-key `ATRN/CHNL` channel plus a cached interval index. Their evaluation route calls `0x00822330` for the requested animation time. It scans forward from the cached interval, resets to interval zero if time precedes that interval, and returns the final value when the selected key's reciprocal interval is zero. Between two keys, it computes a weighted linear interpolation using the stored reciprocal time difference: `current*(nextTime-time)/dt + next*(time-currentTime)/dt`. The exact before-first extrapolation behavior and time-clamp policy should be tested at call-site boundaries rather than assumed.

This closes the major runtime semantic gap for CKAT translation: original assets have directly readable float values and per-axis linear key interpolation. It does **not** establish all higher-level skeletal blending, root-motion, or animation-priority behavior.

Evidence: `0x00825B30`, `0x00824B80`, `0x00825230`, `0x00822FC0`, `0x00822710`, `0x00822200`, `0x00822290`, eight constructors `0x00823560`–`0x00823A50`, their vtable evaluation examples `0x008235E0`/`0x00823680`, and dynamic scalar evaluator `0x00822330`. Confidence: confirmed for static/dynamic route, interval cache and linear interpolation; unknown for out-of-range time handling in the owning animation object.
