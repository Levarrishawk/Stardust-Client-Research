# Harassment Report UI and Name Verification

Date: 2026-09-22

## Result

The downstream consumer of `VerifyPlayerNameResponseMessage` is now bounded.
`SwgCuiHarassmentReport` validates the entered target name asynchronously,
retains the canonical returned UTF-16 name only on success, updates status
text, and gates report submission. The previously identified current-server
truncated-string response remains a real compatibility defect.

## Client evidence

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00E14E20` | `swg_cui_harassment_report_construct_VA_00E14E20` | binds submit/cancel/verify controls, name input, verification status and comments; subscribes verify-name response | Confirmed |
| `0x00E15250` | `swg_cui_harassment_report_button_handler_VA_00E15250` | trims/validates player name and comments, starts verification or submission, and cancels/closes | Strongly inferred |
| `0x00E15510` | `swg_cui_harassment_report_verify_response_VA_00E15510` | consumes `(Boolean exists, Unicode canonicalName)`, clears pending UI state, displays failure or retains returned name and success state | Confirmed |
| `0x00E15900` | `swg_cui_harassment_report_update_VA_00E15900` | advances pending/feedback timing and status presentation | Strongly inferred |

## End-to-end flow

```text
entered player name
  -> VerifyPlayerNameMessage
  -> Stardust lookup
  -> VerifyPlayerNameResponseMessage {U8 exists, Unicode echoed/canonical name}
  -> CommunityManager typed response
  -> SwgCuiHarassmentReport verify callback
  -> submission enabled only with verified target + nonempty report data
  -> existing customer-service ticket/report path
```

The UI trims whitespace from both name and comments. While verification is
pending it disables the relevant interaction state; the callback restores it.
On false it presents the localized failure state. On true it copies the
returned UTF-16 name into retained report state rather than trusting the raw
input.

## Current server defect

Prior byte-level analysis proved that current Stardust writes the Boolean and a
Unicode length of one but omits the corresponding UTF-16 code unit. This is
incompatible with the executable's expected `Boolean + Unicode string`
contract and prevents reliable canonical-name retention. A future server fix
must serialize the full echoed/canonical name; the replacement client should
remain strict about bounded Unicode decoding and diagnose the malformed legacy
response.

## Replacement-client requirement

Keep name verification asynchronous and correlated to the active form. Do not
submit a report using stale results from a previous query. Treat returned
canonical name as server data, retain UTF-16/wire compatibility, and route the
final report through the already mapped customer-service subsystem.

## Confidence

Controls, typed response ownership, Boolean branch, canonical-name copy and
server schema defect are **Confirmed**. Exact localized wording and feedback
timings are presentation fidelity.
