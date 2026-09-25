# Stardust options and preferences architecture

Date: 2026-09-22

Scope: static analysis of the options mediator family in the exact
`Stardust.exe` baseline. Decompiled output is evidence, not original source.

## Confidence legend

- **Confirmed**: directly visible in executable code, RTTI/vtables, widget names,
  or call boundaries.
- **Strongly inferred**: behavior is unambiguous from several adjacent calls but
  the original source identifier is unavailable.
- **Unknown**: historical source naming or intent cannot be recovered safely.

## Shared mediator and binding model

`SwgCuiOptBase` is constructed at `0x00FEFA20`. It derives from the ordinary CUI
mediator, binds a `buttonDefaults` widget, reads the page's `PageName` property,
and owns an ordered/map-like collection of per-widget binding objects.

The binding registrations are not passive metadata. Each record contains:

- the target UI widget;
- a current-value getter;
- a setter;
- a default-value getter;
- numeric minimum/maximum data where applicable;
- an optional change callback and callback context.

The principal registration families are:

| Address | Provisional role | Evidence/confidence |
|---|---|---|
| `0x00FF00E0` | integer slider binding | U32/int limits, getter/setter/default callbacks, slider setup; **confirmed** |
| `0x00FF09A0` | floating slider binding | float limits mapped onto a UI range of 0..100; **confirmed** |
| `0x00FF0B80` | checkbox/Boolean binding | checkbox widget plus getter/setter/default triple; **confirmed** |
| `0x00FF0F60` | combo/list selection binding | selection widget plus getter/setter/default triple; **strongly inferred** |
| `0x00FF12D0` | integer slider shorthand | forwards with equal initial/current bound argument; **confirmed** |
| `0x00FF1300` | floating slider shorthand | forwards with equal initial/current bound argument; **confirmed** |

### Lifecycle

- `0x00FEFE30` iterates every binding during page activation and invokes its
  refresh path before the page-specific activation hook.
- `0x00FEFEC0` invokes each binding's capture-current path when the parent options
  dialog opens. Float/integer bindings retain that original value in the binding
  record.
- `0x00FEFE80` invokes each binding's restore path. The parent dialog uses this
  on Cancel or an abnormal close.
- Widget-change handlers `0x00FF0290` and `0x00FF0690` convert the widget value,
  invoke the preference setter immediately, update display text, and invoke an
  optional dependent-change callback. **Changes therefore preview live.**
- `0x00FEFF00(false)` creates a confirmation dialog for the Defaults button.
  Its callback (`0x00FF00B0` reached through the dialog callback object) returns
  to `0x00FEFF00(true)`, which iterates the binding collection and invokes each
  binding's default getter/setter path (`0x00FF0860`).
- Parent `SwgCuiOpt` (`0x00D58A40`) makes the dialog transactional. Activation
  (`0x00D59440`) snapshots every child page. The close path (`0x00D59520`)
  restores every child if the OK flag was not set. Deactivation (`0x00D594A0`)
  deactivates the children and calls the machine-options writer (`0x00B8D9F0`).
  Cancel therefore rolls back the live previews and persists the restored
  values; OK retains and persists the previews.
- The parent button callback at `0x00D594F0` compares the event source with the
  OK and Cancel widgets. OK leaves the commit flag set; Cancel clears it; both
  then close the mediator. This directly confirms the flag interpretation.

## Parent page composition

The parent constructs the complete page set in this exact numeric order:
`0 Afk`, `1 Alarm`, `2 Chat`, `3 ChatColors`, `4 Combat`, `5 Controls`,
`6 Graphics`, `7 Misc`, `8 Sound`, `9 Terrain`, `10 Ui`. A tab/data widget
selects among the child mediators; `buttonDefaultPage`, `buttonOk` and
`buttonCancel` are parent-owned controls. Every child is initially hidden and
the selected child is activated through the ordinary mediator lifecycle.

## Page inventory and confirmed domains

The following constructors and widget/binding sets are executable-confirmed:

| Page | Constructor | Confirmed settings/workflow |
|---|---:|---|
| UI | `0x00EA3100` | HUD opacity, radar display, Windows keys, popup help, palettes/icon sizing, overhead-map rotation, inventory rotation/sort, modifier window, resource-attribute flags, targeting reticle and space-camera elasticity |
| Terrain | `0x00EA3970` | high/normal terrain thresholds, far/near/non-collidable flora enables and ranges, water detail |
| Sound | `0x00EA3CE0` | effects/master/background/player/UI volume, sound enable, provider selection |
| Misc | `0x00EA4260` | invite/delete/crafting confirmations, network status, name size/range/categories, system messages, waypoints, screen shake, cockpit display |
| Graphics | `0x00EA4B60` | brightness/contrast/gamma, field of view, far plane, LOD and particle/nebula scales, detail/shadow/world/batch/camera settings, screenshot format/quality, fade/crossfade and bloom |
| Controls | `0x00EA54B0` | ground and pilot mouse/joystick response, inversion, inertia/dead zone, movement/targeting behavior, cockpit/POV controls and TrackIR capability-gated controls |
| Combat | `0x00EA5B70` | damage-number scale and visibility for self/others |
| Chat Colors | `0x00EA5E00` | combat/chat color list, word/sentence highlight lists, text editor, palette picker |
| Chat | `0x00EA7140` | font/bubble timing/size/range, input modes, emotes/prose, bubble audience filters, AFK speech, timestamps, profanity and auto-emote |
| Alarm | `0x00EA8CA0` | absolute/relative alarm modes, hours/minutes, recurring flag, sound/test, message and alarm list add/remove |
| AFK | `0x00EAA250` | automatic AFK enable, automatic response text and selectable AFK response set |

Chat Colors, Alarm and AFK are not merely binding tables. Their vtables replace
activation/deactivation/event methods with page-specific list population and
edit logic. Chat Colors also loads `palette/ui.pal`; Alarm owns a collection of
alarm records and a sound preview path; AFK enumerates a manager-owned response
range into its combo box.

### Chat Colors workflow

The Chat Colors callback at `0x00EA6C70` distinguishes three buttons:

- Highlight Word reads the currently selected substring from the text widget,
  obtains the active palette-picker color, and adds or updates a word-highlight
  rule.
- Highlight Sentence performs the parallel operation on the sentence-highlight
  collection.
- Remove obtains the selected row text from the active highlight list, removes
  it from the corresponding manager collection, and removes/refreshes the UI
  row.

After add/update, the page publishes a change notification and refreshes the
affected list through its list-change callback. `0x00EA6640` enables the two add
buttons only when the text widget has a non-empty selection (`selectionStart !=
selectionEnd`). Activation/deactivation at `0x00EA6460`/`0x00EA64A0` attach and
detach this custom callback surface.

The parent's scalar-binding rollback does **not** cover these highlight lists.
The page registers no scalar binding objects, does not override the parent's
restore slot, and the mutations at `0x005B6DD0`/`0x005B6DF0` and
`0x005B6E30`/`0x005B6E50` write the two global manager collections directly.
Consequently Chat Colors add/remove edits are immediate and survive Cancel in
this client. A modern client may deliberately improve this UX, but matching the
original means documenting the asymmetry.

### Alarm and AFK workflows

Alarm button callback `0x00EA9740` provides three explicit actions:

- Test resolves the selected sound record, stops/releases the previous preview
  handle, and starts the selected sound as a new preview.
- Add delegates the absolute/relative time, recurrence, sound and message widget
  state to the alarm-record insertion path at `0x00EA9CD0`.
- Remove delegates the selected alarm row to `0x00EA9F60`.

`0x00EA9870` handles the mutually exclusive Alarm At / Alarm In controls and
toggles the associated absolute-versus-relative editor widgets. The constructor
owns hour/minute selectors, recurrence, sound, message, add/remove buttons and
the alarm list; this is a local scheduler/editor, not a server protocol.
Add/remove call the global alarm manager (`0x005C4A00`/`0x005C4C70`) immediately
and repopulate from its collection (`0x005C50D0`). Alarm has no scalar bindings
or restore override, so these mutations also survive parent Cancel.

AFK constructs its response combo by enumerating the AFK manager's valid index
range. Checkbox callbacks `0x00EAA800`/`0x00EAA820` enable or disable the
automatic-response editor with automatic AFK. Its defaults override
`0x00EAA7B0` first runs the scalar default-confirmation path and, after
acceptance, resets the manager-owned automatic response/default selection and
refreshes the page (`0x00EAA4B0`). This is another custom preference controller,
not a network message path. Its deactivation override `0x00EAA630` writes the
custom checkbox/response state after either OK or Cancel; it likewise does not
participate in the scalar snapshot transaction.

The legacy dialog is therefore intentionally asymmetric: generic scalar pages
roll back on Cancel, while Chat Colors and Alarm collection edits and AFK custom
state bypass that rollback. A modern UI can improve atomicity, but compatibility
tests should capture the original behavior before changing it.

### Combat-page runtime effects

The Combat page's three settings are confirmed client-side presentation only.
Config install `0x009B2F30` reads `showDamageOverHeadOthers=true`,
`showDamageOverHeadSelf=true`, and the historically misspelled
`damageOverHeadSizeModifer=1.5`. `CuiCombatManager` install `0x008C74A0` copies
them into live state. Runtime path `0x00956A10` chooses the self or others flag
by comparing the affected object with the local player before allocating an
overhead-damage record; `0x009563B0` forwards the live size factor to the text
constructor. The slider range is `0.25..2.0`. No network send occurs in these
inspected paths. See the dedicated [runtime-effects checkpoint](ANALYSIS_CHECKPOINT_2026-09-25_COMBAT_OPTIONS_RUNTIME_EFFECTS.md).

## Replacement-client implications

1. Implement a typed settings registry independent of the UI toolkit. Preserve
   explicit getter/setter/default operations and numeric range metadata.
2. Expose live-change notifications so graphics, audio, camera, UI and input
   systems can react without reconstructing a page. Snapshot the settings on
   dialog entry and restore them on Cancel even though previews apply live.
3. Persist wire-neutral client preferences in 64-bit-safe host types, but retain
   original ranges/defaults where they determine visible behavior.
4. Treat hardware/capability gates (bloom, TrackIR, sound providers) as adapters,
   not hard-coded widgets.
5. Implement Chat Colors, Alarm and AFK as dedicated controllers over the same
   settings service. They require collection/edit operations beyond scalar
   bindings.
6. Persistence is already closed by the companion CUI-preferences and
   machine-options analyses: compare-before-write setters request persistence,
   publish typed notifications and ultimately serialize `FORM OPTN/FORM 0002`
   entries in `swgemu_machineoptions.iff`.

## Remaining uncertainty

- Original C++ member and binding-class names are unavailable.
- Disk writing is owned by the separate preferences service and is explicitly
  reached after dialog deactivation; see
  `ANALYSIS_CHECKPOINT_2026-09-22_CUI_PREFERENCES_RUNTIME.md` and
  `ANALYSIS_CHECKPOINT_2026-09-22_MACHINE_OPTIONS_IFF.md`.
- Obsolete provider and TrackIR choices may be intentionally omitted or adapted
  in a modern client, provided protocol and user-visible compatibility are not
  affected.
