# Creature view 4 template initialization

The five template-fed CREO4 float fields originate in a `ClientCreatureObjectTemplate`, through its `SharedCreatureObjectTemplate` base. Their getters resolve local/inherited parameters and may sample runtime state. This establishes conditional constructor inputs, **not effective values for every creature before its first observer delta**. Later transitive writes, effective startup state, and effective asset overlay values remain open.

Baseline: original executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; all addresses below are baseline VAs. Evidence is static instructions, data, RTTI, vtables and loader routing. No client execution, server contact, captures or new asset survey occurred. The [initialization verifier](../tools/python/verify_creature_view4_initialization.py) checks this evidence and reuses the frozen [delivery verifier](../tools/python/verify_creature_view4_delivery.py), including its 58 constructor anchors and 14 registrations. Read with the [delivery contract](CREATURE_VIEW4_DELIVERY_CONTRACT.md) and [Round44 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R44_CREO4_INITIALIZATION.md).

## Template identity and creation

**Observation, Confirmed.** Type descriptor `01881420` contains `.?AVClientCreatureObjectTemplate@@`; vtable `015DFB9C` has a complete-object locator referencing that descriptor and slot 6 points to `006126D0`. Constructor `006125A0` calls shared base constructor `011ACE10` at `006125AA`, then installs this vtable at `006125AF`. The shared base's descriptor is `0186B6EC`, with `.?AVSharedCreatureObjectTemplate@@`; vtable `015FEBE4` references it and has base slot 6 `00B2E7A0`.

**Observation, Confirmed.** Factory `006126D0` saves its `ecx` template in `esi` at `006126EF`, allocates `0x9B8` bytes, and on successful allocation pushes the saved template at `00612707`, sets `ecx` to the allocation at `00612708`, and calls creature constructor `0042ECF0` at `0061270A`. Allocation failure bypasses construction. It passes the resulting object to `00623530` at `0061271D` before returning. In the creature constructor the aligned-stack argument is `[ebx+8]`, loaded at `0042ED1F` and passed into tangible construction `0063EE10` at `0042ED2A`; `0042F09F` loads that same argument into `edi` for the getter calls.

**Interpretation, Confirmed.** The supplied argument on this concrete factory path is the client creature template, not a server-derived class analogy. **Consequence:** replacement initialization must consume the resolved shared creature template values. Factory identity does not establish when every possible creature construction path runs or prove absence of subsequent mutation.

## Constructor values and getter storage

These are the ten registered view-4 F32 fields; collections and listen-to ID are separate unresolved initialization questions. The existing registration audit provides field indexes and wrapper offsets. The constructor writes both cached/current F32 slots of each wrapper; its returned getter value is explicitly spilled to F32 first.

| View-4 index | Wrapper offset | Constructor input | Template parameter and loaded flag |
|---|---|---|---|
| 0 | `+690` | literal `1.0f` | none established |
| 1 | `+67C` | literal `1.0f` | none established |
| 4 | `+640` | literal `1.0f` | none established |
| 5 | `+62C` | literal `1.0f` | none established |
| 7 | `+668` | `011ADE40(0)` at `0042F0E0` | `speed[0]`, `+498`; loaded `+4A8` |
| 8 | `+6B8` | `011AE700(0)` at `0042F194`, followed by angle conversion | `slopeModAngle`, `+514`; loaded `+524` |
| 9 | `+6CC` | `011AEA90(0)` at `0042F1D6` | `slopeModPercent`, `+52C`; loaded `+53C` |
| 10 | `+6A4` | literal `1.0f` | none established |
| 11 | `+654` | `011ADE40(1)` at `0042F0AA` | `speed[1]`, `+4B0`; loaded `+4C0` |
| 12 | `+6E0` | `011AEE20(0)` at `0042F20C` | `waterModPercent`, `+544`; loaded `+554` |

**Observation, Confirmed.** Speed uses `index*24`: `011ADE4B` computes three times index and `011ADE51` shifts by three; local parameter starts `+498`, mode `+4A0`, loaded flag `+4A8`, relative byte `+4AC`, all plus that stride. The three scalar getter mode/relative offsets are `+51C/+528`, `+534/+540`, `+54C/+558` respectively. Scalar arguments are pushed as zero by the creature constructor; their getter bodies use fixed parameter offsets rather than indexed addressing.

**Interpretation, Strongly inferred.** Existing registered field semantics distinguish run (7) and walk (11); direct call arguments establish speed slot 0 versus 1 independently of those labels. **Consequence:** preserve their ordering; do not substitute template acceleration parameters for constructor literal-one fields without evidence of a later write.

**Observation, Confirmed.** Angle conversion is two x87 `fmul` instructions: `0042F199` multiplies by F32 bytes `DB0F4940` at `015DCBD4` (approximately pi), and `0042F1A5` multiplies by F32 bytes `610BB63B` at `015DC5F0` (approximately 1/180), before the F32 spill at `0042F1AB`. **Interpretation, Strongly inferred:** degrees to radians. **Consequence:** do not collapse this to a host-language constant and claim bit equivalence; x87 intermediate precision and rounding still matter.

## Loading and inheritance

**Observation, Confirmed.** Shared template constructor `011ACE10` constructs two 24-byte float parameters at `+498` and scalar float parameters at `+514`, `+52C`, `+544`. Their constructor `013FA000` sets mode `+8` to zero, loaded byte `+10` false, relative byte `+14` to space (`0x20`) and vtable `01609AB8`. It does not initialize a literal value at `+4` in this small constructor body; no default scalar value is inferred from it.

**Observation, Confirmed.** Loader `011B13F0` recognizes `SCOT` (`0x53434F54`). Names at `018DE898`, `018DE864`, `018DE854`, `018DE844` are `speed`, `slopeModAngle`, `slopeModPercent`, `waterModPercent`. The loader routes speed to `+498` at `011B16F3`, and scalar names to `+514`, `+52C`, `+544` at `011B1828`, `011B184C`, `011B1870`, then invokes the parameter virtual loader at `011B1A68`. The speed loop stores at most two parameters, advances by 24 bytes, and parses surplus serialized parameters into a temporary parameter which it destroys; acceptance of excess records does not enlarge the runtime speed array.

**Observation, Confirmed.** All four getters read parent pointer `+18` and cast through `ObjectTemplate` descriptor `0186B44C` to shared creature descriptor `0186B6EC` using `0131E6F1`. A false loaded byte follows the cast parent and retries. If switch `018DE794` is nonzero **and** the cast parent is null, it returns positive F32 zero at `015DC6A0`; with the switch zero, that branch instead follows the null parent without the same zero guard. Speed's exact branch anchors are `011ADE87`, `011ADE8E`, `011ADE95`, `011ADE9C`; scalar getters repeat this shape. There is no demonstrated cycle guard in these getter loops.

**Observation, Confirmed.** Installation routine `00612600` writes its byte argument to `018DE794` at `00612632`, calls `011AD200`, and registers `SCOT` with creator `00612670` via `00B28480`. The observed initializer `004297C0` takes a pointer argument into `esi` at `00429819`; `004298A7` tests its first byte, and the nonzero branch skips this installation region. On the zero-byte path, `004299DC` pushes zero and `004299DE` calls `00612600`, disabling this guarded-zero fallback. **Unknown:** whether this path has run for a particular runtime object, and any alternate indirect installation path. **Consequence:** the zero fallback is conditional, not a universal missing-parameter default. Do not invent deterministic null/cycle recovery as recovered original behavior.

## Parameter modes and relative arithmetic

Four raw jump tables are verified separately from executable instructions: `011ADFD4`, `011AE880`, `011AEC10`, `011AEFA0`. They select modes 1 through 4. An out-of-range mode follows the mutable fallback load described below.

| Mode | Direct observation | Compatibility consequence |
|---|---|---|
| 1 | Virtual slot `+14`; float vtable resolves to `013FE640`, loading parameter F32 `+4` | Literal value |
| 2 | RNG `00B59EF0` through global `01947188`, signed remainder by 100 plus one, subtract weights at entry `+4` with stride 8, select first remainder <= 0; cast selected parameter using descriptor `018DDEF8`, evaluate recursively via `011A1200` | Weighted selection depends on RNG state and nested parameter values; weights are not proven normalized or validated here |
| 3 | Virtual slot `+18` resolves to `013FA040`: endpoints from parameter `+C`, RNG integer converted by `fild`, multiplied by endpoint difference and F32 `00000030` at `015DC804` (2^-31), then minimum added | Runtime range sampling; RNG output distribution/endpoints remain unverified |
| 4 | Virtual slot `+1C` resolves to `013FE660`, loading mutable global `019BBB30` | This float implementation does **not** establish a dice-roll algorithm |
| Other / unselected weighted entry | Getter loads mutable global `019BBBBC` | Do not assign a numeric default from a file-backed constant or assume zero |

**Observation, Confirmed.** Relative bytes `+`, `-`, `=`, `_` cause recursive evaluation of the same parameter on the cast parent; if no valid parent exists in this relative branch, parent operand starts at zero. Speed forwards the same index; scalar getters pass zero. Other relative bytes return the local sampled value. The current operand is spilled with `fst` to F32; when a valid parent is called, it is discarded from the x87 stack and reloaded from that spill, and the parent result is also spilled to F32. Without that call the retained current x87 value can retain extra precision.

With `c` the current operand and `p` the parent operand, nominal arithmetic is:

| Byte | Expression | Instruction evidence in speed getter |
|---|---|---|
| `+` | `c + p` | `011ADF51 fadd` |
| `-` | `p - c` | `011ADFC7 fsubr` |
| `=` | `p * (1 + c * k)` | `011ADFA1`, `011ADFAA`, `011ADFB0` |
| `_` | `p - p * (c * k)` | `011ADFBE`, `011ADFC4`, `011ADFC7` |

Here `k` is F32 bytes `0AD7233C` at `015DE0E4` (approximately 0.01); one is `0000803F` at `015DC6F8`. **Interpretation, Confirmed for nominal operand order; Unknown for full bit-exact host reproduction.** Synthetic arithmetic checks document operand direction only. They are not execution evidence from the original or proof of RNG behavior, nonfinite handling, malformed template recovery or x87 rounding configuration.

## Later paths and delivery boundary

**Observation, Confirmed.** After replicated scalar construction, `0042FAE3` calls template predicate `011B5A80`. On that branch `0063F8D0` supplies an appearance, and five more sampled template results are passed with it to `011ACA20` at `0042FB45` (including a fresh speed[0] sample at `0042FB38`). Inside `011ACA20`, `00B32890` resolves objects through that appearance argument and dynamic-cast results targeting `RangedIntCustomizationVariable` (descriptor `0187A08C`, from `CustomizationVariable` descriptor `0187A068`) receive virtual `+20` calls. **Interpretation, Strongly inferred:** appearance parameter customization, not a demonstrated direct write to the ten replicated wrappers. **Unknown:** transitive virtual implementations and callback side effects. A fresh random getter call need not equal the earlier constructor sample.

**Observation, Confirmed.** Post-factory `00623530` passes callback `00623570` to `007D79F0`. Its complete body through `00623A42` branches on strings `event_`, `hpevent_`, `footstep`, `swing_`, `uswing_`, `hit_`, `uhit_` and calls `005F1C60`/`005F1CA0` with object/context arguments. These strings are pointer-indirected at `015E02B4` onward, not embedded directly there. Post-factory RTTI targets are `Appearance` (`01869430`) and `SkeletalAppearance2` (`0186944C`). Registration helper `007D79F0` uses `007D6830` to form a two-pointer pair, copies that pair into an eight-byte-entry vector at receiver `+2C`, and propagates it through child entries via `007D6C10` at `007D7AFE`; the helper does not directly call the supplied callback in the inspected body. The child propagation callee remains open. Both event wrappers lead to `005F1CD0`, which resolves `SharedObjectTemplate` (`01878D90`) then `ClientDataFile` (`01878D70`, from `SharedObjectTemplateClientData` at `01878D40`) and looks up an event via `00605DD0`. **Interpretation, Strongly inferred:** skeletal animation event registration and client-data event dispatch. **Unknown:** child propagation, transitive event effects and other constructor callees; no claim of an exhaustive scalar writer search.

The frozen delivery audit separately establishes owner-only baseline 4 and the first observer modifier delta for fields 1, 5 and 10 before view 6. Those fields begin at one in this constructor and may be replaced by that delta. The [full contract](CREATURE_VIEW4_FULL_CONTRACT.md) supplies packet layouts. This investigation does not change server behavior, baseline/delta codecs or observer replay models.

## Remaining boundary

The template identity, local field routing, conditional inheritance branches, sample modes, relative operand order and angle conversion are now evidenced. An engine-neutral replacement can specify this boundary without copying legacy memory layouts. It must not advertise a complete initial observer model yet: no effective original template chain was read this round, RNG/global initial values and effective initialization-path state remain unresolved, and later virtual/transitive writers are not exhausted. Collection fields 2, 3, 13 and listen-to field 6 remain independent unknowns.

Next bounded probe: follow `011ACA20`'s ranged-int customization virtual `+20` implementations, `007D6C10` child registration propagation and `005F1CD0` event dispatch callees to determine whether any can write the registered scalar wrappers before observer delivery. Only after those paths are bounded, evaluate one effective shared-creature template chain with the accepted archive resolver and distinguish deterministic literals from sampled values.

## Round45 accepted compatibility increment

Follow-up: [one serialized movement-input chain](CREATURE_TEMPLATE_MOVEMENT_INPUT_CONTRACT.md). The original unresolved boundaries above remain explicit.
