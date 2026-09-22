# Research workspace hygiene

Updated: 2026-09-22

## Canonical location

The canonical research workspace is `C:\Users\ancie\Documents\GitHub\Stardust-Client`. It is a separate local Git repository from the Stardust-3 server repository. Moving it into a Git working directory does not authorize a commit, remote creation, or publication.

## Publication boundary

The curated research record consists of the root control documents plus `docs/`, `research/`, `data/curated/`, `tools/`, and `poc/`. The `local-private/` directory contains heavyweight, generated, licensed, proprietary, or redistribution-sensitive material and must never be committed.

The root `.gitignore` enforces that boundary. Before any first commit or push, follow `PUBLICATION_GUIDE.md` and inspect the staged file list—not merely the working tree.

## Primary indexes

- `START_HERE.md`: review order and workspace map
- `ANALYSIS_STATUS.md`: current coverage and conclusions
- `COMPREHENSIVE_ANALYSIS_LEDGER.md`: chronological whole-client ledger
- `docs/PACKET_HANDLER_MAP.md`: network message-to-handler evidence
- `docs/REPLICATION_SCHEMA_MAP.md`: baselines and deltas
- `UNKNOWN_FUNCTIONS.md`: stable function observations
- `data/curated/provisional_names.csv`: normalized provisional-name registry
- `data/curated/server_packet_gap_matrix.csv`: authoritative packet inventory

## Evidence retention

Do not delete raw evidence merely because it is inconvenient to browse. Generated evidence may be pruned only when its producing script and inputs are retained, no primary/checkpoint document relies on it, regeneration is deterministic, and the removal is recorded in a session report.

The pre-reorganization file/hash manifest is stored under `local-private/manifests/`. It records the original relative path, size, and SHA-256 of every preexisting file.

## Stable identifiers and stale counts

- Use `subsystem_role_VA_XXXXXXXX` for provisional functions.
- Never silently repurpose a stable name.
- Keep current counts near the top of living documents.
- Preserve dated historical counts as historical evidence.
- Keep packet coverage, whole-client coverage, implementation readiness, and raw function-name coverage as separate denominators.

## End-of-session checklist

1. Finish the active bounded analysis.
2. Add or update a dated checkpoint with evidence and confidence.
3. Update stable names and normalize the CSV.
4. Recount the packet matrix mechanically.
5. Update the status and comprehensive ledger without rewriting history.
6. Run the workspace/link validation scripts.
7. Confirm no material meant for `local-private/` is staged.
8. Record the exact resume point.
