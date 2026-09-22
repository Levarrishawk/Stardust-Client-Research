# Analysis Checkpoint: Game Language Manager

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope completed

This block recovers the client language catalog, language-ID rule, speaking/comprehension skill links, audible policy and deterministic incomprehensible-text transformation used by spatial chat.

## Original asset contract

`game_language_manager_install_VA_011C4810` opens `datatables/game_language/game_language.iff`. The effective copy comes from `mtg_patch_013_configurable_02.tre` at priority 42, is 1,663 bytes, DataTable-IFF version `0001`, and contains 11 rows and 30 columns:

```text
speakSkillModName, comprehendSkillModName, stringId,
a, b, c, ... z,
audible
```

The row index is the wire/runtime language ID:

| ID | `stringId` | audible |
|---:|---|---|
| 0 | `basic` | true |
| 1 | `rodese` | true |
| 2 | `dosh` | true |
| 3 | `moncalamarian` | true |
| 4 | `shyriiwook` | true |
| 5 | `bothese` | true |
| 6 | `ryl` | true |
| 7 | `zabrak` | true |
| 8 | `lekku` | false |
| 9 | `ithorian` | true |
| 10 | `sullustan` | true |

Every row also names its `language_<name>_speak` and `language_<name>_comprehend` skill modifiers. Lekku has empty A-Z replacements and is explicitly non-audible.

## Recovered APIs and behavior

- `game_language_name_to_id_VA_011C64B0` maps the canonical `stringId` to row ID.
- `game_language_id_is_audible_VA_011C64E0` returns the table's audible flag for positive IDs.
- `game_language_get_speak_skill_mod_VA_011C6270` and `game_language_get_comprehend_skill_mod_VA_011C6390` return the two skill-mod names.
- `game_language_transform_text_VA_011C6530` performs deterministic letter substitution for a positive language ID. It lowercases only for the A-Z lookup, substitutes each alphabetic character using that row's A-Z string, restores uppercase on uppercase input, and passes punctuation/nonletters through. Language 0 copies input unchanged. There is no random generator in this path.

The transform may expand one input letter to multiple output characters (`rodese` A→`ee`, for example). A modern implementation must size output dynamically and must not assume a one-byte substitution.

## Protocol/state connection

- Spatial-chat controller `0xF4` carries a U8 language ID in all current server constructors. The established `spatial_chat_apply_VA_0091BA10` route can therefore select this table before formatting/publishing chat.
- The player's selected outgoing language is replicated in `PLAY:9` field index 9. Stardust's current delta helper calls `insertInt((uint32)lid)`, so the replicated field is U32 even though the server property and spatial-chat field are byte-sized.
- Speaking/comprehension ability is derived from creature skill modifiers, not from a bitfield in the spatial-chat packet. The server distributes the language ID and original text; recipient-side presentation can use local replicated skills and this table.

## Replacement contract

- Preserve U8 language IDs in spatial-chat payloads and the existing U32 `PLAY:9[9]` replication encoding.
- Treat row order in the original asset as protocol identity; never alphabetically regenerate IDs.
- Apply deterministic variable-length A-Z substitution exactly when the recipient cannot comprehend an audible language.
- Preserve non-audible Lekku as a distinct presentation mode rather than treating its empty substitution columns as corrupt data.
- Represent text internally as Unicode, but reproduce the legacy ASCII-letter substitution over the compatible source text before UI rendering.

## Confidence

- Table schema, row identities/order, skill-mod names, audible flags, ID lookups and substitution algorithm: **Confirmed by code/assets**.
- Exact high-level caller condition combining sender speak and recipient comprehend modifiers: **Strongly inferred** from table APIs, replicated skill mods and spatial-chat route; the final conditional call site remains to be named.
- Presentation chosen for non-audible Lekku in every UI context: **Unknown; retain the flag and defer policy to the chat presentation layer**.

