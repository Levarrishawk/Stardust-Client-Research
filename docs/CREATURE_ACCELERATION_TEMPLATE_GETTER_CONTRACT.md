# Indexed creature acceleration template getter

Date: 2026-10-04. Bounded question: how does `011ADD30` select, inherit and combine one indexed acceleration parameter? All addresses refer to executable SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The direct getter and its one necessary endpoint reader are closed. No compatibility acceleration implementation or physical-unit claim follows.

This extends the [helper contract](CREATURE_ACCELERATION_HELPER_CONTRACT.md), using the existing [template loader/resource oracle](CREATURE_TEMPLATE_MOVEMENT_INPUT_CONTRACT.md) and [initialization evidence](CREATURE_VIEW4_INITIALIZATION_CONTRACT.md). [Round52 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R52_ACCELERATION_TEMPLATE_GETTER.md) records validation and resume scope.

## Extent, identity and indexing

**Observation / Confirmed:** getter `011ADD30..011ADE30` is257 bytes/99 instructions, SHA256 `98776f201b8d00320bc0bd542cf98129e7ef8144591eda4affbae8eaf2405f1e`. Its15 direct jumps target in-body instruction boundaries. Four exits use `ret 4`;15 NOPs precede speed getter `011ADE40`. Receiver ECX is saved in ESI; the four-byte stack argument is an index. Return is x87 ST0. No indirect jump-table dispatch occurs in this body.

`011ADD3D/41` computes index times24. Local parameter storage is receiver `+468 + 24*index`, loaded byte `+478 + 24*index`, relative byte `+47C + 24*index`. `011ADD92/99` computes the same parameter address for endpoint call `013FA1E0`. Constructor witnesses `011ACE81/83/85` supply two elements, stride24 and base `+468` to the established array construction mechanism. The getter has **no direct index-range check**. Caller evidence supports indices0/1; it does not justify arbitrary indices, native pointer exposure or a recovered original rejection policy. Arithmetic index2 reaches base `+498`, which is the separately documented speed-array start; that coincidence is not an authorized third acceleration parameter.

Loader `011B1640` selects string `acceleration` at `018DE8A0`, compares13 bytes including NUL, reads a four-byte little-endian count at `011B165E`, and starts storage at `+468` (`011B1668`). Signed `jge` at `011B1673` skips a nonpositive count. Positive entries load through the float parameter virtual loader, advancing24 bytes, capped at two stored parameters (`011B1684/87`). Additional entries are loaded into a temporary and destroyed through `013FA200/013FA0B0` at `011B16B3/BE`. This is original load/discard behavior. The research decoder's nonnegative/count64/64KiB/depth budgets are explicit inspection guards, not original executable error rules.

**Consequence:** acceleration selectors0/1 are indexed **acceleration records**, distinct from speed getter `011ADE40`'s slots at base `+498`. Do not relabel them run/walk speed or assume a deceleration flag. The [helper](CREATURE_ACCELERATION_HELPER_CONTRACT.md) chooses selector0 when walk is less than current or unordered, and selector1 for ordered equal/greater; controller rise and fall both call that same threshold-dependent helper.

## Loaded, inherited and missing-parent paths

**Observation / Confirmed:** getter loads template parent `+18`, initializes EDI null and, when nonnull, casts from `ObjectTemplate` descriptor `0186B44C` to `SharedCreatureObjectTemplate` descriptor `0186B6EC` through `0131E6F1` at `011ADD5A`. Those descriptors are already pinned by the frozen initialization verifier. The stack index is reloaded after this call.

If loaded byte is zero, `011ADD72..81` checks mutable byte `018DE794`. A nonzero global **and** null cast parent returns F32 zero from `015DC6A0`. Otherwise ESI becomes the cast parent and execution jumps to `011ADD44`, keeping the same index. An absent record's relative byte is not applied before this inheritance loop.

When the global is zero and parent is null/failed-cast, this direct path re-enters with ESI null and next reads receiver `+18` without another null guard. No cycle/depth limit appears in the getter. These are instruction observations; no original crash, recovery invariant, global meaning or permissive malformed-input policy was established by execution. **Unknown:** global initialization/use invariant and transitive parent lifetime. A replacement must explicitly specify a safe missing/cyclic/index policy rather than silently presenting the research inspector's rejection as original behavior.

## The local operand does not sample RNG

**Observation / Confirmed:** complete endpoint reader `013FA1E0..013FA1FC` is29 bytes/12 instructions, SHA256 `049de918f66be46a39d92ecea6a400d7e676301ae0c78945fb607d9c92087638`. It has two direct branches, three returns, no calls and no stores. It reads runtime mode from parameter `+8`:

| Runtime mode | Direct return | Evidence |
| --- | --- | --- |
| 1 | F32 literal at parameter `+4` | `013FA1F9 fld [ECX+4]` |
| 3 | F32 at storage pointer `[parameter+C]+4` | `013FA1F2/F5` |
| Other, including2 and4 | F32 zero at `015DC6A0` | `013FA1EB` |

Frozen serialized loader mode3 reads first and second F32 endpoints into separate locals. `013FA3BB` writes the first to allocated storage `+0`; `013FA3C7` writes the second to `+4`. Consequently this getter uses the **second serialized endpoint**, without sorting, interpolation or random sampling in these bodies. Mode2 weighted storage is not sampled here; it takes zero if reached loaded. Runtime mode4 likewise takes zero; that does not change the loader's separate supported serialized-mode boundary.

This differs materially from the existing speed/scalar getters' sampling paths. No RNG/distribution/global sampling behavior is imported from analogy. Delegated loader allocation/destruction, RTTI implementation and mutable-global semantics remain outside scope; endpoint mode/result and absence of direct RNG calls are Confirmed.

## Relative branches and precision

`011ADDA1` spills the local operand to F32 with `fst` while retaining ST0. Relative byte `+`, `-`, `=`, `_` enables a parent operand; other bytes return the local operand. Parent local starts at exact F32 zero (`011ADDC4`). If original parent pointer and cast parent are nonnull, `011ADDD1/D6/D7/D9` recursively calls this same getter on the parent with the **same index**, discards current ST0 first, spills returned parent ST0 to F32 (`011ADDDE`) and reloads local F32 (`011ADDE1`). Without that recursive call, the original local ST0 stays retained.

The endpoint reader's directly returned local operand starts from F32 storage/zero, so there is no demonstrated pre-spill local arithmetic excess precision. Recursive parent's relative arithmetic can produce a wider ST0 result which this child then narrows to F32. The synthetic parent-return witness demonstrates the distinction between a nominal sum of two F32 operands and its subsequent F32 spill; it is not an original x87 execution or rounding-control proof.

With local operand `c`, F32 parent operand `p` and F32 constant `k` at `015DE0E4` (bytes `0AD7233C`), nominal operation order is:

| Relative byte | Nominal expression | Direct witness |
| --- | --- | --- |
| `+` | `c + p` | `011ADDE9 fadd` |
| `-` | `p - c` | `011ADE25 fsubr` |
| `=` | `p * (1 + c*k)` | `011ADDFF`, `011ADE08`, `011ADE0E` |
| `_` | `p - (c*k)*p` | `011ADE1C`, `011ADE22`, `011ADE25` |
| Other | `c` | branch to `011ADE28` |

One is F32 bytes `0000803F` at `015DC6F8`. No F32 final-result store precedes the getter's return. The helper subsequently multiplies that returned ST0 by its captured received/current scalar inputs. **Unknown:** physical units, active arithmetic controls, exception/signaling-NaN behavior and complete original numeric parity. Ownership/thread assumptions remain Unknown; direct getter writes are stack scratch only and supply no retain/release or synchronization witness.

## One original acceleration record

The accepted pinned human necessary-parent chain is reused with its existing five source identities/8290 bytes and48 descending TRE mounts. No new archive resolver or corpus sweep is introduced. All sources/configuration/executable are checked before interpretation. The producer captures only the root acceleration array's inheritance structure and the nearest parent's one winning acceleration array record. Root count is two with unloaded mode0 entries; nearest parent count is two with mode1, space-relative encodings. Thus selectors0/1 have inherited deterministic literal **serialized inputs** in this chain. Raw words, payload offsets and source names remain in ignored receipts; final runtime values and template installation are not claimed.

Independent verification rereads only the winning original payload through the accepted resolver, checks source priority/size/SHA, locates the unique acceleration record, checks count/order/literal header layout and compares both raw F32 words to the fresh producer receipt. Source names, original numeric values and extracted arrays are absent from the public synthetic fixtures.

## Reproduction, tests and next boundary

[Producer](../tools/python/verify_creature_acceleration_template_getter.py): `--exe EXE --packages STATIC_PACKAGES --server-root SERVER --root ASSET_ROOT --configuration CONFIG --receipt FRESH_PRIVATE_RECEIPT`. It checks71 anchors, complete257/29-byte bodies, in-body direct branches, loader/name/constants, frozen helper/controller/writer/init/resource prerequisites and the one original record. Fresh receipt replay is deterministic.

[Independent runner](../poc/round52-tests/Runtime/research_checks.py): same explicit inputs plus `--oracle PRIVATE_PRODUCER_RECEIPT --output-dir FRESH_PRIVATE_DIRECTORY`. It passes24 byte witnesses, eight relative calls,15 short branches, two parameter strings/two body digests,60 synthetic endpoint/relative cases, eight inheritance cases, a parent-return precision distinction, two array/discard positives,34 negatives and eight original record checks. [Fixtures](../poc/round52-tests/Runtime/getter-fixtures.json) are independently authored nominal/routing expectations, not original sampled trajectories.

Negative cases cover wrong executable/configuration before unavailable imports/asset lookup, missing input, wrong body digest, wrong unaligned branch assertion, every truncated array prefix, unsupported mode/count/trailing data, unmapped/negative spans and existing/public output rejection. The frozen output guard checks lexical link/reparse ancestors before directory creation. Research budgets do not alter original inputs or establish original malformed-input behavior.

**Exact next question:** establish the controller incoming update argument's units at one bounded caller, distinguishing controller/engine scheduling from the helper's current-speed argument. Stop after that call boundary, before dynamic original execution or a whole-program caller census. Vehicle getter `011AC4C0`, parent/global invariants and original arithmetic parity remain independent follow-ups. Modern movement admission remains unchanged.

## Round53 update-unit carrier

[Caller/unit evidence](PLAYER_CONTROLLER_UPDATE_UNITS_CONTRACT.md) closes the
common Object::alter/controller forwarding bodies and scheduler seconds.
Actual player units remain Unknown across CreatureObject override00430260 and
selected receiver attachment. This replaces the former immediate-caller resume
question; getter/global/vehicle/arithmetic boundaries remain separate.
