# Workspace organization manifest

Organized: 2026-09-22

## Result

- Preexisting files: 9,823
- Preexisting bytes: 3,452,304,997
- Relocations applied: 1,044
- Deletions: 0
- Added audit artifact: one pre-organization CSV manifest containing original relative paths, sizes, and SHA-256 hashes

## Classification

- Six living control documents remain at the root.
- Forty-one durable subject documents moved to `docs/`.
- 391 bounded checkpoints moved to `research/checkpoints/`.
- Fourteen consolidated sessions moved to `research/sessions/`.
- Historical archives moved to `research/archive/`.
- Two canonical CSV registries moved to `data/curated/`.
- 104 top-level analysis scripts moved under `tools/` by language.
- Ghidra state, toolchains, scratch work, 76 decompiler trees, 84 extracted top-level assets, and 328 generated top-level evidence files moved under `local-private/`.
- Five generated Godot/.NET cache and build-output directories moved from `poc/` to `local-private/poc-build/`; proof source, fixtures, and results remain under `poc/`.

All moves occurred within `D:\Development\Experimental-Client`. Relative Markdown links were rewritten against the relocation map. The exact pre-move inventory is retained in `local-private/manifests/` and intentionally ignored by Git.

On 2026-09-22, the complete organized workspace was moved to `C:\Users\ancie\Documents\GitHub\Stardust-Client`. The existing destination `.git` directory was preserved; no commit or push was performed. The source directory was verified empty after matching 9,834 files and 3,453,849,918 bytes at the destination.

## October 14, 2026 research-only cleanup

The inventory above describes the historical organization. The duplicate `poc/` tree and implementation-dependent tooling have now been removed after verifying their copies in SWG-Godot-Client. See the [cleanup handoff](research/sessions/RESEARCH_ONLY_CLEANUP_2026-10-14.md).
