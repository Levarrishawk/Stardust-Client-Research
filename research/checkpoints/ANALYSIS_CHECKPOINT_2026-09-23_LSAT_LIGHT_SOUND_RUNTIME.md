# LSAT light and sound activation edges

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`.

## Result

The optional `LGHT` record is not inert. `LightsaberAppearance` state setter
`0x0064E890` controls individual blade activation/deactivation. For the first
blade's activation path, it creates a Light through `0x006E2DE0` (which calls
the common Light constructor `0x0076D500`). When the LSAT light-enabled flag
is set, the method copies the authored RGBA, two `RANG` floats, two `TIME`
floats and `DAYN` Boolean into the Light's corresponding fields. It also
looks for an owner `PaletteColorCustomizationVariable`; if present, that
palette value supplies the live blade/light color, otherwise the fallback
color constants apply. This gives the new client a concrete integration point
between original lightsaber assets and object customization.

The `ASND` path is stored as a retained template string at LSAT `+0x30`.
On first-blade activation, `0x0064E890` fetches that sound resource through
`0x0064FD70` and starts an audio handle under the client sound policy gate.
On deactivation it releases the retained handle at runtime `+0x268`; it also
emits distinct blade-on/blade-off effect notifications through `0x005F1C60`.
The exact sound playback mode is not established from these calls alone.

## Evidence/confidence

- All four LSAT light-field accessor call sites lead to `0x0064E890`:
  `0x0064FD80`, `0x0064FD90`, `0x0064FDA0`, `0x0064FDC0`, `0x0064FDE0`.
- `0x0064E890` performs Light allocation, color and timing/range copies,
  day/night flag, palette-variable lookup, sound fetch and handle cleanup.
- Light construction link `0x006E2DE0 -> 0x0076D500` is confirmed by xrefs.

These are **confirmed by executable code**. The editor-facing interpretation
of the two `RANG`/`TIME` endpoints beyond their copy into the runtime Light
fields, and exact audio voice policy, remain **partially known**.

## Replacement implication

Keep LSAT lighting optional and per authored resource; do not globally attach
one fixed color/radius to every saber. Preserve palette customization, the
first-blade lifecycle trigger, and cleanup. Renderer-specific light objects
may differ, but the authored parameters and activation state are part of
behavioral compatibility.

Scratch: `local-private/session-2026-09-23-marathon/block46_lsat_light.txt`,
`xrefs_block46.tsv`, and `lsat_field_scan.py`.
