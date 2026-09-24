# Default command parser: template and graphics-resource controls

This is a targeted continuation of the [default-parser token inventory](ANALYSIS_CHECKPOINT_2026-09-24_DEFAULT_COMMAND_PARSER_TOKEN_INVENTORY.md), using the original `C:\Stardust-DEV\Stardust.exe`. It does not claim closure of the other parser branches. Decompiled output is reconstructed pseudocode.

| Token / compare | Direct branch and downstream code | Bounded conclusion |
|---|---|---|
| `reloadClientTemplate` / `0x010134CF` | consumes a path argument; builds an IFF context; calls VFS-backed `iff_open_path_allow_missing_VA_00A94210`; on an opened path, calls `0x00B28720` → `0x00B290A0` | client object-template load/reload diagnostic, with explicit failure/status path. `0x00B290A0` reads the IFF tag, selects a registered factory and invokes virtual template operations. Existing instantiated objects are **not** shown to update in this trace. |
| `flushGraphicsResources` / `0x010145D2` | parses leading `t/T/y/Y/1` as true, other text as false; calls `0x00755520(bool)` | forwards a flush request through a nullable graphics callback at global `DAT_01922E6C+0x20`; exact resource types and synchronization are still unknown. |
| `reloadTextures` / `0x010146F4` | calls `0x00764B70` | walks the current texture-object collection and, for each retained texture name, calls the already mapped DDS/VFS construction path `0x00769170`. Whether live GPU handles are replaced or cache entries merely warmed/recreated requires a deeper owner trace. |

`0x00B290A0` does not itself choose an arbitrary fallback template on an unknown IFF root tag; it looks up a registered parser and returns zero if none exists. Its `0x00B2E860` helper sets a diagnostic `ObjectTemplate::Iff - %s` context and calls template virtual operations. This confirms that `/reloadClientTemplate` traverses the original IFF factory architecture, but it is not a substitute for a full template-cache invalidation contract.

**Confirmed:** literal command branches, argument conversion, direct helper calls, IFF/VFS and DDS-reader convergence. **Strongly inferred:** diagnostic/developer intent from tokens and call shape. **Unknown:** GPU callback target, live texture replacement and exact effect of reloading a template already used by existing objects. No new protocol mapping or whole-client percentage is claimed.
