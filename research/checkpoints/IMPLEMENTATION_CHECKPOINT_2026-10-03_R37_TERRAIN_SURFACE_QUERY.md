# Round37 supplied terrain patch surface query checkpoint

Date: 2026-10-03. Bounded question: can an immutable engine-neutral query select a projected supplied mesh triangle and interpolate Y height and supplied normals at native X/Z, with deterministic edges, finite eligibility and capped batch work? Stop condition: four assigned public additions and own runner pass independent rational/double supplied fixtures, negative/lifetime cases, a separate Core assembly build and exact overlay/scope validation. No original collision, sampling, procedural generation or world-axis parity is inferred.

## Input identity and orientation

Approved Round37 overlay: 2,981 files. Manifest SHA-256 `de3bf3446f9233c613a848c251494e06ff1b67b6a1614b65e6ee24e5964f5f5f`; archive SHA-256 `9ba86cda0979b203eea36ff52dcdfaccfb9ea637982e94f9ced53bd7cea741bf`. Main snapshot HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`; worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`.

Before the first overlay write, the entire archive passed listed-member set, canonical/casefold uniqueness, sizes/hashes, containment and target/ancestor symlink/junction/reparse checks. Postflight verified every listed file; private/unlisted content and HEAD/index were preserved. Read START_HERE/latest accepted36, repository instructions, relevant current status/ledger/unknown-function terminology, accepted geometry/topology/assembly contracts/checkpoints and existing tools/runners before implementation. Frozen32 TerrainPatchGeometry and33 TerrainPatchAssembly remain unchanged. No original executable address was used; executable SHA verification is therefore not an evidence claim for this modern task. No original process, live contact, extraction, download, sibling interface, staging, commit, push, Godot, movement or world modification occurred.

## Observations, interpretation and consequences

Observation: the new query copies accepted position/normal/index arrays, caches bounded double areas/bounds, and returns immutable hit values. Constructor rejects projected degenerate/vertical triangles and, by default, winding inconsistent with double-summed supplied normals. Geometry remains owned and caller copies cannot mutate the query. Native old-query alias/failure and cross-thread tests pass.

Interpretation: Confirmed immutable supplied modern height-function input and independent query lifetime; no shared engine ownership or mutable source dependency. Consequence: use the same native coordinate frame for queries, and retain explicit conversion at any engine boundary.

Observation: native X=first, Y=height, Z=second is an explicit supplied convention. Oriented computed edge tests include zero, either winding sign is supported, and source ordinal wins shared/overlapping coverage. Height and raw normal interpolation use anchored double affine arithmetic; normal normalization occurs once. All locations/caps and selected results are finite/eligible or reject. Batch count/location checks and copy precede interpolation; later failure returns no partial result.

Interpretation: Confirmed modern deterministic policy for this corpus, not recovered client collision behavior or exact-real predicates for every ill-conditioned input. Consequence: preserve edge/tie/normal cancellation policy at adapters; do not silently substitute geometric face normals or snap coordinates. Returned normal is interpolated supplied shading data. Numeric classification near internal edges remains affected by ordinary binary64 rounding.

Observation: accepted Godot adapter reflects Z and preserves indices. Interpretation: inherited adapter boundary; source CCW becomes engine CW. Consequence: for reflection alone, Godot-local (x,z) maps to native query (x,-z), height unchanged, normal Z negated. Additional transforms require explicit caller conversion. No Godot implementation is changed or tested here.

Observation: fixed owned array/area charge is at most 328 logical bytes, single lookup tests at most eight triangles, and batch count is capped at 65536. Interpretation: Confirmed bounded data/work definition, excluding CLR overhead, scalar metadata, temporary copies and caller-held results. Consequence: do not treat LogicalOwnedBytes as total heap measurement.

The [durable contract](../../docs/TERRAIN_PATCH_SURFACE_QUERY_CONTRACT.md) defines API, limits, arithmetic order, source identity, ownership and modern policy. [Source](../../poc/Stardust.Core/Geometry/TerrainPatchSurfaceQuery.cs); [scenarios](../../poc/Stardust.Core.SmokeTests/TerrainPatchSurfaceQueryScenarios.cs).

## Reproduction and results

Local tools: .NET 8 and bundled Python `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`. Use existing NUGET_PACKAGES cache `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages` and a round-specific temporary APPDATA. The own NuGet.Config clears package feeds; no downloads occurred.

```powershell
python poc/round37-tests/Runtime/peer.py generate poc/round37-tests/Runtime/supplied-fixtures.json
dotnet restore poc/round37-tests/Runtime/Runtime.csproj --configfile poc/round37-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round37-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round37-tests/Runtime/bin/Debug/net8.0/Runtime.dll poc/round37-tests/Runtime/supplied-fixtures.json <ignored-query-receipt>
python poc/round37-tests/Runtime/peer.py verify <ignored-query-receipt>
python tools/python/validate_organized_workspace.py .
git diff --check
```

Actual separate assemblies: Core, the separate Stardust.Core.SmokeTests project, and own Runtime build with zero warnings/errors; runner uses a ProjectReference and explicitly compiles only the assigned public scenario. Native result: `R37_TERRAIN_SURFACE_PASS checks=10131 negatives=44 records=270`. Independent oracle passes 46 geometry cases and 224 seam assemblies: 4,678 hits and 4,364 outside points (9,042 total). All nine patterns cover flat/sloped/nonplanar/reversed/translated surfaces, plus one overlapping source-order case; all sixteen masks at all cells in counts1..3 exercise seam-selected geometry and coinciding boundaries.

The independent Python oracle imports no native code. Literal topology and integer seam specification feed exact Fraction containment, weights and height; double math.hypot normalizes the independently weighted reference normal. Exact identity/outside/triangle comparisons and relative/absolute 2e-13 numeric comparisons pass. These fixture-level numeric tolerances do not establish bit-exact arbitrary arithmetic or original procedural/collision parity.

Negative checks cover finite/coordinate cap, policy hard caps, strict projected area threshold, normal cancellation/length threshold, winding bypass/direction, zero/vertical projection, empty/excess/late-invalid batches, source nonfinite, alias/lifetime and old-result retention. Queries are concurrently readable without owner resources. Bounds reject representable points just beyond outer edges.

Failed attempt: initial native run passed, but first oracle stopped at fixture-regeneration equality because generated tuples differed from JSON lists. Canonical JSON normalization corrected only the verifier's container representation; fixture bytes and numerical implementation were unchanged, and complete replay then passed. A later explicit computed-weight [0,1] guard was built and replayed through all cases; final native/oracle results above are after that guard. Detailed receipts/build/failure notes remain ignored.

Final scope gate passed: exactly nine additions (four assigned public files and five own-runner files), zero baseline modifications, all 2,981 baseline files byte-identical, HEAD/index preserved, and evidence/build artifacts ignored. Workspace validation: 1,360 Markdown files, zero broken/private links, 2,626 distinct provisional rows and 345 packet rows. `git diff --check` and explicit UTF-8/LF/no-BOM/trailing-whitespace checks on all scoped additions passed. The focused source/scenario/contract diff and overlay-related final status were inspected; unlisted/private material remains preserved. Synthetic fixture SHA-256 `a2ee8a02391910d872a7de572a028f0b2f6b5a70457575acf5cbba55c9a26214`. Shared living status/ledger updates are reserved for lead acceptance.

## Remaining uncertainty and suggested next milestone

No engine/world/movement integration or original input was evaluated. Next bounded milestone: connect the supplied query to an explicitly transformed terrain inspection host or movement adapter, test the native/Godot reflection and parent transform boundary, and keep supplied surfaces distinct from original collision or generated terrain. Exact internal-edge predicates for ill-conditioned inputs, procedural heights, original sampling axes and collision receiver semantics remain unclaimed. Research coverage/readiness/function/protocol denominators are unchanged.
