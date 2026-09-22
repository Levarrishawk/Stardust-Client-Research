# Analysis checkpoint — scene worker retirement reconciliation

Target: `C:\Stardust-DEV\Stardust.exe`  
Date: 2026-09-22

## Result

The earlier scene-lifecycle question “are old-scene workers joined or are completions discarded?” does not have one universal answer. Static ownership evidence establishes two distinct policies, selected by worker ownership.

| Worker/service | Ownership | Scene retirement behavior | Confidence |
|---|---|---|---|
| procedural-terrain worker | `ClientProceduralTerrainAppearance`, therefore scene-owned | stop flag, signal, join, then queue/result and terrain-state destruction | Confirmed |
| general asynchronous asset loader | process-global asset service | remains alive; exact callback/context cancellation suppresses publication, while any in-flight work is still cleaned | Confirmed |
| file streamer | process-global synchronous request service | remains alive across scene reset; each admitted request is serviced to terminal completion | Confirmed |

`scene_transition_begin_VA_00462AE0` retires/destroys the old GroundScene before installing the new one. The terrain appearance destructor's stop/signal/join sequence therefore prevents its worker from publishing into freed terrain state. By contrast, the general async loader deliberately does not interrupt in-flight I/O: scene owners cancel their callback identity, and the main-thread drain skips the callback but always cleans the result. The file streamer has no scene cancellation API and blocks its request owner until terminal completion, so it is not a detached scene callback source.

## Replacement invariant

A replacement should preserve the ownership distinction rather than forcing all jobs into a single teardown rule:

1. join scene-owned producers before freeing their state;
2. invalidate the scene generation and cancel publication handles for process-global asynchronous services;
3. drain and clean every produced result even when publication is suppressed;
4. keep process-global VFS/asset services alive for return to shell/login UI;
5. join global workers only during process teardown.

This closes the worker-retirement policy at architectural depth. It does not enumerate the exact ordering of every scene-change subscriber, and generation tokens remain a recommended modern safety mechanism rather than a claim about the legacy implementation.
