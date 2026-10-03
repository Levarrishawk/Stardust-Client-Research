# SWTS fresh registration and first material query

## Scope and result

Static baseline: `Stardust.exe`, SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Addresses below are VAs. This supplements the [runtime contract](SWITCH_TEXTURE_RUNTIME_CONTRACT.md),
[clock contract](SWITCH_TEXTURE_CLOCK_CONTRACT.md), and
[modern scene visual contract](SCENE_STATIC_SWITCH_TEXTURE_VISUAL_CONTRACT.md).

**Confirmed:** fresh object-clock registration records the current global tick;
it does not itself deliver a shader update. A same-tick scheduler encounter skips
the object alter callback. The default material query reads the current DTST
index. **Unknown:** the first visible index of an original mission-terminal
instance. Construction, deferred loading, scheduling, and first submission have
not been ordered for a particular instance by runtime observation.

## Construction and clock ownership

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| Object constructor `00B21B80` clears appearance `+28` and clock-record pointer `+98`; sets eligibility byte `+94` to 1 (`00B21C26`, `00B21C33`). | Confirmed initial fields; clock storage is allocated later. | Do not model a freshly constructed object as already serviced. |
| Registration `00B40070` checks `00B24CC0`, obtains global low/high tick words at `01947010/01947014`, calls `00B24670`, then `00B40A80`. | Confirmed eligibility check, lazy record creation, and queue request. | Registration is distinct from elapsed-time delivery. |
| `00B24670` returns when `Object+98` is already non-null. Otherwise it calls `00B57190` with the supplied tick and stores the resulting pointer. Record constructor stores low/high at `+00/+04`, zeros remaining fields including queue index `+24`. | Confirmed fresh seed; repeated registration does **not** reseed. | The modern fresh-registration seed is supported. Removing and re-registering an existing record needs a separately stated policy. |
| `00B24700` reads and `00B24710` writes the record's two tick words. `00B40A80` uses record queue index and `00B24760` to add active links. | Confirmed scheduler bookkeeping; meaning beyond queue selection is unknown. | Avoid assigning an unsupported semantic priority to initial index 0. |
| `00B400C0` removes queue memberships. Object destructor tail `00B21FA5..00B21FDE` calls it, destroys/frees the record, and clears `+98`. | Confirmed record ownership by Object and distinct queue removal versus destruction. | Queue removal alone must not be equated with fresh clock construction. |

Scene insertion wrapper `00561230` optionally adds the object to a manager,
calls object vtable `+08` (base `00B225F0`, add-to-world), and always calls
`00B40070`. Base add-to-world marks the world flag and calls the appearance's
world-entry slot `+50` when present. These are direct instruction observations;
they do not prove every concrete object's transitive callback behavior.

Appearance assignment `00B22F60` stores `Object+28`, calls new appearance slots
`+28` and `+48`, and, if already in-world, `+50`. Mesh owner attachment
`00B2C430` stores its owner at `+0C`. Neither inspected body directly calls DTST
alter or writes the object-clock tick. This is a bounded local absence claim,
not an assertion about every virtual/customization callback.

## First eligible service

Scheduler update `00B410D0` calls tick advance `00B3FE60`, transfers active
queues through `00B40E70`, and processes objects through `00B40ED0`.
The established clock contract supplies the 1000-tick scale and minimum one-tick
advance on its ordinary update path.

`00B40ED0` computes `(globalTick - lastTick) * inverseTickScale`, stores a
binary32 delta, and compares the tick words again. Equality takes the
`00B40F4E` skip branch: no object virtual alter call. Otherwise it writes
`lastTick = globalTick` at `00B40F58` and calls object vtable `+10` at
`00B40F6E` with the computed delta.

Therefore, a freshly registered object selected after a subsequent ordinary
tick advance receives positive elapsed time under monotonic, non-wrap
conditions. Registration within a current processing phase does not establish
that ordering. A queued same-tick encounter is not a zero-second shader update.
This distinction matters when defining a modern registration API.

There is an alternate direct child-update path: base Object alter calls
`00B40A50` at `00B231ED` to synchronize a child's stored tick, then calls the
child's alter slot at `00B231FD` with the parent's delta. Thus equality of stored
and global tick does not universally prohibit alteration outside the inspected
top-level scheduler dispatch. Pause, culling, alternate owners, and all queue
mutation phases remain outside this closure.

## Primitive readiness and first query

Mesh construction `007931C0` initializes primitive-set pointer `+54` to null
and calls `00793360`. That routine returns without creating a set when its
template's `+38` loaded-data pointer is null. When present, it creates the set
through `007711E0` and stores it at Mesh `+54`. A deferred template completion
path calls the same ensure routine at `00796822`, after loading at `00796809`.
The phase/thread ordering of that completion with registration and first draw
is **Unknown** here.

Set construction `0077E4F0` creates local primitives through `0077E120`.
Each fetches its shader through `007707A0`, template slot `+20`; the SWTS
template slot resolves to `0077EBF0`, which calls runtime constructor
`00785B00`. The existing runtime contract establishes DTST constructor
`00B5ACA0`, residual 0 and index 0, and interval-driven update `00B5AD20`.
Mesh alter `007933C0` only reaches the set update when `+54` is non-null.
Mesh render `007934F0` also returns without submitting a set when null.

With a ready set, render calls enqueue `0077E840`, which calls sorter submit
`00773870`. Its query callback pointer `019234A4` defaults to `00773F50`
(installed in the block at `007732E2`). The callback invokes primitive slot
`+0C`, local primitive `0077E370`, which forwards to shader slot `+14`.
SWTS `00785C30` asks selector slot `+0C` (`00B5AD80`) for the current index,
then forwards to that variant. It does not advance the selector. Callback
setter `00773F60` allows an override; this conclusion is for the default route.

The established frame update-before-render order permits an elapsed DTST
update to advance the variant before a later draw. A positive delta below the
interval leaves index 0; a sufficiently large delta can advance it. A newly
constructed selector queried before any update reads index 0. None of these
conditional facts identifies a particular original instance's first visible
frame, especially across deferred primitive construction.

## Modern policy and next probe

Keep fresh registration's current-tick seed and explicit elapsed-time delivery
separate. Do not inject an assumed startup update or require the first query to
observe index 0. Define record reuse and deferred-resource registration policy
explicitly; the modern wrapper's policy is not proof of original reattachment
behavior. Preserve current-index-only queries.

Reproduction and validation are recorded in the
[Round 30 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R30_SWITCH_TEXTURE_FIRST_TICK.md).
The exact next static probe is the deferred mesh-template completion caller and
thread/frame-phase chain around `00796809/00796822`, joined to one ordinary
GroundScene object's construction, `00561230` registration, and first enqueue.
Do not infer a first-visible result until that order is established.
