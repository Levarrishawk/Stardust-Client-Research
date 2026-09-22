# Analysis checkpoint: survey result grid

Date: 2026-09-15

## Result

`SurveyMessage` (`0x877F79AC`) is closed from Stardust density-grid generation through the executable's generated point parser and survey-map UI construction.

## Wire and codec

The packet is a U32 count followed by fixed 16-byte records. Client parser `0x01199160`, vector parser `0x01199480`, and serializer `0x01199430` establish four F32 values per record:

| Offset | Meaning | Evidence |
|---:|---|---|
| `+0x00` | world X | server writer and client range scan |
| `+0x04` | world Z/reserved, currently zero | server literal zero |
| `+0x08` | world Y | server writer and client range scan |
| `+0x0C` | density fraction | server writer and client threshold/display path |

## Server behavior

`ResourceSpawner::sendSurvey` validates the active survey session/tool, selected resource and zone. It clamps tool range to a default of 320 outside 0..1024 and point count to 3 outside 1..6, constructs a square `points × points` grid centered on the player, samples authoritative resource density at each x/y point, and queues the message through the survey session. It also creates/updates a blue resource waypoint at the maximum-density point when density is at least 0.1.

## Client behavior

Listener `0x00E35550` parses the message with `0x01199160` and passes it to active survey UI method `0x00F0FD70`. The UI clears prior visualization state, derives plot scale from X/Y extents (`0x00F0FF00`), establishes world-to-widget mapping, creates a visual sample for each point meeting its display threshold through `0x00F0FFA0`, shows an empty/no-result message if none qualify, and marks the map for refresh.

## Mapping

| Message/opcode | Parser | Handler | Result | Confidence |
|---|---|---|---|---|
| `SurveyMessage` / `0x877F79AC` Z->C | `0x01199160` / `0x01199480` | `0x00E35550` -> `0x00F0FD70` | replace and render the survey density plot | Confirmed |

## Replacement requirements

- Preserve U32 count and four F32 fields, including the reserved/zero Z field.
- Retain density as a fraction on the wire; presentation may convert to percent.
- Bound point counts and reject non-finite coordinates/densities.
- Compute plot extents independently of rendering technology.
- Keep the server-created maximum-density waypoint separate from client plot rendering.

## Remaining bounded unknowns

- Exact numeric client display threshold constant.
- Original widget material/texture names and final marker styling.

These are presentation details and do not block protocol compatibility.
