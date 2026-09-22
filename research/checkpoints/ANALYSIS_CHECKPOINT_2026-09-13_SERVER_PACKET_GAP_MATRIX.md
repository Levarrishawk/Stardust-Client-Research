# Stardust server packet gap matrix and ChatSystem closure

Date: 2026-09-13  
Client authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Server authority: local Stardust-3 source tree

## Matrix outcome

The first mechanical cross-reference now covers every class declared in the 337 server packet headers. It records class/base, inferred direction, declared opcode when locally visible, source-reference count, and whether the private client documentation already contains the class name or opcode.

Initial result: 202 classes have a documentation hit and 143 source-referenced classes do not. Of those 143 candidates, 66 expose a literal top-level opcode in their header (64 S2C, 2 C2S). The remainder are predominantly baseline/delta subclasses, controller codecs, callback classes whose opcode is registered elsewhere, or support records; they are not 77 confirmed missing client handlers.

After publishing the message closures completed later in this marathon and rerunning the same scanner, the matrix reports **210 documented classes and 135 reachable-gap candidates**. Exactly **60** of the remaining candidates expose a literal opcode (58 S2C and 2 C2S). These are the operative post-session triage counts; the initial counts above are retained to show the effect of the completed handler work.

Family concentration among all candidates is chat 44, UI 20, auction 11, player 11, tangible 7, ship 7, object 6, and zone 6. This makes chat/UI breadth—not the login handshake—the largest immediately visible protocol-documentation gap.

## Limitations and confidence

This is a **triage matrix**, not yet proof of runtime reachability. A source-reference count greater than one proves use outside the declaration text but may represent inheritance, inclusion, or construction. A documentation hit proves prior coverage exists somewhere, not that its field map is complete. Each opcode-bearing gap still requires client class/registry/handler evidence before closure.

False positives are intentionally retained for auditability. `MessageCallback`, delta/baseline subclasses without standalone opcodes, `RadialMenuItem`, and packed transform/quaternion helpers are structural rows that should be reclassified rather than reverse-engineered as top-level messages.

## First closed high-use gap: ChatSystemMessage

Stardust emits opcode `0x6D2A6413` from 45 source references. Two wire variants share one message:

1. U8 display type, UTF-16 literal message, U32 zero parameter length;
2. U8 display type, empty literal, U32 UTF-16-unit length, then a `StringIdChatParameter` prose package containing STF identity, TU/TT/TO substitution records, integer/float variables and a final byte.

The client embeds `ChatSystemMessage` at `0x0189B6EC`. Listener `0x008ABDA0` verifies that message type, deserializes through `chat_system_message_deserialize_VA_00AEE970`, and passes the object to `chat_system_message_publish_VA_008ABEB0`. Legacy members are display type at `+0x3C`, literal Unicode at `+0x44`, and structured/localizable payload at `+0x54`.

`chat_system_message_build_presentation_VA_009CAA40` packages display policy and both text forms and invokes the localization/prose service. The publisher fans the resulting payload out to registered chat/system-message consumers and the UI message channel. Server display values are 0 for chat plus screen and 2 for chat only; the client preserves and forwards the byte rather than collapsing it during parsing.

## Next matrix priorities

After removing structural rows, close opcode-bearing candidates by source use and replacement impact. Immediate targets are PlayClientEffectLoc/Object, ChatSystem-adjacent room/friend/instant-message families, auction query/result flows, Create/DestroyClientPath, SurveyMessage, and remaining derived baseline/delta schemas. The generated `server_packet_gap_matrix.csv` is the stable queue for subsequent blocks.
