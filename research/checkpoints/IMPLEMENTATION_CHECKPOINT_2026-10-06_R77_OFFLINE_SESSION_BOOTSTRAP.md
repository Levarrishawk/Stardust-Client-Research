# Round77 synthetic session bootstrap — 2026-10-06

Question: can a tiny synthetic helper replace profile handshake hex using exact frozen raw boundaries and caller-owned transport cleanup? Stop after nine independent handshake pairs and three actual point/session updates.

Input accepted76 frozen4504 public files; manifest00f4d8e82b7d84df3f00b539d649d48a9e39aaca0edbe360c440c2386a21e5fa, archive0e5378488a7244bfd3a2e0da0de0710d93ad1eca20a6f0601872aa5870963960. Trusted sync verifies prior/own sealed hashes/HEAD/index before copying. No executable input/VA claims.

Observation: actual SoeSessionCodec request length14/protocol2 and response length17; BeginSession and AcceptSessionResponse install state with selectors1/4. No response encoder exists. Helper packs only established raw response schema with fixed max496/CRC2 and caller ID/seed, explicitly DecodeResponse then actual AcceptSessionResponse. [Durable contract](../../docs/OFFLINE_SOE_SESSION_BOOTSTRAP_CONTRACT.md) records exact fields/representative hex and lifecycle.

Findings Confirmed by57 native checks: three IDs × three seeds give nine independently expected14/17-byte pairs; caller transport starts Established/empty, owns one retained and one pending message, closes idempotently to zero counts, strings survive. Three actual frozen point updates (two moving, one stopped) produce one independently predicted root/datagram/ACK and three exact RootSession receipts. Session precancel/disposal preserves bootstrap transport until caller Close.

Interpretation/compatibility consequence: returned transport belongs to caller; composed session borrows it. Hex is synthetic construction history, not server receive/send evidence. Helper exception catch Close and scratch zeroing finally are source-inspected; no public parameter-induced failure/cancellation path exists in fixed U32 API. No fault injection or execution claim for rare allocation errors.

Tools cached Python independent struct/binary32/frozen peer framing before native, offline .NET8, validator/AST/XML/UTF8/diff/hash/private-boundary checks. First replay passes, zero warnings/errors; zero broken/private links; exact frozen scope/HEAD/index preserved. No older production changes, sibling77 imports, original assets/EXE execution/socket/login/live contact/capture/fuzz/publication. No registry/name/coverage denominator change; shared canonical status/ledger remain lead-owned.

Next: lead user-profile settings call Create(connectionId,seed), remove handwritten handshake fields, preserve caller Close and synthetic labeling, independently check unchanged profile receipts. Seal and idle.
