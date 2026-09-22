# Audio listener and source coordinate closure

Date: 2026-09-20  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The Miles spatial-coordinate adapter performs no hidden handedness or axis remap. Each audio frame, `audio_update_VA_0040E030` obtains the listener object's world transform and extracts:

- position through `0x0040EC20`: transform offsets `+0x0C`, `+0x1C`, `+0x2C` -> Miles X, Y, Z;
- forward through `0x0040EC60`: transform K basis offsets `+0x08`, `+0x18`, `+0x28` -> Miles forward X, Y, Z;
- up through `0x0040EC40`: transform J basis offsets `+0x04`, `+0x14`, `+0x24` -> Miles up X, Y, Z.

Those nine floats are stored in the listener adapter at `0x01907F74` and passed by `audio_listener_commit_VA_0041B780` directly to `AIL_set_3D_position` and `AIL_set_3D_orientation`. Listener velocity is explicitly set to zero on this path.

Spatial voices use the same convention. During 3D voice creation, `0x0040F590` calls the sound object's position getter `0x0041B290` and passes its returned three floats directly to `AIL_set_3D_position`; velocity again starts at zero. The generic source-position update `0x0040EC80` likewise forwards a supplied three-float vector without reordering.

The apparent axis issue belongs at the protocol boundary, not the audio boundary: several SWG wire records serialize positions in X/Z/Y order, while the client's internal transform and Miles calls use ordinary X/Y/Z. A replacement must reorder while decoding those wire/file structures, then keep one engine-space convention through scene and audio adapters.

## Replacement requirements

- Convert SWG wire X/Z/Y records into internal X/Y/Z exactly once.
- Derive listener forward from the transform K axis and up from J.
- Pass source/listener X/Y/Z consistently to the chosen engine audio API.
- If Godot or another host uses a different forward convention, implement that as an explicit engine adapter and validate with left/right/front/back fixtures; do not alter protocol values.
- Preserve separate source obstruction, occlusion and distance policy already mapped elsewhere.

## Confidence

- Listener position/basis extraction and Miles argument order: **Confirmed by executable code and imported signatures**.
- Source position pass-through: **Confirmed by executable code**.
- Internal X/Y/Z versus wire X/Z/Y boundary: **Confirmed across movement and audio paths**.

## Stable provisional names

| VA | Name |
|---:|---|
| `0x0040E030` | `audio_update_listener_and_voices_VA_0040E030` |
| `0x0040EC20` | `transform_extract_position_xyz_VA_0040EC20` |
| `0x0040EC40` | `transform_extract_up_j_xyz_VA_0040EC40` |
| `0x0040EC60` | `transform_extract_forward_k_xyz_VA_0040EC60` |
| `0x0041B780` | `audio_listener_commit_VA_0041B780` |
