# R4-ASSETS-01: modern projected-size skeletal detail selection

Date: October2,2026. Result: ready for lead review; stop after this milestone.

## Bounded question and baseline

Does an engine-neutral caller-supplied three-threshold policy choose detail0..3 deterministically from bounded sphere/projection inputs, retain explicit hysteresis, select finest near/inside without singular division and reject invalid samples atomically? Stop after own native/independent formula-state oracle, ownership/rejection and build/link/scope checks. No original asset/parser/render/GPU changes or later-round work.

Checkout `C:/Users/ancie/.codex/worktrees/520a/Stardust-Client`; HEAD65de8c88141071744dc75594be126b3bab3696a5 plus Round4 manifest e53b9decda458cb7ea9f335ea4eda9422ea915415f514316d7e07df2507609d7 and ZIP d7364d604436d086b9103c2a76489ce51bb27b1bbe464b07fb918f4a586a5b38. Verified all1792 relative archive paths inside checkout, source length/hash before overlay and all destination hashes after overlay; preserved ignored/unlisted paths. Read START_HERE/latest session/Round3 accepted report, repository/parallel rules, relevant lower-detail contracts and prior tools/checkpoints/status/ledger/function terminology. Existing accepted work remains the baseline.

## Result and evidence

Added immutable threshold policy, serial state selector, projection/result DTOs and reset. Full bounds, centered-sphere approximation, conservative eye-plane/near-sphere policy and strict downward/inclusive upward equality are documented in the [durable policy](../../docs/SKELETAL_DETAIL_SELECTION_POLICY.md). Production has no default thresholds; fixture thresholds240/120/60 and hysteresis10% are synthetic supplied choices.

Observation: native sweeps in both directions,24-sample jitter, repeated identical input, near/inside/just-outside/extreme projections, zero/max hysteresis and reset pass. Exact initial/lower/upper equality and just-below-lower fixtures pass. Invalid constructor/input/nonfinite/bounded/projection-underflow cases leave selection unchanged; invalid first input leaves null. Caller array mutation and public collection mutation attempts preserve retained immutable values.

Observation: independent Python projected formula and crossing-set state oracle agrees across76 ordinary supplied cases, three near/inside samples, three exact equality fixtures and three resets. Maximum projection difference0 on this host. Stateless wrong policy differs25 times. No original executable/address/asset input is needed or read for the milestone.

Interpretation/confidence: Confirmed modern implementation/fixture agreement. This is not recovered original LOD research, automatic skeleton coupling or runtime threshold evidence. Consequence: callers can select retained compatible meshes through an explicit modern policy in a later adapter; do not raise research or function/protocol scores.

## Validation

Task APPDATA=`$env:TEMP/stardust-assets-round4-appdata`; existing accepted private NuGet package cache; sources-cleared task config created by the documented commands; NuGetAudit=false and UseSharedCompilation=false/UseAppHost=false. PythonUTF8=1 with explicit UTF8 text reads/writes. Bundled Python executable used where Python is absent from PATH.

- `dotnet restore poc/round4-tests/Assets/Assets.csproj --configfile local-private/round4/NuGet.Config -p:NuGetAudit=false`: pass offline.
- `dotnet build poc/round4-tests/Assets --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false`: pass, zero warnings/errors.
- `dotnet poc/round4-tests/Assets/bin/Debug/net8.0/Assets.dll > local-private/round4/native.txt`: pass.
- `python tools/python/verify_skeletal_detail_selection.py local-private/round4/native.txt`: pass; fixed supplied formula/state/equality/wrong-policy oracle.
- Shared smoke project offline restore/build with warnings as errors: pass, zero warnings/errors; shared runner was not modified or executed.
- Organized workspace validator: 1035 Markdown files, zero broken links or links into private material; no duplicate provisional addresses.
- Two deterministic incorrect-output fixtures (detail and projected diameter) rejected by the independent oracle.
- Baseline-relative audit: all1792 existing files unchanged, exactly seven scoped additions, empty staging index; focused whitespace checks pass. Hash manifest and diff retained privately.

The first oracle expected77 ordinary rows instead of the actual76; corrected the fixture count, without changing production or loosening numerical tolerances. No original executable execution, live contact or GPU check was performed/authorized by this milestone.

## Handoff and remaining uncertainty

Seven new scoped files: Core policy/selector, smoke scenario, Python oracle, durable policy, checkpoint, own console project and entry point. All existing files remain read-only. No shared runner registration or canonical status/ledger/registry change. Lead may register `SkeletalDetailSelectionScenarios.Run()` in the synthetic native runner and later bind the modern selector to retained meshes. Private hash/scope/diff records support baseline-relative integration.

Remaining uncertainty: original thresholds/automatic mesh-skeleton coupling, exact off-axis sphere projection and renderer integration. Current policy intentionally uses center-depth approximation and conservative near rule; viewport visibility is caller-owned. Exact next action: lead review/acceptance/integration. Do not begin another round automatically.
