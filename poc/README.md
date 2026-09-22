# Stardust Godot Feasibility Proof

This bounded proof evaluates whether Godot can host an engine-neutral Stardust client core. It is not a replacement client and does not contact a live server.

## Boundaries

- `Stardust.Core` contains no Godot references.
- `Stardust.Core.SmokeTests` validates lifecycle, packet length checks and deterministic rejection of malformed data.
- `Stardust.Godot` hosts the same proof path in headless and graphical modes.
- The message envelope is deliberately synthetic. It must not be mistaken for a recovered Stardust wire format.

## Pass criteria

1. Both projects compile for x64-capable .NET 8.
2. The standalone core accepts the known synthetic response and rejects malformed input.
3. Godot invokes the same core without a render window or frame-dependent state.
4. Godot graphical mode presents resulting state without owning it.
5. Startup and shutdown finish without errors.

Run the verified headless path with:

```powershell
& '..\toolchain\godot-4.7.2-dotnet\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path .\Stardust.Godot -- --self-test
```
