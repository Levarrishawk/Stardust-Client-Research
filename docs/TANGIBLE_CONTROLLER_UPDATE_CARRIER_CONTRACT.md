# Update carrier closure at 00554C60

Date: 2026-10-04. Static-only scope: one new complete body `00554C60`, within a maximum budget of two. The filename is organizational; direct RTTI evidence references this entry from **ClientObject** vtable slot `+10`, not an assumed TangibleObject semantic label.

`00554C60` forwards its incoming word and unchanged ECX directly to accepted Object update `00B23100`. This closes the previously missing carrier edge. Combined with the accepted scheduler and CreatureObject carriers, the callback word at `00629750` represents **seconds under the explicit conditions below**. There is no remaining unknown unit-conversion body on that selected static path. An unconditional claim about all runtime player callbacks, instance/type stability, or live occurrence is not established.

## Conditions for the seconds conclusion

1. The accepted ordinary scheduler route selects the relevant CreatureObject receiver and dispatches its installed slot `+10` override.
2. Preceding callees return normally and preserve the ordinary nonvolatile-register ABI. Exception, reentrancy, and invalidated receiver paths are outside this conclusion.
3. At Object update's flag checks, object `+0C` bit `04` is clear and bit `02` is set.
4. When Object reads object `+2C`, the current controller is non-null. It need not equal a cached constructor return.
5. The held controller remains valid through queue update and, at wrapper dispatch, its current table is PlayerCreatureController `015E034C`, slot `+14` target `00629750`.
6. The queue update returns normally. No unproved guarantee that setup-installed pointer or dynamic type persists is assumed.

These are static route/gate/receiver conditions, not observed runtime events. The selected successful scene setup supplies independent evidence that a PlayerCreatureController is installed on the relevant object; current lookup and dispatch determine whether that installation still applies at an update.

## Identity and reproduction

Executable: `C:/Stardust-DEV/Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All addresses are baseline VAs. Installed offline Python/pefile/Capstone x86-32 perform static validation. No toolchain download, original execution, live contact, capture, fuzzing, or external mutation is used.

Producer (external historical reference): `--exe EXE --packages STATIC_PACKAGES --server-root SERVER_ROOT --receipt FRESH_PRIVATE_RECEIPT`. Independent runner (external historical reference): same explicit inputs with `--output-dir FRESH_PRIVATE_DIRECTORY`. Receipts must use fresh ignored private destinations under the checkout. SHA validation precedes VA interpretation. The frozen prerequisite's server input is correlation evidence only; the carrier claims derive from executable bytes.

## New body and direct evidence

Complete extent: `00554C60..00554CE1`, exclusive end `00554CE2`; **130 bytes, 54 instructions, five direct jumps**. Returns at `00554C8C` and `00554CDF` are both `ret 4`. The fourteen following NOP bytes are excluded. SHA-256: `3a248d6f027f1e0b90b17575b22e150ba129313716b24a549c14ad0fac0e6ad0`. Every byte decodes and every direct-jump target is an instruction boundary in the extent. This does not label every callee or establish complete component semantics.

| Observation | Interpretation and confidence | Compatibility consequence |
| --- | --- | --- |
| `00554C65` loads `[EBP+8]` into EBX; `00554C69` pushes EBX; `00554C6A` saves ECX in ESI without changing ECX; `00554C6C` directly calls `00B23100`. No branch or return precedes that call. | Confirmed raw-word copy and unchanged object receiver. No local scale, temporal subdivision, or gate precedes the Object call. | Preserve the incoming F32 word at this boundary. Legacy registers/layouts need not be copied into a replacement. |
| The Object call appears exactly once. All five direct branches are after it and none targets its prefix. | Confirmed bounded intraprocedural control flow; no local bypass/reentry of the parent forwarding edge. | Do not interpret the later loop as subdividing that earlier parent update. Indirect callees and reentrant execution remain unexamined. |
| Vtable `015DE26C +10` contains `00554C60`; its COL is `0160E0A4`, TypeDescriptor `0186AC18`, RTTI `.?AVClientObject@@`. | Confirmed direct class/table association. The entry is not claimed to be exclusive to that class or the file's organizational name. | Use verified identity instead of naming the carrier from analogous source. No canonical function rename is made. |
| `00554C71` stores returned ST0 into the former argument slot using F32 `fst`; `00554C74` compares the returned value with literal `015EC4C4`; `00554CC3/C7` pass that slot's address to `00B35110`; `00554CD6` reloads its later value. | Confirmed result-slot reuse. The downstream helper's semantics and return aggregation are Unknown. Its pointer could modify the stored result; it is not the earlier input argument passed to Object. | Separate incoming time from return/result processing. Do not claim x87 return bit preservation or recovered scheduling-result policy. |
| After the parent result gate, `00554C97` reads object `+284`, takes start/end pointers from offsets `0/4`, iterates 4-byte elements, and skips null elements. At `00554CB8/B9`, each non-null receiver gets the original EBX word at virtual `+10`. `00554CAA` is `lea ebx,[ebx]`; no other non-save EBX write occurs after the input load. | Confirmed per-element dispatch with unchanged word. Semantic ownership/class of the list, `005558E0`, and result helper `00B35110` remain Unknown. | Preserve the distinction between separate receiver updates and time substeps. Do not infer ownership/destruction or unit conversion from this loop. |
| Accepted Object `00B23100` saves its incoming word and reads current object `+2C` before calling wrapper `00B3D1C0`. The wrapper reloads the same stack word and dispatches held controller table slot `+14` after queue update `00AA6670`. | Confirmed reused current-receiver expression and raw forwarding; strongly inferred ordinary ABI seconds consequence. | The callback receiver is the current controller, not the original object pointer or a permanently cached setup allocation. Type/validity at dispatch is an explicit condition. |
| Accepted setup `00518EB0` constructs `00629400` for the supplied object at `00519238`; constructor installs `015E034C` at `006295B8`. Setup passes its returned pointer to setter `00B22BB0` at `00519252` and stores the object in scene `+1C` at `00519259`; setter stores into object `+2C` at `00B22BCB`. | Confirmed successful installation branch, reused from accepted Networking evidence. Intervening helper side effects and allocation-failure behavior remain unclosed. | There is independent evidence of relevant player attachment; it does not prove pointer/type stability until every future callback. |
| Accepted binder independently reads scene `+1C`, then current object `+2C` at `0051FAF9/FF`, before obtaining that controller's queue. | Confirmed current-instance lookup, independently consistent with Object update's current lookup. | A replaced/cleared controller must be treated according to the current state. Shared input queue identity is not a guarantee of object lifetime or dynamic type. |

## Reused evidence and validation

The accepted [CreatureObject carrier](CREATURE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md) and [Object/controller units contract](PLAYER_CONTROLLER_UPDATE_UNITS_CONTRACT.md) provide the ordinary scheduler seconds and unchanged word chain. The accepted [scene installation and queue assignment](GROUND_SCENE_INPUT_MAP_QUEUE_ASSIGNMENT_CONTRACT.md) supplies receiver attachment evidence. Frozen proofs are reused; no broad caller census or startup FPU survey is repeated. Four accepted setup/constructor/setter/binder body identities are hash-checked without new semantic follows or another caller scan.

The new producer verifies 23 carrier anchors, 13 reused attachment anchors, the complete 130-byte body, return forms, branch boundaries, call uniqueness, EBX writes, and two RTTI/dispatch chains. Independent raw PE mapping verifies 25 literal byte windows, eight relative calls, five short-branch destinations, five new/reused body digests, and two RTTI/dispatch chains.

Opaque fixtures (external historical reference) are synthetic. Their 288 word/object/flag/current-controller combinations yield 36 player-dispatch cases, including a replacement player and suppression after clearing or replacement by another type. Four conditional seconds representations, twelve verifier negatives, and three output-guard cleanup assertions pass. These examples test the copying/gate model; unusual F32 patterns do not establish original-client acceptance. They execute no recovered instructions. Producer/independent fresh receipts agree.

## Remaining uncertainty and next probe

No native acceleration or movement implementation is added. Current-speed/helper-return units, effective template state, callback arithmetic/exception controls, queue/controller lifetime, error recovery, and original runtime occurrence remain separately Unknown. This result closes update-argument units only under the enumerated conditions; it neither combines coverage denominators nor claims global runtime type stability.

Next precise receiver probe: inspect one selected `00AA6670` queue-update path for callbacks that invalidate or change the held controller before `00B3D1D6`, with a separate bounded body budget and explicit normal/error-return handling. Keep the conditional validity/type premise until that evidence exists. Acceleration implementation also requires independently closing helper-return/current-speed units and the arithmetic policy at `0062A00E/0062A045`; seconds alone is insufficient.
