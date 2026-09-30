# Original static mesh Godot preview — 2026-09-29

## Question and scope

Can Godot render the verified original static mesh and DDS chain while preserving the coordinate, UV, index and mip boundaries? Stop after an actual GPU render, independent upload checks and deterministic resource cleanup. This is an offline unlit asset preview, not a world/client completeness claim.

## Inputs and adapter

Uses the exact mesh/configuration/effect identities in the [material graph checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-29_MATERIAL_GRAPH.md) and the [DDS contract](../../docs/DDS_DECODER_CONTRACT.md). Godot 4.7.2 .NET x64 receives an owned engine-neutral bundle. `StaticMeshPreview` reflects Z on positions/normals once, reverses each triangle's winding, and preserves UV0. One ArrayMesh surface represents each original batch. Nine owned textures receive complete RGBA8 mip chains; all 80 mip byte sequences are compared after upload/readback against CPU data. Positions, normals, indices and UVs are also compared.

The explicit preview policy is opaque, unlit MAIN/UV0 sampling with repeating linear mip sampling. It requires the observed single opaque pass and fixed/programmable texture bindings; unsupported bindings fail. Unspecified legacy color space is treated as sRGB for presentation; DX10 Linear omits the color-source hint. Premultiplied alpha is retained by CPU decoding but rejected by this preview. No original shader program is translated or executed.

## Observations and confidence

Confirmed on this host: OpenGL 3.3, NVIDIA RTX 4070 SUPER, driver 591.86. The original slice renders 9 batches, 190 vertices, 159 triangles, 9 textures. A 64×64 asymmetric four-color GPU fixture validates UV corner orientation and the reflected front face with back-face culling. A private 1280×720 capture shows the original textured building section. Actual GPU gates pass; headless dummy rendering is used only for resource/host checks. The existing root-certificate-store diagnostic remains, with successful exit 0.

`PreviewOrbitCamera` supports drag orbit, scroll zoom and R reset, framed from mesh bounds. `_ExitTree` disposes adapter-owned mesh, textures, shaders and materials; constructor failure also cleans up. CPU input remains unmodified. `Invoke-StardustAssetPreview.ps1` launches hidden bounded self-tests by default, accepts explicit identity/path arguments, and restricts capture output to ignored private storage. Interactive mode requires its explicit switch. No original executable or socket is run.

## Reproduction

Build the Godot project in Debug. Invoke `tools/powershell/Invoke-StardustAssetPreview.ps1` with `-GodotPath`, `-ArchiveRoot`, `-Configuration`, `-Member` and `-ExpectedSha256`, using identities above. `-Headless` runs the resource gate; omit it for the GPU gate. `-CapturePath` must name a fresh private PNG. `-Interactive` opens the inspector with orbit controls. The launcher enforces a 45-second deadline for noninteractive runs and checks process/capture results.

## Remaining uncertainty and next action

The visible object is one L4 mesh section, not its full parent appearance or a world scene. Original material lighting, shader constants, LOD selection, other vertex declarations/topologies, animation and full texture-format coverage remain open. Next bounded asset milestone: parent appearance/LOD loading with two verified detail levels, preserving explicit resource identities and adapter cleanup. Modern compressed-format work can separately add BC7 with independent mode fixtures; BC6H needs an HDR output contract.
