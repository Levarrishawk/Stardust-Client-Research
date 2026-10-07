# Offline client regressions

Run the curated offline suites through [run_client_regressions.py](../tools/python/run_client_regressions.py). Supply explicit local Python, .NET, cached package and fresh private output paths. No original executable, sockets, packet capture, Godot or GPU is invoked by the default list. These are component regressions; a passing report does not establish live interoperability or a complete client.

```powershell
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' ./tools/python/run_client_regressions.py --dotnet 'C:/Program Files/dotnet/dotnet.exe' --python 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' --packages 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round49/godot-packages' --output "$PWD/local-private/client-regressions-new"
```

Choose a new output directory each time. Existing outputs are never removed. The package directory must contain cached `bcnencoder.net/2.3.0` and `communitytoolkit.highperformance/8.4.0` package files. Package sources are cleared, NuGet audit is disabled, and required packages are copied to private cache. Source-only staging preserves relative project references and Python imports. All builds, generated lock files, obj/bin/artifacts, test inputs, logs and reports stay beneath the private output; source files in the caller's tree remain unchanged.

## Required default suites

Execution order is fixed: .NET projects in table order, then Python suites in table order. A synthetic profile-generation command precedes them. The current list retains the twenty-one accepted Round91 defaults and adds Runtime92 strict controlled-launch/attempt author-native-verifier (twenty-two required suites). Missing required integration is a required-suite failure, never an optional success.

| ID | Entry point and arguments | Evidence boundary |
| --- | --- | --- |
| core-smoke | `poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj`, no arguments | Existing default engine-neutral smoke, including injected offline login |
| runtime82-manifest | `poc/round82-tests/Runtime/Runtime.csproj`, fresh private directory | Strict manifest parser/compiler, defensive ownership and malformed admission |
| runtime83-session | `poc/round83-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Required fixture authoring, native lifecycle and independent verification pipeline |
| assets83-preflight | `poc/round83-tests/Assets/Assets.csproj`, authored fixture/receipt paths | Required fixture authoring, native preflight and independent comparison; original install remains optional |
| runtime84-scene-bridge | `poc/round84-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Ordered world staging, minimum barrier, one ready, cleanup and independent ready verification |
| assets84-creature-preparation | `poc/round84-tests/Assets/Assets.csproj`, authored fixture/receipt paths | Installed template/animation discovery and fail-closed preparation; default uses synthetic resources |
| runtime85-selected-ingress | `poc/round85-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Selected lifecycle/baseline admission, explicit nearby/unsupported output and independent verification |
| runtime86-player-motion | `poc/round86-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Session local root submission and selected authoritative corrections; independent wire verification |
| runtime87-disconnect | `poc/round87-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Bounded outbound disconnect and local send/drain ownership; independent wire verification |
| runtime88-client-run | `poc/round88-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Independent client-run author/native/verifier pipeline; requires integrated Runtime88 |
| runtime89-client-application | `poc/round89-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Application lifetime author/native/verifier; exact CRC/PTAT static dependency staged and hashed |
| runtime90-controlled-movement | `poc/round90-tests/Runtime/Runtime.csproj`, pre-native fixture JSON | Received-ready, caller-clock cadence, backpressure, stop/cap and independent protected roots |
| runtime91-endpoint-approval | `poc/round91-tests/Runtime/Runtime.csproj`, authored fixture JSON | Mapping/preflight admission before acquisition, state preservation and cleanup |
| networking91-endpoint-handoff | `poc/round91-tests/Networking/Networking.csproj`, authored fixture JSON | Independent protected handoff, adversarial advertisements and rejected tampered receipts |
| runtime92-controlled-launch | `poc/round92-tests/Runtime/Runtime.csproj`, authored fixture JSON | Strict launch, one approved character, exact attempt deadlines and protected handoff |
| assets88-launch-profile | `poc/round88-tests/Assets/Assets.csproj`, authored fixture/receipt paths | Independent launch-profile producer/native/verifier pipeline; requires integrated Assets88 |
| runtime81-verifier | `poc/round81-tests/Runtime/test_verifier.py` | Authored parser/log mutation tests; no native scene proof |
| networking81-producer | `poc/round81-tests/Networking/replay.py --profile PROFILE --output-dir DIR` | Protected frame/cadence producer and authored-log verifier positives/negatives |
| networking82-producer | `poc/round82-tests/Networking/replay.py --profile PROFILE --output DIR` | Scenario authoring/admission and output/source immutability |
| networking83-authentication | `poc/round83-tests/Networking/replay.py --dotnet DOTNET --output DIR` | Independent authentication vectors before standalone native codec verification |
| networking84-injected-pump | `poc/round84-tests/Networking/replay.py --dotnet DOTNET --packages CACHE --output DIR` | Pinned injected peers, old I/O retirement, owned sends and independent protected auth/select/ready |
| runner83-tests | `poc/round83-tests/Rendering/test_runner.py` | Existing process/boundary checks plus Round85 real-process orchestration surrogates |

The [authored profile helper](../poc/round83-tests/Rendering/synthetic_profile.py) supplies only the fields consumed by the frozen81/82 producers. Its protected bytes come from the existing independent Python encoder. It is labeled synthetic and is **not** a runnable original-resource Godot profile. Producer reports explicitly distinguish authored log fixtures from native execution. No historical private profile is inferred.

For the joined original-resource Godot path, use the explicit command in
[selected-player verification](../poc/round84-tests/Integration/README.md). It runs
headless success, cancellation and minimum-service failure; optional `--gpu`
adds visible publication. This separate native command needs explicit original
installation and Godot inputs. Its PTAT decode is a bounded modern minimum
provider; complete procedural terrain readiness, live interoperability, movement
and protocol disconnect remain unverified.

`--timeout SECONDS` bounds each runner-owned command, default180, range0.1..3600. Windows commands start suspended, join a kill-on-close Job Object and then resume, allowing timeout to terminate their descendants. POSIX uses a new process group. Command arrays are passed without a shell; spaces, ampersands and dollar signs remain literal. Nested test drivers retain their own build/native command logs and receipts inside the same isolated workspace; the runner records their outer command and exit status.

## Explicit review overrides

`--suites core-smoke,runtime82-manifest,runtime81-verifier,networking81-producer,networking82-producer,runner83-tests` replays the frozen existing suites before Round83 integration. Every omitted default appears in `omittedDefaults` as `skipped-unverified`, `accepted:false`. This worker subset is not full-default acceptance. Unknown or duplicate IDs fail. Selection preserves canonical order rather than input order.

`--projects ABSOLUTE_JSON` replaces the .NET project list with a reviewed exact array. Each entry has exactly `id`, repository-relative `project` beneath `poc/` ending in `.csproj`, and a literal string-array `args`. Unique IDs, count1..32, path/argument bounds and duplicate JSON properties are validated. Whole `{output}` and `{profile}` arguments substitute fresh suite output and the authored profile; no shell expansion or partial placeholders occur.

```json
[{"id":"reviewed-parser","project":"poc/round82-tests/Runtime/Runtime.csproj","args":["{output}"]}]
```

`runtime83-session` is reserved for its exact project and `{fixture}` argument. It first runs `fixtures.py --out FILE --client-id-opcode 0xD5899226 --permissions-opcode 0xE00730E5`, then builds/runs the native project with that fixture, then runs `verify.py --fixtures FILE --result NATIVE_LOG --out RECEIPT`. The opcode arguments are the lead-reviewed sealed83 codec constants; native output never seeds the fixture. Missing author/verifier is failure.

`assets83-preflight` is reserved for its exact project and `{fixture}`, `{receipt}` arguments. It runs `peer.py generate DIR` before native build/run with `DIR/fixtures.json DIR/receipt.json`, then `peer.py verify DIR`. No installation arguments are supplied by default. The optional explicit installation producer mode is outside this command.

`runtime85-selected-ingress` is reserved for `poc/round85-tests/Runtime/Runtime.csproj`
and exactly one `{fixture}` argument. It first runs
`fixtures.py --out FILE`, builds/runs `Runtime.dll FILE`, then runs
`verify.py --fixtures FILE --result NATIVE_LOG --out RECEIPT`. The fixture is
authored before native startup; native stdout is captured independently for the
verifier. An absent project, producer or verifier, malformed override, build/test
failure or verifier failure prevents suite acceptance. The test uses actual Core;
the runner's orchestration surrogates exercise command ordering/failure behavior
and are not counted as selected-ingress native evidence. Selected CREO3/4/6 receipt
does not establish decoded CREO1/PLAY or live interoperability.

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

Round85 orchestration validation adds seven independent real-process surrogate
tests to the existing six `runner83-tests` methods. They verify author → build →
native → verifier execution, explicit omitted85 `skipped-unverified/accepted:false`,
failure stopping at author/native/verifier, missing required85 project and
rejection of an override that bypasses the reserved pipeline. Every process uses
private clear-only NuGet settings; surrogate build/native scripts never restore
packages or contact sockets. Actual Runtime85 and full thirteen-suite acceptance
require the lead-integrated implementation; workers do not import unpublished
sibling source. To replay accepted84 suites before that integration, explicitly
select all twelve prior IDs. The omitted85 entry remains unverified even when
those twelve pass.

Round86 reserves `runtime86-player-motion` for its exact project and one
`{fixture}` argument: `fixtures.py --out FILE`, build/run `Runtime.dll FILE`, then
`verify.py --fixtures FILE --result NATIVE_LOG --out RECEIPT`. The producer runs
before native execution, and verifier failure prevents acceptance. Missing86 is
required failure unless explicitly omitted and reported unverified. Caller
flags/counter/timestamp are bounded authored policy; a pass establishes neither
original cadence/units nor live movement compatibility.

The required runner suite now runs twenty methods: six original process/boundary
checks, seven frozen85 pipeline surrogates and seven86 surrogates. The86 tests
preserve the same real-process ordering, omitted-suite reporting, failure-stop
and isolated-package checks for the new exact CLI. They are orchestration
evidence, not the actual Core motion test. Before lead integration, select all
thirteen prior IDs to replay accepted85; omitted86 remains unverified. The
separate [motion adapter proof](../poc/round86-tests/Rendering/replay.py) needs
explicit Godot and installation inputs and runs headless only; it is outside
this engine-neutral default list.

Round87 reserves `runtime87-disconnect` for its exact project and one `{fixture}`
argument. It runs `fixtures.py --out FILE` before build/run `Runtime.dll FILE`,
then `verify.py --fixtures FILE --result NATIVE_LOG --out RECEIPT`. Missing project,
producer or verifier, a bypassing override, any build/native/verifier failure,
or timeout prevents acceptance. The ordinary default command above includes all
fifteen required suites. Omission requires an explicit reviewed override and is
reported `skipped-unverified`, `accepted:false`; fourteen prior suites passing
cannot substitute for the disconnect test.

The required runner suite now contains twenty-nine methods: six original checks,
seven85 and seven86 pipeline surrogates, plus nine87 surrogates. The87 tests use
real subprocesses for fixture-before-native ordering, exact CLI paths, omitted
reporting, failure propagation at producer/build/native/verifier, missing project,
reserved-override rejection and a timeout with a spawned descendant that must
not survive. Surrogate build/native programs do not implement protocol disconnect
and do not establish actual Core behavior. Full fifteen-suite acceptance requires
lead integration of the sealed Runtime87 implementation.

The default remains cache-only and engine-neutral; Godot/original-resource/GPU
joins require separate explicit inputs and runners. Local disconnect send/drain
fixtures do not prove server-observed session release or live interoperability.


## Round88 consolidated validation

`runtime88-client-run` reserves exactly one `{fixture}` argument and runs
`fixtures.py --out FILE`, build/run `Runtime.dll FILE`, then
`verify.py --fixtures FILE --result NATIVE_LOG --out RECEIPT`.
`assets88-launch-profile` reserves `{fixture}`, `{receipt}` and runs
`peer.py generate DIR`, build/run `Assets.dll DIR/fixtures.json DIR/receipt.json`,
then `peer.py verify DIR`. Authors precede native execution. Missing inputs,
bypassing overrides, failed commands and timeouts prevent acceptance.

The [consolidated driver](../tools/python/run_replacement_client_validation.py)
runs all eighteen current defaults followed by the reviewed original-resource
join. Supply every runtime and installation input explicitly:

```powershell
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' ./tools/python/run_replacement_client_validation.py --dotnet 'C:/Program Files/dotnet/dotnet.exe' --python 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' --godot 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --packages 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round49/godot-packages' --asset-root 'C:/Stardust-DEV' --config 'C:/Stardust-DEV/swgemu_live.cfg' --output "$PWD/local-private/replacement-validation-new"
```

Default `--join-round 89` selects the owned application entry
`poc/round89-tests/Integration/run_selected_player.py`; explicit87 or88 selects
the corresponding accepted entry without reducing the eighteen core requirements.
No arbitrary launcher, suite override or historical private profile is inferred.
Each component runs from its public repository entry so the join's Git source
inventory remains valid, then stages its own private sources and build outputs.
The fresh private aggregate output contains disjoint `core/` and `join/` results,
private cache settings, command logs, `source-manifest.json` and `report.json`.
Each outer component has a 900-second descendant-owned timeout; nested drivers
retain their own bounds. Public source hashes must remain unchanged, and the
join's reported source hashes must match current public files.

Aggregate acceptance requires all ordered core defaults with no omissions plus
a successful join receipt containing clean original-resource success,
cancellation, terrain failure, send error and send timeout modes. Add `--gpu`
only for an authorized GPU run; it requires a success screenshot receipt.
Without it GPU is explicitly `skipped-unverified`, `accepted:false`. A pass is
bounded offline evidence; live interoperability, server-observed release and
complete terrain readiness remain unverified.

The runner suite contains fifty-four methods: the previous twenty-nine plus
sixteen Round88 pipeline and nine consolidated-driver tests. The new tests use
real subprocess surrogates for CLI ordering, omissions, failures, source identity,
linked inputs, receipt rejection and timeout descendant cleanup. They establish
orchestration behavior, not Runtime88, Assets88, original-resource or GPU behavior.
The worker replays the prior fifteen defaults and required missing88 failures;
full seventeen-suite and original join88 acceptance belongs to lead integration.

Round88 integration exposed a Windows path-length failure when the runner itself
was staged beneath the consolidated output. Private build artifacts now use
compact numeric directories; suite names remain in reports and command labels.
An explicit 124-character staged-root regression replays the unchanged Round85
and Round86 author/build/native/verifier pipelines without suppressing tests.

## Round89 application acceptance

Round89 adds the engine-neutral `ReplacementClientApplication`, owning original preparation, session, pump, world, motions and run. Its injected pinned ports and minimum provider make startup reproducible without constructing sockets or DNS. Presentation remains caller-owned. Nonblocking retirement retains actual resource/provider/port borrowers; owner-thread finalization waits for their completion and the exact presentation task, with faults separately observed. Independent sealed-component replay and the original-resource headless/GPU application join pass. All eighteen required defaults pass through the consolidated command. Synthetic peers remain test inputs, not interoperability evidence.

The native provider-failure receipt reports the observed InvalidDataException and unsuccessful application drain separately from safely finalized ownership. Successful and cancellation cases require successful drains. No failed port/presentation cleanup is silently accepted.

## Accepted Round90 and next boundary

Round90 joins normal Godot login, credential entry, received character selection, controlled input and close through the accepted application owner. The engine-neutral pacer requires received server-ready and explicit fresh-state assertions, queues at most three roots at least400 ms apart on the caller clock, and stops on corrections, changed selection, unsupported placement or close. Independent original-resource native controls/input/close and GPU replay pass; all nineteen required defaults pass through the consolidated command. Signals and injected keys are offline test inputs, not physical OS input or live interoperability proof.

Three existing Low specialists supplied controlled movement, the Godot host and the named baseline comparison; Assets idle. Lead independently reviewed frozen components, replayed exact protected roots, inspected fourteen server inputs including inherited tangible/envelope serializers, and ran the original host and consolidated gate. Source correlation establishes no additional field-order blocker for a bounded outdoor human within limits. The prepared1,024-byte CREO3/4/6 cap remains a conditional rejection boundary; no chosen real character was measured. Deferred CREO1/PLAY supply no minimum local readiness mask, while gameplay/appearance parity remains unproved.

Next: validate the selected advertised galaxy/zone endpoint against an explicit operator-approved pinned mapping before acquiring the zone port. Current application fixes the peer but does not establish its relationship to the received hostname/zone port. Chosen-character baseline sizes, resource compatibility, validated position/floor and deployed movement admission still require scenario evidence. General CREO1/PLAY, avatars/customization, nearby actors, full terrain and live authority/server release remain open. Request specific controlled-server authorization only when offline preparation and operator inputs make it actionable.

## Accepted Round91 and next boundary

Round91 binds received selected galaxy and advertised zone hostname/zone port to an explicit operator-approved pinned IPv4 mapping before acquiring the zone port. Login mapping is checked before installation preparation or factory acquisition. Rejected selection preflight preserves login credentials, character-list state, clocks and submission counts. Independent protected-peer adversarial cases and the policy-enabled original-resource Godot host pass; the consolidated command runs all twenty-one defaults and nine native host cases including GPU. Mappings and peers are authored offline inputs; no DNS, socket or live interoperability proof is claimed.

Next: provide a standalone explicit-input bootstrap for the reusable application and Godot host, require approved endpoint and selected-character inputs, and enforce one bounded attempt with redacted phase diagnostics and observed cleanup. The main Godot project still opens the feasibility scene; the reusable host currently needs a caller to configure it. Chosen real-character baseline sizes/resources, validated position/floor, deployed movement admission and operator server/account prerequisites remain gates. General avatar/customization, nearby actors, full terrain and live server authority/release remain unproved. Request specific live authorization only when offline preparation and operator inputs make a controlled attempt actionable.

## Accepted Round92 and next boundary

Round93 adds an explicit controlled-transport entry over the reusable installation/application/Godot host and a bounded redacted attempt receipt written after actual local drain or startup rollback. Default startup still refuses contact. Offline native headless/GPU replay verifies the same strict admission/composition with mandatory synthetic spy ports, protected exchanges, one approved original human, bounded movement, deadlines and disconnect. Run, cleanup, rollback and diagnostic failures remain distinct; standalone quit follows ownership completion. The real UDP constructor branch is compiled and source-reviewed, never executed.

Next: obtain operator-approved deployment/account access, explicit advertised hostname-to-IPv4 mappings/ports and client version, one existing supported outdoor human with measured CREO3/4/6 limits and original resource identity, and a bounded minimum-readiness scenario. Keep movement disabled unless fresh stamp/ready state, matching validated position and passable floor are established. Then request specific authorization for one bounded controlled attempt. No original executable, real socket, DNS/server contact or live interoperability has been verified. General avatar/customization, nearby actors, full terrain and server-observed authority/release remain open.


## Current consolidated acceptance: Round93

The documented run_replacement_client_validation.py command now defaults to --join-round 93, executing all twenty-two Core/Python defaults and the [standalone original-resource harness](../poc/round93-tests/Rendering/README.md). Supply the explicit installation/tool/package paths above and a fresh private output; add --gpu for a native success screenshot. Earlier sections describe dated acceptance history; explicit87–92 joins remain available.

The current native matrix covers twelve standalone outcomes, plus one GPU success when requested, one unchanged injected92 success and four direct default-refusal/rejection cases. No worker delivery seal, historical argument vector or manually assembled original fixture is required. Original expectations and the synthetic protected transcript are independently regenerated from the supplied installation. Every admitted scene uses mandatory spy ports; the real UDP branch is source-reviewed and unexecuted.

Expected native exit1 retains command status failed and exact exit code, alongside ExpectedOutcomePassed; the aggregate checks the intended typed operation/cleanup/rollback/write outcome and actual ownership completion. Intentional failures never become successful drains. Source identities must remain unchanged. GPU omission is explicitly unverified. Server admission, authoritative movement, complete terrain and server-observed release remain unproved.
