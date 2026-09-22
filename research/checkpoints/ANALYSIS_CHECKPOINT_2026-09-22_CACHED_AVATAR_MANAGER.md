# Cached Avatar Manager

## Scope and conclusion

This block reconstructs the durable boundary of `CuiCachedAvatarManager`. The subsystem is a local cache used by login/avatar-selection presentation. It is not an authoritative network object store and it is not part of the live baseline/delta replication protocol.

Confidence labels follow the project convention: **Confirmed**, **Strongly inferred**, **Tentative**, and **Unknown**.

## Event lifecycle

| Stage | Candidate function | Behavior | Confidence |
|---|---:|---|---|
| callback installation | `cached_avatar_callbacks_install_VA_0093F1C0` | connects owned transceivers for `CreatureObject::Messages::PlayerSetup` and `ClientObject::Messages::AddedToContainer` | Confirmed |
| player setup callback | callback body `0x0093F1F0` | sets global dirty byte `0x0192F51C` | Confirmed |
| containment callback | `cached_avatar_added_to_container_callback_VA_0093F200` | compares the added object's container/owner with the current player object and sets the same dirty byte on a match | Strongly inferred; comparison and dirty write confirmed |
| deferred update | `cached_avatar_update_if_dirty_VA_00940CE0` | when dirty, calls the current-player cache refresh/save path, then clears the flag | Confirmed |
| callback removal | `cached_avatar_callbacks_remove_VA_0093F220` | disconnects both owned transceivers from the manager callback object | Confirmed |

This design prevents every equipment/container event from immediately rewriting disk. A replacement can preserve the behavior with an explicit dirty flag and a safe entity identifier; it must not retain the original raw-pointer identity comparisons.

## Versioned cached-avatar file

`cached_avatar_load_file_VA_0093F5A0` validates an IFF and requires outer form tag `CADF` (`0x43414446`) with version form `0000`. Other versions produce the embedded `unsupported Cached Avatar Data version` diagnostic.

`cached_avatar_write_iff_VA_00940020` writes:

```text
FORM CADF
  FORM 0000
    CHUNK DATA
      avatar object template name
      serialized avatar customization/state blob
    CHUNK PLAN
      planet/location string
    FORM WEAR
      CHUNK ITEM (repeated)
        wearable object template name
        serialized wearable customization/state blob
        32-bit arrangement/containment value
```

The corresponding reader is `cached_avatar_read_iff_VA_0093FCD0`. It verifies that the cached avatar template name matches the newly created avatar object, creates each cached wearable through `object_instance_create_by_template_name_VA_00B2E760`, requires it to cast to `TangibleObject`, restores its serialized state, and transfers it to the cached avatar through the normal containment path. The reader emits explicit diagnostics for template mismatch, creation failure, non-tangible items, and failed inventory transfer.

The 32-bit value stored in each `ITEM` is file-format data and must remain exactly 32 bits in an x64 implementation. In-memory object pointers and handles are not serialized into this file.

## Cache filename construction

`cached_avatar_build_filename_VA_0093FB80` constructs a path from the current login identity plus character/cluster identifiers and a fixed cached-avatar suffix. The exact constant text is less architecturally important than the confirmed key composition: cache identity is account/session + cluster + character, not merely character name. The current-client refresh path `cached_avatar_refresh_current_player_VA_0093F920` obtains the current player, derives that key, writes the avatar cache, updates the character's planet and last-played marker, and saves the character list.

## Character-list metadata

The manager maintains a tree/list rooted at global `0x0192F4F8`. A record contains at least:

- cluster/server integer at offset `+0x10`;
- 64-bit network/character identifier represented as two 32-bit words at `+0x18/+0x1C`;
- character name/string storage beginning at `+0x20`;
- planet/location string storage at `+0x38`;
- last-played/current marker byte at `+0x44`.

`cached_avatar_set_character_planet_VA_00940860` locates a record by cluster plus 64-bit character ID and replaces the planet string. `cached_avatar_set_character_last_played_VA_00940980` sets exactly one matching record's byte at `+0x44` and clears it on all others. Both emit explicit warnings when the character is absent.

`cached_avatar_save_character_list_VA_00940A80` writes a local `characterlist` file beneath the per-user profile path using a textual record format. The decompiler confirms that cluster/server, character identity/name information, a known-server membership flag, and last-played state are included. Exact punctuation of the legacy text format remains presentation-cache compatibility detail, not a server wire requirement.

## Replacement-client requirements

1. Treat this data as rebuildable local cache. Network truth and replicated world state must never depend on it.
2. Preserve the `CADF/0000/DATA/PLAN/WEAR/ITEM` reader if compatibility with existing local cache files is desired; writing the legacy file can be optional after migration.
3. Preserve original template names and wearable containment/arrangement values because the cache reconstructs objects through the original SWG asset/template pipeline.
4. Key records with explicit `{account, cluster_id, NetworkId}` value types. Keep the wire/file `NetworkId` as two 32-bit words or one defined 64-bit integer; never serialize an x64 pointer.
5. Use stable entity handles or IDs for dirty-event correlation instead of comparing object addresses.

## Unknowns intentionally retained

- Exact extension/path suffix constants used by `cached_avatar_build_filename_VA_0093FB80` have not been assigned semantic names.
- Exact textual punctuation of the legacy `characterlist` rows has not been reconstructed field-for-field.
- The complete internal layout of the legacy character metadata node is unnecessary for architectural replacement and remains only partially labeled.

These unknowns do not block login protocol, live object replication, original asset loading, or the first x64 networking proof of concept.
