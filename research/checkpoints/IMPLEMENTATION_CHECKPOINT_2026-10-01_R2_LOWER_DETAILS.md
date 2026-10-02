# R2-ASSETS-01: lower original human details and sampled deformation

Date: October1,2026. Result: ready for lead review, not final acceptance.

## Bounded question and baseline

Do original mesh details2/3 close strictly through object -> SAT -> MLOD -> MGN -> level0 skeleton/bind geometry/material defaults, and do details1..3 deform correctly under selected original walk/run poses? Stop after the four-detail source table, independent sampled matrix/weighted-position comparison and failure/ownership gates. No automatic LOD, GPU or next milestone.

Checkout `C:/Users/ancie/.codex/worktrees/520a/Stardust-Client`; HEAD65de8c88141071744dc75594be126b3bab3696a5. Verified Round2 manifest SHA-256 e2afb787d6cdb9f8c1971183d5a63cdc2b4736e24ec41e039f94feeaf058d097 and ZIP cebf4bd8017249ecc1fc9a9647900bd4062c134ca10040d5b2420c04a3cc4981. Validated all1735 relative ZIP paths resolve inside this checkout, verified source bytes before overlay and all resulting manifest hashes afterward; unlisted/ignored paths preserved. Read START_HERE/latest session, accepted Round1 report, parallel rules, relevant existing contracts/tools/checkpoints and current terminology/status/ledger. Baseline includes accepted uncommitted work and is defined by manifest, not HEAD alone.

## Evidence, interpretation and consequence

See the [durable lower-detail contract](../../docs/SKELETAL_LOWER_DETAIL_CONTRACT.md) for exact source hashes/counts, original clip pins, frame set, tolerance and scope. Original asset/config pins are unchanged and enforced. Static quaternion table reading checks executable SHA before addresses; no executable execution.

Observation: details2/3 each own15 sources, respectively160,163/119,845 bytes,386/101 positions and756/190 triangles. Both contain zero tangents, one material/one texture/nine decoded mips. All0..3 source/skin/geometry/material/mip hashes and scoped joint mappings agree independently. Material closure and animation closure are counted separately. Animation-only closures each have17 sources. Both skeletons explicitly use level0.

Observation: independently sampled raw walk/run CKAT bytes and static quaternion table agree with1,272 local/world matrix rows and11,640 weighted positions across detail1..3. Max matrix error9.806433240999723e-7; max position error5.277724678121043e-7 against2e-5. Motion from bind exceeds0.68 units. Missing hold_l bind policy and unused polearm1_polearm/l0 are preserved. Selected paths reject missing/masked; malformed MGN, unavailable detail, aggregate count/byte budgets and invalid detail/frame/palette/joint inputs reject. Clearing borrowed sources preserves owned geometry/pixels/prepared poses; failed sample calls preserve retained results.

Interpretation/confidence: structures, path selection, source union and independent replacement deformation agreement are Confirmed for pinned inputs and sampled frames. No production defect demonstrated. Automatic mesh/skeleton coupling and original scheduling remain Unknown. Compatibility consequence: keep explicit skeleton selection and preserve lower-detail material/tangent differences; this evidence supports a future bounded rendering adapter without inventing distance policy.

## Verification

APPDATA=`$env:TEMP/stardust-assets-round2-appdata`; NUGET_PACKAGES points to the existing primary checkout's ignored local-private/nuget-packages. Private NuGet.Config clears package sources; NuGetAudit=false; UseSharedCompilation=false. PythonUTF8=1 and explicit UTF8 output decoding. Bundled Python executable `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.

- Offline restore of own console: pass.
- `dotnet run --project poc/round2-tests/Assets --no-restore -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round2/native.txt`: pass, meaningful original positive/rejection/ownership tests.
- `python tools/python/verify_skeletal_lower_details.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round2/native.txt`: pass, exact four-detail closure and raw-byte/matrix oracle.
- `dotnet build poc/Stardust.Core.SmokeTests --no-restore -p:UseSharedCompilation=false`: pass, shared warnings-as-errors build, zero warnings/errors.
- Fixed independent output mutations (one matrix component and one position component): both rejected by the oracle.
- Workspace validator:1016 Markdown files, zero broken links/private links;2602 canonical names/zero duplicate addresses and345 packet rows unchanged. Focused baseline-relative diff/whitespace checks pass.
- Existing Round1 console plus `verify_skeletal_lod_selection.py` default0/1 audit: pass, preserved previous mode/results.

An initial new console build caught nullable test references; fixed with explicit non-null owned geometry/bank locals. No production source changed.

## Lead handoff

Allowed changes only: existing LOD scenario/verifier gain optional0..3 mode; new lower-detail scenario/verifier, own console and this checkpoint/durable contract. Shared Program.cs/project files/root ledgers/registries remain untouched by this milestone. Lead may register an explicit original lower-detail CLI route calling `SkeletalLowerDetailScenarios.Run(args)`; it requires pinned private inputs and should not be invoked by the no-argument synthetic suite. Lead should reconcile accepted navigation/status/round report; no research/function/protocol score increase implied.

Remaining uncertainty: automatic skeleton detail policy, other character families, GPU rendering of lower-detail material family, original runtime interpolation precision and distance/fallback scheduling. Suggested next bounded probe: detail2/3 adapter rendering under explicit level0 skeletons; do not begin until assigned. No Round3 work started.
