# Combat special-move visual metadata and playback bridge

Scope: static analysis of the effective `combat/special_move_visuals.iff`, its `Stardust.exe` loader/lookup functions, and the sole lookup consumer. This is a client-presentation service, not an opcode or authoritative combat-rule table.

## Confirmed file contract

The effective file is an SOE IFF `FORM CSMV` containing `FORM 0000` and fourteen `MOVE` chunks. Each `MOVE` payload is an unsigned-byte visual ID, a NUL-terminated marker-appearance path, and a little-endian float32 lifetime. All fourteen shipped lifetimes are `3.0` seconds.

| ID | Marker appearance |
|---:|---|
| 1 | `combat/appearance/ui_marker_target.mkr` |
| 2 | `combat/appearance/ui_marker_blinded.mkr` |
| 3 | `combat/appearance/ui_marker_damage.mkr` |
| 4 | `combat/appearance/ui_marker_dizzied.mkr` |
| 5 | `combat/appearance/ui_marker_duck.mkr` |
| 6 | `combat/appearance/ui_marker_intimidated.mkr` |
| 7 | `combat/appearance/ui_marker_knockdown.mkr` |
| 8 | `combat/appearance/ui_marker_startle.mkr` |
| 9 | `combat/appearance/ui_marker_startled.mkr` |
| 10 | `combat/appearance/ui_marker_stunned.mkr` |
| 11 | `combat/appearance/ui_marker_surprised.mkr` |
| 12 | `combat/appearance/ui_marker_target_body.mkr` |
| 13 | `combat/appearance/ui_marker_target_head.mkr` |
| 14 | `combat/appearance/ui_marker_target_leg.mkr` |

The corpus uses back-to-back IFF chunks without an added pad byte after odd payload sizes. `parse_special_move_visuals.py` preserves that observed rule.

## Confirmed runtime model

| Address | Provisional name | Behavior | Confidence |
|---|---|---|---|
| `0x005A8FE0` | `CombatSpecialMoveVisualRecord::ctor` | Stores ID, persistent CRC string, and float lifetime in a 24-byte record. | Confirmed |
| `0x005A90D0` | `CombatSpecialMoveVisualData::load_0000` | Reads `MOVE` chunks, allocates records, stores pointers, and sorts by ID. | Confirmed |
| `0x005A9450` | `CombatSpecialMoveVisualData::install` | Opens `CSMV`, dispatches `0000`, marks installed, and registers cleanup. | Confirmed |
| `0x005A9580` | `CombatSpecialMoveVisualData::lookup` | Binary-searches by ID and returns marker plus lifetime; a miss returns false and clears the marker result. | Confirmed |
| `0x005A9010` | `CombatSpecialMoveVisualData::remove` | Destroys strings/records, frees the pointer vector, and clears installed state. | Confirmed |
| `0x004660F0` | `scheduleCombatSpecialMoveMarker` | Creates a ref-counted callback payload containing a scene object and visual ID, then schedules it. | Strongly inferred |
| `0x00466160` | `combatSpecialMoveMarkerCallback` | Resolves the ID, fetches its appearance, creates a temporary marker with the configured lifetime, and adds it through presentation services. | Strongly inferred |
| `0x00469550` | `applyCombatPlaybackAction` | Handles attacker/defender combat playback; for the local-player side, schedules marker IDs from two action fields, then continues posture/script-selector playback. | Strongly inferred |

The callback is the lookup's only executable caller, so this is the complete observed table-consumption path.

```mermaid
flowchart LR
  A[combat playback action] -->|attacker/defender marker id| B[schedule callback]
  B --> C[lookup sorted CSMV record]
  C -->|marker resource + 3.0 s| D[fetch appearance]
  D --> E[create temporary presentation object]
  E --> F[add near referenced combat object]
```

## Replacement-client implications

- Preserve these IDs as an asset-driven presentation namespace, not server combat opcodes.
- Combat playback supplies marker identifiers; the client resolves original `.mkr` assets locally.
- Missing IDs are intentionally nonfatal: no marker is created.
- Retain file-driven resource/lifetime indirection rather than hard-coding it into combat rules.
- A pending callback needs an x64-safe entity/scene handle. Do not serialize or merely widen a legacy raw pointer.

## Remaining uncertainty

- The marker object's precise class and attachment transform are not yet recovered.
- This block proves how decoded combat playback triggers local markers; it does not independently identify every originating wire field. That boundary remains in the separately documented combat playback/message path.
