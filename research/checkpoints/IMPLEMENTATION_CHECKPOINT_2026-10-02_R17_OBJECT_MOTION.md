# R17-R object motion store

October 2, 2026. Bounded question: can finalized world incarnations retain independent native pose revisions and authoritative serial freshness across local updates, state/resource changes and lifecycle retirement, without stale mutation or leaked snapshot roots? Stop when frozen source API, native positive/negative/lifecycle evidence and exact repository scope gates are complete.

## Input identity and orientation

Round17 full overlay verified: 2,272 public files; manifest SHA-256 `f7af15cbcd8ac9a219a03cf7ae21502c5c69b9fecca16ed3ef3a20eb1be6d832`; ZIP `c4b37e54c60d74c8b737a5918b97eca0d08dcc60e50ecc5c10929e258db5ae08`. Snapshot HEAD from manifest `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`; isolated checkout HEAD separately preserved `65de8c88141071744dc75594be126b3bab3696a5`. All unique archive members exactly match manifest; safe relative/resolved-contained/no-symlink paths, byte lengths and SHA checked before any writes; every output hash verified after listed-only overlay. Unlisted/ignored/HEAD/index preserved; main is untouched.

Read AGENTS, START_HERE/[latest Round16](../sessions/PARALLEL_DEVELOPMENT_ROUND_16_2026-10-02.md), status/ledger/UNKNOWN, relevant mixed/consumer/pump/native-transform/movement contracts and prior integration/transform probes, frozen motion interface, registry/AppearanceTransform/coordinator APIs and original accepted fixture producer, curated DataTransform boundary rows and workspace tools. No executable VA reliance or new protocol/static research claim. No original assets/config/executable are needed or read in this milestone; profile JSON metadata and synthetic fixture bytes are sufficient. Existing Python/.NET8/offline cache only, sources-cleared NuGet/task APPDATA/no shared compiler. No main/shared files/Git mutation/download/live contact/original executable/asset publication.

## Observations, interpretation and consequences

Observation: store (external historical reference) retains one immutable pose/private last-counter row per live finalized incarnation and one store-global checked U64 revision. Canonical object/placement, handle, typed currentness and opaque history validate incoming snapshots; rows retain no old world/state/asset snapshots. Interpretation: modern borrowed motion provider, separate from registry/resource/state ownership. Confidence: Confirmed native tests/source. Consequence: consumers request a fresh world snapshot, then may retain motion until replaced or its incarnation retires; disposing the store does not close the world.

Observation: accepted local input exposes null counter but keeps the private authoritative counter. First authoritative U32 is unrestricted; later modular distance1..7FFFFFFF accepts, duplicate/older/exact-half rejects atomically. Independent U64-distance test model covers64 boundary pairs, including wrap and local-after-authoritative. Interpretation: explicit modern serial freshness with no original timing claim. Confidence: Confirmed deterministic native tests; original counter behavior remains outside scope. Consequence: integration must not call Clear for ordinary state/resource updates, since Clear intentionally resets row counter history.

Observation: pose values preserve native affine rows, translation and signed zero, with finite nonnegative speed and double-precision rigid/proper tolerance1e-4. Scale/shear/reflection/nonfinite/collapse reject without modifying pose, global revision or private counter. Height updates do not modify pose/raw placement. Interpretation: engine-neutral rigid boundary, independent replication Height. Confidence: Confirmed known rotated point, signed-zero/extreme finite and invalid/tolerance tests. Consequence: rendering handles any Height scaling separately; no implicit engine conversion/physics/interpolation is supplied.

Observation: containment/resource restart/state/history updates retain creation motion; destroy/recreate/reset/failure/close retire obsolete rows; capacity rejects before mutation, successful valid publication reclaims stale slots. Clear/Prune retain global revision. Lead identified that per-row revision reset would strand an adapter's revision guard; implementation now uses a monotonic bounded global sequence, tested by Clear while the same incarnation remains live then republishing with a strictly newer revision. Interpretation: modern adapter freshness with no unbounded tombstones. Confidence: Confirmed lifecycle/identity/capacity tests; checked U64 overflow verified by source inspection only, not an unreachable execution claim.

## Reproduction and results

Scenario entry: `SceneWorldObjectMotionScenarios.Run(IReadOnlyDictionary<string,string> peer, string exportPath)`. Own runner (external historical reference) takes peer/export arguments. Run from repository root; it reads accepted catalog and motion-bank profile JSON from poc/preview-profiles, without opening TREs or loading resources. Existing independent Round14 producer supplies synthetic real-layout lifecycle/CREO/TANO fixture bytes; this store performs no wire parsing itself.

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r17-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA,local-private/round17 | Out-Null
python poc/round14-tests/Runtime/Generate-Peer.py local-private/round17/peer.json
dotnet restore poc/round17-tests/Runtime/Runtime.csproj --configfile poc/round17-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round17-tests/Runtime/Runtime.csproj --no-restore -p:UseAppHost=false -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round17-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round17/peer.json local-private/round17/export.json
python tools/python/validate_organized_workspace.py .
git diff --check
```

New native runner PASS: 280 checks,64 independently modeled serial pairs; zero warnings/errors. Tests cover native point transform, arbitrary first/wrap/duplicate/older/half-range counters, local private-counter retention, exact current motion/foreign-store clone rejection, rigid finite/tolerance/signed-zero/extreme values, stale/null/altered/stripped snapshots, typed Height and static opaque state independence, containment/resource restart, pending/recreated/destroyed/reset/failed/closed incarnations, capacity/invalid/counter atomicity, Clear/global revision continuity, foreign thread/constructor affinity and nested busy-world construction. Default resource loader is never invoked; two explicit already-faulted synthetic jobs solely exercise busy-world construction and close observation, with zero asset reads. Initial test compile caught an assumed static policy name/omitted nullable frame argument; corrected to baseline RequireComplete/null before passing execution. No required private input/tool was missing. U64 exhaustion, direct unreachable callback-free store reentry, GPU/physics/host/wire movement/live integration are unrun and not claimed.

Shared native default suite passes with a zero-warning build. Final workspace/link/scope/whitespace/unchanged-HEAD/empty-index checks accompany handoff. Durable [contract](../../docs/SCENE_WORLD_OBJECT_MOTION_CONTRACT.md) contains exact acceptance/revision/counter/rigid/cleanup policy.

## Exact scope and lead action

Seven additions: core SceneWorldObjectMotionStore.cs; SmokeTests SceneWorldObjectMotionScenarios.cs; docs/SCENE_WORLD_OBJECT_MOTION_CONTRACT.md; this checkpoint; own poc/round17-tests/Runtime Program.cs, Runtime.csproj, NuGet.Config. Frozen interface/coordinator/pump/consumer/shared project/Program/navigation/current docs/status/ledger/curated data remain unchanged. All logs/generated exports/fixture/manifests/diffs/hashes remain ignored.

Lead registers Run(peer,export) in shared native harness; inject the accepted motion source into independently reviewed native/visual adapters and publish local/authoritative native poses explicitly. Borrowed store Dispose is separate from pump/world close. No API conflict remains; the global sequence corrects the adapter join concern without interface changes. Suggested next action: bounded motion-to-visual/source join using current incarnation and global motion revision, then fixed-step/interior host integration and protocol-authoritative movement only from separately accepted codecs. No next milestone or cross-chat message; worker stops after Round17.
