# Player controller queue lifecycle: bounded constructor and destruction carriers

The selected constructor chain is **Confirmed** through two new complete bodies:

`00629400 -> 006385C0 -> 006E4A20 -> 00B4AE50 (unexpanded)`

Both new bodies forward the same receiver and object argument. Actual queue `controller+4` allocation, queue class/RTTI, counts/layout, ownership, non-null preconditions, and queue destruction ordering remain **Unknown**. The two-primary-body limit is reached; no underlying queue implementation is inferred from constructor names or the already established input-map assignment.

## Input and reproduction

All VAs use executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Verifier (external historical reference): explicit `--exe`, `--packages`, and fresh ignored `--receipt`. Independent runner (external historical reference): explicit `--exe`, `--packages`, and fresh ignored `--output-dir`. SHA validation precedes package import and VA interpretation. These are static checks, not original execution or a runtime occurrence.

## Observations, interpretation, and consequence

| Direct observation | Interpretation and confidence | Compatibility consequence |
| --- | --- | --- |
| Accepted player constructor `00629400` calls `006385C0` at `00629425` and eventually installs vtable `015E034C` at `006295B8`. | Confirmed selected constructor boundary, reusing the prior construction proof. | Follow the base constructor before assuming a queue exists. |
| Complete `006385C0` body is 267 bytes/90 instructions. `006385D9` loads argument `[ebp+8]`; `006385DE` retains incoming ECX in ESI; `006385E0` pushes the argument; `006385E4` calls `006E4A20` without receiver adjustment. It installs `015E04AC` at `006386B4` and returns that same receiver. | Confirmed CreatureController constructor carrier via actual RTTI. | The queue field belongs to the same object/base address along this carrier; unrelated higher-offset allocations do not establish its queue. |
| Complete `006E4A20` body is 85 bytes/29 instructions. It loads `[ebp+8]`, retains incoming ECX, pushes the argument, and calls `00B4AE50` at `006E4A43` without adjustment. It writes bytes/member state at `+0C/+10`, installs `015E1AB8` at `006E4A57`, and calls unexpanded `00B4B070` with receiver and argument 1. | Confirmed ClientController constructor carrier. The delegated constructor and post-base helper semantics remain Unknown. | Stop queue allocation claims at `00B4AE50`; do not treat the argument-1 helper as a proven queue operation. |
| Neither selected carrier directly stores to controller `[ESI+4]`. The CreatureController body does initialize an unrelated newly allocated object's `[EAX+4]`. | Confirmed direct write distinction; callee side effects remain Unknown. | Keep base queue storage separate from higher-offset support objects and their null branches. |
| Known accessors `00B3D340` and `0062A9B0` each read `[ECX+4]` and return. | Confirmed accessor structure, not an allocation or ownership proof. | Preserve the queue identity already closed by the scene bind contract while leaving queue lifetime open. |

The input-map assignment/replacement proof remains in [the accepted ground map contract](GROUND_SCENE_INPUT_MAP_QUEUE_ASSIGNMENT_CONTRACT.md). It is not replayed in full here. This investigation does not change its confirmed pointer-expression identity.

## Actual destruction-slot boundaries

The RTTI chains and slot-zero targets come from the exact installed vtables, not adjacent-function guesses:

| Controller class | Vtable / COL / type descriptor | Slot-zero thunk | Unexpanded body called first |
| --- | --- | --- | --- |
| PlayerCreatureController | `015E034C / 016113A0 / 0186F484` | `006295D0` | `00629600` |
| CreatureController | `015E04AC / 016116B4 / 0186AE94` | `006386D0` | `00638700` |
| ClientController | `015E1AB8 / 0161385C / 0186D06C` | `006E4A80` | `006E4AB0` |

Each thunk is exactly 33 bytes/14 instructions. It retains the incoming receiver in ESI, calls the listed body, tests bit zero of its stack flags argument, and conditionally passes the same receiver to `00AC1640`; it returns that receiver with `ret 4`. The call/branch/release ordering is **Confirmed**. A deleting-destructor role and allocation-release role of `00AC1640` are **Strongly inferred** from the slot, flags, and shared thunk shape. The unexpanded bodies are not treated as a proven base-destructor chain, and their queue cleanup operations are Unknown.

These three directly needed tiny thunks do not consume another primary lifecycle-body investigation. No non-deleting destruction body was expanded. This establishes where a future selected teardown trace must begin, not when queue storage is freed or whether map disconnection precedes controller destruction at runtime.

## Null/error and ownership limits

The CreatureController constructor has allocation-null branches for support members at `+18`, `+1C`, `+24`, and `+4C`; none establishes the queue field `+4` or its policy. The delegated constructor at `00B4AE50`, post-base helper `00B4B070`, unwind cleanup, and non-deleting destruction bodies may determine queue initialization and cleanup, but are unexpanded. Do not infer non-nullness, graceful recovery, destruction after failed construction, borrowed/owned queue storage, or a recovered count/layout from this evidence.

The prior GroundScene binder disconnects an old map before releasing that map reference. Its ordering relative to destruction of the selected controller is a separate **Unknown**; this investigation has not closed one complete scene/controller teardown sequence. Physical bindings, configuration/profile, normalized event occurrence, and live emission also remain separate.

## Validation and resume

The verifier pins 38 anchors, exactly two primary constructor bodies, three tiny deleting-destructor-shaped bodies, two tiny accessors, and three controller RTTI chains. Thirteen selected call/vtable candidate edges fit the fresh limit of 20; no whole caller/queue census is performed. The independent runner checks 11 byte witnesses, ten calls, three RTTI strings, three virtual slots, two carrier patterns, and three thunk patterns. Eighteen rejection cases cover identity, missing input, altered instructions/spans/RTTI/edges, truncated or wrong receiver carrier, flags-mask changes, unmapped spans, malformed output choices, lexical link/reparse ancestry, and cap overflow. Fresh nested output succeeds; rejected output effects are checked. All checks are static or synthetic helper tests.

Next bounded question: inspect `00B4AE50` as the next construction body and one corresponding destruction body selected from its actual installed vtable. Establish the direct queue `+4` writer, exact queue class/count/layout, and allocation/failure path before extending queue teardown. Reuse the closed carrier and scene bind evidence rather than repeating it.
