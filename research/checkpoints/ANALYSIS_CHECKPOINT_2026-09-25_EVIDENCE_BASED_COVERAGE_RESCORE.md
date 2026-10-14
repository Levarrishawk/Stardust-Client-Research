# Evidence-based client-research coverage rescore

Date: 2026-09-25. Scope: the verified `Stardust.exe`, effective configured original assets, current Stardust-3 source correlations, and durable research present in this repository. This is a planning measurement, not a claim that a percentage of source functions has been recovered.

## Result

The current evidence supports two different planning scores:

- **89% comprehensive behavioral research coverage** (unrounded calculation: **88.71%**).
- **94% implementation/engine-decision research readiness** (unrounded gate calculation: **94.15%**).

The former historical **90% / 96%** estimates are superseded. Nothing regressed in the evidence; the lower readiness score reflects a stricter denominator and explicit penalties for UI/gameplay breadth, runtime fixtures, lifecycle/failure tails, and still-conditional original-asset behavior.

Neither score measures exhaustive function recovery. The separately observable registry contains **2,568 unique semantic addresses** against **93,287 non-`Unwind@` Ghidra entries**, or **2.75%** as a raw registry/export ratio. That ratio is not a valid completion percentage because the analyzer denominator contains thunks, CRT/STL/library code, wrappers, splits and unreachable routines, while one recovered behavioral contract can cover many unnamed helpers. A normalized reachable-application denominator still does not exist.

## Comprehensive behavioral rubric

This reuses the historical 14-category weights, which total 100, but replaces every stale score with a current-evidence judgment and an explicit residual.

| Workstream | Weight | Current | Weighted points | Principal evidence-backed strength | Material residual |
|---|---:|---:|---:|---|---|
| Executable identity, imports, resources, compiler/RTTI | 5 | 85% | 4.25 | exact PE/hash/imports, extensive RTTI/vtables, stable address registry | reachable application-function denominator and long-tail virtual targets |
| Startup, shutdown, configuration, logging, main loop | 5 | 88% | 4.40 | entry/frame order, configuration, Splash→intro, major shutdown owners | exceptional startup failures and complete cancellation/destruction ordering |
| SOE transport, sockets, reliability and policy | 12 | 92% | 11.04 | framing, transforms, seeded CRC, channels, ACK/fragment/multipayload contracts | controlled-server golden transcripts, timeout/reconnect and adverse ordering fixtures |
| Login, ConnectionServer and zone state machines | 8 | 92% | 7.36 | credentials, clusters, characters, permissions, zone selection/entry paths | deployed-server failure/reconnect/version-mismatch fixtures |
| Named/controller messages and handler registry | 12 | 96% | 11.52 | 345 current-source rows with zero documented reachable inventory gaps; major client handlers/state paths | dynamic/optional producers and runtime byte/state verification |
| Object hierarchy, baselines, deltas and lifecycle | 12 | 88% | 10.56 | common schemas, containment, creation/destruction and numerous derived families | remaining derived families, malformed/stale update and transfer cleanup scenarios |
| VFS/TRE, templates, tables, localization and assets | 10 | 91% | 9.10 | overlay rules and broad corpus-backed IFF/template/table/resource families | three broken redirects, generated-name reachability and rare forward/optional variants |
| Renderer, shaders, materials, meshes, textures, effects | 12 | 88% | 10.56 | D3D9 device/frame phases, material/effect/texture/mesh/light/effect paths | reachable forward shader fidelity, visual fixtures and complete device/failure tails |
| Animation, skeletal appearance and equipment | 6 | 91% | 5.46 | skeleton/mesh/customization, controller/playback, CKAT, messages and exact root-motion application | conditional PSKA/FIPL/PPRL and small numerical/optional fidelity tails |
| Terrain, world, collision, camera and transitions | 5 | 90% | 4.50 | terrain generation/LOD/seams/collision, cells/portals, water/flora and transition paths | representative cancellation/failure fixtures and conditional content edges |
| UI, input, commands and mediator lifecycle | 6 | 75% | 4.50 | framework/input maps plus many complete high-value workflows | largest remaining breadth: live mediator register, less-used actions, failures and cleanup |
| Audio and feedback | 3 | 82% | 2.46 | initialization, descriptors, lookup, spatial playback and shutdown | archive-backed `.ffe`, anomalous reachable descriptors and selected failure paths |
| Memory, allocators, threading and jobs | 2 | 72% | 1.44 | principal allocators, loader/streamer/terrain ownership and several joins | systematic owner/callback cancellation and race/error cleanup audit |
| Remaining client gameplay/simulation | 2 | 78% | 1.56 | representative combat, crafting, auction, missions, social and space managers | scenario-based long tail and client-local policy/state effects |
| **Total** | **100** |  | **88.71** |  |  |

The appropriate whole-number report is **89%**, with a reasonable judgment interval of approximately **86–91%**. The interval reflects uncertainty in the size and reachability of the UI/gameplay/function long tail; it is not statistical confidence.

## Implementation/engine-decision research readiness

This measures whether the recovered contracts are sufficient to specify fair prototype host/custom-DX12/Vulkan spikes and the narrow x64 networking proof of concept. It does **not** mean those spikes have run or an engine has been selected.

| Decision input | Weight | Current | Weighted points | Remaining gate |
|---|---:|---:|---:|---|
| Protocol/session architecture | 20 | 98% | 19.60 | controlled-server transcripts and adverse fixtures |
| Original-resource scope and asset architecture | 25 | 95% | 23.75 | reachable forward/failure fixtures, not more broad format census |
| Character/customization/animation requirements | 15 | 94% | 14.10 | conditional playback variants and spike validation |
| Terrain/cells/world requirements | 15 | 94% | 14.10 | cancellation/streaming spike validation |
| Renderer/material requirements | 10 | 95% | 9.50 | forward shader visual fixture and candidate spike |
| Lifecycle/threading constraints | 10 | 88% | 8.80 | stale-completion/cancellation tests and remaining owner tails |
| UI/input/audio scope | 5 | 86% | 4.30 | representative integration and long-tail prioritization |
| **Total** | **100** |  | **94.15** |  |

The correct whole-number report is **94% readiness**. The remaining six points are not a mandate to reverse-engineer every optional screen before a networking POC; they are the research/fixture gaps that prevent claiming complete fidelity or selecting an engine without spikes.

## Strict function-accounting statement

| Measure | Count / result |
|---|---:|
| Ghidra non-`Unwind@` entries | 93,287 |
| Curated stable semantic addresses | 2,568 |
| Raw ratio | 2.75% |
| Analyzer entries not represented by a curated address | at most 90,719 before normalization |
| Valid reachable-application denominator | Unknown |

Therefore it is false to say that 89% of executable functions have been reverse-engineered. It is also false to say that exactly 97.25% of meaningful client code remains unknown. Exhaustive per-function classification is still a large, separate project, while current documentation emphasizes observable replacement behavior.

To make function-level coverage scoreable, first normalize raw entries into application/library/thunk/data/error classes, recover omitted callback/vtable roots, determine reachable roots, and attach a role/confidence to every in-scope reachable application routine. Until then publish counts, never a function-completion percentage.

## Highest-value remaining research

1. Build the live UI/workflow register and close reachable unvisited mediators by activation → event → effect → failure/cleanup, not by RTTI count.
2. Create passive deterministic transport/login/zone byte fixtures against the controlled Stardust server configuration.
3. Close conditional original-asset reachability: PSKA/FIPL/PPRL, broken APT redirects, reachable forward shader cases, `.ffe` and anomalous sound descriptors.
4. Audit representative lifecycle/thread cancellation and stale-completion paths across loaders, scenes and UI managers.
5. Continue derived replication/gameplay scenarios only where the server can produce them or assets/actions establish reachability.
6. Maintain function classification as a parallel evidence index, but do not let raw denominator work displace observable contracts.

## Score-change rule

Future scores change only by rescoring an affected weighted row against its stated residual. Block count, provisional-name count, packet-row count, RTTI mentions and asset-file count cannot directly advance the total. A one-point whole-client increase requires at least one additional weighted point after showing the revised row arithmetic.

Confidence: **high** that the foundational architecture and POC requirements are understood; **medium** for the 89% point estimate because remaining UI/gameplay reachability is not yet fully enumerated; **low/undefined** for any exhaustive function-completion percentage.
