# Analysis checkpoint: customer-service category request

Date: 2026-09-15

## Result

The `RequestCategoriesMessage` / `RequestCategoriesResponseMessage` workflow is closed from the executable's request source through Stardust serialization to the client customer-service category cache and UI notification boundary.

The protocol supports a recursive category tree. Current Stardust deliberately bypasses category loading, however, so its live response contains the leading zero field and an empty root vector. The recursive grammar remains necessary for compatibility with original or future populated responses.

## Request (`0xF898E25F`)

Client function `0x005C96F0` obtains the current language string from the game configuration object, constructs `RequestCategoriesMessage` with `0x00ADF140`, and sends it reliably through `0x00462670`. Constructor and parser evidence establish one ASCII string field after the generated message header.

Stardust registers `RequestCategoriesMessageCallback` at literal opcode `0xF898E25F`, reads the ASCII language, and asks `HolocronManager` to send the response. The language is currently not used to select a category tree.

## Response (`0x61148FD4`)

Stardust writes U32 zero, U32 root category count, then each category recursively. The executable deserializer at `0x00AFAA40` registers the U32 at object `+0x3C` and a vector of 32-byte category records at `+0x40` (storage accessor `0x00AFAC50` returns `+0x44`). Vector codec vtable `0x015EBC64` uses serializer `0x00AFAD10` and parser `0x00AFAD60`.

Record serializer `0x00B1D050` confirms this recursive layout:

| Field | Type | Client record offset |
|---|---|---:|
| category name | UTF-16 | `+0x00` |
| category ID | U32 | `+0x0C` |
| child count | U32 | wire only |
| children | recursive category records | `+0x10` vector |
| flag 1 | U8 | `+0x1C` |
| flag 2 | U8 | `+0x1D` |

The record parser is the corresponding recursive codec reached through vector parser `0x00AFAD60`; record lifetime/destruction is rooted at `0x00ADEAB0`.

## Client consumption

Customer-service listener `0x00A19580` matches the generated message type, invokes `0x00AFAA40`, takes the category-vector accessor at `0x00AFAC50`, and calls `0x005CAB60`. That function deep-copies the records into the manager-owned vector at `0x0191525C`, then publishes the response to registered `CuiCSManagerListener` subscribers. The leading U32 is parsed but not consulted, so its meaning remains unknown.

## Current Stardust behavior

`HolocronManager::loadBugCategories()` returns immediately before its database-loading code. Consequently the category vector remains empty and `sendRequestCategoriesResponseTo()` emits zero plus a root count of zero. The unreachable code indicates an intended Mantis-backed tree, but this is dormant evidence, not current behavior.

## End-to-end mapping

| Message/opcode | Codec | Handler | State/action | Confidence |
|---|---|---|---|---|
| `RequestCategoriesMessage` / `0xF898E25F` C->Z | `0x00ADF140`; ASCII language | `RequestCategoriesMessageCallback::run` | request the customer-service category tree | Confirmed |
| `RequestCategoriesResponseMessage` / `0x61148FD4` Z->C | `0x00AFAA40`, vector `0x00AFAD60`, recursive category codec | `0x00A19580` -> `0x005CAB60` | replace the CS category cache and publish a UI response event | Confirmed; leading U32 meaning Unknown |

## Replacement requirements

- Preserve the two U32 response prefixes and recursive UTF-16 category grammar.
- Bound recursion depth, counts, aggregate nodes, and string lengths.
- Deep-copy or own decoded strings and children independently of packet storage.
- Accept an empty tree as the expected response from current Stardust.
- Keep the language field even though the current server ignores it.
- Do not infer flag meanings without populated evidence.

## Remaining bounded unknowns

- Semantic name of the leading response U32.
- Meanings of the two per-category bytes.
- Original populated category hierarchy and localization policy.

These do not block compatibility with current Stardust.
