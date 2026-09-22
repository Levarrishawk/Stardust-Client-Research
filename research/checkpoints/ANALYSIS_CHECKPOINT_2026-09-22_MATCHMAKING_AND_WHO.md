# Matchmaking and `/who` Checkpoint (2026-09-22)

## Scope

This block closes the current Stardust character-search result path and separates it from the largely dormant personal-matchmaking preference path. Evidence combines `Stardust.exe` codecs/controller dispatch with the current Stardust server command and packet sources.

## Confirmed executable path

`SetupSwgSharedNetworkMessages` registers inner object-controller message `0x1E7` with encoder `0x011962B0` and decoder `0x01196340`. The decoded payload is a `MessageQueueGenericValueType<vector<MatchMakingCharacterResult>>`.

The player-creature controller dispatcher at `0x0062AA10`, case `0x1E7`, copies the decoded vector and emits the typed `PlayerCreatureController::Messages::CharacterMatchRetrieved` transceiver. Confirmed consumers include `Search::SwgCuiCommunity` and `SwgCuiChatWindow`.

The individual record decoder is `0x012566D0`; the inverse encoder is `0x012569F0`. A record occupies `0x50` bytes in the 32-bit client. Wire fields are:

1. U32 count followed by that many U32 matchmaking/player bitmask words.
2. U32 species.
3. Unicode string: displayed character name.
4. ASCII string: region name.
5. ASCII string: zone/planet name.
6. ASCII string: guild name.
7. ASCII string: profession title.

Strings use the shared archive variable-length convention: U16 byte length when below `0xFFFF`, otherwise U16 `0xFFFF` followed by U32 byte length. The executable decoder accepts a variable bitmask-word count. Current Stardust writes exactly four words.

## Current server path

`requestCharacterMatch` parses nine whitespace tokens: five initial integers (`unk1`, player flags, and three further currently ignored integers), faction, species, profession/title string, and wildcard string. It searches nearby creatures (or the zone range fallback), resolves riders for mounts/vehicles, and rejects missing ghosts, god-mode players, and anonymous non-self players.

For non-self results it applies wildcard name/guild matching; role-player, newbie-helper and looking-for-group flags; faction; species; and profession/title/novice-skill filters. The result is capped at 32 entries and returned as object-controller inner type `0x1E7`, flags `0x0B`.

`setMatchMakingCharacterId` and `setMatchMakingPersonalId` are registered but their current command implementations only perform state/locomotion checks and return success. They do not persist or replicate submitted preference changes. In contrast, `PLAY:3` still exposes two replicated `MatchMakingId` fields. Those vectors must remain decodable, but current-server behavior does not justify inventing a complete matching service around them.

## Replacement-client consequences

- Implement `0x1E7` as a bounded vector decoder and publish an immutable character-search result collection to presentation code.
- Preserve wire widths; the internal x64 representation must never serialize native pointers or `size_t`.
- Accept variable bitmask counts, retain unknown extra words, and cap allocations defensively.
- Preserve the nine-token request grammar. Label weakly understood slots as legacy fields instead of inventing semantics.
- Treat the 0x50-byte result object as a 32-bit implementation artifact, not a portable structure layout.
- The UI can be redesigned; its compatibility boundary is the request command, `0x1E7` result, and replicated PLAY fields.

## Confidence

- `0x1E7` registration, vector type, decoder, dispatch event and UI subscribers: **Confirmed by executable code**.
- Current server field order, filtering, count limit and controller envelope: **Confirmed by server source**.
- Four-word current bitmask convention: **Confirmed**.
- Meanings of four otherwise unused request integers: **Unknown but positions preserved**.
- Historical intended behavior of the two set-matchmaking commands: **Unknown/dormant in current Stardust**.
