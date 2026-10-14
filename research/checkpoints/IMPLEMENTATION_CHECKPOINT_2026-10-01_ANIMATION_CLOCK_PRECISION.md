# Fractional preview clock boundaries — October1,2026

## Question, evidence and stopping condition

Do mathematically complete fixed-step cycles stop/wrap across clip lengths and proxy time scales other than the selected120-frame idle? Native regression tested end frames1,7,110,117,131,32767 with scales0.3,1,2.5. Before repair, end1/scale0.3 ended480 ticks at0.9999999999999954 and counted only one loop. This is a reproduced prototype numerical bug, not an original executable claim.

## Repair and confidence

The modern preview clock uses compensated frame accumulation and snaps a positive nearest loop boundary only within eight double machine epsilons scaled by max(endFrame,absolute nextFrame). Seek resets compensation; pause/focus do not accumulate elapsed time. This explicit numerical policy also permits fractional one-shot clips to stop at their intended endpoint. It is not a recovered legacy loop rule.

Native full suite passes all18 duration/scale combinations, two-cycle480-step and one-shot240-step cases, materially incomplete boundary preservation, seek reset, existing pause/focus/invalid-time gates. Prototype host build passes, selected original textured GPU480-update/control/readback regression passes and textured interior headless two-owner regression passes. Original byte sources/material/pose logic are unchanged.

## Consequence and next probe

Fractional clips no longer stall a loop counter or one-shot completion just below an intended endpoint. The tolerance is confined to numerical boundary equality and does not truncate a materially incomplete cycle. See the [playback contract](../../docs/ANIMATION_PREVIEW_PLAYBACK_CONTRACT.md). Next: prepare immutable mesh joint mappings once rather than repeating name resolution on every visual pose; retain exact original independent pose agreement and ownership checks.
