# NGE options as a private loose override

Date: October14,2026. The user explicitly requests retaining all source-client options by using the original NGE includes as loose files, rather than omitting settings for Pre-CU widget compatibility.

## Failure and input identity

The user invokes Ctrl-O in the rendered source client. Its owned fatal report identifies missing comboCombatSpamFilter in GroundHUD.OptMain.comp.target.chat / SwgCuiOptChat. The effective Stardust ui/ui_options.inc comes from priority53 stardust_3_01.tre,263023 bytes, SHA256D7C02229CA4E0487D31AAD38C3B4F497A6323B7683CBE666A24AEDCF492B5217. Static inspection confirms that it supplies the original chat widgets, with no newer spam-filter controls. **Confirmed source/widget mismatch**, not a spatial-chat or HAM failure.

The authorized NGE asset source is D:\Development\SWGDevWork-OLD\StarWarsGalaxies. Its standard sku0_client.toc is read using the source TreeFile_SearchNode.h/.cpp::SearchTOC layout:36-byte header, backing-tree name block,24-byte records and filename block; record filename lengths are accumulated to offsets. Stored/zlib member slices are bounded against their backing files. The custom legends.toc has a different unverified header/body and is not decoded; no protection or custom transformation is bypassed. Initial direct TRE-only lookup missed files whose external TOC provides their index.

| Loose member | Original NGE backing archive/offset | Bytes | SHA256 before adaptation |
|---|---|---:|---|
| ui/ui_options.inc | patch_44_client_00.tre /62958677 |362200 |5A5A8F732692F3FE74DDEF1B9D17D6D3BE9604D2B7A29B084BDE8A8E5777F5AB |
| ui/ui_options_keymap.inc | patch_42_client_00.tre /70676072 |21690 |1E5113F97EFBB29D1B337A040D51CD67FF2DC30157236DD9F10108F44CDE0EDD |
| string/en/ui_opt.stf | hotfix_55_client_00.tre /4607046 |94286 |F6531FBEC3F0A7B3B8680C221D60BBDF009BE570619D1FE1F4E58FD3C109C219 |
| string/en/ui_opt_sound.stf | patch_44_client_00.tre /34338545 |3200 |93FE03B67F433EC10C707262067BCE3B634265E38D79A861192BD3C94FD4B1AE |

All four files are installed only under C:\Stardust-Experimental at those relative paths. These original/proprietary assets are not added to either Git repository. The existing Pre-CU HUD/toolbar, TRE ordering, private runtime configuration and blank launch credentials are preserved. Source/executable TreeFile precedence already admits current-directory loose overrides above TREs; [asset contract](ASSET_PIPELINE.md).

## Two authored binding additions

The NGE include supplies the new controls required by the source except two active bindings. Controls already contains the ButtonFindControllers widget, but its CodeData lacks the corresponding property; add ButtonFindControllers='ButtonFindControllers'. Interface lacks the newer cooldown checkbox; add checkShowToolbarCommandCooldownTimer='main.comp.checkShowToolbarCommandCooldownTimer' and a visible checkbox alongside the double-toolbar control, captioned Show toolbar cooldown timers, using the existing checkbox style. No source option setter/registration is removed or made optional.

The adapted main include is362798 bytes, SHA256BFC3C7A97D4FF18A78599D9859F1748E43D40DE728AAA84BF98EDE959CA36C78. Existing duplicated UI attributes are preserved for the game's parser; XML inspection deduplicates only its analysis copy. One static lookup is an external /controls page owned by the global UI, rather than a child of this include; do not misclassify it as an absent option widget.

## Acceptance and limits

BuildB78D8649F02B17AB05DBB0F421008B8D40F5053D284F406CF228E87E5C7444D1 renders with the loose override. Computer use opens Ctrl-O and inspects Graphics, Terrain, Sound, Combat, Chat including its bottom spam-filter controls, Chat Color, Alarm, AFK, Interface including the added cooldown preference, Controls/Find Controllers, Misc and Keymap. All initialize without the prior fatal; the existing Character sheet still opens and independently constrained full Health/Action bars remain visible with Mind hidden. Normal Disconnect returns to selection and same-process re-entry renders again.

The retained Voice page is also visibly inspected; its mediator construction is already commented out as deprecated in SwgCuiOpt.cpp. Displaying its authored controls does not establish a working voice service. No voice service or recording is enabled. Read-only tab/scroll inspection and Cancel do not establish every setter, native device operation, server acceptance or later-feature behavior. In particular, a visible cooldown preference does not implement Pre-CU toolbar cooldown animation. The current keymap preset shows an unresolved localization token; label completion is still open. Existing hardware-dependent disabled controls are not compatibility removals introduced here.

Remaining probes: exercise approved local preferences with restoration, reconcile server-dependent NGE settings at their actual boundaries, complete preset localization, and eventually verify radar resizing. [Daytime checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md).
