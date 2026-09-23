# TSTR/TSTP trail action pair (overnight block 7)

Four effective scripts (`play_1`, `play_1_face`, `play_2_face`, `play_2_on_queue`) each contain `TSTR/0001` followed later by `TSTP/0000`. The start INFO has a 16-bit actor selector and tags `FBRT` and `HSWS`; the stop INFO has two 16-bit actor selectors (both zero in these four instances). Original tags should be retained as opaque keys until their exact authored names are verified.

`0x005F9350` registers `StartTrailsActionTemplate`; factory `0x005F9450`/parser `0x005F94B0` accepts `0000` and `0001`. `0x005F9660` reads actor index plus two tags for `0001`. `0x005F9000` registers `StopTrailsActionTemplate`; `0x005F9190` accepts `0000`, with `0x005F92D0` reading two actor indices. The start template creates a runtime (`0x005F93D0` → `0x005A70E0`), whose update/start method is `0x005A6850`; the stop template method `0x005F90E0` resolves the selected pair/context and calls `0x005A6A20`.

`0x005A6850` reads `ClientGame.forceTrailsOnAllActions` through `0x00503470`; absent force mode, it requires a nonzero integer variable via `0x00408080`. It resolves the actor, a string variable through `0x005A7120` using the template's second tag, then calls `0x005A6AB0` with a bitmask. `0x005A6AB0` iterates enabled bits and creates/attaches 0x344-byte swoosh/trail appearance objects via `0x007AF4E0`, retaining them in a vector. `0x005A6A20` walks that vector and stops/destroys attached effects before clearing it. This is a concrete cross-action lifetime: start and stop must reference the same logical object/trail state.

The decompiler loses several implicit x86 arguments in `0x005A6850`, so the exact mapping of `FBRT` to bitmask and `HSWS` to appearance path is **strongly inferred**, not yet a confirmed schema. The observed call sequence does confirm a typed integer gate, string lookup, swoosh creation, and retained-effect cleanup. No server packet is emitted by these local action handlers as far as this path shows.

Provisional names: `0x005A6850` `start_trails_action_runtime`; `0x005A6AB0` `create_masked_swoosh_trails`; `0x005A6A20` `stop_and_release_retained_trails`.
