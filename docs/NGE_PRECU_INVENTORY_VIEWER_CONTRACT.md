# Original inventory equipment viewer compatibility

October 14, 2026. Bounded question: why does opening the original inventory crash before its equipment panel is constructed?

## Observations and confidence

The authorized read-only inventory opening on build DC9D0CBAF3D7EDAAF8A2AA455495754BE8673B082CAFC908ADD9CC5846E004F6 captures access violation C0000005 at own-image RVA00C4401F, CuiMediator::registerMediatorObject+0F. Matching symbols identify SwgCuiInventoryEquipment constructor+1C8, called by SwgCuiInventory and the HUD inventory route. The owned helper exits at the terminal fault; no client window remains. No gameplay command was intentionally executed. **Confirmed** failure chain.

Effective ui/ui_pda_inventory.inc comes from priority53 stardust_3_01.tre, 52,935 bytes, SHA-256 C0B7D04CBB6724968262D1FC68EA41F53423FE75402E24B96A5683CAF7B0F858. Its equipment CodeData points CharacterViewer to a CuiWidget3dPaperdoll. Current source CuiWidget3dPaperdoll derives from CuiWidget3dObjectViewer; the NGE equipment mediator casts it to the unrelated CuiWidget3dObjectListViewer and registers the resulting null pointer. **Confirmed** source/asset mismatch. The [original equipment investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_INVENTORY_EQUIPMENT_PAPERDOLL.md) independently proves the original paperdoll binding.

Original baseline SHA-256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 is reverified before probes. Setup00E88E00 enables auto view computation through00A665D0 (flag2), disables object alteration through00A667E0 (flag40), supplies minimum camera vector zero and maximum (0,1.65,1) at00E88E4F..00E88E6A, and updates the label from the object name. Callback00E88F20 changes clamped target zoom by plus/minus0.2, enables interpolation, turns object yaw by plus/minus PI/4 and stops automatic rotation. Initializer00E88A20 computes PI times0.25 into global01973190. Center00E88FB2..00E88FD9 restores zoom0, interpolation, rotate speed0 and yawPI. **Confirmed** controls and constants; dynamic render framing remains a live fidelity gate.

## Implemented adapter and lifecycle

The Pre-CU HUD profile binds the original paperdoll explicitly and rejects a wrong viewer type before registering it. The NGE profile retains its list viewer. The original preview setup and five zoom/orbit/center controls use the existing legacy viewer API. The borrowed live object is not altered or owned; deactivation clears its target and pauses it. Explicit target clearing is a modern lifecycle safeguard, not an assertion that the original mediator cleared on every deactivation. UI resources remain owned by the page/mediator framework. No transfer/equip/drop behavior or wire layout is changed.

The first compilation fails on an incorrect sharedMath/Constants.h include; the existing sharedFoundation/FloatMath.h supplies the confirmed constants and the corrected Release rebuild passes. Build F18F0C2F8FA9FEA1C27E252D47ADCFFC95789C99F09768B6E07AEC6D87DEA389 is deployed with matching private symbols. Computer use reports a physical Escape stop on fresh window observation before login; no further UI action occurs. Live original inventory opening, paperdoll preview/buttons, container contents, Close/reopen and Disconnect/reentry remain pending. Native codec/projection counts remain11,850/3,541 plus five isolated cases; this UI adapter is verified by production compilation and bounded live use rather than a copied fake-widget test.

## Separate XP-character loading asset

The user reports that Sirravel on another account crashes during loading while trying to inspect nonempty XP. A contemporaneous fatal report names missing vertex_program/a_vertexlit_fog_gradient.vsh; unlike the owned inventory debugger stack, it is a separate render-resource failure. Correlation to that character attempt is **Strongly inferred** from timing/user report, not an observed authenticated retry under that account.

The configured Stardust TRE member inventory has no matching program. Original NGE sku0_client.toc resolves it in patch_24_client_01.tre at offset87,579,800: 1,064 bytes, SHA-2564652E159562604BBB76ED759A19654F84F09A6A1A9FFF0F819F6D06A81F18DD3. Its two include paths and their nested shared-program include already exist in the effective Stardust archives. The referenced transform matrix is declared at c0 in those headers. The exact original program is copied privately to Experimental/vertex_program; no existing loose file is overwritten and no archive/include/renderer is replaced. This is asset presence verification, not compilation/render or Sirravel entry acceptance. A retry on that other account needs an authorized UI login; existing disposable credentials remain scoped to the known test account.

Next: finish the inventory Release rebuild, deploy matching EXE/PDB and use computer use for the authorized account's read-only inventory opening/preview/close. Then validate the separate missing-program case when its account login is available. Keep planet-map diagnosis user-deferred and no gameplay mutation, proprietary publication or staging.


## Renewed evening read-only acceptance

Renewed computer-use testing first identifies a separate [object attribute decoder mismatch](NGE_PRECU_OBJECT_ATTRIBUTE_CONTRACT.md) on F18F0C2F. Corrected E49357E5 enters the world, opens the original inventory/paperdoll, displays selected shirt condition/volume and tooltip, switches icon/list views, closes/reopens, normally disconnects/reenters and reconstructs inventory. Opening/render/lifecycle are accepted for this bounded case. Preview buttons are authored hidden; zoom/yaw/center input remains unaccepted. No inventory mutation or full gameplay/endurance claim. Sirravel shader/retry and nonempty XP remain separate.
