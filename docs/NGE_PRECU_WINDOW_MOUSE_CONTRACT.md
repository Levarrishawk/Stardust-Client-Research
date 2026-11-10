# Source-client Windows mouse ordering

October 14, 2026. This is a source-client platform adapter and live conversion receipt, not recovered original-client implementation.

## Observation and interpretation

The existing source queues absolute cursor positions from `Os.cpp::WM_MOUSEMOVE` and button transitions from buffered `DirectInput.cpp::MouseDevice::submitEvent`. A bounded numeric `CuiIoWin` trace of the same lower-left radar gesture records start position1507,178, then left-down at1447,238, then movement/up at that endpoint. The starting widget is the resizable radar; the incorrectly positioned down targets another widget. No root resize callback occurs. **Confirmed observed ordering**; the two independently delivered streams explain this failed gesture. This does not establish that every physical mouse gesture fails.

## Adapter

`SharedFoundation.useWindowsMouseButtons` defaults false. When enabled with registered down/up hooks, the Windows platform adapter queues each button's signed client coordinates immediately before its transition. Logical left/right/middle and X1/X2 buttons are supported; Windows already performs left-handed mapping. Double-click messages enter the ordinary down path so the existing UI double-click classifier retains ownership. DirectInput suppresses only the duplicate first five buttons; extra buttons and wheel remain available. The window captures the mouse while button-state bits indicate a held button and releases its own capture after release. Existing focus/input-reset handling remains responsible for lost focus.

In pointer mode with mouse-look disabled, `CuiIoWin` forwards the absolute-position stream as UI movement and blocks duplicate relative X/Y UI movement. Free-look and active mouse-look retain the existing relative-motion/camera path. This is a general client input adapter, with no automation-specific event injection or alternate computer-control API. Hooks are registered/unregistered with `IoWinManager` lifetime. Neither the engine-neutral radar layout nor assets are changed by this increment.

## Validation and limits

Release build SHA256 `3BC0CEEACF2A59C72C3167095EA696463A90A86A0EA7CBF4F330C6AAC7EC3502` is deployed with matching symbols only in Experimental. Native suite passes12,324 checks/3,887 malformed rejections plus five isolated boundary cases. Actual platform-helper fixtures cover disabled/missing hooks, five buttons, double-click input, signed coordinates, unsupported messages/X selectors, and one-frame drag ordering. Production widget tests independently cover resize/packing. These do not simulate the entire Windows capture/focus lifecycle.

Computer use through the authorized disposable test login confirms pointer switching, radar180→240→300→180, circular imagery/compass/control alignment, then240 retained across normal Disconnect/re-entry and restored180. Numeric receipts confirm root240 with square192, root300 with square252, and root180 with square132. The authored four-pixel horizontal ground margin remains. No deliberate movement, combat or item mutation occurs. Full physical input fidelity, multi-button mouse-look, capture loss, full process-restart persistence and other display scales remain open.

See the [radar geometry contract](NGE_PRECU_RADAR_RESIZE_CONTRACT.md). This implementation acceptance changes no research/function/protocol denominator.

October 14 process-restart gate: changed240 radar size and local F2 placement survive normal Exit, full restart, login/world on D97C177C. Restored180 then survives normal Exit/restart on ABB2FDBD. This closes the tested-resolution restart gate; other scales, capture loss and full physical-input fidelity remain open.

## October 16 fixed reticle image follow-up

A separate failure leaves the original green reticle image fixed while numeric
UI cursor positions advance. The effective priority-53 ui/ui_styles.inc
(1,686,109 bytes; SHA256 B2E1992D2831DC76C0046577BD325F90F999C29E64A18737DE1D12F8D214C216)
has reticle_default but lacks the NGE intended_inactive cursor selected by
SwgCuiHud when auto-aim is enabled. Its null-style guard also skips image
position updates. This explains the observed image lock; it is not evidence
that relative input or the bounded-box camera policy stopped receiving motion.

The source Pre-CU ground adapter falls back to the existing original default
only when the selected context cursor is absent. Live E72BDF31 verifies two
interior-box movements, all four edge clamps/panning, unrestricted Alt pointer
access and return to free look. Observed UI bounds at the tested resolution
are x789/891, y474/576. The final 5C6A7068 candidate removes temporary numeric
logging while retaining the fallback; its smoke is separate. Other resolutions,
focus/capture cases and exact sensitivity remain open. No input math, saved
preferences, coverage score or packet count changes follow from this receipt.
