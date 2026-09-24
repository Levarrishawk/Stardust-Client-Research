# Support screens: Holocron and server-disconnect presentation

## Scope and evidence

Static inspection of the original `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`) and read-only correlation with the current Stardust-3 command implementation. The executable pseudocode is reconstructed, not source. This is two small UI boundaries, **not** a new transport or packet-codec discovery.

## Holocron help mediator

RTTI identifies `SwgCuiHolocron` and two vtables at `0x015F18D4` (callback face) and `0x015F1958` (mediator face). Constructor `0x00D550B0` installs both, binds `buttonBack`, `buttonNext`, `buttonBug`, `buttonReportPlayer`, `buttonServices`, `buttonreload`, `treeTopics`, `entrycomp`, `textsample`, `imagesample`, `buttonsample`, and `entrytitle`, and populates a topic tree via `0x00D55810`. The binding names and calls confirm that this Holocron is a **help/knowledge-base UI**, not the Jedi holocron inventory item found in server Lua.

The callback at `0x00D560F0` branches on those control pointers. Back/Next consume retained navigation records; reload invokes the topic rebuilding path; Bug and Report Player open other UI mediators; Services checks service state before opening another view. Selecting a tree item resolves its path/text and updates the displayed article (`0x00D56650`); one article-action branch constructs literal `executeKnowledgeBaseMessage` and passes it to the already-identified ordinary command queue `0x0046E620`. The exact contents of the helper-managed topic/article catalog and all button-mode policies are not reconstructed here.

Current server source registers `ExecuteKnowledgeBaseMessageCommand` in `CommandConfigManager2.cpp`. Its `doQueueCommand` checks state mask and locomotion, then returns `SUCCESS` without a substantive game-state mutation. Therefore the presence of this command must not be mistaken for a server-side article fetch. The client-side help tree/content path remains separately asset-/service-dependent. Whether the command is emitted for all article actions or only a selected subtype is **tentative** until the relevant article object vtable is named.

## Server-disconnect mediator — prior finding verified, not new breadth

The [2026-09-22 session-shell checkpoint](ANALYSIS_CHECKPOINT_2026-09-22_SESSION_SHELL_UI.md) already identified `SwgCuiServerDisconnected` constructor `0x00CA87C0` and its `ServerDisconnected` page. This follow-up **does not count it as a new class or constructor**. RTTI/vtable `0x015F08E8` and constructor code agree with that earlier finding. Additional inspection of destructor `0x00CA88D0` and factory methods `0x00CA88E0`/`0x00CA89C0` found common mediator teardown and name registration/removal; no class-specific network-close callback was recovered from the inspected virtual slots.

This proves presentation registration and lifecycle, **not** which network failure opens it, whether reconnection is possible, or how the session is cleared. Those belong to W03/W02 and remain open where material.

## Compatibility significance and confidence

- **Confirmed:** Holocron RTTI/vtables, named widget bindings, literal `executeKnowledgeBaseMessage` in the UI callback, ordinary command queue call, and current server command's no-op-success body after gates; disconnected screen registration and page lookup.
- **Strongly inferred:** Holocron is a help/knowledge-base panel and its visible topic navigation is primarily local UI state.
- **Tentative:** exact article-subtype trigger for the named command; complete source and fallback of help content.
- **Unknown:** disconnect trigger/teardown policy and how optional customer-service/knowledge-base responses populate all Holocron screens.

No additional opcode or whole-client percentage is claimed. The help UI and disconnect UI are not first networking-POC gates.
