# Terrain arithmetic runtime policy

This static investigation closes the native startup route for the normal terrain worker and the CRT registration of the terrain color endpoints. It does **not** establish the active worker x87 control word. [AHCN's explicit precision input](TERRAIN_HEIGHT_CONSTANT_OPERATION_CONTRACT.md) and [color's explicit configuration inputs](TERRAIN_VERTEX_COLOR_CONTRACT.md) remain required. No runtime-profile API is introduced.

Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses below are VAs. The [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R35_TERRAIN_RUNTIME_POLICY.md) defines the bounded questions and reproduction.

## Actual worker creation and generation route

| Direct observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| Terrain constructor `00849350` calls wrapper constructor `00B8D450` at `00849DE5`, writes appearance to wrapper `+14` at `00849DEA`, callback `0085EE60` to `+18` at `00849DED`, and vtable `015E66A8` at `00849DF4`. | Member-function thread binding, Confirmed. | Trace this object, rather than an unrelated thread class. |
| `00849E41` starts it through `00B8D550`; that function passes wrapper pointer and entry `00B8D600` to CRT routine `0132104A` at `00B8D563`. | Native thread startup route, Confirmed. | The new thread has a distinct arithmetic state boundary. |
| CRT stores callback/argument at its record `+48/+4C`, supplies CRT entry `013210B5`, and calls imported `CreateThread` at `0132108B` through IAT `015DC180`. | Actual OS thread creation, Confirmed. | Parent-thread foundation installation alone cannot determine new-thread state. |
| CRT entry loads pointer `018EE4AC` at `013210FF`, optionally calls it at `01321108`, then invokes saved user callback at `01321111`. Baseline pointer is `0131DB40`, whose entire body is `ret`. | The baseline CRT floating-point initialization hook does no work here, Confirmed. | Do not claim that this hook installs PC24, PC53 or PC64. Its two literal references are reads. Computed/external changes remain outside that occurrence proof. |
| `00B8D600` performs TLS/name setup, calls `00AA44E0` for per-thread foundation registration, then virtual vtable `+4` at `00B8D631`. Slot `015E66AC` is `0084BFF0`. That thunk loads owner `+14` and jumps through callback `+18`. | The wrapper reaches the bound terrain worker, Confirmed. | Thread identity is established by the binding and native entry chain. |
| `0085EF69` calls `0084A530`; `0084A79E` calls generator `00B65400`; `00B6571A` calls layer route `00B65280`; `00B65366` calls `00B63D20`; its affector call `00B6418C` invokes virtual `+10`. AHCN slot `015ED688` is `00B76540`. | Ordinary worker-to-TGEN-to-AHCN chain, Confirmed subject to enabled layer/rule and selected runtime type. | Actual arithmetic eligibility still follows the accepted layer/local gates. |
| The same chunk builder calls vertex-color/geometry builder `008618D0` at `0084A7D0`, after generation. | Generated RGB conversion is on this worker path, Confirmed. | Separate it from later render submission. Do not assume a main-thread color callback. |

A separate synchronous call to `0084A530` exists at `0084A87C`. Its caller context and active control word are not closed by this worker investigation; the worker route is not a universal thread-affinity claim for every chunk build.

Ten precisely bounded, aligned bodies are checked for absence of local `fldcw`, `fninit`, `finit`, `ldmxcsr` and direct calls to foundation install/restore/write/precision/rounding functions: wrapper start, normal CRT startup path, wrapper entry, foundation thread registration, member thunk, terrain worker, chunk builder, generator, outer layer route and AHCN. **Confirmed local absence** is narrower than a transitive no-mutation proof. Allocator/lock/import behavior and layer/filter/affector virtual callees are not exhaustively traversed. The integer helper temporarily sets truncation control and restores its prior word, as already established in the color contract.

## Foundation policy is conditional and mutable

Existing [foundation evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_CKAT_X87_EXCEPTION_MASK_POLICY.md) establishes main-thread installation at `00A9FEAB -> 00AA7B00` and frame restore at `00AA0729 -> 00AA7BD0`. Installer `00AA7B1D/22` forms `(prior & F0FF) | 003F`, then optionally unmasks configured exceptions before applying `019447B8` via `00AA7C20`. This sets PC00/round-to-nearest on the installing thread. It does not run on the inspected worker-entry chain.

Precision setter `00AA7C30` reads **previous stored mode** `019447CC` at `00AA7C33` and maps it to PC bits: prior0 ->0000, prior1 ->0200, prior2 ->0300; other values leave zero. Only afterward does it load the new argument and store it to `019447CC` at `00AA7C57`. It masks stored control word with FCFF, merges the previously selected bits, writes `019447B8`, and applies it to the calling thread. This ordering is **Confirmed**, without assigning an intended source-level setter contract.

Direct calls at `01264B99` and `01264CA9` bracket another module's work: the caller saves the prior stored enum, requests2, then requests the saved enum. For the concrete synthetic starting mode0, the first call records2 while applying PC00; the second records0 while applying PC0300. Consequently the initialized global policy cannot be treated as immutable. This is an instruction-derived state example, not observation that a terrain worker receives either value. The rounding setter `00AA7C80` can also update the stored word; this executable contains no direct-call encoding to it in the checked inventory. Global control-word mutation does not by itself load that word into another thread.

**Exact unresolved edge:** which x87 control word the OS supplies to the new `CreateThread` context, and whether any intervening imported, direct/transitive or virtual callback changes it before a particular AHCN/color operation. The baseline CRT hook is a no-op; foundation thread registration supplies no local install. Static instructions here do not identify the OS/runtime's active value. No PC/RC/exception-mask runtime default follows from this investigation, and no original process was run.

## Color endpoint and enable producers

The PE entry `0131DC7A` calls initialization routine `01324772` at `0131DD1F`, before WinMain `00401810` at `0131DD55`. That initializer calls table walker `01324896` with start `0184D000` and end `01866B90`. The walker traverses four-byte entries, skips null and invokes non-null pointers. Slots `01855ED0` and `01855EEC` are within that range and contain `00AB8600` and `00AB8750`; those wrappers jump to `00AB8610` and `00AB8760` respectively.

The first body zeros endpoint bytes `01945AA4..AA6`; the second writes FF to `01945A88..A8A`. Startup registration and initialized black/white values are now **Confirmed**, rather than merely candidate initializer bodies. All44 literal references to these six byte addresses are enumerated and decoded: the only six direct absolute writes are those initializer stores. Other sites read or pass the address as a source input. The non-terrain passed-white uses reach `005B5690` directly or through `005B7210`; they read RGB to format another output. The passed-black palette and terrain uses reach the accepted byte interpolation helper with a distinct output buffer. This closes **literal-reference write ownership**, not arbitrary computed aliases or external mutation. Stability of the initialized endpoint values on an ordinary unchanged baseline is **Strongly inferred** within that scope.

Enable getter `0084A4D0` reads `01924B80`. Setter `0084A4C0` copies its stack byte there at `0084A4C6`. This byte lies beyond `.data` raw size inside its virtual extent and is initially zero in the mapped PE. No executable `E8` direct-call encoding, mapped-image literal address reference or PE export targets `0084A4C0`. The only two literal references to the enable byte are setter and getter. **Confirmed:** initial zero and no discovered static setter producer by these mechanisms. **Strongly inferred:** the modifier is dormant in ordinary baseline use absent an undiscovered computed/external mutation. **Unknown:** general pointer-alias/debug/external mutation reachability. This is not a proof that the executable can never enable the branch.

The existing explicit caller configuration remains appropriate. An integration may model the observed initialized endpoints and disabled baseline as a conditional profile, but should label the unresolved mutation and actual callback-state boundary; no automatic default is added in this milestone.

## Reproduction and verification

Runner (external historical reference) uses explicit paths:

```powershell
.\poc\round35-tests\Rendering\Invoke-RuntimePolicy.ps1 -Exe C:\Stardust-DEV\Stardust.exe -Packages <cached-pefile-capstone-directory> -Python <python.exe>
```

Verifier and oracle (external historical reference) hash-gate before PE decoding, then check70 instruction anchors, five data pointers,50 literal-reference occurrences across11 keys,44 endpoint instruction sites/six writes, ten passed-pointer/callee anchors, five direct-call target inventories and ten local bodies. An independent bit oracle covers all65,536 prior words times64 exception-unmask combinations (4,194,304 cases). A bounded decoded integer interpreter agrees with a separate precision-setter model for125 synthetic states. It interprets only the recovered setter's integer state transitions; it is not dynamic execution of the original executable and does not emulate OS thread state.

Identity tests (external historical reference) reject a modified executable and a one-byte truncation before importing the unavailable decoder packages or creating a receipt. All checks pass. Generated receipts and proprietary-derived inputs remain ignored. There is no GPU work, new corpus sweep or software-profile implementation.

Next: establish the OS-specific new-thread control state with independently authorized evidence, then audit the bounded transitive/virtual generation route for control changes. Separately, investigate computed aliases to the endpoint/enable storage only if a concrete reachable producer is found. Keep the accepted supplied policies until those edges close.
