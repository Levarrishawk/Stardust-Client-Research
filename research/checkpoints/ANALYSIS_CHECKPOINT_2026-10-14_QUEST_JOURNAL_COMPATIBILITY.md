# Original journal page, catalog and replicated-bit reconciliation

October 14, 2026. Bounded question: why does opening the original quest journal request missing NGE buttonAbandon, and which data/state bindings restore the original screen? Stop condition: establish effective page/schema/bit behavior, validate a bounded source adapter and observe its read-only empty-state lifecycle.

Input identity, direct VAs, effective archive hashes/counts, confidence and implementation consequences are consolidated in the [durable journal contract](../../docs/NGE_PRECU_QUEST_JOURNAL_CONTRACT.md). Existing September manager/screen investigations supplied leads; initialized-global references and bit-lookup instructions were checked against the rehashed original executable before implementation.

Observation: the live source client reports missing buttonAbandon under /GroundHUD.Quest. The actual embedded page uses the original seven controls, despite a separate newer standalone quest include in the overlay. Interpretation: source mediator/page mismatch, Confirmed. Consequence: bind the original page through a profile-specific view rather than hiding the journal.

Observation: seven column globals resolve to CATEGORY, JOURNAL_ENTRY_SUMMARY, JOURNAL_ENTRY_DETAILED, NAME, PARENT, IS_VISIBLE and VISUAL_ORDER. The effective table has 188 unique rows/49 roots/156 visible entries and no missing/cyclic parent links. Interpretation: immutable original definition hierarchy with row-index progress identity, Confirmed. Consequence: load the real schema and keep progress separate; stricter catalog rejection remains implementation policy.

Observation: original active/completed getters use an LSB-first bounded byte-vector lookup; the adapted source receives byte vectors but formerly queries unused NGE bits. Interpretation: missing profile-specific state query/event wiring, Confirmed. Consequence: use received bits and typed successful-update notifications, without asserting malformed-vector rollback.

Validation: full Release build FC8E65918D139A5A7B0B1687EAFCEAEE16188E27D0C73A2F1819EA64C8273705; native 12,136 checks/3,761 malformed rejections plus five isolated cases. One initial offline corpus attempt failed because TreeFile was not installed; the corrected harness reads explicit bytes into Iff and passes. Live read-only opening, checkbox, Done/close, reopen, Disconnect/re-entry and reconstructed empty journal pass. Count-only receipt 188 definitions/zero projected rows/zero received active and completed bytes establishes empty-state acceptance only.

No proprietary assets or generated client binaries enter the research tree or Git. No staging/publication, alias, packet-row or completion denominator changes. Required next probe: explicitly reconcile current Core3 counter/timer fields and IDs; populated quests and changing-state presentation need appropriate naturally received state before live acceptance.

## Current Core3 overlay increment

The [current Stardust overlay contract](../../docs/STARDUST_QUEST_OVERLAY_CONTRACT.md) distinguishes matching NGE/current IDs from mismatched fields. Counter task identity is absent on the wire; the adapter explicitly uses quest-wide local sentinel -1 rather than fabricating a task. Timer seconds receive relative countdown treatment; scene teardown retires transient overlays. ReleaseE18EBB28 passes; native12,280/3,887 plus five isolated cases passes actual registered envelopes and malformed inputs. The initial missing-include build failure and removed graphics-coupled cache test are recorded, not counted as successful checks. Stationary entry/journal/Disconnect/re-entry passes, but no naturally received overlay or populated quest state is accepted. No matrix/coverage/publication changes.
