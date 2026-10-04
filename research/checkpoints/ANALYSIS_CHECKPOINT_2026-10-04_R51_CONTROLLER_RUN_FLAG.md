# Round 51: run-default flag and local inversion

Date: 2026-10-04. Question: what is byte `01882394`, and do its direct writers and input branch establish default, toggle, held or reset semantics? Stopping condition: bounded exact-address candidate scan (maximum 32), no more than three enclosing writer functions, settings/UI correlation and the frozen run-positive/walk consumer. Stop with Unknown where indirect/state ownership remains unclosed.

## Inputs and method

Executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` was checked before optional imports and VA use. Frozen51 contains 3,593 files; manifest SHA-256 `89dc98232914213f9116518ae81b4c19acec314caf28cc96620127306714663b`, ZIP SHA-256 `ae1bd4fc59917d18cac2e0463cee9ee635831e9856f886c3515429dd3150ef89`. Full archive/path/hash/current-destination preflight passed before copying once, followed by all-member hashes and HEAD/index preservation. Every own50 final path matched the independently reviewed integration hash first.

Orientation covered START_HERE, accepted50, controller contract/checkpoint, current status/ledger/UNKNOWN terminology, curated rows and existing tools. Static pefile/Capstone and a private function inventory supplied direct bytes and extent correlation. No original execution, live contact, packet capture, download, publication or canonical rename occurred. The [durable contract](../../docs/PLAYER_CONTROLLER_RUN_FLAG_CONTRACT.md) owns the detailed observations and compatibility consequences.

## Findings versus interpretation

Six exact-address byte candidates were found: `00629354`, `006293B8`, `00629A14`, `00629B85`, `0062CEA7`, `0062CEB1`. These are operand/immediate addresses, not function entries. They correlate to initializer `00629310`, controller `00629750`, setter `0062CEA0`, and a getter at `0062CEB0` absent from the inspected function-inventory row set. No global absence-of-indirect-writers claim follows.

**Confirmed:** initializer stores a default supplier's return, then registers the byte under `clientGame/runWhenMoving`; registration writer `00B9B3B0` writes a configuration-reader return through the supplied pointer. The raw setter accepts an arbitrary byte, and the raw getter returns it. UI `checkRunDefault` binds the same pair and default supplier. Three enclosing writer functions were examined; no broad settings/startup survey followed.

**Strongly inferred semantic role:** player default run-when-moving preference, supported by settings identity, independent UI binding, and frozen run/walk consumer. The file-backed byte is one, but the initializer overwrites it; effective default remains Unknown.

**Confirmed local transition:** each relevant controller update copies the preference to local `-1B`; message `0x99` overwrites that local with logical inverse of the global. Repeated messages do not cumulatively toggle the local. This branch does not mutate the preference. The frozen reader/selection proof supplies run-positive versus walk behavior.

**Unknown:** physical input command, held/release/edge timing, UI callback/reset invocation, configuration parser/precedence, aliased writers, and complete state lifetime. Preference/default selection does not close final original locomotion arbitration, factor units, or related-pointer ownership.

## Reproduction and checks

[Producer](../../tools/python/verify_player_controller_run_flag.py) accepts explicit executable, static-package, server-root and fresh private receipt paths. [Independent runner](../../poc/round51-tests/Networking/research_checks.py) accepts those inputs plus a fresh private output directory. Both reuse the frozen lexical ancestor/reparse output gate. Detailed original bytes and exploratory output remain private; public evidence is small independently selected witnesses and descriptions.

Independent checks passed six byte witnesses, five relative calls, three strings and eleven rejection cases. The SHA-gated producer checks six candidates, 38 instruction anchors, complete bounded bodies, and the frozen controller/movement prerequisite. Synthetic symlink/reparse tests ensure rejection occurs before resolution/creation; they do not create host links. A read of default-supplier backing storage returned no file bytes (the mapped storage is not established as an effective runtime default); that observation was retained as Unknown, not treated as a false verification pass.

Final proportional smoke, workspace/link validation, whitespace, source hashes and scope/HEAD/index results are recorded in the handoff receipt after execution. No planned check is represented as completed by this paragraph. Lead owns shared status/ledger/navigation/registry reconciliation.

## Next action

Trace only the producer of message `0x99` and prove its input-map command and enqueue held/edge/release rule. Keep configuration-reset lifecycle, indirect writers and full movement fidelity as distinct questions. Implement no arbitration from guessed input timing.
