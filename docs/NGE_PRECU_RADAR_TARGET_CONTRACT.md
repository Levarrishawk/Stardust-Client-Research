# Radar tooltips and original target HUD compatibility

Date: October 14, 2026, late evening Eastern (October 15 UTC). Scope: two source-client access violations during radar interaction. This complements the [radar resize contract](NGE_PRECU_RADAR_RESIZE_CONTRACT.md) and [original HUD architecture](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_HUD_TARGET_STATUS_ARCHITECTURE.md); it does not close combat or difficulty-rating parity.

## Evidence and confidence

The user-supplied `20261015020457` minidump has SHA-256 `0D5B55EA9AC94343A2850A77947EE816991B61F7939CFEAD1B7F152668287B5B`. Main image base `00E60000`, PC `019ADCB6`, source-build RVA `00B4DCB6`: the fault reads `0000003C` through `mov ecx,[edx+1c]`, with EDX `20`. These dump observations are **Confirmed**. They are not VAs in the research repository's original executable baseline.

Exact old symbols were no longer available. The fault sequence `8B4A1C0F45EA894C24388BCB` occurs once in camera-default source build `3CFE308E99638BFCBBBC237EFD2779DE77B60499FBD03228BDB3AF9BD8EEDE44`, at RVA `00B4DD66`. DIA lookup with that build's symbols resolves `CuiCombatManager::getConMessage`, line 2144. Its unguarded vector index and 32-byte entries explain the read at index one, offset 28. This correspondence is **Strongly inferred** root-cause identification, not an exact old-PDB stack unwind.

In source `src/engine/client/library/clientUserInterface/src/shared/core/CuiCombatManager.cpp`, `install` loads `datatables/player/level_con_table.iff` and `datatables/player/con_display_data.iff`. Neither path exists in the effective 203,617-name installed overlay inventory. Missing display data leaves `s_conLevelData` empty. `getConIndex(int,int)` formerly manufactured an empty threshold row and returned default index one; `getConMessage` indexed the empty display vector. Radar `SwgCuiGroundRadar::getTooltip` calls `ObjectAttributeManager::formatDescription`; its creature branch calls `getConMessage`. Local `/consider` also uses that function. Source relationships and inventory absence are **Confirmed**; no original Pre-CU difficulty algorithm is established by them.

A first retry on `9C1938B61AC49558C3A63C221970927A0F38C610505EA305F9D1B10A9456EBA3` exposed a separate `20261015022547` dump, SHA-256 `DA1158F8DC03D1B04C84F690492E5592B1DEBF8A6620B671E6C6EDBBD214C30C`. Main base `00450000`, PC `0145E019`, RVA `0100E019` maps directly with matching symbols to `SwgCuiStatusGround::updateAttackableTargetHam`, line 1598. Dump RSDS identity matches the preserved executable. The null health mediator is dereferenced before its later null check. This identification is **Confirmed**.

Effective `ui/ui_ground_hud.inc` comes from `stardust_3_01.tre`, priority 53, 64,909 bytes, SHA-256 `11048CAC8FF60B3EE2E09B7309B1310FB21974C7F11C8953778C7D4B55A80999`. Its `Target` root CodeData binds collapse/expand and `pagestatus=status`. HAM pages `h/a/m`, state volume, sample state icon and `textName` live inside `Target.status`. The NGE manager instead constructs `SwgCuiStatusGround` directly on `Target`. Missing outer bindings explain the null health mediator, illustrative name and sample icons. Asset/source mismatch is **Confirmed**; the original constructor-composition research independently establishes the outer target container's role.

## Required behavior and bounded repair

- An unavailable player, missing threshold table, or negative/out-of-range con index yields no difficulty message. Output is cleared. Attribute description appends the message only on success with nonempty text; other description fields remain available.
- Missing con colors return white, including the previously unguarded invulnerable accessor. A display table longer than the threshold-derived count must not write past the vector. Fill all remaining allocated entries under the existing source fallback policy.
- Under the Pre-CU HUD profile, the intended-target mediator retains the movable outer window but binds `pageH/pageA/pageM`, `volumeStates`, `sampleStateIcon`, `textName` and `hamParent` into the authored `status` child. The ordinary constructor clears sample state images and binds real health/action widgets. The attackable-object path also checks for an absent health widget before dereferencing it.
- Hidden Mind remains data-backed and hidden by the existing Stardust profile. No NGE con tables or modified HUD assets are supplied. No protocol, damage authority, ability execution or server behavior changes.

This safely tolerates missing NGE data; it does **not** restore skill-based Pre-CU difficulty text/colors. Existing valid NGE table paths remain supported. Target posture/state rendering, remote buffs, changing HAM/wounds and full combat behavior remain independent gates.

## Verification and limits

Final deployed x86/DX9 source build: `D51DD63E29A3A6223C6003EC141B601ACBC63736612B07CB401D1467CEBE5A13`, with matching symbols in Experimental. Release/Win32 build succeeds. Production-library native tests pass **13,496 checks / 4,780 malformed rejections**, plus five isolated CRC/ACST cases. Six new checks exercise actual empty-table color accessors, including extreme level inputs; they do not instantiate a complete gameplay tooltip or target mediator. The runner now includes the existing Experimental runtime in its process DLL search path because linking these production accessors introduces `dpvs.dll`. The first missing-DLL test run failed; the corrected runner passes.

Computer use verifies existing-character world entry, radar zoom out/back, self and multiple NPC tooltips, a shelter target with real name/health, an NPC target with real name/Health/Action, no sample icons, hidden Mind and normal Exit. The bounded fault trace ends `exit=00000000`, without a new access violation/dump; caught C++ first-throw receipts occur during shutdown, so this is not a zero-exception claim. No deliberate movement, combat, item mutation or server administration was performed. Broad endurance and every radar menu/input combination remain untested.

Next probe: recover original skill-based consider semantics and fixtures before implementing difficulty parity; separately exercise target state/HAM changes and lifecycle with authoritative gameplay evidence. Retain [deferred planet-map work](NGE_PRECU_CONVERSION_PLAN.md) and the user's post-main-conversion buff-builder/Collections scope.
