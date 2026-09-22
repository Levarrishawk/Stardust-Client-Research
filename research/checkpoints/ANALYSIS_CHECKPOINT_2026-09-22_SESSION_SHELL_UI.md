# Session Shell and Loading UI

Date: 2026-09-22

## Result

The non-gameplay shell is a set of distinct mediators rather than one monolithic
login screen. Ground/space loading, scene selection, news, disconnect and
splash presentation are locally driven shells around the previously mapped
login/session state machine. They add no hidden authentication or transport
protocol.

## Concrete mediators

| Address | Provisional name | Confirmed controls/state |
|---|---|---|
| `0x00BA8180` | `swg_cui_loading_ground_construct_VA_00BA8180` | progress, description/planet text, flag/planet/screenshot art, screenshot caption, previous button and five readiness diagnostics |
| `0x00BA9530` | `swg_cui_loading_space_construct_VA_00BA9530` | screenshot/caption, progress text, space-loading content pane and back control |
| `0x00BE0B80` | `swg_cui_scene_selection_construct_VA_00BE0B80` | selection list, description, placeholder image, OK/cancel/create and text input |
| `0x00CA8470` | `swg_cui_news_construct_VA_00CA8470` | back/next/intro navigation, text and image |
| `0x00CA87C0` | `swg_cui_server_disconnected_construct_VA_00CA87C0` | dedicated disconnect page using the `ServerDisconnected` UI path |
| `0x00CA93C0` | `swg_cui_splash_construct_VA_00CA93C0` | variant splash pages and executable/version text selected by game-feature/platform configuration |

## Ground readiness model

The ground-loading mediator explicitly exposes five internal diagnostic flags:

```text
server objects
file caching
world loaded
terrain generated
player ready
```

These flags corroborate the previously reconstructed scene bootstrap ordering:
receiving the start-scene instruction is not sufficient to expose gameplay.
Object bootstrap, resource/cache work, world construction, terrain readiness
and local-player readiness are separate conditions. The retail UI hides these
check widgets in normal presentation but retains them as diagnostic state.

## Architecture consequence

The replacement session coordinator should publish explicit progress phases and
readiness gates. Loading UI consumes those phases; it must not itself own
network connection state or resource loaders. Ground and space can share a
common progress interface while retaining different visual content and asset
requirements.

Scene selection belongs to session/game-state management, news and splash are
content presentation, and disconnect is a terminal/recovery state. None should
be coupled to the renderer backend.

## Original-asset dependency

Screenshots, planet/flag art, news images/text, splash variants, localization
and the declarative page hierarchy come from the original asset corpus. Asset
compatibility is therefore required even before world rendering if faithful
shell presentation is desired. A networking proof of concept may replace them
with diagnostic text, but the full client cannot discard these lookup paths.

## Confidence and residuals

Class identities, constructors, controls and ground readiness categories are
**Confirmed**. Exact progress weighting, screenshot rotation policy and every
feature-bit splash variant are presentation fidelity and can be validated when
the shell is implemented.
