# Analysis checkpoint — CKAT asset messages to client-event routing

Target: verified `Stardust.exe`, 7,771 effective CKAT clips, and previously reconstructed skeletal-animation callback architecture. This connects *existing* analyses rather than assigning new semantics from message names alone.

The newly verified path is: `MSGS/MESG` asset name and occurrences -> CKAT template at `+0x5C` -> compressed-animation virtual message query `0x00822F30` -> track controller message enumeration `0x00830170` -> appearance listener fan-out -> `GameAnimationMessageCallback` `0x00623570` where its recognized prefixes can route to `ClientEventManager` and effects. Earlier checkpoints already established the controller/listener ordering and callback behavior; the new corpus audit anchors that route to actual CKAT records. This remains local presentation/event behavior, separate from incoming network event packets.

Of **6,952** deployed CKAT message groups, **3,129** have one of the six recognized callback prefixes: `event_` 2,675, `hpevent_` 55, `swing_` 112, `uswing_` 95, `hit_` 103, `uhit_` 89. **3,823** groups have other names; many are `fire*`. A recognized prefix confirms a candidate route, not that every occurrence fires a visible effect in all contexts. The exact consumers of every non-prefix message name remain unknown.

Evidence: current `CKAT_MSGS_DISK_CORPUS` and `CKAT_TIMED_MESSAGE_RUNTIME_BRIDGE` checkpoints; earlier `ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_APPEARANCE_ALTER_ORDER.md` and `ANALYSIS_CHECKPOINT_2026-09-21_CLIENT_EVENT_AND_ANIMATION_CALLBACKS.md`; ignored corpus probe. Confidence: confirmed for structural event path and prefix counts; tentative for individual effect outcomes.
