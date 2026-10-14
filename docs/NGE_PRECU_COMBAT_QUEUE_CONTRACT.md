# Pre-CU combat queue view in the source client

Date: October 14, 2026. This is a bounded view over the existing authoritative command model; row animations and gameplay acceptance remain open.

## Evidence and confidence

Original input: Stardust.exe SHA-256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150. The [original queue investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_COMBAT_QUEUE_UI.md) confirms constructor 00F503A0 binds VolumePage, sampleitem, clearbutton, peaceattackbutton and targetnametext, with typed queue/state subscriptions. Clear at 00F517B0 enumerates eligible sequences and reaches 0046E650: cancellation sender 0062CBD0, then local remove 0046E720. These are direct executable observations, **Confirmed**, rather than a second speculative queue.

Source correlation: SD-Src-Client src/engine/client/library/clientGame/src/shared/command/ClientCommandQueue.cpp owns entries/results; src/engine/client/library/clientGame/src/shared/controller/PlayerCreatureController.cpp::sendCommandQueueRemove emits the existing CM 0x117 cancellation envelope. The current source's aggregate clear has NGE primary-queue policy and is deliberately not the Pre-CU clear implementation. Server authority is unchanged.

## Implementation boundary

Engine-neutral PrecuCombatQueueProfile.h projects owned sequence/command/target rows from combat-eligible entries. It preserves input iteration order and all U32 identifier bits, filters empty/noncombat entries and deduplicates sequence IDs without retaining source pointers. Source SwgCuiCombatQueue supplies the UI adapter and existing controller sink. Added/Removing/StatesChanged/LookAtTargetChanged callbacks only mark the view dirty; a bounded periodic refresh resolves the current model. It clones the original hidden sample row, supplies localized command labels/icon/drag metadata and replaces the three visible authored placeholders.

The page is hidden for an empty idle queue while the mediator stays active. Combat state comes from the existing creature state-based CuiCombatManager; target presentation follows its resolved look target. Clear takes a fresh owned snapshot, rechecks identity/eligibility before each send, and removes locally only if the same row still exists afterward. A concurrent authoritative removal becomes a no-op; a changed row is preserved. A failed send does not preemptively remove that row. Attack/Peace delegates existing commands. These are modern ownership/idempotence policies implementing the observed boundary, not claims of identical native container layout.

Ground HUD owns/fetches/releases the mediator only with ClientGame.usePrecuHud. Deactivation disconnects typed subscriptions, disables update and discards owned presentation rows before release. Neither decompiler pseudocode nor proprietary UI bytes are added to curated data.

## Validation and acceptance

Native fixtures cover filtering, ownership, duplicate IDs, full-width IDs, send-before-remove, repeated cancellation, stale/replaced entries, server removal during send and failed-send preservation. The combat-queue increment passes 11,454 native checks / 3,538 malformed-input rejections plus five isolated CRC/ACST cases; the subsequent radar increment adds layout checks without changing this queue boundary.

Release build SHA-256 1AE82D48E878E81121B6683884AB11558DD32E2AB4C6A40F0101A9EB5C5D3EEC enters the existing stationary character through authorized computer use. The three XXX sample rows disappear and the empty queue is hidden. Its fifteen-minute code-only probe detaches without a client exception. This is **Confirmed idle UI integration**, not full endurance or gameplay acceptance. No attack, peace or clear action is sent in this bounded probe.

Queued live rows, authoritative result transitions, individual row cancellation, progress/completion animations and later toolbar cooldown presentation remain unaccepted. Exact next queue probe: an explicitly scoped combat scenario that observes enqueue/result/cancel state, using disposable server state and preserving authoritative outcomes.
