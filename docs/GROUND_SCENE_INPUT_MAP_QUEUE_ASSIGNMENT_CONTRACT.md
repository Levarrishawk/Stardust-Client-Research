# GroundScene InputMap queue assignment

One selected GroundScene construction/setup path establishes **Confirmed pointer-expression identity** at its bind boundary:

`scene[0x4C][0x0C] = scene[0x1C][0x2C][0x04]`

The right-hand controller queue field is exactly what `0062A9B0` returns. The binder calls the identical four-byte getter `00B3D340`; it does not call `0062A9B0` directly. This closes the map/controller queue assignment gap for this path. Physical bindings, effective profile, runtime occurrence, and non-null queue allocation remain separately Unknown.

## Identity and reproduction

Addresses are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static verifier (external historical reference): explicit `--exe`, `--packages`, and fresh ignored `--receipt`. Independent runner (external historical reference): explicit `--exe`, `--packages`, and fresh ignored `--output-dir`. SHA validation precedes imports and VA interpretation. Original execution and live contact are not used.

## Selected evidence chain

| Observation | Interpretation and confidence | Implementation consequence |
| --- | --- | --- |
| GroundScene constructor `00519C40` installs vtable `015DD988` at `00519E7F`, with GroundScene RTTI. It passes the same scene receiver to setup `00518EB0` at `00519F68`. | Confirmed class/caller path. The earlier null initialization at `00519C78` precedes this later setup. | Do not treat initial map nullness as the final constructor state. |
| Setup preserves scene in ESI at `00518ED1` and object argument in EDI at `00518EDB`. Successful allocation at `00519218..00519238` constructs `00629400` with that object argument. Its return value is passed to `00B22BB0` at `00519252`; object is stored into scene `+1C` at `00519259`. | Confirmed successful controller installation branch. Constructor installs `015E034C` at `006295B8`, whose RTTI is PlayerCreatureController. | Bind against the current controller stored on the selected scene object. |
| Setter `00B22BB0` compares old object `+2C` with its argument; if different and old is non-null, it calls the old controller's slot zero with argument 1 before storing the new pointer at `00B22BCB`. | Confirmed pointer replacement/destruction-call shape. Full destructor semantics are not closed here. | Preserve replace-versus-same-pointer behavior and prior controller teardown ordering. |
| Setup uses scene receiver at `00519611` and calls binder `0051FAD0` at `00519616`. Binder reads existing `scene+4C`; if present, it writes zero to that map `+0C`, calls `006E2350`, then clears scene `+4C`. | Confirmed selected acquisition/replacement boundary. | Disconnect the old map queue before releasing the old map reference. |
| Binder calls `006E1E70` at `0051FAF4`, stores EAX into scene `+4C` at `0051FAFC`, reads object from scene `+1C` and controller from object `+2C`, and calls `00B3D340` at `0051FB02`. It reloads the stored map at `0051FB07` and writes EAX into map `+0C` at `0051FB0A`. | Confirmed exact selected-map queue assignment. This is direct register/data flow, not a match between producer and consumer semantics. | Retain pointer/reference identity at the input-to-controller queue boundary. A replacement need not imitate legacy internal offsets. |
| `00B3D340` and `0062A9B0` each contain `mov eax,[ecx+4]; ret`, with identical SHA-256 `f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f`. | Confirmed identical queue accessor expression for the current controller. | The binder writes that controller's actual queue field, without cloning it or substituting another queue. |

The construction branch establishes the player controller installation. The binder independently re-reads the current object/controller fields rather than using a cached constructor return. Transitive side effects of every intervening setup helper and the controller's runtime dynamic type at a later rebind are not exhaustively proved. The pointer-expression identity at the binder remains direct regardless of those wider questions.

## Acquisition, replacement, teardown, and errors

Acquisition `006E1E70` reads shared global `0191D214`. If it constructs a template, it supplies a null queue to `00AACD50`, then stores the result into that global. Its successful tail loads the global into ESI at `006E202D`, increments reference counter `0191D218` only when that pointer is non-null, and returns ESI at `006E204C`. Thus a successful non-null acquisition stores the exact shared map into scene `+4C`, then the binder overwrites its queue with the current controller queue. Effective selection/matching and configuration helper semantics remain Unknown.

Release `006E2350` decrements the global counter at `006E235E`. At zero it calls `00AAD020` on the global map and passes that map to `00AC1640`, then clears the map global and related profile-object global. A destruction/free role is Strongly inferred from this release pattern; the two callees are not separately audited here. It uses the global rather than the pushed map argument. This establishes shared reference handling; unique map ownership, simultaneous scene behavior, underflow prevention, and the global's full shutdown lifecycle are not inferred.

Binder null-checks only the old map. It contains no guard for a null acquisition result, scene object, or controller before dereferencing the new chain. Acquisition has explicit zero-return exits, including `006E20D6`, if the preceding helper calls return normally; its allocation branch can also produce a null global. The original error helper's throwing/termination policy and externally enforced preconditions remain Unknown. Static inspection therefore does not establish graceful failure recovery or prove an observed crash.

GroundScene destructor's disconnect-before-release ordering is already evidenced in [the prior map-selection contract](PLAYER_CONTROLLER_INPUT_QUEUE_BINDING_CONTRACT.md). That contract also closes selected normalized type-8, device-matched family-1 end-binding. [The producer contract](PLAYER_CONTROLLER_RUN_INPUT_PRODUCER_CONTRACT.md) and [consumer contract](PLAYER_CONTROLLER_RUN_FLAG_CONTRACT.md) remain the downstream semantic evidence. Their frozen 44-anchor producer audit is not repeated here.

## Validation scope and remaining boundary

Three candidate direct-call xrefs were inspected within a fresh cap of 20: `0051FAF4 -> 006E1E70`, and `00519616` / `00BAA610 -> 0051FAD0`. Only the constructor/setup caller was selected; the latter caller's lifecycle is not closed. The receive-message lead `0051BD10` was not needed to prove this acquisition path.

The verifier checks 60 anchors, two RTTI chains, and nine complete bounded bodies with instruction counts 253, 728, 110, 19, 20, 187, 21, 2, and 2. Body signatures constrain reproduction; they do not imply all instructions have semantic labels. The independent runner checks 14 byte witnesses, seven calls, two RTTI strings, two data pointers, and a separately parsed seven-instruction instance expression. Its 18 rejection cases include wrong map/controller offsets, wrong getter agreement, missing candidate, wrong identity/evidence, invalid outputs, lexical link/reparse ancestry, and cap overflow. Synthetic checks never execute original input.

Next bounded question: trace the player constructor's base call `006385C0` to controller `+4` queue construction and teardown, and determine the selected null/error preconditions. Then separately establish one effective physical binding/profile occurrence. No runtime reachability percentage or overall compatibility completion is implied by this closure.
