# Analysis checkpoint — deployed SKTM/0002 disk layout

Target: verified `C:\Stardust-DEV\Stardust.exe` and configured effective original TRE overlay. This narrows the older skeleton-format checkpoint's partially open disk-field order. Decompiled output is reconstructed pseudocode, not original source.

The version dispatcher sends `SKTM/0002` to `0x0082B320` (`0x0082AC30` is the `0001` reader). The v0002 reader consumes the following exact ordered child chunks. Every array cardinality is the U32 `INFO` joint count `N`.

| Child | Disk content established by reader | Width |
|---|---|---:|
| `INFO` | U32 transform count `N` | 4 |
| `NAME` | `N` NUL-terminated transform names | variable |
| `PRNT` | `N` signed/bit-equivalent 32-bit parent indices (`-1` root sentinel in corpus) | `4N` |
| `RPRE` | `N` four-F32 rotation records | `16N` |
| `RPST` | `N` four-F32 rotation records | `16N` |
| `BPTR` | `N` three-F32 vector records | `12N` |
| `BPRO` | `N` four-F32 rotation records | `16N` |
| `JROR` | `N` U32 values | `4N` |

The v0001 reader additionally visits `BPMJ`; v0002 does not. The eight v0002 child tags above are an exact reader/corpus sequence, not a claim that historical names like “pre” and “post” fully specify the mathematical convention. The executable stores each parsed family in a distinct runtime vector and builds a name-to-index map. `BPTR` is a 3-vector by its 12-byte stride, and the three 16-byte families are rotation/quaternion-shaped; precise component handedness and pre/post multiplication order still require runtime pose evaluation or known-pose fixtures.

An effective-overlay audit visited all `.skt` winners, including inline SKTM children of `SLOD/0000`, and decoded **739 SKTM/0002 records**: 114 standalone and 625 nested. Every record matched the count-derived byte widths and exactly `N` names; no malformed chunk, invalid parent index, self-parent or nonfinite tested rotation component was found. Joint count ranges **0..73**. 737 records have exactly one `-1` root. Two valid zero-joint hand skeletons (`hum_m_hands.skt`, `bth_f_hands.skt`) have no root, so a replacement parser must allow `N=0` and must not require one root unconditionally.

This closes the deployed disk-field order and size contract. A follow-up corpus audit (`ANALYSIS_CHECKPOINT_2026-09-23_SKTM_HIERARCHY_INVARIANTS.md`) confirms parent-before-child order and unit quaternions across all effective records. It does **not** close bind-pose transform composition, units, axis convention, or `JROR` semantic values. A renderer/animation importer should preserve named arrays and validate skeleton-parent topological order before evaluating transforms, with those math conventions verified against original asset poses.

Evidence: read-only `0x0082B320` and `0x0082AC30` decompilations, three representative raw file probes, and ignored `local-private/session-2026-09-23-marathon/audit_sktm_corpus.py`.
