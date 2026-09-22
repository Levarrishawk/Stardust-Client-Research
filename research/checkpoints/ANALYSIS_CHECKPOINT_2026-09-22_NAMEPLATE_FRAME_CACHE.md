# Nameplate frame-cache fields

Date: 2026-09-22

## Result

The two formerly unnamed integer pairs in each `CuiObjectTextManager` cache
entry are current/previous-frame presentation values:

| Entry offsets | Meaning | Reset |
|---|---|---|
| `+0x30/+0x34` | current and previous vertical nameplate/layout displacement | current -> previous, current = 0 |
| `+0x38/+0x3C` | current and previous projected screen-Y | current -> previous, current = 10000 |

Maintenance `0x008A8A00` performs both rolls together at the frame-age boundary.
The sentinel `10000` means no usable projected screen-Y for the new frame.

## Evidence

`0x008A7CD0` returns `+0x30` and optionally writes `+0x34`; `0x008A7DD0`
sets `+0x30`. Direct UI consumers add the returned value to the projected
label Y coordinate and store the resulting displacement minus widget height,
which identifies it as the vertical collision/stacking displacement.

`0x008A7F20` returns `+0x38` and optionally writes `+0x3C`, defaulting both
outputs to 10000 when no entry exists; `0x008A8030` sets `+0x38`. Its writers
pass the Y member produced by the world-to-screen/layout projection path.
`0x008A8180` consumes the current/previous value while constructing a smoothed
screen anchor. This identifies the second pair as projected screen-Y history.

`0x008A8810` is a separate lazy world-query/visibility result cache. It stores
the Boolean result at `+0x40`, valid flag at `+0x41`, and accompanying float at
`+0x44`; frame maintenance clears only the valid flag. It must not be conflated
with either integer history pair.

## Replacement-client consequences

- Nameplate overlap/layout may use redesigned algorithms, but temporal state
  must be per entity and rolled once at a defined UI-frame boundary.
- Use an explicit optional projected position rather than carrying sentinel
  `10000` internally; preserve the original sentinel only where fixture
  comparison requires it.
- Invalidate the cached visibility/query result each frame independently of
  projected-position and layout histories.
- Entity keys must remain lifetime-safe NetworkIds/handles, not pointers.

## Confidence

- Pair topology, frame roll/reset, getter/setter ownership and direct consumer
  arithmetic: **confirmed by code**.
- Friendly labels "vertical layout displacement" and "projected screen-Y":
  **strong**, based on projection and widget-placement consumers.
- Exact historical C++ member names: **unknown and unnecessary**.

