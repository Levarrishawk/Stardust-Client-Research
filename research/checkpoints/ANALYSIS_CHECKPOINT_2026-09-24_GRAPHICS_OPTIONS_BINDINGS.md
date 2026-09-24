# Analysis checkpoint — graphics-options UI bindings

Question A09 / W10+W07. Target: verified original `C:\Stardust-DEV\Stardust.exe`; function text is reconstructed pseudocode. **Verification/refinement, not new mediator breadth.** An initial exact-name/address search missed the existing [options-family checkpoint](ANALYSIS_CHECKPOINT_2026-09-22_OPTIONS_PREFERENCES_ARCHITECTURE.md), which already listed constructor `0x00EA4B60`, the binding model, live preview, Cancel rollback and persistence. This follow-up only adds exact graphics control ranges, callback addresses and capability-gated widgets.

RTTI `SwgCuiOptGraphics` identifies vtables `0x015F4B34` / `0x015F4AAC`; their constructor reference at `0x00EA4B60` binds declarative controls on the page. The screen uses generic option-binding helpers `0x00FF1300`/`0x00FF09A0` for sliders and `0x00FF0B80` for check/radio controls, each supplied with current-value, mutation/default callbacks. The bindings refresh initial widget state, register UI callbacks and retain them in the options-base collection. This is a local settings path; no Stardust server opcode or command was observed in this constructor.

Representative direct bindings (the strings and callback addresses come from `0x00EA4B60`):

| Page control | Bound domain / observed limits | Function evidence |
|---|---|---|
| `brightnessSlider`, `contrastSlider`, `gammaSlider` | image adjustment; each 0.5–1.5 | getter/mutation/default triples beginning `0x004261B0`, `0x00426230`, `0x004262B0` |
| `sliderFov` | camera FOV; 60–120 in the UI binding | `0x00518C80`, `0x00518C70`, `0x00503360` |
| `sliderFarPlane` | far plane; 512–2048 | `0x00518D20`, `0x00518D00`, `0x00503340` |
| `sliderStaticLodBias`, `sliderCharacterLodBias` | static/character LOD bias; 0.5–2 | `0x00787320`/`0x007C7790` callback families |
| `sliderParticleLodBias` | particle LOD; 0.25–1 | `0x00EA4AD0` family |
| `sliderNebulaDensity` | space nebula density; 30–120 | `0x00EA4B30`, `0x00911A90`, `0x00911AB0` |
| `sliderWorldDetailBias`, `sliderShadowDetail` | world detail/shadow detail; 0–1 | `0x0059DC10`, `0x00786610` families |
| render/detail/shadow/camera toggles | detail objects, shadow enable, character/non-character shadow modes, batch renderer, character LOD manager, simple camera collision, fade objects, cross-fade LOD, bloom | concrete getter/mutation/default callbacks supplied to `0x00FF0B80` |
| screenshot controls | BMP/TGA/JPG radio selection, JPEG quality 1–100 | `0x00EA48A0`–`0x00EA4960`, `0x00755860` families |

The constructor explicitly gates cross-fade detail controls on a device-capability query `0x00754D40` and bloom controls on both that capability path and `0x0064B110`; the exact capability encoding is not yet resolved. Missing required UI `CodeData` controls trigger the existing fatal-configuration path, not a silent fallback. The derived virtual callback at `0x00EA5340` calls the options-base hook then updates multiple enable states from capability checks. The earlier checkpoint already establishes live preview, Cancel rollback and machine-options persistence. Per-setting renderer/device-reset timing still needs selected setter and backend tracing; do not infer a DX12/Vulkan setting policy from these legacy slider ranges.

**Outcome:** exact graphics binding details are refined, but this is **not counted as new UI breadth**. This does not close renderer policy or W10 UI breadth. Confidence: **confirmed by code** for control names, callback addresses and numeric UI limits; **unknown** per-setting device-reset timing. No percentage change.

Evidence: RTTI/vtables above; `0x00EA4B60`, `0x00EA5340`, `0x00FF1300`, `0x00FF09A0`, `0x00FF0B80`, selected getters `0x00426190`, `0x00518C70`, `0x00787330`; read-only decompilation in ignored `local-private/session-2026-09-23-marathon/`.
