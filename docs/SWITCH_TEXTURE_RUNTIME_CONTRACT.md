# SWTS delta-time selector runtime

October 3, 2026, Round27. Static analysis of the pinned executable closes the two terminal DTST words, initial index, local update algorithm, and selector ownership. The outer scheduler's time units, pause/culling behavior, and thread affinity remain unknown. No animation implementation or original execution is part of this result.

## Identity and distinct domains

All addresses are VAs for Stardust.exe SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, verified before analysis. RTTI identifies `SwitchTextureShaderTemplate` vtable `0x015E39D4`, `SwitchTextureShader` `0x015E3B54`, `DeltaTimeSwitcherTemplate` `0x015ECBD0`, and `DeltaTimeSwitcher` `0x015ECC24`. These identities are Confirmed from the PE complete-object locator/type descriptors, not class-name proximity.

Registration `0x0077EBC0` passes SWTS (`0x53575453`) and factory `0x0077ECD0` to the shader-template registry `0x0075A890`. The shared install caller at `0x0075A4CE` calls this registration separately from SWSH registration at `0x0075A4C9`. SWTS creates a 36-byte legacy template through `0x0077ED30`; `0x0077EEC0` dispatches its outer version 0000 to `0x0077EF30`. The latter loads the base shader template, calls common switcher dispatch `0x00B5A0C0`, and prepares authored variants. Confirmed.

[Earlier SWSH research](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_SWITCH_SHADER_TEMPLATE.md) correctly distinguishes shader-side SWSH from appearance-side SWSH, but its `0x00785C30/50` forwarding addresses belong to SWTS. RTTI identifies SWSH runtime vtable `0x015E3B80`; its corresponding selected-child forwarders are `0x007860A0/C0`, with update forwarding at `0x00786080`. This is an address attribution correction, not a claim that either wrapper lacks runtime selection. Canonical reconciliation is deferred to the lead.

## DTST bytes and initialization

The [original terminal contract](SWITCH_TEXTURE_STATIC_RESOURCE_CONTRACT.md) records the 475-byte `shader/anim_mission_terminal.sht`, SHA-256 `B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62`. Its DTST/0000 payload is `08 00 00 00 CD CC CC 3D` and its eight ordered TEXT references name `_01.dds` through `_08.dds` under `texture/anim_mission_terminal`, each carrying MAIN (`0x4D41494E`). Original occurrence and order: Confirmed by independent byte parsing and pinned identity.

| Evidence | Observation | Meaning / confidence |
| --- | --- | --- |
| `0x00B5A1E2..0x00B5A228` | DTST discriminator allocates 12 bytes and calls `0x00B5A3F0` | Concrete delta-time selector template, Confirmed |
| `0x00B5A43E..0x00B5A482` | Enters DTST form and 0000 chunk; two four-byte reads; first stored at template +8, second at +4 | First word is signed comparison count; second is float interval, Confirmed by runtime use |
| `0x00B5A524..0x00B5A52E` | Passes +4 and +8 to `0x00B5ACA0` | Interval and count transferred into a new runtime selector, Confirmed |
| `0x00B5ACA0..0x00B5ACC9` | Runtime +4 receives interval, +12 count; accumulator +8 and index +16 start at zero | First index is zero, with no initial randomization, Confirmed |
| `0x00B5AD80/90` | Return +16 and +12 respectively | Current index and authored count, Confirmed |

Thus terminal count is 8 and interval is float32 `0x3DCCCCCD` = approximately `0.10000000149011612`. Calling this a 0.1-second interval or 10 Hz animation requires the outer clock-unit evidence, which this bounded trace has not established. The original reader makes two four-byte reads; it is not evidence that every original parser enforces an exact eight-byte chunk extent or modern finite/positive limits.

## Local update and selection

DeltaTimeSwitcher vtable slots 1 through 4 are `0x00B5AD10` (returns true), `0x00B5AD20` (update), `0x00B5AD80` (index), and `0x00B5AD90` (count). In update, the supplied float delta is added to the accumulator. If the sum is below the interval, index is unchanged. Otherwise the code repeatedly increments the index, wraps it to zero when it is not signed-less-than the count, subtracts one interval, and repeats until the remainder is below the interval. An exact interval boundary advances, and a large finite delta can advance multiple frames. The residual accumulator is retained; there is no single-step clamp, random frame choice, ping-pong direction, or frame rebuild in this path. Confirmed from `0x00B5AD20..0x00B5AD6E`.

The selector uses x87 arithmetic and float32 fields. Intermediate precision depends on the active control word; the reachable installer/frame-pump policy selects PC00 (24-bit significand) and RC00 (round-to-nearest), subject to any intervening override or alternate thread context. See the [Round28 clock correction](SWITCH_TEXTURE_CLOCK_CONTRACT.md). Replacing it with integer elapsed-frame division has not been verified as bit-identical. Input validation is absent from this local routine: count/list consistency, positive finite interval, finite delta, and bounded catch-up work require explicit modern policy. A nonpositive interval with a nonnegative accumulated delta can fail to make progress through subtraction; this is a static malformed-input consequence, not an executed crash or hang test.

`0x00785C30/50` query the selector index and fetch `variants[index]`, then forward virtual operations at +0x14/+0x18. No index-versus-vector bounds check appears in those functions. The authored DTST count is therefore a selector modulus, while variant count is independently determined by the remaining child records in the SWTS loader (`0x0077EF6B` onward). The examined path does not compare these two counts. Their equality in the terminal is an asset observation, not a universal rejection rule. Confirmed local control flow; complete malformed asset acceptance remains Unknown.

## Ownership and update boundary

The SWTS template owns selector template at +0x18, base shader template at +0x1C, and ordered prepared shader-variant vector at +0x20. During `0x0077EF30`, each remaining chunk is read as binding tag/name, the base template creates a shader variant, and the loaded texture is applied through its static shader interface when available. Variants are appended in order. This supports authored MAIN replacement in the terminal; it does not recover original shader equations. Confirmed structural calls and ordering; texture/material visual parity remains outside scope.

Runtime creation `0x0077EBF0/0x0077EC50` allocates a 20-byte wrapper and calls `0x00785B00`. Base constructor `0x0076D8E0` retains the parent shader template via `0x00769910`; wrapper construction creates a new selector from the template and stores its pointer at +0x0C, while borrowing the parent's variant vector at +0x10. Separate wrappers consequently have separate initially zero clocks/indices and shared template-owned variants. Confirmed.

Runtime destructor `0x00785B90` virtually deletes its selector and invokes base destruction `0x0076D970`, which releases the retained parent via `0x00769930`. Template destructor `0x0077EDE0` deletes the selector template, releases every variant with `0x0076D980`, frees vector storage, and releases the base template. The borrowing relationship is protected by parent retention; no independent timer or global selector registration appears in these constructors/destructors. Confirmed for examined paths.

RTTI-backed `LocalShaderPrimitive@ShaderPrimitiveSet` vtable `0x015E3984` points at `0x0077E2C0`, which forwards its supplied argument to the shader's +0x10 update slot. For SWTS that slot is `0x00785C10`, which forwards the same argument to selector +8, reaching `0x00B5AD20`. This closes local callback-driven scheduling. The outer primitive-set/appearance frame caller, elapsed-time units, first callback relative to first draw, pause/culling behavior, and thread affinity remain Unknown. Query-before-update selects terminal `_01.dds`; which texture is first visible on an actual live frame depends on the missing outer scheduling evidence.

## Reproduction and compatibility boundary

[Static verifier](../../SWG-Godot-Client/poc/round27-tests/Rendering/Verify-SwitchRuntime.py) accepts explicit `--exe`, pinned extracted `--shader`, existing `--packages` directory containing pefile/Capstone, and fresh ignored `--receipt`. It verifies executable identity before VA use, six RTTI identities, 17 instruction ranges and the original asset grammar/order. Obtain the shader read-only through the existing configured-overlay route or original archive extraction tools; its pin is checked independently. The accepted original Round26 private export provided the same pinned bytes for this investigation. The verifier writes only hashes/compact metadata into a fresh private receipt, never public raw bytes or decompilation. Two synthetic wrong-executable/shader inputs reject without a receipt.

A future engine-neutral selector may preserve initial zero, ordered modulo count, accumulator residual, and repeated catch-up at exact boundaries, but must explicitly define numeric safety and clock integration after the outer caller trace. Keep today's explicit pinned frame preview unchanged until that milestone. No overall protocol/function/research percentage increases follow from this one finding. See the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R27_SWITCH_TEXTURE_RUNTIME.md) for proposed canonical names and the resume point.
