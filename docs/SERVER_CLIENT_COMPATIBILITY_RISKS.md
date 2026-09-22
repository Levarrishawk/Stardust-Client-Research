# Server/client compatibility risks

This register records byte-level contradictions between `Stardust.exe` and the current local Stardust server source. It is intentionally separate from the packet-handler map: a packet can be fully documented and still be incompatible.

| Status | Risk | Consequence | Resolution gate |
|---|---|---|---|
| open, active | quest counter/timer IDs `0x440/0x441` versus server `0x441/0x444` | wrong factory or absent factory; body schema collision | select and test an original-client-compatible or explicitly negotiated server profile before quest UI implementation |
| open, active | name verification response omits UTF-16 name | strict client parser reads a truncated string | update server producer or implement one-request-at-a-time named compatibility recovery |
| dormant | create-ticket response omits second U32 | truncated response if reactivated | correct constructor before enabling ticket service |
| dormant | harvester delta helper uses inconsistent tag/schema | corrupt replication if called | keep unreferenced or replace with correct active hierarchy delta |
| dormant | loop-times wrapper has one ignored trailing U32 | no current effect | normalize if diagnostic exchange is enabled |

Primary evidence and detailed policy: `ANALYSIS_CHECKPOINT_2026-09-21_SERVER_SCHEMA_RISK_AUDIT.md`.
