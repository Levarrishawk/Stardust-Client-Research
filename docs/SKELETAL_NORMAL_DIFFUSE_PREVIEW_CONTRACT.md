# Normal mapped diffuse preview and live detail choices

## Explicit adapter policy

The engine-neutral compressed-normal decoder remains the compatibility boundary. Prototype host preview shading uses its recovered alpha/green and absolute-radicand reconstruction with explicit diagnostic ambient0.35, directional0.75 and world light normalize(0.4,0.6,-0.7). This is a diffuse inspection policy; original light selection, specular, bloom and fog remain unimplemented.

For normal-enabled materials, the adapter uploads authored tangent mappings after shape application and skinning. It reflects Z and negates tangent W. Because prototype host packs an orthonormal basis, it normalizes N, projects T away from N and normalizes T. Zero directions and handedness other than±1 reject. Core raw weighted directions are retained. GPU readback tolerance is0.003 for tangent XYZ, exact W; normal tolerance remains0.002. These are adapter packing tolerances, not legacy shader bit-identity claims.

Normal sampling uses raw DDS channels, linear filtering and mipmaps before decoding. Tangent-space vectors are transformed through prototype host view T/B/N, then into world space. Materials without CNRM use geometric normals. Albedo hue factors and the existing display transfer adapter remain applied after sampling.

## Live age details

Strict version1 profiles explicitly select the private index_age variable. Authored examples choose0 or3; neither is asserted to be an original character default. Four owned wrinkle maps are available before GPU publication. `CreatureNormalDiffuse` requires albedo factors; `CreatureTextureProfile` requires that normal preview.

Live detail edits recompute serialized bindings from owned original material descriptions, validate mesh/selection/reference identities and bind the selected CNRM texture on retained materials. No original bytes change and no archive is reopened. Color revisions preserve current independent detail selection. Failure restores previous bindings; invalid prepared references reject before publication. Reset restores the loaded explicit profile.

## Verification and limits

Original object→APT→SAT→LMG/MGN and material/face/program chains load79 unique sources/6,982,490 bytes. Original body/head30 sparse targets and27 controls remain available. GPU cache14 textures/18,786,972 decoded uploaded bytes includes the generated face; this is not physical VRAM measurement.

Three GPU fixtures cover both tangent signs and a rotated object basis. Maximum diffuse/display channel error0.00137 at2/255 tolerance. Original sampled idle480 ticks, all27 shape controls, rapid/repeated color edits, all four detail choices, reset, invalid detail bindings and color/detail interaction pass. Headless and first-lookup cancellation pass. The complete native union independently agrees with raw face/material/shape/normal/animation audits. No original executable was run.

[Compressed-normal specification](SKELETAL_COMPRESSED_NORMAL_CONTRACT.md), [texture choices](SKELETAL_TEXTURE_CHOICE_CONTRACT.md), [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_NORMAL_DIFFUSE_PREVIEW.md).

## Interior integration

`SceneCharacterNormalDiffuse` and `SceneCharacterTextureProfile` use the same owned closure and retained detail controls. Reader options reject unsupported combinations before lookup. Shared union144 sources/16,596,575 bytes,2,880 native route ticks/24 transfers/182 contacts and independent normal/shape/face/floor audits agree. GPU routes/reflected owner/door guard/idle/control/reset/cancellation pass. Each edit preserves actor state/root and current pose; asynchronous color waits compare stable identities while idle continues. [Interior checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_NORMAL_DIFFUSE.md).

Next: recover selected specular material/constants and program equations before adding further material fidelity.
