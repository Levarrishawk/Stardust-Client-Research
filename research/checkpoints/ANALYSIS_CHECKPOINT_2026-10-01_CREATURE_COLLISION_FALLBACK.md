# October 1, 2026 — Creature collision fallback

## Question

What creates a body extent when the authored appearance secondary is NULL? Stop at source precedence, activation flags, exact creature fields and fallback geometry; global actor controller reachability remains separate.

## Inputs and evidence

Same executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; existing read-only Ghidra project, instruction disassembly, direct-call search, literal references and RTTI bytes. Private detailed output remains ignored; no original execution.

Confirmed observations:

- Constructor `01260110` installs CollisionProperty vtable `015FFB00`; RTTI locator `01638F40` references descriptor `01884664` named `.?AVCollisionProperty@@`. It initializes byte `+78` false, byte `+7B` false and body eligibility `+7E` true, then configures the property from the owner template through `012604D0`.
- `012604D0` dynamically casts the template to SharedCreatureObjectTemplate. This branch enables `+78`, reads six creature parameters and creates a floor-related state object at `+E4`. Other template families follow distinct branches; NULL source alone does not activate creature fallback.
- Loader `011B13F0` binds literal `stepHeight` to template `+55C`, `collisionHeight` to `+574`, `collisionRadius` to `+58C`, `collisionOffsetX` to `+71C`, `collisionOffsetZ` to `+734` and `collisionLength` to `+74C`. Getter/instruction chains map step height through `011AF1B0` to property `+9C`, radius through `011AF8D0` to `+A0`, X/Z through `011B05B0`/`011B0940` to `+A4/+A8`, length through `011B0CD0` to `+E8`. Collision height is a distinct field; it is not the `+9C` value used by the fallback sphere.
- `01260E40` first converts appearance-template secondary `+10`. If no local extent results, it follows owner `00B245D0`, source getter `00B2EE70` (`+20`), indexed cell accessor `00B47C00` with index0 and extent getter `00B45A50` (`+1C`). CELL/0005 loader `00B451E0` stores its optional extent at that same `+1C`. This identifies a layout cell0 extent source, separate from selected interior CELL bounds.
- If both conversions still leave local extent absent and property byte `+78` is true, it creates a sphere-derived SimpleExtent. For ordinary byte `+7B` false, local center is `(offsetX * scale, (stepHeight + radius) * scale, offsetZ * scale)` and radius is `radius * scale`. Byte `+7B` true substitutes radius0.5 and step height1.0 before scaling (`012614B0`/`012614D0`; PE constants checked).
- When a floor-related state object exists, the scaled sphere radius is supplied through `01264080`. Later transformation can remove rotation for byte `+78` true with `+7C` false while retaining translation. Full meanings of these flags and controller transitions remain open.

## Interpretation and consequence

The fallback sphere's lowest point is step height above its local origin for the ordinary positive-scale case. That is confirmed geometry. It differs from the modern box starting at feet plus0.1 mm clearance. Whether this sphere is the active original body on the exact route requires the original creature template and reachable controller chain; no runtime equivalence claim is made.

Template parameter getters support inheritance, randomized modes and relative operations. A simple authored scalar cannot silently replace those semantics. Next implementation should decode and resolve a bounded deterministic template profile, rejecting unsupported parameter modes.

Direct caller search found the object-aware wrapper `01266CD0` and raw query wrapper `01266C20` invoking `01266E80`; the latter performs floor continuation. No external direct caller of these two wrappers was found in this scan. Absence of a direct call does not prove unreachable code or exclude indirect use. Do not strengthen the inspected helper-chain conclusions to every original actor controller.

## Validation and next action

Direct instructions verify source precedence, field assignments and scaled sphere construction; independent PE reads verify constants and RTTI. Durable conclusion is in the [actor participation specification](../../docs/GROUNDED_ACTOR_COLLISION_PARTICIPATION.md). No canonical names or score changes.

Next: decode the original human creature template and inherited collision fields from unchanged configured assets. Establish a reproducible deterministic profile and appearance path before selecting a replacement actor volume. Preserve unsupported inheritance/random/relative modes explicitly.
