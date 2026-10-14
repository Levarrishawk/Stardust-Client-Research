# Round 41: supplied layer plus ordered AHCN height execution

Date: 2026-10-03. Question: can one owned supplied layer sample drive ordered constant-height rules without losing local versus incoming-product gates, per-rule F32 stores, query precision/provenance or atomic bounded ownership?

Stopping condition: one bounded immutable executor, one narrowly authorized polygon carrier factory, independent exact receipts/factory regression, separate assemblies and a reviewable handoff. Full filters, recursive/cache/ADTA traversal, original generation and GPU work remain outside scope.

## Identity and orientation

Authoritative input was the frozen Round 41 baseline, not the unused preliminary baseline. It contains 3,174 public files, manifest SHA-256 `74cc0552f0715e52b782c9aec68dc003584fabbdd8987f55cc6684c971238ea8`, ZIP SHA-256 `c788a27f07bb5d5b5c4613c6b54a015ecf673288e3518fadff2cad79dd94c746`, main HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`.

Both full identities, member/path/casefold/size/hash/containment and lexical root/ancestor/target symlink/junction/reparse conditions were checked before the single overlay. Overwrites were permitted only for accepted prior baseline, reviewed Round 40 hashes or incoming hashes. All 3,174 files passed postflight; ignored private evidence was preserved.

Read START_HERE, accepted Round 40, status/ledger/unknown-function vocabulary, relevant curated rows, the accepted [layer contract](../../docs/TERRAIN_LAYER_INFLUENCE_IMPLEMENTATION_CONTRACT.md), [AHCN contract](../../docs/TERRAIN_HEIGHT_CONSTANT_OPERATION_CONTRACT.md), [polygon checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-03_R40_POLYGON_INFLUENCE.md), APIs and existing static/Fraction tools before substantive implementation. Shared files remain frozen except the expressly authorized query factory extension.

Original SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` was verified before VA use. Tools were cached Python 3.12/PE/Capstone and .NET SDK 8.0.300, with offline cached NuGet and private output/environment. Original input was static/read-only; only replacement code and labeled synthetic fixtures executed.

## Evidence and confidence

**Confirmed direct prerequisites:** general layer source-order stores/comparisons/gates/product from `00B63D20`; the caller's enabled check and F32 influence argument at `00B64166..00B6418C`; AHCN process `00B76540` and its ordered arithmetic/one final F32 store. The implementation delegates these already specified graphs to the frozen APIs, rather than reimplementing them.

Fresh static replay checks the 58 coordinate prerequisite anchors, 60 layer anchors plus 41 extended anchors/two getter instructions, two tables/three constants/twelve status cases; 21 AHCN process/five caller anchors plus exact table/constants/error-string evidence; and 93 polygon/71 raw-input anchors with87 status cases. These remain separate evidence counts, not a new combined coverage denominator.

**Confirmed modern behavior:** an immutable sample snapshots ordered rule references, uses one retained supplied layer sample and separately selected affector precision, reserves complete work and publishes atomically. Factory `FromPolygon` retains the frozen polygon Number and source precision directly. Existing factories and source values0..2 preserve behavior; polygon appends value3.

**Unknown:** original active arithmetic controls, full filter/state dependencies, cache/recursive activation and generated original terrain parity. No raw ADTA field or recursive runtime semantics are invented. Supplying a fixed filter receipt does not prove whole-layer precomputation before height mutations.

## Compatibility consequence and API

The [durable executor contract](../../docs/TERRAIN_SUPPLIED_LAYER_HEIGHT_CONTRACT.md) records complete policy. New [TerrainSuppliedLayerHeight](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainSuppliedLayerHeight.cs) accepts `TerrainSuppliedLayerHeightSample.Create(layer, initialHeightBits, rules, affectorPrecision, maximumRules)` and exposes `Evaluate`/`EvaluateBatch`. Receipts expose `Sample`, `Influence`, ordered `Visits`, `FinalHeightBits` and `FinalHeightIsFinite`. Every visit includes index and the full frozen AHCN result.

Local ineligibility skips the entire rule traversal, even if the stored product is positive. Local-positive samples visit ordered supplied rules through the frozen wrapper, where disabled or nonpositive-product rules leave raw heights unchanged. Each applied F32 result feeds the next rule. Positive-product operation4 returns positive zero; unknown operation words use the frozen default blend. Incoming zero/negative influence never replaces the earlier local gates.

Unused nonfinite raw initial/constant words are retained according to the frozen kernel's skip/error-zero eligibility. Final finite inspection is separate from raw acceptance. There is no blanket finite-height clamp, invented filter callback or external mutation.

The sole existing-file edit is [TerrainLayerInfluenceQuery](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainLayerInfluenceQuery.cs): append polygon source/provenance, an optional private-constructor reference and `FromPolygon(TerrainPolygonQuery)` retaining Number. All other 3,173 baseline public files remain immutable.

Budget defaults:256 samples,256 queries/sample,65,536 total queries,256 rules/sample,65,536 total rules and327,680 reserved operations. Hard ceilings:65,536/4,096/1,048,576/4,096/1,048,576/5,242,880. Complete rule graph reservations are3 for operations1/2,1 for operation4 and5 for every other operation; they count arithmetic plus final store even if skipped. This is a conservative work reservation, not actual executed CPU/BigInteger instruction count. Aggregate checks precede batch execution allocations. Failed late rule or layer work preserves all earlier results and caller inputs.

## Reproduction and results

The [replay script](../../../SWG-Godot-Client/poc/round41-tests/Rendering/Invoke-SuppliedHeight.ps1) takes explicit Python, executable, cached static packages, cached NuGet, dotnet and a fresh private output root:

```powershell
& .\poc\round41-tests\Rendering\Invoke-SuppliedHeight.ps1 `
  -Python $pythonPath -Exe $exePath -StaticPackages $staticPackages `
  -CachedPackages $cachedPackages -Dotnet $dotnetPath -RunRoot $privateRunRoot
```

Exact local paths, commands, final file hashes and receipts are recorded in the private Rendering handoff. Successful final evidence is named `final-replay` under the ignored Round41 evidence root. Each replay requires a fresh output root. Builds use isolated artifacts, APPDATA/CLI_HOME, cleared feeds, locked restore and no shared compiler/app host. Python uses UTF8 and disables bytecode.

The independent [Fraction/integer composition oracle](../../../SWG-Godot-Client/poc/round41-tests/Rendering/verify_height.py) checks **13,531 executor cases:12,268 accepted and1,263 rejected**, with **654 premature-projection witnesses**. Mixed raw/circle/rectangle/polygon inputs cover all243 combinations of cache/query/caller/product/affector phase profiles, all selectors and operations, source order, signed zeros/subnormals, local/incoming gates, unused nonfinite heights, operation4 recovery, overflow and exact budgets. It reuses frozen independent query/layer/AHCN references; polygon extraction returns the Fraction before its inspection section, independently of production.

The public [native scenario](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainSuppliedLayerHeightScenarios.cs) passes **52 ownership/budget/late-failure checks**. All **6,354 frozen factory cases and53 prior ownership checks** pass unchanged through the actual public frozen scenario, proving old factory behavior while checking the new carrier separately. Both actual Core-reference runner and actual Core/SmokeTests builds pass with zero warnings/errors. Shared scenario registration was not edited. Wrong/truncated executable tests reject before decode or fixture generation. Workspace/link/source/scope checks are recorded in the final private handoff.

Failed attempt: the first replay script omitted the new regression-fixture argument in its native verification command. Fixture generation, separate build and native execution had succeeded. The missing argument was corrected; saved native receipts verified, followed by the complete fresh-output final replay. No arithmetic/build repair or original execution verification is claimed.

## Scope and exact next action

Four new public scope files plus six owned runner files and one authorized factory modification comprise this handoff. No other baseline changes, unrelated additions, deletions, staging, commits, pushes, downloads or GPU/live actions occur. Lead owns living-ledger reconciliation and any downstream scene join.

Next probe: trace one original filter query, including source inputs and dependence on earlier height mutations, before adding a full layer evaluator. Preserve phase provenance, local gates and per-rule F32 stores; do not infer recursive/cache activation from this supplied composition.
