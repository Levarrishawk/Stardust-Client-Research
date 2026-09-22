# CUI Preferences Runtime

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Findings

`0x0090FA10` is the `CuiPreferences` installation path. It initializes defaults
from the client configuration layer and registers typed persistent options in
the `ClientUserInterface` namespace. The recovered registrations distinguish
Boolean, integer, float and string values rather than serializing a raw C++
structure.

The registered surface includes, among others:

- camera inertia, first-person inertia and multiple space-camera controls;
- object-name category filters, range and font-size factor;
- network status, tooltips, system-message visibility/duration and popup help;
- ground-radar terrain, radar NPC/object-arrow display and radar selection;
- mouse mode and modal-chat settings separately for ground and space scenes;
- waypoint/experience monitors and onscreen waypoint arrows;
- UI palette, HUD opacity and drop shadows;
- inventory/datapad auto-sort and rotating inventory objects;
- joystick inversion, sensitivity, dead zone, POV-hat behavior and pilot mouse
  mode;
- screen shake, targeting reticle, ship destruction/debris and nebula controls;
  and
- tutorial, chat, confirmation and speech-display policy.

This confirms a layered preference system: configuration supplies initial
defaults, while the typed option manager owns mutable persisted values. Setters
such as `0x00910AC0` and `0x00910B10` compare-before-write, request persistence,
publish typed UI notifications, and trigger the machine-options save path.
`0x009106D0` unregisters/relinquishes the installed callback objects during
shutdown.

The machine-options filename embedded in this build is
`swgemu_machineoptions.iff`; this is a compiler/build-brand artifact in
`Stardust.exe`, not evidence that another executable was analyzed.

## Scene-specific policy

Ground and space retain separate mouse-mode-default and modal-chat values.
`0x00910A10`, `0x00910A70`, `0x00910CB0` and `0x00910D40` reject unknown scene
types. `0x00910E10` additionally combines the configured mouse-mode toggle with
current input/UI state. The replacement must therefore preserve scene-scoped
preference semantics rather than flattening all controls into one global flag.

## Palette application

`0x00910EC0` resolves a named palette under the active palette folder, validates
space-specific eligibility, applies the palette to the root UI system, and can
persist the selected name. If a selected palette cannot be used in the active
space mode, it falls back through the configured override palette. Original
palette definitions are consequently runtime assets, not cosmetic constants.

## Replacement design

Use a schema-based preference registry:

`compiled default -> cfg override -> profile/machine option -> runtime value`

Each entry should have a stable textual key, explicit type, validator/clamp,
scope (machine/profile/character and ground/space where relevant), change event
and side-effect adapter. Persist portable values rather than in-memory layouts.
Unknown legacy keys should be retained where feasible so loading and saving a
profile does not erase settings owned by later compatibility work.

## Confidence

| Finding | Confidence | Evidence |
|---|---|---|
| Typed `ClientUserInterface` preference registration | Confirmed | calls and literal keys in `0x0090FA10` |
| Defaults originate in configuration accessors | Strongly inferred | initialization calls preceding typed registration |
| Mutations publish and persist | Confirmed | setter paths into notification and machine-option save helpers |
| Ground/space mouse and modal-chat settings are distinct | Confirmed | paired storage and checked scene dispatch |
| Palette is an asset-backed runtime selection | Confirmed | tree lookup, palette validation/application in `0x00910EC0` |

The binary grammar is closed separately in
`ANALYSIS_CHECKPOINT_2026-09-22_MACHINE_OPTIONS_IFF.md`.
