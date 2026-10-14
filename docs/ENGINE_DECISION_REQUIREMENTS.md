# Pre-Build Engine Decision Requirements

Current scope — October 14, 2026: this is research documentation. Any implementation choices or acceptance results below are historical evidence. Use [current status](../ANALYSIS_STATUS.md) for coverage and open questions; no engine choice or client development is prescribed here.


## Decision rule

Do not choose an engine from feature lists. Score each candidate using the same four bounded spikes and require evidence artifacts, profiler captures, code-size/complexity notes and identified escape hatches. The historical comparison considered an existing host engine, custom native DX12, and custom native Vulkan. Other engines may be added only if they can be evaluated with the same tests.

## Non-negotiable architecture

The following must remain engine-neutral libraries or interfaces:

- fixed-width SOE transport and application-message codecs;
- session/login/zone state machine;
- 64-bit network-object registry and transactional baseline/delta model;
- TRE overlay virtual filesystem and bounds-checked IFF/data-table codecs;
- typed resource dependency graph and legacy-to-intermediate conversion;
- scene-generation/readiness barrier;
- semantic material, animation and portal/cell descriptions;
- headless tests and structured diagnostics.

No candidate may require network IDs to become engine instance IDs, protocol handlers to mutate engine nodes directly, or asset virtual paths to lose archive-overlay identity.

## Required spikes

### A. Headless protocol ownership

Integrate a fake/server transcript through transport -> named message -> transactional object state without creating a render window. Verify deterministic startup/shutdown, malformed input tests and no engine-frame dependency.

### B. Static material chain

Load `APT -> MSH -> SHT -> EFT -> DDS`, translate one multi-implementation effect into a semantic material, render with correct depth/blend/cull/texture behavior and record what translation remains custom.

### C. Character vertical slice

Load the documented human `SAT -> LMG/MGN -> SKT -> LAT/ANS` chain, compose body/face skeletons, decode/apply customization v1, mount `/shared_owner`, attach and detach one mapped wearable, and execute one selector/blend/state transition.

### D. World vertical slice

Load a PTAT/TGEN terrain chunk and `appearance/yt1300.pob` with its seven cell meshes/floors. Demonstrate exterior/cell camera movement, portal visibility, door transform, floor collision/path continuity, background preparation and scene-generation cancellation.

## Weighted scorecard

| Area | Weight | Evidence required |
|---|---:|---|
| Protocol/headless ownership | 15 | transcript tests and clean teardown |
| Legacy resource integration | 20 | all three asset spikes and dependency invalidation |
| Character/customization/animation | 15 | spike C correctness and extension cost |
| Terrain/cells/large world | 15 | spike D correctness, streaming and precision |
| Renderer/material escape hatches | 10 | spike B plus GPU debugging capture |
| Lifecycle/threading control | 10 | cancellation and stale-completion tests |
| Windows tooling/debugging | 5 | crash/GPU/profiler workflow |
| UI/input/audio productivity | 5 | minimal representative integration |
| Maintenance/licensing/upgrade risk | 5 | written dependency/fork/update assessment |

Any candidate fails regardless of score if it cannot satisfy fixed-width wire compatibility, engine-neutral headless testing, generation-safe scene teardown, custom resource loading or required licensing/redistribution constraints.

## Current evidence-based expectations

| Candidate | Likely advantage | Principal risk | Status |
|---|---|---|---|
| prototype host | Rendering, windowing, UI, input, audio, editor and cross-platform facilities reduce commodity work | SWG resource graph, skeletal customization, terrain/cell streaming and deterministic lifecycle may fight scene/resource assumptions | Must be spiked after this analysis |
| Custom DX12 | Maximum lifecycle/data ownership; best fit for Windows-first HLSL/DXC and debugging; clean semantic renderer boundary | Highest implementation burden for UI, animation, terrain, audio and tools | Leading custom-backend candidate |
| Custom Vulkan | Explicit control plus portable backend potential | More platform/extension/tooling complexity without immediate portability requirement | Choose only if non-Windows becomes concrete |

The analysis does not preselect prototype host or custom DX12. The expected decision hinges on spikes C and D: if prototype host hosts the recovered character/world model without a deep fork or parallel scene graph, it is likely the lower-risk product choice. If those spikes require bypassing core engine systems, retain engine-neutral libraries and use a narrow custom DX12 renderer first.

## Deliverable after spikes

Produce a signed-off matrix with measured implementation effort, engine modifications, unsupported semantics, debugging quality, licensing, performance headroom and a recommendation. Only then authorize the x64 networking POC/build phase; the POC itself remains renderer-independent.
