# Analysis checkpoint: packet-ledger reconciliation

Date: 2026-09-15

## Result

The packet gap CSV was recounted directly after this session. Its authoritative physical state is **284 documented / 61 reachable gaps**, of which **7** have a literal declared opcode and **54** are callback/schema rows without a literal opcode in their packet class.

Recent narrative status entries used projected counts that assumed both sides of several request/response workflows would transition from gap to documented. In several cases the callback side was already documented or was represented under a different class name, so those projections accumulated an eleven-row overstatement. They are not reliable as exact CSV totals.

## Policy going forward

- Treat `server_packet_gap_matrix.csv` recounts as authoritative.
- Label estimated future transitions separately from current counts.
- Do not add request/response counts without checking each exact CSV row first.
- Overall weighted architectural coverage remains **78%**; this correction affects packet-ledger bookkeeping, not the evidence already collected.

## This session's verified transitions

Four physical CSV rows moved from `reachable_gap` to `documented`:

- `RequestCategoriesResponseMessage`
- `ResourceListForSurveyMessage`
- `IsVendorOwnerMessageCallback`
- `IsVendorOwnerResponseMessage`

`SurveyMessage` was already marked documented; this session deepened its executable-side evidence without changing its row status.
