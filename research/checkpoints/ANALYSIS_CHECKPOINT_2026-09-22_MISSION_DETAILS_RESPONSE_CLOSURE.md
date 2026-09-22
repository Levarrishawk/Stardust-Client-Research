# Mission details response closure

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The dormant object-controller `0xF8` mission-details response does **not** carry
an undiscovered details record. `MessageQueueMissionDetailsResponse` is allocated
from a dedicated fixed-size pool with element size `0x0C`; its only derived payload
field read anywhere in the publication path is the U8 request sequence at object
offset `+0x08`. Matching responses publish a trigger event; actual mission details
remain on the replicated `ClientMissionObject` selected by the browser.

## Evidence

- `0x00B16630` constructs the named
  `MessageQueueMissionDetailsResponse memoryBlockManager` with object size `0x0C`.
- `mission_details_response_publish_VA_009788B0` compares byte `+0x08` with the
  current request sequence, clears pending state, and publishes the object through
  `CuiMissionManager::Messages::ReceiveDetails`.
- On mismatch it logs `discarding stale mission details` and performs no publish.
- No room remains for an independent variable-sized details envelope after the
  base/vtable and sequence state, and the browser already owns the selected
  replicated mission object.

## Replacement contract

Treat controller `0xF8` as a sequence-correlated completion signal:

```
U8 sequence
```

The exact outer object-controller scalar and flags remain governed by the common
controller envelope. On a matching sequence, clear details-pending UI state and
refresh from the selected replicated `MISO` object. On mismatch, discard without
altering the current selection. Current Stardust emits no `0xF8`, so this remains
an optional legacy compatibility extension rather than a live server dependency.

## Confidence

**Confirmed for this executable:** fixed allocation size, sole derived field,
sequence guard and resulting event. The historical class name is preserved by
RTTI. No unknown details payload remains.

