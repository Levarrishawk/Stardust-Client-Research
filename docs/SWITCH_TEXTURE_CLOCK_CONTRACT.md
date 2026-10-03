# Switch-texture clock and update order

Round 28, 2026-10-03. This supplements the [runtime contract](SWITCH_TEXTURE_RUNTIME_CONTRACT.md). The bounded question is the origin and unit of the float reaching `LocalShaderPrimitive` at `0077E2C0`, its relation to drawing, and the reachable x87 policy. All addresses are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Evidence is static; no original executable was run.

## Clock and scheduler boundary

**Confirmed:** the ordinary GroundScene route delivers elapsed **seconds**, quantized and accumulated through the object scheduler. It does not deliver the raw frame delta directly to every shader.

| Boundary | Executable observation | Consequence |
| --- | --- | --- |
| Clock initialization `00AA4B80` | Calls imported `QueryPerformanceFrequency` through `015DC244`; converts its result to float32 at `019446D8/019446D4`. | Counter frequency supplies the time denominator. |
| Clock update `00AA4DE0` | Calls imported `QueryPerformanceCounter` through `015DC240`; subtracts the previous counter and divides by `019446D4` at `00AA4E24`. | The quotient is seconds because frequency is counts per second. |
| Published frame time | `00AA4E6D` publishes float32 to `01944720`; the following branch can cap it to `0194471C`. If `00AA0120` returns nonzero, publication is skipped; the previous published value remains. | Frame time can be capped or retained. This branch does not establish zero-delta pause behavior. |
| Exported getter `00AA4EC0` | PE export `?frameTime@Clock@@SAMXZ` loads `01944720`. Frame tick calls it at `0042383C`, stores float32 locally, reloads EBX at `00423922`. | The outer frame reads the published clock value. |
| Event delivery | Frame tick calls `00AB5310` at `00423979`. It enqueues type 4 through `00AB5670`; allocator `00AB5620` places the fourth argument at event `+10`. IoWin dispatch invokes slot `+4` at `00AB5495`. GroundScene `0051AF10` reads event `+10` at `0051AF80` and passes that float to `005616D0` at `0051B25D`. | This is a conditional reachable event route, not an unconditional callback for every outer frame. |
| Scene scheduler `005616D0` | Byte `019136BC` selects `00B401C0` or `00B401E0`; both call `00B410D0`. | Both examined branches advance the scheduler with the supplied frame value. The byte's semantic name remains unassigned. |
| Tick advance `00B3FE60` | Multiplies frame float by constant `015EC614 = 1000.0`; helper `0131E27C` temporarily selects RC `11` for `FISTP`, then restores the prior word. Adds the resulting integer to 64-bit tick `01947010/14`. If no change results, `00B3FED4` adds one tick after a diagnostic. | Conversion truncates toward zero. Ordinary nonnegative elapsed time loses sub-tick fractions per advance; zero/sub-millisecond input still advances one tick. No fractional carry is observed here. |
| Per-object dispatch `00B40ED0` | Subtracts the object's last tick (`00B24700`) from the global tick, converts the unsigned 64-bit difference, multiplies by `0194702C`, stores float32, and calls object slot `+10` at `00B40F6E`. Equal last/current ticks skip this call. Initialization `00B3F6C0` sets the scale to float32 `1.0/1000.0`. | Callback delta is accumulated scheduler seconds since that object's previous update, with x87 arithmetic and float32 storage. It may span multiple frames. |

The API unit interpretation comes from [Microsoft's frequency definition](https://learn.microsoft.com/en-us/windows/win32/api/profileapi/nf-profileapi-queryperformancefrequency). That documentation establishes API semantics; the client imports and arithmetic establish its use here. Invalid, negative, NaN, infinite, overflow, and wraparound timing behavior is not closed by the ordinary-time conclusion.

## Appearance-to-selector route

RTTI/vtable slots corroborate the following call chain:

`Object 00B23100 (+10)` → appearance slot `+20` → `MeshAppearance 007933C0` → `ShaderPrimitiveSet 0077E7E0` → primitive slot `+8` → `LocalShaderPrimitive 0077E2C0` → shader slot `+10` → `SwitchTextureShader 00785C10` → selector slot `+8` → `DeltaTimeSwitcher 00B5AD20`.

**Confirmed observations:** Object checks byte `+0C` bits `04` and `02` before the appearance call at `00B23144`. Mesh forwards the incoming float to its set at `007933D5` before later transparency/fade work. The set loops its primitive pointers and preserves the original argument in EBX; return scheduling values are collected separately. The remaining SWTS wrappers forward the float without an observed unit conversion.

DetailAppearance `00787CB0` first alters its base, then iterates every non-null loaded child in the array beginning `+58`, with count `+54`, forwarding the original argument through child slot `+20` at `00787CDF`. This initial child loop has no selected-LOD or camera test. It supports updates of loaded, unselected LOD children when the parent is altered. It does not establish that every culled or unscheduled object is altered.

## Drawing, first visibility, and thread limits

**Confirmed ordering:** frame tick pumps the platform at `004237DC`, reads clock time at `0042383C`, dispatches IoWin events at `00423979`, and later calls IoWin rendering at `00423AA6`. For an existing object whose update route runs in that event phase, its SWTS update precedes the later render phase. The [frame architecture](GAME_LOOP_ARCHITECTURE.md) documents the surrounding begin/end/present guards.

**Confirmed construction state:** Round 27 established selector index zero and accumulator zero. A query made before any update therefore selects variant zero. **Unknown:** the index of the first visible frame. Resource creation, registration, due scheduling, event propagation, and first submission may place an update before visibility. Do not promise that variant zero is necessarily drawn first.

No asynchronous transfer appears in the examined synchronous route; this is evidence for a frame/event-thread path. It is not a proof that all alternate SWTS owners or updates execute on that thread. IoWin propagation, object flags, due queues, and clock publication gates prevent a universal claim that animation runs once per frame, stops when culled, or receives zero while paused.

## Floating-point policy and Round 27 correction

**Confirmed instructions:** OS installation `00A9FE20` calls `00AA7B00` at `00A9FEAB`. That routine reads the control word, applies `AND EAX,F0FF` at `00AA7B1D`, then `OR EAX,003F`, stores the configured word at `019447B8`, applies optional low-byte exception-mask changes, and loads it through `00AA7C20`. The mask clears PC bits 8–9 and RC bits 10–11 to `00`. `00AA7BD0` compares and conditionally restores this stored word; the platform frame pump calls it at `00AA0729`, before the clock/event route above.

**Architecture interpretation, Confirmed:** PC `00` selects a 24-bit significand; RC `00` selects round-to-nearest. See [Intel's control-word constants](https://www.intel.com/content/www/us/en/docs/fortran-compiler/developer-guide-reference/2023-2/getcontrolfpqq.html). This concerns arithmetic precision, not a claim that x87 registers have only 32-bit storage or that every operation has binary32 exponent behavior.

**Strongly inferred scope:** this configured policy is reachable on the normal frame-thread route. The actual control word at an SWTS callback has not been observed, and intervening virtual callbacks or other thread contexts have not been exhaustively audited. The integer conversion helper restores its prior word. Do not infer unconditional extended precision from the use of x87 registers.

Proposed bounded correction to Round 27's sentence about extended intermediate values: “The selector uses x87 arithmetic and float32 fields. Intermediate precision depends on the active control word; the reachable installer/frame-pump policy selects PC `00` (24-bit significand) and RC `00` (round-to-nearest), subject to any intervening override or alternate thread context.” Round 27 files are unchanged in this worker's scope; canonical reconciliation belongs to the lead.

Replacement implementations should preserve seconds, scheduler accumulation/quantization where required, callback-before-render ordering, and per-instance selector state. A modern double-intermediate/float32-storage cursor must remain explicitly identified as a chosen policy. In that policy, the final float32 residual can round to the interval even when the double temporary is below it; do not add a clamp/reset or assert a persistent strict residual bound without separate evidence.

## Reproduction and remaining probe

The [static verifier](../poc/round28-tests/Rendering/Verify-SwitchClock.py) checks input SHA before VAs, the Clock export/imports, seven RTTI slots, tick constants, 26 instruction ranges, and frame call-site order. It writes range hashes only to an explicitly supplied private receipt. It neither runs the client nor proves runtime reachability of every branch.

Next bounded probe: trace one newly created SWTS mesh object's constructor → appearance assignment → world registration/initial last-tick assignment → first due update → first render enqueue. Stop when first-visible ordering is established or the exact deferred-load/event gate remains identified. Separately audit control-word-changing callbacks only if exact numerical parity is required. Full pause/culling policy and alternate appearance families remain open.
