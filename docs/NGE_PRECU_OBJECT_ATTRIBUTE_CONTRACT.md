# Original object attribute boundary

October 14, 2026. Bounded question: why does the source-derived client throw while opening the original inventory, and what request/response contract must it preserve?

## Evidence and identity

The documented Stardust.exe baseline SHA-256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 is reverified. Original AttributeListMessage construction at VA 00AD1D70 registers only the object-ID variable at object+0x38 and attribute vector at +0x48 (registration calls 00AD1E54/00AD1E5C), then unpacks at 00AD1E67. The existing [attribute-manager investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_OBJECT_ATTRIBUTE_MANAGER.md) supplies the parser, listener/cache/events and batching chain.

The local Core3 producer MMOCoreORB/src/server/zone/packets/scene/AttributeListMessage.h constructors emit LE16 operand count 4, LE32 opcode F3F12F2A, LE64 object ID and LE32 pair count. insertAttribute emits the narrow key and Unicode value. GetAttributesBatchCommand::doQueueCommand consumes each decimal argument token as an object ID; no revision token is parsed. This local source is correlation evidence, not proof of the running server build.

The F18F0C2F source client enters the stationary world, then fails during the bounded inventory retry. Owned first-throw stack: Archive::get -> AutoArray<pair<string,Unicode>>::unpack -> AttributeListMessage constructor -> ObjectAttributeManager listener -> network receive/dispatch. Own-image base 00020000; relevant RVAs 005FC97A, 00E817F0, 00E81257 and 006FF6B7. No payload, object ID, credential or endpoint is captured. This identifies the decoder for this run. It is consistent with the earlier intermittent Archive string-bounds failure, but does not prove every earlier fault had this cause.

## Wire contract

| Field | Representation | Rule |
|---|---|---|
| Operand count | LE U16 | Original automatic serializer writes 3 including opcode; local Core3 writes 4. Original automatic reader consumes but does not validate this count. Both observed envelopes carry the same payload. |
| Identity | LE U32 | F3F12F2A, AttributeListMessage. Auction details FE0E644B are a separate decoder despite the server class-name alias. |
| Object ID | LE U64 | Preserve all bits; unrelated to native pointer width. |
| Pair count | LE U32 | Zero is valid; ordered pairs follow. |
| Attribute key | Narrow Archive string | U16 byte count; FFFF sentinel followed by U32 byte count; then bytes. |
| Attribute value | Unicode Archive string | U32 UTF-16 code-unit count; little-endian units. |

There is no static-item name or revision field. The NGE source inserts a static-item name before the vector and revision afterward; reading original bytes with that schema shifts the list boundary. NGE requests also append revisions between IDs, so Core3 can interpret those tokens as additional object IDs.

A compatibility decoder may explicitly accept the observed operand counts 3/4, validate the opcode, preserve ordered key/value strings and reject truncated/oversized fields, impossible pair counts and trailing bytes. A pair requires at least six bytes of length prefixes; compare the count with remainingBytes/6 before constructing entries. Parse into a local owned result and publish only after complete validation. Empty replies retain their legitimate missing/inaccessible-object meaning. These stricter rejection rules are implementation policy; the original count reader was more permissive.

Pre-CU requests contain space-delimited decimal IDs only. NGE retains ID/revision pairs. Revision zero in the adapted cache is a local placeholder, not recovered server metadata. Cache replacement, formatting and typed AttributesChanged publication remain above the codec.

## Confidence and compatibility consequence

Confirmed: original two-variable registration, current source schema mismatch, identified first-throw decoder, local server producer/parser contract, synthetic native decoder/request checks and the bounded live inventory/item-information result. Strongly inferred: this mismatch also explains at least some earlier intermittent string-bounds failures. Unknown: whether all previous late failures share this cause, behavior on other accounts or inventories, and complete inventory/equipment gameplay parity.

The source implementation belongs in the sibling SD-Src-Client repository, not this research workspace. Its engine-neutral PrecuObjectAttributeProfile codec is wired to AttributeListMessage and both requested/watched batching paths under usePrecuReplication. NGE decoding and request shape remain intact. Full Release build E49357E5B990E1904D03209A2D89FD3F71BC5BC69C99A411F6A4FCF0B9AC5531 passes; native suite 12,088 checks/3,751 malformed rejections plus five isolated CRC/ACST cases. Added cases exercise counts 3/4, zero/one/two ordered pairs, Unicode, empty key/value, narrow extended-length sentinel, every fixture prefix, bad headers/counts/lengths/trailing bytes, ID-only and NGE requests, and NGE object/static-item response round trips.

Live computer use observes world entry, original inventory and paperdoll, shirt condition/volume and tooltip, icon/list view switching, Close/reopen, normal Disconnect/reentry and reconstructed inventory. No equip/transfer/delete or deliberate gameplay mutation is performed. Original preview buttons remain authored hidden, so their input behavior is not yet accepted. No complete endurance or game-client claim follows.

Next probe: inspect further read-only original UI consumers and retain first-throw diagnostics for any new decoder failure. Validate larger inventories and actual equipment transactions only in a separately bounded authorized test. No packet coverage denominator or canonical alias count changes.
