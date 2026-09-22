# Stardust client analysis session — through 07:00 checkpoint

Date: 2026-09-22  
Canonical target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Mode: static analysis and server-source correlation only

## Outcome

This continuation closed nineteen bounded research areas and reconciled nine previously staged reports into the canonical private workspace. It did not implement a replacement client, modify the executable or assets, publish anything to GitHub, or issue a shutdown command.

The implementation-grade estimate advances from **94% to 95%**. The expanded comprehensive whole-client estimate advances from **85% to 87%**. These are conservative planning estimates: they measure behavior sufficient to specify a compatible replacement, not the percentage of machine-code functions assigned historical source names.

The stable provisional-name registry now contains **2,034 distinct executable addresses with zero duplicate address rows**. There are **76 dated 2026-09-22 checkpoint reports**. The authoritative server packet-source inventory remains **345 documented / 0 reachable gaps**; no new server packet family was discovered in these UI-heavy blocks.

## Completed blocks

| Block | Principal result | Confidence |
|---|---|---|
| Stat migration workflow | UI request/response and local presentation bounded against existing migration messages | Confirmed/Strong |
| Image designer workflow | role-sensitive session, variable `ImageDesignData`, change/reject paths and terminal state bounded | Confirmed/Strong |
| Character sheet runtime | subscriptions, player guards, biography/badge/faction/money behavior and requests bounded | Confirmed |
| Permission list workflow | create/apply/edit behavior tied to permission-list messages | Confirmed |
| Mission details response | response consumption and mission-detail UI boundary closed | Confirmed |
| Documentation/unknown reconciliation | nine previously staged reports normalized into canonical ledgers | Confirmed |
| CuiConsent boundary | generic yes/no callback mediator separated from persistent server consent commands | Confirmed/Strong |
| Examine/inventory information | ordinary attribute request/cache route and specialized creature presentation bounded | Confirmed/Strong |
| Community UI composition | six child pages and matchmaking-table dependency mapped; root adds no protocol | Confirmed |
| Planet-map presentation | map assets, extents, coordinate conversion, markers, zoom/pan and settings bounded | Confirmed/Strong |
| Customer-service ticket UI | dependencies, validation, ticket limit, cooldown and create-message handoff mapped | Confirmed |
| Bug-submission UI | legacy report composer and optional ticket bridge bounded; obsolete routing excluded | Confirmed/Strong |
| Vehicle hover prototype | local developer-only twelve-parameter tuning panel; no network authority | Confirmed |
| CuiTransition overlay | paired-door close/wait/open state machine, tokens, geometry and sounds bounded | Confirmed |
| Ship diagnostic widgets | hull/armor/shield/capacitor/ammo/refire/threat/component display mapped | Confirmed/Strong |
| CuiInputBox callback boundary | one-shot text callback and waypoint-name adapter separated from protocol | Confirmed/Strong |
| CuiNotepad persistence | per-profile `notes.txt` load/save, marker and size caps bounded | Confirmed/Strong |
| CuiKeypad and CuiMoney | local one-shot keypad callback and reactive replicated-balance view bounded | Confirmed |
| Generic CuiTransfer | exact conserved-total arithmetic, rounding, clamps and UI lifecycle bounded | Confirmed |

## Architectural conclusions

1. Several message-like UI classes are presentation adapters, not protocol surfaces. Consent, transition, ship diagnostics, community root, vehicle tuning and generic input must remain decoupled from network code.
2. Original assets remain mandatory. Map page selection, mediator widget lookup, marker styles, localized strings and table-driven community behavior are asset-authored.
3. Object examination reuses the ordinary attribute-list service. A replacement should retain a shared asynchronous attribute cache/request API rather than invent an examine packet.
4. Customer-service behavior is server-capability dependent. Retain the codec and disable unavailable service cleanly; do not revive obsolete SOE beta destinations.
5. Ship diagnostic widgets provide useful acceptance-test oracles for replicated state before final UI work.

## Network/function mapping additions

| Server message / operation | Parser or adapter | Consumer | Result | Confidence |
|---|---|---|---|---|
| ordinary attribute-list response | mapped `AttributeListMessage` codec/cache | `SwgCuiInventoryInfo` | populates examined-object attributes | Confirmed |
| guild information service | previously mapped guild route | creature examine branch | augments creature details | Strong |
| `CreateTicketMessage` | established serializer | new-ticket and legacy bug forms | submits a ticket when enabled | Confirmed |
| persistent consent commands | command queue/server handlers | social state | durable consent mutation/query, distinct from `CuiConsent` | Confirmed |
| waypoint rename | established waypoint route | input-box waypoint adapter | applies accepted Unicode name to a verified waypoint | Strong |

No independent opcode was found for community-root composition, planet-map rendering, transition animation, ship diagnostics, vehicle tuning or the generic text prompt.

## Remaining work

The remaining **5% implementation-grade** work is spread across long-tail optional UI/gameplay behavior, rare-path ownership and shutdown auditing, rare asset-version behavior, and integration cross-checks joining already decoded network, replication, asset and session components.

The remaining **13% comprehensive** breadth is larger because it also includes optional tools, dead/legacy paths, exhaustive naming and presentation details not all required for replacement compatibility. Unknowns still must be retired where behavior matters; recovery of every historical identifier is not itself a compatibility requirement.

## Closing integrity audit

- All 423 top-level Markdown documents in the private canonical workspace were
  scanned: **0 broken relative document links**.
- No `stage_*` files remain in the canonical workspace.
- The chronological unknown-function tables contain 1,110 address rows / 1,079
  primary addresses; 1,072 rows cite Confirmed evidence, 54 cite Strong evidence,
  none cite Tentative, and the counts overlap.
- The sole table row containing the word Unknown is behaviorally Confirmed
  transport setup; only historical labels for unused selector values remain.
- Packet-map uses of “unknown” are predominantly preserved legacy field labels,
  dormant unsupported messages, or explicitly bounded server/client omissions;
  they are not silently treated as decoded semantics.

## Recommended next direction

Use integration-boundary sweeps rather than arbitrary adjacent functions: trace remaining material state transitions from server/replication input through manager state into presentation, then audit teardown/ownership and original-asset failure policy. Engine prototyping remains deferred until the requested complete-picture analysis threshold is reached.
