# Analysis checkpoint — CKAT combined root-motion order

Question: A03 / W08. Target is the verified original `Stardust.exe`; disassembly is executable evidence, while decompiler text is reconstructed pseudocode. This closes the **computational order** of the rare top-level QCHN+LOCT mode, not every world-coordinate convention or external actor-transform call site.

## Installed scope

Only two of the 7,771 effective CKAT clips have both optional top-level channels: `appearance/animation/all_b_cbt_hop_turn_right_90.ans` (11 QCHN keys, eight LOCT keys) and `appearance/animation/mouse_droid_idl_stand.ans` (106 QCHN keys, 14 LOCT keys). The names and key counts come from the read-only effective-asset scan in `tools/python/ckat_combined_root_samples.py`. Thus this mode is real installed content, albeit uncommon; asset presence is not proof of ordinary gameplay reachability.

## Executable contract

`0x00825710` selects `0x008267A0` when both top-level QCHN and LOCT exist. In `0x008267A0`, the two caller times are sampled into `q0`, `q1`, `p0`, `p1`. The instruction chain calls `0x0081EE40` to conjugate `q0`, then `0x00ABA9F0` with `q1` as its object and `conj(q0)` as its argument. Inspection of that routine's multiply terms establishes `q_relative = q1 * conj(q0)` (confirmed for its raw `(w,x,y,z)` quaternion storage). The function copies that quaternion to the caller's rotation output.

Translation is **not** simply the LOCT difference as in the LOCT-only `0x00826930` path. The combined routine first computes `d = p1-p0` (three stored float components). It passes **q1**, not q0 or q_relative, to `0x00ABA530`, which creates a 3×3 quaternion-derived matrix in a 16-byte-stride row layout. Then the scalar instructions at `0x0082689C..0x008268FF` write:

```text
out[0] = M[0,2]*d[0] + M[1,2]*d[1] + M[2,2]*d[2]
out[1] = M[0,1]*d[0] + M[1,1]*d[1] + M[2,1]*d[2]
out[2] = M[0,0]*d[0] + M[1,0]*d[1] + M[2,0]*d[2]
```

In conventional matrix notation this is the component-reversed result of `transpose(M(q1)) * d`. That notation is an algebraic description of the instructions, **not yet** a claim that the result is a particular named SWG world/local axis convention. The original caller's basis conventions and application of an external actor transform remain to be traced. In particular, substituting q_relative for q1 in a replacement would differ from the executable.

Evidence: original instructions in `0x008267A0..0x00826910`, `0x0081EE40`, `0x00ABA9F0`, `0x00ABA530`; reconstructed pseudocode in ignored local scratch files of those addresses; previous [root-motion dispatch](ANALYSIS_CHECKPOINT_2026-09-23_CKAT_ROOT_MOTION_OPTIONALS.md) and [LOCT corpus](ANALYSIS_CHECKPOINT_2026-09-23_CKAT_LOCT_CORPUS_AND_ROOT_MODES.md). Confidence: **confirmed by code** for multiplication and scalar write order; **strongly inferred** conventional matrix shorthand; **unknown** external actor frame and normal-play reachability.

Outcome: A03 narrowed to the external application/basis boundary; no whole-client percentage change. A later differential fixture should use one of the two named clips and compare caller-side actor displacement, not rediscover this inner formula.
