# Resolution of Server-Only Message-Class Names

Date: 2026-09-11  
Executable: `C:\Stardust-DEV\Stardust.exe`  
Server scope: `MMOCoreORB/src`

`PROTOCOL_CATALOG_COVERAGE.md` mechanically found 37 server classes ending in `Message` or `Msg` without an exact same-name ASCII string in the executable. Exact spelling is a weak test, so this ledger resolves the misses without inflating them into protocol incompatibilities.

## Result

| Resolution tier | Count | Meaning |
|---|---:|---|
| Correlated to an executable route, alias, or registered ObjectController type | 34 | The missing literal is explained; numeric/structural evidence anchors compatibility. |
| Confirmed dormant/obsolete server artifact with no executable route | 3 | Source class is never constructed and the executable contains neither its active identity nor opcode bytes. |
| Server wire schema known, direct executable handler still uncorrelated | 0 | No entries remain in this reconciliation ledger. |
| Total exact-name misses | 37 | Reconciles exactly with the catalog. |

This raises the **identity-resolved lower bound** from 77/114 exact names to 111/114 classes (97.37%). It does **not** mean 97.37% of packet schemas or handlers are complete; multiple classes are aliases for one envelope, and class frequency is not weighted.

## Resolved aliases and top-level routes (15)

| Server class | Executable/client identity | Evidence status |
|---|---|---|
| `AccountVersionMessage` | `LoginClientId`, `0x41131F96` | Confirmed constructor/parser correlation |
| `BaseLineMessage` | `BaselinesMessage`, `0x68A75F0C` | Confirmed route and wrapper |
| `ClientIdMessage` | `ClientIdMsg` | Confirmed constructor/parser correlation |
| `DeltaMessage` | `DeltasMessage`, `0x12862153` | Confirmed route and wrapper |
| `LightUpdateTransformMessage` | `UpdateTransformMessage`, `0x1B24F808` | Same bytes/opcode; confirmed |
| `LightUpdateTransformWithParentMessage` | `UpdateTransformWithParentMessage`, `0xC867AB5A` | Same bytes/opcode; confirmed |
| `ObjectControllerMessage` | `ObjControllerMessage`, `0x80CE5E46` | Confirmed top-level route |
| `StandaloneObjectControllerMessage` | standalone transport form of `ObjControllerMessage` | Confirmed shared application envelope; delivery distinction retained |
| `SceneObjectCreateMessage` | `SceneCreateObjectByCrc`, `0xFE89DDEA` | Confirmed parser/create route |
| `SceneObjectDestroyMessage` | scene destroy opcode `0x4D45D504` | Confirmed parser/destroy route |
| `SceneObjectCloseMessage` | `SceneEndBaselines`, `0x2C436037` | Confirmed hash alias, parser, object lookup and per-object finalizer |
| `AuctionItemDescriptionMessage` | active wire/client identity `GetAuctionDetailsResponse`, `0xFE0E644B` | Confirmed opcode alias; executable parser, auction cache/update handler and UI publication |
| `ItemSoldMessage` | active wire/client identity `CreateAuctionResponseMessage`, `0x0E61CC92` | Confirmed opcode alias; executable parser, pending-request cleanup, status presentation and typed UI publication |
| `UpdateCellPermissionsMessage` | `UpdateCellPermissionMessage`, `0xF612499C` | Confirmed singular/plural alias |
| `UpdatePVPStatusMessage` | executable `UpdatePvpStatusMessage`, `0x08A1C126` | Confirmed spelling alias, parser, object lookup, and status/faction state writes |

## Resolved ObjectController inner classes (15)

These server class names are not expected to appear as top-level message names. Each is carried inside opcode `0x80CE5E46` and has a recovered numeric client factory except the separately documented Stardust quest mismatch.

| Server class | Inner type | Status |
|---|---:|---|
| `AddBuffMessage` | `0x229` | Registered client factory |
| `BeginHyperspaceMessage` | `0x3FE` | Registered client factory |
| `GroupMemberSendSpaceInviteMessage` | `0x437` | Registered client factory |
| `GroupMemberSpaceInviteResponseMessage` | `0x439` | Registered client factory |
| `HarvesterResourceDataMessage` | `0xEA` | Registered client factory |
| `HarvesterSetupSharedNetworkMessage` | `0x22B` | Registered client factory |
| `ImageDesignRejectMessage` | `0x239` | Registered client factory |
| `NpcConversationMessage` | `0xDF` | Registered client factory |
| `OrientForHyperspaceMessage` | `0x42D` | Registered client factory |
| `PlayersNearYouMessage` | `0x1E7` | Registered client factory |
| `PostureMessage` | `0x131` | Registered client factory |
| `QuestTaskCounterMessage` | server `0x441`, client counter `0x440` | **Confirmed mismatch**, not a resolved compatible mapping |
| `QuestTaskTimerMessage` | server `0x444`, client timer `0x441` | **Confirmed mismatch**, no client `0x444` factory |
| `RemoveBuffMessage` | `0x22A` | Registered client factory |
| `StartingLocationListMessage` | `0x1FC` | Registered client factory |

The 29-count means “identity explained,” so it includes the two quest identities even though their current numeric compatibility is defective. They must remain negative compatibility tests.

## Confirmed dormant server-only artifact

| Server class / opcode | Server-derived wire body | Disposition |
|---|---|---|
| `GameSceneChangedMessage` / `0xBBADAEB9` | no body | Header-only legacy artifact: no current producer; no executable name, opcode bytes, parser or dispatcher route |
| `ConnectionServerTestMessage` / `0x0E20D7E9` | ASCII16 `ConnectionServer:1348` | Removed 2008 developer packet-test case: no current producer; no executable name, opcode bytes, payload literal, parser or route |
| `LinkDeadMessage` / `0xA16CF9AF` | no body; declares operation count 5 | Incorrect alias/incomplete stub for exact hashed identity `HeartBeat`; neither class has a current producer and the executable has no identity/opcode route |

## Remaining direct client-route correlations

None. All 37 server class names missing as exact executable strings now have an executable correlation or an evidence-backed dormant/obsolete disposition.

Four prior rows are now resolved further. Server `AppendCommentsResponseMessage` maps to executable `AppendCommentResponseMessage` parser `0x00AFA390` and apply path `0x005CBC10`; server `SearchKnowledgebaseResponseMessage` maps to executable `SearchKnowledgeBaseResponseMessage` parser `0x00AF8060` and customer-service publication; server `PlayerMoneyResponseMessage` maps to executable `PlayerMoneyResponse` parser `0x013F63A0`; and `ResourceHarvesterActivatePageMessage` maps through parser `0x013F3F60` to the harvester UI boundary at `0x008CC460`.

## Important anomaly

`0xFE0E644B` is conclusively the hash of executable identity `GetAuctionDetailsResponse`; server class `AuctionItemDescriptionMessage` is an older source-side alias and is not instantiated by the current source tree. The active server path uses the bazaar/vendor constructor of `AttributeListMessage` to emit the same identity with the full client-supported schema. Likewise, `0x0E61CC92` is the exact hash of executable identity `CreateAuctionResponseMessage`, not `ItemSoldMessage`; the server's latter class name is misleading because the packet reports creation of an auction/immediate-sale listing, not a later purchase. Finally, exact hashing resolves `0xA16CF9AF` as `HeartBeat`; `LinkDeadMessage` is an erroneous duplicate alias, not a second semantic identity. These cases justify keying the replacement catalog by numeric opcode and validated schema, with source class names stored as aliases.

## Next use

This exact-name-miss reconciliation ledger is closed. The three dormant artifacts are not handler-search items unless future server code begins constructing them or a different authoritative client build supplies a route.

## LinkDeadMessage / HeartBeat collision resolution

The exact engine hash gives `HeartBeat == 0xA16CF9AF`; `LinkDeadMessage == 0x740D0DBC` and `LinkDead == 0xB80A60B9`. Thus `HeartBeat` is the only valid hashed identity for the shared numeric value. The current headers differ structurally: `HeartBeat` declares operation count 1 and no body, while `LinkDeadMessage` declares operation count 5 but writes no fields. The latter is an incomplete or erroneous duplicate stub, not evidence for a five-field client contract.

Neither class is constructed in the current source. Stardust.exe contains none of the opcode bytes, `HeartBeat`, `LinkDead`, or related parser/registration evidence. Local history shows old `HeartBeat` production only in `PlayerManagerImplementation::attemptPlayerCreation`, immediately before `ClientCreateCharacterSuccess`; it was removed during the 2009 IDL-era refactor. No historical `new LinkDeadMessage` producer was found. Server-side player link-dead state is instead driven by disconnect/session lifecycle logic and is not this application packet.

For the analyzed pair, catalog `0xA16CF9AF` as the obsolete zero-body `HeartBeat` identity, retain `LinkDeadMessage` as an invalid source alias, and do not implement its operation-count-5 shape. A replacement client may safely log/ignore the opcode unless a future server reintroduces an evidence-backed producer.

## ConnectionServerTestMessage historical/disposition resolution

The current source tree contains only the packet header. It writes operation count 2, opcode `0x0E20D7E9`, and ASCII16 literal `ConnectionServer:1348`; there is no current constructor call. Stardust.exe contains none of the class name, the payload literal, or little-endian opcode bytes `E9 D7 20 0E`, and therefore has no direct named or numeric route for it.

Local repository history establishes provenance. Commit `ade150f21e` (2008-12-03) introduced it as case 5 of `GameCommandHandler::sendp`, under the comment `TESTING PURPOSES ULTYMAS.` The command manually sent a collection of experimental packets selected by an integer. Commit `c093b0719d` (2008-12-08) removed the entire testing switch, including this only producer, while the unused header remained. This is an obsolete developer smoke-test packet, not the connection-server handshake or lag protocol. Current connectivity uses SOE connection lifecycle events, `ClientIdMsg`, and the separately recovered `LagRequest`/`ConnectionServerLagResponse` exchange.

## GameSceneChangedMessage reachability resolution

The current source tree contains only `GameSceneChangedMessage.h`; no production code includes by use or constructs the class. Its complete body is operation count 1 plus opcode `0xBBADAEB9`, with no payload. A byte-exact scan of Stardust.exe finds no little-endian `B9 AE AD BB`, and its string/RTTI inventory contains neither `GameSceneChangedMessage` nor a corresponding network-message parser type. Therefore this exact executable cannot directly dispatch that opcode through its normal named-message mechanism.

Stardust.exe does contain RTTI for `Game::Messages::SceneChanged` transceivers consumed by scene/UI systems such as hyperspace, space-death and toolbar code. Those are in-process `MessageDispatch` notification types, not evidence of a network packet bearing `0xBBADAEB9`. Conflating them would invent a bridge unsupported by code. The compatibility disposition is **dormant/unsupported legacy packet**: do not send it from the current server, and let a replacement client's unknown-opcode policy log/ignore it unless future evidence establishes a required behavior.

## ItemSoldMessage naming and route resolution

The exact engine string hash gives `CreateAuctionResponseMessage == 0x0E61CC92`; `ItemSoldMessage` instead hashes to `0x4C8D5CB0`. Stardust.exe registers the former identity in its auction listener. The receive branch at `0x006A8291` parses the two-field archive with `0x00AE8650`, extracts the result record through `0x006A88B0`, and passes it to `0x004CD7D0`. The handler removes the matching pending request keyed by the U64 ID, maps the U32 result to auction/vendor-specific localized presentation, and publishes `AuctionManagerClient::Messages::CreateAuctionResponse`. Values outside the client's handled result domain reach the diagnostic `AuctionManagerClient::receiveCreateAuctionResponse unhandled result %d`.

Current server request handlers establish the role. `CreateAuctionMessage` (`0xAD47021D`) and `CreateImmediateAuctionMessage` (`0x1E9CE308`) both parse object ID, vendor ID, price, duration, description and premium flag, then call `AuctionManager::addSaleItem` with the auction-mode Boolean set respectively true or false. Validation failures and the final successful listing creation return `ItemSoldMessage(objectId, status)`. The current named status subset is 0-9, 13 and 14. Therefore status 0 means that listing creation succeeded; it does not mean another player bought the item. The callbacks' contradictory duration comments (minutes versus seconds) remain a request-side unit question requiring implementation inspection or a controlled trace.

## AuctionItemDescriptionMessage naming and reachability resolution

The exact engine string hash gives `GetAuctionDetailsResponse == 0xFE0E644B`; `AuctionItemDescriptionMessage` hashes to `0x174D8A0E`. Stardust.exe registers and dispatches the former identity. Its listener branch at `0x006A8010` compares the message hash, parses with `0x00AE7B60`, converts the archive record with `0x006A88E0`, and applies it through `0x004CD5B0`.

The active server request path is `GetAuctionDetails` (`0xD36EFAE4`, U64 listing ID) -> `GetAuctionDetailsCallback` -> `AuctionManager::getItemAttributes` -> bazaar/vendor `AttributeListMessage` constructor using response opcode `0xFE0E644B`. It emits `U64 listingId`, `Unicode description`, `U32 attributeCount`, repeated `ASCII attributeName + Unicode value`, `ASCII templateFile`, and `ASCII customization`. The two older standalone response headers are not constructed anywhere in the current source tree and describe only the zero-attribute prefix of this richer schema.

## SceneObjectCloseMessage naming resolution

`SceneObjectCloseMessage` is a server C++ class name, not the hashed protocol identity. Applying the exact engine `String::hashCode` algorithm gives `STRING_HASHCODE("SceneEndBaselines") == 0x2C436037`; `STRING_HASHCODE("SceneObjectCloseMessage") == 0x5F62343B`. The executable compares the former identity at `0x0051C314`, parses one LE64 object ID through `0x00AD58D0`, resolves the object, and calls `0x0051F9B0`.

Server send ordering independently fixes the lifecycle meaning: create, containment, baselines, contained/slotted objects, then `SceneObjectCloseMessage` when `doClose` is true. Guild and manufacture-schematic producers likewise send it only after their baseline views. It is therefore an end-of-initial-baselines/finalization barrier, not object destruction or UI closure. Destruction remains the separate `SceneDestroyObject` opcode `0x4D45D504`.
