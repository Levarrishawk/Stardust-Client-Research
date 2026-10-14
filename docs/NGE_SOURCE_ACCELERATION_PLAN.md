# Stardust implementation acceleration plan

Mos Taike increment, October 12: the bounded district (external historical reference) now joins original static placement and buildings with terrain and animated movement. NGE snapshot WXYZ framing, template inheritance and terrain traversal/road bounds supplied concrete leads reconciled with original corpus bytes and existing Pre-CU contracts. Independent placement, height-scope and five foundation checks accompany actual DX12 visual review. This advances M3/M4/M5's outdoor presentation only: full numeric/material parity, interiors, wider terrain and live authority remain separate gates.

Progress on October 12: the the separate implementation record is accepted with a self-contained export, real D3D12 device verification and advancing original animation. The 22 existing default regression suites also pass on this workstation. A subsequent terrain roaming build (external historical reference) joins original idle/walk/run with a 256 × 256 m partial Tatooine base/detail generator; eight exported checks pass. NGE height-order and base-material observations accelerated this bounded composition. M0's wider feature inventory, M1's broader performance workload and complete terrain traversal are not claimed complete. The starting-position observations below describe the pre-change baseline.

October 12, 2026. Target: a Windows x64 Direct3D 12 client interoperating with Stardust-3 and consuming the expected Pre-CU installation. The user prioritizes the fastest route to completion over a particular engine. Retain the existing engine-neutral core and prototype host host; use the NGE source to resolve concrete implementation gaps. Replace the host only after a measured blocking limitation.

This is the current implementation planning direction. It does not authorize live contact, original executable execution, publication, or a source-code transplant. The accepted implementation baseline remains [Round93](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_93_2026-10-06.md); new milestones below are planned, not completed.

## Starting position

The milestone record (external historical reference) establishes an offline joined login, selection, zone, original-human presentation, bounded movement and disconnect path. The existing core already supplies transport, codecs, resource readers and lifecycle ownership. Preserve these components and their independent fixtures. Live Stardust-3 interoperability, general avatar support, nearby actors, full terrain and gameplay breadth remain open.

The [existing architecture](64BIT_PORT_PLAN.md) targets prototype host with Direct3D 12, but the checked-in project configuration (external historical reference) selects `gl_compatibility`. That observation is Confirmed for the file, not for every historical launch override. Existing GPU results do not by themselves prove a DX12 release. Explicit backend and process-architecture verification are release gates.

Prototype host supports Direct3D 12 through its RenderingDevice renderers; use a verified Forward+ configuration for the first spike. See the official renderer overview (external historical reference). Pin the actual available engine build and deployment dependencies during reproduction rather than assuming a version named in historical reports is installed here.

## Architecture and scope

Keep protocol, replication, world identity, assets, terrain evaluation, animation semantics and simulation in `Stardust.Core`. Prototype host owns presentation, windowing, input and audio adapters. Preserve SWG coordinate conventions internally and apply the already documented conversion once at the host boundary. Keep fixed-width wire and asset fields unchanged when running x64.

Use NGE source as an explanatory reference. Write implementation from independently authored compatibility contracts and fixtures. An NGE implementation proves what that source does; matching Pre-CU executable, corpus and server evidence determines what belongs in this client. A generic prototype host animation or terrain feature must not silently replace required SWG semantics.

First playable scope: existing-character login, populated outdoor scene, terrain and interiors, supported avatar appearance, authoritative movement, targeting, chat, inventory/equipment and one Pre-CU combat interaction. This is an intermediate milestone. Release scope additionally needs an explicit feature matrix for character creation, professions/skills, crafting, harvesting, trade, vendors/auction, missions, groups, housing, vehicles and any Stardust-3-specific or space features actually required. Deferred features must be visible in that matrix; a narrow playable slice is not a complete replacement.

## Source evidence workflow

Pin `client-tools` commit `949451032647e45e42c3aaef3f41b132c8af36e3`, inspected October 12. Its README reports post-release refactoring and a Win32 build. The source tree is a reference input, not an assumed x64-ready replacement.

For each bounded implementation ticket, record the existing blocker, NGE path/symbol/revision, Pre-CU contract or hash-verified executable evidence, relevant original corpus/server occurrence, differences, confidence, implementation consequence and acceptance fixture. Classify behavior as verified compatible, changed, newer-only, or unresolved. Do not assign Pre-CU names solely from similar NGE symbols. Confirm the documented executable hash before new address-based comparison.

Stop source exploration when the ticket's acceptance criteria can be implemented and checked. If compatibility cannot be established, retain the unresolved behavior explicitly and choose another ticket that can advance independently. Keep source-derived bulk output private; promote only compact evidence and independently authored specifications. Direct source reuse is a separate provenance/licensing decision and is not assumed by this plan.

## Milestones and dependencies

| Milestone | Work and NGE contribution | Depends on | Exit evidence |
|---|---|---|---|
| M0 Reproduce and prioritize | Reproduce the accepted regression baseline on this workstation. Inventory tools/private inputs. Pin client, source and server revisions. Build a bounded feature/backlog matrix from existing contracts. | None | Reproduction receipt identifies passes, failures and missing inputs; every first-wave ticket has a specific blocker and oracle. No historical pass is represented as a new run. |
| M1 Verify x64 and DX12 | Configure an explicit DX12 host profile. Replay the existing textured animated human/interior scene, then a representative terrain/material workload. Audit legacy material semantics using clientGraphics source. | M0 | Packaged Windows process is x64; runtime confirms D3D12 without silent fallback. Shader, alpha, depth, skinning and coordinate checks pass; resize, shutdown and resource retirement pass. Record GPU/driver/build and timing/memory measurements. |
| M2 Establish real interoperability | Use the existing controlled bootstrap with the exact deployed Stardust-3 version/configuration. Resolve mismatches against existing wire evidence and server source; consult NGE only for targeted lifecycle questions. | M0 and operator prerequisites | Separately authorized bounded attempt proves login, approved character selection, scene admission and clean server-observed release. Movement remains disabled until readiness/floor/state prerequisites hold. Retain redacted client and server evidence. |
| M3 Complete a terrain region | Correlate MultiFractal, FractalGroup, height affectors, slope filters and derived maps. Implement missing engine-neutral numerics, then chunk streaming, normals, materials, flora and water needed by the selected region. | M0; M1 for DX12 presentation | Reproducible CPU samples with declared numerical tolerances, original asset coverage, adjacent chunk seams, height/collision agreement, cancellation and bounded residency. NGE outputs alone cannot certify Pre-CU parity. |
| M4 Populate the world | Extend existing templates, baseline/delta admission, nearby actors, equipment, customization and animation. Use source to identify ownership, defaults, skeleton/mesh composition and playback semantics. | M0; integrate with M1 and M3 | Selected and nearby actors spawn, update and retire correctly. Representative species/body/equipment cases, late assets, missing references and stale completions pass. Visibility never implies unproved gameplay readiness. |
| M5 Traverse outdoor and interior spaces | Correlate floor/cell transitions, collision, root motion and correction handling. Integrate terrain, avatar and controller paths. | M3 and M4; M2 for authority tests | Outdoor-to-interior traversal, slopes, doors, teleport/corrections and repeated teardown pass. Controlled server agrees on position and containment under authorized scenarios. |
| M6 Deliver a playable Pre-CU slice | Join targeting, chat, inventory/equipment and one server-authoritative combat flow. Use NGE UI/model separation and event lifetimes as leads; implement Pre-CU commands and state from existing evidence and Stardust-3. | M2, M4 and M5 | A repeatable player journey reaches the same server outcomes, displayed state and cleanup. Rejection, interruption and reconnect cases pass. |
| M7 Complete required gameplay and harden | Work through the release feature matrix, original content matrix, effects/audio/UI, streaming performance and deployment. NGE tools/loaders guide asset interpretation and diagnostics. | M6, then feature-specific dependencies | All required workflows pass; remaining exclusions are explicit. Packaged x64/DX12 release passes clean-machine, endurance, memory, network recovery and supported hardware checks. |

M2 and M3 can advance independently once M0 is complete. Keep offline implementation moving when live prerequisites are unavailable. Do not wait for complete terrain or all gameplay before the first bounded interoperability test. M1 is an early rendering gate rather than the project's main compatibility workstream.

## First source-guided implementation tickets

The following paths are relative to the pinned `client-tools` repository. They identify inspected source or concrete follow-up entry points, not transplanted code.

| Priority | Ticket | Source entry points | Required result |
|---|---|---|---|
| 1 | Resolve terrain noise and cache semantics | `src/engine/shared/library/sharedFractal/src/shared/MultiFractal.cpp`; `src/engine/shared/library/sharedTerrain/src/shared/generator/FractalGroup.cpp` | Explain seed/RNG, octave accumulation, bias/gain, cache keys and invalidation against the existing terrain contracts; implement only verified missing behavior. |
| 2 | Resolve terrain height and normal dependencies | `src/engine/shared/library/sharedTerrain/src/shared/generator/AffectorHeight.cpp`, `Filter.cpp`, `TerrainGenerator.cpp` | Connect height operations and normal-demand/invalidation to the [current layer-order contract](TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md); verify ordered samples and seams. |
| 3 | Generalize actor appearance | `src/engine/client/library/clientSkeletalAnimation/src/shared/appearance/SkeletalMeshGeneratorTemplate.cpp`, `SkeletalAppearance2.cpp`; `src/engine/client/library/clientGame/src/shared/object/CreatureObject.cpp` | Extend the existing supported actor policy with a small representative matrix for customization, equipment and nearby-object lifecycle. |
| 4 | Close animation semantics needed by that matrix | `src/engine/client/library/clientSkeletalAnimation/src/shared/animation/CompressedKeyframeAnimationTemplate.cpp`; `src/engine/client/library/clientGame/src/shared/playback/PlaySkeletalAnimationActionTemplate.cpp` | Correlate timing, root motion, event ordering and abort behavior. Source names the PSKA boolean and newer extra tag; preserve the Pre-CU version ceiling until independently resolved. |
| 5 | Expand traversal and corrections | `src/engine/shared/library/sharedCollision/src/shared/core/CollisionWorld.cpp`; `src/engine/client/library/clientGame/src/shared/controller/PlayerCreatureController.cpp` | Verify floor/cell ownership and movement/correction rules for M5. Do not import NGE movement cadence as a Pre-CU fact. |
| 6 | Compile legacy material meaning into the modern host | `src/engine/client/library/clientGraphics/src/shared/StaticShaderTemplate.cpp`, `ShaderImplementation.cpp` | Build a representative opaque/alpha-test/blended/material matrix and deterministic fallback diagnostics. Translate material meaning into modern shaders rather than carrying old device state into the core. |

The [PSKA checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_PSKA_FORWARD_VERSION_AND_RUNTIME.md) supplies a useful low-cost correlation exercise, but its dormant newer records should not displace terrain and populated-world work on the critical path.

## First two implementation cycles

Treat these as two proposed five-working-day cycles, not a completion forecast. Staffing and environment availability have not been established. With one implementer, perform the same tickets sequentially; with multiple implementers, separate ownership as below. This plan does not dispatch agents or restart historical overnight authorization.

Cycle 1: reproduce the current baseline, establish the DX12 profile and packaged architecture check, create the bounded source-comparison record for terrain, and prepare the exact controlled-server scenario. Finish with a backend result, a reproducible baseline and one terrain semantic contract ready for implementation. If an input is unavailable, record it and continue independent work.

Cycle 2: implement and verify the first terrain numerical slice; integrate it into the existing host; add one actor generalization case; execute the controlled scenario only after explicit authorization and prerequisites. Finish with one demonstrably broader world slice and a recalibrated backlog. Do not spend either cycle renaming all unknown functions, rebuilding every original tool, or polishing a disconnected preview.

Suggested work ownership: compatibility/core owner handles source comparisons and terrain; presentation owner handles DX12/materials and actor adapters; session/gameplay owner handles server integration and state; integration reviewer owns independent fixtures and end-to-end acceptance. Owners may be the same person. Each ticket must end in the reusable client path, with a focused reproduction command, rather than remaining only in a round-specific harness.

After two accepted slices, estimate remaining work from measured ticket throughput, reopened defects and content breadth. Maintain separate estimates for first live session, first playable slice and release parity. A fixed completion date or percentage saved is not justified yet.

## Renderer fallback gate

Keep prototype host unless M1 or representative M3/M4 integration demonstrates an essential semantic or performance requirement that cannot be met at reasonable cost. Record the reproducible limitation and compare a bounded adapter fix with a custom-backend spike before switching.

If custom DX12 is required, preserve the core and introduce a narrow native renderer adapter. Choose the language/interoperability boundary through a small measured spike rather than rewriting the .NET core preemptively. The spike must render the same original static mesh, skinned actor and terrain/material samples, support resource retirement and pass the same coordinate/visual tests. It must budget descriptor/pipeline management, upload queues, synchronization, device failure handling, UI/input/audio replacements and packaging. DX12 makes resource states and synchronization application responsibilities; see [Microsoft's programming guide](https://learn.microsoft.com/en-us/windows/win32/direct3d12/directx-12-programming-guide).

Switch only when measured total remaining work supports it. NGE source helps explain rendering semantics but does not remove the engineering cost of a new DX12 backend.

## Acceptance and reporting

Each ticket reports the player-visible capability gained, authoritative evidence, confidence, positive and negative fixtures, integration result, unresolved differences and exact next action. Preserve separate research, function, protocol and implementation measures. Do not change existing research percentages because source became available.

Run the workspace validator and focused checks for changed contracts, registries and code; run the consolidated client gate at milestone integration. Original input absence is a stated limitation, not a pass. Never use an NGE-derived expected result as the sole independent oracle for a Pre-CU claim. Keep source pins and failures reproducible without committing proprietary corpora or assets.

Proposed performance acceptance for ratification during M0: name a reference Windows machine/GPU, resolution, actor count and traversal scene; target a 60 Hz presentation budget and bounded loading/memory behavior. Measure CPU/GPU frame distributions, stutter, peak memory/VRAM and residency after repeated traversal. These are proposed targets, not current measured capabilities. Full release acceptance includes representative supported hardware, stable long sessions, disconnect/reconnect, missing/corrupt resource handling and clear unsupported-feature reporting.

Immediate next action: reproduce Round93 on this workstation and run a bounded x64/DX12 original-scene spike, while preparing the MultiFractal comparison ticket. No change to protocol or accepted asset behavior is needed merely to begin.
