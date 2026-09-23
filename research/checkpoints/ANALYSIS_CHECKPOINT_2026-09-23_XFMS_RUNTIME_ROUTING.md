# Analysis checkpoint — XFMS runtime transform routing

Target: verified `C:\Stardust-DEV\Stardust.exe`, static Ghidra decompilation only. Addresses are provisional identifiers; pseudocode is not original source.

The preceding XFMS block established nine effective name-list assets and the loader. This block traces the list into animation runtime behavior.

| Stage | Candidate function | Observed behavior | Confidence |
|---|---|---|---|
| masked-priority template decode | `0x00817BA0 -> 0x00817F80` (or older `PPTR` path `0x00817DE0`) | Reads `transformMaskLocation`, `transformInGroupName`, `transformOutGroupName`, and `locomotionGroupName`; mask location goes through cached `0x008291B0` loader | Confirmed by code |
| runtime instance creation | virtual template method `0x00818110` | Passes loaded mask and three group identifiers to `0x0083D350` | Confirmed by code |
| skeleton-size allocation | `0x0083D350` | Allocates one 32-bit group-reference slot per transform reported by the target skeleton, then invokes `0x0083CA00` | Confirmed by code |
| membership routing | `0x0083CA00 -> 0x0083C950` | Iterates skeleton transform indices, obtains each transform name, tests membership in XFMS name set, assigns transform-in group if present and transform-out group if absent | Confirmed by code |

`0x0083C950` compares each target transform name against the ordered CRC-string set loaded from `INGR`, returning a boolean membership result. At `0x0083CA00`, that result selects the first group argument for members and the second for nonmembers. The caller `0x0083D350` supplies the template's transform-in and transform-out group identities in that order. This establishes the mask's inclusion polarity. The separate locomotion group is retained on the runtime object; its precise priority interaction is outside this narrow trace.

For a replacement client, import XFMS as a set of transform names and build a per-skeleton routing array at animation-instance creation. Resolve names against the actual skeleton rather than relying on source-client pointer values, disk order, or a fixed bone index scheme. The data contract is name membership; the original 32-bit allocation width is an internal implementation detail. Exact blend/priority mathematics beyond the in/out group route remains a separate question.

Evidence: read-only decompilations under ignored `local-private/session-2026-09-23-marathon`, especially `00818110`, `0083D350`, `0083CA00`, and `0083C950`; effective XFMS corpus enumerated in the preceding checkpoint.
