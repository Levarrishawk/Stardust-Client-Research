# NGE source conversion: offline Stardust login startup

Date: October 13, 2026. Question: can the built NGE client consume the installed Stardust overlay sufficiently to reach its actual login screen, with unsupported NGE extensions disabled? Stopping condition: visual login-screen confirmation, no credentials/server contact, reproducible profile and named missing-data rejection. This is a source-conversion receipt, not a clean-room replacement-client or protocol acceptance claim.

## Inputs and tools

Source: `C:\Users\ancie\Documents\GitHub\SD-Src-Client`, HEAD `25c173bc2eac4dd48e41236cb4ce96d972d1818e` with uncommitted changes. Delivery: `C:\Stardust-Experimental`, using 48 configured copied TREs and unchanged priority order. Effective metadata index: 203,617 names. Proprietary payloads and generated diagnostics remain private.

VS2013 Update 5/v120 MSBuild, Release/Win32 SwgClient target; bounded offline launches and a symbol-backed exception probe; Computer Use visual inspection. Automatic connections false, numeric loopback port zero and blank credentials. No original executable launch or live-server contact. Fresh original VA analysis was unnecessary.

Delivered executable SHA-256: `2A4FBB2C3C1A44CF91A56AFF963BD8D188D7A31C8E773FBFD53B29160D2A4C0E`. Profile SHA-256: `06E8803F6DB84E849168D55FD39765C30056E3F8D1EE2DE36FD2BC815F0D5C9F`. Matching runtime hashes, named switches and reproduction steps are recorded in source `docs/conversion/EXPERIMENTAL_STARTUP.md`.

## Observations, confidence and consequences

| Observation | Confidence / interpretation | Compatibility consequence |
|---|---|---|
| NGE ConfigFile scalar overloads return the last occurrence; indexed overloads select an occurrence. Early raster override was superseded by options, loading an old renderer and failing a delayed import. | Confirmed source and runtime. Earlier first-wins receipt was incorrect and is corrected in the source runbook. | Put includes before scalar overrides. Loaded-module inspection confirmed matching gl05 and system D3D9. |
| Missing NGE builder/collections and extension metadata prevent initialization; several managers dereference missing tables. | Confirmed source and bounded startup failures. | Named profile switches quarantine those loads. Required unrelated data still fails. Retain ownership and safe empty query paths. |
| Both saddle consumers ask for sat_name; original logical table uses sat_or_skt_name. | Confirmed source/corpus; see [mount research](ANALYSIS_CHECKPOINT_2026-09-21_MOUNT_SADDLE_PIPELINE.md). | Add the exact alias only; reject neither-name cases. Startup passes both managers. |
| NGE projectile loader reads column 13, beyond the original thirteen-column table. | Confirmed source/exception and 17-row effective corpus; [projectile research](ANALYSIS_CHECKPOINT_2026-09-21_PROJECTILE_MANAGER.md). | Preserve required prefix and constructor default when the NGE extension is absent. Gameplay beam-count/visual equivalence remains unverified. |
| Video and heat-compositing templates refer to absent shader programs; alternate lightsaber table is missing. | Confirmed configured-overlay metadata/source and runtime. | Disable optional video/alternate loads and existing POST option. Do not manufacture shader assets or introduce generic shader fallbacks. |
| Copied splash lacks NGE 0ANV, while login CodeData supplies required fields. | Confirmed effective UI and constructor failure. | Existing skipSplash flag now bypasses promotional bindings at construction; page pointers initialize for cleanup. Actual Stardust login mediator activates. Splash presentation remains unverified. |
| Rendered Stardust login has empty username/password fields and Back/Next controls. | Confirmed visual inspection and responsive process. | Offline startup milestone reached. No acceptance of login authentication, zone entry or gameplay. Normal UI shutdown was not established; owned process stopped for negative tests. |

Source buffers/callbacks/controllers/HUD actions and factory registrations are gated for disabled buff-builder; builder data ownership/teardown is retained. Collections table/factory/HUD actions are disabled. New flags default to existing NGE behavior; the authored offline profile disables them explicitly. No UI include fabrication was needed.

## Deferred versus required

The user explicitly puts buff-builder and collections **after all main conversion goals, including x64/DX12**. Their source remains for later authored UI/data and Core3 support. The preserved builder retains NGE expertise assumptions and is not yet an enabled, playable Pre-CU feature.

Original Pre-CU timed buff add/remove (`0x229`, `0x22A`) remain main-client requirements, separate from negotiation UI and newer metadata. See the [packet map](../../docs/PACKET_HANDLER_MAP.md). Other startup exclusions are provisional and must be reconciled against required Pre-CU workflows; they are not an authorization to omit gameplay.

## Validation

- Final source build: 38 legacy warnings, zero errors.
- Exact copied overlay reaches visually verified login screen with matching delivered renderer and system D3D9.
- Explicit enableBuffBuilder=true negative launch rejects absent builder table with named fatal.
- Explicit enableCollections=true negative launch rejects absent collection table with named fatal.
- Both negative processes exited; original profile bytes/hash restored before relaunch.
- Disabled session/controller guards inspected, not packet-driven tests. Enabled-feature assets/UI/protocol remain untested.

Workspace validator passed: 1,958 Markdown files, zero broken links or links into private storage, 2,626 unique provisional addresses and 345 packet rows. Focused source/research Markdown links and UTF-8 reads passed; git diff --check passed in both repositories. No generated/private files appeared among source Git changes, and the source index remained empty. No curated registry, original-function label, packet denominator or research coverage score changes are warranted by this source-conversion milestone.

## Next bounded question: exact login version and codecs

Hash of the working `C:\Stardust-DEV\Stardust.exe` was freshly checked and equals the documented baseline `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Its researched default login and zone version is `20260817-SD:01`; [login state machine](../../docs/LOGIN_AND_ZONE_STATE_MACHINE.md). NGE source `sharedNetworkMessages/src/shared/common/GameNetworkMessage.cpp:21` instead defaults to `20100225-17:43`.

Source `clientLoginServer/ClientLoginMessages.cpp`: two-argument LoginClientId reads SharedNetworkMessages/version; GUID constructor ignores that override and appends another string. `clientGame/src/shared/network/LoginConnection.cpp::onConnectionOpened` chooses the GUID variant when user GUID is populated. ClientIdMsg separately supports the override. This is a confirmed starting-source discrepancy; no packet was sent.

Core3 `MMOCoreORB/src/server/login/account/AccountManager.cpp::loginAccount` parses and validates the requested version before normal credential validation; AccountManager.h::isRequiredVersion compares exact strings unless the configured requirement is empty. ConfigManager.h::getLoginRequiredVersion reads Core3.LoginRequiredVersion with historical repository fallback. SWGRealms builds delegate approval. The local tree does not prove the Debian deployment version or configuration.

Next: pin deployment/version, reconcile both outgoing login routes and zone ClientIdMsg using original byte fixtures, then compare token length and cluster/status/character response codecs. Do not weaken authentication/version enforcement or transmit a hardware identifier solely to support an unverified extension. Startup remains offline until those changes are concrete and the bounded test can be reviewed.
