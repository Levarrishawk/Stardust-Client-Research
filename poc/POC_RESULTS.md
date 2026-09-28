# Godot Feasibility Proof Results

Date: 2026-09-11

## Outcome

The bounded host-engine proof passed. Godot 4.7.2 .NET x64 can reference and execute an engine-neutral .NET 8 core, run that core without a render window, present the resulting state in a graphical scene, use the Direct3D 12 Forward+ backend, and terminate deterministically.

This supports continuing the Godot evaluation. It does not establish Stardust protocol or legacy-asset compatibility.

## Verified evidence

- Portable engine: official `Godot_v4.7.2-stable_mono_win64`.
- Download SHA-256: `a2a48473a7414c5f19fab690518caebb738c09ef9601f6bd2388676a7f53b3c0`.
- Host architecture: x64 Godot .NET with .NET 8.
- The engine-neutral project has no Godot package or reference.
- The standalone smoke test accepted a known synthetic message, updated session state and rejected malformed length input.
- Godot headless mode invoked the same core and exited cleanly.
- Graphical verification used `D3D12 12_0 - Forward+` on an NVIDIA GeForce RTX 4070 SUPER.
- The two-frame deterministic render completed without runtime errors.
- Visual evidence: `Stardust.Godot/proof00000001.png`.

## Explicit non-claims

- The synthetic opcode and envelope are not the Stardust/SOE wire protocol.
- No connection to the Stardust server was attempted.
- No TRE archive or SWG asset was loaded.
- No baseline/delta, network-object, terrain, portal, animation or customization behavior was tested.
- C# was selected for the lowest-friction host-boundary test; this does not decide the final native-core implementation language.

## Historical decision impact (2026-09-11)

Godot remains viable and advances to the next spike. The next implementation step should replace the synthetic envelope with a bounded, transcript-driven subset of the recovered SOE/Stardust handshake while keeping all protocol code outside Godot. After that passes, return to deeper `Stardust.exe` analysis before implementing broad gameplay systems.

## 2026-09-27 implementation follow-up

The synthetic envelope has been removed from the active core and Godot host.
Godot 4.7.2 headless mode now runs the engine-neutral offline transcript through
the recovered SessionRequest/SessionResponse, protected reliable
`LoginClientId`, `LoginClientToken`, cluster list/status, and character-list
fixtures. The host printed the six expected transcript events and its PASS line.

With its user-data path redirected to an ignored writable directory, the
managed desktop sandbox emitted only a nonfatal denial while reading the Windows
root-certificate store. That did not affect compilation or the offline
transcript; no network access was attempted.
