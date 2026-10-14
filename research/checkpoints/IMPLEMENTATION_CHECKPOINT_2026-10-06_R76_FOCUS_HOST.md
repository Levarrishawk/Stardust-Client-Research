# Round76: offline focus owner

Date:2026-10-06. Question: can exact Godot window focus connections clear gameplay
input, capture/restore prior pause and retire safely without taking ownership of a
session or disturbing other listeners? Stopping condition: independent pre-native
rows/trace/prefixes/teardown, actual headless/GPU, accepted keyboard regression and
scope/workspace validation pass.

## Inputs and reproduction

Accepted75 frozen input:4469 public files. Manifest SHA-256:
873d99dd4dcefb51300e68942dbf31e5bd71d24c593fbd9e2ceb6f3ba497fa97;
archive SHA-256:2887d1c027dff45fcb834e72228359219f52528985e0631470b9734f7926a845.
Original executable SHA-256 rechecked:
E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150.
The original executable is read for identity only; no VAs, asset reads or original
execution are used. Tools are cached Python, offline .NET8/Godot4.7.2 SDK/packages
and Godot .NET x64 headless/OpenGL compatibility GPU. No unfinished76 sibling imports.

[Contract](../../docs/TERRAIN_OFFLINE_FOCUS_HOST_CONTRACT.md),
[host](../../../SWG-Godot-Client/poc/Stardust.Godot/TerrainOfflineFocusHost.cs),
[Godot verification](../../../SWG-Godot-Client/poc/Stardust.Godot/TerrainOfflineFocusHostVerification.cs),
[runner](../../../SWG-Godot-Client/poc/round76-tests/Rendering/Invoke-Picker.ps1),
[oracle](../../../SWG-Godot-Client/poc/round76-tests/Rendering/verify_picker.py).

Invoke the runner with cached Python/dotnet, original executable identity input,
accepted offline package cache and a fresh ignored RunRoot. It validates the frozen
manifest/archive and seven borrowed source hashes, emits independent expectations
before build, runs headless/GPU plus the accepted keyboard proof, and compares results.
Private logs/native rows remain ignored and are not linked from curated documents.

## Observations versus interpretation

- Twenty-two real Window-signal rows across initially paused/unpaused sessions match
  independent expected state and callback order. Repeated loss/gain is idempotent.
  Confirmed. The policy is modern; OS/hardware focus behavior remains unproved.
- Actual accepted manual/run hosts receive viewport W/Shift, clear held masks and
  emit zero intents when disabled; gain enables empty key sets and fresh presses
  work. Confirmed. This establishes input clearing, not simulation pause authority.
- Twenty-two independently modeled failures preserve exact source/gameplay/pause
  prefixes. Gain failure retains one prior committed loss. Source read cancellation,
  sink/lifetime cancellation, caught mutation/disposal reentry, callback owner/window
  deletion, window exit, off-thread and failed admission are covered. Confirmed.
  Partial callback effects cannot be rolled back.
- Close disconnects only own handlers: independent neighboring listeners still fire
  after closure/disposal. Five exact raw/direct/deferred teardown outcomes preserve
  borrowed owner where appropriate, allow repeat Dispose, and finish without native
  leak/resource-in-use diagnostics. Confirmed. Protected native Dispose never Free.
- Accepted75 keyboard source/proof passes74 unchanged checks,15 actions,16 negatives
  and five native teardown cases. Confirmed; no existing public file is changed.

Final:81 headless/82 GPU checks;22 focus rows/22 exact failure prefixes/five teardown
outcomes; zero build warnings/errors. Eight executable/output/link guard probes,
PowerShell/Python/XML syntax, Markdown links, focused whitespace/diff checks and
workspace validator pass. All4469 frozen files remain byte-identical; ten new owned
public files; preserved HEAD and empty index. Offline certificate-store diagnostic
remains with exit0; no network request is part of the proof.

## Consequences and resume

Configure captures exact owner/parent/viewport/window and retains its lifetime token.
Caller owns initial focus admission, fresh input clearing, actual pause state and
teardown sequencing. Close has no restore or gameplay callback; captured primitive
history remains readable. No controller, world, clock, packet, movement or HUD is owned.

Next bounded probe belongs to lead: idle-start preview loss/gain with held W/Shift,
prior pause both ways and session/packet clocks that do not advance while suspended.
Initial OS-unfocused startup, arbitrary listener order, hardware notification timing,
original focus parity and counter overflow remain open. This milestone makes no
aggregate research, protocol or function-coverage claim.
