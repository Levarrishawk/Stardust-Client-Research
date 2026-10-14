# Round 40: conditional supplied-query layer influence

Date: 2026-10-03. Question: can an owned finite per-sample combination preserve the direct general path's selector operation order, F32 stores/unspilled comparisons, gates, inversions, early exits and incoming product without claiming real filter queries or full layer traversal?

Stopping condition: an immutable bounded API, independent exact receipts, fresh SHA-gated static recheck and actual separate-assembly verification. No original process, full generator, GPU or live-system work.

## Input identity and orientation

Frozen Round 40 contains 3,127 files, manifest SHA-256 `32f715040a532c164c056e76d2eb9547f5b592edb3dc3dda2090c5afbde62bfe`, ZIP SHA-256 `c46605bde6a8c9b6e698de63d4395da2929b20d0f9615c8d3399b42d2b8f7d95`, manifest HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`. Full member/path/casefold/size/hash/containment/reparse preflight preceded the one public overlay; previous worker baseline and reviewed additions protected existing targets. All-file postflight passed. Private evidence was preserved.

Orientation read START_HERE, its latest accepted Round 39 session, status/ledger/unknown-function terminology and relevant curated rows; [static layer contract](../../docs/TERRAIN_LAYER_INFLUENCE_CONTRACT.md), its [prior checkpoint](ANALYSIS_CHECKPOINT_2026-10-03_R39_LAYER_INFLUENCE.md), existing static tools and accepted circle/rectangle/internal arithmetic APIs were inspected before implementation. Shared canonical artifacts remain frozen for lead reconciliation; no naming or coverage-metric change is claimed.

Original executable SHA-256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, checked before address use. Tools: cached Python 3.12, PE/Capstone mapping, independent Fraction/integer arithmetic and .NET SDK 8.0.300. Original instructions were read statically; only replacement assemblies and synthetic fixtures executed. Cached NuGet packages and cleared feeds supplied dependency resolution without downloads.

## Observations, interpretation and confidence

**Confirmed:** signed gates at layer +40/+44, complete counts +18/+24 and arrays +14/+20 determine loop entry independently of each object's enabled byte. The getter `00B62F20` reads +8; boundary/filter query calls use vtable +24/+14. Source selectors at object +14 use unsigned table dispatch, with values above three producing positive zero. Direct field/selector/opcode evidence is recorded in the [implementation contract](../../docs/TERRAIN_LAYER_INFLUENCE_IMPLEMENTATION_CONTRACT.md).

**Confirmed conditional graph:** local positive zero; no-enabled-boundary fallback one; source-order boundary selection against the unspilled shaped query while retaining F32 candidates; strict replacement and exact-one early exit; boundary inversion then positive gate; filter minimum selection against that same accumulator; exact-zero early exit; inversion of the combined accumulator even when the filter loop is empty; second positive gate; local times saved incoming F32 product. Ties retain old bits. Cubic order is query+query, 3-minus-that, multiply by query, multiply by query again. Identity performs no extra precision-rounding operation. There is no added clamp to [0,1].

The independent static recheck passes 58 inherited coordinate anchors, 60 existing layer anchors, 41 additional exact graph anchors and two enabled-getter instructions; both four-entry selector tables, three constants and twelve status cases pass. Special filter-type-five setup at `00B64083..00B6408D` is observed but no real query semantics are assigned.

**Unknown:** actual active query/caller/product arithmetic profiles, floating-point exceptions/control modes, real filter semantics, transformed boundary runtime instances, cache activation and full recursive/height-mutating traversal. Ancestor/cache propagation remains a prior strongly inferred interpretation. Synthetic receipts do not prove original generated influence or heights.

## Implementation consequence and API

New [query carrier](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainLayerInfluenceQuery.cs) factories `FromRawBits`, `FromCircle` and `FromRectangle` retain source/query precision and the unspilled internal Number. F32 projections are inspection only. Geometric source receipts are immutable retained references; no helper was made public. The raw factory explicitly marks supplied/synthetic query data.

New [sample/combine API](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainLayerInfluence.cs) snapshots ordered entries with distinct gates, counts and enabled predicates. `TerrainLayerInfluenceSample.Create` takes incoming bits, boundary/filter spans, signed gates, inversion flags, separate caller/product precisions and a complete-query cap. `Evaluate` and `EvaluateBatch` publish immutable receipts for local/product bits, visits, replacement/early-stop decisions, fallback and separate local/product eligibility. Disabled visits represent entries checked rather than queries executed.

Modern support requires finite input/projection/visited stores and finite product; a visited negative square root rejects. Skipped queries are not shaped. Enabled entries require carriers even when unvisited. Sample and batch complete-work budgets precede typed snapshots/results and combination. Defaults are 256 samples, 256 queries per sample and 65,536 total queries; hard ceilings are 65,536/4,096/1,048,576. Complete disabled, unvisited and aliased references are charged. Failed calls publish no partial result and retain prior results.

The receipt always requests a product; original conditional cache/affector activation could avoid that instruction. This finite-output requirement is modern policy, not malformed-input legacy equivalence. `AffectorEligible` means the local gates permit work; actual enabled affectors and mutation are outside the API. Supplying precomputed filters does not prove that precomputing a whole height-mutating layer is safe.

## Reproduction and validation

Run the [isolated replay](../../../SWG-Godot-Client/poc/round40-tests/Rendering/Invoke-LayerInfluence.ps1) with explicit installed Python, pinned executable, cached static packages, cached NuGet packages, installed dotnet and a fresh ignored private output root:

```powershell
& .\poc\round40-tests\Rendering\Invoke-LayerInfluence.ps1 `
  -Python $pythonPath -Exe $exePath -StaticPackages $staticPackages `
  -CachedPackages $cachedPackages -Dotnet $dotnetPath -RunRoot $privateRunRoot
```

The private handoff records exact local argument paths, file hashes and receipts. Runner builds use isolated artifacts, APPDATA/CLI_HOME, cleared sources, locked restore, disabled shared compilation and no app host. The final successful replay is named `final-replay-2` under the private Round 40 evidence root.

Results: **6,354 cases; 5,674 accepted, 680 rejected; 53 ownership/budget checks**. All exact native receipts match the independent [Fraction/integer oracle](../../../SWG-Godot-Client/poc/round40-tests/Rendering/verify_layer.py). It covers all three precisions independently across query/cache/caller/product phases, all selectors, signed zeros, subnormals, overshoot, finite overflow, exact source-order exits, gates/count/enabled states, inversion and zero/negative incoming products. There are **216 premature-query-projection witnesses** and **272 spilled-comparison witnesses**. Circle/rectangle fixtures are synthetic inputs decoded through frozen public APIs, not new original corpus samples.

The runner references the actual separate Core assembly and executes the new public [scenario](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainLayerInfluenceScenarios.cs). Both this build and actual Core/SmokeTests build pass with zero warnings/errors. The latter verifies compilation without modifying shared scenario registration. Wrong-identity and truncated executable tests reject before decoder use or fixture/receipt creation.

The first full replay passed. A subsequent runner environment edit accidentally interpolated PowerShell variable names and produced an invalid `=1` assignment before fixture generation. Literal quoting corrected it; a fresh-output final replay passed. This failed attempt and its output directory were preserved privately. No original execution-based verification is claimed.

Workspace organization validation, focused links/whitespace/Python/XML/PowerShell checks, unchanged frozen hashes, HEAD/index and scope audit are recorded in the private final handoff. New files only: two Core APIs, one smoke scenario, one durable contract, this checkpoint and the owned Round 40 Rendering runner directory. No staging, commit, push or shared-document changes.

## Exact next probe

Trace one original filter query, including its source inputs and any earlier affector-mutated state, before introducing a full layer evaluator. Preserve phase profiles and source order; do not replace the retained query with its inspection projection or treat product positivity as local affector eligibility.
