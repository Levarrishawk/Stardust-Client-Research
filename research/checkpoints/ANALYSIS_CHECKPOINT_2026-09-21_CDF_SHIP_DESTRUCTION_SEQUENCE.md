# CDF Ship Destruction Sequence Block

Date: 2026-09-21

## Result

This block reconstructs the compound `DSEF` record used for capital-ship destruction presentation. Unlike the simple terminal `DSTR` client effect, `DSEF` is a timed sequence with repeated spatial breakpoints, split explosions, layered audio, and optional spawned-object events.

## Loader architecture

`FUN_00606250` recognizes one `FORM DSEF`, constructs the sequence through `FUN_006FCC70`, stores it at `ClientDataFile +0x7C`, then calls `FUN_006FD080` and `FUN_006FCF80`.

- `FUN_006FCC70`: sequence constructor; creates four owned vectors/collections.
- `FUN_006FD080`: parses the complete compound record.
- `FUN_006FCF80`: post-load preparation/resolution over each vector.

Only one DSEF is retained per CDF; later duplicates are skipped.

## DSEF grammar

```text
FORM DSEF
  INFO
  FORM SPTL
    FORM BRKP ...     repeated spatial break events
  FORM SXPL
    SEDF ...          repeated split client effects
  FORM ASNL
    SDAS ...          repeated ambient sequence sounds
  FORM SDOL           optional
    SDOE ...          repeated spawned-object events
```

### INFO

The top-level `INFO` contains:

1. one `float32` sequence-duration/timing scalar;
2. string + `float32` pair for the repeating break/explosion client effect;
3. string + `float32` pair for the final client effect.

The pair parser `FUN_006FCB50` confirms a NUL-terminated string followed by a 32-bit float. In the effective rebel-shuttle record the first value is `3.0`; the effects are `cbt_ship_capital_destruction_break_explosion_01.cef` and `cbt_ship_capital_destruction_final_01.cef`, each followed by `50.0`.

The precise semantic label for each scalar remains tentative (timing, radius, or weighting), but its encoding and association are confirmed.

### SPTL / BRKP

`SPTL` contains repeated `BRKP` records loaded by the same 100-byte breakpoint loader used by the ordinary CDF breakpoint system. The shuttle has twelve records: one `break_req_01` and `break_01` through `break_11`. These records use an additional leading string (`one`) and explicit/randomized transform extents of `10.0`, with a trailing `1.0`.

This ties the previously reconstructed hardpoint/random-transform breakpoint mechanism directly to the timed destruction sequence.

### SXPL / SEDF

Every `SEDF` is a NUL-terminated client-effect name followed by one `float32`, parsed through the same string+float record helper. The shuttle defines eight split effects, each followed by `100.0`, including six `_00_01` through `_00_06` resources and two further split branches.

### ASNL / SDAS

Every `SDAS` is likewise a NUL-terminated sound-template name plus `float32`. The shuttle configures:

- capital destruction loop at `1.0`;
- metal-creak loop at `1.0`;
- alarm loop at `0.5`.

This is a sequence-owned sound layer, separate from VTHR engine state and DAMA stage sounds. Runtime tracing shows that each paired float participates in positioning/coupling sequence elements; it must not yet be labeled as volume solely from the observed values.

### Optional SDOL / SDOE

`FUN_006FD080` optionally enters `SDOL` and repeatedly parses `SDOE` records through `FUN_006FC720`. The SDOE parser reads three variable byte/string vectors; the enclosing runtime record is 44 bytes. The only occurrence in the corrected effective 6,288-file CDF corpus is `client_shared_star_destroyer.cdf`: empty first string, `appearance/star_destroyer_baffles.apt` second string, empty third string. This confirms that at least the second field is an appearance reference; the optional first/third field meanings remain unresolved. The grammar, optionality, and corpus rarity are confirmed.

## Post-load preparation

`FUN_006FCF80` visits:

- two top-level effect pairs;
- every 20-byte split-effect record;
- every 20-byte ambient-sound record;
- every 44-byte spawned-object event.

This strongly indicates eager resolution/cache preparation of referenced effect, sound, and object resources after parsing.

## Replacement-client consequence

Capital-ship death should be implemented as a data-driven presentation timeline owned by the scene/effect layer:

1. start layered loops;
2. select eligible breakpoint transforms/hardpoints;
3. emit repeated break effects over the sequence interval;
4. trigger configured split effects/spawn events;
5. play the final effect;
6. stop sounds and destroy all transient objects deterministically.

The exact 32-bit floats and strings must be consumed from original assets. Internal scheduling, resource handles, and ownership can be redesigned for 64-bit safety. This system is not required for the networking proof of concept but is required for full original-asset fidelity.

## Superseding field-semantic trace

`ANALYSIS_CHECKPOINT_2026-09-21_DSEF_FIELD_SEMANTICS.md` subsequently closes the previously tentative fields: the top scalar is an initial split-delay seed; effect-pair floats are nominal reference lengths; the SDAS float controls spatial placement/coupling; and SDOE strings are object template, appearance fallback and parent hardpoint.

## Confidence and unknowns

- Compound grammar and collection boundaries: confirmed by decompilation.
- Record encodings stated above: confirmed by loader and effective asset bytes.
- High-level staged-destruction purpose: confirmed by resource names and runtime helpers.
- Exact meaning of each numeric scalar: tentative pending runtime timing-call correlation.
- SDOE first/third field semantics remain unknown; the sole corpus instance confirms the second field is an appearance reference.
