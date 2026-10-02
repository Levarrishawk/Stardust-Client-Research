# Original light masks, ranking and bounded slots — October1,2026

Question: which cell light instances can reach baked versus unbaked materials, how are they ranked, and what color channels enter each fixed slot? Stop at direct instruction/caller/constant evidence, owned slot model and independent original/synthetic agreement.

Inputs: SHA-verified Stardust.exe E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 and s207_r.dll D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D; existing156-source configured original scene. Tools: read-only PE/Capstone anchors, prior Ghidra evidence, native owned fixtures and independent Python. No original execution/server contact.

Observation: primary CELL lights clear+0x11D, secondary specular-only lights clear+0x11C; registration maps these to two masks and submission explicitly selects the secondary mask for precalculated vertex lighting. DLL classifier has1 specular-capable+2 diffuse directional and1 specular-capable+4 diffuse point slots. Ranking uses cached absolute weighted RGB intensity, no distance term. Strict ties preserve competition arrival order; specular displacement can reorder diffuse ties. Raw/scaled callback offsets and channel-specific shader uploads are closed. Detailed addresses/consequences in the [durable contract](../../docs/LEGACY_LIGHT_SELECTION_CONTRACT.md).

Changes: independent engine-neutral finite/owned selector; authored-order cell group fixture; static25-anchor/hash verifier and independent insertion-slot verifier. Twelve original and64 synthetic selection cases agree; source/route/native/negative gates pass. No new canonical names or packet rows; weighted research estimates unchanged.

Confidence: Confirmed masks/slots/score/channel upload at this boundary. Live visibility and registration order, external lights/global ambient, mode-byte higher-level semantics, original vertex LIT and exact precision remain Unknown. Historical unsolved light-selection claims narrow to those remaining scopes.

Next: correct baked-room primary diffuse over-lighting, then offer explicit recovered slot selection for unbaked character/doors while preserving source/cache/GPU resource identities and modern attenuation/ambient choices.
