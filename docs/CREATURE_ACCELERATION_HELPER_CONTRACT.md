# Creature acceleration helper contract

Date: 2026-10-04. Bounded question: what does helper `00431840` consume and return in the frozen PlayerCreatureController rise/fall paths? All VAs refer to executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. This closes the direct helper body and one small walk wrapper; delegated getter semantics and units remain Unknown. It does not implement compatibility acceleration.

The [controller contract](CREATURE_CONTROLLER_SPEED_INPUT_CONTRACT.md), [current writer/readers](CREATURE_MOVEMENT_WRITER_CONTRACT.md), and [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R51_ACCELERATION_HELPER.md) provide the preceding evidence and reproduction boundary. No canonical rename, protocol change, or coverage rescore follows.

## Extent and ABI

**Observation / Confirmed:** `00431840..00431903` is a complete 196-byte, 70-instruction body. Its SHA-256 is `03d39d78a24d6c95f9c14bba2a41e7883521b54134380dea627ab78d63523700`. Entry saves EBP and ESI; `00431846` saves ECX in ESI. All six direct conditional branches target decoded instruction boundaries inside this body; all three exits are `ret 8`. Twelve NOP bytes separate the last exit from the following function. There are no indirect branches in the helper; delegated calls have independent unresolved behavior.

The receiver is the selected object already established in the controller contract. First stack argument `[EBP+8]` is a four-byte word used as an F32 comparison operand or forwarded unchanged to a delegated getter. Only the low byte of second argument `[EBP+C]` is initially tested. Return is x87 ST0; no EAX numeric-return interpretation is supported. The helper overwrites its own argument slots as scratch storage, restores ESI/EBP and pops eight argument bytes. The directly decoded body has no receiver/global write, allocation, retain/release or synchronization operation. **Unknown:** transitive side effects, exception behavior, pointer lifetime and thread affinity. These direct observations do not establish a pure or thread-safe API.

## Controller arguments and update separation

| Witness | Observation / Confirmed | Implementation consequence |
| --- | --- | --- |
| `0062A001..0062A009` | Load controller current F32 word `+64`, push zero then that word, set ECX=EDI, call helper | First argument is current speed; second argument is zero |
| `0062A036..0062A03E` | Same arrangement through ECX temporary, then ECX=EDI | Fall path uses the same helper inputs; it does not pass a separate deceleration selector |
| `0062A00E/0062A045` | Caller multiplies returned x87 value by its own incoming `[EBP+8]`; fall path first applies `fchs` | Incoming update argument is outside the helper and is not forwarded to it |

The containing controller's incoming argument is not the helper's `[EBP+8]`: each EBP belongs to its own stack frame. Both controller paths add current speed and apply their existing target clamp afterward. This supports a rate-like interpretation, **Strongly inferred**, but neither physical time units nor an acceleration/deceleration law is established merely by this multiplication. The later current-speed zeroing and vector sink remain as documented in the controller contract.

## Gated vehicle path

**Observation / Confirmed:** the alternate path requires every following gate to return normally and succeed, in this order:

1. `00435250(receiver, 0x1C)` returns nonzero AL. Its existing curated role is state-bit testing; the gameplay meaning of this bit is not newly assigned.
2. The low byte of second argument is zero. A nonzero low byte skips this path; upper argument bytes are not inspected by this test.
3. `00557360(receiver)` supplies EAX to `011B5A80` with literal `0x10000`; returned AL is nonzero.
4. `00648080(receiver)` returns a nonnull pointer. Its `+30` member is cast through `0131E6F1` from `Dynamics` descriptor `0186AFC4` to `VehicleHoverDynamics` descriptor `0186AFDC`; cast result is nonnull. These descriptors are already independently pinned by the frozen reader audit.

Any failed gate reaches `004318B8`. On success, `0043189C` captures receiver raw F32 `+69C` into the second argument scratch slot. `004318A2/A8` loads and pushes the original first argument word. `004318AB` calls `011AC4C0` on the cast result, then `004318B0` multiplies its x87 return by captured `+69C` and returns. There is no direct `+688` multiplier on this path. **Unknown:** lookup ownership, gate semantics beyond the observed literals, complete vehicle getter computation and exception/null recovery inside callees. No additional callee survey was performed.

## Fallback comparison, template and received fields

**Observation / Confirmed:** `004318B8/BA/BC` calls `00431920` on the same receiver with literal zero. The complete 16-byte/seven-instruction wrapper `00431920..0043192F` forwards its argument unchanged to frozen walk reader `00438950`, preserving ECX, and returns with `ret 4`. Wrapper SHA-256 is `662d0bcba876c776f85d14c55da198ca9d0a176f4e5c95d0070fb1121cc81d63`. This is the only immediate small callee followed this round. The run reader is not directly called by this helper; the frozen walk reader retains its documented alternate-object behavior.

`004318C1` compares returned walk value against the original current-speed argument. `fnstsw AX; test AH,1; jne` examines C0. On a normally returning comparison, walk less than current, or unordered, chooses `004318F2` and template argument **0**. Ordered equal or greater chooses `004318E0` and argument **1**. An unordered comparison does not select argument1. Synthetic quiet-NaN routing fixtures describe this control bit; they do not establish unmasked exception/signaling-NaN behavior in the original process.

Before either call, the helper reads template receiver from object `+10`, captures raw F32 `+69C`, and overwrites the first argument scratch slot with raw F32 `+688`. Both branches call `011ADD30(template, selected argument)`, multiply its returned x87 value first by captured `+69C`, then by captured `+688`, and return. No local F32 store narrows the multiplication result before return. This is an observed operand/store order, not a host rounding-parity claim.

The [replication contract](CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md) establishes F32 wrappers `+690` for CREO4 field0 and `+67C` for field1; frozen current-wrapper evidence establishes current member `+C`. Therefore helper `+69C/+688` are those current members, **Confirmed structural mapping**. Their acceleration base/modifier role names are **Strongly inferred** from the existing server/client correlation. The helper directly uses received/current values, not a newly inferred authored default. `011ADD30`'s complete parameter sampling/inheritance and the meaning of indices0/1 remain **Unknown** in this bounded investigation; do not rename them run/walk acceleration constants solely from the threshold.

## Verification and compatibility boundary

The [producer](../../SWG-Godot-Client/tools/python/verify_creature_acceleration_helper.py) gates executable identity before optional imports/VA access, reuses the frozen50 controller and frozen48 writer/reader verifier chain, verifies complete body digests/decoding/exits/direct destinations, and checks63 selected witnesses. It writes only a fresh ignored receipt through the frozen lexical reparse/containment guard.

The [independent runner](../../SWG-Godot-Client/poc/round51-tests/Runtime/research_checks.py) maps PE sections without pefile/Capstone or the producer anchor table, checks26 literal-byte witnesses,12 relative calls and two complete body digests. [Synthetic fixtures](../../SWG-Godot-Client/poc/round51-tests/Runtime/route-fixtures.json) contain42 authored branch cases, including each failed gate, nonzero discriminator, upper-byte-only discriminator, ordered/equal/unordered/signed-zero/negative/infinity comparisons. They verify routing, not original numeric returns. Eleven negatives cover identity, missing input, wrong digest/assertion, truncated/empty/unmapped/negative spans and existing/public output rejection. No proprietary asset values are in these fixtures.

Preserve selected-object identity, current-speed first argument, low-byte discriminator, ordered gate fallback, walk-threshold selection, current scalar provenance, multiplication order, x87 return and caller-side update/sign/clamp separation in any future compatibility specification. The current modern planar admission remains unchanged. Native acceleration is deferred while getter semantics, physical units and original effective arithmetic remain unresolved.

**Exact next probe:** bound `011ADD30`'s indexed template getter: identify parameter storage, allowed indices, loaded/parent/sample branches and return narrowing, correlating one serialized acceleration record through the accepted read-only overlay. Stop at delegated sampler/RNG/global semantics and record them Unknown. Independently justify the controller incoming update argument's units before implementing compatibility integration. Vehicle getter/related lifetime are separate follow-ups.

## Round52 indexed-getter closure

[011ADD30](CREATURE_ACCELERATION_TEMPLATE_GETTER_CONTRACT.md) now has complete bounded storage/inheritance/relative and endpoint-reader evidence. Mode3 returns the second endpoint without RNG calls in these bodies; a recursive parent result is narrowed to F32 by its child. This closes the indexed-getter probe above. Controller update units, global/parent invariants, active controls and vehicle getter remain Unknown; compatibility acceleration remains deferred.

## Round53 caller carrier

[Update-unit contract](PLAYER_CONTROLLER_UPDATE_UNITS_CONTRACT.md) distinguishes
the unchanged controller argument carrier from helper current speed. Scheduler
seconds do not yet prove player units across override00430260. No native
acceleration implementation is admitted by this static increment.


## Round56 bounded nominal prototype

[Supplied nominal speed](CREATURE_NOMINAL_SPEED_TRANSITION_CONTRACT.md) now implements the caller's finite rise/fall/store/retained-clamp graph. It supplies synthetic rates and conditionally justified seconds, invokes no original helper/getter and does not infer physical SI units or vehicle behavior. The old deferred native-acceleration statements remain applicable to recovered physical locomotion; [host integration](CREATURE_NOMINAL_SPEED_TRANSITION_HOST_CONTRACT.md) is an explicitly modern controlled trace.
