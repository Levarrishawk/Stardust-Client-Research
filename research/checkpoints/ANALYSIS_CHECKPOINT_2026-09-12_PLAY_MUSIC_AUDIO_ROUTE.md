# PlayMusicMessage to Audio Runtime

Date: 2026-09-12  
Executable: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This checkpoint follows the server-emitted `PlayMusicMessage` from the `GroundScene` message listener through message construction, parsed fields, channel selection, replacement policy, asset validation, and ordinary sound submission. It also tests whether this route explains the previously observed sound-template category-13 boundary anomaly.

Confidence labels retain their project-wide meanings: **Confirmed by code**, **Strongly inferred**, **Tentatively inferred**, and **Unknown**.

## Server wire contract

`MMOCoreORB/src/server/zone/packets/player/PlayMusicMessage.h` emits:

1. `U16` operand count `5`
2. `U32` message CRC `0x04270D8A`
3. ASCII16 sound-template path
4. `U64` target object ID
5. `S32`/`U32` channel selector (`unkInt`, default `1`)
6. `U8` trailing flag (`unkBool`, default `false`)

This order is **Confirmed by server code**. The client parser object registers four payload variables at object offsets `+0x38`, `+0x48`, `+0x58`, and `+0x60`, then invokes the generic message reader at `0x0118B740`. The value storage observed by the listener is consistent with path data at `+0x3C` and the 32-bit selector value at `+0x5C`. This client/server correspondence is **Confirmed**.

## Client dispatch and field use

The main `GroundScene` listener compares the incoming message name against the literal `PlayMusicMessage` at `0x01875A14`. Its branch begins at `0x0051CB49`:

```text
GroundScene message listener
  -> named-message gate 0x0051CB49
  -> construct/read PlayMusicMessage at 0x00AD9F40
  -> inspect parsed selector at message +0x5C
       selector == 0 -> 0x006C9300(path)
       selector != 0 -> 0x006C93B0(path)
  -> destroy parsed message at 0x00ADA0C0
```

The path passed to both handlers is the parsed ASCII value at message `+0x3C`. Neither the parsed target ID nor the trailing byte is read in this listener branch. This is **Confirmed by code**. The original semantic names of those two fields remain **Unknown**; their server names are placeholders and should not be promoted into the replacement protocol model.

## Two playback channels

Both client handlers perform the same policy against different global channel holders:

| Selector | Handler | Channel holder | Additional observed effect |
|---:|---:|---:|---|
| `0` | `0x006C9300` | `0x0191B770` | sets byte `0x0191B558` to `1` after the request |
| nonzero | `0x006C93B0` | `0x0191B980` | no corresponding byte write observed |

Each handler:

1. calls `0x006CA150(path)` to fetch/validate the sound template and reject an unsuitable/already-active case;
2. checks whether its channel holder currently contains a live audio ID;
3. if so, calls `0x0040F360(audioId, 1.0)`, which forwards to the common sound stop/fade routine `0x0040C630`;
4. submits the new path through `0x0040D990(path, null object, false)`;
5. stores the returned audio ID in its channel holder.

Thus `PlayMusicMessage` is not a bare fire-and-forget sound command. It is a two-channel replacement/fade facility. The `1.0` value is definitely supplied as the stop/fade scalar; its exact time unit/name is **Strongly inferred** to be a one-second fade from the sound-envelope implementation, not yet proven from symbols.

The first channel's extra flag is consumed by the music-manager update routine `0x006C9450`. While set, the update checks whether channel `0x0191B770` still contains a playing sound; if so, it returns early. Once that sound is no longer playing it clears the flag and returns, allowing normal state-driven music selection on following updates. The same channel is used locally by the `SpaceDeath` constructor at `0x005866A0`, which plays `sound/music_space_player_death.snd`. This establishes channel zero as a **temporary game-music override channel** with automatic return to state-driven music (**Confirmed behavior**, provisional name). The nonzero channel has no equivalent update/return logic and is called only by the network listener among direct callers found. It is therefore a separate server-event channel, although its original name remains **Unknown**.

All eight C++ construction sites in the current Stardust server use the constructor default selector `1`; no server source call supplies an alternate selector. Thus the present server always selects the nonzero client channel. Selector zero remains protocol-compatible and is used by the client itself for the local space-death sequence.

## Ordinary audio path, not a special voice bypass

`0x0040D990` is a thin wrapper around common submission `0x0040D9F0`, which reaches the central sound-template path at `0x0040BFA0`. That routine resolves the `.snd` through the normal template registry (`0x00417320`) and constructs the normal 2D/3D sound runtime selected by template properties. There is no `PlayMusicMessage`-specific mixer or category override between the listener and ordinary runtime construction.

This means the message route does **not** resolve the category-13 anomaly by bypassing normal audio. If a category-13 descriptor is supplied in `PlayMusicMessage`, it can reach the same runtime whose effective-volume adapter indexes the category gain using template `+0x44`.

## Category-13 reachability assessment

The gain accessor at `0x0040C780` performs a direct unbounded read from:

```text
float categoryGain = *(float *)(0x01907F34 + category * 4)
```

The installation loop at `0x0040CBA0` initializes only indices `0..12` (`esi < 13`). Category `13` therefore reads at `0x01907F68`, immediately beyond the initialized table. No category-13 special comparison was identified on the `PlayMusicMessage` route.

The server currently sends ordinary music, ambience, tutorial narration, and effects through this packet. Cross-checking representative live descriptors shows that this does not imply category 13:

| Server-used descriptor | Parsed category `+0x44` |
|---|---:|
| `music/mus_vader_terror.snd` | 8 |
| `sound/music_mission_complete.snd` | 8 |
| `sound/tut_01_welcome.snd` | 4 |
| `sound/amb_alarm_air_raid_lp.snd` | 2 |
| `sound/amb_alarm_air_raid_short.snd` | 10 |

The 558 category-13 descriptors are a distinct dialogue corpus dominated by `c3po*`, `han*`, `darth*`, and `vo*` names. No direct references to representative numbered `c3po`, `han`, or `darth` descriptors were found in the unpacked Stardust server source.

A full configured-overlay scan examined 203,617 winning TRE records and extracted searchable non-audio payloads. It found exactly two category-13 path references:

- `quest/npe_new_main_quest.qst` line-oriented XML field `musicOnActivate="sound/han_94.snd"`;
- the same `.qst` source field with `sound/han_95.snd` on a later task.

This is real content evidence, but not yet proof of runtime reachability. `Stardust.exe` contains no `.qst` format string and constructs runtime quest-task paths as `datatables/questtask/<name>.iff`; 791 such compiled quest-task tables exist, but none under `datatables/questtask/npe...`, and the payload scan found neither Han path in compiled content. The `.qst` is therefore strongly indicated to be an editor/source artifact shipped in a patch archive rather than a runtime input for this executable. Category 13 remains **not demonstrated reachable in the installed runtime**, rather than proven unreachable: a server can still send any descriptor path dynamically and non-string/CRC references have not been excluded.

The conservative replacement policy remains:

- parse and preserve all on-disk category values;
- never reproduce the original out-of-bounds read;
- model category 13 explicitly as an unresolved legacy-dialogue category with a safe default gain;
- collect runtime/content evidence before assigning it a user-facing mixer slider or aliasing it to another category.

## Stable provisional names

| VA | Provisional name |
|---:|---|
| `0x00AD9F40` | `play_music_message_parse_ctor` |
| `0x0051CB49` | `ground_scene_dispatch_play_music_message` |
| `0x006C9300` | `play_music_on_channel_zero` |
| `0x006C93B0` | `play_music_on_channel_nonzero` |
| `0x006CA150` | `validate_play_music_sound_template` |
| `0x0040D990` | `audio_play_path_unattached` |
| `0x0040F360` | `audio_stop_or_fade` |

Names describe observed behavior, not recovered source symbols.

## Replacement-client contract

An implementation-compatible handler can be designed now:

```text
parse ASCII16 path, U64 target, S32 selector, U8 trailingFlag
choose channel 0 when selector == 0, otherwise channel 1
fade/stop the currently held AudioId on that channel with scalar 1.0
resolve and play the supplied .snd through the normal asset/audio pipeline
store the new AudioId in the selected channel
retain target and trailingFlag in diagnostics until their semantics are established
```

Security/robustness additions are appropriate in the clean client: validate the path as a canonical client-asset path, reject traversal or host paths, bound the ASCII length, and log unknown selector values without changing the zero/nonzero compatibility rule.

## Remaining unknowns

- Original semantic names for the two channels and the first channel's extra global flag.
- Whether `1.0` is seconds or another envelope unit, although a fade duration is strongly indicated.
- Intended meaning of the parsed target ID and trailing flag; neither affects this executable's listener branch.
- Whether any active non-string/CRC content reference or server-generated path reaches the category-13 dialogue corpus. The only direct packed references found are two `musicOnActivate` fields in a `.qst` source artifact without a corresponding runtime NPE quest-task table.
- Intended category-13 mixer behavior in other client revisions.

## Bottom line

The complete active `PlayMusicMessage` path is now implementation-grade: wire order, parser, selector branch, two independent playback holders, prior-sound fade/replacement, and ordinary `.snd` submission are all known. The work also rules out a special-message bypass as the explanation for category 13. That anomaly is now bounded to legacy content reachability and intended mixer policy rather than packet or handler uncertainty.
