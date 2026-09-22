# Quad-particle CPU batch and renderer handoff

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Legacy path

`particle_quad_prepare_cpu_vertex_batch_VA_007A2660` walks the emitter's live particle pointer vector, evaluates PTCL/PTQD channels at normalized age, selects the PTEX frame, computes two oriented extent vectors, and expands each particle into six vertices (two triangles). It allocates exactly `particleCount * 6` dynamic vertices.

The generated vertex declaration is accessed generically through signed-byte offsets and stride. For each of the six vertices the routine writes:

- a float3 position;
- one packed color value shared by the quad;
- a float2 UV selected from the PTEX rectangle.

Positions are the four combinations `center ± extentA ± extentB`, repeated in triangle-list order. One additional normalized direction/corner vector is calculated for the declared layout before emission. PTQD's primary and secondary size curves scale the two extents; its lock flag reuses the primary result for both. If `EMTR +0x1E4` is set, particle-local centers are transformed through the current owner transform before expansion.

After filling the dynamic buffer the routine unlocks/commits it through the shared graphics API. The local shader primitive draw callback checks that the emitter has drawable content, temporarily sets raster culling to none, submits the prepared triangle list, then restores the normal cull mode. Its shader is resolved from PTEX during primitive construction.

## Modern design contract

The six-vertex CPU expansion is observed legacy behavior, not a compatibility requirement. A modern renderer may publish one instance per particle containing center, two extents or orientation/size, packed color, UV rectangle and material identity, then generate corners in the vertex shader. Required visible behavior is:

1. waveform and color sampling use normalized particle age;
2. environment color influence is applied before color packing;
3. PTEX frame selection preserves legacy inclusive range/wrap behavior;
4. local-space particles follow the owner transform;
5. quads are rendered two-sided;
6. material/shader grouping must not reorder effects in ways that violate transparency ordering.

This cleanly separates asset/protocol compatibility from the D3D9 dynamic-buffer implementation and is directly representable in Godot, DX12, or Vulkan.

