# Analysis checkpoint — chat-room slash-command entry path

Question A09 / W10+W04. Target: verified `C:\Stardust-DEV\Stardust.exe` and previously correlated current Stardust chat callbacks. This is a **new input entry point** into already mapped chat manager senders; it does not add a new wire opcode or re-count the earlier Chat Rooms button workflows.

RTTI `SwgCuiCommandParserChatRoom` has vtable `0x015F81A0`; constructor `0x01038B10` registers command root `chatRoom` with help text. Its virtual parse method is the large `0x01038BC0`. The ordered literal-token table at `0x015F814C..0x015F8198` contains `create`, `destroy`, `join`, `leave`, `list`, `listAll`, `refresh`, `send`, `who`, `query`, `invite`, `uninvite`, `invites`, `info`, `op`, `deop`, `ops`, `kick`, `ban`, `unban`. These are parser tokens, not network opcodes.

Direct parse branches establish useful alternate routes:

| Token family | Client route and behavior | Confidence |
|---|---|---|
| `join` | Takes a room path/name argument and calls manager `0x00A27DB0`, which resolves a local room when available and reaches `0x00A27B90` (`ChatEnterRoomById`); it has a separate create/fallback branch for unresolved names. | Confirmed route; unresolved-name policy partial |
| `leave` | An explicit room argument calls `0x00A291F0`; no argument uses retained current-room ID `DAT_0198D2A0` through `0x00A29000`, or writes a local error if absent. | Confirmed |
| `create` / `destroy` | `create` passes path plus two Boolean tokens (each checks whether its first UTF-16 character is `1`) and title to existing `0x00A28C10`; `destroy` passes room path to `0x00A28D40`. | Confirmed parse branches; external argument labels inferred from known sender schema |
| `refresh` | Calls guarded room-list request `0x00A27EE0`. `list` and `listAll` walk local room structures and append formatted text; their display content is local, not proof of a fresh server request. | Confirmed |
| `send` | Collects remaining token text and calls `0x00A28720`; that method splits a leading room name from the remaining Unicode text, resolves the room and calls `0x00A289A0` (`ChatSendToRoom`); local parse/lookup failures are reported without a send. | Confirmed |
| `query`, `who`, `invite`/`uninvite`, `op`/`deop`, `kick`, `ban`/`unban` | Branches resolve an explicit room argument or current-room fallback, then call existing chat-manager query/moderation functions. `invites`, `info` and `ops` have local formatting paths. Fine-grained text formatting and every permission error string are not closed here; the server remains authoritative. | Confirmed branch families/callees; partial formatting semantics |

This parser is important to a replacement even if the graphical room panels are rebuilt: existing users and scripts may invoke `/chatRoom ...`; it feeds the same underlying protocol state rather than a parallel chat transport. The future command layer can map typed slash arguments into the same chat service API. No executable runtime fixture or packet interception was performed.

Evidence: exact RTTI/vtable, constructor `0x01038B10`, parser `0x01038BC0`, string-pointer table `0x015F814C..0x015F8198`, manager paths `0x00A27DB0`, `0x00A27B90`, `0x00A28C10`, `0x00A28D40`, `0x00A27EE0`, `0x00A28720`, `0x00A289A0`, `0x00A291F0`, `0x00A29000`; [Chat Rooms UI blocks](ANALYSIS_CHECKPOINT_2026-09-24_CHAT_ROOMS_MAIN_MEDIATOR.md) and [send protocol](ANALYSIS_CHECKPOINT_2026-09-15_CHAT_SEND_ROOM_MESSAGE.md). Decompiled C is reconstructed pseudocode. Confidence: **confirmed by code** for token inventory and principal call routes; **unknown/partial** for all local error text, unresolved-name join policy, and runtime slash reachability.
