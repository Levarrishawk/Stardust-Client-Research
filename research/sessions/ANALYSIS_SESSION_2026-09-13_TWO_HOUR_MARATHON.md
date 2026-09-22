# Stardust.exe Static-Analysis Marathon — 2026-09-13

## Scope and provenance

- Authoritative binary: `C:\Stardust-DEV\Stardust.exe`
- SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
- Method: static executable and asset-corpus analysis, correlated with the local Stardust server source.
- No replacement-client implementation was begun.

## Completed analysis blocks

1. Closed the modern EMTR flocking/cage field layout and corrected the 0012-0014 version boundary.
2. Reconstructed common EMTR collision, wind-resistance, and local-space controls.
3. Reconstructed CPU quad-particle expansion and its dynamic triangle-list render handoff.
4. Surveyed all deployed PTMH particle-mesh resources and determined the applicable appearance/tint policy.
5. Built a server-source message-class gap matrix and closed `ChatSystemMessage` end to end.
6. Reconstructed `PlayClientEffectObjectMessage`, `PlayClientEffectObjectTransformMessage`, and `PlayClientEffectLocMessage`.
7. Reconstructed `CreateClientPathMessage` and `DestroyClientPathMessage`.

## High-value findings

- EMTR 0012-0014 move the flocking-mode dword four bytes earlier than previously assumed. The corrected scanner and regenerated corpus tables supersede the earlier scalar export.
- The active flocking fields, cage dimensions and behavior, terrain/environment color influence, collision controls, local-space switch, and wind-resistance control now have stable offsets and evidence-backed semantics.
- Two EMTR floats at `+0x20c/+0x210` remain serialized but dormant in the observed executable and corpus. They must be preserved as opaque compatibility data rather than assigned invented semantics.
- The quad-particle path expands each live particle into six CPU vertices. This behavior can be represented with GPU instancing in a modern renderer while preserving sampling, UV animation, color, local-space behavior, two-sided rendering, and ordering.
- The deployed PTMH corpus contains 399 references: 275 APT, 79 MSH, and 45 SAT. A replacement must route PTMH through a general appearance factory; it cannot assume mesh-only or APT-only resources.
- PTMH mesh appearances do not consume the PTCL quad color/alpha channels in the observed path; their own appearance/material behavior governs rendering.
- `ChatSystemMessage` is now mapped from opcode through parsing, presentation construction, and UI publication.
- Both object-attached and location-based client-effect messages are mapped. The server's optional trailing label on the location form is not consumed by this client revision and must be handled explicitly by a compatible replacement parser.
- Client-path create/destroy messages are a retained debug-visualization facility, not navigation authority.

## Progress assessment

- Weighted whole-client research coverage: **77%**.
- Replacement-decision readiness: **93%**.

These are evidence-weighted planning estimates, not claims that 77% of machine-code functions have individually been named. The remaining work is concentrated in unresolved message families, derived baseline/delta paths, UI/auction behavior, and lower-priority leaf functions.

## Remaining priority queue

1. Refine the server message matrix by separating true standalone packets from structural helpers, callbacks, and baseline/delta subclasses.
2. Trace the 60 remaining literal-opcode candidates (58 S2C and 2 C2S), prioritizing auction, chat, UI, player, ship, and zone families.
3. Continue derived object-baseline/delta reconstruction and connect each field mutation to the client object model.
4. Resolve the remaining EMTR range-policy pairs at `+0x23c/+0x240` and `+0x244/+0x248` if an active consumer can be proven.
5. Continue asset-format closure beyond particle systems, especially appearance/template composition and animation/terrain dependencies.

## Confidence policy

- **Confirmed:** direct code/data-flow evidence or exact client/server serialization agreement.
- **Strongly inferred:** multiple converging observations with no contradictory path.
- **Tentative:** plausible interpretation with an unresolved consumer or incomplete path.
- **Unknown:** preserved without assigning semantics.
