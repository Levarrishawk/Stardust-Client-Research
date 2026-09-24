# Login screen UI to connection — gated block A09 / W03, W10

Date: 2026-09-24. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static analysis only; no connection to the configured live service was attempted. Decompiler output is reconstructed pseudocode, not original source.

## Prior boundary and question

The existing UI RTTI inventory identified `SwgCuiLoginScreen`, and the network documents established the application login packet and configured endpoint. They did not connect this screen's controls to endpoint selection and connection creation. The prior `SwgCuiAvatarSelection` `0x00C62970` SelectCharacter route is already closed; it is **not** counted again here. See `ANALYSIS_CHECKPOINT_2026-09-21_UI_MEDIATOR_CLASS_INVENTORY.md`, `ANALYSIS_CHECKPOINT_2026-09-15_SELECT_CHARACTER_SESSION.md`, and `docs/NETWORK_ARCHITECTURE.md`.

## New executable evidence

| Function / source | Observation | Confidence |
|---|---|---|
| `0x00C8CE00` `SwgCuiLoginScreen` constructor | Binds `LocalButton`, `FinishButton`, `CancelButton`, `UsernameTextbox`, `PasswordTextbox`, `pageSession`, and `pageNormal`; installs callback/mediator vtables `0x015F0504` and `0x015F0588`. | Confirmed by RTTI/vtable immediate references and constructor code. |
| `0x00C8D190` activation | Registers a `CuiLoginManager::Messages::AvatarListChanged` transceiver, subscribes to `LoginConnectionOpened`/`LoginConnectionClosed`, chooses session-versus-normal page visibility, and activates the `LoginScreen` mediator. | Confirmed; exact behavior of every UI-helper call is not asserted. |
| `0x00C8D460` button callback | The Local button sets a mode flag and transitions toward `SceneSelection`; Finish invokes `0x00C8D5D0`; Cancel takes a separate close/exit path. Offset matching uses constructor-bound button members at object `+0x9C/+0x98/+0x94` after the callback-subobject adjustment. | Confirmed branch/control identity; precise Cancel presentation remains untraced. |
| `0x00C8D5D0` Finish path | Reads two UI strings, rejects empty first string in the non-session mode, shows a progress/wait UI, copies the strings to GameNetwork state via `0x00462800` and `0x00462970`, and enumerates `[ClientGame] loginServerAddress%i` plus `loginServerPort%i` from index zero until the first absent address. Only entries with a nonzero U16 port enter the candidate vector. | Confirmed instruction/decompiler path. The names of the two intermediate UI accessors are provisional; the bound username/password controls and downstream GameNetwork fields strongly support credential interpretation. |
| `0x00C8D5D0` endpoint choice | If the candidate vector is empty, uses a configured/default getter pair; otherwise computes a random index modulo candidate count, stores chosen address in `0x00504D40` global and chosen port in `0x00504ED0`, then calls `0x00462290` to create the login connection. | Confirmed branch and data flow. The exact fallback getter semantics are not closed here. |
| `0x00462290` | Clears any prior network setup, creates shared network options and constructs a login connection via `0x0069BB50`; this is connection setup, not authentication completion. | Confirmed call flow; lower SOE behavior was mapped earlier. |
| `0x00C8DC30` update / `0x00C8D3A0` deactivate | An avatar-list-changed state flag triggers transition from `LoginScreen` to `AvatarSelection`. Deactivation removes its avatar-list transceiver and login-open/close subscriptions and clears a UI state flag. | Confirmed typed event, transition literals and removal path. |
| `0x00C8D9E0` event receiver | `LoginConnectionOpened` while a pending modal exists displays a status message and subscribes to `LoginClientToken`. `LoginConnectionClosed` branches on active-screen state and pending modal, may close the modal/re-enable a control, and clears the pending-connect flag. Message-box completion/closed events clear modal or pending state. | Confirmed event comparisons and control flow; exact localized prose and all error codes not yet traced. |

The installed `swgemu_login.cfg` has `loginServerAddress0=live.stardust-swg.com` and `loginServerPort0=44453`. This is evidence of the *configured candidate*, not evidence that we contacted it or that it is the only possible endpoint in every installation.

## Contract, corrections and remaining work

The observable screen/session sequence is: activate login UI → Finish gathers credentials → select indexed configured endpoint (or fallback) → start login connection → login-manager avatar-list event → transition to avatar selection. A compatible client need not reproduce the original mediator inheritance or randomized choice if it has one endpoint; it must preserve configured endpoint and credential-to-login behavior. The credentials must not be logged in research fixtures.

This block adds UI-to-connection evidence; it does **not** close the full login workstream. Remaining named questions: precise blank-credential/error presentation; Cancel semantics; fallback endpoint getter values; localized open/close/modal prose and retry/error UI; exact lifetime of credentials after submit. Those require targeted call/handler traces before claiming the login mediator closed. No whole-client percentage increase is assigned.

Evidence source: executable decompilation of the listed addresses in ignored `local-private/session-2026-09-23-marathon/` and read-only installed configuration. Confidence: **confirmed** for branch/event/endpoint/connection data flow; **strong** for semantic credential naming at the textbox-to-network bridge; **unknown** for the listed failure/presentation details.
