# Analysis Checkpoint: SceneObjectClose / SceneEndBaselines

Date: 2026-09-13  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

The prior server-name miss `SceneObjectCloseMessage` is fully correlated to the executable's already recovered `SceneEndBaselines` path. This is a naming alias across source and wire terminology, not an absent handler.

## Confirmed identity and schema

- Server `SceneObjectCloseMessage.h` writes opcount U16 `2`, opcode U32 `0x2C436037`, then object ID U64.
- The exact engine CRC implementation (`String::hashCode`, polynomial table form) yields:
  - `SceneEndBaselines` -> `0x2C436037`
  - `SceneObjectCloseMessage` -> `0x5F62343B`
- Therefore the wire identity is `SceneEndBaselines`; the server class name is merely its producer-side wrapper.
- Stardust.exe compares that message identity at `0x0051C314`, constructs/parses it at `0x00AD58D0`, extracts the sole LE64 ID, resolves the client object, then calls finalizer `0x0051F9B0` at dispatcher call site `0x0051C3B1`.

## Confirmed lifecycle placement

Current server `SceneObjectImplementation::sendTo` emits:

1. `SceneCreateObjectByCrc`
2. containment link
3. object baselines
4. contained objects
5. slotted objects
6. `SceneObjectCloseMessage`, conditionally on `doClose`

`sendWithoutParentTo` ends with the same close message. Manufacture schematics send it after create, containment, baselines and child contents. Guild state sends it after create plus views 3 and 6. These independent producers make its role an end-of-initial-baseline transaction barrier.

## Executable behavior

`scene_end_baselines_finalize_VA_0051F9B0`:

- checks object state through virtual calls;
- performs an auxiliary setup/publication path when the first predicate is false;
- inspects parent/attachment relationships;
- conditionally invokes parent-related validation;
- finishes with an object virtual callback when readiness/attachment predicates permit.

The individual virtual method names remain **Strongly inferred**, because the relevant object-class vtables have not all received semantic labels. The route, target object, lifecycle boundary and non-destruction meaning are **Confirmed**.

## Replacement-client contract

Maintain a per-object construction phase. On `0x2C436037`:

1. Require an exact 8-byte body after opcode.
2. Resolve the U64 network object ID without pointer-width conversion.
3. Mark initial baseline ingestion complete.
4. Resolve deferred containment/appearance dependencies.
5. publish object-ready only after required baseline state is valid.
6. Never remove the object; removal belongs to `SceneDestroyObject` (`0x4D45D504`).

An unknown object ID should be logged as a lifecycle ordering anomaly and handled according to a bounded deferred-message policy. It must not be interpreted as authorization to create or destroy an object.

## Coverage effect

- Server exact-name misses correlated by alias/route: 32 of 37.
- Remaining direct correlations: 5.
- Server message-class identity-resolved lower bound: 109 of 114 (95.61%).
- Overall weighted behavioral-research estimate remains 74%; this block closes a material correctness gap but does not justify a whole-point increase after rounding.

## Remaining local uncertainty

The exact names of the state predicates and final virtual callbacks inside `0x0051F9B0` remain to be assigned during object-class/vtable reconstruction. That uncertainty affects fidelity of the readiness implementation, but no longer affects message identity, wire parsing, dispatch, or the distinction between finalization and destruction.
