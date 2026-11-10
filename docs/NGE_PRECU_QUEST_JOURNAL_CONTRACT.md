# Original quest journal compatibility boundary

October 14, 2026. Question: which installed page, original definition columns and replicated state must a source-derived client consume to open the Pre-CU journal correctly?

## Input identity and direct evidence

The original Stardust.exe SHA-256 is reverified as E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150. Addresses below are VAs for that executable. Static probes use the existing disasm_range.py and raw-dword-reference tools; original asset extraction follows effective TRE precedence. Prior [manager](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_QUEST_JOURNAL_MANAGER.md) and [screen](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_QUEST_JOURNAL_UI_RECONCILIATION.md) investigations remain the lifecycle foundation.

Original constructor 00DB3B10 binds checkShowMission, descQuest, counterSample, listCategories, treeQuests, buttonOK and descriptionComp. Both counter-sample lookups use the same CodeData key; they do not establish two separately authored sample widgets. Activation/deactivation 00DB4080/00DB4150 manage local-player subscriptions; refresh 00DB43E0 delegates category/tree work to 00DB4A70 and description to 00DB4410. Selection and Done use 00DB5550/00DB5590. No direct network send was observed in these inspected paths.

The effective embedded /GroundHUD.Quest page comes from ui/ui_pda.inc, priority 53 stardust_3_01.tre: 273,589 bytes, SHA-256 7A0F01D740FE2A13344909EEDBD5C9077A5656F099839F7F665BAB5B977E5C64. Its checkbox label is Include Completed Quests; original tree queries pass the inverse checked state as active-only. The separate ui/ui_pda_quest.inc at priority 46 is not the embedded page being bound. Using that standalone file to infer available controls incorrectly suggests NGE buttonAbandon exists in the original page.

## Resolved seven-column schema

Catalog builder 0049B400 resolves initialized globals and constructs 0x50-byte records, retaining numeric row identity and resolving parent names in a second pass. Direct global initializer references now resolve the previously unknown names:

| Column | Global VA | Initializer VA | Retained record offset |
|---|---|---|---|
| CATEGORY | 0190CD88 | 0049A550 | +34 |
| JOURNAL_ENTRY_SUMMARY | 0190CD7C | 0049A5A0 | +1C |
| JOURNAL_ENTRY_DETAILED | 0190CD70 | 0049A5F0 | +28 |
| NAME | 0190CD64 | 0049A640 | +04 |
| PARENT | 0190CD58 | 0049A690 | +10 |
| IS_VISIBLE | 0190CD4C | 0049A6E0 | +40 |
| VISUAL_ORDER | 0190CD40 | 0049A730 | +44 |

The effective datatables/player/quests.iff is priority 42 mtg_patch_013_configurable_02.tre: 42,810 bytes, SHA-256 0135FD981835207840425C48EFE7D5CA7C4ED62BAF9A143E152A36B3446D5A99. It has 188 rows, 188 unique names, 49 roots and 156 visible definitions; zero missing parents and zero parent-cycle rows. Its 13 columns also include ATTACH_SCRIPT, TASK_ON_COMPLETE, TASK_ON_FAIL, TARGET, PARAMETER and SHOW_SYSTEM_MESSAGES. Those additional script/gameplay fields are outside the seven-column journal projection, not evidence that server behavior can be discarded.

Keep definitions immutable. Replicated active/completed bits and quest-library tasks select the presentation; transient overlays remain separate. Original root query 0049C140 and descendant predicate 0049C760 include visible active/completed definitions and relevant ancestors. Original sorting adds 1,000,000 to active order before ascending sort. A replacement can use a wider composite key without reproducing legacy overflow.

## Replicated bit boundary

Original isQuestActive 00660A90 and isQuestComplete 00660AB0 use object members +644 and +610 respectively and common bit lookup 00AA6BD0. Negative indexes and byte indexes beyond the received vector return false; byte index is index >> 3, mask is 1 << (index & 7). Local Core3 DeltaBitArray independently uses the same least-significant-bit-first byte layout. PlayerObjectMessage8 places completed and active byte vectors in PLAY8 fields 4/5 before quest-library state.

The source conversion already decodes these byte vectors but previously queried unpopulated NGE BitArray members and did not emit their typed change notifications. The Pre-CU profile must query the received vectors and publish the existing ActiveQuestsChanged/CompletedQuestsChanged events after a successful baseline/delta operation. Do not claim atomic rollback of an AutoDeltaVector partially modified by malformed input merely because no callback fires.

## Implementation consequence and acceptance limits

The sibling source repository implements an engine-neutral definition catalog and bit accessor, a validated DataTable adapter, profile-specific player getters/events and a separate read-only original-page mediator. NGE journal routing remains available outside the Pre-CU HUD profile. The adapted view localizes labels, retains canonical identity and hierarchy, merges quest-library rows, hides authored samples and balances active subscriptions/pointer requests. Its strict empty/duplicate-name, missing-parent and cycle rejection is implementation policy; the original missing-parent path reports a diagnostic and is not proven to reject the whole table.

Release executable FC8E65918D139A5A7B0B1687EAFCEAEE16188E27D0C73A2F1819EA64C8273705 builds successfully. Native suite passes 12,136 checks and 3,761 malformed rejections plus five isolated CRC/ACST cases. New fixtures cover bit boundaries, baseline/delta notifications, ten truncated byte-vector baselines, active/completed ancestor projection, catalog rejection with prior-state preservation, and the actual 188-row table. These do not instantiate a complete live PlayerObject or establish populated UI parity.

Authorized stationary computer use observes world entry, journal open, Include Completed toggling/restoration, Done and close controls, reopening, normal Disconnect/re-entry and journal reconstruction without a new failure. Count-only receipts show catalog 188, projected rows 0, active bytes 0 and completed bytes 0 for this test. Empty presentation agrees with received state; populated hierarchy, quest-library localization, changing progress and dynamic counter/timer display remain unaccepted. No quest acceptance, abandonment, reward or deliberate gameplay mutation is performed.

Confirmed: original controls, resolved columns, corpus identity/counts, byte-bit lookup, source state/event gap, native bounded checks and empty-shell live lifecycle. Strongly inferred: complete category and hierarchy presentation fidelity. Unknown: populated live parity and whether current server content exercises the mismatched counter/timer helpers. No coverage denominator or alias/packet count changes.

Next probe: compare current source controller registration and Core3 counter/timer payload fields with the original codec, keeping explicit compatibility profiles. The existing 0x440/0x441/0x444 discrepancy must not be hidden by UI changes.
