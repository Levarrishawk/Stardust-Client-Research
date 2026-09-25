# Non-prefix CKAT messages: subscriber scope and ordinary-gameplay behavior

Date: 2026-09-25. Question A04 / W08. Target: verified original `C:\Stardust-DEV\Stardust.exe` and prior 7,771-clip message corpus. This block classifies dispatch scope; it does not assign effects from message names.

## Result

The 3,823 CKAT message groups outside the six `GameAnimationMessageCallback` prefixes do not have a generic ordinary-gameplay effect route in this executable. The shared skeletal-appearance listener registration function `0x007D79F0` has exactly two analyzed direct call sites:

1. `0x00623530` installs the ordinary `GameAnimationMessageCallback` (`0x00623570`), which recognizes only `event_`, `hpevent_`, `swing_`, `uswing_`, `hit_`, and `uhit_` families and otherwise returns without routing a client event.
2. `0x0139E040` installs callback `0x0139C870` for the local `scene/intro.iff` controller. It strips an underscore prefix and updates intro-local current-name/CRC state; it is not a universal gameplay subscriber.

Ghidra's reference graph and the prior independent raw `E8 rel32` scan agree on those two registration calls. `0x00830170` synchronously fans each emitted name only to the registered callback/context vector; it contains no fallback name interpreter. Therefore names such as the common `fire1`, `fire3`, and other non-prefix groups are retained asset metadata but are inert in ordinary gameplay unless a scoped controller registers a distinct listener. No such third registration is present in the executable's direct call graph.

This conclusion is intentionally narrower than “the strings are meaningless.” They may be authoring leftovers, content intended for another client build, or names used by the intro controller when its selected assets satisfy that controller's naming convention. A replacement importer must preserve them and expose them through the animation event stream, while its default gameplay adapter should ignore unknown names rather than invent effects from English-looking labels.

## Replacement contract

- Dispatch all authored names to the animation layer in deterministic order.
- The ordinary SWG gameplay adapter handles only the six confirmed prefix grammars.
- Unknown names are nonfatal and ignored unless a scoped subsystem explicitly subscribes.
- Keep the intro-scene listener separate and validate the underscore delimiter; do not reproduce its unbounded scan.
- Do not infer weapon fire, sound, or particles from bare `fireN` names without a registered consumer.

## Confidence and outcome

Confirmed: listener-vector fan-out, two direct registration sites, six-prefix ordinary callback, and intro-local state sink. Strongly inferred: non-prefix groups have no ordinary-gameplay effect in this executable. Unknown: dynamically generated code or an external module registering through an unobserved indirect pointer; no evidence for such a route was found.

Outcome: **A04 closed for the executable's ordinary gameplay subscriber model.** Intro-scene asset selection remains a scoped optional-content question, not a blocker for gameplay animation compatibility.
