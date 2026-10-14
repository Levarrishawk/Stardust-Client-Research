# CreatureObject update carrier contract

Date: 2026-10-04. Scope: two new complete bodies, `00430260` and `0063F3B0`, on the documented executable baseline. Static analysis only.

The CreatureObject update override forwards its incoming 32-bit word and original object receiver through `0063F3B0` into **`00554C60`**. The latter body is unexamined because the two-body budget is exhausted. On the accepted ordinary scheduler route, the word still represents seconds at that boundary. **Actual PlayerCreatureController callback units remain Unknown**, including the missing connection from `00554C60` to the accepted Object/controller path and the selected controller-instance binding.

This advances the unresolved edge in the frozen [player update units contract](PLAYER_CONTROLLER_UPDATE_UNITS_CONTRACT.md); it does not close the entire route or authorize native acceleration.

## Input identity and reproducibility

Executable: `C:/Stardust-DEV/Stardust.exe`; SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses are baseline VAs. Tools: installed offline Python, pefile, Capstone x86-32; the independent runner maps PE sections and decodes selected relative-call bytes without pefile or Capstone.

Run the producer (external historical reference) with explicit `--exe EXE --packages STATIC_PACKAGES --server-root SERVER_ROOT --receipt FRESH_PRIVATE_RECEIPT`. Run the independent checks (external historical reference) with the same inputs and `--output-dir FRESH_PRIVATE_DIRECTORY`. Output destinations must be fresh, ignored private paths under the checkout. The server source input is reused by frozen prerequisites; it is correlation evidence, not proof of these carrier instructions. Neither command executes the original program.

## Complete body bounds

| Entry | Exclusive end | Bytes | Instructions | Direct jumps | Normal returns |
| --- | --- | ---: | ---: | ---: | --- |
| `00430260` | `00430D80` | 2848 | 882 | 127 | `004302EA`, `00430D7D`: `ret 4` |
| `0063F3B0` | `0063F4E5` | 309 | 96 | 8 | `0063F4E2`: `ret 4` |

Body SHA-256 values, in table order:

- `59f2babe65ab48b18824dbab63e77f2d6a1f4ce21ba487f9274902edc777dd28`
- `af01c6b4ad50658f6fbc6355fec4bdf3a0fe5af8705ff5e5f6787598e9462bc0`

Every byte in each extent decodes; every direct-jump destination is an instruction boundary in that body. The eleven NOP bytes after the second extent are excluded. No function semantics are assigned to adjacent bodies. Complete-body validation does not mean every component operation in these bodies has been interpreted.

## Evidence and interpretation

| Observation | Interpretation | Confidence | Compatibility consequence |
| --- | --- | --- | --- |
| Frozen RTTI identifies CreatureObject vtable `015DCC34`; slot `+10` contains `00430260`; constructor store at `0042F5F1` installs that vtable. Independent bytes reconfirm slot and RTTI. | This is the CreatureObject override selected by that installed table. Actual scheduler instance selection remains conditional. | Confirmed static identity | Preserve the override entry semantics when implementing this route; do not assume every object uses this table. |
| `00430282` saves ECX in EDI; `004302B3` loads `[EBP+8]` into ESI. `004302C0` pushes ESI, `004302C1` restores ECX from EDI, and `004302C3` directly calls `0063F3B0`. | Original word and original object pointer are forwarded without a local unit conversion or receiver offset. | Confirmed instructions; strongly inferred ordinary ABI carrier | Copy the update argument as an opaque F32 word at this boundary; the internal legacy register layout is not required. |
| `004302B6` can skip the side call, rejoining at `004302C0`. When present, `004302B8/B9/BB` push the same ESI word and call `00660E10` with ECX from the preceding `00431A20` result. | The optional side receiver differs; this branch does not bypass the base forwarding call. The side-call body and its semantic effects are unexamined. | Confirmed branch/call; Unknown side-call semantics | Retain the distinction between the object receiver and the optional returned receiver. Ordinary forwarding assumes normal return and ABI preservation of ESI/EDI. |
| `0063F3B6` loads `[EBP+8]` into EAX; `0063F3BA` pushes EAX; `0063F3BB` saves ECX in ESI without changing ECX; `0063F3BD` calls `00554C60`. | The second body forwards the same incoming word and receiver immediately. No local rescale, receiver adjustment, or substep loop precedes this call. | Confirmed | Seconds reach the input of `00554C60` if the accepted ordinary scheduler selected the CreatureObject route. |
| Each selected direct carrier call occurs once in its body; there is no preceding input-slot write, return, or direct jump past it, and no later direct jump re-enters its prefix. | Intraprocedural normal-return flow has no discovered gate or loop around this forwarding edge. | Confirmed bounded control flow | Do not invent carrier subdivision or a skip gate in these two prefixes. Exceptions, indirect callees, and reentrancy are outside this result. |
| `0063F3C2` stores returned ST0 into `[EBP+8]` with F32 `fst`; `0063F3C5` compares the returned value with the literal at `015EC4C4`; `0063F4DB` reloads the stored value before returning. CreatureObject compares that result at `004302C8` and can return at `004302EA`. | The reused slot now contains a narrowed returned result, not the incoming update word. Its later loads do not establish input units. | Confirmed structure; Unknown result semantics | Keep argument forwarding separate from return-value processing. Do not claim bit preservation of the x87 return across its F32 spill or close ambient floating-point control here. |
| CreatureObject writes EAX into `[EBP+8]` at `00430B0E`, `00430BC2`, and `00430C67`, all after the base call. The first two follow `00AC15C0`; the third follows `00438650`. No later direct jump reaches the carrier prefix. | Later code reuses the argument slot as scratch for callee results. It cannot change the word already passed by the listed earlier direct edge on ordinary intraprocedural flow. Full allocation/ownership semantics are unexamined. | Confirmed order and scratch writes; Unknown downstream semantics | Avoid interpreting these later stores as time rescaling or using them to infer gameplay units. |

## Reused evidence and limits

The frozen prior producer supplies the accepted ordinary QPC/QPF seconds path, scheduler tick accumulation and seconds conversion, Object `00B23100` flags and controller `+2C` gate, wrapper `00B3D1C0`, and PlayerCreatureController vtable `015E034C` slot `+14` target `00629750`. Those bodies were not newly followed. Helper-return/current-speed unit uncertainty and callback floating-point state remain as previously recorded.

The proven additional path is `00430260 -> 0063F3B0 -> input of 00554C60`. A connection from that input to `00B23100 -> 00B3D1C0 -> 00629750` has **not** been established. Do not infer the intermediate class name from analogous source. No controller instance attachment, whole-program caller census, startup FPU survey, live server, original execution, or native acceleration implementation is included.

Synthetic fixtures (external historical reference) exercise 36 opaque-word/receiver combinations and four conditional seconds representations. They include unusual F32 bit patterns solely to check the model's copying behavior; they do not show that the original client accepts those values. Eight negative checks cover input identity, missing/truncated/unmapped reads, and forbidden/existing output paths. Independent raw witnesses include 16 byte windows, three relative calls, one optional branch, RTTI/dispatch, and two complete-body digests. The initial independent RTTI length expectation was 20 bytes rather than 21 including the terminator; it was corrected before the successful fresh run.

## Exact next probe

Follow complete `00554C60` as the first body of a separately bounded investigation. Trace the incoming word and ECX into the accepted Object/controller path, including conversion/substeps, gates, and receiver binding. Follow at most one necessary additional carrier body. Stop at a precise remaining edge if that budget is reached; promote player seconds only if both carrier continuity and relevant receiver selection are established.
