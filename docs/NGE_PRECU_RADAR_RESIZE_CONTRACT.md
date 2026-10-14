# Pre-CU radar wrapper resize contract

Date: October 14, 2026. Question: why does the map stay at its initial diameter when the original HUD radar is resized?

## Observations

Original executable SHA-256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 is verified before address use. Known radar update 00F03A40 compares root size with cached size at offsets DC/E0. On change, 00F03A72 reads the square member E4; 00F03A78 gets its parent via 0110C450. 00F03A83..00F03A8F chooses the lesser parent dimension; 00F03AA3 sets both square dimensions to that side; 00F03AAC calls 0110CA70 to center it; 00F03AB7 calls 010FE1B0 to force-pack descendants. Constructor 00F029A4/00F029A9/00F029B5 binds member E4 to the string square at VA 018CF004. Static inspection of the two packing callees matches source UIWidget::Center and UIPage::ForcePackChildren. **Confirmed** square-parent fit/center/pack behavior. No original client execution is needed.

The effective original ui/ui_ground_radar.inc comes from priority 53 stardust_3_01.tre, 20,239 bytes, SHA-256 EC4B1E59766762F3939978C2E0F327D6E784D56716BD83C67C851AD86354B3A9. Root is resizable; altRadarFg packs in both axes; its initial 132 x 128 square has no PackSize. The nested map and compass use authored packing. The migrated NGE update aligned blip/waypoint panes to the ground widget but never resized this square. This explains the fixed inner diameter; source/asset evidence is **Confirmed**, user-visible causal interpretation **Strongly inferred** until the live resize gate.

## Implementation consequence

Engine-neutral PrecuRadarLayout fits the nonnegative lesser parent dimension and returns centered integer offsets. The Pre-CU-only radar adapter applies that square size/location and force-packs its descendants before synchronizing the blip and waypoint panes. No hardcoded replacement diameter, root aspect change, map-range change or loose radar asset override is introduced. NGE skin selection stays on its original path.

The original trigger is root-size change. The modern adapter also compares actual square geometry with its currently packed parent, handling delayed parent packing and initial restored sizes. Negative dimensions clamp to zero; centered integer rounding follows the source widget convention. This is an explicit modern defensive policy, not claimed original malformed-layout behavior.

Native tests cover rectangular parents, odd offsets, expansion/shrinkage, zero/negative dimensions and maximum signed long dimensions. Release HUD library and complete native suite pass 11,515 checks / 3,538 malformed rejections plus five isolated cases. Build DAC5873C0E4A5D142BF02C8A1C538C159DAF5934EAC75196A50B02284480E013 is deployed with matching symbols in Experimental. Live resizing, marker/compass alignment, coordinate labels and persistence remain pending at this preparation point.


## Handle correction before acceptance

The first live DAC5873 build renders, but dragging the circular edge does not resize the root; no size-change receipt is available in that build. This is not declared acceptance or evidence against the square rule. Source UIWidget defaults ResizeInset to zero and implements outer-frame resize margins when it is zero; the effective Pre-CU asset has no ResizeInset override. The NGE radar update introduced a computed inner inset. The Pre-CU profile now explicitly retains the original zero inset while the NGE path keeps its existing calculation. Capped diagnostics report only root/parent/square/ground dimensions and UI location, never character identifiers or world coordinates. Source Release library/main build pass; CC87635865A96F1552015947F3BD3F24F9313191FF7D934E3268D25E0ED642A7 is installed for the next resize gate.


## Live receipt and unresolved input/persistence gate

CC876358 enters the world. Initial geometry is root180x180, packed parent132x148, square132x132 and ground128x132; the authored four-pixel ground horizontal margin remains. Computer-use drags do not show an immediate retained new root; a title-bar drag on a separate browser also fails to move its frame and selects the endpoint row. An input-ordering issue is **Tentative**, not a proven cause or a permission to bypass computer use. Later detected user input yields transient root181..205 and square133..157 receipts interleaved with root180. **Confirmed** fitting responds to parent changes; reliable retained resize, markers/compass and saved sizes remain unaccepted. No further source change is inferred from these ambiguous transitions. User physical Escape then stops computer use; no more app input is issued. Next probe: distinguish actual delivered drag/capture and root-packing/aspect behavior, then verify expansion/shrinkage/persistence against the original client constraints.


## Afternoon native UI discrimination

A synthetic hierarchy reproduces the observed original packing rules with actual production UIPage/UIWidget objects: fixed root packing, parent absolute packing, un-packed square and the four-pixel ground margin. Explicit sizes180/200/280/400/256/180, recursive packing, and actual source lower-left mouse resize to280 all retain the expected root size and fit. Native11,546 checks/3,538 malformed rejections plus five isolated cases pass. The native runner now matches production /Zc:wchar_t- and supplies UI forward declarations; initial missing header/ABI compilation errors are corrected. These are production-library synthetic tests, not original asset execution or live resize acceptance. Bare packing/resize therefore does not reproduce the observed180 snap-back. A capped Pre-CU OnSizeChanged diagnostic is prepared to record dimensions/user-modification state and main-image code RVAs only, discriminating the actual root-resize caller without packet/account content.
