# Analysis checkpoint — space-conversation presentation and response selection

Question A09 / W10+W04. The four ObjectController conversation messages and server `npcConversationSelect` policy were already mapped in [NPC conversation flow](ANALYSIS_CHECKPOINT_2026-09-12_NPC_CONVERSATION_FLOW.md); this block covers the previously unvisited `SwgCuiSpaceConversation` presentation boundary only. Target is the verified original `C:\Stardust-DEV\Stardust.exe`. Decompiled C is reconstructed pseudocode.

RTTI exposes `SwgCuiSpaceConversation` vtables `0x015F2834` and `0x015F28B8`. Constructor `0x00DC4270` binds `viewer`, `buttons`, `viewerpage`, and `textpage`, creates a per-mediator transceiver owner, and connects typed `CuiConversationManager` `TargetChanged`, `ResponsesChanged`, and `ConversationEnded` notifications to callbacks `0x00DC48E0`, `0x00DC49A0`, and `0x00DC4990`. Destructor `0x00DC45C0` disconnects those exact owned transceivers, releases displayed response entries and the retained presentation object. This is an explicit lifetime boundary; subscriptions must not survive mediator teardown.

On target change, `0x00DC48E0` reads current conversation target and mobile CRC, compares retained target state, and if changed calls `0x00DC49B0`, clears the choice UI and resets presentation timing. `0x00DC49B0` resolves a suitable scene/presentation object, can retain a source appearance, tests a `face_focus` animation, and configures the space communicator/viewer. Its many virtual scene-object calls make the exact camera/appearance ownership partial; the existence of a separate space presentation path is confirmed.

On response change, callback `0x00DC49A0` reaches `0x00DC4ED0`. The latter rebuilds button items from the manager's current response vector (`DAT_0192B8F0..F4`, twelve-byte records), prefixes them with zero-based numeric identifiers, adjusts the control/page geometry, and requests layout refresh. `0x00DC4D70` checks whether displayed responses diverge and refreshes them during mediator update; it also advances a timing/fade state. A widget action at `0x00DC5200` checks the event source and selection index: an out-of-range index calls `0x008E8370` (manager stop request) and the ended callback; a valid index calls `0x008E7280` with the current target ID and selected zero-based index. The manager method validates matching current target, active conversation, range and player state, formats the index as Unicode decimal, and queues the already documented `npcConversationSelect` command. Thus the *new* UI path converges on the known command, not a new opcode. `ConversationEnded` callback `0x00DC4990` closes the presentation through `0x009C2620`.

```text
ObjectController 0xDD/0xDF/0xE0 (already mapped)
  -> CuiConversationManager target/text/responses
  -> SwgCuiSpaceConversation typed subscriptions
  -> viewer + numbered response controls
  -> selection 0x00DC5200 -> 0x008E7280
  -> queued npcConversationSelect(index) (already mapped)
  -> ObjectController 0xDE / ConversationEnded -> presentation close
```

Evidence: constructor/destructor `0x00DC4270/0x00DC45C0`, callback instructions `0x00DC48E0/0x00DC49A0/0x00DC4990`, target binding `0x00DC49B0`, response rebuild `0x00DC4ED0`, update `0x00DC4D70`, selection `0x00DC5200`, manager validation/send `0x008E7280` and stop `0x008E8370`, prior server correlation. Confidence: **confirmed by code** for subscription, widget bindings, response numbering, valid-selection convergence and teardown; **partial** for camera/viewer animation details and exact timing/fade appearance. No new packet row and no whole-client percentage change.
