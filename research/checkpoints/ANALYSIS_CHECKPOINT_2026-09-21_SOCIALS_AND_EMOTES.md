# Analysis Checkpoint: Socials and Emotes

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope completed

This block closes the normal social/emote loop: canonical social IDs, outgoing command text, server fan-out, incoming controller payload, localized prose, optional animation and chat publication.

## Shared social catalog

`SocialsManager::install` opens `datatables/chat/social_types.iff`. The effective installed copy comes from `mtg_patch_013_configurable_02.tre` at search-tree priority 42, is 2,981 bytes, DataTable-IFF version `0001`, and contains 382 rows with two string columns:

```text
canonical_name
wtf
```

The current server's `ChatManager::loadSocialTypes` uses row index + 1 as the numeric social ID and column zero as the canonical name. Numeric IDs are consequently content-order dependent; the replacement must load the exact effective table rather than hard-code an independently sorted name list.

`cui_socials_manager_install_VA_009704B0` obtains the localized `social` string table and validates each social entry. A recognized social type must map to a string-table entry with at least 44 indexed lines. The table supplies viewpoint/target/text and animation variants; it is distinct from the two-column ID catalog. The `whisper` social is cached specially.

## Outgoing path

`cui_social_send_VA_00970F80` accepts source, target, numeric social ID, animate flag and text flag. Outside local single-player/debug mode it requires the source to be the local player, formats the Unicode command arguments exactly as:

```text
<targetNetworkId> <socialId> <animateAs0Or1> <textAs0Or1>
```

and submits them through the command queue using the CRC of `socialInternal`.

Current Stardust registers `SocialInternalCommand`. It parses U64 target ID plus the three U32 values, enforces state/locomotion/mute rules, range and ignore filtering, and sends an `Emote` controller message to eligible nearby players. It additionally hooks applause/cheer/clap into entertainment sessions and routes creature/pet reactions server-side.

## Incoming path

The server `Emote` constructor emits ObjectController inner `0x12E` with flags `0x0B` and body:

```text
U64 sourceId
U64 emoteTargetId
U32 socialId
U8  flags          // bit 0 animate, bit 1 text
```

`cui_social_process_message_VA_009706E0` receives the corresponding `MessageQueueSocial` projection, resolves source and optional target client objects, validates the social ID through `social_types.iff`, and then:

- when text is enabled, chooses the appropriate localized social line for self/no-target/target viewpoint and publishes the Unicode result through `CuiSocialsManager::Messages::SocialReceived` to `SwgCuiChatWindow`;
- when animation is enabled, parses the selected animation line and asks the source creature controller/animation manager to play it;
- reports invalid IDs, absent tables, non-client-object sources and malformed animation lines without inventing semantics.

The local/debug path constructs the same queue object and calls the normal receive routine, which is useful evidence that the server and presentation paths share one payload contract.

## Replacement contract

- Preserve controller `0x12E` and its exact U64/U64/U32/U8 body.
- Preserve `socialInternal` argument ordering and numeric boolean formatting.
- Mount the original `social_types.iff` and `social.stf` through the effective VFS overlay; IDs cannot safely be regenerated from names.
- Resolve object IDs through stable handles and treat a missing target as a valid no-target case.
- Keep server range, ignore, mute, AI reaction and entertainment effects authoritative.
- Publish a typed presentation result containing localized text and optional animation request. The legacy 44-line layout should be decoded at the asset boundary, not propagated through the new UI.

## Confidence

- Catalog shape/count/order rule, outgoing command grammar, server parsing/fan-out, incoming wire body, flag bits and text/animation branches: **Confirmed by code/assets/server source**.
- Exact semantic label for every one of the 44 localized lines and the historical meaning of column `wtf`: **Unknown; preserve by indexed compatibility decoding until exhaustively mapped**.

