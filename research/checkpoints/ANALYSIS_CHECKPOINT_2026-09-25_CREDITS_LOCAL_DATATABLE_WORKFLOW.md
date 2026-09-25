# Credits local data-table workflow

Date: 2026-09-25. Workstreams A09/R01. Target: verified `Stardust.exe` and current configured assets. Decompiled output is reconstructed pseudocode.

## Result

`SwgCuiCredits` is a reachable, wholly local presentation workflow. It loads a six-column credits data table, maps each populated cell to one of six styled UI prototypes, inserts row spacers, and vertically scrolls the generated children. It contains no packet construction, command queue, browser, or server-authority path.

The factory at `0x00BCB660` allocates `0xAC` bytes and calls constructor `0x00BF95A0`. The current priority-53 `ui/ui_credits.inc` is nonempty (6,990 bytes), and `datatables/player/credits.iff` is present in the configured overlay.

## Workflow

The constructor binds a scrolling container and seven hidden cloning prototypes: `sampleHeader`, `SampleNames`, `SampleMain`, `SampleCompany`, `SampleTitle`, `SampleText`, and `sampleSpacer`. Activation `0x00BF9980` enables the mediator/input context, loads the credits records through `0x00BF9150`, builds presentation children through `0x00BF9A30`, and begins updating.

Loader `0x00BF9150` requires exactly six columns. For each row it uses the first nonempty cell. A leading `@` is parsed through the localized-string path; a single `.` becomes an empty/spacer-style value; other cells become ordinary text. Completely empty rows emit record type `7`, the explicit spacer. The retained record therefore consists of a style/type discriminator, Unicode content, and supporting lifetime state.

Builder `0x00BF9A30` clones the matching prototype for types `0..5`, localizes/copies its text, sizes and centers it, and inserts it into the scrolling page; type `7` clones the spacer. It hides the original prototypes after construction. Per-frame method `0x00BF9EC0` advances the vertical scroll using elapsed time, wraps to zero at the content boundary, and honors a widget behavior flag that suppresses automatic movement.

Input/message callback `0x00BF9A00` closes the mediator and changes the surrounding UI state when message code `1` arrives. Deactivation `0x00BF99C0` disables the update/input state, resets the scroll container, and clears generated records through `0x00BF9070`. Destructor `0x00BF98C0` releases the bound prototypes and base mediator state.

## Replacement contract

Credits are optional local content, not a protocol dependency. A replacement can render the same effective data table through a modern scroll view and style mapping. Preserve localization references and tolerate malformed column counts with diagnostics; do not couple login or world entry to this screen.

## Confidence

- **Confirmed by code/assets:** factory/constructor, widget prototypes, exact six-column validation, row/cell selection rules, `@`/`.`/empty-row handling, clone/style mapping, timed scrolling/wrap, input close, cleanup, and absence of a send.
- **Strongly inferred:** friendly lifecycle names and exact label for message code `1`.
- **Unknown/non-blocking:** original scroll-speed constant name and whether an external action independently starts credits music.

Ignored reproduction evidence: `local-private/generated-evidence/swg_cui_credits_*`, `ui_credits_effective.inc`, and `local-private/ghidra/decompiled/swg-cui-credits*/`.
