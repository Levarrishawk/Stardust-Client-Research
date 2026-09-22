# Stardust client analysis session — through 09:00 checkpoint

Date: 2026-09-22  
Canonical target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Mode: static analysis, effective-asset inspection and current-server correlation only

## Outcome

This continuation closed eight bounded research areas around server-driven UI, asset forward compatibility and lifecycle integration. It did not implement a replacement client, alter the executable or original assets, publish to GitHub, or issue a shutdown command.

The implementation-grade estimate advances conservatively from **95% to 96%**. The comprehensive whole-client estimate advances from **87% to 88%**. These remain architecture/compatibility estimates, not a claim that every compiler-generated helper has an original historical name.

The stable provisional-name registry contains **2,056 distinct executable addresses with zero duplicate address rows**. The authoritative server packet-source inventory remains **345 documented / 0 reachable gaps**; no new top-level packet family was discovered.

## Completed blocks

| Block | Principal result | Confidence |
|---|---|---|
| Data-driven SUI runtime | command types 1–7, command wire grammar, event adapters 1–10, property extraction and reliable event return mapped | Confirmed |
| SUI page-manager lifecycle | create/update/force-close/user-close/scene-change routes and page-ID registry ownership closed | Confirmed |
| Server SUI asset footprint | every root page named by current native/Lua producers found in the effective original UI overlay | Confirmed |
| Stardust SUI behavioral surface | current event/property/widget/expression/object-viewer requirements bounded; update subscription quirk identified | Confirmed |
| `PASS/0010` alignment | 57-byte layout resolved as 0009 plus one Boolean at offset `0x04`; true-value assets isolated | Confirmed structure; semantic label unknown |
| `IMPL/0009` schema | all 82 effective forward implementations use the executable-supported 0007 envelope grammar | Confirmed/Strong |
| Quest counter/timer reachability | controller mismatch retained, but current shipped content has no active producer | Confirmed |
| Session/process teardown integration | scene reset, disconnect and process-exit scopes plus worker drain/join ordering separated | Confirmed constraints / recommended replacement order |

## Most important findings

### Server-driven UI is an original-asset interpreter

The complete current route is:

```text
Stardust SuiPageData
  -> SuiCreatePageMessage / SuiUpdatePageMessage
  -> client message parser
  -> page-ID registry
  -> resolve original Script/PDA UI page
  -> execute commands against legacy widget/data-source tree
  -> subscribed UI event
  -> SuiEventNotification
  -> Stardust callback
```

The client refuses a missing/non-page root rather than synthesizing a layout. Current Stardust roots (`Script.listBox`, message/input/transfer/keypad/color picker, four calibration games and `PDA.Questpopup`) are all present in effective `stardust_3_01.tre` UI resources.

The replacement therefore needs legacy `.ui`/`.inc` parsing and typed widget/property adaptation before gameplay SUI can be compatible, even if the internal presentation uses a modern engine.

### SUI lifecycle and compatibility details

- Active pages are keyed by U32 server page ID; never turn that ID into a native pointer.
- Create applies initial commands before activation.
- Update ignores a missing/stale page safely.
- Server force-close removes a page without returning cancel.
- User/window close returns event 10 exactly once, then removes the page.
- Scene change drains all active data-driven pages.
- U64 target plus F32 close distance feed generic `CuiMediator` range closure.
- Type-5 event subscriptions are accepted only on initial creation and skipped on updates.
- Current scripts use events 1, 9 and 10; text values are sampled as properties on terminal events, so event 7 is presently unused.

Several current puzzle scripts attempt to replace callbacks in update batches. The original client deliberately skips those commands, so the attempted replacement has no effect. A replacement should preserve that behavior initially rather than silently changing server semantics.

### Forward shader records are now parseable

The effective overlay's unsupported version tags no longer imply unknown packed schemas:

```text
IMPL/0009 = IMPL/0007 envelope
  SCAP, optional OPTN, 7-byte DATA, counted PASS children

PASS/0010 = PASS/0009 DATA
  plus one inserted Boolean at byte offset 0x04
```

The new pass flag is zero in 104/108 records and one only in four passes across `ui_bar_glow` and two lava shaders. The true cases are precisely two programmable bloom programs and the dedicated lava heat-render program; same-shader fixed-function/ordinary passes are false. This strongly identifies an auxiliary post-process write/contribution role, though its historical name and exact target/channel remain unknown. All subsequent fields are aligned, so a safe decoder does not depend on that name.

### Quest controller mismatch is dormant, not resolved

Historical client IDs remain counter `0x440`, timer `0x441`; current Stardust defines counter `0x441` with a longer body and timer `0x444`. That collision is unsafe if activated. However, current shipped scripts never call the timer and no live journal map defines the optional counter operation. The mismatch should remain a versioned compatibility-profile decision, not be counted as a current active-content failure.

## Remaining work

The remaining **4% implementation-grade** work is concentrated in:

- integration and teardown cross-checks joining already decoded systems;
- rare original-asset variants and visual semantics, especially the one `PASS/0010` Boolean;
- long-tail optional gameplay/UI behaviors and dormant compatibility profiles;
- acceptance-fixture definition proving asset and protocol behavior across subsystem boundaries.

The remaining **12% comprehensive** breadth also includes dead/developer paths, exhaustive presentation fidelity, historical field names and generic compiler/container helpers. Those do not all gate a replacement architecture, but behaviorally material items should continue to be retired before the build decision.

## Recommended next direction

Continue integration-boundary sweeps with emphasis on original-asset failure/fallback behavior, skeletal LOD selection composition, rare appearance/animation fixtures, and session/scene teardown ordering. Keep engine implementation deferred until this complete-picture pass is intentionally declared sufficient.

## New checkpoint files

- `ANALYSIS_CHECKPOINT_2026-09-22_DATA_DRIVEN_SUI_RUNTIME.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_SUI_PAGE_MANAGER_LIFECYCLE.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_SERVER_SUI_ASSET_FOOTPRINT.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_STARDUST_SUI_BEHAVIORAL_SURFACE.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_PASS_0010_FIELD_ALIGNMENT.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_IMPL_0009_FORWARD_SCHEMA.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_QUEST_COUNTER_TIMER_REACHABILITY.md`
- `ANALYSIS_CHECKPOINT_2026-09-22_SESSION_TEARDOWN_INTEGRATION.md`
