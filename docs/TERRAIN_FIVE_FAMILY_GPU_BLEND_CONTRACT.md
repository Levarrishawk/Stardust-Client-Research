# Modern five-family GPU blend boundary

The [engine-neutral attributes](TERRAIN_MODERN_FIVE_FAMILY_BLEND_CONTRACT.md)
have an explicit Godot rendering adapter. This is modern linear interpolation
of the [partial original family labels](TERRAIN_SURFACE_PREVIEW_CONTRACT.md),
not recovered ALP selection, original shader execution or complete traversal.

## Adapter and ownership

[TerrainPatchBlendMaterialBinding](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainPatchBlendMaterialBinding.cs)
accepts exactly five supplied material/texture entries keyed3,5,6,8,9:

```csharp
var binding = new TerrainPatchBlendMaterialBinding(materials, textures);
var weights = TerrainModernFiveFamilyBlend.Bind(geometry, nineFamilyKeys);
var visual = new TerrainPatchVisual(weights, parent, binding);
visual.VerifyReadback();
```

The binding owns one shader/material pair, borrows the textures and tracks
live borrowing visuals. Dispose every visual before the binding, then dispose
the host-owned textures. Attempting to dispose the binding with live borrowers
rejects without changing its state. Disposal is owner-thread-only and idempotent.

`TerrainPatchRenderPolicy.ModernFiveFamilyVertexLinearBlend` is appended as
value4. Existing policies retain their values and constructors. The separate
diagnostic value5 is unshaded and intended for the synthetic proof.

Weights upload as CUSTOM0, four full binary32 components per vertex. The pinned
GodotSharp4.7.2 XML describes `ArrayType.Custom0` as `float[]` for
`ArrayCustomFormat.RgbaFloat`; that format is shifted by
`ArrayFormat.FormatCustom0Shift` in the surface flags. Existing COLOR, base UV,
UV2, indices, positions and normals keep their existing adapter paths. Readback
checks all36 custom component bits, the surface format, the exact shared
material, all five texture identities and numeric diffuse uniforms.

The fragment formula is:

```text
weights = interpolated CUSTOM0
fifth = 1 - sum(weights)
rgb = sum(familyWeight * sampledFamilyTexture.rgb * familyDiffuse.rgb)
ALBEDO = rgb * interpolatedVertexDiffuse.rgb
```

The fixed slots are3,5,6,8, with9 the remainder. Production textures retain
their declared sRGB sampling hints and use anisotropic mip filtering/repeat.
The synthetic diagnostic uses linear solid-color textures and nearest filtering;
the weight and modulation formula is identical. There is no ALP sampling,
label smoothing, priority reconstruction, geometry change or child RNG.

Numeric diffuse multipliers upload as `Godot.Vector4`. An early GPU candidate
uploaded `Godot.Color`, which received engine color-space treatment and failed
the independent non-white multiplier fixture. The correction is confined to
this new binding. The existing original-region host loads every family under
`TextureTimesVertexColor`, whose `DiffuseMultiplier` is always `(1,1,1,1)`;
its accepted center-material path is unaffected and remains unchanged.

Shared-edge continuity requires identical endpoint labels/attributes and texture
coordinates agreeing modulo repeat. The current integral scale1 tiles satisfy
the texture-phase requirement. General nonintegral UV scale, stitched topology,
mixed-resolution transitions and a wider family set are not admitted claims.
Road edges may become softer across the5m sample spacing. The production
shader samples five textures per fragment, so large-window performance remains
a separate host acceptance gate.

## Focused verification

The standalone
[scene](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainFiveFamilyBlendVerification.tscn) and
[harness](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainFiveFamilyBlendVerification.cs) require no
original assets. They create five pure-family patches and two adjacent patches
at negative X whose shared lower edge varies between family3 and9.

Actual x64/DX12 Forward+ on RTX4070 SUPER passes36 checks and7 rejection cases:
five pure-family pixels, nine quarter/midpoint probes across both sides of the
shared edge,63 uploaded vertices,252 exact custom component bits and three
identical shared-edge vertex attributes. Maximum RGB error is0.0018979907
against independent linear blending plus the sRGB display transfer, below the
declared0.015 tolerance. The probes use their actual pixel-center world points,
which lie close to the requested quarter/midpoint coordinates. Cleanup balances
seven patch allocations/disposals and one shared binding allocation/disposal.
Headless passes the attribute/ownership checks without claiming GPU pixels.

Run the scene with the pinned local Godot executable, project path
`poc/Stardust.Godot`, `--rendering-driver d3d12 --rendering-method forward_plus`,
and optional `-- --receipt <fresh-private-json-path>`. It writes a private
receipt/capture and prints renderer admission plus cleanup counters.

The [independent verifier](../../SWG-Godot-Client/tools/python/verify_terrain_five_family_blend_gpu.py)
recomputes all14 pixel expectations, cross-edge continuity and six tamper
rejections from the receipt and stdout, and binds all14 pixels to the adjacent
PNG capture:

```powershell
python tools/python/verify_terrain_five_family_blend_gpu.py --receipt <private-json> --stdout <private-stdout>
```

Original-resource texture association, actual lit presentation and streaming
cost are parent-host acceptance responsibilities. Original blending parity is
false throughout; no original executable or live service is used.

The parent integrated opt-in host and self-contained export now pass original
Tusken and streaming acceptance; see the [joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_STREAMED_TERRAIN_MATERIALS.md).
