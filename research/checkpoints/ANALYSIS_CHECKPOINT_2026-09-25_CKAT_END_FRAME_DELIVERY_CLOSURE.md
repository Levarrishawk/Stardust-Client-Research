# CKAT end-frame message delivery: loop/reset/replacement closure

Date: 2026-09-25. Question A02 / W08. Target: verified original `C:\Stardust-DEV\Stardust.exe`. This block extends the prior half-open collector and physical-track work; it does not recount the 1,517 installed end-frame occurrences.

## Result

For the standard CKAT physical-track path, events authored exactly at the INFO-declared end frame are **not delivered** on ordinary completion, looping reset, or animation replacement. The outgoing animation is queried once with its current frame capped to the declared end. CKAT delegates that interval to the already-proven half-open `[previous,current)` collector, excluding the endpoint. The track then either installs the returned replacement or resets the same looping animation; neither branch performs an inclusive endpoint query.

## Executable path

`0x0083E630` is the common physical-track advance used by the direct wrapper `0x0082F940` and both composite callers `0x0083D890` and `0x0083E0F0`. It calls the active animation's advance slot, then—when message delivery is enabled—calls the sole direct event enumeration routine `0x00830170` before replacement/reset handling.

For CKAT:

- advance slot `0x00822C40` caps current playback at the declared end and returns overrun time;
- message slot `0x00822F30` calls `0x00825610`, whose sole direct collector is `0x008245D0` and whose interval remains half-open;
- reset slot `0x00822D30` writes zero to previous frame `+0x14`, current frame `+0x10`, and elapsed time `+0x18` only. It emits no message and performs no endpoint query;
- replacement is installed only after the outgoing animation's ordinary message enumeration; no second query occurs;
- a non-progress/termination branch can leave before enumeration, which cannot rescue an endpoint event.

The Ghidra reference audit finds:

- five direct calls to `0x0083E630`, all in the three already identified wrapper/composite functions;
- one direct call to `0x00830170`, from `0x0083E630`;
- `0x00822F30` referenced as the CKAT vtable message slot and directly calling only `0x00825610`;
- `0x00825610` directly calling only `0x008245D0`;
- `0x00822D30` referenced as the CKAT vtable reset slot and no code caller outside virtual dispatch.

This does not prove that no debug tool or entirely separate subsystem can manually inspect CKAT message tables. It does close the normal installed animation playback contract represented by the physical track and its single/composite wrappers. No alternative executable dispatch path to the CKAT message collector was found.

## Replacement consequence

Preserve disk events, including endpoint records, but model original playback intervals explicitly. Compatibility mode should not silently emit an event merely because its authored frame equals clip end. A modern engine may optionally diagnose these retained-but-unfired records. If later live differential testing contradicts the static path, record the exact animation/controller responsible rather than globally changing interval semantics.

## Confidence and outcome

- **Confirmed by code:** reset only zeros time/frame state; message query precedes replacement/reset; standard query is half-open; observed direct physical-track callers and collector chain are exhaustive in Ghidra's direct-reference graph.
- **Strongly inferred:** installed CKAT endpoint events are intentionally or historically unreachable through ordinary physical-track playback.
- **Unknown but non-blocking:** whether external debug/editor code can query an endpoint record manually.

Outcome: **A02 closed for replacement-relevant standard playback.** This is a substantive behavioral closure, not a new corpus or address-count block.
