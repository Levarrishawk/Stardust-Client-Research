# Analysis checkpoint: quest counter/timer reachability

Date: 2026-09-22  
Scope: confirmed controller-ID mismatch correlated with current Stardust content call graph

## Result

The `0x440/0x441/0x444` quest counter/timer mismatch remains a real codec incompatibility, but it is not presently reached by active shipped Stardust quest content.

## Source reachability

The only construction path for the two server messages is:

```text
Lua screenplay
  -> Journal.count / Journal.timer
  -> LuaPlayerObject::sendQuestTaskCounter / sendQuestTaskTimer
  -> QuestTaskCounterMessage / QuestTaskTimerMessage
```

Repository-wide results:

- `Journal.timer(...)` has no caller outside its own definition.
- `sendQuestTaskTimer(...)` has no caller outside `Journal.timer` and its binding/implementation.
- `Journal.count(...)` is callable only through `JournalMirror`'s optional `entry.count` operation.
- None of the current Mustafar journal maps defines an exact `count = {...}` member.
- No other native message producer exists.

The many live `JournalMirror.applyStage` calls update begin/task/finish journal bits, but do not currently emit the incompatible counter packet.

## Reconnect behavior

The registered `requestQuestTimersAndCounters` command currently validates player state/locomotion and returns success without resending anything. That would be insufficient if transient counters/timers became live, but it has no observable missing overlay under the current no-producer content set.

## Compatibility decision

For Stardust's present content, the replacement client can safely implement all three IDs as versioned codecs without needing to select one immediately:

| Profile | Counter | Timer |
|---|---:|---:|
| historical executable | `0x440` | `0x441` |
| current Stardust definitions | `0x441` (long body) | `0x444` |

Parser selection must include negotiated/profile context or exact body schema; dispatching `0x441` blindly is unsafe because it means two different messages. Supporting Stardust's current pair is recommended for future content, while exact original-client compatibility requires correcting or versioning the server definitions.

## Status

- codec mismatch: **Confirmed**;
- current active-content reachability: **Confirmed absent by static call/content search**;
- future risk if scripts begin using counters/timers: **High unless profile decision is implemented**;
- production runtime usage via dynamically loaded out-of-tree scripts: **Not provable from this repository**.

