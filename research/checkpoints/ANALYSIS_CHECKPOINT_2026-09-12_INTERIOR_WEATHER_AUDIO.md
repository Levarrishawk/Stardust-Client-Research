# Interior Environment and Server Weather Audio Boundary

Date: 2026-09-12  
Executable: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This checkpoint closes the two audio/environment questions left by the GameMusicManager block: the identity and data contract of the interior-cell override, and the route from `ServerWeatherMessage` to environment-family and ambient-sound changes.

## InteriorEnvironmentBlockManager identity

The executable contains the original diagnostic names `InteriorEnvironmentBlockManager::remove` at `0x0188FF1C` and `datatables/interior/interior.iff` at `0x0188FF44`. Install routine `0x00791E30` creates its default block, map and manager state, then loader `0x007922A0` reads that table. This class identity and source asset are **Confirmed**.

The effective overlay winner is `mtg_patch_022.tre` at configured priority 51. It is `DTII/0001`, has 13 columns, and supplies the exact compact record consumed by GameMusicManager.

## Exact interior record contract

Loader `0x007922A0` reads the following columns in order and builds a 52-byte record. Accessors are single-field getters, making the mapping direct rather than speculative.

| Index | Datatable column | Record offset | Accessor | Runtime use |
|---:|---|---:|---:|---|
| 0 | `PobName` | key input | — | portal-object/building key component |
| 1 | `Cell Name` | key input | — | cell key component |
| 2 | `Day sound/ambient Sound (2d .snd)` | `+0x04` | `0x00791D30` | interior day ambient layer 1 |
| 3 | `Night sound/ambient Sound (2d .snd)` | `+0x08` | `0x00791D40` | interior night ambient layer 1 |
| 4 | `First Music (2d .snd)` | `+0x0C` | `0x00791D50` | once-per-encounter interior music |
| 5 | `Surface Type` | `+0x10` | `0x00791D60` | surface/material result for contained objects |
| 6 | `Fog Enabled` | `+0x14` | `0x00791D70` | interior fog enable |
| 7 | `Fog Density` | `+0x18` | `0x00791D80` | interior fog density |
| 8–10 | `Fog Color R/G/B` | `+0x20..+0x28` | `0x00791D90` returns color at `+0x1C` | interior fog color; alpha/default component is initialized to 1.0 |
| 11 | `Environment Map (dds)` | `+0x2C` | `0x00791DA0` | cell environment map |
| 12 | `Room Type` | `+0x30` | `0x00791DB0` | declared room/acoustic classification; no direct caller found in this executable |

The record also retains its combined lookup key at `+0x00`. String-valued sound and environment-map fields are interned/owned through the client's shared string objects. The surface type consumer at `0x006231B0` uses this same lookup when an object is in an interior cell. Fog and environment-map consumers are in GroundScene processing around `0x006E67C0`.

The deployed `Room Type` enumeration contains 26 values (`alley` through `underWater`, with `generic` as its default). Its intended acoustic/reverb role is **Strongly inferred from the field name and domain**, but runtime application is **Unknown** because accessor `0x00791DB0` has no direct call site in this build.

## Lookup and fallback policy

Lookup `0x007920F0` receives portal-object and cell names. It searches in this exact order:

1. `%s_%s` — exact POB and cell;
2. `%s_default` — POB-specific default;
3. `default_default` — global default.

Install helper `0x00792280` explicitly primes the manager by looking up `default_default`. The effective table's global row supplies `sound/default_interior.snd` for both day and night. This fallback chain is **Confirmed by code and asset bytes**.

At `0x00848551`, the terrain/environment update resolves the current cell owner, obtains its portal-object identity and cell name, calls `0x007920F0`, and stores the new block at terrain-environment member `+0xB0` while preserving the prior block at `+0xB4`. GameMusicManager receives this current block through `ClientProceduralTerrainAppearance` accessor `0x0084B8E0` → environment accessor `0x00846CD0`.

## ServerWeatherMessage route

Current Stardust server constructor `ServerWeatherMessage.h` emits:

```text
U16 opcount = 3
U32 opcode = 0x486356EA
S32 weather
F32 windX
F32 windMagnitude
F32 windY
```

The last three scalars are parsed by the client as one three-float vector. The server-side variable names are preserved above; the wire does not independently label their coordinate semantics.

| Stage | Address | Confirmed behavior |
|---|---:|---|
| Ground-scene dispatcher branch | `0x0051C4E9` | recognizes the registered ServerWeatherMessage type |
| Parser constructor | `0x00AD3F90` | parses a 32-bit scalar and three-float vector |
| Weather accessor | `0x00AD4100` | returns parsed scalar at object `+0x3C` |
| Wind accessor | `0x00AD4110` | returns vector at object `+0x44` |
| Wind setter | `0x00844AA0` | stores and normalizes the vector, then notifies registered consumers through `0x00844B10` |
| Ground-environment weather setter | `0x00845C90` | stores the weather index at `0x01924B6C` |

The end-to-end message layout, parser, handler and immediate state destinations are **Confirmed by executable code and current server source**.

## How weather affects ambience

Ground-environment update `0x008479B0` reads the server weather index at `0x008485CB` and calls environment-family selector `0x0085D7F0` with the player's terrain position and that index. When the selected environment descriptor changes, the runtime retains old and new descriptors and begins the existing environment transition path.

GameMusicManager does not need a separate weather-message listener. Its per-frame update `0x006C9450` polls the current environment descriptor:

- outdoors, it selects named day/night ambient sound 1 and 2 from the current planet environment-family row;
- indoors, it selects day/night sound 1 from the resolved InteriorEnvironmentBlock and deliberately submits no layer 2;
- if either selected path changes, the corresponding held AudioId is faded/stopped with the shared `1.0` scalar and the replacement is started through `0x0040D990`;
- unchanged paths cause no restart.

Therefore the actual chain is:

```text
ServerWeatherMessage
  -> weather index + normalized wind vector
  -> terrain environment-family selection by position/weather
  -> current environment descriptor changes
  -> GameMusicManager observes new day/night sound paths
  -> independent ambient channels cross-fade as necessary
```

This is **Confirmed** through the message, setter, environment selector and music-manager code. Exact visual environment blend duration remains outside this audio checkpoint. Ambient audio uses the music manager's path-change behavior rather than directly sharing the visual interpolation scalar.

## Replacement requirements

- Preserve the `ServerWeatherMessage` wire widths and field order exactly; internal x64 types must not alter them.
- Implement the three-level interior lookup chain, including `default_default`.
- Preserve interior suppression of outdoor ambient layer 2.
- Treat weather as an input to terrain environment-family selection, not as a direct sound opcode.
- Keep wind-vector propagation independent from weather-index/environment selection.
- Model current and previous environment descriptors explicitly for visual transitions, while making audio changes path-driven and restart-free when paths are equal.
- Parse all 13 interior columns even if room acoustics are initially deferred.

## Remaining unknowns

- Whether `Room Type` drove an omitted/disabled acoustic backend or is consumed only indirectly in another build.
- Original source names for the wind manager and several terrain-environment accessors.
- Exact meaning/range of the server's middle `windMagnitude` component relative to the normalized client vector and separately configured wind magnitude scalar.
- Complete environment-family selection algorithm inside `0x0085D7F0`; its weather/position inputs and returned descriptor role are confirmed.

## Bottom line

Regional and weather audio precedence is now implementation-grade. The client uses a named, table-driven `InteriorEnvironmentBlockManager` with exact POB/cell fallback, while server weather changes the terrain environment selection that GameMusicManager subsequently observes. There is no separate weather-sound packet or GMUS-WATR playback route.
