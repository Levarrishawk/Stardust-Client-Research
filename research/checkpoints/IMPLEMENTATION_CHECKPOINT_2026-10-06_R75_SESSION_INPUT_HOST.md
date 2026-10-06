# Round75: offline session input host

Date:2026-10-06. Question: can a borrowed pause/stop adapter honor actual Godot GUI
routing and retire deterministically through callback failure and owner loss, while
preserving the existing default HUD? Stopping condition: independent action/prefix
comparisons, real headless/GPU runs and exact prior default-HUD regression pass.

## Inputs and method

The incoming accepted74 snapshot contains4436 public files. Manifest SHA-256:
6956cc00d990625861fb705822d2be3e98f5f94c6ab827b6964f8a489f6dcc57;
archive SHA-256:4b0ca114aaaebe129d4d433424dd9b1d76f30e4a36875715abb88c025e93290d.
Original input executable SHA-256 was checked before work:
E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150.
No original instructions, VAs or execution are used for this modern policy.

Tools: cached Python, offline .NET8/Godot4.7.2 SDK/packages and Godot .NET x64,
headless and one OpenGL compatibility GPU run. Independently authored Python fixtures
and expected rows are written before native execution. Only accepted frozen orbit
and default-HUD proof sources are staged as dependencies. No unfinished sibling
source or original asset is imported.

[Durable contract](../../docs/TERRAIN_OFFLINE_SESSION_INPUT_HOST_CONTRACT.md),
[host](../../poc/Stardust.Godot/TerrainOfflineSessionInputHost.cs),
[verification](../../poc/Stardust.Godot/TerrainOfflineSessionInputHostVerification.cs),
[runner](../../poc/round75-tests/Rendering/Invoke-Picker.ps1),
[oracle](../../poc/round75-tests/Rendering/verify_picker.py).

## Observations, conclusions and consequences

- Actual15 viewport actions produce two P toggles, one Escape finish, two accepted
  focused-GUI keys and no callback for echo/release/unrelated/mouse/repeated finish.
  Confirmed. Logical action mapping belongs to modern caller policy.
- Sixteen exact negative rows cover source/pause/finish exceptions, caught reentry,
  reentrant Dispose, initial and sink cancellation, owner/host exit, owner deletion
  during a sink, wrong-owner/configuration retry and off-thread dispatch. Prefixes
  match the independent model. Confirmed. Sink effects cannot be rolled back.
- A source exception through actual viewport input is caught at _UnhandledInput and
  preserves a faulted event diagnostic; the borrowed owner survives. Confirmed.
- First native run failed because base Godot Dispose did not remove the own input
  node. Explicit own-node Free first corrected the concrete Dispose path. Lead
  integration then exposed recursive Free from the native disposal callback.
  Reopening with detached raw Free reproduced process failure at teardown begin.
  Public concrete/IDisposable Dispose now owns Free; protected native Dispose
  only clears references and calls base. Final runs prove five independent
  teardown outcomes: detached/attached raw Free, idle parent Free/QueueFree and
  interface Dispose, repeated disposal, parent survival and complete native exit
  without leak/resource-in-use diagnostics. Confirmed.
- Opt-in HUD title Movement controls and P/Escape captions fit the inspected640x480
  image. Default API/captions retain the old116-check proof, eight exact captions,
 21 negatives and actual input pass-through. Confirmed for these bounded fixtures.

Final reopened native counts:74 headless and75 GPU. Offline build:zero warnings/errors.
Eight runner guard probes reject incorrect/empty executable identity, public/existing
outputs, live/dangling links and nondirectory ancestors before build; prior receipt
and linked target are preserved. Scope audit verifies4435 unchanged frozen files,
one owned HUD change and ten new public files, empty index and preserved HEAD.
Workspace validator, PowerShell/Python/XML syntax, relative links and whitespace
checks pass. Logs retain the offline certificate-store diagnostic with successful
exit; it is not claimed as a clean environmental diagnostic.

## Reproduction and resume

Run the Rendering Invoke-Picker.ps1 with cached Python, dotnet, original executable
identity input and accepted offline package cache, passing a fresh ignored RunRoot.
It verifies incoming frozen inputs, emits independent fixtures before build, executes
new headless/GPU fixtures and the frozen default HUD proof, then compares receipts.
The original executable is read only for SHA-256. Private captures, detailed logs and
handoff remain ignored; curated documents neither embed nor link to private outputs.

The controller/packet/world join is the lead's next bounded probe: actual P at5/8
and Escape at600 with unchanged route and independently checked counters. This worker
establishes no session-controller behavior, original key parity, timer authority,
network interoperability or aggregate coverage improvement. Counter overflow,
hardware keyboard layouts, arbitrary GUI arrangements and resize policy remain open.
