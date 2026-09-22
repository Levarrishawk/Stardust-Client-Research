# Vehicle hover-dynamics tuning prototype

Date: 2026-09-22

## Result

`CuiVehicleProto` is a live client-side developer/tuning panel for
`VehicleHoverDynamics`, not the production vehicle-control UI and not a network
protocol. It exposes twelve sliders, reads their initial values from the current
vehicle dynamics object, and writes changed values directly back to that local
dynamics instance.

## Functions and parameters

| Address | Provisional name | Role |
|---|---|---|
| `0x013BD170` | `cui_vehicle_proto_construct_VA_013BD170` | Constructs the panel and binds twelve slider/value-widget pairs. |
| `0x013BD340` | `cui_vehicle_proto_destroy_VA_013BD340` | Releases UI callback state. |
| `0x013BD3B0` | `cui_vehicle_proto_activate_VA_013BD3B0` | Resolves controlled vehicle, requires `VehicleHoverDynamics`, reads all twelve parameters and seeds sliders. |
| `0x013BD540` | `cui_vehicle_proto_deactivate_VA_013BD540` | Removes the input context. |
| `0x013BD620` | `cui_vehicle_proto_find_slider_VA_013BD620` | Maps slider widget to parameter index. |
| `0x013BD650` | `cui_vehicle_proto_find_value_widget_VA_013BD650` | Maps paired value widget to parameter index. |
| `0x013BD680` | `cui_vehicle_proto_apply_slider_VA_013BD680` | Converts a slider position to a bounded float and writes the indexed dynamics property. |
| `0x013BD710` | `cui_vehicle_proto_handle_value_edit_VA_013BD710` | Converts an edited value back to the same indexed property and synchronizes its slider. |
| `0x013BCE80` | `cui_vehicle_proto_slider_to_value_VA_013BCE80` | Maps slider position through indexed min/max and updates text. |
| `0x013BCF60` | `cui_vehicle_proto_value_to_slider_VA_013BCF60` | Performs the inverse float-to-slider conversion. |
| `0x013BCFA0` | `vehicle_hover_parameter_set_dispatch_VA_013BCFA0` | Dispatches ordinary indexed setters. |
| `0x013BCFC0` | `vehicle_hover_parameter_get_dispatch_VA_013BCFC0` | Dispatches ordinary indexed getters. |
| `0x013BCFE0` | `vehicle_hover_special_parameter_set_dispatch_VA_013BCFE0` | Dispatches the first three specialized setters. |
| `0x013BD000` | `vehicle_hover_special_parameter_get_dispatch_VA_013BD000` | Dispatches the first three specialized getters. |
| `0x013BD020` | `vehicle_hover_parameter_get_VA_013BD020` | Selects specialized or ordinary getter by index. |
| `0x013BD050` | `vehicle_hover_parameter_set_VA_013BD050` | Selects specialized or ordinary setter by index. |
| `0x013BD080` | `cui_vehicle_proto_resolve_hover_dynamics_VA_013BD080` | Resolves controlled creature and finds `VehicleHoverDynamics`. |

The serialized UI control names preserve the intended twelve parameters:
`moveScale`, `accelScale`, `turnScale`, `dampRoll`, `dampPitch`, `dampHeight`,
`glide`, `pitchAccel`, `pitchDecel`, `rollTurn`, `autoLevel`, and `hoverHeight`.

Each parameter has a compiled min/max conversion pair; the panel sets slider
range 0..1000 and maps that integer range to the float. These bounds are useful
evidence about the old hover model but are tuning UI, not a portable simulation
contract by themselves.

Indices below three use distinct getter/setter tables; later indices use the
ordinary tables, and both UI directions share the compiled range pairs.
Activation resolves the supplied creature or current player, follows the
controlled-object path when present, and scans dynamics children for
`VehicleHoverDynamics` by RTTI.

## Classification and replacement impact

- No message construction, command queue or server call occurs in this class.
- No direct production invocation string was found in the effective input/UI
  event inventories; the class is registered with the mediator factory and can
  be retained as developer tooling evidence.
- Production vehicle movement remains governed by the vehicle dynamics/controller
  paths and server state already analyzed elsewhere.

A replacement should not treat this panel as required gameplay UI. Preserve the
twelve conceptual controls in a development-only diagnostics/tuning facility if
the recreated hover model benefits from them. Do not expose them as networked or
authoritative player settings.

## Confidence

- Local tuning-panel role and twelve parameter names: **Confirmed**.
- Direct mutation of `VehicleHoverDynamics`: **Confirmed**.
- No wire protocol: **Confirmed**.
- Original production availability/hidden command route: **Unknown; no effective
  invocation found and not required for compatibility**.
