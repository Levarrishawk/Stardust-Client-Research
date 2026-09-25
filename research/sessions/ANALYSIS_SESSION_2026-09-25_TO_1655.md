# Analysis session — 2026-09-25 to 16:55 EDT

Target: verified `C:\Stardust-DEV\Stardust.exe`, configured original assets, and existing research evidence. No replacement-client code, client execution, commit, or push occurred.

## Blocks completed

1. **A03 — root-motion object application: closed to replacement depth.** The CKAT/physical-track delta reaches `CreatureController` through the animation-controller and skeletal-appearance chain. Rotation is post-composed with the object's local transform; translation is rotated by the object's current local orientation and added in parent space. This closes the external basis/application boundary for ordinary and combined QCHN+LOCT locomotion. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_ROOT_MOTION_OBJECT_APPLICATION.md).
2. **R01 — PASS/0010 true-value reachability: bounded.** The only three shaders/four passes carrying a true value have no decompressed full-path or basename reference in configured TRE payloads, the executable, or current server source. They are strongly inferred dormant/orphaned current configuration, not proven impossible through synthesized/CRC/admin paths. The field remains opaque and preserved. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_PASS_0010_TRUE_REACHABILITY.md).
3. **A09 refinement — Combat options runtime effects: closed.** Damage-number size (`0.25..2.0`) and self/others visibility were connected from config defaults through live setters to their exact presentation consumers. These are client-only overhead-text preferences with no send or authoritative combat effect. This refines an already counted options-family block and is not double-counted as new UI breadth. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_COMBAT_OPTIONS_RUNTIME_EFFECTS.md).
4. **A09/R01 — News mediator and effective asset: bounded.** `SwgCuiNews` is factory-present and binds Back/Next/Intro/content/image objects, but its active priority-53 `ui/ui_news.inc` override is zero bytes. It is not a current gameplay/protocol dependency; lower-priority historical copies are shadowed. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_NEWS_MEDIATOR_AND_DISABLED_ASSET.md).
5. **A09/A04/R01 — Splash to intro-scene startup handoff: closed.** The current 13,610-byte splash asset selects Japanese/JTL/non-JTL variants. `SwgCuiSplash` advances three pages using `splashTimeoutSeconds`, pauses timing while active `CuiMessageBox` entries exist, cleans up its listener/input state, and constructs/installs the confirmed `scene/intro.iff` controller. This resolves the prior normal-reachability unknown for the intro scene. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_SPLASH_TO_INTRO_SCENE.md).
6. **A09/R01 — Credits local data-table workflow: closed.** The nonempty Credits asset supplies seven style prototypes; executable code validates and parses a six-column table, preserves localization references, clones styled children, scrolls/wraps them over time, closes on input and cleans up. It is wholly local and has no server/protocol dependency. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_CREDITS_LOCAL_DATATABLE_WORKFLOW.md).

## Evidence/tooling improvement

`tools/ExportIndirectCallsByDisplacement.java` is a reusable Ghidra discovery probe for indirect CALL operands containing a requested displacement. It was used only to find candidates; every resulting semantic conclusion was established from typed caller/callee decompilation and existing transform contracts.

## Progress accounting

Six bounded blocks were completed: two replacement contracts closed, one dormant shader edge was bounded, one existing UI family was refined, and three local/startup UI slices were reconstructed. The historical **96% implementation-grade / 90% comprehensive** figures remain suspended pending the current evidence-based rescore. No percentage was advanced from block count alone. The provisional-name ledger and workspace validation provide separate reproducible counts below.

## Next target

Continue the A09/R01 audit with a genuinely reachable, previously untraced workflow. Prefer a class with a nonempty effective asset and proven factory/action caller. Do not reopen root-motion basis, PASS/0010 current literal reachability, Combat option consumers, News, or Splash without contradictory evidence.
