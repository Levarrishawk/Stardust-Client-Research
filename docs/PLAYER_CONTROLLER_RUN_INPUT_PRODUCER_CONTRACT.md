# Run-input producer: original command record to queue boundary

Date: 2026-10-04. Scope: one supported original command producer of message `0x99` consumed by `00629750`. Executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. This extends the [run-default preference contract](PLAYER_CONTROLLER_RUN_FLAG_CONTRACT.md); the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R52_RUN_INPUT_PRODUCER.md) records reproducibility and limitations.

## Original occurrence and command identity

**Confirmed:** a fresh read of the original ground command asset in `mtg_patch_011_files_01.tre` yields 15,253 bytes with SHA-256 `bbff0aad5678600493b89a4c27d85da05842548163bb7f44921bfa64347571f4`. It is `CMDS/0006`. One named command has an action operation equal to decimal 153 (`0x99`). The original command key and all four records are retained in the private receipt; the public verifier selects that unique record from the identified bytes rather than hard-coding extracted names.

The dated command inventory independently lists one such occurrence among 1,744 action rows, in the ground character category, with device mask 3 and both command flags zero. Fresh original bytes corroborate that row. This is one real original occurrence, not a newly recomputed effective overlay or proof of the active user profile.

| Action slot | Runtime offset | Original operation | Raw scalar parameter | Text payload |
| --- | --- | --- | --- | --- |
| 0: begin/activation | `+1C` | 0 | zero | empty |
| 1: end/deactivation | `+30` | `0x99` | F32 positive one (`3F800000`) | empty |
| 2: held/update | `+44` | 0 | zero | empty |
| 3: cancel/reset | `+58` | 0 | zero | empty |

**Implementation consequence:** preserve the actual action-slot record. A momentary-sounding name is insufficient evidence for continuous held emission. This record emits on the runtime deactivation slot; its begin, held and cancellation slots do not enqueue `0x99`. Custom or changed command assets may differ and must be decoded independently.

## Supported enqueue path

**Confirmed observation:** `00AAE150` finds a matching active record. At `00AAE1D3`, it loads the InputMap queue pointer from `this+0C`; null queue or null command skips dispatch. At `00AAE1E4/00AAE1E7`, command action `+30` is passed to `00AB33E0`, with the queue and a null scalar override. The active record is subsequently unlinked and freed at `00AAE1F9`. Invocation of this end-binding transition is conditional on a matching live record; no event is promised for an unmatched input.

Dispatcher `00AB33E0` reads the operation at action `+0`, rejects zero, and uses the action scalar at `+4` when the override pointer is null. Empty text is represented by equal string begin/end pointers at action `+8/+0C`. That branch calls `00AA6640` at `00AB3437`, with operation, scalar word and null payload. It avoids the nonempty-text payload-allocation branch.

Wrapper `00AA6640` forwards to append routine `00AA6480` at `00AA6651`. The latter stores a 16-byte queue record containing operation, unchanged scalar word, null payload pointer, and a null fourth word. Both direct-capacity and growth-copy paths preserve four words. A zero operation follows the diagnostic/early-return path. An optional queue callback at `queue+10`, virtual slot `+4`, follows append; its side effects and allocator failure policy are outside this contract.

Queue appends use the buffer at `queue+0C`; indexed reads use `queue+8`. `00AA6670` swaps those pointers at `00AA667D/00AA6687` and clears/reuses the former read buffer. Reader `00AA63E0` indexes 16-byte records and copies the first three words to output pointers, optionally copying the fourth. This proves the buffer-transfer boundary, not its complete scheduling lifecycle.

## Consumer and remaining connection

The frozen controller proof establishes `0062A9B0` returning controller `+4`, the indexed queue read, message comparison at `00629A91`, and `00629B84/00629B8B` assigning the logical inverse of global `01882394` to local `-1B`. The scalar parameter and null payload do not supply the preference to that branch. Repeated consumer messages recompute the inverse of the global, and a later update copies the preference again.

**Confirmed:** the original command record and library producer support an end/deactivation-slot `0x99` enqueue, with exact operation/scalar/payload structure. **Unknown:** whether the currently selected ground InputMap's queue pointer is the same instance as the controller's `+4`, the actual profile/overlay in use, physical key identity/translation, and the exact raw-event caller that invokes end-binding. The prior slot-transition evidence labels this slot release, but this probe does not independently close a particular physical keyboard key-up path or guarantee live emission. No packet or server message is involved in this local queue tuple.

**Compatibility consequence:** model command lifecycle separately from physical bindings and default run preference. Preserve the recovered deactivation record and local inversion when supplied, while leaving unsupported binding/timing policy explicit. Do not implement a held toggle or global mutation from the command name.

## Verification and reproduction

Use the [verifier](../tools/python/verify_player_controller_run_input_producer.py) with explicit executable, static packages, server root, command input and a fresh private receipt. The command input may be the original `.tre` archive or a private extracted asset. The archive path reuses the inspected existing archive reader functions without executing its CLI; it selects the ground command source and pins its extracted hash. Archive and direct-asset sizes are checked before reading. No priority scan is claimed.

The verifier checks 44 instruction anchors and six complete body digests/extents, plus the frozen controller/run-flag evidence and the original four-slot record. The bounded command witness reader preserves four actions and rejects invalid forms/versions, lengths, strings, duplicate/ambiguous commands and excessive size/counts; it is a focused research checker, not a replacement general importer.

The [independent runner](../poc/round52-tests/Networking/research_checks.py) checks ten instruction-byte witnesses, six relative calls, a dynamic original command-string occurrence and four original action slots using separate decoding. One independently authored synthetic record and 30 deterministic rejection cases cover identity/assertion failures, malformed framing/metadata/strings, missing/ambiguous producers, size caps, and fresh/private output safety. Synthetic lexical symlink/reparse tests reject before resolution/creation without creating host links. Original command names and bytes remain private.

Next bounded question: trace the actual selected ground InputMap queue assignment and its end-binding caller to controller `+4` and one normalized key-up event. Stop at that connection; leave effective profile selection, configuration reset, queue callback and broader input timing separately open.

## Round53 normalized caller

[Ground routing evidence](PLAYER_CONTROLLER_INPUT_QUEUE_BINDING_CONTRACT.md)
closes selected-map routing and the normalized type8/device-matched/family1
end-binding caller. Selected map+0C equality with controller+4 remains Unknown;
constructor/template null queue and teardown disconnection are explicit.
Physical binding and effective profile remain separate.
