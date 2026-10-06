# Offline client regressions

Run the curated offline suites through [run_client_regressions.py](../tools/python/run_client_regressions.py). Supply explicit local Python, .NET, cached package and fresh private output paths. No original executable, sockets, packet capture, Godot or GPU is invoked by the default list. These are component regressions; a passing report does not establish live interoperability or a complete client.

```powershell
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' ./tools/python/run_client_regressions.py --dotnet 'C:/Program Files/dotnet/dotnet.exe' --python 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' --packages 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round49/godot-packages' --output "$PWD/local-private/client-regressions-new"
```

Choose a new output directory each time. Existing outputs are never removed. The package directory must contain cached `bcnencoder.net/2.3.0` and `communitytoolkit.highperformance/8.4.0` package files. Package sources are cleared, NuGet audit is disabled, and required packages are copied to private cache. Source-only staging preserves relative project references and Python imports. All builds, generated lock files, obj/bin/artifacts, test inputs, logs and reports stay beneath the private output; source files in the caller's tree remain unchanged.

## Required default suites

Execution order is fixed: .NET projects in table order, then Python suites in table order. A synthetic profile-generation command precedes them. Missing Round83 integration is a required-suite failure, never an optional success.

| ID | Entry point and arguments | Evidence boundary |
| --- | --- | --- |
| core-smoke | `poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj`, no arguments | Existing default engine-neutral smoke, including injected offline login |
| runtime82-manifest | `poc/round82-tests/Runtime/Runtime.csproj`, fresh private directory | Strict manifest parser/compiler, defensive ownership and malformed admission |
| runtime83-session | `poc/round83-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Required fixture authoring, native lifecycle and independent verification pipeline |
| assets83-preflight | `poc/round83-tests/Assets/Assets.csproj`, authored fixture/receipt paths | Required fixture authoring, native preflight and independent comparison; original install remains optional |
| runtime81-verifier | `poc/round81-tests/Runtime/test_verifier.py` | Authored parser/log mutation tests; no native scene proof |
| networking81-producer | `poc/round81-tests/Networking/replay.py --profile PROFILE --output-dir DIR` | Protected frame/cadence producer and authored-log verifier positives/negatives |
| networking82-producer | `poc/round82-tests/Networking/replay.py --profile PROFILE --output DIR` | Scenario authoring/admission and output/source immutability |
| networking83-authentication | `poc/round83-tests/Networking/replay.py --dotnet DOTNET --output DIR` | Independent authentication vectors before standalone native codec verification |
| runner83-tests | `poc/round83-tests/Rendering/test_runner.py` | Real process nonzero/missing/timeout, descendant termination and input rejection |

The [authored profile helper](../poc/round83-tests/Rendering/synthetic_profile.py) supplies only the fields consumed by the frozen81/82 producers. Its protected bytes come from the existing independent Python encoder. It is labeled synthetic and is **not** a runnable original-resource Godot profile. Producer reports explicitly distinguish authored log fixtures from native execution. No historical private profile is inferred.

`--timeout SECONDS` bounds each runner-owned command, default180, range0.1..3600. Windows commands start suspended, join a kill-on-close Job Object and then resume, allowing timeout to terminate their descendants. POSIX uses a new process group. Command arrays are passed without a shell; spaces, ampersands and dollar signs remain literal. Nested test drivers retain their own build/native command logs and receipts inside the same isolated workspace; the runner records their outer command and exit status.

## Explicit review overrides

`--suites core-smoke,runtime82-manifest,runtime81-verifier,networking81-producer,networking82-producer,runner83-tests` replays the frozen existing suites before Round83 integration. Every omitted default appears in `omittedDefaults` as `skipped-unverified`, `accepted:false`. This worker subset is not full-default acceptance. Unknown or duplicate IDs fail. Selection preserves canonical order rather than input order.

`--projects ABSOLUTE_JSON` replaces the .NET project list with a reviewed exact array. Each entry has exactly `id`, repository-relative `project` beneath `poc/` ending in `.csproj`, and a literal string-array `args`. Unique IDs, count1..32, path/argument bounds and duplicate JSON properties are validated. Whole `{output}` and `{profile}` arguments substitute fresh suite output and the authored profile; no shell expansion or partial placeholders occur.

```json
[{"id":"reviewed-parser","project":"poc/round82-tests/Runtime/Runtime.csproj","args":["{output}"]}]
```

`runtime83-session` is reserved for its exact project and `{fixture}` argument. It first runs `fixtures.py --out FILE --client-id-opcode 0xD5899226 --permissions-opcode 0xE00730E5`, then builds/runs the native project with that fixture, then runs `verify.py --fixtures FILE --result NATIVE_LOG --out RECEIPT`. The opcode arguments are the lead-reviewed sealed83 codec constants; native output never seeds the fixture. Missing author/verifier is failure.

`assets83-preflight` is reserved for its exact project and `{fixture}`, `{receipt}` arguments. It runs `peer.py generate DIR` before native build/run with `DIR/fixtures.json DIR/receipt.json`, then `peer.py verify DIR`. No installation arguments are supplied by default. The optional explicit installation producer mode is outside this command.

The override is a reviewed execution list, not an untrusted-code sandbox. Review a new project's build targets, code, argument requirements and offline behavior before selecting it; do not select live socket adapters. A missing explicitly listed project fails. Default .NET suites omitted by the replacement are reported unverified. Python default suites remain required unless explicitly omitted with `--suites`.

## Optional evidence remains unverified

The report always lists the following as `skipped-unverified`, `accepted:false`:

- Round80 terrain restart numeric verification needs its explicit pinned runtime and terrain recipes. Existing [verifier](../poc/round80-tests/Runtime/verify_restart.py) checks their hashes and pre-native expectations. This command does not infer those private inputs.
- Round80 Networking's historical [replay](../poc/round80-tests/Networking/replay.py) hardcodes runtime/cache and tree-local build outputs. It is not invoked by this isolated runner. Later producer suites do not substitute for that native regression.
- Godot/HUD/world fixtures require explicit Godot and fixture inputs. This command lacks them, makes no Godot changes this round, and does not run them. Use their bounded explicit runners for separate evidence.
- Original-installation proofs require explicit installation paths and resource/context inputs. A synthetic preflight pass does not establish original-player/terrain dependency closure.

These skips concern this execution only and do not erase previously accepted evidence. No optional skip is included in the executed pass count.

## Receipts and validation

`report.json` records the exact ordered command arrays/cwd, exit codes, timeout status, duration, private logs, actual suite statuses, omissions and limitations. `source-manifest.json` pins staged source paths, SHA-256 and byte lengths separately to keep the report compact. Exit0 means every selected required suite exited0; exit1 means admission rejection, missing input, build/test failure or timeout. Preflight rejection happens before output creation and is printed to stderr. After output creation, failures retain a failed report and logs; unattempted suites are not claimed passed.

Round83 worker verification: explicit frozen subset passed Core smoke, Round82 parser28 checks/90 negatives, Round81 verifier six unittest methods, Networking81 producer35 checks/43 rejections, Networking82 producer412 checks/44 rejections and six runner test methods. A first timeout-child probe exposed denied `taskkill`; Job Object ownership repaired it, and the real descendant timeout test passed. Separate CLI negatives returned1 for missing required project/script/runtime and timeout. The integrated default83 lifecycle/codec/preflight list is not executed by this worker; the lead owns its final replay after integration.

The next gate is that full default replay against the lead-integrated exact sources, followed by separately authorized and bounded interoperability validation. The lead-owned `docs/REPLACEMENT_CLIENT_MILESTONE_PLAN.md` and `docs/CONTROLLED_SERVER_VALIDATION_PLAN.md` were written after this frozen snapshot; they define the next integration and controlled-server gates on the main workspace. Research, implementation, protocol and function coverage denominators remain separate.
