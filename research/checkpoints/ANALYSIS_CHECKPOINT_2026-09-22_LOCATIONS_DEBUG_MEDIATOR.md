# Locations Debug Mediator

Date: 2026-09-22

## Result

`SwgCuiLocations` is not the ordinary planet-map or waypoint browser. It is a
developer/debug location selector that enumerates planets and named locations,
then executes a `planetwarp` operation or constructs a local terrain context.
It is not required for normal Stardust protocol compatibility.

## Client evidence

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00C89050` | `swg_cui_locations_construct_VA_00C89050` | binds planet/location lists, row sample and OK/cancel controls | Confirmed |
| `0x00C893B0` | `swg_cui_locations_activate_VA_00C893B0` | populates the available planet/location model and subscribes UI callbacks | Strongly inferred |
| `0x00C894B0` | `swg_cui_locations_confirm_VA_00C894B0` | validates selection, formats coordinates, invokes `planetwarp`, and contains a local `terrain/%s.trn` validation/creation branch | Confirmed principal behavior |
| `0x00C89A10` | `swg_cui_locations_selection_changed_VA_00C89A10` | rebuilds locations for the selected planet and updates list visibility/selection | Strongly inferred |

The literal diagnostic `You must select a valid location`, command name
`planetwarp`, terrain path `terrain/%s.trn`, and warning for a missing terrain
file establish the developer nature of this mediator.

## Replacement decision

Do not treat this as a player-facing gameplay subsystem or a required network
message. A modern client may omit it from release builds and provide equivalent
functionality through a gated developer console/tool. If retained, the warp
must still pass through the in-game command authority and never become a local
authoritative teleport.

The local terrain-construction branch is useful as an engine-development
diagnostic because it can validate original `.trn` consumption independently
of a live gameplay session.

## Confidence and residuals

Debug purpose, controls, `planetwarp` handoff and terrain probe are
**Confirmed**. The exact provenance of the named-location list and every local
preview branch are low-priority developer-tool details.
