# Core3 Client Reference Implementation

Status: source audit checkpoint 2026-09-08.

The Stardust server repository contains a client-side implementation under `MMOCoreORB/src/client`. This is an important interoperability reference because it uses the same engine transport and packet classes as the server. It is not evidence of the original retail client's internal architecture, and it should not be copied wholesale into the replacement client.

## Confirmed scope

The `core3client` target is built from all sources under `MMOCoreORB/src/client` and links `engine3`, zlib, Boost program options, pthreads, OpenSSL, and the repository's other server dependencies. Its current purpose is scripted login, character/zone actions, and protocol exercise—not rendering.

Key components:

| Component | Responsibility | Evidence |
|---|---|---|
| `ClientCore` | configuration, action dependency ordering, lifecycle, result collection | `src/client/ClientCore.cpp/.h` |
| `LoginSession` | transport connection, authentication request, wait for login result | `src/client/login/LoginSession.cpp` |
| `LoginClient` | `BaseClient` transport adapter and asynchronous message scheduling | `src/client/login/LoginClient.cpp` |
| `LoginPacketHandler` | login response dispatch and field parsing | `src/client/login/LoginPacketHandler.cpp` |
| `Zone` / `ZoneClient` | zone transport, `ClientIdMsg`, object manager, scene state | `src/client/zone` |
| `ZonePacketHandler` | zone message dispatch and limited scene/object parsing | `src/client/zone/ZonePacketHandler.cpp` |
| Actions | scripted login, selection, zone-in, character creation, logout | `src/client/actions` |

## What this resolves

- Default client version: `20050408-18:00` in `ClientCore.cpp`.
- Default login endpoint: `127.0.0.1:44453`.
- Login request and success-response ordering are represented on both client and server sides.
- Zone authentication sends `ClientIdMsg` immediately after transport establishment.
- Character selection sends `SelectCharacter`, waits for the server scene sequence, answers `CmdStartScene` with `CmdSceneReady`, and completes when a server `CmdSceneReady` is received.
- Client-side field parsers exist for login token, galaxy enumeration/status, character enumeration, permissions, scene start, object create, baselines 3/6, containment, selected chat messages, and character creation responses.

## Limitations and risks

- This is a test/automation client, not the original `Stardust.exe`; empty handlers do not prove that the retail client ignores those messages.
- `ZonePacketHandler` recognizes `DeltasMessage` but does not parse it. It is not sufficient as the replicated-object specification.
- Object creation skips 16 bytes as an opaque quaternion instead of interpreting it.
- Only baseline views 3 and 6 are dispatched by the generic handler; supported per-object fields depend on the small client object hierarchy.
- The client reuses server packet constructors, including a hard-coded zone `ClientIdMsg` version string. The new client must have one explicit configured compatibility version.
- `ClientCore::saveStateToFile` and successful action JSON include the session token. Do not use this output format unchanged. Research tooling must redact credentials and session tokens.
- The build is Linux/Core3-oriented and pulls in the full server engine. It is useful as an oracle and test harness, not as the architectural base of a modern Windows x64 client.

## Recommended use

1. Treat packet constructors and parsers as executable protocol documentation.
2. Build/run it only in the authoritative Debian test environment after confirming the deployed build configuration.
3. Generate sanitized golden transcripts or byte vectors from a controlled local account.
4. Compare the independent x64 codec against those vectors.
5. Never persist passwords, session IDs, or account-specific identifiers in committed or shared artifacts.

Confidence: **Confirmed by repository source** for the behavior described above; **unknown** whether the checked-in target currently builds and passes against the deployed Stardust instance.
