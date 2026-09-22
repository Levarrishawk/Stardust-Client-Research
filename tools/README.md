# Analysis tools

- `python/`: independently authored extraction, inventory, codec, and verification helpers.
- `ghidra/`: Ghidra Java scripts used for repeatable exports and targeted decompilation.
- `powershell/`: workspace normalization helpers.

Run `python/validate_organized_workspace.py [workspace-root]` after moving documentation or updating either canonical CSV. It checks relative Markdown links, links that cross into `local-private/`, provisional-address uniqueness, packet-row counts, and curated-tree size.

Scripts may rely on local paths, Ghidra APIs, or packages stored under `local-private/toolchain/`. Review dependencies, licensing, and machine-specific assumptions before publication. Generated outputs belong in `local-private/generated-evidence/` unless they are promoted deliberately to `data/curated/`.
