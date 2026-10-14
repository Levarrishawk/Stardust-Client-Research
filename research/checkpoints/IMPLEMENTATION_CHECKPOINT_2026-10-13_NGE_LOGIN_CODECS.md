# NGE source conversion: Stardust login codecs and UI transition

Date: October 13, 2026. Bounded question: restore the original login version/GUID boundary, decode the actual Core3 login responses and reach stable character selection. Runtime delivery is C:\Stardust-Experimental; source is SD-Src-Client, HEAD 25c173bc2eac4dd48e41236cb4ce96d972d1818e with uncommitted changes. The original Stardust executable matches the previously verified SHA-256 baseline. No new original VA analysis was needed.

## Evidence and implementation consequences

| Observation | Confidence | Consequence |
|---|---|---|
| Original login is username/password/version only, version 20260817-SD:01; NGE default 20100225-17:43 and GUID overload appends a field/ignores override. | Confirmed prior executable contract and current source. | Default restored; both constructors serialize exactly three strings and share override behavior. LoginConnection uses the two-argument path; MachineGUID collection removed. |
| Core3 status records end after five four-byte fields and bool; NGE reads another ten bytes. | Confirmed source and existing independently authored fixture. | Restore record boundary; initialize unavailable NGE permission extensions false. Multi-record native fixture verifies alignment. |
| Received opaque token length was narrowed to U8 before entering a U16 session buffer. | Confirmed source. | Preserve lengths through U16; reject above 65,535. The wire length remains U32. |
| Core3 character response uses Unicode32 names, U32 template/galaxy/type and U64 object ID. | Confirmed research/server boundary, actual native decoder fixture. | Positive character fixture and every truncated prefix tested without using account data. |
| First submitted Next crashes at CuiMessageBox's null input page before connection opening. | Confirmed matching v120 PDB/DIA function/source line, effective ui/ui_messagebox.inc and call order. | Ordinary message-box construction/layout tolerates absent newer input/verification widgets. Explicit extension requests fail rather than bypassing confirmation/challenge behavior. |
| Subsequent Next reaches AvatarSelection, which requires absent checkHideClosed. | Confirmed fatal, source AvatarListChanged transition, effective ui/ui_avatar_selection.inc. | Guard checkbox registration/visibility and absent separate Back/Exit captions. Core viewer/table/buttons and deletion-confirmation bindings are present and remain required. |

The second live failure demonstrates progress to the server-fed character-list transition. Stable visual roster acceptance and clean disconnect/session release remain unverified at this checkpoint. No character was selected, created or deleted.

## Reproduce and verification

Source tools/python/run_login_wire_tests.py builds tests/compatibility/LoginWireTests.cpp against the actual Release/Win32 serializers. Outputs stay in Experimental diagnostics. Its synthetic data opens no sockets, uses no original assets/credentials, disables RegistryKey writes and uses nonmodal foundation teardown.

Accepted result: **193 checks, 153 malformed-input rejections**. Exact login default/override/GUID bytes, token preservation including 300-byte data, galaxy enumeration/status alignment, character record decoding, truncated status/character prefixes and oversized token rejection pass. Existing tools/python/verify_login_zone_fixtures.py also passes.

The native ClientIdMsg fixture checks current-source version/token serialization only. Full zone authentication remains open, including reconciliation of historical operand-count examples and the later packet-map gameBitsToClear finding. Do not promote this into a claim of zone-entry compatibility.

## Documentation correction and publication boundaries

The [login state machine](../../docs/LOGIN_AND_ZONE_STATE_MACHINE.md) prose previously swapped the fourth/fifth status values. October 13 reconciliation states the exact wire order already used by the existing fixture and Core3 serializer: 100, 0xCB2, 8, 0xFFFF8F80, 2, then zero byte. Other field semantics remain qualified. Original packet maps/registries and all research denominators are unchanged.

Runtime target/configuration hashes and endpoint pins remain private. Only the copied operator login hostname was resolved. User enters/submits the password locally; no credentials are recorded in curated evidence. Dumps are private; only curated fault locations are reported. Source docs/conversion/LOGIN_COMPATIBILITY.md is the implementation runbook and exact live acceptance resume point.

## Next probe

Rebuild the guarded AvatarSelection UI, then obtain a user-submitted login and verify the rendered received character list without selecting a character. If another failure occurs, identify its required UI/data/protocol boundary from the matching build, preserve the original corpus, and continue. After a stable list, verify normal disconnect/session release before considering zone entry. Buff-builder and collections retain their user-approved post-main-conversion/x64/DX12 deferral.

## Accepted retry and validation

The operator confirmed **Character list appeared** after submitting the corrected client. The delivered/source hashes match B14E10ABED62A82D92921281E254A2C7A9149D33721730DBD0BCAB2536AC5676; link/build completed with 24 dependency warnings and zero errors. This accepts live authentication/enumeration reaching character selection on operator observation, without another automated visual inspection. No character/world entry was requested or tested. The process subsequently ended without a new crash report, and the operator confirmed closing it normally. Normal application closure is operator-confirmed; no process exit code or server-side session-release receipt was collected.

Workspace validation passes with 1,964 Markdown files, zero broken/private links, 2,626 unique provisional addresses and 345 packet rows. Relevant source and research diff/link checks pass. No generated/private material is staged or published. Next: disconnect/zone-authentication boundary and exact Pre-CU replicated schemas before entering the world.
