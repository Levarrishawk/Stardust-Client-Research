# AvatarSummary final character-creation UI and completion state

Date: 2026-09-24. Scope A09/W10/W03. Source is verified original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; all decompiler text is reconstructed pseudocode. This complements, rather than replaces, the earlier [creation wire/workflow checkpoint](ANALYSIS_CHECKPOINT_2026-09-22_AVATAR_CREATION_WORKFLOW.md).

`SwgCuiAvatarSummary` constructor `0x00C745D0` binds Back, Next and Random Name buttons; name and surname textboxes; biography; tutorial checkbox; 3D viewer; `pageattributes` and `pageskills`. It constructs child `CuiCreatureAttributeTable` (`0x013CF610`) and `CuiCreatureSkillsList` (`0x013CEDE0`). These children are creation-summary projections, **not** the in-game `SwgCuiSkills2` progression mediator. The constructor also loads a configured list/presentation choice from local UI data; its full item semantics remain open.

Activation `0x00C74CC0` resets local completion/abort flags, fills the summary view from `SwgCuiAvatarCreationHelper`'s current candidate, initializes presentation subsystems, and subscribes to typed `Aborted`, `CreationFinished` and `RandomNameChanged` events. Deactivation `0x00C751A0` removes all three subscriptions. The UI callback `0x00C75360` handles:

| Control | Observed action |
|---|---|
| Back | Transition from `AvatarSummary` to `AvatarSetupProf` through the UI mediator manager |
| Next | Calls the previously mapped `avatar_creation_request_build_send_VA_00BB4D10`; hides the mediator on successful local submission, leaving server result asynchronous |
| Random Name | Calls creation-helper random-name request `0x00BB6670` |
| Editable text/toggle state | Copies/validates name, surname, biography and tutorial choice into candidate state before transitioning/submitting; exact user-facing validation messages need finer reconstruction |

The event callbacks are intentionally small: `0x00C75720` sets abort and clears completion (warning if both in one frame); `0x00C75750` sets completion only for true result and warns if abort already arrived; `0x00C75780` applies a random-name update unless a local suppression flag is set. Per-frame `0x00C758B0` consumes the flags. On abort it closes the server connection and returns to AvatarSelection. On creation-finished it clears the flag, finalizes local candidate/account presentation state, transitions to AvatarSelection and clears the creation-helper pending state. The decompiler's many UI/global helper calls do not justify assigning all cache mutations yet. Creation failure text/result behavior remains owned by the earlier network/helper path; it is not equivalent to this typed successful `CreationFinished` event.

Dataflow:

```text
original UI and creation candidate -> AvatarSummary (name/bio/tutorial + attributes/skills)
Back -> AvatarSetupProf
Random Name -> helper request -> RandomNameChanged -> summary text update
Next -> 0x00BB4D10 -> ClientCreateCharacter 0xB97F3074 -> helper result
  -> CreationFinished / Aborted -> deferred update -> AvatarSelection (abort also closes server connection)
```

Original asset compatibility includes the declarative summary page, candidate species/appearance templates, skill and attribute catalogs, localized labels and 3D preview resources. A replacement can redesign the screen, but should keep submit/result asynchronous and make the abort-versus-success transition explicit. The helper already owns the actual wire serializer; the UI should not duplicate it.

Confidence: **Confirmed by executable** for controls, child composition, event subscriptions, button-to-helper calls and abort/finish flag routing. Exact candidate-field validation, summary-list contents, and all post-result cache side effects are **partial/unknown**. No new opcode or whole-client percentage change.
