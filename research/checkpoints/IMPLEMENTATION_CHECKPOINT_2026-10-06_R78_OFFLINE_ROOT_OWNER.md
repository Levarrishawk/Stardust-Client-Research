# Round78 offline root owner — 2026-10-06

Question: can one wrapper own actual synthetic bootstrap transport/root session and close them in order while preserving scene/FIFO borrowers and histories? Stop after one handshake/three updates/one frame plus ownership negatives.

Input accepted77 frozen4537 files; manifestfae3830a886e9cb4bfd6639c6337052539a35e77c84dd9c5dadf62212f32650f, archive4a11a66f312fd654aae850dc94c3214b58130068ce7896108e31dc78a5d275d4. Trusted sync checks accepted own77/prior/incoming hashes, HEAD/index before writes. No EXE input/VA claims.

Observation: frozen bootstrap returns caller-owned transport; frozen RootSession only borrows it. Wrapper takes both lifetimes, guards properties/disposal by thread, requires active world, disposes root session before transport Close in finally. It holds children for historical inspection and does not dispose borrowed world/store/FIFO. Partial root construction closes created transport; source-inspected hidden cleanup.

Findings Confirmed by31 executed checks: independent exact14/17 handshake and one root/protected frame/ACK with three actual ordinary point-driver updates/combined receipts. Teardown fixture's owned retained1/pending1 clears, borrowed FIFO1/current LOCAL/active world/driver survive. Histories/strings retained after idempotent disposal; caller drains surviving FIFO. Null/cap/closed-world constructor and null/stale/canceled/owner/disposed-use gates preserve appropriate prefixes.

Interpretation/compatibility consequence: wrapper owns transport and session, exposed references are borrowed and must not be independently advanced/disposed outside intended Session.Observe. Existing hidden queue association/capacity caller contracts remain. Synthetic bytes do not prove a server handshake/ACK, and direct teardown-fixture transport mutation is not normal ownership policy. [Durable contract](../../docs/OFFLINE_SOE_ROOT_MOTION_SESSION_OWNER_CONTRACT.md) records final API and limits.

Tools cached Python independent struct/binary32/frozen peer expectations before native, offline .NET8, workspace/UTF8/AST/XML/diff/scope/private-boundary/hash gates. First replay passes; zero warnings/errors, zero broken/private links, frozen4537 preserved/HEAD/index unchanged. No existing production edits, sibling78 inputs, original assets/EXE/socket/send/capture or publication. Private failure cleanup/order and rare allocation errors source-inspected, not injected. No registry/coverage denominator changes; shared living docs lead-owned.

Next: lead configured host uses owner, preserves serialized Session.Observe and separate scene/FIFO cleanup, independently checks unchanged bytes/history. Seal and idle before next assignment.
