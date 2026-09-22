# Analysis checkpoint: resource list for survey

Date: 2026-09-15

## Result

`ResourceListForSurveyMessage` (`0x8A64B1D5`) is closed from Stardust's zone/resource selection through the executable's generated parser to the active survey-tool UI rebuild.

## Wire layout

Stardust emits a U32 count, followed by records, then an ASCII survey type and U64 player ID. Generated executable parser `0x01199900` and record codec `0x01199D00` confirm each record is:

| Field | Wire type | Client record offset |
|---|---|---:|
| resource display name | ASCII | `+0x00` |
| resource object ID | U64 | `+0x10` |
| resource type/class | ASCII | `+0x18` |

Client records are 40 bytes because both strings are owning dynamic objects. Message members are the record vector at `+0x38` (backing storage `+0x3C`), survey-type ASCII at `+0x48` (data `+0x4C`), and player U64 at `+0x58` (data `+0x60`).

## Server selection policy

`ResourceSpawner::sendResourceListForSurvey` obtains the player's zone resource map, retains only active/in-shift resources compatible with the survey tool type (including the inorganic umbrella case), orders the selected resources through a `VectorMap`, appends name/type/object ID records, and finishes with the tool's survey type and player object ID. If no zone resource map exists, it sends a system failure message instead.

## Client dispatch and action

Listener `0x00E35550` matches the generated type descriptor, calls parser `0x01199900`, looks up the active survey UI mediator, and invokes `0x00F0F690`. The mediator clears its previous row/widget state, installs the response's player ID and survey type, iterates the decoded records, converts the two ASCII strings for presentation, and constructs selectable resource rows. This is a full list replacement, not a delta.

## End-to-end mapping

| Message/opcode | Parser | Handler | Result | Confidence |
|---|---|---|---|---|
| `ResourceListForSurveyMessage` / `0x8A64B1D5` Z->C | `0x01199900`, record `0x01199D00` | `0x00E35550` -> `0x00F0F690` | replace the active survey-tool resource selection list | Confirmed |

## Replacement requirements

- Preserve U32 count, ASCII strings and U64 identifiers exactly.
- Treat resource IDs as protocol-width U64 regardless of host pointer width.
- Bound count and string lengths before allocation.
- Preserve record order supplied by Stardust.
- Replace the old list atomically and associate it with the supplied survey type/player ID.
- Keep localized UI presentation separate from server-provided resource names/types.

## Remaining bounded unknowns

- Exact player-ID mismatch policy deeper in the mediator.
- Final row sorting/grouping behavior after the observed construction loop.

Neither blocks decoding or a headless compatibility implementation.
