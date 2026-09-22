# Analysis checkpoint: planetary map locations

Date: 2026-09-15

## Result

The `GetMapLocationsMessage` / `GetMapLocationsResponseMessage` exchange is closed from client request construction through Stardust filtering and serialization to the executable's `PlanetMapManager` snapshot installation.

The original protocol supports three arrays of `MapLocation` records and three associated scalar tokens. Stardust sends a full snapshot in the first array, empty second and third arrays, and zero tokens. Dormant meanings are preserved as unknown.

## Request (`0x1A7AB839`)

Constructor `0x00ACEAC0` and parser `0x00ACEC70` establish: ASCII planet/zone name followed by three U32 tokens. Normal request path `0x004B9270` sends the current planet and cached tokens; `0x0048A7B0` sends a derived planet and three zeros. Both use reliable send `0x00462670`.

Stardust parses only the ASCII planet, requires an authenticated player, resolves the named zone and responds only for a non-space zone. It ignores all three tokens, so current responses are full snapshots.

## Response (`0x9F80464C`)

Deserializer `0x00ACF5A0` reads ASCII planet, three arrays of `MapLocation`, then three U32 tokens. Members are planet `+0x38`, arrays at `+0x48/+0x58/+0x68`, and tokens at `+0x78/+0x80/+0x88`.

Array parser `0x00ACFA70` reads U32 count and records. Record parser `0x00B1B640` reads:

| Field | Type | Client offset |
|---|---|---:|
| object ID | U64 | `+0x00` |
| display name | UTF-16 | `+0x08` |
| map X | F32 | `+0x14` |
| map Y | F32 | `+0x18` |
| category | U8 | `+0x1C` |
| subcategory | U8 | `+0x1D` |
| icon | U8 | `+0x1E` |

This is two-dimensional x/y, not transform x/z/y. Stardust patches array-0 count after writing visible records, then writes two zero counts and three zero tokens. Compression is enabled.

## Server record policy

`MapLocationEntry::insertToMessage` omits null/uncategorized objects and applies recipient visibility. Faction-only categories require matching faction. Cloning facilities can require Imperial, Rebel, Jedi, light-Jedi or dark-Jedi eligibility. Regions use their area-shape center. Each admitted record carries ID, server-resolved display name, x/y, category, optional subcategory, and icon (`0` none, `1` moon, `2` star).

## Client consumption

Listener `0x004B87E0` parses the response and passes planet, three arrays and three tokens to `0x004B9D40`. The manager clears its outstanding flag, rejects the wrong planet, compares/caches tokens, replaces corresponding 32-byte-record arrays, and publishes refreshed map state.

Category/subcategory numbers are interpreted using original client category data and UI assets. Diagnostic `0x018DFE10` confirms runtime construction failure reporting includes ID, name, category, subcategory and flags.

## End-to-end mapping

| Message/opcode | Codec | Handler | State/action | Confidence |
|---|---|---|---|---|
| `GetMapLocationsMessage` / `0x1A7AB839` C->Z | `0x00ACEAC0`; server reads planet | `GetMapLocationsCallback::run` | request recipient-filtered non-space-zone snapshot | Confirmed |
| `GetMapLocationsResponseMessage` / `0x9F80464C` Z->C | `0x00ACF5A0`, `0x00ACFA70`, `0x00B1B640` | `0x004B87E0` -> `0x004B9D40` | replace cached map arrays/tokens and notify map UI | Confirmed; dormant array meanings Unknown |

## Original-asset dependency

The wire carries numeric categories, not textures, localized category labels or map geometry. A replacement client must consume original category configuration, localization/string tables, planet-map imagery and marker UI assets. Server display names and visibility decisions remain authoritative.

## Replacement requirements

- Preserve ASCII planet identity, UTF-16 names, U64 IDs and F32 x/y.
- Bounds-check array counts and string lengths.
- Structurally support all three arrays/tokens.
- Validate category bytes against loaded original data.
- Discard responses not matching the outstanding planet request.
- Keep server recipient filtering separate from client display filtering.

## Remaining bounded unknowns

- Original semantic names/delta behavior of arrays 1/2 and tokens.
- Exact UI refresh granularity.
- Category-table variation among deployed asset sets.

These do not block current Stardust full-snapshot compatibility.
