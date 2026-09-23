# LSAT dynamic bounds and unresolved light/sound consumers

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`.

## Result

The lightsaber runtime recomputes visibility bounds from current blade
lengths, not just the base hilt. `0x0064E090` checks a dirty flag, queries
the base appearance's extent, traverses every blade with positive current
length, transforms blade geometry points and builds an updated bounding
volume. It then creates/replaces the DPVS test model for the composite
appearance. `0x0064E390` marks bounds dirty as blade opening/closing state
changes. This is a material runtime requirement for correct visibility and
effect hardpoints, even if a modern renderer uses a different culling system.

Template function `0x0064FD50` fetches the BASE appearance (or an internally
cached wrapper), while `0x0064FE00` resolves a per-blade appearance through
the authored shader-bearing blade definition. `0x0064EFD0` installs both
base and blades beneath a shared DPVS visibility parent.

## Follow-up closure

The initially open `LGHT` and `ASND` edges were traced through the blade-state
setter `0x0064E890`: it constructs a Light through a wrapper and activates/
releases the stored sound path on first-blade state changes. See
`ANALYSIS_CHECKPOINT_2026-09-23_LSAT_LIGHT_SOUND_RUNTIME.md`. Exact audio
voice mode remains partially known.

Evidence: `0x0064E090`, `0x0064E390`, `0x0064EFD0`, `0x0064FD50`,
`0x0064FE00`; `local-private/session-2026-09-23-marathon/xrefs_block45.tsv`.
Dynamic bounds are **confirmed by executable code**.
