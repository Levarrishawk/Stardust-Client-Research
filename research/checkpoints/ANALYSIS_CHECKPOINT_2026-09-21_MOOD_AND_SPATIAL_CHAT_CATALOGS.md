# Mood and Spatial-Chat Catalogs

## Scope

This block closes the static catalog layer immediately below controller `0xF4` spatial chat. It correlates the effective Stardust TRE assets, the server loaders and the original client managers. It does not reinterpret the already-mapped `SpatialChat` message body.

## Findings

### Spatial-chat type identity is positional

The effective `chat/spatial_chat_types.iff` comes from `mtg_patch_011_files_01.tre` (configured priority 40), is 876 bytes, and has the exact grammar:

```text
FORM SPCT
  FORM 0000
    CHUNK TYPS: NUL-terminated names in protocol-ID order
    FORM VOLS
      CHUNK VOLT: name string, little-endian U16 distance
    FORM PRIV
      CHUNK PRVT: private type name
    FORM SKCS
      CHUNK SKCT: type name, required-skill name
```

`TYPS` contains 102 entries. Both client `spatial_chat_manager_load_VA_011C8100` and server `ChatManagerImplementation::loadSpatialChatTypes()` increment an integer while reading the strings, making `ID = row + 1`. Important fixed values include `say=1`, `emote=38`, `sing=82`, `social=84`, `whisper=99`, `yell=101`, and `yelp=102`. The server chooses `say` as its default type.

The effective volume policy is a default distance of 50, `yell=100`, `shout=100`, `whisper=25`, and `emote=25`. `whisper` is the only `PRVT` entry. `sing` requires the `musician` skill. The client validates every named VOLS/PRIV/SKCS reference against the TYPS map and strict-fatals on missing mandatory forms, unknown names or duplicate types.

### Mood identity is independently positional

The effective `chat/mood_types.iff` comes from `mtg_patch_017.tre` (priority 46), is 1,636 bytes, and contains 188 NUL-terminated names under `MOOD/0000/TYPS`. Client `mood_manager_load_VA_011CEE70` assigns sequential IDs beginning at 1 and creates both name-to-ID and ID-to-name maps; `mood_name_to_id_VA_011CEB70` returns zero when absent. Server `loadMoodTypes()` uses the same ordering, then adds server-only names `meditating=189` and `entertained=190`.

The effective English `string/en/mood_types.stf` comes from `mtg_patch_013_configurable_02.tre` (priority 42), is 34,793 bytes, and contains 188 named/value records. Client `mood_manager_client_install_VA_00500410` iterates that localized table, ignores a reserved entry, validates every other key through the core mood map, and constructs presentation records indexed by the positional mood ID. Each value is newline-delimited: display name, animation token (`~` means no animation), followed by one or more prose/adverb variants. Server `loadMoodTypes()` reads the first two lines to make its mood-to-animation map and adds direct animations for the two server-only moods.

### Runtime connection to spatial chat

`spatial_chat_apply_VA_0091BA10` consumes the already-documented controller object. Its observed fields include spatial type at `+0x20` (U16), mood at `+0x24` (U16), flags at `+0x26` (U8), and language at `+0x27` (U8). A nonzero mood is expanded through `mood_client_append_presentation_VA_00500F50`. The function distinguishes the special `emote` spatial type, applies private-chat validation, language transformation/filtering, chat formatting and optional publication/animation.

The wire values therefore cannot be converted to hashes or regenerated alphabetically. A replacement must mount the original effective assets and preserve their row order, or use a generated catalog whose numeric values are demonstrably identical.

## Server/client cross-reference

| Boundary | Server producer/loader | Candidate client function | Result | Confidence |
|---|---|---|---|---|
| spatial type U16 | `ChatManagerImplementation::loadSpatialChatTypes`; `SpatialChat` constructors | `spatial_chat_manager_load_VA_011C8100`; `spatial_type_name_to_id_VA_011C7E80`; `spatial_chat_apply_VA_0091BA10` | positional type lookup, range/privacy/skill policy, formatted chat | Confirmed |
| mood type U16 | `ChatManagerImplementation::loadMoodTypes`; `SpatialChat` constructors | `mood_manager_load_VA_011CEE70`; `mood_name_to_id_VA_011CEB70`; `mood_client_append_presentation_VA_00500F50` | mood prose and optional creature animation | Confirmed |
| language U8 | `SpatialChat` constructors | `game_language_transform_text_VA_011C6530`; `spatial_chat_apply_VA_0091BA10` | comprehension/garbling and UI presentation | Confirmed |

## Replacement requirements

- Parse the original IFF catalogs through the normal TRE/VFS overlay.
- Preserve 1-based TYPS row identity exactly and reject values outside the loaded catalog.
- Treat VOLS, PRIV and SKCS as policy metadata rather than renderer/UI data.
- Parse mood STF values as structured newline-separated presentation data and retain `~` as the no-animation sentinel.
- Accommodate Stardust's two appended mood IDs explicitly; they are not present in the client's base IFF.
- Keep wire fields fixed-width (`U16`, `U8`) regardless of the replacement process's pointer width.

## Remaining bounded unknowns

- The individual semantics of every high flag bit in spatial chat remain only partly labeled; the branch topology is already mapped.
- Whether every historical client release appended the same two server moods is not established. For this Stardust deployment, server behavior is confirmed.
- Exact original C++ member names and container types are unavailable and are not compatibility requirements.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`: client install/load/lookup functions at `0x00500410`, `0x00500F50`, `0x0091BA10`, `0x011C7A50`, `0x011C7E80`, `0x011C8100`, `0x011CE960`, `0x011CEB70`, and `0x011CEE70`.
- Effective archive assets extracted from `C:\Stardust-DEV` using `swgemu_live.cfg` precedence.
- `MMOCoreORB/src/server/chat/ChatManagerImplementation.cpp` spatial and mood loaders.
- Server `SpatialChat` message constructors previously correlated in the packet-handler map.
- Reproducible decoders: `parse_chat_catalogs.py` and `parse_stf.py`.

