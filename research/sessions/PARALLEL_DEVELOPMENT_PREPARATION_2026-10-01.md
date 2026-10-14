# Parallel worker preparation — October 1, 2026

The user adopted [the operating rules](../../docs/PARALLEL_DEVELOPMENT_RULES.md) and authorized preparation only. No development round is dispatched.

Primary HEAD remains05e3c2b6394e8e98741ed6f694c39876449ffd37 on main; staging remains empty. A private ZIP/manifest/patch preserves1707 baseline paths, including the uncommitted implementation. Manifest SHA256 d6054a286fedda209bcc67669728437786cc9b7eb7c90287d0f04d9fa0c7e685. Four isolated worktrees contain every baseline file; checkout newline normalization is distinguished from implementation changes. Ignored private toolchains/evidence are intentionally absent from worker checkouts and must be supplied as explicit read-only paths or worker-local dependencies in assignments.

## Worker requests

All requests specify Low reasoning and preparation-only orientation, followed by stopping. Creation is asynchronous; client request IDs below are not usable task IDs. Retrieve actual IDs/status with the app thread list before sending assignments or waiting on tasks. Do not duplicate these requests.

| Specialty | Creation request |
|---|---|
| Assets | client-new-thread:2710a3c0-4fff-4544-afe8-ef9044fe138e |
| Rendering | client-new-thread:c0727124-44b0-4014-997d-e0f007455d53 |
| Runtime | client-new-thread:2b04e0e1-20ac-4072-b152-3f0ed5408287 |
| Networking | client-new-thread:91c9a22c-9352-49c5-ac40-f4b11b054d0d |

Managed worktrees were created detached at the primary HEAD; lead assigns codex/ worker branches before the first implementation round. Do not assume a specialty-to-worktree mapping until each task reports its actual checkout. Four observed checkout suffixes are61c9,a94e,bdb2,ec53. No commits or changes to the primary HEAD occurred.

Validation: main prototype host build passes with0 warnings/errors; organized-workspace validator and diff check pass; no staged paths. Native/GPU acceptance from the previous completed session remains historical verification rather than new preparation execution.

## Resume

Resolve actual worker task IDs and collect readiness reports. Confirm full working-tree baseline in each checkout using the private manifest; account for Git newline normalization. Prepare four independent round1 assignments with explicit file ownership and stable interfaces. Shared entry points, canonical registries and ledgers remain lead-owned. Do not dispatch until the user authorizes the round. Initial rendering candidate is the original mouth VSH/source color-output boundary; other candidates must be chosen from current evidence and existing implementation gaps without repeating closed work.
