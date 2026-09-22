# Localization and STF Runtime

## Scope

This block separates runtime localization from the similarly named DataLint asset inventory and maps the original client's STF path construction, cache/fallback policy, binary table reader, in-memory indices, and StringId validation behavior.

## Runtime architecture

```text
StringId { table, key }
        |
        v
localized-table manager/cache
  primary locale path ----+
  fallback locale path ---+--> TreeFile/VFS open --> LocalizedStringTable parser
                                                |
                                      key index + numeric-ID index
                                                |
                                      UTF-16 localized value
```

`localized_string_table_cache_get_VA_0118DC40` first checks the selected cache, constructs a locale-dependent VFS path ending in `.stf`, opens and parses it through `localized_string_table_load_VA_01190FB0`, stores the result with a last-use timestamp, and retries through its alternate/fallback path when the primary load fails. The table name is retained on the loaded object.

The path is resolved through the existing TreeFile/VFS abstraction. Original STF files can therefore remain inside TRE archives; they do not need to be extracted for normal lookup.

## Locale configuration

`shared_game_config_load_VA_011AA4B0` reads these `SharedGame` settings:

| Key | Default/role |
|---|---|
| `defaultLocale` | locale used to build the primary/fallback table path |
| `fontLocale` | independently selects font/glyph behavior |
| `debugStringIds` | defaults true |
| `displayBadStringIds` | defaults true |

The distinction between text locale and font locale is observable and should be retained. The exact empty-string default ultimately supplied by the config helper is not assigned a language name here.

## STF binary table

The reader validates:

- 16-bit magic `0xABCD`;
- one-byte format variant, accepted values `0` and `1`;
- a 32-bit maximum/next-ID-related value;
- a 32-bit record count;
- a first pass containing numeric record identities and Unicode string values;
- a second pass containing numeric identity and length-prefixed narrow key names.

The two accepted variants use separate string-record readers (`0x01192300` and `0x011924E0`) but populate the same logical table. Each completed entry links a numeric ID, narrow key name, and owned UTF-16 value. Failed reads, invalid magic, unsupported variants, or duplicate/index insertion failure reject the table rather than returning partial content.

Some local-variable identities in the decompiler are damaged by optimized stack reuse, so this report deliberately avoids claiming byte offsets for every header member beyond the independently visible magic, variant, counts, identity, name, and value components.

## In-memory indices and lookup

`LocalizedStringTable` is a 0x40-byte object with:

- retained table name;
- a numeric-ID ordered index;
- a string-key ordered index;
- a maximum/next-ID-related U32;
- ownership of allocated UTF-16 record values.

`localized_string_table_lookup_key_VA_01191140` performs a key comparison in the name index, copies the canonical stored key when requested, and returns the associated numeric identity. `localized_string_table_find_id_VA_011912F0` traverses the same logical entries and returns the key/value record for a numeric identity. Destruction explicitly frees each owned localized UTF-16 value before releasing both indices.

The higher client layer validates both dimensions of `StringId`: `string_id_validate_or_warn_VA_008825F0` distinguishes an invalid TABLE from an invalid NAME and honors the debug/display-bad-ID policy.

## Important separation: DataLint

The `0x00A8C140..0x00A8CF30` family enumerates registered assets for lint reports. Its `.stf` predicate and “Localized String Tables” heading are asset-audit behavior, not the runtime lookup implementation. It is optional development tooling for a replacement and must not be mistaken for the live localization cache.

## Replacement requirements

- Read original binary `.stf` files through the same VFS/TRE precedence as every other client asset.
- Preserve `StringId` as the pair of narrow table name and narrow key; do not replace it on the network with localized text.
- Store decoded strings internally as Unicode scalar text or UTF-16-aware strings, while preserving original code-unit content at the asset boundary.
- Maintain primary-locale then fallback resolution and separate text/font locale configuration.
- Cache immutable parsed tables by canonical table identity; use safe shared ownership rather than raw pointers and timestamps embedded in tree nodes.
- Treat malformed STF data as a bounded asset error. Never trust counts or lengths before bounds checking.
- Preserve diagnostic distinction between a missing table and a missing key.

## Confidence

- **Confirmed:** `.stf` VFS path suffix, two cache/path choices, failed-primary retry, magic, accepted variants, two-pass identity/value then identity/name organization, dual indices, UTF-16 ownership, key and ID lookup, and bad-table/bad-name diagnostics.
- **Strongly inferred:** primary path is the selected locale and retry path is fallback/default locale, based on manager construction and configuration flow.
- **Unknown:** historical source names for all header members, exact semantic label of the maximum/next-ID field, and exact language represented by an empty default configuration value.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- `localization_addresses.txt`
- `decompiled-localization/`

