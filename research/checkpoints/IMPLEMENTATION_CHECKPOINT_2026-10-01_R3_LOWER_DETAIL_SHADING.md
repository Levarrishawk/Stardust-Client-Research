# R3-ASSETS-01: original lower-detail specular program closure

Date: October1,2026. Result: ready for lead review; acceptance is lead-owned.

## Bounded question and input identity

Do original details2/3 share a strictly owned material/program closure through hum_m_body_sm.sht -> h_specmap.eft -> explicit PS20 nodot3, and do64 discriminating original texel/supplied-color cases agree with compiled pixel instructions? Stop after source/read-once, exact bindings/factors/MATS/samplers and CPU compiled-pixel/failure/ownership gates. No GPU, automatic LOD or next round.

Checkout `C:/Users/ancie/.codex/worktrees/520a/Stardust-Client`; HEAD65de8c88141071744dc75594be126b3bab3696a5. Round3 manifest SHA-2563549f10f0ac0602e53837010475cf0139bfea4d2c1ce2bb3148228887c12c9bc; ZIP201434fc21c1c6641658f8ce6055bdc4bcab2fb25d2b190a2fa84b7e8b7b0a3f. Validated all1762 ZIP paths remain inside checkout, source manifest length/hash before overlay and every resulting hash; ignored/unlisted files preserved. Baseline includes accepted uncommitted work. Read START_HERE/latest session, accepted Round2 report, parallel rules, relevant contracts/checkpoints/tools and status/ledger/UNKNOWN_FUNCTIONS terminology.

Pinned original object/SAT/configuration and all material/program/source identities are in the [durable contract](../../docs/SKELETAL_LOWER_DETAIL_SHADING_CONTRACT.md). Asset reads use only C:/Stardust-DEV and pinned configuration. No executable addresses or original execution are used by this audit.

## Evidence and conclusions

Observation: the existing reader pipeline, with palette defaults/albedo factors/vertex-lit specular enabled and compressed normals disabled, closes22 sources for each detail:183,408/143,090 bytes. The seven added shading dependencies total23,245 bytes relative to the prior default-material closure. Exact source unions, source priorities/lengths/hashes and read-once counts agree independently. Details2/3 share identical material/program descriptions, same MAIN/SPEC DDS path and UV0,11-byte TXM0001 sampler records, palette-selected factor and MATL power20/specular0.2.

Observation: five effect implementations/six passes are retained, including three programmable candidates and two fixed-function implementations. Candidate negotiation is not selected by the audit. Four pinned VSH/include nodes/three include edges agree. The explicit single-color PS20 code hash matches and64 distinct original texels with varied supplied vertex colors/bloom/fade match independent compiled instruction execution at max8.869665502064805e-8, tolerance1e-5. Strong wrong-policy discriminators cover bloom RGB scaling, SPEC alpha channel, unscaled luminance input and fade override.

Observation: meaningful missing/masked/changed/oversized/truncated source/program, factor-map/material/sampler/coordinate tag, numeric input and aggregate budget gates pass. Missing/masked palette preserves explicit diagnosed opaque-cyan fallback; malformed present palette rejects. Borrowed input mutation leaves geometry/pixels/owned PEXE/evaluator outputs unchanged. Overlay disposal is scoped.

Interpretation/confidence: source identity/structure/material values and compiled pixel CPU agreement are Confirmed for these original inputs and fixtures. Supplied vertex colors are modern fixtures. Original compiled vertex LIT, runtime candidate negotiation/filtering and automatic LOD remain Unknown. GPU is unrun. Compatibility consequence: preserve distinct single-color bloom RGB scaling and alpha semantics, material power20, shared sampler texture identity and explicit palette fallback; do not infer a tangent family or hardware branch.

No production Core defect demonstrated; all existing readers/evaluators are unchanged.

## Verification record

Environment: task APPDATA `$env:TEMP/stardust-assets-round3-appdata`; NUGET_PACKAGES=existing primary ignored local-private/nuget-packages cache; private offline NuGet.Config clears sources; NuGetAudit=false; UseSharedCompilation=false. PythonUTF8=1 and explicit UTF8 decoding/writing; bundled Python runtime.

- Offline own-console restore: pass. Durable reproduction instructions create the sources-cleared task-specific NuGet.Config and identify cached prerequisites; no private configuration file needs integration.
- `dotnet build poc/round3-tests/Assets --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false`: pass, zero warnings/errors.
- `dotnet poc/round3-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round3/native.txt`: pass, original positives/rejection/fallback/ownership gates.
- `python tools/python/verify_skeletal_lower_detail_shading.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round3/native.txt`: pass, raw source/geometry/material/mip/palette/effect/include/compiled-pixel audit.

- Shared SmokeTests offline restore/build: pass with warnings-as-errors, zero warnings/errors.
- Fixed independent output mutations (pixel result and SPEC binding metadata): both rejected.
- Workspace validator:1026 Markdown files, zero broken/private links;2602 canonical names/zero duplicates and345 packet rows unchanged. Focused baseline-relative whitespace checks pass; index empty.

Corrected verification assumptions rather than production: changing the base TFNS MAIN tag may remain valid because the palette supplies MAIN; changed the negative fixture to an unsupported map version. Original samplers are TXM0001/11 bytes, not face-composition TXM0002/12. The effect has five implementations/six passes, including fixed stages; it is not a three-pass-only graph. A failed test process held output files; stopped that test session, preserved outputs, built without apphost and ran the DLL directly. No permission escalation or external contact occurred.

## Handoff

Six new scoped files only: new shading scenario/verifier, own console project/entry point, durable contract and this checkpoint. Shared runners/project files/status/ledgers/registries remain read-only. Baseline-relative hash/scope evidence remains private. Lead may add an explicit original-assets route calling `SkeletalLowerDetailShadingScenarios.Run(args)`; private original inputs are required, so this is not a no-argument synthetic smoke scenario. Lead should reconcile accepted round/navigation/status records. No research/function/protocol score increase implied.

Remaining uncertainty: original compiled LIT/power precision and hardware capability/fixed-function selection, runtime sampler filtering, rendered lower-detail appearance and automatic mesh/skeleton/distance policy. Exact next action is lead review/integration; no further milestone begins here.
