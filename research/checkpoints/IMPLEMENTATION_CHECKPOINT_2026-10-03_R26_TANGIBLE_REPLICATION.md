# R26 Runtime: standalone tangible store

Date: 2026-10-03. Question: can frozen common tangible models maintain incarnation-safe, ordered, bounded state without changing existing creature/world code? Stopping condition: isolated store, native negative cases, independent receipt checks, durable contract, and exact scope audit.

## Inputs and method

The supplied R26 baseline contains 2,596 files. Manifest SHA-256: `2f5f65bd69ac86003ab9ddb14ecb01e411d4702a069934147a2d57d2607eecc9`; archive: `f847efa6c85734f9078c53eeb55dda7a33bafac6aee89bfed0178121b12010ad`. Preflight checked exact archive names, lengths, hashes, containment and symlink/junction constraints; postflight checked every baseline file. HEAD remains `65de8c88141071744dc75594be126b3bab3696a5` and index bytes are preserved.

Read-only configuration SHA-256 is `22158e5bd9b7ebe5a068f10c4d15fcfaa7ab56c9130af875a1ba47a9b41da524`. Tools were the existing cached .NET build/runtime, isolated no-source NuGet configuration, bundled Python, and workspace validator. No executable addresses were used, so no original executable identity or behavior is asserted. No original execution, server contact, asset loading, corpus sweep, publication, or main-checkout writes occurred.

## Result and confidence

Confirmed: the new [store](../../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneTangibleReplicationStore.cs) implements the [durable contract](../../docs/SCENE_TANGIBLE_REPLICATION_CONTRACT.md), with frozen model inputs, historical baseline retention, ordered common changes, component single-pass observation, alias-aware logical charging, checked precommit publication, exact snapshots, registry incarnation checks, and deterministic cleanup. Seed computes its checked retained total before any store assignment. Rejected writes preserve unrelated stale capacity; explicit reads retire stale entries.

Confirmed by native execution: 93 checks / 28 cases. Confirmed by independent Python arithmetic: forward/reverse order, raw/name units, component counter edge behavior, aliases and equal separate storage, and all six exported receipt records. Synthetic seed baseline is 73 decoded bytes; normalized seed charge is 97; forward/reverse current charges are 109/110; alias/equal-storage charges are 105/108. The independently authored fixture preserves negative component keys, duplicate historical keys and unpaired UTF-16 units.

Inspected: same-thread busy guard. There is no reachable external callback during public operations, so reentry rejection is not dynamically claimed. Unknown/deferred: TANO wire decoding, family/recipe gates, world coordinator and router integration, raw source and presentation journal joins, and a combined world/store transaction.

## Verification and reproduction

The own [runner](../../../SWG-Godot-Client/poc/round26-tests/Runtime/Runtime.csproj) builds with zero warnings and errors using cached dependencies, `NuGetAudit=false`, `UseSharedCompilation=false`, and `UseAppHost=false`. Run its `tangible` mode with root/config/catalog/bank, independent peer JSON and receipt destination; run `kernels` without other arguments. Other modes retain the preceding accepted scenario suites. Private inputs and receipts remain ignored; reproducibility requires the accepted synthetic peer vectors and cached toolchain. The fixture is a common-model projection from CREO, not a recovered TANO byte fixture.

Run `python poc/round26-tests/Runtime/verify.py <receipt-directory>` after native runs. The directory contains `tangible-receipt.json`, preceding mode receipts and native logs. The independent verifier passes R26 and inherited R25 through R21/drain audits. Native regression totals: named 489/93, extended 407/81, full 338/67, scalars 196/40, state 136/28, consumer 160, pump 171, drain 1,165/37; four component/wounds/mixed kernel suites pass.

Scope is four explicitly allowed new artifacts plus five own runner files; no baseline file changes. Workspace validation and git diff --check pass. Exact baseline/new-file audit passes: all 2,596 baseline files unchanged, exactly nine authorized additions, HEAD and index preserved, and private evidence/build products ignored. Relative links in both new documents resolve. Generated evidence, build output and receipts remain ignored.

## Compatibility consequence and next probe

The standalone model boundary is ready for a bounded integration experiment; protocol coverage is unchanged. No combined research/function/implementation denominator is claimed. Next probe: introduce internal owner-held prepare/commit tokens and join them to the lead's world transaction, proving failures cannot publish either side and defining raw journal ownership. Keep TANO codec evidence separate from projected model tests.
