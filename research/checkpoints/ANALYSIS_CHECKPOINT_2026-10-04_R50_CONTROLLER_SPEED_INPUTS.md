# Round 50: controller speed receiver and direct consumption

Date: 2026-10-04. Question: which receiver supplies run/walk at `00629F7E/00629F8A`, and where are those returns compared, stored, and used in the containing controller? Stopping condition: identify containing class/entry, receiver alternatives/null handling, selection, intermediate/target/current stores, and immediate vector/update consumer. Broader input, factor, ownership, and movement algorithms remain open.

## Inputs and tools

- Pinned executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; checked before VA use and optional imports.
- Frozen round 50 source HEAD: `5493338d9cd4255cf58f25de6c4cd15f6507b818`; 3,548-file overlay preflight/postflight passed once. Manifest SHA-256 `4c041de82399f47d5968db6752dae245835f953e2d0b192e2aaf9690bb2f5627`; ZIP SHA-256 `d50c8328081477f21bc610001553b942dd997f9710ec87e67c05f907304be140`.
- Worker HEAD: `65de8c88141071744dc75594be126b3bab3696a5`; index was empty. Accepted round 49 owned rows were independently checked before overlay.
- Python, pefile, and Capstone performed static reads only. Curated provisional-name rows and the prior movement-reader/writer evidence supplied terminology; local Ghidra inventory supplied function extent correlation, not semantic proof.
- [Producer](../../../SWG-Godot-Client/tools/python/verify_creature_controller_speed_inputs.py) and [independent checks](../../../SWG-Godot-Client/poc/round50-tests/Networking/research_checks.py). No original execution, live-server contact, asset mutation, or packet capture.

## Evidence and findings

The [durable contract](../../docs/CREATURE_CONTROLLER_SPEED_INPUT_CONTRACT.md) is canonical for this bounded result. It records exact VAs, confidence, and compatibility implications without duplicating the entire function body.

**Confirmed:** `00629750..0062A62F` is the vtable `+14` entry of `PlayerCreatureController`: constructor install `006295B8`, vtable `015E034C`, COL `016113A0`, TypeDescriptor `0186F484`, exact RTTI name. Complete 3,808-byte body digest: `fcc6a2b5711874f2f8d6bea09bb54b388a696859b44b2c038a339a4d1b241b0a`; 1,163 instructions. Proposed role is input update and speed consumption; no canonical registry rename.

**Confirmed:** controller `+8` or `00438650`'s related result supplies both reader receivers, controlled by byte accessor `00438620`. Null branches fall back to `00638810`. Selected `EDI` is saved/restored around the matrix temporary. The two readers take literal zero and narrow to separate F32 locals.

**Confirmed:** local flag plus run-positive comparison selects run; otherwise walk. Selection feeds vector magnitude, a separate controller `+154` store, factor-adjusted target, and ordered rise/fall current-speed `+64` updates. A later branch can zero current speed before the immediate three-component vector write through `007CA1F0`. The next base update is `00638810`.

**Interpretation / Unknown:** owner member name is inferred from controller relation; predicate, global flag, related-pointer relationship/lifetime, factor meaning, time units, acceleration-helper alternatives, and vector sink type are not recovered. Direct branch validation does not establish complete indirect reachability or whole-controller semantic closure. The original x87 stores/arithmetic order are evidence; no replacement bit-parity claim is made.

## Reproduction and validation

Run the independent checks with explicit executable, package, server-root, and a fresh output directory under repository `local-private`. Run the producer separately with a fresh private receipt. Original executable bytes and bulk decoded rows remain private and are not linked from curated documents.

The independent runner passed 12 literal-byte witnesses, eight independently decoded relative calls, and nine rejection cases. The producer also passed the complete function/destination and frozen reader/writer prerequisite checks. An initial assertion incorrectly named `EAX` in the downward clamp; direct bytes showed `EDX`, and the corrected assertion is independently checked. Initial private probes failed to find a prologue in an overly narrow window; later complete decoding and inventory correlation established the correct extent. Misaligned exploratory windows were not promoted as evidence.

Final workspace/link validation, smoke verification, owned-path hashes, HEAD/index checks, and scope results are recorded in the private handoff receipt. Any limitation in those checks must be reported in the handoff; this checkpoint does not treat a planned check as executed.

## Compatibility consequence and next probe

Preserve selected-object identity, zero reader argument, F32 narrowing, ordered selection, distinct intermediate/target/current states, conditional later zeroing, and immediate vector/update ordering. Do not infer final movement arbitration from these stores. No production implementation, protocol claim, corpus denominator, or readiness metric changed.

Next action: a bounded static probe of global `01882394` and its input dispatch branch, to determine the flag's gameplay meaning and state transitions. Stop after resolving that flag; related lookup ownership, factor/helper closure, sink type, and downstream root-motion remain separate questions. Lead integration owns status, ledger, navigation, unknown-function, and curated registry reconciliation.
