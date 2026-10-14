# Ground normalized input selection and queue binding

The GroundScene normalized event caller and selected family-1 end-binding edge are **Confirmed**. Equality between the selected InputMap queue at `+0C` and the player controller queue returned from `+4` remains **Unknown**. This contract does not establish a physical key binding or an effective profile.

## Input identity and reproduction

All addresses are VAs in the executable with SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Use [the static verifier](../../SWG-Godot-Client/tools/python/verify_player_controller_input_queue_binding.py) with explicit `--exe`, `--packages`, and a fresh ignored `--receipt`. Run [the independent checks](../../SWG-Godot-Client/poc/round53-tests/Networking/research_checks.py) with explicit `--exe`, `--packages`, and a fresh ignored `--output-dir`. Both hash the executable before relying on VAs; neither executes it or contacts a server.

## Observations and confidence

| Observation | Interpretation and confidence | Compatibility consequence |
| --- | --- | --- |
| Constructor `00519C40` clears `scene+4C` at `00519C78` and installs vtable `015DD988` at `00519E7F`; its COL resolves to GroundScene RTTI. Vtable slot `015DD98C` points to `0051AF10`. | Confirmed receiver class and initially null map slot. | Preserve a nullable selected map; non-null acquisition is a separate requirement. |
| The pinned 43-byte prefix at `0051AF10` forwards its event pointer to `0051AA40` at `0051AF36`. | Confirmed GroundScene virtual event caller. The rest of this method is outside this proof. | Deliver normalized events through the scene routing boundary. |
| Router `0051AA40` selects maps using `scene+9C`: selector 3 uses `+58`, 5 uses `+50`, 6 uses `+54`, and the default branch at `0051AA7B` uses `+4C`. A null selected map skips the call. At `0051AAC0`, selected map is ECX for `00AAE2E0`. | Confirmed map selection, without assigning profile names or asserting a current selector value. | Retain selector-dependent routing and the null-map behavior. |
| Handler `00AAE2E0` reads type at event `+4`, subtracts 5, and rejects values outside the unsigned 0..11 range. Type 8 table entry `00AAE448` points to `00AAE320`. That branch compares event device `+8` with map `+10`; mismatch skips processing. It pushes event index `+0C` and family 1, then calls `00AAE150` at `00AAE332`. | Confirmed normalized family-1 deactivation caller and discriminator/device/index structure. Physical event translation remains Unknown. | Preserve the type, device match, and input index conditions at this boundary. |
| Destructor `00519F90` null-checks `scene+4C`, clears that map's queue `+0C` at `0051A004`, calls `006E2350` at `0051A00B`, then clears `scene+4C` at `0051A013`. | Confirmed teardown order. Acquisition and full ownership semantics remain Unknown. | Disconnect the map queue before releasing this map slot. |
| Profile-template construction pushes a null third argument at `006E1FBF`, calls `00AACD50` at `006E1FC5`, and stores the result in `0191D214`. Constructor `00AACD50` stores that third argument into map `+0C` at `00AACD85`. | Confirmed null queue at template construction. Equality of this global template with `scene+4C` is Unknown; later assignment remains Unknown. | A template constructor alone cannot establish a live controller queue connection. |
| Getter `0062A9B0` returns `[controller+4]`; PlayerCreatureController RTTI is independently checked. | Confirmed getter structure; equality with selected map `+0C` remains Unknown. | Do not wire a replacement on assumed pointer identity. |

The producer behavior downstream of `00AAE150` is documented in [the prior producer contract](PLAYER_CONTROLLER_RUN_INPUT_PRODUCER_CONTRACT.md), with consumer behavior in [the run-flag contract](PLAYER_CONTROLLER_RUN_FLAG_CONTRACT.md). This investigation does not repeat the frozen producer audit. Matching producer and consumer semantics do not prove the queue instance identity.

## Bounded validation and limits

The verifier pins 44 instruction anchors, five complete bodies (253, 333, 61, 131, and 2 instructions), a 14-instruction event prefix, two RTTI chains, and selected data pointers. Independent checks pin ten byte witnesses, five relative calls, two RTTI names, and three data pointers, plus 14 rejection cases. These are static fixture checks; they establish no runtime occurrence or physical profile reachability.

Exactly 20 candidate xrefs were considered: 11 direct relative calls targeting `00AACD50`, `00AAE150`, or `00AAE2E0`, and nine operands referencing `0191D214`. Only the `0051AAC0` event path was selected for caller closure. The verifier regenerates and caps this inventory; a twenty-first candidate rejects the bounded check. Map setup for other selector branches does not establish the default map's queue assignment. Router helper `0078FC70` and other normalized event branches were not semantically closed.

## Next bounded question

Find the non-null writer to GroundScene `+4C`, then trace that exact map's `+0C` assignment to the queue returned by `0062A9B0`, including acquisition and teardown callers. Start from the profile acquisition lifecycle at `006E1E70` and GroundScene receive-message entry `0051BD10` as leads, not proven assignment sites. Stop when one instance identity is proved or a fresh bounded candidate budget is exhausted. Keep physical bindings, profile overlays, reset paths, callback failures, and downstream movement separate.


## Round54 accepted increment

[Selected GroundScene assignment](GROUND_SCENE_INPUT_MAP_QUEUE_ASSIGNMENT_CONTRACT.md) closes the bind expression scene[4C][0C]=scene[1C][2C][04], using an identical queue getter. Old-map disconnect precedes release. Queue construction/lifetime, selected error preconditions and physical profile remain separate open questions.
