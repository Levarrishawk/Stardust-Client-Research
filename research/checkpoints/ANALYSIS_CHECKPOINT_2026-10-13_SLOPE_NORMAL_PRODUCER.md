# October 13: slope normal producer and query-time cache closure

## Bounded question and input

Does `00B65730`, called at `00B63E99`, produce the state +3C descriptor consumed by `FSLP` query `00B78A90`, and what are its axes, current-height source, edge behavior, normalization and dirty timing? Static-only, twenty-minute boundary; no original execution, Core/Godot changes, registry changes or broad regression runs.

Executable SHA-256 was checked before VA interpretation: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Input path was `C:/Stardust-DEV/Stardust.exe`. PE mapping and Capstone were used through the existing private analysis packages. No new original asset output was generated. The [endpoint closure](ANALYSIS_CHECKPOINT_2026-10-13_SLOPE_ENDPOINT_GRAPH.md) was read first; it remains separate from this normal producer.

## Findings versus interpretation

The full producer `[00B65730,00B65AEA)` is 954 bytes. The earlier coordinate-only range ending at `00B657F9` excluded its normal algorithm. Direct target observations close the following facts, all Confirmed:

- State +18 is the current scalar-height descriptor shared with FHGT/AHCN. Positions at +38 are XYZ, with Y=currentHeight+originY. Normal output is exactly +3C, and FSLP reads its second/Y component.
- Caller builder `0084A530` binds +18/+38/+3C to bufferBase/+60/+6C. Context helper chain `00849040 -> 00B5CF20` carries the caller appearance's +70 buffer address. State and producer borrow descriptors; state destruction clears pointers without freeing. Ultimate allocation/destruction remains outside scope.
- For every square, two unnormalized cross products split along Q10–Q01. Upper=(Q01−Q10)×(Q11−Q01); lower=(Q10−Q01)×(Q00−Q10). Upper is added to Q01/Q11/Q10, lower to Q10/Q00/Q01, in row-major square order. Differences, cross components and additions have explicit F32 stores. Edges contain only incident in-domain triangles.
- Each accumulated normal is passed to `00AB5C40`. Magnitude is retained FSQRT of (Z²+X²)+Y². Finite magnitude below F32 word `3727C5AC` remains unchanged; equality normalizes. The reciprocal remains retained until XYZ F32 stores. Zero remains zero; there is no up-vector fallback.
- Demand/dirty rebuild precedes a layer's per-cell evaluation. Height-affector dirty changes do not trigger per-cell regeneration. A later layer demand or the final driver rebuild refreshes the cache. Slope input therefore belongs to ordered query-time height state, not final render heights/normals.

The [durable contract](../../docs/TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md) contains exact anchors, component/descriptor mapping, helper roles, arithmetic spill graph, normalization threshold, finite-grid edge policy and caller/timing scope. It includes a clearly synthetic 2×2 saddle witness to distinguish this diagonal from central differences. NGE generator code corroborates the target-derived geometry only.

## Verification and remaining boundary

Run the [static verifier](../../tools/python/verify_terrain_slope_normal_producer.py) with explicit inputs:

```powershell
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/verify_terrain_slope_normal_producer.py --exe C:/Stardust-DEV/Stardust.exe --packages local-private/toolchain/python-static-analysis-packages
```

Result: 13 body identities, 71 instruction anchors, exact epsilon/unit literals and mapped/file bytes pass. Three wrong executable identities and three separately labeled synthetic mapped-body mutations are rejected. The producer and normalizer contain no control-word instruction. Static checks do not execute the original or establish original output bits. Focused Python syntax and patch whitespace checks also pass; parent owns canonical status/session reconciliation and workspace validation.

Effective x87 precision/rounding/exception controls, exact FSQRT/F32 outcomes and endpoint FSIN outputs remain Unknown. There is no claim of original-normal parity. Ultimate descriptor lifecycle, unchecked/malformed original inputs, nonfinite behavior and complete traversal/affector coverage remain outside this closure.

## Compatibility consequence and exact next action

The structural normal prerequisite for deferred slope-shader evaluation is closed. A replacement may implement an explicitly modern numeric cache with these source triangles, finite-grid boundaries and ordered dirty timing; it must retain a separate filter cache from constrained render normals. The next bounded step is an ordered height/shader adapter admitting selected known slope branches with declared modern arithmetic, endpoint policy and original parity false. Using final-height normals as a shortcut remains unsupported. Original parity would additionally require justified effective arithmetic controls and independent numeric evidence, not NGE correlation.
