# Original auction query and the Core3 parser subset

October 17, 2026. This corrects the earlier description of two unknown S32
fields. One is a scalar supplied from `ItemTemplateCrc`; the other is the length
prefix of a second Unicode string that the original UI supplies empty.

## Observation and confidence

Input: original Stardust.exe, SHA-256
E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150,
reverified before this probe. No original dynamic execution or live auction
transaction was used.

Constructor `0x00ACFFA0` registers thirteen payload members, in order. Its two
string members at object offsets `+0x60` and `+0x70` both use vtable
`0x015EAD30`; assignments are at `0x00AD00EC` and `0x00AD0119`. Vtable write/read
thunks `0x00ACEA50`/`0x00ACEA70` call `0x011949D0`/`0x011948B0`. Those routines
write/read a U32 code-unit count followed by twice that many bytes. The second
member is therefore a Unicode string, not a scalar integer. **Confirmed.**

The filter/list path `0x010B5BC0` obtains two strings through
`0x010BF090`/`0x010BF0B0` and passes both through request wrapper `0x004CFBB0`
to that constructor. The first getter uses the bound text widget; the second
returns the shared empty string. The original constructor binds only one text
filter widget. **Confirmed for this original UI path**; this does not prove no
other caller can supply a nonempty second string.

Initializer `0x010BD390` binds the literal `ItemTemplateCrc` at `0x018D780C` to
property object `0x0199472C`. Getter `0x010BF2D0`, specifically
`0x010BF347..0x010BF358`, reads that property into its scalar output, defaulting
to zero when absent. The list/request chain supplies that output to the scalar
between category and first text filter. **Confirmed structural/dataflow
observation; strongly inferred template-selection role.** This does not resolve
the separate final U32 in commodity catalog leaves.

## Original wire contract

All integers are little endian. Unicode strings are U32 code-unit count plus
that many UTF-16LE units. Object layouts and native container sizes are not wire
structures.

| Field, in order | Type |
|---|---|
| Archive member count, opcode | U16 14, U32 679E0D00 |
| Location, request counter, search family | S32 each |
| Object category | U32 |
| Template-selection scalar | U32 bits; property ItemTemplateCrc |
| Primary text filter | Unicode32 |
| Second text filter | Unicode32; empty in the investigated original UI path |
| Minimum, maximum price | S32 each |
| Include entrance fee | U8 Boolean |
| Market/vendor identity | U64 |
| Vendor-context flag | U8; exact historical business label remains bounded |
| Page offset | U16 |

The original request has no NGE exact-match Boolean before the template scalar
and no NGE advanced-search list/match-mode members after the fee flag.

## Current Core3 correlation and consequence

Local `MMOCoreORB/src/server/zone/packets/auction/AuctionQueryHeadersMessageCallback.h`
reads the template scalar as `unk1`, the primary Unicode string as `filterText`,
then one S32 as `unk2`. It does not consume bytes for the second string's code
units. `run()` passes neither unknown scalar to `AuctionManager::getData`.
This is a server-only parser limitation, not proof of the deployed revision.

An empty second string is exactly U32 zero, so both endpoints agree on offsets
for that original UI path. A nonempty second string moves price, fee, identity
and pagination bytes relative to the callback's reads. It must not be sent to
this Core3 profile as though supported. Do not silently truncate such a filter
or call a broad query equivalent to an exact requested search. Basic category,
primary-text, price, fee, location and pagination queries can use the supported
subset after their source serializer is adapted and tested.

## Reproduction and remaining work

Run `tools/python/verify_auction_query_wire_contract.py <original-exe>`.
It verifies the pinned input, constructor/vtable evidence and synthetic field
fixtures. The callback field model confirms alignment for an empty second
filter and misalignment for a nonempty one. It is not a server execution test.
Detailed instruction exports remain private; the producer is
`tools/python/export_replication_vector_instructions.py` with bounded ranges
listed in the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-17_AUCTION_QUERY_TYPES.md).

The source conversion's catalog adapter has separate build/live receipts in its
own repository. Query serialization, capability presentation, original compact
response rows, live terminal browsing and transactions remain implementation
gates. No new message identity, inventory row, semantic alias or coverage score
is added by this correction.
