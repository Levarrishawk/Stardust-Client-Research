# Round53: controller update caller and unit carrier edge

Date: 2026-10-04. Question: identify one caller of PlayerCreatureController update00629750, its incoming argument and clock/unit provenance. Stop after one selected scheduling chain and at most two necessary new callees, with an evidenced unit contract or a precise remaining carrier edge. The [durable contract](../../docs/PLAYER_CONTROLLER_UPDATE_UNITS_CONTRACT.md) is canonical.

## Orientation and identities

Read AGENTS, START_HERE, accepted [Round52](../sessions/PARALLEL_DEVELOPMENT_ROUND_52_2026-10-04.md), [daytime authorization](../sessions/DAYTIME_DEVELOPMENT_2026-10-04.md), controller/helper/getter and existing ordinary clock/game-loop contracts, prior scheduling checkpoints and verifier. Checked status/ledger/UNKNOWN and curated terminology. No new script was invented before checking the existing clock verifier. Startup-FPU analysis was reused as an existing uncertainty boundary, not repeated.

Frozen53 contains3659 public files; manifest SHA256 `0373dac76092ab2cb10fc6311436885f9ecb5cfd62285f37042d57dd912693b4`; ZIP `434824c9edfc15d6390d339551207f7cf5701e934fa337444f09181a2dbc3ca0`; frozen main HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. Complete archive/member/name/casefold/hash/size, lexical root/ancestor/target links/reparse/containment and all-destination preflight preceded one overlay write. Prior52 manifest pin `71ca0659a6063973aa58d5133d1648d06c4aba063ffc3c05b3e4272f86dd243f` and every own52 final handoff SHA were cross-checked against accepted main52 integration before writing. Existing final52 Runtime-handoff was discovered and used directly; no reconstruction was necessary. All3659 postflight hashes pass; worker HEAD/index/private/unrelated preserved and main remains read-only.

Executable `C:/Stardust-DEV/Stardust.exe` SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` gated private probes and the public producer before optional imports/VA interpretation. Installed offline Python/pefile/Capstone and .NET only. No original execution, assets/network/capture/fuzz mutation, download, staging/commit/push or publication.

## Observation versus interpretation

**Confirmed:** immediate wrapper00B3D1C0 is30 bytes/14 instructions, digest `3ed9930225632212619fd5a20ef7ec4be3d584d346f99f9c4d3dc9c5cb685f37`. After00AA6670 it reloads its original argument and invokes receiver virtual+14 unchanged. Player vtable015E034C+14 maps00629750. That argument is the player's F32 multiplier, distinct from acceleration-helper current-speed argument.

**Confirmed:** common Object::alter00B23100 is398 bytes/152 instructions, digest `35ff7d2e4c18c3923b603194f018a84f9d058e50dfc15b226b89c30f867c7eee`. Literal flag gates and nonnull object+2C select the wrapper call; its incoming raw word is forwarded through ESI without direct unit conversion. Standard callee-save/normal-return and installed player-vtable conditions are explicit, not live execution claims.

**Confirmed prior evidence, selected rechecks:** QueryPerformance counter difference divided by API frequency yields seconds; established event chain reaches scheduler, which quantizes thousandths and converts per-object accumulated tick difference back to F32 seconds. Callback is object slot+10; equal ticks skip. Clock/global/due-queue and FPU/overflow/malformed policies remain bounded as in prior evidence.

**Confirmed remaining carrier:** CreatureObject constructor0042F5F1 installs015DCC34, whose+10 is00430260 rather than Object00B23100. **Unknown:** argument transformations and dispatch/attachment through that override. Under the explicit two-new-callee limit, its body is not followed. **Conditional implication:** unchanged override forwarding to common alter with a player controller would make player argument seconds. **Actual player unit classification:** Unknown until the override/receiver edge is closed. No units are inferred solely from rate-looking arithmetic or expected test values.

**Implementation consequence:** retain raw F32 forwarding and ordered rise/fall/sign/clamp/zeroing evidence; preserve the helper/update frame distinction. Native acceleration and actual player-unit parity remain deferred. No registry rename, shared project/entrypoint edit or research/function/protocol rescore occurs.

## Validation and corrected failure

[Producer](../../../SWG-Godot-Client/tools/python/verify_player_controller_update_units.py) passes56 anchors, two complete body digests/extents/direct destinations, one scheduler carrier window, two clock imports/one export, exact unit constants and three RTTI dispatch slots, plus frozen controller prerequisites. [Independent runner](../../../SWG-Godot-Client/poc/round53-tests/Runtime/research_checks.py) passes24 bytes/five calls/two constants/three dispatch/RTTI/two imports/two body hashes,55 synthetic opaque carrier cases/six nominal unit cases/11 negatives. Fixtures are synthetic and make no original malformed timing or floating-parity claim.

Initial producer asserted a scheduler branch at00B40F43; the aligned entry decode shows that address loads global tick high word, and the branch is00B40F48. Corrected that address only; fresh producer and independent checks pass without changing expected carrier/unit semantics. Failed producer.log and corrected producer receipt/log are retained privately. A partial exploratory tick window was not promoted as a complete function extent; the final scheduler window is explicitly named as such.

Default Core smoke passes18 journal events/zero failures; build zero warnings/errors. Workspace validator passes1550 Markdown files/zero broken or private links,2626 names/zero duplicates and345 packet rows. New-file AST/JSON/UTF8/BOM/links/whitespace and git diff --check pass. All3659 frozen files remain byte-identical with exactly five additions/public3664; HEAD/index and all five prior review hash pairs are preserved. Fresh producer and independent audit receipts agree. Exact path/size/SHA rows and failed/corrected logs are retained in the handoff. No original executable/runtime units were observed. No GPU/host change or unrelated closed-suite replay is required for this static-only scope.

Reproduce producer with explicit exe/packages/server-root and a fresh private receipt; independent runner uses same inputs and fresh private output-dir. Both output paths require ignored storage and lexical reparse/containment/fresh checks. Proprietary raw probes remain private. Public outputs are independently authored descriptions, compact verification witnesses and synthetic fixtures.

## Resume point

Next: bound00430260's incoming word through its necessary common alter/controller call(s), identifying any rescale/substep/discriminator and receiver binding. Stop at the next precise unclosed carrier rather than assuming Object-base forwarding. Then actual player seconds may be promoted if supported. Full pause/alternate-object/global/arithmetic parity and vehicle helper remain separate. Lead owns canonical reconciliation; worker stops after its three allowlisted new artifacts and two own53 runner files are hashed.
