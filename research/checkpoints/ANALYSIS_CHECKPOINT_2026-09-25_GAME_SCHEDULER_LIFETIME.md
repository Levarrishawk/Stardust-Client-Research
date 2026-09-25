# GameScheduler lifetime and cancellation audit

Date: 2026-09-25  
Target: `C:\Stardust-DEV\Stardust.exe`

## Queue contract

Install `0x0119DF70` creates one process-global scheduler object and registers
its removal with ExitChain. The underlying object (`0x013E5D80`) owns:

- a deferred-insertion vector;
- a minimum heap of event pointers;
- the current integer scheduler time; and
- a byte indicating that callbacks are currently being drained.

Public schedule wrapper `0x0119E010` converts a floating delay to an integer and
calls `0x013E60A0`. Each 12-byte event stores absolute due time
`current + delay`, callback and context. Normal insertion maintains a minimum
heap by due time. If a callback schedules another event while the drain flag is
set, the new event goes into the deferred vector instead of mutating the heap
being traversed.

Frame tick `0x004237C0` is the sole direct caller of advance wrapper
`0x0119E040`. Core drain `0x013E6110` updates scheduler time, repeatedly invokes
and destroys every event whose due time is at or before the new time, clears the
drain flag, then merges deferred events into the heap. Callbacks therefore run
on the frame-owning thread, once, in deadline order; equal-deadline stable order
is not established.

## Cancellation and ownership

The schedule API returns no event handle, and no cancel-by-callback/context
route was found in the object methods or xrefs. Destruction drains both heap and
deferred collections and releases every unexecuted event, but does not invoke
their callbacks. Context destruction is callback-specific rather than owned by
the scheduler.

There are three direct schedule call sites in this executable:

| Caller | Callback/context policy |
|---|---|
| `0x004660F0` | allocates a 12-byte context; callback `0x00466160` releases its retained object/context before applying the delayed effect |
| `0x004B9270` | schedules global callback `0x004B8190` with null context |
| `0x00D844F0` | schedules multiple `0x00D83720` calls; each 12-byte context contains an index, request/generation value and raw mediator-relative pointer; callback frees the small context immediately, then performs work using copied values |

The last route demonstrates a legacy lifetime coupling: the scheduler itself
does not retain or validate an owning UI object and cannot cancel its pending
callbacks. Static evidence does not prove a use-after-free occurs in normal
flow, because the mediator may be retained by the active UI until all short
delays fire. It does prove that safe lifetime is an external invariant, not a
service guarantee.

## Replacement contract

Use a frame-thread scheduler with:

- monotonic time and explicit sub-frame/rounding policy;
- deterministic ordering for equal deadlines;
- cancellation handles or owner tokens;
- weak/generation-checked owner references;
- explicit context ownership/destruction even when cancelled; and
- shutdown that invalidates submissions, discards callbacks and destroys all
  contexts before dependent services are released.

Reentrant scheduling should remain deferred until after the current due batch,
matching the original observable ordering. Raw UI pointers must not be carried
across delayed execution.

## Confidence

| Finding | Confidence | Evidence |
|---|---|---|
| frame-thread deadline heap and reentrant staging | Confirmed | `0x013E5F50`, `0x013E60A0`, `0x013E6110`, frame xref |
| no exposed cancellation handle | Confirmed for all direct callers/core methods | return signatures and complete core cluster/xrefs |
| unexecuted callbacks suppressed on scheduler destruction | Confirmed | `0x013E5DF0` releases events without callback invocation |
| ticket mediator is always alive until callbacks finish | Unknown | no scheduler-side retention/cancellation; requires runtime lifecycle fixture |

This closes the GameScheduler service contract to replacement depth while
recording the ticket UI owner lifetime as a bounded runtime-risk fixture.
