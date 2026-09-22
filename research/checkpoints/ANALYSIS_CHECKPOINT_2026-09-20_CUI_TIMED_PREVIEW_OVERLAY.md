# CuiIoWin timed preference-preview overlay

Date: 2026-09-20

## Result

The final rectangle drawn by `cui_iowin_render_VA_0093B2B0` is not an unidentified scene fade. It is a two-second, centered CUI preference-preview overlay used when reticle dead-zone size or HUD opacity changes.

## Lifecycle

- `cui_iowin_set_reticle_dead_zone_size_and_preview_VA_0093D720` normalizes the requested integer size to an even value, recomputes the centered rectangle, selects one preview color, and sets the shared timer `0x018A0BC8` to `2.0f`.
- The constructor reads `ClientUserInterface/reticleDeadZoneSize` and calls that setter.
- `cui_iowin_preview_hud_opacity_VA_0093D7E0` selects the alternate preview color and also sets the timer to `2.0f`.
- `cui_manager_set_hud_opacity_VA_00881980`, reached from the `CuiPreferences` `hudOpacity` setter family, calls the opacity-preview trigger when the live CUI exists.
- The CuiIoWin event/update path at `0x0093CB1E` subtracts frame/event delta while the timer is positive.
- The render path clamps the timer-derived opacity to at most `1.0f`, writes it into the preview color, binds the 2D primitive, and draws the rectangle from CuiIoWin offsets `+0x3c..+0x48`.
- On conditions where preview drawing is disallowed, it clears the timer.

`0x0093D640` obtains screen dimensions and centers a width/height pair, proving the stored four integers are rectangle boundaries rather than an arbitrary full-screen fade.

## Composition significance

The preview is deliberately the last element in `CuiIoWin::render`, after world, post-bloom overlays, and ordinary CUI. A replacement client should reproduce it as a UI diagnostic/preferences preview, not as a world post-process pass.

## Confidence

- Two-second timer, update decrement, centered rectangle, reticle-size and HUD-opacity callers, alternate colors, and last-in-frame position: **Confirmed by code**.
- Exact product-facing name of this preview widget/effect: **Unknown**; `preference-preview overlay` is a stable behavioral name.

## Evidence

- draw path: `0x0093B2F4..0x0093B37C`
- timer update: `0x0093CB1E..0x0093CB40`
- centered-rectangle calculation: `0x0093D640`
- reticle setter/preview: `0x0093D720`
- HUD opacity preview trigger: `0x0093D7E0`
- HUD opacity bridge: `0x00881980`, called by `0x009107C0`
- preference strings: `reticleDeadZoneSize` at `0x018A0D10`, `hudOpacity` at `0x0189F0C4`
