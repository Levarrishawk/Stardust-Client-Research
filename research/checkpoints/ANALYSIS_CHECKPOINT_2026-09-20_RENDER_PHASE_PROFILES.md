# ShaderPrimitiveSorter phase profiles

Date: 2026-09-20

## Scope

This block closes the two integer configuration fields assigned to each of the twelve semantic render phases during `client_graphics_application_install_VA_0075A220`. It also identifies the four selectable sorter callbacks and the end-of-environment drain order. Historical enum labels are not present, so neutral profile/mode names are retained.

## Confirmed phase table

The installer registers phase tags in ascending indices, then calls `0x007734D0(index, profile)` and `0x007734F0(index, mode)`. Each phase record is 0x24 bytes.

| Index | Tag | Sort profile | Execution mode |
|---:|---|---:|---:|
| 0 | `ENVM` | 2 | 1 |
| 1 | `TERR` | 1 | 1 |
| 2 | `MAIN` | 1 | 1 |
| 3 | `BLOB` | 1 | 1 |
| 4 | `PREA` | 1 | 1 |
| 5 | `ALPH` | 3 | 2 |
| 6 | `WBLW` | 3 | 2 |
| 7 | `WATR` | 3 | 2 |
| 8 | `WABV` | 3 | 2 |
| 9 | `PSTA` | 3 | 2 |
| 10 | `SHDW` | 2 | 2 |
| 11 | `LAST` | 2 | 2 |

The executable uses `ALPH`; earlier notes that rendered this tag as `ALPH/BLND` were conceptual descriptions, not a second literal tag.

## Phase record and profile selection

`phase_set_sort_profile_VA_007734D0` addresses `phaseBase + index * 0x24` and calls `phase_select_sort_profile_VA_00771C90`. The selector accepts values 0..3 and installs a pair of function pointers at record offsets `+0x04` and `+0x08`:

| Profile | `+0x04` callback | `+0x08` submission/sort callback | Observed comparison keys |
|---:|---:|---:|---|
| 0 | `0x00771FB0` (no-op) | `0x00772AB0` (no-op) | none |
| 1 | `0x00771FC0` (no-op) | `0x00772AC0` (no-op) | phase uses the shared unspecialized path |
| 2 | current default at `0x01923530` (initially `0x00771FD0`) | `0x00772AF0` | record fields `+0x10,+0x14,+0x18,+0x1c` |
| 3 | `0x00772A80` | `0x00772AD0` | record fields `+0x18,+0x1c` |

Profile 2 therefore has a four-key ordering comparator (`0x00772310`), while profile 3 has a two-key comparator (`0x00772A40`). The runtime preparation callback for profile 2 snapshots four values into the sortable 0x28-byte entry; profile 3 refreshes the entry's depth-like float at `+0x20` through a virtual method. Exact historical names for those keys remain unknown.

`phase_set_execution_mode_VA_007734F0` writes the second configured integer directly to record offset `+0x00`. The alternate phase-processing entry `0x00772C40` tests for mode 1 before invoking its callback and immediate draw/clear path; the common end-environment drain `0x00772D60` performs the callback, draw/clear, and stack-pop sequence unconditionally. This establishes a real execution distinction, but not enough evidence exists to label the modes `opaque` and `alpha` as original semantics.

## Drain order and batching

`shader_primitive_sorter_end_environment_VA_00773DE0` walks the phase vector from its first record to its last in 0x24-byte increments. Consequently the visible phase order is exactly the registration/index order in the table above. Each `0x00772D60` call:

1. invokes the phase callback at `+0x04`;
2. iterates the phase's 0x28-byte sortable entries and issues draw work;
3. compacts/clears that phase's entry vector;
4. pops the phase's count-stack entry.

The sorter uses explicit per-environment stacks, so nested cameras/environments do not require a global destructive reset.

## Replacement-client consequence

The semantic phase sequence is compatible with a modern render graph, but these integers must not be copied as DX12 pipeline-state identifiers. A replacement should preserve the phase/tag contract and ordering behavior while mapping each phase to modern passes, material queues, depth policy, and synchronization. Profiles 2 and 3 provide concrete evidence that the original distinguished multi-key state ordering from a depth-oriented ordering path.

## Confidence

- Phase tags, indices, assigned numeric profiles/modes, record stride, callback selection, comparator key offsets, and final drain order: **Confirmed by code**.
- Profile 2 as broadly state/material ordering and profile 3 as broadly depth-oriented: **Strongly inferred**.
- Original enum names and exact meaning of every comparison key: **Unknown**.

## Evidence

- phase registration and constants: `0x0075A380..0x0075A4A0`
- profile/mode setters: `0x007734D0`, `0x007734F0`
- profile selector: `0x00771C90`
- profile comparators: `0x00772310`, `0x00772A40`
- phase execution: `0x00772C40`, `0x00772D60`
- end-environment ordered drain: `0x00773DE0..0x00773E44`
