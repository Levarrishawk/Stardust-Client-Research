# Publication guide

This workspace is prepared for a future standalone research repository. Organization does **not** authorize publication, and no remote should be created or pushed without an explicit instruction.

## Mandatory pre-publication review

1. Confirm `local-private/` is ignored and absent from `git status --short` and `git ls-files`.
2. Review `git status --short` file by file before the first commit.
3. Search curated files for absolute local paths, user names, credentials, tokens, hostnames, IP addresses, and private server configuration.
4. Review every binary and asset fixture for redistribution rights. Original game assets and `Stardust.exe` must not be committed.
5. Review decompiler-derived excerpts for necessity and licensing implications; prefer factual descriptions, addresses, schemas, and independently written pseudocode.
6. Review `tools/` for bundled third-party code, generated runtime packages, and machine-specific paths.
7. Review `poc/` for caches, imported assets, and third-party dependencies.
8. Select and add an explicit repository license only after deciding what rights apply to the original research and scripts.
9. Run link validation, provisional-name deduplication, packet-matrix recounting, and a staged-file size audit.
10. Make the first push only after the user reviews the exact staged inventory.

## Intended public surface

- Root navigation, status, and ledger documents
- Durable documentation in `docs/`
- Dated evidence reports in `research/`
- The two normalized registries in `data/curated/`
- Independently authored reproducibility scripts in `tools/`, after review
- The bounded prototype host proof under `poc/`, after review

## Always private by default

- `Stardust.exe` or any other client executable
- Original SWG/TRE/IFF assets unless redistribution rights are established
- Ghidra project databases and raw bulk decompiler output
- Downloaded toolchains, Python environments, JARs, archives, and DLLs
- Large generated corpora that reproduce proprietary asset content
- Credentials, private endpoints, logs containing account data, and machine-specific secrets
