# STOP targets a playback thread/action, not the whole script (overnight block 10)

Five effective `STOP/0000` actions appear in reflect/ricochet scripts. Their four-byte INFO is two little-endian 16-bit integers: `(0,0)` in three instances and `(2,0)` in two. The executable registers `StopActionTemplate` at `0x00402BB0`; parser `0x00402D30` accepts `0000`, and `0x00402E60` stores the two integers at template `+8/+0xC`.

The template's action method `0x00402C90` calls `0x00408720`. That helper treats the two values as **thread index and action index**: `0x004085F0` indexes the instance's thread-action table by both, and `0x00408720` checks the thread's current action index against the requested target. If the current index has not passed it, the helper invokes virtual `+0x14` on the selected action and sets current index to target+1. Thus this action can stop/skip a targeted playback action in another thread; it does not inherently cancel the entire playback instance.

This matters for mirrored projectile/reflect choreography: threads can end a wait or visual subaction while other threads continue. The exact semantics of the selected action's virtual `+0x14` are action-specific (typically stop/cleanup in inspected implementations). No bounds checks are visible in `0x004085F0` itself, so a replacement parser should validate thread/action indices against the parsed template before execution rather than trust asset bytes or reproduce unsafe x86 indexing.

Provisional names: `0x00402E60` `parse_STOP_0000_thread_action_indices`; `0x004085F0` `lookup_playback_action_by_thread_and_index`; `0x00408720` `stop_and_advance_targeted_playback_action`.
