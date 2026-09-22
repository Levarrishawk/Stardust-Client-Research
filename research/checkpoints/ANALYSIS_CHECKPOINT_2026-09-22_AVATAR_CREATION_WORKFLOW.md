# Avatar Creation Workflow

## Outcome

The character-creation protocol is closed as a client state workflow and is
correlated with the current Stardust creation authority. This is distinct from
login character enumeration and the local cached-avatar preview system.

## Client state path

```text
original creation tables/templates/customization catalogs
  -> avatar setup/customize/location/profession mediators
  -> collector and sender 0x00BB4D10
  -> ClientCreateCharacter 0xB97F3074
  -> success 0x1DB575CC or failure 0xDF333C6E
  -> listener 0x00BB4770
  -> state/result applicator 0x00BB6380
```

Random-name generation is an independent correlated request:

```text
0x00BB6670 -> request 0xD6D1B6D1
  -> response 0xE85FB868 -> 0x00BB6880
```

The response is accepted only when its race/template matches the pending
request and the result identifies `ui:name_approved`; mismatches clear pending
state and produce diagnostics.

## Creation wire record

In order after opcode `0xB97F3074`:

1. ASCII customization string
2. UTF-16 character name
3. ASCII player/race template path
4. ASCII starting-location token
5. ASCII hair template path
6. ASCII hair customization string
7. ASCII profession
8. U8 reserved
9. F32 height/scale
10. UTF-16 biography
11. U8 tutorial flag

All widths remain fixed on an x64 client. Template paths and customization
blobs are data identities, not native pointers or structs.

## Current server authority

Stardust validates account/galaxy character limits and name policy, resolves
the race path through the server template manager, clamps scale to the selected
player template, creates the creature and child objects, applies appearance and
hair customization, grants profession/race/common starting state, chooses
tutorial versus skipped-tutorial placement, persists account/character state,
then returns the new U64 object ID. The starting-location string is currently
parsed but not used by this path. The server also normalizes a requested Jedi
profession to `crafting_artisan`.

This makes original assets mandatory on the replacement side for valid player
template choices, visual customization ranges/groups, hair templates,
profession presentation, species naming, and avatar preview. The server remains
authoritative for validity and gameplay grants.

## Compatibility defect found

`ClientCreateCharacterFailed` always encodes the supplied value as a key in the
`ui` string table, and its own comment requires a specific STF key. Several
current API/rate-limit failure sites pass human-readable sentences instead.
The legacy client therefore attempts to localize those sentences as keys and
may show a missing/incorrect error rather than the intended text. A replacement
must faithfully decode the structured form, but Stardust should eventually
send valid keys or add a deliberately versioned literal-error message; the
client must not silently guess which representation was intended.

## Replacement boundary

- Build an asset-derived creation catalog; do not hard-code races, hair, or
  customization ranges into UI code.
- Keep a pending request record keyed by operation and selected template.
- Serialize exact legacy field widths and preserve the reserved byte.
- Treat success/failure as terminal transitions and make duplicate responses
  idempotent.
- Display unknown/missing STF failure keys safely while logging their exact
  file/key identity.

## Confidence

- All four message identities, field order, client response functions and
  server parsing/creation behavior: **Confirmed by code**.
- UI mediator-to-collector ownership and catalog boundary: **Strongly
  inferred** from concrete send state and original asset dependencies.
- Reserved request byte: **Unknown**, byte-preserved.

