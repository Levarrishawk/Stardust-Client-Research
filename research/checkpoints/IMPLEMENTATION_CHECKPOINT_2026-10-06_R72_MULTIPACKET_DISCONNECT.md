# Round72 multipacket disconnect — 2026-10-06

Question: which reliable application/control prefixes survive outer-packet ordering, prevalidation and semantic disconnect failure? Stop after two orderings, caller apply/drop policy and two negative prefixes.

Input accepted71:4335 public files, manifest a42f6a8d17554a82c5eb07ad09693484febd48eadf30698458115f8d9cd96426, archive e4f5fd7d111d4006ee09ace0db755353274dbf00bfa5ee848df66322672a79f5. No executable address claims/input needed. Tools cached Python independent packing/frozen peer framing, offline .NET8 native runner, validator/diff/AST/XML/hash gates.

Observation: SoeOuterMultiCodec flattens one-byte-length leaves. AcceptOuterMulti validates all leaf shapes first; dispatches in order; stops at matching PeerDisconnected. Wrong ID is checked during dispatch. Its catch previously cleared collected apps only. Lead explicitly approved adding control zeroing beside app zeroing; successful transfers unchanged. No other production code changes.

Findings/confidence: [durable contract](../../docs/SCENE_WORLD_MULTIPACKET_DISCONNECT_CONTRACT.md) records exact fields/control bytes and all policies.52 native checks Confirmed: app-first returns/copies one root and ACK; disconnect-first none; caller applies or drops pre-disconnect root; pending outbound frame preserved before owner disposal. Malformed suffix rejects before reliable mutation, direct retry delivers; wrong-ID after prefix consumes reliable0 but transfers no receipt/copy, direct retry duplicates. Ingress faults terminally in both. Private arrays' exceptional zeroing is source-inspected; no reflection/backdoor.

Compatibility consequence: distinguish completed reliability from ingress receipt/copy and caller application policy. Caller owns controls and scene/FIFO teardown; no rollback or automatic world closure. Pure transport returned root survives repeated close. Caller controls survive all successful owner closures. Apply context cancels/observes two real controlled resource jobs; no late route.

Validation: independent bytes/expected cases generated before native, final build zero warnings/errors, replay52 checks; validator zero broken/private links, frozen public scope and HEAD/index preserved. Initial compile exposed PublishLocal's bool return; fixture now retrieves actual issued row with Find. Failed build retained privately. No original assets, socket/send, EXE run, live legal-order/reason claim or full reliability suite. Living ledgers/status remain lead-owned; no registry/count denominator change.

Next probe: lead integrates bounded final zero-speed root-before-disconnect and explicitly applies queued prefix before caller teardown. Worker seals handoff then idles.
