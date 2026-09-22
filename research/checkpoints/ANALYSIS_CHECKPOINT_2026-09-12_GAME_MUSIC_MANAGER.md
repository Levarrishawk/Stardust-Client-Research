# Game Music Manager State and Asset Contract

Date: 2026-09-12  
Executable: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This checkpoint reconstructs the data contract and principal runtime policy of the state-driven music system adjacent to `PlayMusicMessage`. It establishes how the client loads `scene/game_music_manager.iff`, selects combat/space tracks, pairs space entry and exit music, and yields to temporary overrides.

## Asset format

The effective overlay winner `scene/game_music_manager.iff` is in `mtg_patch_017.tre` and has this structure:

```text
FORM GMUS
  FORM 0000
    ENTR <null-terminated sound path>
    EXIT <null-terminated sound path>
    DETH <null-terminated sound path>
    INCP <null-terminated sound path>
    SENT <null-terminated sound path>  repeated
    SEXI <null-terminated sound path>  repeated
    WATR <null-terminated sound path>
```

The loader at `0x006C8920` opens the literal path `scene/game_music_manager.iff`, enters `GMUS/0000`, iterates its child chunks, interns/validates every sound path through `0x004568C0`, and stores owned path objects by tag. This is **Confirmed by code and asset bytes**.

The installed file contains:

- `ENTR`: `sound/music_combat_loop.snd`
- `EXIT`: `sound/music_combat_victory.snd`
- `DETH`: `sound/music_combat_defeat.snd`
- `INCP`: `sound/music_combat_defeat.snd`
- three repeated `SENT` space-battle loop tracks;
- three positionally corresponding `SEXI` space-battle victory tracks;
- `WATR`: `sound/amb_seashore_outside.snd`.

Although ENTR/EXIT are singular in this particular asset, the loader stores ENTR, EXIT, DETH, INCP, SENT, and SEXI as vectors. It stores WATR as a singleton. The replacement loader must therefore accept repeated chunks for all six vector-backed tags and must not hard-code the current counts.

## Loader storage map

| Tag | Client storage | Shape | Strong semantic label |
|---|---:|---|---|
| `ENTR` | `0x0191B528..530` | vector | ground-combat entry/loop |
| `EXIT` | `0x0191B51C..524` | vector | ground-combat exit/victory |
| `DETH` | `0x0191B534..53C` | vector | death/defeat |
| `INCP` | `0x0191B510..518` | vector | incapacitation/defeat |
| `SENT` | `0x0191B504..50C` | vector | space-combat entry/loop |
| `SEXI` | `0x0191B4F8..500` | vector | space-combat exit/victory |
| `WATR` | `0x0191B4F4` | singleton | water/seashore ambience |

The behavioral labels are **Strongly inferred** from tags, filenames, and state transitions. Storage type and association are **Confirmed**. WATR has an important reachability caveat: the loader assigns the singleton and remove routine `0x006C8FC0` releases it, but there is no other direct reference to `0x0191B4F4`. No runtime playback of the GMUS WATR value is present in this executable's GameMusicManager. It is retained legacy/dead configuration here, not an active water-ambience route.

## Runtime update

`0x006C9450` is the principal game-music update. It resolves scene/player/controller state, maintains multiple held AudioIds, detects changes in selected path, stops/fades the previous sound, and starts the replacement through common path-based audio entry `0x0040D990`.

It maintains at least four separate audio holders:

- `0x0191B770`: principal state-driven track and temporary local override;
- `0x0191B980`: nonzero-selector `PlayMusicMessage` server-event channel;
- `0x0191BDA0`: environment ambient-sound layer 1;
- `0x0191BB90`: environment ambient-sound layer 2.

The two ambient layers and their independence are **Confirmed** by matching the runtime slot order to the deployed environment datatable schema. Their exact original C++ member names remain unknown.

Outdoor/interior selection is controlled by `0x00B22BE0`. It walks the object's containment-parent chain, finds the cell-property owner when present, falls back to global world cell `0x01946874`, and returns true when the result is that world cell. It is therefore **Confirmed behaviorally** as `object_is_in_world_cell`. Outdoors, both day/night ambient layers and first/sunrise/sunset music come from the current planet environment-family row. In a non-world/interior cell, a smaller cell/region audio object can override ambient layer 1 and first music; ambient layer 2 is not submitted from that smaller override object.

### Planet environment datatable

`datatables/environment/<planet>.iff` is `DTII/0001`. The deployed Tatooine table has 25 typed columns and directly names the seven audio columns copied into terrain/environment runtime slots:

| Datatable column | Source descriptor offset | Runtime getter/slot |
|---|---:|---:|
| Day Ambient Sound 1 | `+0x44` | `0x00850F40` / `+0x4148` |
| Day Ambient Sound 2 | `+0x48` | `0x00850F50` / `+0x414C` |
| Night Ambient Sound 1 | `+0x4C` | `0x00850F60` / `+0x4150` |
| Night Ambient Sound 2 | `+0x50` | `0x00850F70` / `+0x4154` |
| First Music | `+0x54` | `0x00850F80` / `+0x4158` |
| Sunrise Music | `+0x58` | `0x00850F90` / `+0x415C` |
| Sunset Music | `+0x5C` | `0x00850FA0` / `+0x4160` |

This correspondence is **Confirmed by code and asset column names**. The environment rows also carry family name, weather index, sky/cloud/light/fog data, day/night environment maps, and wind scaling, so music and ambience selection are part of the same environment-family transition system rather than an isolated playlist.

### Temporary override

`0x006C9300` installs a sound on principal holder `0x0191B770` and sets `0x0191B558`. While that flag is set, update `0x006C9450` returns early as long as the held sound reports that it is playing. When playback ends, the update clears the flag and returns; ordinary state selection resumes on a later frame. The local `SpaceDeath` path uses this facility for `sound/music_space_player_death.snd`.

This is an explicit one-shot override state, not merely a call-site convention. A replacement should represent it as such.

### Nine-way principal state selection

The update derives a bounded state value `0..8` and dispatches through the jump table at `0x006C9CB0`:

| State | Selection behavior | Evidence/confidence |
|---:|---|---|
| 0 | steady non-combat state; select `First Music` once on first encounter with an outdoor environment-family or interior override object | Confirmed |
| 1 | randomly select an `ENTR` item | Confirmed |
| 2 | randomly select an `EXIT` item | Confirmed |
| 3 | randomly select a `SENT` item and retain its vector index at `0x0191BFB8` | Confirmed |
| 4 | select `SEXI` at the retained space-entry index, falling back to element zero if out of range | Confirmed |
| 5 | randomly select a `DETH` item | Confirmed |
| 6 | randomly select an `INCP` item | Confirmed |
| 7 | sunrise/day transition; select `Sunrise Music` through `0x00850F90` | Confirmed |
| 8 | sunset/night transition; select `Sunset Music` through `0x00850FA0` | Confirmed |

The positional coupling of `SENT` and `SEXI` is important: a chosen space battle loop and its victory/exit cue form a pair by vector index. A modern asset representation should validate equal vector lengths but preserve the original element-zero fallback for malformed or mismatched data.

The state derivation is now also bounded:

- when the scene is in space (`0x00425E20`), helper `0x006C9DE0` supplies the current space-combat boolean; false-to-true selects state 3 and true-to-false selects state 4;
- on the ground, creature state bit 1, tested at `0x00435250`, supplies the combat boolean; false-to-true selects state 1 and true-to-false selects state 2;
- visual posture byte `13` forces state 6 (incapacitation) on posture change, while posture `14` forces state 5 (death);
- while out of combat, changes in scene predicate `0x00B5CBF0` select state 7 when true and state 8 when false.

The last predicate is **Confirmed behaviorally as day/night**. The main frame owner passes its logical inverse to a `GameLight` control at `0x006E2DD0`; the same predicate chooses named Day versus Night ambient columns, and its transitions select named Sunrise versus Sunset music columns. True is the day/sunrise polarity and false is the night/sunset polarity. Only the original C++ method name is provisional.

For vector states other than the paired space-exit case, the client uses the global random generator and modulo vector length; empty vectors produce no new path. State 0 maintains separate visited-object vectors for outdoor environment families and interior overrides. It submits First Music only when the current object is newly inserted, preventing per-frame or repeated re-entry playback within the retained manager lifetime. When the selected path changes, the current holder is faded/stopped before a new AudioId is created. Global `0x015E15B4` is the IEEE-754 value `1.0`; together with the reconstructed envelope API this is **Strongly** a one-second crossfade/stop duration.

## Replacement architecture

The conceptual behavior is reusable, but the global-memory implementation is not. A clean client can model:

```text
GameMusicDefinition
  groundCombatEnter[]
  groundCombatExit[]
  death[]
  incapacitation[]
  spaceCombatEnter[]
  spaceCombatExit[]
  waterAmbience?

GameMusicManager
  state
  selectedSpacePairIndex?
  principalChannel
  serverEventChannel
  environmentChannels[]
  temporaryOverride?
```

The renderer choice has no bearing on this subsystem. It belongs above a modern audio backend and consumes original `.snd` assets through the compatibility asset layer.

## Remaining unknowns

- `Room Type` intent/application beyond its confirmed InteriorEnvironmentBlock field; no direct accessor caller exists in this build.
- Original source names for the day/night predicate and ambient AudioId holders.
- Original unit/name and configuration provenance of the `1.0` music fade scalar, although seconds are strongly indicated.
- Whether another revision used WATR; this executable loads/releases it but does not otherwise reference it.
- Original enum/type names for the nine principal states.

## Bottom line

The music system is now reconstructable at the asset-and-policy level: its global GMUS schema, per-planet environment audio columns, combat/day-night state table, random selection behavior, paired space cues, concurrent ambient/music/server channels, and one-shot override lifecycle are known. `ANALYSIS_CHECKPOINT_2026-09-12_INTERIOR_WEATHER_AUDIO.md` subsequently resolves the regional override as `InteriorEnvironmentBlockManager`, including its complete table schema and weather-driven selection chain.
