# FSLP clamp startup initialization

The FSLP clamp word `01947228` has a registered startup initializer. On the normal returning startup path, it runs before ordinary application entry at `00401810`. Its write is the exact binary32 half of literal `40490FDB`: `3FC90FDB`. This closes the missing registration and application-entry ordering chain in the [raw slope contract](TERRAIN_FILTER_SLOPE_CONTRACT.md), while leaving x87 `FSIN` cache results and active controls Unknown. No production influence API is added.

All VAs use executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The [static verifier](../tools/python/verify_terrain_slope_initialization.py) hashes source bytes before PE parsing, imports or VA interpretation. The [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R44_SLOPE_INITIALIZATION.md) records reproduction and limits.

## Observations, interpretation and consequence

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| PE entry is `0131DC7A`; startup calls `01324772` at `0131DD1F`, then ordinary application entry `00401810` at `0131DD55`. | Startup call ordering, Confirmed on its returning path. | Successful ordinary application entry follows the initializer walk. Early termination or exceptions do not establish that application entry occurred. |
| `01324772` invokes a nonzero callback from `018EE4A8`, then walks `01866B94..01866BA8`, then `0184D000..01866B90` through `01324896`. | Two ordered initializer ranges, Confirmed. CRT/static-constructor role Strongly inferred from code and position. | Do not infer scheduling merely from adjacency or a table pointer. |
| `01324896` compares current/end unsigned, skips zero slots, calls each nonzero word, advances four bytes and repeats until current >= end. | Ascending half-open U32 function-pointer walk, Confirmed. | Slot `018560AC` is aligned, inside the second range, and visited if preceding callbacks return normally and preserve the table. |
| Second range contains 26,340 words, 26,339 nonzero. Zero-based slot 9,259 at `018560AC` is `00B77660`; preceding/following slots are `00B77640` and `00B77690`. | Actual initializer registration and local order, Confirmed. | A literal search for `00B77670` alone misses the registered thunk. |
| `00B77660` jumps directly to `00B77670`; the helper loads F32 `40490FDB` at `015ED6D8`, multiplies F32 `3F000000` at `015DC5E4`, stores F32 at `01947228`, and returns. | Scalar initialization, Confirmed. Exact finite product `3FC90FDB` follows independently from exponent adjustment. | Mapped-file zero is a pre-initialization image value, not the successful post-helper bound. Multiplying this normal number by one half loses no significand bits; it does not require a sine approximation. |
| Neighbor initializer `00B77640 -> 00B77650` stores twice the same literal at `0194722C`; `00B77690 -> 00B776A0` divides one by that neighboring word and stores `01947230`. | Ordered neighboring scalar initialization, Confirmed; broader semantic roles Unknown. | Establish local ordering without assigning units from constants. |
| The full executable-section linear scan finds eleven direct clamp references: one `FSTP` write and ten setter reads. Its only direct CALL/JMP to either slope thunk/helper is `00B77660 -> 00B77670`. Whole-section literal search finds one thunk pointer, at `018560AC`, and no helper pointer. | Reproducible bounded direct-reference inventory, Confirmed. | This is not a proof against indirect writes, synthesized addresses or dynamically modified control flow. Linear decoding is a discovery cross-check; selected positive instructions are independently anchored. |
| Constructor `00B789E0` installs the FSLP vtable and calls both setters at `00B78A26/2E`; their six `FSIN` sites remain the accepted raw contract. | Constructor consumes the live scalar through setters, Confirmed. | Construction after ordinary application entry sees the initialized bound if nothing subsequently changes it. Universal ordering for constructors invoked by earlier initializer callbacks is not proved. |
| Helper has no calls, allocation or cleanup registration and returns immediately after the store. Destructor `00B78A50 -> 00B78A80 -> 00B63410` is unchanged. Exit paths use separate table ranges `01866BAC..01866BB4` and `01866BB8..01866BC0`. | Scalar helper owns no resources in its observed body; cleanup range separation Confirmed. | No per-FSLP clamp reset or resource teardown is introduced. Indirect global reset/exceptional process teardown remains unproved. |

## Precisely bounded ordering

The proven chain is:

```text
0131DC7A startup
  0131DD1F -> 01324772
    01324796 -> 01324896(0184D000, 01866B90)
      [018560AC] -> 00B77660 -> 00B77670
        F32 store 01947228 <- exact half of 40490FDB
    return
  0131DD55 -> 00401810 ordinary application entry
```

The table consumer is executable code, not a server or external-source analogy. Its null-slot and end checks, helper return, neighboring initializer ordering and startup/application call sequence are checked explicitly. The PE TLS directory is absent; this fact does not rule out imported-library initialization or callback side effects.

The callback hook before the table is mapped to `0131DB29`, which calls `0131DB41`, `01324323` and `013242D3`, then executes `FNCLEX`. Neither that single instruction nor startup order establishes the later active FPU control word. Earlier initializer callbacks were not exhaustively transitively analyzed. Therefore **before every possible FSLP constructor** remains Unknown. The confirmed result applies to the ordinary returning application-start path and subsequent construction, conditional on absence of later writes/table mutation.

## Verification and next question

The research-only [runner](../../SWG-Godot-Client/poc/round44-tests/Assets/verify.py) uses the production static verifier without launching the original image. Positive checks cover selected startup/table/helper/neighbor/destructor/exit anchors, table extent/count/unique slot, constants and direct references. Wrong-identity negatives are rejected before decoding; synthetic mapped-image anchor/table/constant mutations are separately labeled. Executed checks pass 62 selected instruction anchors, eight wrong-identity negatives and 66 synthetic structure negatives. The linear cross-check covers 7,228,061 decoded instructions and 263 skipped data records; none is executed. Full raw disassembly and detailed inventories remain private. No original asset is reread or evaluated this round; the accepted single FSLP raw occurrence remains corpus evidence in the prior contract.

Next bounded probe: characterize the effective control state and a defensible x87 `FSIN`/F32 result contract for the setter subtraction-and-cache sites `00B78F9F` and `00B7900F`. Investigate control writes and conditional numerical behavior before any slope influence implementation. Original execution, if required, needs separate explicit bounded authorization. Normal production, axes/units and layer adaptation remain independent gaps.
