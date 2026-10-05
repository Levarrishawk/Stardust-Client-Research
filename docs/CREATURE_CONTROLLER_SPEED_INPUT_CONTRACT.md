# PlayerCreatureController speed-input contract

Date: 2026-10-04. Scope: receiver selection and direct consumption of the run/walk returns at `00629F7E` and `00629F8A`. All addresses are VAs in the executable with SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

This extends the [movement writer/reader contract](CREATURE_MOVEMENT_WRITER_CONTRACT.md). The [round 50 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R50_CONTROLLER_SPEED_INPUTS.md) records reproducibility and remaining probes. No canonical function name or readiness percentage changes here.

## Identity and receiver

**Confirmed observation:** the containing function is `00629750..0062A62F`, 3,808 bytes and 1,163 decoded instructions. Vtable `015E034C` has `00629750` at slot `+14`; its preceding COL pointer is `016113A0`, whose TypeDescriptor pointer is `0186F484`, with name `.?AVPlayerCreatureController@@`. Constructor `00629400` installs this vtable at `006295B8`. This supports the proposed role “PlayerCreatureController input update and speed consumption”; it does not authorize a registry rename.

The function saves controller `this` in `EBX` and reads `[EBX+8]` into local `[EBP-20]`. This pointer is called the owner below as an interpretation of the controller relationship, not a recovered source member name. A null pointer follows the base-update fallback at `0062976C`.

At `00629880`, the owner is passed to `00438620`, a byte accessor for receiver `+98D`. If that result is zero, `EDI` is assigned the owner at `00629898`. Otherwise `00438650(owner)` supplies `EDI` at `00629894`. A null selected pointer follows another base-update fallback at `006298A8`. The selected pointer is saved at `[EBP-3C]`, and restored after a matrix-copy temporary at `0062A1D6`.

**Confirmed consequence:** both speed calls receive the same selected object in `ECX=EDI`. They are not proven to always receive the original owner. **Unknown:** the byte predicate's gameplay meaning, the related object's relationship, and the lifetime guarantee of that returned pointer. The selected reads add no observed retain/release operation; this does not establish ownership across the whole controller lifecycle.

## Returns, selection, and target

| Witness | Confirmed observation | Compatibility consequence |
| --- | --- | --- |
| `00629F7A..00629F83` | Push zero, call run reader `004388A0`, spill return to F32 local `-14` | Preserve the actual reader argument and return narrowing |
| `00629F86..00629F8F` | Push zero, call walk reader `00438950`, spill return to F32 local `-34` | Same receiver and zero argument; zero does not force an owner-only reader path |
| `00629FAE..00629FCD` | Test local byte `-1B`; compare run against F32 zero; select run only with nonzero flag and ordered run greater than zero, otherwise walk | Keep this selection separate from run/walk reader computation |
| `00629FCD..00629FE7` | Compute the magnitude of three local vector components; multiply selected speed; `fst` F32 controller `+154` | This is a distinct magnitude-scaled intermediate |
| `00629FED..00629FF2` | Multiply the retained x87 intermediate by a preceding factor; spill F32 target to local `-14` | Do not assume the stored `+154` F32 value is reloaded before the factor multiplication |

Local flag `-1B` is populated from global byte `01882394` at `00629A12/00629A2D`; a later input branch uses `sete` after testing the same global at `00629B84/00629B8B`. Its gameplay name remains **Unknown**. The run comparison's `fnstsw`/`test AH,41` routes unordered comparison to walk; no signaling-NaN or exception-mode claim follows.

The preceding factor is F32 one from `015DC6F8` when controller byte accessor `00639EA0` (`this+55`) is true; otherwise it is the return of `00628DF0(selected object)`. The factor's physical meaning and full callee semantics remain **Unknown**. The zero and one literal bytes are verified independently of any gameplay interpretation.

## Current speed and immediate consumer

**Confirmed:** target is compared against controller F32 `+64`. For ordered target greater than current, `0062A009` calls `00431840(selected object, current, zero)`, multiplies its return by incoming argument `[EBP+8]`, and adds current. The F32 store at `0062A014` retains the x87 value for the following comparison; overshoot copies the raw F32 target into `+64` at `0062A024`.

For ordered target below current, `0062A03E` calls the same helper with the same argument arrangement, negates the return, multiplies by the incoming argument, and adds current. The corresponding store and clamp are at `0062A04B` and `0062A05B` (the latter copies through `EDX`). Equality/unordered branches bypass these ordered rise/fall paths. These are direct instruction observations, not a complete recovered acceleration model; the incoming argument's units and `00431840`'s alternate-object behavior remain **Unknown**.

A later conditional branch can set controller `+64` to zero at `0062A367`. Therefore the smoothing result is not unconditionally the final vector magnitude. Orientation and other input branches in this function are outside this bounded contract.

At `0062A36E`, selected object `+28` supplies a pointer; if non-null, current speed `+64` multiplies three global F32 components at `019459A8/AC/B0`. The three products are spilled to a local vector, then `007CA1F0` is called at `0062A39F`. Its complete 32-byte body copies the vector's three words into the destination pointer read from sink receiver `+CC`. This establishes the immediate vector write, without assigning an unproven appearance/dynamics type to either pointer.

The controller then calls the already named `creature_controller_alter_and_apply_root_motion_VA_00638810` at `0062A3AA`, with the incoming argument, and discards its return. This connects the write to the outer update boundary; it does not close all later movement, root-motion, collision, or reconciliation behavior.

## Verification and open boundary

The [static verifier](../tools/python/verify_creature_controller_speed_inputs.py) SHA-gates the executable, reuses frozen movement evidence, verifies selected instruction witnesses, full-body digest/decoding, direct branch destinations, RTTI/vtable pointers, literal constants, and the complete vector sink. Direct branch checks do not exhaust indirect jump-table reachability.

The [independent runner](../poc/round50-tests/Networking/research_checks.py) maps PE sections without the producer's decoder, checks literal instruction bytes and relative calls, and rejects wrong/missing executables, a deliberately wrong assertion, truncated helper spans, unmapped spans, existing outputs, and public outputs. Helper truncation cases are synthetic, not execution of a modified original executable. Both launchers require fresh private output and inspect lexical symlink/reparse ancestors before resolving or creating output.

Next bounded probe: resolve the meaning and permitted transitions of global `01882394` and its input branch, using direct executable callers/strings and a concrete stopping condition. Keep related-object lookup, factor units, helper semantics, and vector destination type separately unresolved; do not promote this evidence to full locomotion fidelity or runtime arbitration readiness.


## Round56 supplied transition

[Conditional callback seconds](TANGIBLE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md) closes the prior carrier gap under explicit runtime-selection premises. [Nominal transition](CREATURE_NOMINAL_SPEED_TRANSITION_CONTRACT.md) and [host trace](CREATURE_NOMINAL_SPEED_TRANSITION_HOST_CONTRACT.md) implement the immediate supplied-rate store/clamp graph; full target construction, dynamic getter/physical units, later zeroing/vector/root-motion and original controls remain separate. The historical Unknown unit statement above describes the earlier stopping point.


## Round57 supplied target construction

[Target builder](CREATURE_NOMINAL_TARGET_SPEED_CONTRACT.md) implements the selected finite sum/sqrt/intermediate-store/retained-factor graph using supplied getter-return words and factor. [Host trace](CREATURE_NOMINAL_TARGET_SPEED_HOST_CONTRACT.md) joins actual targets to the supplied nominal transition. Getter/factor ownership, physical semantics, original controls and complete locomotion remain separate.
