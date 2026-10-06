# Round71 peer disconnect — 2026-10-06

Question: does actual matching standalone disconnect clear transport while preserving borrowed scene owners and pending root FIFO until caller cleanup? Stop after bounded offline positive/negative fixtures and lifecycle proof.

Input: accepted70 frozen4297 public files; manifest12bb3453e94a2041068db521c9cdcea6486a009baedab72a7e2f6e73cf814c37, ZIP eb66e5727b76f79969738c26c5ac89da1cb16f7422612920cacd4d26b9f7428d. No new executable address claims or executable input used. Tools: cached Python independent struct packing/frozen peer framing, offline .NET8 native runner, workspace validator and diff checks.

Observation: transport ValidatePacket requires decoded length8; AcceptDisconnect reads BE32 ID only and calls Close. Close zeroes pending arrays, clears reliability/fragments, resets session fields and sets Closed. Ingress forwards PeerDisconnected and owns no scene close. Root bridge declines non-Established before FIFO take. Pump.Close owns world/queue/job cleanup. Exact bytes and replay are in the [durable contract](../../docs/SCENE_WORLD_PEER_DISCONNECT_CONTRACT.md).

Tested observation:39 checks pass; one exact caller ACK, one actual typed correction/ordinary driver tick, four negative packets, two pending root applications and two canceled/drained jobs. Wrong connection and valid-CRC seven-byte decoded packet throw, leave transport prefix and terminal-fault ingress. Bad CRC/short framing reject without terminal fault. Valid standalone disconnect closes with no apps/control, preserving scene/FIFO/control ownership until explicit owner disposal. Alternative reason FE DC accepted without interpretation.

Interpretation/confidence: Confirmed offline frozen behavior and ownership; source-inspected private zeroing/reset. Compatibility consequence: caller handles disconnect receipt and separately drains outbound FIFO and closes/drains scene pump. Decoder acceptance does not establish live server legality or reason semantics. No shared production change, full retransmission suite, original assets or live activity.

Initial replay assertion assumed Ready after the minimum barrier; fixture actually permits AwaitingServerReady. Corrected assertion uses the frozen active states; retained failed replay logs. Final native build has zero warnings/errors. Root living metrics/ledgers remain lead-owned; this milestone changes no registry, coverage denominator or semantic name.

Next probe: lead joins matching disconnect after movement arrival, checks host-owned cleanup and records remaining live phase/reason uncertainty. Worker stops after sealed handoff.
