# Character deletion: confirmation UI to login protocol and reply

Date: 2026-09-24. Scope A09/W03/W04. Static-only inspection of verified original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, correlated with current Stardust-3 server source. No deletion request was sent. Ghidra output is reconstructed pseudocode.

`SwgCuiDeleteAvatarConfirmation` constructor `0x00E87AE0` binds OK, Cancel, `textBoxAvatar` and instructions. Activation `0x00E87F00` receives a `CuiLoginManagerAvatarInfo` record, retains its object/galaxy identity and builds an instruction panel including avatar information. OK callback `0x00E87ED0` calls `0x00E881F0`; Cancel only closes the mediator. The OK helper checks that the text box is nonempty and equals the literal `delete` converted to Unicode. Mismatch displays feedback and does not emit the event. Match publishes typed `DeleteAvatarConfirmation` carrying the retained avatar info, then closes the dialog. This is a *local explicit-confirmation gate*, not an authentication bypass or the network sender.

`SwgCuiAvatarSelection` subscribes to that typed event in activation `0x00C613A0`. Handler `0x00C63DB0` copies the selected record to stable pending storage and invokes `0x00C63990`. That sender rejects a missing/zero identity, requires an open login connection, constructs `DeleteCharacterMessage` through `0x013F0770` from U32 galaxy/server ID and U64 character ID, and sends via GameNetwork login connection `0x00462710`. It enters pending UI/message-box state. Current server `LoginPacketHandler::handleMessage` registers opcount 3, opcode `0xE87AD031`, and `handleDeleteCharacterMessage` parses U32 server ID followed by U64 character ID, conditioned on an authenticated account.

Server policy is authoritative and materially consequential: the ordinary DB path copies the account+galaxy+OID-matched row into `deleted_characters`, verifies the copy, then deletes from `characters`; the configured SWGRealms API path instead calls its blocking delete operation. It returns `DeleteCharacterReplyMessage` opcode `0x8268989B` with one U32 failure code (`0` success, nonzero failure). A future client must treat deletion as an intentional user-confirmed operation and not probe it during interoperability tests.

The client reply listener `0x00C62F40` matches `DeleteCharacterReplyMessage`, decodes it with `0x013F0610`, and only applies the result while a deletion is pending. On zero it shows success feedback, updates the selected-avatar/list state through `0x00C61AB0` and related refresh calls; on nonzero it shows failure feedback. Both branches clear pending UI state. Exact message-box text and every cached-avatar update are not completely labeled.

```text
selected avatar -> confirmation UI -> typed confirmation event
  -> AvatarSelection -> DeleteCharacterMessage 0xE87AD031 (U32 galaxy, U64 character)
  -> login server account/galaxy/ID policy + archive/delete
  -> DeleteCharacterReplyMessage 0x8268989B (U32 failure code)
  -> AvatarSelection reply handler -> success refresh or failure feedback
```

Confidence: **Confirmed by executable** for control gate, typed event, request construction/send and reply dispatch; **Confirmed by current server source** for opcodes, field parsing, account/DB policy and status encoding. The exact validation of an already-deleted/stale character before request and full post-success cache reconciliation are **partial**. This closes a new request/reply path but does not authorize runtime deletion or change whole-client percentage estimates.
