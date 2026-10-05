# Player controller update argument: caller and remaining unit edge

Date: 2026-10-04. Question: what supplies the incoming argument consumed by `00629750`, and can its units be established on one bounded scheduling chain? All VAs use executable SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

**Result:** the immediate controller caller forwards its incoming F32 word unchanged. The established ordinary scheduler produces accumulated seconds at object dispatch. A CreatureObject-specific override lies between that dispatch and the newly closed common object/controller forwarding path. Therefore actual PlayerCreatureController units across that override remain **Unknown**; seconds are a conditional implication, not an unconditional recovered player-unit claim.

This is the permitted stopping condition: publish a precise remaining carrier edge after two necessary new callees. The [controller consumption contract](CREATURE_CONTROLLER_SPEED_INPUT_CONTRACT.md), [acceleration helper](CREATURE_ACCELERATION_HELPER_CONTRACT.md), [indexed getter](CREATURE_ACCELERATION_TEMPLATE_GETTER_CONTRACT.md), [prior clock evidence](SWITCH_TEXTURE_CLOCK_CONTRACT.md) and [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R53_CONTROLLER_UPDATE_UNITS.md) constrain this result. No acceleration implementation, registry rename or unit-derived speed/distance model is added.

## Immediate controller invocation

**Observation / Confirmed:** `00B3D1C0..00B3D1DD` is30 bytes/14 instructions, SHA256 `3ed9930225632212619fd5a20ef7ec4be3d584d346f99f9c4d3dc9c5cb685f37`. It saves receiver ECX in ESI, passes receiver `+4` to `00AA6670` at `00B3D1C9`, then reloads its original stack argument word at `00B3D1CE`. It reads the receiver's vtable, pushes that word, restores ECX=ESI and invokes virtual slot `+14` at `00B3D1D6`. Exit is `ret 4`; two NOPs precede the next function. The direct body performs no time multiplication/division or incoming argument rewrite.

PlayerCreatureController vtable `015E034C` contains `00629750` at `+14`; its COL/TypeDescriptor/name remain exactly the frozen controller evidence. Thus when that vtable is installed on the caller's receiver, this virtual invocation enters the proven player update function with the same raw word. No whole-program direct/indirect caller census was performed. The preliminary `00AA6670` implementation, alternate vtables, current owner/controller attachment and transitive side effects/lifetime are not exhaustively closed here.

The player interprets that word as F32 at `0062A00E/0062A045`, multiplying the acceleration helper's return. It is **not** the helper's first argument: `00431840` separately receives current speed and zero. EBP belongs to a different frame in each function. Rise/fall sign, ordered comparisons, F32 stores, target clamps, later zeroing and immediate vector consumer remain as in the existing controller contract.

## Common object forwarding

**Observation / Confirmed:** `Object::alter` target `00B23100..00B2328D` is398 bytes/152 instructions, SHA256 `35ff7d2e4c18c3923b603194f018a84f9d058e50dfc15b226b89c30f867c7eee`. Object vtable `015EC274` has this target at slot `+10`; RTTI is `Object`. All direct branch targets in the complete body are instruction-aligned and internal; indirect virtual callees remain separate.

Object byte `+0C` bit04 selects an early return; bit02 must be set to reach the examined component updates. The incoming raw word is saved in ESI at `00B2313C`. A nonnull appearance `+28` is invoked before the controller path, using that word; ordinary callee-save/normal-return assumptions apply across that call. At `00B2315A`, object `+2C` supplies the controller receiver. If nonnull, `00B23161` pushes ESI and `00B23162` calls the complete wrapper above. There is no direct rescale between incoming object argument and controller argument.

These instructions establish conditional forwarding and gates, not a recovered gameplay name for the flag bits or a proof that a particular live object has a player controller attached. No retained reference, asynchronous transfer or unit conversion is observed on this direct carrier path. Ownership/thread affinity and transitive mutation remain Unknown.

## Reused clock and scheduler evidence

The [accepted ordinary clock chain](SWITCH_TEXTURE_CLOCK_CONTRACT.md) already closes the frame/event/scene scheduler route. This round rechecks selected import/export/math/dispatch witnesses from that chain without repeating startup-FPU installation or following a new scheduling family.

| Existing boundary | Rechecked observation / Confirmed | Unit implication |
| --- | --- | --- |
| `00AA4BBE`, `00AA4BF1/FA` | `QueryPerformanceFrequency` import at `015DC244`; convert its counter frequency and store F32 denominator `019446D4` | API frequency is counts per second, as established by the linked prior API evidence |
| `00AA4DEA`, `00AA4E0F/14/1C/24` | `QueryPerformanceCounter` import at `015DC240`; counter difference, integer-to-x87 conversion, division by frequency | Ordinary quotient has units seconds |
| `00AA4E6D`, `00AA4EC0`, `0042383C` | Published F32 `01944720`; exact Clock frameTime export; outer frame reads it | The established event chain carries the published seconds; publication can be capped/retained |
| `00B3FE66/6F/8B` | Multiply supplied frame value by exact F32 1000 and call existing integer conversion helper | Existing scheduler quantizes seconds to thousandths; minimum-one and truncation behavior remain prior evidence |
| `00B3F6C0/C6/CC` | Exact F32 one divided by exact F32 1000, stored at `0194702C` | Nominal tick-to-seconds scale is1/1000 |
| `00B40EE7..00B40F6E` | Subtract object's last U64 tick from global tick, convert difference, multiply scale, spill F32, skip equal ticks, otherwise push spilled word through object slot `+10` | Callback receives accumulated scheduler seconds since that object's preceding update |

The 161-byte scheduler carrier window `00B40ED0..00B40F70` has SHA256 `918fdcc38cf3e71d5b47e580e891a0794d16203aeb3a63595e68a28a9f272f55`. It is a **window**, not a claimed complete scheduler function. No new helper/scheduler/global survey was performed. Prior event/due-queue/publication gates remain necessary; no every-frame, paused-zero, overflow or malformed-time acceptance claim follows. Active callback arithmetic controls remain Unknown, despite the separate existing installation evidence.

## Why the actual player unit remains open

**Observation / Confirmed:** known CreatureObject constructor installs primary vtable `015DCC34` at `0042F5F1`. Its COL/TypeDescriptor identify `CreatureObject`, and slot `+10` is **`00430260`**, not the common Object target `00B23100`. The scheduler's object slot therefore has an actual override on that constructed type.

This round does **not** decode that override or assume its incoming argument is forwarded unchanged. The two new complete callees were common object `00B23100` and immediate controller wrapper `00B3D1C0`; the explicit two-callee bound is reached. Clock-to-scheduler seconds plus the common wrapper cannot bypass the override and prove player units by analogy.

**Conditional interpretation / Confirmed implication:** if the selected CreatureObject override reaches the common object/controller path while preserving its argument's units and the controller pointer holds the player vtable, the player update argument is accumulated seconds. **Actual carrier classification / Unknown:** transformations, alternate dispatch, splitting/capping or other gating inside `00430260`, and selected runtime attachment. Physical acceleration units, speed/distance units and full integration parity remain separately Unknown.

## Reproduction and negative policy

[Producer](../tools/python/verify_player_controller_update_units.py): `--exe EXE --packages STATIC_PACKAGES --server-root SERVER --receipt FRESH_PRIVATE_RECEIPT`. Executable identity precedes optional imports/VA use. It reuses the frozen controller evidence and checks56 anchors, two complete bodies, the scheduler window, two clock imports/one export, constants and three RTTI/vtable slots. The receipt explicitly reports the actual player units as Unknown and names the override edge.

[Independent runner](../poc/round53-tests/Runtime/research_checks.py): same inputs with `--output-dir FRESH_PRIVATE_DIRECTORY`. It independently maps PE file sections/import thunks without pefile/Capstone, checks24 byte witnesses/five relative calls/two constants/three dispatch and RTTI types/two import slots/two complete body digests. [Synthetic fixtures](../poc/round53-tests/Runtime/carrier-fixtures.json) exercise55 opaque-word forwarding/gate cases and six nominal accumulated-unit/equal-tick cases. Nonfinite and negative words test the abstract integer carrier only under normally returning callees; they do not prove original acceptance, original execution or FPU parity.

Eleven negatives reject wrong/missing executable, deliberately truncated body expectation, unaligned branch assertion, truncated/empty/negative/unmapped spans and existing/public outputs. The frozen private-output guard inspects lexical symlink/reparse ancestors before creating directories. No original executable is modified or run. Synthetic unit expectations are consequences of the clock/math evidence; they are not evidence for units by plausible numeric values.

**Exact next probe:** bound CreatureObject slot `+10` target `00430260` from its actual entry through the necessary common alter/controller forwarding call(s), tracing incoming word, any copies/rescale/substeps and discriminators. Stop at the next necessary unclosed carrier if a two-callee budget is reached. Promote actual player seconds only after that edge and receiver binding are supported. No original execution, full caller census, startup-control repeat or acceleration implementation is needed for that probe.


## Round54 accepted increment

[CreatureObject carrier](CREATURE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md) forwards the same incoming word and receiver through00430260 and0063F3B0 into00554C60. Conditional scheduler seconds advance to that boundary; actual player units remain Unknown until00554C60 and receiver attachment are closed. No native acceleration was added.


## Round55 conditional carrier closure

[Tangible-named ClientObject carrier](TANGIBLE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md) closes00554C60 and the selected current controller attachment evidence. The historical Unknown statements above describe the earlier stopping point. The continuous static word/receiver route now supports accumulated scheduler seconds at the PlayerController callback conditionally under the six documented scheduler/normal-return/flags/current-pointer/current-type/queue-return premises. It does not prove runtime lifetime or physical acceleration/speed/distance units. Next implementation may use explicit caller-supplied nominal rates with these limits stated.
