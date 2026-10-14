# Player controller run-default preference

Date: 2026-10-04. Scope: global byte `01882394`, its bounded writers and local input override. All VAs refer to executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

This extends the [controller speed contract](CREATURE_CONTROLLER_SPEED_INPUT_CONTRACT.md). The [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R51_CONTROLLER_RUN_FLAG.md) records scope, reproducibility and open questions. No registry rename or production arbitration implementation follows.

## Preference identity and writers

**Confirmed observation:** initializer `00629310` calls `00504AF0` at `0062933F`, then stores `AL` to `01882394` at `00629353`. The getter reads a separate byte at `0191112A`; its effective value is not established here. The executable file-backed byte at `01882394` is initially `01`, but the initializer overwrites it, so that is not proof of the effective gameplay default.

At `006293AD..006293BC`, the initializer passes key string `runWhenMoving` (`01874BD0`), section `clientGame` (`0188239C`), and the byte address to `00B8DA10`. That wrapper passes the address as argument one to `00B9B3B0`. The latter reads the pointed byte at `00B9B4D5`, supplies it to `00B9BAC0`, and writes returned `AL` back through the same pointer at `00B9B4E3`. Registration and an indirect byte write are confirmed; the configuration reader's complete source precedence, parsing and error behavior remain Unknown.

Setter `0062CEA0` is a complete 13-byte body: read the low byte of argument one and assign it to the global. Getter `0062CEB0` is a complete six-byte body returning the global byte. The setter does not normalize arbitrary nonzero bytes to one.

**Independent context:** UI function `00EA54B0` uses widget string `checkRunDefault` (`018CC9D8`) at `00EA5653`, then passes setter `0062CEA0`, getter `0062CEB0`, and default supplier `00504AF0` to `00FF0B80` at `00EA567C`. The binding stores those pointers at record offsets `+0C/+10/+14`. The roles of the first two pointers are confirmed by their complete bodies; actual UI callback invocation/reset timing is outside this probe.

**Interpretation, Strongly inferred:** this is the player's default run-when-moving preference. That role is supported by the registration key, separate UI label/binding, and the already confirmed run/walk selection consumer. It is not an autorun movement-vector switch or a persistent per-input toggle proved by this investigation.

## Input-local transitions

| Trigger | Observation | Consequence |
| --- | --- | --- |
| Each returning controller update reaching `00629A12` | Copy current global byte to local `-1B` at `00629A2D` | Initialize local selection from the preference again |
| Input message ID `0x99` | `00629A91/00629A9C` route to `00629B84`; read global, test it, `sete` local `-1B` | Global zero gives local one; any nonzero global gives local zero |
| Repeated `0x99` messages in the loop | Branch reads the global each time, then joins at `00629CCC` | Recompute the inverse of the global; do not toggle the preceding local value |
| No such message | The copied default remains the input to the recovered speed selection | It still selects run only when run is ordered greater than zero |

Queue count/read calls are `00AA6660/00AA63E0`; the loop increments its index and repeats through `00629CE3`. The branch does not write the global. The frozen full-controller extent and six exact-address references corroborate the local/global distinction.

**Confirmed compatibility consequence:** preserve preference storage separately from an update-local logical override, and preserve the run-positive/walk fallback from the previous contract. For ordinary Boolean preferences this yields default run versus default walk and the opposite choice for the local override. For arbitrary nonzero bytes, the copy remains nonzero while the override normalizes to zero.

**Unknown:** the input producer's semantic command name, held/press/release/repeat timing, physical key binding, UI reset invocation, effective startup preference, later configuration loads, and aliased/indirect writers outside the bounded chain. No original executable or input loop was executed. Related receiver lifetime, acceleration/factor units and downstream movement remain separate questions.

## Verification boundary

The [verifier](../../SWG-Godot-Client/tools/python/verify_player_controller_run_flag.py) SHA-gates before address use, reuses frozen controller evidence, caps exact-address candidates at 32, checks the six observed candidates, 38 instruction anchors, three small string witnesses, and four complete body digests/extents. The investigation examined three enclosing writer functions: initializer, setter, and configuration-registration writer. Getter and UI-binding context add no claim of exhaustive writer closure.

The [independent runner](../../SWG-Godot-Client/poc/round51-tests/Networking/research_checks.py) maps PE sections independently and checks six byte witnesses, five relative calls and three strings. Eleven rejection cases cover identity/missing inputs, wrong assertion, truncation/unmapped spans, existing/public outputs and synthetic symlink/reparse ancestors. Lexical rejection is tested before resolution or creation; host links are not created. Private output must be fresh. Synthetic malformed helper spans do not execute a mutated original.

Next bounded action: identify the producer of queue message `0x99` and its held/edge/release emission rule, stopping at one input-map command and enqueue boundary. Leave full configuration/reset lifecycle and indirect writers separately open.

## Round52 producer boundary

[Original command-to-queue evidence](PLAYER_CONTROLLER_RUN_INPUT_PRODUCER_CONTRACT.md) closes one deactivation-slot0x99 producer and exact scalar/null-payload tuple. Its begin/held/cancel slots do not contain0x99. Selected InputMap/controller queue identity and normalized raw event invocation remain Unknown; no physical binding or live held-toggle claim follows.

## Round53 normalized input edge

[Selected GroundScene routing](PLAYER_CONTROLLER_INPUT_QUEUE_BINDING_CONTRACT.md)
reaches the accepted end-binding producer under type8/device/index conditions.
The queue instance assignment and effective physical profile remain Unknown;
matching producer/consumer roles do not prove instance identity.
