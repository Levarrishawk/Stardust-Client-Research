# Pre-CU spatial-chat source adapter

Date: October14,2026. Bounded correction for the NGE source client; original text/language/grammar presentation is retained.

## Observation and identity

Constrained-HUD buildFCE1A0DA5053F18A4AA31BDF365F23DB29AE3C016A9904118AD26834761167CC fails during its authorized existing-character entry. The first-throw stack is Archive::get → MessageQueueSpatialChatArchive::get → ControllerMessageFactory::unpack → ObjControllerMessage → GameNetwork listener. Count-only receipt records outer0x80CE5E46/172 application bytes, without message content or IDs. **Confirmed current failing decoder**, separate from the earlier combat-spam fault.

Original SHA256E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 is reverified before static probes. Registered reader wrapper00AEF2C0 calls00B1BA00; writer00AEF2A0 calls00B1BC40. Reader calls00B1BADF/BAE9 read two NetworkIds;00B1BAF3 reads UTF16 text. Direct reads00B1BB03/BB10/BB1D consume two bytes each;00B1BB2A consumes one flags byte;00B1BB41..BB46 reads/advances one language byte. Unicode calls00B1BB4D/BB57 read OOB and source name. Writer00B1BCA9/BCB3/BCBD, then00B1BCCD/BCDA/BCE7, BCF4/BD01 andBD0B/BD15 independently confirms order and widths. **Confirmed original codec structure.** Existing downstream behavior is documented in [controller effects](CLIENT_CONTROLLER_EFFECTS.md) and [catalog checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_MOOD_AND_SPATIAL_CHAT_CATALOGS.md).

The source NGE codec instead consumes U32 flags immediately after text, then the three U16 fields. This both shifts fields and can interpret later bytes as a Unicode length. Some synthetic original bodies are accepted with wrong fields rather than throwing; decoder success alone is not compatibility evidence.

## Original inner body and current producer variant

After the existing controller envelope/scalar: U64 sender, U64 chat target, U32 UTF16-unit count/text, U16 volume, U16 spatial type, U16 mood, U8 flags, U8 language, U32 UTF16-unit count/OOB and U32 UTF16-unit count/source name. Widths are little-endian; IDs retain all64 bits. Text and opaque OOB/source-name strings are owned values. The outer scalar is not an additional inner field.

Core3 correlation: Stardust-3/MMOCoreORB/src/server/zone/packets/object/SpatialChat.h, all three constructors. Literal and @file:key variants append16 zero bytes after language. The original codec consumes the first eight as two empty strings and leaves eight unread. Structured StringIdChatParameter instead occupies the OOB string and has the final empty source name. Its wrapper padding is authored by MMOCoreORB/src/server/chat/ChatParameter.cpp::insertToMessage; nested prose bytes/grammar remain a separate consumer contract.

PrecuSpatialChatProfile implements the original compact shape and additionally accepts exactly eight trailing zero bytes only when OOB/source name are empty. It retains that producer-padding variant for exact round trips. Other tails/nonzero padding reject. Complete consumption and this narrow admission policy are modern bounds policy, not original malformed-input parity. No message variant is selected from an invented discriminator.

MessageQueueSpatialChat retains a copied padding flag alongside existing owned data. Its decoder translates the explicit ClientGame.usePrecuReplication profile into the existing spatial-chat presentation model. The source encoder uses the original compact grammar for locally constructed Pre-CU messages; flags above255 reject before writing, rather than silently truncating a newer high-bit flag. The NGE codec stays available under false. No live outgoing chat is submitted by this test.

## Verification and remaining gate

Release client build succeeds. Native11439 checks/3538 malformed rejections plus five isolated boundary cases pass: literal/@localized/opaque-OOB shapes, compact and current-producer zero tails, owned copy, high ID bits, exact fields, prefix rejection with the separately legal compact prefix, nonzero/extra padding, flags width, registered172-byte controller round trip, and retained NGE fields/prefix rejection. The synthetic172-byte application matches the live length, not captured live content. Arbitrary opaque OOB fixtures do not establish nested prose localization.

Initial fixture failures are preserved in the investigation history: missing fixture UnicodeUtils include, running the new install before the controller factory, and an overly strict expectation that the old NGE decoder always throws. These are corrected without widening the decoder to accept malformed data.

BuildB78D8649F02B17AB05DBB0F421008B8D40F5053D284F406CF228E87E5C7444D1 is deployed with matching symbols and unchanged renderer/TREs for live observation. Next: verify naturally received spatial chat, stationary rendering and independently constrained Health/Action bars with Mind hidden; then normal disconnect/re-entry. Nested prose/language/animation fidelity, outgoing server acceptance and endurance remain open. [Daytime checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md).

Subsequent live receipt: the same build renders the stationary world with full independent Health/Action widths and Mind hidden. Count-only spatial receipt records text-units0/OOB-units55/padding0, establishing a naturally received structured-body decode. A user-triggered Ctrl-O then fails at a missing options widget, a separate UI asset mismatch. No claim of complete nested prose rendering or natural combat-spam receipt follows. The later NGE options loose override permits the options tabs and normal Disconnect returns to selection; same-process re-entry is under observation.
