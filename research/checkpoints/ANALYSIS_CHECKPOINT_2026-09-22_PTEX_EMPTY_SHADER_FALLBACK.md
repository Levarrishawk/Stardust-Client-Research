# Enabled PTEX records with empty shader names

Date: 2026-09-22. Target `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

The corrected CEF→particle closure reaches **1,891** PTEX shader slots. **87** carry an empty authored shader name. All 87 have PTEX's shader-enable byte set to 1; they are not the explicit disabled-PTEX case. The installed full particle-texture inventory has 331 enabled empty names and one disabled empty name, so this is a common valid asset pattern.

`0x007A15C0`, the quad local-shader-primitive constructor, tests the PTEX enable byte and then calls `0x00AA2E10` on the shader-name object. That helper returns true for null or empty C strings. For an enabled empty name, the constructor **skips named shader fetch** and leaves primitive material pointer `+0x30` null. The primitive's virtual shader accessor at `0x007A1760` returns `0x0075AF00` when this pointer is null (or when the readiness test rejects it). `0x0075AF00` returns global `0x01922F94`, which is initialized by `0x0075AD80` from `shader/3d_vertexcolorac.sht`. This fallback shader is live in the configured overlay, winning from `mtg_patch_009_Shader_01.tre` at priority 38. Its `IMPL/0005` and `PASS/0009` versions are within this executable's recognized shader ceilings. **Confirmed by code plus asset inventory.**

Therefore an enabled PTEX with an empty name is **not** an absent-file dependency and does not imply an invisible quad; it deliberately falls back to the vertex-color material at the primitive shader-selection boundary. This does not assert every one of those 87 particle emitters visibly renders in normal play, only that the shader accessor has a concrete fallback. In a replacement renderer, model this as a defined material identity, distinct from missing named-shader error handling and from PTEX's explicit disabled state.

This closes the empty-slot caveat in `ANALYSIS_CHECKPOINT_2026-09-22_CEF_PARTICLE_SHADER_CLOSURE.md`. No network/protocol rows change.
