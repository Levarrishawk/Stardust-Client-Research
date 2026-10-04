# Round 42: ordered supplied flat layer sequence

Date: 2026-10-03. Question: can immutable supplied layer descriptors execute in explicit flat source order, threading only the previous raw F32 height into the next frozen executor while reserving complete work and receipt duplication?

Stopping condition: bounded engine-neutral sequence, owned immutable input/results, independent exact receipts, separate Core/SmokeTests and reviewable scope. Original recursion, real height-dependent filters, cache/ancestor activation, ADTA interpretation, materials/normals and GPU work remain excluded.

## Input identity and orientation

Authoritative frozen Round42 contains3,222 files, manifest SHA-256 `c4dd7ca90525964037c264ee757e3678defd252425e6243eec33473bdf1ddbe7`, ZIP SHA-256 `3be9a0a1071ab564808f035510573c216478b91e32bf55cf82641d5727d056a9`, main HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`. Actual worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`.

Both full identities and all archive members/canonical paths/casefold/size/hash/containment/root+ancestor+target reparse conditions were checked before the single overlay. Current destination hashes were admitted only against accepted41 baseline, incoming hashes or main's reviewed41 Rendering integration after-hashes. Every integration row was reconciled against the prior reviewed worker handoff before writing. Full3,222-file postflight passed; private/unrelated bytes were preserved.

Read START_HERE, accepted41, current status/ledger/UNKNOWN terminology, relevant curated discovery and prior layer/AHCN/polygon/executor contracts, checkpoints and tools. All existing public files are frozen. The prior [height executor contract](../../docs/TERRAIN_SUPPLIED_LAYER_HEIGHT_CONTRACT.md) is the execution boundary, rather than an inferred full-generator model.

Original executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` was rechecked before VA use. Cached Python3.12/PE/Capstone and .NET SDK8.0.300 supplied static/native verification. NuGet feeds were cleared and existing caches used in a private environment. Original execution, live contact and GPU work did not occur.

## Findings, interpretation and confidence

**Confirmed prerequisites:** accepted layer candidate/unspilled comparisons, gates/product, AHCN ordered arithmetic/F32 stores and immutable query/rule inputs. Fresh static prerequisites recheck58 coordinate anchors;60 layer anchors plus41 extended/two getter anchors/tables/constants/status cases;21 AHCN process/five caller anchors;93 polygon/71 raw-input anchors and87 polygon status cases. These are separate prerequisite evidence counts, not additional function/research coverage.

**Modern supplied contract:** explicit source-order flat traversal threads each stage's `FinalHeightBits` into the next child initial height. It preserves that descriptor's own fixed incoming weight, supplied queries, gates and arithmetic policies. No product/ancestor influence is inherited. The plan order is chosen by its caller, not recovered original recursion.

A descriptor validates/snapshots rules through the frozen height factory and retains its immutable layer and precision. Its private storage template uses initial zero, which traversal always replaces. Plans own descriptor reference snapshots and immutable cached complete counts/maxima. All child graphs are delegated to the frozen executor. No new arithmetic or filter graph is copied into production.

Complete preflight reserves all supplied layers/queries/rules/frozen operation charges, including aliases and skipped work, before stage/result/transfer/child allocations. A separate rule-reference cap reserves2*R+M for descriptor rule arrays, completed child rule arrays and the largest current transfer. The frozen span API necessitates this transfer and child snapshot. This is logical reachable-reference accounting, not a heap-byte/GC guarantee; visit value records, query records, stage/plan/batch reference arrays have independently documented count bounds.

**Unknown:** active original arithmetic controls, real filter validity after height mutation, recursive/cache/ancestor behavior and generated original terrain. Fixed supplied queries are not asserted to be safe precomputed real filters. No semantic identifier or canonical registry/count is changed.

## API and compatibility consequence

The [durable sequence contract](../../docs/TERRAIN_SUPPLIED_LAYER_SEQUENCE_CONTRACT.md) specifies [TerrainSuppliedLayerSequence](../../poc/Stardust.Core/Geometry/TerrainSuppliedLayerSequence.cs):

```csharp
TerrainSuppliedLayerSequenceLayer.Create(layer, rules, affectorPrecision, maximumRules = 256)
TerrainSuppliedLayerSequencePlan.Create(initialHeightBits, layers, maximumLayers = 64)
TerrainSuppliedLayerSequence.Evaluate(plan, limits = null)
TerrainSuppliedLayerSequence.EvaluateBatch(plans, limits = null)
```

Receipts retain `Plan`, ordered `Stages`, `FinalHeightBits` and `FinalHeightIsFinite`; each stage has `Index` and a complete frozen height `Receipt`. Batch results expose aggregate `Charge` and owned-copy height inspection. Raw signed zeros/nonfinite unused heights, local/product eligibility, disabled rules, operation4 recovery, default operations and per-rule stores retain the frozen semantics. A later failure publishes neither a partial plan nor partial batch and does not change earlier results.

Defaults/hard ceilings are recorded in the contract:256/65,536 samples;64/4,096 layers per sample;16,384/65,536 total layers;256/4,096 queries and rules per layer;65,536/1,048,576 aggregate queries and rules;327,680/5,242,880 operation reservations;131,328/2,101,248 rule-reference slots. Child limits are mapped explicitly from these admitted caps. Empty work supports zero caps.

## Reproduction and verification

The [replay script](../../poc/round42-tests/Rendering/Invoke-LayerSequence.ps1) accepts explicit installed Python, pinned executable, cached static packages, cached NuGet, dotnet and a fresh private output root:

```powershell
& .\poc\round42-tests\Rendering\Invoke-LayerSequence.ps1 `
  -Python $pythonPath -Exe $exePath -StaticPackages $staticPackages `
  -CachedPackages $cachedPackages -Dotnet $dotnetPath -RunRoot $privateRunRoot
```

Exact argument paths, hashes and receipts are in the private Rendering handoff. Successful evidence is named `replay-2` under the ignored Round42 root. Use a fresh root on replay. Builds have isolated artifacts/private APPDATA/CLI_HOME, locked offline restore and disabled shared compiler/app host; Python uses UTF8/no-bytecode.

The [independent Fraction/integer oracle](../../poc/round42-tests/Rendering/verify_sequence.py) passes **15,456 plans:14,160 accepted and1,296 rejected**, including **1,904 multi-layer plans** and **1,140 premature-projection witnesses**. It independently computes descriptor admission, child limit mapping, source-order height threading and complete reference/work budgets while reusing frozen independent numerical references. Synthetic mixed raw/circle/rectangle/polygon carriers cover five arithmetic phases, selectors/operations/gates/incoming signs, raw nonfinite recovery, empty plans, ordering permutations, late overflow and all exact/one-less caps.

The [native scenario](../../poc/Stardust.Core.SmokeTests/TerrainSuppliedLayerSequenceScenarios.cs) passes **64 ownership/budget/atomicity checks**, including immutable aliases, complete aggregate rejection before late arithmetic failure,4,096 rules,4,096 layers, read-only collections and retained results. Actual separate Core-reference runner and actual Core/SmokeTests builds pass with zero warnings/errors. No shared registration was edited. Wrong/truncated identities reject before decoder use or fixture/receipt creation. Final workspace/source/link/scope validation is retained in the private handoff.

Failed attempt: the first runner build reported CS0136 because the fixture-local `limits` name conflicted with a later scenario variable. Renaming it `fixtureLimits` corrected the build; fresh `replay-2` passed all checks. Final audit initially duplicated runner paths already included in the expanded allowlist; deduplicating its admission list corrected that check. No arithmetic repair or original execution-based verification is claimed.

## Scope and next probe

Ten additions only:one Core API, one public smoke scenario, durable contract, this checkpoint and six owned runner files. All3,222 existing public files remain unchanged; no unrelated additions/deletions, staging, commits, pushes or downloads. Lead reconciles living documents and owns downstream joins.

Exact next probe: determine whether one real filter observes height changed by preceding affectors before admitting precomputed queries in a full generator. Keep source order, raw F32 stores and phase provenance separate from unknown recursive/cache policy.

