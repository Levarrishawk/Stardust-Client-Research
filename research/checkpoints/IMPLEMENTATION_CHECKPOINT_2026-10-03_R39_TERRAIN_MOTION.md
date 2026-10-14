# R39 supplied terrain motion

October 3, 2026. Bounded question: can immutable ordered supplied terrain queries ground the accepted planar integer-clock driver before each local publication, while preserving current-incarnation ownership, authoritative corrections and prior successful ticks on a later failure? Stopping condition: bounded public API, independent analytic/schedule fixtures, separate actual Core/SmokeTests build and final scope/link/whitespace checks; hand off, then stop idle.

## Inputs, identity and orientation

Frozen accepted38/main Round39 snapshot: 3,081 public files; manifest SHA-256 `4cac8ae4fe7a5315d8958682151ae982ec85de7f41f089afc8e8694ccf3a6e65`; ZIP SHA-256 `b390959b5cb8e53e40523ea3764e7388a7975ca5708f897873d557e95b92dacb`. Manifest main HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`; worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`. Full unique archive membership, casefold uniqueness, canonical paths, sizes/hashes, resolved containment and every target/ancestor symlink/junction/reparse condition were verified before the first overlay write. Postflight verified every baseline byte, preserving unlisted/private data and actual HEAD/index. No Git reset/stage/commit/push or publication.

Read START_HERE, accepted38 session, status/ledger/UNKNOWN, accepted18 local-motion contract/checkpoint, accepted37 supplied-query contract/checkpoint, frozen query/geometry/topology, local motion/store/world/interface, generic host/timeline, accepted local scenarios and peer/tools/curated movement packet rows. Existing Python/.NET/offline NuGet only. No executable VA, original input or new static claim is used; executable hash verification is therefore not invoked. No unfinished Rendering39 or other sibling dependency is consumed. Existing historical implementation path (omitted) files remain immutable.

## Evidence and compatibility consequences

Observation: driver (external historical reference) implements the frozen local-driver interface, copies a count-capped ordered reference vector of immutable TerrainPatchSurfaceQuery objects, and creates the frozen private host/timeline. It uses current finalized typed CREO snapshots and the frozen motion store. Interpretation: bounded modern native X/Z travel with supplied Y grounding. Confidence: Confirmed implementation/source and public separate-assembly fixtures. Consequence: geometry must already share native coordinates; callers supply ordered surfaces, foot offset, active/loss boundaries and one serialized driver per object.

Observation: each candidate validates incoming position, reserves each attempted patch's full TriangleCount, chooses the first patch hit and uses that query's frozen triangle order. It computes height+offset in double, bounds it before float conversion, replaces Y alone, and publishes through the frozen proper-rigid/finite transform validator. Query exceptions and outside coverage stop selection without fallback. Interpretation: deterministic source-order overlap/seam policy and logical per-candidate work reservation. Confidence: Confirmed independent analytic flat/sloped/seamed fixtures and exact/one-less/native late failure tests. Consequence: no extrapolation, seam repair, sliding, jumping, normal alignment or original terrain/collision parity; work charges are not measured heap/CPU costs.

Observation: local X/Z accumulation, tick-start input, yaw/speed and integer scheduler are the accepted planar operation graph; grounding runs only on constructor/reset/local ticks. Fresh authoritative corrections snap raw pose/speed without pseudo-publication and are grounded at the next actual local tick. Interpretation: accepted modern immediate correction timing remains intact. Confidence: Confirmed three matched correction partitions, paused/no-tick correction, over-cap rejection and next-tick outside tests. Consequence: complete ticks to a common observation boundary before comparing partitions; a correction with raw altitude/outside coverage can remain visible until a tick, and the driver cannot undo external authoritative writes.

Observation: a late outside/query/budget/position failure preserves two already published ticks and their exact current record, reports completed count, drops uncompleted time and sticks until reset. Reset queries create placement before publication, then resets private history without another fallible query. Destroy/recreate/generation/failure/close never reattach the old driver. Dispose releases reference roots and leaves borrowed rows/world intact. Interpretation: per-tick publication atomicity, not whole-Advance rollback. Confidence: Confirmed public failures/ownership/lifecycle tests. Consequence: new incarnations need a new driver; still-current faults need explicit reset. Owner affinity is execution-tested; reentry guard and absence of injected operation callbacks are source-verified, not forged callback execution.

The durable [contract](../../docs/SCENE_WORLD_TERRAIN_MOTION_CONTRACT.md) is the canonical constructor, limit, lifetime, correction and failure specification. Limits default patches64/triangle reservation512/position1e6/offset1000; hard patches1..256, triangle1..2048, finite positive position<=1e9, finite nonnegative offset cap<=1000, absolute supplied offset within cap. Frozen steps1,000..100,000 and maximum1..32 remain unchanged.

## Independent fixtures and validation

Native scenarios (external historical reference) are compiled by both the actual separate SmokeTests project and the own runner (external historical reference) with a ProjectReference to actual Core. They use public APIs only; no linked production source, reflection, private accessor or testing API is introduced. Shared test entry/registration remains lead-owned.

Independent Python peer (external historical reference) reuses the accepted14 independent synthetic lifecycle/CREO fixture generator, then authors a zero-placement identity-quaternion create message through struct.pack_into. Supplied planes/slopes/ordered seams and schedules are independently authored. Its oracle computes analytic height from X/Z, binary32 displacement/accumulation/speed/yaw and integer tick selection/pending/drop/suspension/revision/correction transitions without using native expected models or query receipts. Height/vector tolerance is relative/absolute2e-6; exact integer/identity flags are compared exactly. Frozen37 query tests remain the broader barycentric precision evidence.

Native result: **2,954 checks / 22 sequences / zero resource loads / zero asset reads**. Independent oracle: **21,735 comparisons**, including every exported X/Y/Z, all nine row components, speed, timestamp, completed/dropped/suspended work, active/fault state, source revision and authoritative/local flag. Four sets of three partitions (flat/two slopes/correction) produce bit-identical final poses and revisions with matching completed ticks/correction boundaries. Exact/one-less policy and late outside/normal/triangle-budget/position faults, invalid input/time/order/horizon/capacity, aliases, owner, proper non-yaw/zero-speed orientation, diagonal yaw, authoritative/paused correction, borrowed capacity/close, recreation/generation/failure/world close, failed reset and disposal pass.

Core/own runner and actual separate Core/SmokeTests build: zero warnings/errors. Final baseline-relative audit found nine additions, all3,081 baseline files unchanged, no existing edits/deletions, preserved HEAD/index and ignored private/build outputs. Workspace validator passed1,385 Markdown files, zero broken/private links,2,626 provisional addresses/zero duplicates and345 packet rows. Owned Python AST/project-config XML and UTF8/no-BOM/LF/final-newline/trailing-whitespace checks passed; git diff --check passed. Focused production/scenario/contract/checkpoint changes and Git status were inspected; synthetic fixture correctness is checked by exact independent regeneration and all oracle comparisons. No broad default suite, GPU/original host, live/server effects or original executable execution is claimed.

### Reproduction

Run from repository root with the existing bundled Python at `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe` and existing .NET8. Logs, receipts and regenerated comparison fixture are ignored under local-private/round39. Existing NuGet cache is `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages`; no sources/downloads are enabled.

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r39-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA,local-private/round39 | Out-Null
python poc/round39-tests/Runtime/peer.py generate local-private/round39/regenerated-fixtures.json
dotnet restore poc/round39-tests/Runtime/Runtime.csproj --configfile poc/round39-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round39-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round39-tests/Runtime/bin/Debug/net8.0/Runtime.dll poc/round39-tests/Runtime/ordered-fixtures.json local-private/round39/motion-receipt.json
python poc/round39-tests/Runtime/peer.py verify poc/round39-tests/Runtime/ordered-fixtures.json local-private/round39/motion-receipt.json
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
python tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Regenerated bytes exactly match canonical synthetic ordered-fixtures.json: SHA-256 `1613dd321a68257cbcab374c3d5a99325f5cfa36d7e4546f2261d469a0f6b817`. Producer explicitly writes LF for platform-stable identity. Python in the commands denotes the exact bundled path above. The separate SmokeTests build uses its already restored accepted baseline dependencies.

### Failed attempts and corrections

Initial orientation attempted three nonexistent paths (Session/FixedStepActorHost.cs, Session/PortalActorInputTimeline.cs, SmokeTests/SceneWorldLocalMotionDriverScenarios.cs); command exit1. Two further Geometry/host and Geometry/timeline guesses with SilentlyContinue yielded no content. rg located Assets/host, generic timeline and actual SceneWorldLocalMotionScenarios.cs; correct files were read before implementation.

First native run rejected synthetic surface winding against supplied upward normals before producing a receipt. The synthetic vertex helper's Z order was reversed to match the frozen topology; production query/driver semantics did not change. The verifier was mistakenly invoked after that failed run and reported FileNotFoundError for the absent receipt; subsequent commands gate verification on native success.

After winding correction, native2,926 checks and all per-record oracle comparisons passed, but the final correction partition equality assertion failed: the fixture published correction before completing different partial-boundary tick counts. The independent fixture was corrected to advance every partition to40,000 first, then publish/observe correction at the same timestamp. Native2,932 and complete oracle passed. Additional boundary/lifecycle tests raised final checks to2,954. All builds passed; no failed compilation or missing tool/input is concealed. Final source review also captured collection Count once before allocation to preserve the configured cap for custom collection implementations.

## Scope and next action

Exact nine additions: core driver, public SmokeTests scenario, durable contract, this checkpoint; own round39-tests/Runtime Runtime.csproj, Program.cs, NuGet.Config, peer.py and ordered-fixtures.json. No baseline public file is modified/deleted. Private regenerated fixtures/logs/receipts/diffs/audit helpers and build output are ignored; curated files have no private links or proprietary content. Lead owns shared registration, living ledger/status and integration.

Next bounded action: lead replay the frozen public runner/oracle, then join this implementation of ISceneWorldLocalMotionDriver to an original host with explicitly supplied native-space geometry. Verify active/loss, correction, current-generation and disposal behavior at the host boundary. Original floor/collision, full terrain generation, slope arc-distance behavior, owner gameplay and live interoperability remain outside these confirmed modern fixtures. Stop idle after final handoff.
