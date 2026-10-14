# Round37 complete ordered CREO6 composition

Date: 2026-10-03. Worker: Networking, Low. Bounded question: can the eighteen already specified CREO6 fields be consumed/applied in arbitrary ordered interleaving through frozen public kernels, with exact aggregate message/model/event/work accounting and atomic immutable failure? Stop condition: independent complete raw models/counters/ordered events/work/intermediate peaks agree with native separate-assembly and standalone runners; bounded malformed/ownership/cap checks and scope gates pass.

## Input identity and scope

Frozen Round37 input has2981 public files. Baseline manifest SHA256 is `de3bf3446f9233c613a848c251494e06ff1b67b6a1614b65e6ee24e5964f5f5f`; archive SHA256 is `9ba86cda0979b203eea36ff52dcdfaccfb9ea637982e94f9ced53bd7cea741bf`. Source HEAD is `5493338d9cd4255cf58f25de6c4cd15f6507b818`; worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5` with empty index. Overlay preflight checked all members, hashes/sizes, canonical/case-fold paths, containment and reparse ancestors before writing; post-write hashes matched. No second overlay was performed on resume.

The original executable was not newly inspected or executed in this round. No new VA inference is made: accepted [baseline/scalar](../../docs/CREATURE_VIEW6_CODEC_CONTRACT.md), [HAM](../../docs/CREATURE_HAM_VIEW6_CONTRACT.md), [wearable](../../docs/CREATURE_WEARABLES_VIEW6_CONTRACT.md) and [defender](../../docs/CREATURE_DEFENDERS_VIEW6_CONTRACT.md) evidence supplies the established semantics. Those contracts pin executable SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Prior static identity/kernel checks are not claimed as freshly executed here.

Nine new public paths are scoped: the FullCodec/FullState, one FullScenarios class, [durable contract](../../docs/CREATURE_VIEW6_FULL_CONTRACT.md), this checkpoint, and four files in poc/round37-tests/Networking (peer.py, Program.cs, Networking.csproj, Networking.Standalone.csproj). Existing public files remain frozen. Registry/status/ledger changes belong to lead integration under this round's scope; research/readiness/function/protocol denominators are unchanged.

Tools: bundled Python3, .NET8.0.300, cached pinned BCnEncoder.Net2.3.0 with package sources cleared, standard workspace/scope validators. No downloads, original assets, copied decompiler output, server contact, captures, store/world mutation or original callback execution.

## Observations, interpretation and consequences

Observation: [FullCodec](../../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView6FullCodec.cs) independently scans an ordered four-way update union covering indices0..17, preserves raw envelope metadata/string spelling/U64 identities, and measures whole-plan field/command/element/string/payload/decoded/work totals. The independent Python parser agrees on exact bytes/plans and complete adjusted-prefix acceptance. Interpretation: all accepted field grammars compose without sorting or reducing repeated fields. Confidence: Confirmed for deterministic synthetic fixtures. Consequence: applications can retain the complete ordered boundary rather than split messages into lossy per-family batches.

Observation: [FullState](../../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView6FullState.cs) delegates applied commands to frozen public kernels with private zero-skip one-command projections, while computing raw field skip/cursor externally against actual local counters. Its candidate replaces only the corresponding immutable vector; all scalar fields, raw baseline metadata and untouched vector counters/contents survive. Full receipt retains only Current and owned descriptions. Interpretation: ordered counter/equality/default/assignment/clear rules are preserved with one outer checked revision and atomic whole-plan publication. Confidence: Confirmed for raw-model/counter/event fixture equality; legacy side effects retain the linked contracts' limitations. Consequence: no private cached pointer/owner/resource representation is required by this neutral kernel.

Observation: full descriptions cost48 logical bytes each; defender item/general multiplicity is preserved, HAM/wearable use one mutation description, scalar assignments use one description. Before every applied command, full candidate sizes/counts/strings and event multiplicity are checked with all other partitions/prior events reserved. Frozen child remove/clear checks independently reserve old narrow state plus24/32-byte construction journals. Python peaks include both the child construction reservation and candidate48-byte publication. Interpretation: aggregate caps cover actual intermediate public-adapter behavior and cannot be evaded by later shrink. Confidence: Confirmed exact/one-less fixtures, including long wearable removal/clear and all vector families with prior events. Consequence: do not silently reuse the older24-byte composite journal or widen child output budgets.

Observation: work includes full validation, global message consumption, vector occurrence seeds, applied frozen one-command work and scalar seed/apply scans. This intentionally charges public-adapter copying/consumption overhead repeatedly. Interpretation: explicit reproducible modern logical units, not a minimal original CPU/heap cost estimate. Confidence: Confirmed exact independent sums. Consequence: full limits must use this new policy; old scalar/HAM limits do not silently change.

## Validation and corrected attempts

Both new builds passed with zero warnings/errors. Each runner passed **97 exact cases,148202 negative checks,3335 budget checks and967 ownership checks**. Python independently regenerated expected data and compared all raw full models/counters, plans, events, work and reseeds to both exports. The cases include all18 fields, accepted vector edge corpora with scalar interleaving, cross-skip/overshoot/wrap, equal/default growth, U64 high/low-word distinctions, wearable metadata equality ignores, all commands, repeated fields, long strings, within/across-field growth/shrink, separate child construction/candidate peaks and every remove/clear family with retained partitions/prior events. Tests cover wire/ordered-plan/nested command/value/customization aliases, retained old states, concurrent branches, foreign IDs, checked revision/reseed overflow, policy failure rollback and immutable earlier results.

The separate-assembly runner ProjectReferences the actual Stardust.Core project and invokes its public API. The standalone runner source-links the frozen Core plus the new component, with separate private intermediate/output paths; neither relies on internal frozen state helpers. Shared64-bit Core smoke build/run and accepted34 scalar/HAM,35 wearable and36 defender regressions passed. Workspace link/registry and nine-addition/frozen-hash checks are recorded in the final private handoff receipt; no generated evidence is promoted as canonical data.

Corrected attempts: the first exact-cap run exposed use of pre-mutation state plus new full48-byte events. Candidate remove/clear prediction now subtracts actual removed contents, while the frozen child pre-reservation remains enforced and independently accounted. A temporary experimental child-output widening was removed before the successful runs. An initial default4096-element scenario policy rejected the intentional index65535 fixture; the test seed now explicitly allows the accepted65536 per-vector ceiling. An unhandled failing runner temporarily held its DLL, causing MSB3027/MSB3021 copy failure; its owned sessions were interrupted and the runner now catches failures, reports them and exits2. The subsequent builds/runs passed. These failures are not counted as successful validation.

## Reproducible commands

From the worker root, set PYTHONUTF8=1 and PYTHONDONTWRITEBYTECODE=1. Use the existing ignored cached NuGet packages and a private NuGet.Config whose packageSources contains only clear; set a private APPDATA for this worker. No package feeds/downloads are required.

```powershell
python poc/round37-tests/Networking/peer.py --generate local-private/round37/peer.json
dotnet restore poc/round37-tests/Networking/Networking.csproj --configfile local-private/round37/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round37-tests/Networking/Networking.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round37-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round37/peer.json local-private/round37/export.json
python poc/round37-tests/Networking/peer.py --peer local-private/round37/peer.json --verify local-private/round37/export.json
```

For the standalone project, use the same restore/build flags plus an absolute private BaseIntermediateOutputPath and OutputPath (worker local-private/round37/standalone-obj/ and standalone-bin/). Run Networking.Standalone.dll against peer.json and standalone-export.json, then verify that export with the same Python command. The additional project contains no ProjectReference. Regenerate/replay accepted34/35/36 peers with their existing commands and private exports. Build/run Stardust.Core.SmokeTests with the same no-restore/compiler flags.

```powershell
python tools/python/validate_organized_workspace.py .
python tools/python/parallel_round_workspace.py review --root . --worker . --baseline C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round37 --allowed local-private/round37/allowed.json
git -c core.safecrlf=false diff --check
git status --short
```

The allowed list contains only the nine new paths named above. Independently hash every frozen manifest member and assert2981 unchanged; assert every changed row has before:null, no other public additions, unchanged worker HEAD and empty index. Check new files for UTF8 without BOM, LF-only newlines, final newline/no trailing whitespace; AST/XML parse the new oracle/projects. Curated Markdown contains no links to private output.

## Remaining uncertainty and exact next probe

No current-incarnation store/world publication, historical full-event/resource retention, owner/gameplay callbacks or live interoperability is established. Full schema/kernel compatibility is a bounded implementation result, not a new combined completion percentage. Next probe: design a new full-union prepared publication seam using these owned candidates, with stale-incarnation rejection, exact48-byte event accounting, historical event ownership, deterministic commit/failure cleanup and an independent store/world oracle. Lead accepts/reconciles this milestone before that work begins.
