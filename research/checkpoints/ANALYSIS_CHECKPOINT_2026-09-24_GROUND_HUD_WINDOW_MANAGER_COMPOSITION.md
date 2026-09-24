# Ground HUD window manager composition

Date: 2026-09-24. Scope W10/A09. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses are provisional; Ghidra output is reconstructed pseudocode, not original source.

## Confirmed by code

RTTI identifies `SwgCuiHudWindowManagerGround` with primary vtable `0x015F39A0`. Its constructor `0x00E34BE0` calls the common HUD-window-manager constructor and binds `RadarPage`, `AllTargets`, `AttribMod`, `MfdStatusPage`, and `group` from the supplied page. It creates child mediators for radar (`0x00F02770`), all-targets (`0x00F00940`), attribute modification (`0x00EFE910`), ground status (`0x00EF4A60`/`0x00E43760`), and group (`0x00EF1780`). Missing named CodeData objects trigger `Fatal`, so these page names are a real UI-asset contract. Child ownership and exact lifetime for every submediator need further inspection.

The ground manager's message callback `0x00E35550` is an additional listener for **already mapped** `ResourceListForSurveyMessage` (`0x8A64B1D5`) and `SurveyMessage` (`0x877F79AC`). It parses with `0x01199900` or `0x01199160` and, only when a `SwgCuiSurvey` mediator is present, forwards to `0x00F0F690` or `0x00F0FD70`. These correspond to the existing [resource-list](ANALYSIS_CHECKPOINT_2026-09-15_RESOURCE_LIST_FOR_SURVEY.md) and [survey-result](ANALYSIS_CHECKPOINT_2026-09-15_SURVEY_RESULT.md) mapping; do **not** count as newly recovered opcodes. The same callback also handles `NewbieTutorialEnableHudElement` through parser `0x013F29E0` for two retained keys and delegates other keys to base `0x00E40E40`; exact key values at `0x0196D9E4`/`0x0196D9D8` were not decoded in this block. On `Game::SCENE_CHANGED` it resets ground-status state if the status child exists. It then invokes the base callback.

Virtual update `0x00E357D0` checks the current terrain appearance for `ClientProceduralTerrainAppearance`; when the survey mediator is visible and a further survey condition succeeds, it sends the terrain appearance to `0x00F102B0`, then calls the base update. This links survey UI refresh to local procedural terrain without proving terrain sampling formulas here. Destructor `0x00E35090` releases radar and attribute-modification child references before base destruction. No direct network send is visible in these inspected functions.

## Confidence and limits

- **Confirmed:** RTTI/vtable identity, named page children, listener type checks and parser calls, survey mediator forwarding, scene-change branch, procedural-terrain gate, absence of direct send in these methods.
- **Strongly inferred:** this class is the composition root for several ground HUD mediators, not a single monolithic widget.
- **Unknown:** the two tutorial key literals, all child ownership/refcount details, complete survey terrain refresh behavior, and whether every page is present in every shipped UI variant.

Evidence: read-only PE RTTI/vtable scan and Ghidra decompilation of `0x00E34BE0`, `0x00E35090`, `0x00E35550`, `0x00E357D0`, and existing packet checkpoints. The transient decompilation files remain under ignored `local-private/scratch/hud_window_manager_ground/`.
