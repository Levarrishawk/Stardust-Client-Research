# Round76 synthetic ACK factory — 2026-10-06

Question: can an explicit stateless offline helper create the exact bounded channel0 ACK using frozen framing, without session-state effects or an ACK corpus? Stop after72 independent vectors, configuration negatives and15 actual point/session steps.

Input accepted75 frozen4469 public files; manifest873d99dd4dcefb51300e68942dbf31e5bd71d24c593fbd9e2ceb6f3ba497fa97, archive2887d1c027dff45fcb834e72228359219f52528985e0631470b9734f7926a845. Dispatch's stale Frozen74 phrase was explicitly corrected to frozen75; sync and actual RootSession APIs use accepted75 only. No executable input/address claims.

Observation: frozen framing supports CRC1..4, adds marker/selector4/seeded CRC. New helper writes exact0015/U16BE into four stack bytes after width/range/capacity guards, delegates framing and zeroes scratch in finally. It returns fresh caller-owned array, stores no state and performs no receive/send. [Durable contract](../../docs/OFFLINE_SOE_ACKNOWLEDGEMENT_FACTORY_CONTRACT.md) records API, bytes and ownership.

Findings Confirmed:350 native checks/72 independently expected protected ACKs and four-byte decodes; sequences0/1/255/256/32767/65535 × seeds0/A1B2C3D4/FFFFFFFF × widths1..4. Exact-capacity success, invalid width/max range/max capacity and upper-range small allocation pass. Returned buffers survive another call's clearing and are cleared by caller. Temporary stack zeroing is source-inspected, not dynamically exposed.

Brief actual frozen point-driver/session integration passes15 updates and15 exact combined receipts, three independently predicted roots/datagrams/ACKs through movement/pause/resume/stop. Factory leaves actual session/queue/transport state unchanged; explicit offline processing subsequently releases roots. No full cadence/retransmission resurvey or sibling76 API used.

Tools: cached Python independent framing/struct/binary32 expectations before native, offline .NET8, workspace validator and AST/XML/diff/hash gates. Final build zero warnings/errors; first replay passed. Frozen scope/HEAD/index/private-ignore preserved; zero broken/private links. No old production source edits, original assets, EXE run, socket/send/capture/live legality claim, registry/name/count denominator change. Shared ledgers/status remain lead-owned.

Compatibility consequence: helper is synthetic offline convenience, never evidence of a server ACK receive. Caller supplies sequence/config and owns result cleanup; RootSession's dedicated CRC2 contract remains. Next: lead replaces the new offline profile's256 ACK entries with on-demand caller-owned factory output, validates unchanged session receipts and disposes actual owners. Seal and idle.
