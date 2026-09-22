# In-Game Menu Mediator

Date: 2026-09-22

## Result

`SwgCuiGameMenu` is a thin UI-action router. It owns no gameplay or network
state and introduces no protocol. Resume closes the modal; disconnect routes
to avatar/scene selection according to session state; exit invokes controlled
client shutdown; credits and the hidden locations tool open named mediators.

## Client evidence

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00C7D360` | `swg_cui_game_menu_construct_VA_00C7D360` | binds Resume, Disconnect, Exit, Credits and hidden Locations controls | Confirmed |
| `0x00C7D6E0` | `swg_cui_game_menu_activate_VA_00C7D6E0` | enters modal/menu input state | Strongly inferred |
| `0x00C7D700` | `swg_cui_game_menu_button_handler_VA_00C7D700` | closes menu or activates Backdrop plus SceneSelection/AvatarSelection/Credits/Locations | Confirmed principal routes |

The constructor explicitly hides `locationsButton`, corroborating the separate
classification of `SwgCuiLocations` as a developer tool.

## Replacement-client requirement

Route these actions through a game-state/session coordinator rather than
letting widgets tear down sockets or the process directly. Disconnect must
perform orderly scene/session cleanup before returning to selection. Exit must
use the established shutdown path so network, workers, assets and logs are
released deterministically.

## Residual uncertainty

Exact confirmation-dialog policy varies by current scene/session state and is
a UX detail. Action ownership and destinations are closed.
