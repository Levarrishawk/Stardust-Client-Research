# Round63: retained terrain rays and owned current-speed playback

Date: October5,2026 Eastern; validation runs October6 UTC. Question: can the retained terrain set support bounded nearest-ray inspection and a proven world/store/current-plan binding without rebuilding terrain or recomputing movement? Stopping condition: independently accepted workers, actual host consumption, malformed-input cleanup and preserved earlier modes. [Round record](../sessions/PARALLEL_DEVELOPMENT_ROUND_63_2026-10-05.md) owns final scope/timing; [overnight authorization](../sessions/OVERNIGHT_DEVELOPMENT_2026-10-05_TO_06.md) permits another round only after acceptance.

## Inputs and method

4017 frozen public files; main HEAD `49378e7eb5372e32be940239867ae66ceeb3f88c` and empty index preserved. Exact disjoint scopes, archive/path/ancestor/hash preflight and postflight precede worker changes. Static executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` is confirmed before VA use. Cached Python, static-analysis packages, .NET8 and Godot4.7.2 .NET are used offline; original executable is never launched.

The six accepted62 host fixtures are byte-identical copies with verified hashes. The new Version4 joined fixture changes explicit synthetic mode metadata while retaining accepted current-plan inputs; its late-direction variant remains synthetic. Original resources come from the unchanged installed overlay/configuration, with original human root SHA `E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243` and generator SHA `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`. Terrain still uses supplied warm priors and modern arithmetic; received message fixtures are authored offline bytes, not captured traffic. Original/proprietary-derived fixture contents and detailed static witnesses stay ignored.

## Accepted findings and compatibility consequences

| Observation | Interpretation / confidence | Consequence |
| --- | --- | --- |
| Predicate01320590 dereferences both arguments as bytes; its zero-mode branch folds ASCII A–Z and returns normalized comparison signs | Confirmed conditional byte-string comparison; runtime mode and nonzero delegates Unknown | Earlier opaque argument is structurally a string pointer. Asset-name, resolver and material roles are still unproved |
| Parser00AC9660 initializes+38 to zero and stores factory EAX at00AC97AC, including null | Confirmed local source/write/normal-return flow; construction shape Strongly inferred | Factory00B0D190 owns the next construction question; field writes do not prove payload type, deletion, reuse safety or ownership |
| Ray query snapshots actual retained child geometry, selects nearest computed parameter and supplied-order ties | Confirmed modern implementation and independent rational fixtures | Engine-neutral ray inspection/picking can use actual source identities, with explicit double-sided/numerical limits and no original collision claim |
| Owned playback checks exact source target/world/store references and creates the actual terrain driver/playback | Confirmed source and native identity/failure checks | Current movement can ground against the actual retained query without accepting arbitrary driver identity or recomputing targets |

Durable boundaries: [conditional shader predicate](../../docs/TERRAIN_SHADER_FAMILY_PREDICATE_CONTRACT.md), [parser payload writer](../../docs/OBJECT_CONTROLLER_PARSER_PAYLOAD_WRITER_CONTRACT.md), [terrain rays](../../docs/TERRAIN_MODERN_PATCH_SET_RAY_QUERY_CONTRACT.md), [owned terrain playback](../../docs/SCENE_WORLD_CURRENT_SPEED_TERRAIN_PLAYBACK_CONTRACT.md). Their individual checkpoints preserve exact evidence and confidence.

## Lead changes and actual consumption

One approved internal read-only `SceneWorldObjectMotionStore.IsBoundTo` method rejects foreign/closed stores without pruning or publication; the exact existing-file diff is reviewed. The wrapper additionally checks actual finalized typed world/Object/Placement/replication identity. Initial driver publication is a documented prefix effect; post-publication failure/cancellation is not rollback. Immutable terrain has no retirement signal, so hosts must explicitly retire the owned wrapper when replacing terrain.

The Godot host accepts an explicit Version4 joined current/terrain route. It consumes the actual owned binding, retained source/plan steps and driver results, while the host performs the final inactive stop. Disposal verifies owned driver/playback state, preserves borrowed source/terrain until their own close, then drains actors/resources. Prior Version3 planar current and Version2 target modes remain distinct accepted routes.

The [ray host adapter](../../poc/Stardust.Godot/ScenePumpedTerrainRayQuery.cs) snapshots once, inspects104 fixed rays and projects one actual camera-center ray. Engine/native Z reflection occurs once at that boundary. This is camera-ray inspection, not an interactive mouse-picking UI. [Independent ray reference](../../poc/round63-tests/Integration/ray_peer.py) uses rational plane and Gram-system barycentric calculations over sealed stored geometry. [Joined playback reference](../../poc/round63-tests/Integration/terrain_playback_peer.py) checks independent target/transition bits, F32 displacement/speed, timestamps, terrain heights and source/step/result markers.

## Executed validation

| Gate | Main-checkout result |
| --- | --- |
| Final worker hashes | All27 reviewed/integrated/worker/handoff hashes match |
| Assets replay | 208 bytes,85 instructions,53 anchors,127 negative guards,65541 independent comparisons; zero-mode only |
| Networking replay | 530 bytes,162 instructions,28 byte witnesses,32 negative checks; all20 sites/15 leads accounted for; no callee expansion |
| Runtime replay | 63 traces,60 sixty-step traces,3630 compared steps,17342 native checks and1202 negative checks |
| Rendering replay | 333 cases:230 accepted/103 rejected;5760 hits/397 misses;7494 ownership checks;86 sealed original-layer rays and400 height/normal observations |
| Combined ray maxima | 16 chunks/256 patches/2048 triangles/4096 queries together;8388608 triangle bound and1090781312 work; one-less limits reject |
| Joined headless/GPU hosts | 60 grounded playback steps;400 terrain height/normal observations;85 surface queries;104 rays with98 hits/six misses and one actual camera hit |
| Joined received-NaN rejection | No pair/plan/owned binding/new playback motion; three archives,16 terrain visuals,one light and actors drain |
| Joined late-direction rejection | Pair accepted, no plan/owned binding/new playback motion; same complete resource drain |
| Prior modes | Version3 planar current independent60-step comparison; Version2 target and Version8 first-curve headless hosts pass |
| Builds/smoke | Runtime, ray and Godot builds zero warnings/errors; default smoke18 events/zero failures |

The joined GPU capture was inspected: original human/terminal and supplied green terrain are visible. It does not demonstrate original terrain material or broad visual fidelity. The60 playback observations stay within one patch after starting on a chunk boundary; native worker traces separately cross multiple patches/chunks. Do not merge those test denominators.

Lead reference initially assumed native start X=0. The established mixed-scene create/reset placement is X=-1.5,Z=0. Correcting that fixture interpretation made both fresh host comparisons pass; production and terrain/speed arithmetic expectations were unchanged. Rendering also corrected test exception handling and selection of the sixteen-chunk maximum case; earlier failures are preserved privately. Parser's complete-body inventory exceeded the initial generic12 budget, so the lead explicitly amended it to20 raw sites/16 total leads without expanding callees. Full inventory denominators are preserved.

## Limits and next action

Original terrain generation/control/material/blending, movement units/gameplay/physics/collision and concrete payload type/ownership/delete behavior remain Unknown. Computed binary64 ray containment/ties are not exact arbitrary-real predicates; tiny direction denominators can underflow to misses. Selected normal failure and contained candidate position-bound failure reject without partial batch publication or fallback. The wrapper borrows immutable terrain and cannot observe arbitrary host selection replacement. No original execution, live contact, capture, fuzzing, downloads, staging, commit or publication occurs.

Next development priority is useful visual/protocol consumption rather than another broad corpus survey: trace the second-SFAM string setter00B69390 and one direct consumer before assigning asset semantics; implement the already documented directional full transform codec with byte fixtures; advance explicit terrain material/selection ownership using accepted contracts. A bounded00B0D190 discriminant/callback probe is available if its unclosed construction semantics actually block implementation. Function, protocol, research and readiness measures remain separate and unchanged.
