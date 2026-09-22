# Terrain collision policy and tolerance closure

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
Scope: `0x0084BDF0`, `0x00860F60`, `0x00860FF0`, `0x00861140`, `0x008662E0`, `0x00866550`

## Result

The active procedural-terrain triangle collision path is unconditionally one-sided. The apparent collision-parameter argument is forwarded by the appearance-level traversal but is not read by `ClientChunk` or its terrain ShaderSet collision routine. No movement/camera caller can request two-sided behavior through this path in this executable.

For each selected stitched triangle, `terrain_shader_set_collide_segment_VA_008662E0` computes:

```text
dot(segmentEnd - segmentStart, triangleNormal)
```

and admits the triangle only when that value is strictly less than zero. The comparison constant at `0x015DC6A0` is exact IEEE-754 `0.0f`; it is not an epsilon. Thus only a segment moving against the stored triangle normal reaches the segment-plane and point-in-triangle tests. Coplanar and back-facing travel is rejected.

The routine then keeps the nearest accepted point relative to the segment start and writes:

| Result offset | Confirmed value |
|---:|---|
| `+0x04..+0x0C` | hit point X/Y/Z |
| `+0x10..+0x18` | stored triangle normal X/Y/Z |
| `+0x1C` | Euclidean distance from segment start to final nearest hit |

The result point is initialized to the input segment endpoint by `terrain_appearance_collide_VA_0084BDF0`, providing the initial nearest-distance bound.

## Height-query tolerance

The vertical height and height/normal methods construct their query segment from:

```text
(x, chunkMaximumY + 0.1f, z)
to
(x, chunkMinimumY - 0.1f, z)
```

The constant at `0x015DD53C` is exact `0.1f`. This is a vertical extent guard so triangles lying precisely at the recorded chunk bounds remain intersectable. It is not a lateral LOD-seam tolerance.

`terrain_shader_set_query_vertical_surface_VA_00866550` uses the currently selected stitched index list and strict X/Z triangle half-plane tests against zero. No separate seam epsilon or cross-chunk blending tolerance was found in this query. Continuity comes from using the same selected seam topology as rendering.

## Confidence

- **Confirmed:** one-sided acceptance is fixed as `dot(direction, normal) < 0.0f`.
- **Confirmed:** the forwarded collision-parameter object is ignored below the terrain appearance traversal.
- **Confirmed:** hit-point, normal, and distance output offsets.
- **Confirmed:** vertical query extends chunk Y bounds by exactly 0.1 on each side.
- **Confirmed:** collision and height lookup both use the active stitched terrain topology.
- **Unknown:** original public C++ type names and any unused fields preceding the confirmed result payload. These names are not needed to reproduce behavior.

## Replacement-client consequence

Implement legacy-compatible terrain queries as front-face-only against the current seam topology. An engine-native physics API must be configured or filtered to preserve this policy; enabling double-sided heightfield collision by default would change camera/movement behavior. Use the exact 0.1 vertical extent expansion for compatibility tests, but do not treat it as a general positional epsilon.
