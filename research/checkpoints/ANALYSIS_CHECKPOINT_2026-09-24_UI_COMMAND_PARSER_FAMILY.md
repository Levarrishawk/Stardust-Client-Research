# `/ui` command parser: local settings and diagnostic subtrees

## Scope

Static reconstruction from the original `C:\Stardust-DEV\Stardust.exe`. This block covers `SwgCuiCommandParserUI` and its three installed child parsers, **not** the much larger `SwgCuiCommandParserDefault` parse function. Decompiled C is reconstructed pseudocode. No new network message is inferred from a command name alone.

## Registration and dispatch

RTTI gives `SwgCuiCommandParserUI` vtable `0x015F8100`. Constructor `0x01027160` installs a root described as `ui commands` and constructs three children: `MessageBoxParser` at `0x01027DB0`, `ShowParser` at `0x010280F0`, and `ChatBubbleParser` at `0x010291C0`. The root dispatch `0x01027290` uses literal comparisons, with no opaque opcode table in this method.

| Root token | Observed behavior | Boundary |
|---|---|---|
| `reticleDeadZone` | query reports current two-dimensional extents; set parses one or two integers and calls the reticle/input update helpers | local HUD/input state |
| `cameraInertia` | query reports static camera inertia; set parses float and calls camera option helper | local camera state |
| `pointerModeMouseCamera` | query/toggle mouse-camera mode and report enabled/disabled | local input/camera state |
| `action` | joins remaining arguments and calls `0x008CC460` | action dispatch may lead to ordinary game/network commands; not a new packet by itself |
| `hudReset` | marks HUD reset and reports that it occurs after disconnect | local/persistent UI state |
| `palette` | lists available palettes or selects one via `0x00910EC0` | local UI theme |
| `inputScheme` | lists/sets an input scheme through `0x006E1B40` | local input binding/profile state |
| `reticleSelect`, `radarSelect` | require numeric argument, then call `0x00911820`/`0x00911850` | local HUD style |

`MessageBoxParser` (`0x01027F80`) registers list/create/property/text/button commands. The inspected parse method confirms list and create branches (creation calls `0x00895AD0`); other commands are registered for child/object handling but their detailed property mutation is not recovered here. `ShowParser` (`0x01028240`) registers object-tree, child-object listing and UI object-property inspection. It resolves a UI object by path and enumerates properties; this is an introspection/debug surface, not evidence of world-object replication. `ChatBubbleParser` (`0x01029270`) exposes `durationScale`, `durationMin`, `durationMax`, `enabled`, and `minimized`, routing numeric setters to `0x00954720`/`30`/`40` and toggles to `0x00954700`/`0x009546E0`.

The sibling `SwgCuiCommandParserDefault` constructor `0x01010980` registers `warpme`, `drawNetworkIds` and child parser objects including ship/UI-adjacent parsers, but its 2,139-line decompiled parse entry `0x010116C0` was only inventoried during this block. It contains alias/profile and some network-send paths and remains a separate, unclosed analysis target; those branches are **not** being counted as understood here.

## Confidence and replacement implications

**Confirmed:** parser RTTI, constructors, literal tokens, root branches and listed local helper targets. **Strongly inferred:** most `/ui` tokens are developer/diagnostic controls over local UI settings. **Unknown:** full MessageBox property handling, whether all UI commands should be supported in a replacement, and which `action` arguments ultimately enqueue network commands. A modern client can preserve command intent through a typed local command registry without reproducing these original parser class layouts.

No new opcode, packet parser or whole-client percentage is claimed.
