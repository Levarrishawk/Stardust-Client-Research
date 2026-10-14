# Pre-CU ability-to-command availability bridge

Date: October14,2026. Source-client boundary, separate from command execution and skill progression.

## Evidence

[Replication specification](REPLICATION_SCHEMA_MAP.md) confirms PLAY9[0] as an indexed ASCII ability vector: snapshot00661E20, delta00662030. Core3 PlayerObjectMessage9 serializes AbilityList first; AbilityList supplies names including certifications. Original player setup at0065C3FB..0065C430 binds aggregate0065D200, erase0065D4F0, insert0065D5F0 and set0065D6F0 to that vector. Small probes establish vector iteration and notification/conversion paths; they do not prove every old command-cache rule. Original SHA256 is reverified in the [daytime checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md).

NGE source PlayerObject's Pre-CU registration correctly decodes m_precuAbilities but had no semantic consumer. CreatureObject::getCommands returned its NGE-only m_commands, skipped in the Pre-CU wire registry. SwgCuiCommandBrowser::reset reads CreatureObject::getCommands; the read-only live browser displayed only global Attack/Peace. **Confirmed source disconnect and observed UI symptom**, not proof that the selected account has every expected command or that all empty toolbar slots share this cause.

## Modern adapter policy

Engine-independent PrecuCommandProjection preserves exact wire spelling and the vector. It projects each distinct string to one availability entry. Duplicates preserve presence until the last occurrence is removed. The merged view keeps existing local command values and adds missing server-granted names with value1. This is a deliberate availability union, not recovered legacy reference-count equivalence. No casing, trimming, command-table filtering, fabricated grants or network sends occur.

The PlayerObject callback publishes the new projection before owner notification; setPlayerSpecificCallbacks also refreshes for baseline/owner ordering. CreatureObject keeps the derived map and emits the existing aggregate and added/removed transceivers for actual membership transitions. Local contributions prevent false removal when a replicated name disappears. Unchanged derived maps retain their storage. NGE profile false retains its prior getter/map path.

The existing Archive container mutates and notifies incrementally during multi-command deltas. This bridge does not introduce a full-message transaction or claim rollback of all prior changes on a later malformed command. Invalid-payload cleanup and full atomic replication remain separate work. Commands, drafts, quests and skills have separate identities; this availability bridge does not complete crafting, quests, training or ability execution.

## Validation and exact next gate

Native fixtures use production AutoDeltaVector snapshot, indexed add/remove, replacement and clear framing with the actual projection helper, including duplicate membership, exact case/whitespace and local contribution retention. Complete world-object/callback lifetimes and persistent toolbar side effects still require live validation. Next launch: same existing character, remain stationary, open the command browser and confirm the server-provided list reaches the UI without executing an ability. Build/test/live receipts belong in the daytime checkpoint.


## Count-only category probe prepared, October14

The raw/projected model receipt already confirms82 names and attached owner. The command browser also appends global visible commands, so the populated Other list does not establish owned grants. Static effective command-table inspection finds771 rows, including valid visible combat groups; no filter change follows from this alone. A capped optional trace now reports owned/matched/visible/combat/page/localized counts for source browser rebuilds, without names, IDs or payload content. Live receipt remains pending; do not fabricate commands or alter grants in response to the Attack/Peace-only observation.


Live CC876358 category receipt: both tabs see82 owned model names,10 command-table matches and10 visible matches, with0 classified combat matches. Combat has0 owned page candidates; Other has10 page candidates and10 successful labels. This establishes UI projection/localization for those10 names, not completeness of every grant, the meaning of the72 unmatched names, or gameplay execution. No category/grant change follows.
