# Analysis checkpoint — login-adjacent SceneSelection local path

Question A09 / W10+W02. Target: verified original `Stardust.exe`; decompiler output is reconstructed pseudocode. This is the `SceneSelection` destination reached from the LoginScreen **Local** action already traced in the [login checkpoint](ANALYSIS_CHECKPOINT_2026-09-24_LOGIN_SCREEN_UI_TO_CONNECTION.md). It is a **local scene path**, not a newly identified Stardust server packet flow.

MSVC RTTI `SwgCuiSceneSelection` leads to main vtable `0x015EF368` and callback subobject table `0x015EF2E4`. Constructor `0x00BE0B80` binds `OkButton`, `CancelButton`, `buttonCreate`, description text, placeholder image, selection listbox and textbox. Missing required UI widgets take a fatal path. Activation `0x00BE0E30` changes mediator/input flags, refreshes selected-row presentation via `0x00BE1290`, attaches four UI callbacks, and activates the `SceneSelection` mediator through the common mediator manager. `0x00BE1290` reads selected-item metadata keys `screenshot`, `Description`, and `filename` from the listbox. These are UI data properties, not wire fields.

The button handler `0x00BE0FB0` distinguishes three bound widgets by pointer identity. `CancelButton` transitions back to `LoginScreen`. `buttonCreate` requests `AvatarCreation` after validating a non-empty edit string; the exact meaning of that string for creation remains to be resolved. `OkButton` calls `0x00BE1570` with a default player-template path or one supplied by the local selection state. `0x00BE1570` rejects an empty selection or missing template, displays a loading message, prepares local scene/UI state, allocates a 0xF4-byte `GroundScene` through `0x00519830`, and transfers ownership to `game_set_scene_VA_00424220`; on success the mediator is hidden. `0x00519830` is a `GroundScene` constructor by its embedded class string and RTTI checks. No login-server endpoint selection or outgoing packet construction appears in this path.

| Event | Client path | Result | Confidence |
|---|---|---|---|
| LoginScreen Local | previously mapped `0x00C8D460` -> `SceneSelection` | Opens local scene chooser | Confirmed by code |
| Selection change/activation | `0x00BE0E30` -> `0x00BE1290` | Screenshot, description, filename presentation | Confirmed by code for property lookups; data-source provenance open |
| Cancel | `0x00BE0FB0` -> mediator transition | Returns to LoginScreen | Confirmed by code |
| OK | `0x00BE0FB0` -> `0x00BE1570` -> `0x00519830` -> `0x00424220` | Creates and installs local GroundScene | Confirmed by code; exact scene data source remains open |
| Create | `0x00BE0FB0` -> mediator transition | Opens AvatarCreation | Confirmed by code; subsequent creation flow separate |

The Stardust server source is not an input to this local chooser path. This boundary must not be conflated with server-driven `SelectCharacter`, zone authentication, or scene-ready protocol messages. A replacement's first networking proof of concept does **not** require reproducing this screen; original-asset/local-scene compatibility may require it later.

Evidence: executable `0x00BE0B80`, `0x00BE0E30`, `0x00BE0FB0`, `0x00BE1290`, `0x00BE1570`, `0x00519830`, main/subobject vtables above; [scene lifecycle](../../docs/SCENE_LIFECYCLE_ARCHITECTURE.md). Outcome: new UI/session sub-boundary, not whole W10 closure; no whole-client percentage change.
