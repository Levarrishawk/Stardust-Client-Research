# Current Stardust quest overlay profile

October 14, 2026. This contract separates original executable behavior from the current Core3 boundary. The [original journal contract](NGE_PRECU_QUEST_JOURNAL_CONTRACT.md) covers definitions, player progress and the screen lifecycle.

## Observed producers and codecs

Original baseline SHA-256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 registers counter 0x440 at VA 00B0F3B6, factory 00B0F6F0, and timer 0x441 at 00B0EDA6, factory 00B0F0D0. The original/server discrepancy remains true for Stardust.exe. Current source GameControllerMessage.def instead assigns CM_questTaskCounterData = 1089 (0x441) and CM_questTaskTimerData = 1092 (0x444), matching Core3's IDs. Matching IDs do not establish matching field meaning.

| Current Core3 message | Payload after object-controller scalar | NGE source interpretation before adaptation |
|---|---|---|
| 0x441 counter | Archive narrow quest; LE I32 maximum; Unicode32 label; LE I32 zero; LE I32 current | quest; task; label; current; maximum |
| 0x444 timer | Archive narrow quest; LE I32 task; Unicode32 label; LE I32 seconds | same fields, but final value treated by manager as absolute player-played-time deadline |

Direct local source: MMOCoreORB/src/server/zone/packets/object/QuestTaskCounterMessage.h constructor; QuestTaskTimerMessage.h constructor; src/server/zone/objects/player/LuaPlayerObject.cpp::sendQuestTaskCounter/sendQuestTaskTimer; bin/scripts/managers/quest/journal.lua::Journal.count/Journal.timer. Counter taskId is constructor-only and explicitly not serialized. Timer's helper accepts seconds and serializes it directly. This is confirmed local source behavior, not verification of the deployed server revision or naturally received packets.

NGE source correlation: sharedNetworkMessages MessageQueueQuestTaskCounterMessage::unpack/pack uses task,current,maximum; timer codec uses task,timerLength. PlayerCreatureController passes those fields into QuestJournalManager. The latter subtracts PlayerObject::getPlayedTime from the final value at receipt and update. This would swap counter semantics and prematurely expire a relative timer after sufficient played time.

## Explicit compatibility consequence

The source project's usePrecuReplication profile selects current Stardust decoding at these registered IDs. The engine-neutral StardustQuestTaskProfile owns parsed strings/scalars, requires the observed counter reserved value to be zero and rejects trailing bytes/truncation before allocating a message. Signed scalar bits are preserved. Negative/zero received timer seconds mean zero remaining duration; nonnegative elapsed updates count down to zero and negative/NaN elapsed values cannot increase or poison the duration. These rejection and clamp rules are explicit implementation policies.

Core3's omitted task identity cannot be reconstructed. The adapter represents it with local task sentinel -1, keeps current/maximum under the canonical quest CRC and label, and presents the counter at quest level. It does not pretend that the zero field is the sender's task ID. A pack request carrying a known task is rejected before writing, since this server grammar cannot represent it. Task-specific fidelity requires a future agreed server/profile extension, not a guessed client field. Timers retain their actual transmitted task identity.

NGE decoding, packing and played-time semantics remain on the false profile path. Do not register original 0x441 timer and current 0x441 counter together or select a codec by server class name. Scene teardown clears transient counters, timers, locations and pending expiry iterators for the Stardust profile; immutable catalogs and replicated authoritative progress remain separate.

## Validation and limits

Full Release build E18EBB2897E834028F03A7173084E482C5E31B9E10E7B8C6AFEEB588819C1FA6 passes after correcting a missing ConfigFile include. Native suite passes 12,280 checks/3,887 malformed rejections plus five isolated CRC/ACST cases. Added tests construct independent current-server envelopes, use the actual registered 0x441/0x444 factory route, test every truncated prefix, Unicode and signed scalar preservation, zero reserved-field enforcement, extra-tail rejection, unsupported task-preserving pack rejection, duration expiry/negative/NaN behavior and NGE counter round-trip. An attempted direct manager cache test pulled Game/graphics dependencies into the offline harness and failed before execution; it was removed. No native full-manager/graphics integration acceptance is claimed.

Bounded live computer use on the unchanged known test character observes login, stationary world, original journal, Done, normal Disconnect and world re-entry with the teardown correction. The character has empty quest state; no counter/timer packet occurrence or populated overlay is accepted by this run. The prior [reachability checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_QUEST_COUNTER_TIMER_REACHABILITY.md) found these helpers dormant in shipped content, and out-of-tree runtime scripts remain unknown.

Confirmed: original/current registration distinction, inspected producer/consumer field mismatch, explicit source bridge and native registered-codec checks. Strongly inferred: current seconds parameter intends receipt-relative countdown. Unknown: actual runtime emission, reconnect resends, populated live presentation and task-specific counter recovery. No new packet schema row or coverage denominator follows; both existing matrix rows remain documented original/server discrepancies.

Next: validate naturally received populated quests and changing overlays when an authorized character/content case exists. Independently continue read-only UI and rendering compatibility; do not mutate server content or a character merely to manufacture acceptance evidence.
