# R6-ASSETS-01: decoded-create presentation placement

Date: October 2, 2026. Result: ready for lead review; stop after this Round 6 milestone.

## Bounded question and baseline

Can a decoded create derive a finite engine-neutral row-stored column-vector placement with robust standard quaternion normalization, explicit zero policy and exact raw identity, while independently verifying point/composition/reflection conventions? Stop after own native/Python and ownership/rejection, build/link/scope gates. Existing codecs, AppearanceTransform, materials, scenes, registries and all Godot code remain read only.

Checkout `C:/Users/ancie/.codex/worktrees/520a/Stardust-Client`; HEAD `65de8c88141071744dc75594be126b3bab3696a5`. Round 6 ZIP SHA-256 `0e55506a789046b8a875688c83285e27e535a51fbe09b2c28e7696c679077453`; manifest SHA-256 `3a1e812a4dd0d3de1c61b8e385a30c7851918c015b7757fe316296de4bd6d83a`. Verified 1,855 safe archive paths, file lengths and hashes before copying, then every destination hash. Preserved ignored/unlisted files. Main/shared runner/status/ledger/registries read only.

Orientation: START_HERE/latest named implementation session, accepted Round 5 report, repository/parallel rules, current status/ledger/unknown-function terminology, lifecycle codec/resource contract, component transform/coordinate-adapter evidence and existing source/verification tools. No original executable VA is used or new executable claim made. No original executable, asset corpus, live-server, GPU or engine execution is required for this synthetic mathematical boundary.

## Evidence, interpretation and compatibility consequence

Observation: existing `AppearanceTransform` stores three rows/translation and applies column vectors. `parent.Compose(local)` applies local first; previously established original component corpus and reflected Godot adapter evidence is cited in the [durable contract](../../docs/SCENE_CREATE_PLACEMENT_CONTRACT.md). Existing create codec preserves raw XYZW quaternion / XZY position wire identity and accepts zero/nonunit quaternions; it does not repair them.

Observation: [new builder](../../poc/Stardust.Core/Session/SceneCreatePlacement.cs) rejects null/invalid policy/nonfinite fields; normalizes every nonzero quaternion with scaled double intermediates; handles only exact zero via caller-selected Reject/Identity; derives standard XYZW column-vector rotation and Cartesian translation from named DTO fields. It returns a distinct immutable raw scalar snapshot and derived norm/normalization/fallback metadata. No parent/container/root-motion/engine inference is applied.

Observation: native 27 synthetic decoded-create cases / 105 point probes / 26 compositions / 25 rejections pass. Axes 90/180/270, asymmetric q/-q, nonunit, smallest subnormal/minimum-normal/maximum finite/unbalanced values, signed zeros and extreme/subnormal positions are represented. Clearing encoded source arrays leaves raw DTO bits intact; failed derivations do not affect returned placements.

Observation: independent `math.hypot` + cross-product basis oracle agrees with native rows, point transforms, parent composition and S R S/S p reflection. Maximum basis error 2.9225356801276803e-8, point error 5.651403363060581e-7, composition error 3.0472874641418457e-6 and norm relative error 1.1102230246251565e-16. Deliberate wrong transpose / XZY / single-reflection / reversed-order policies differ in 48 / 97 / 52 / 24 cases respectively.

Interpretation/confidence: **Confirmed** modern derived placement/fixture agreement. Standard quaternion normalization and zero identity are replacement presentation policy. Prior recovered transform storage and packet scalar order constrain representation, but do not prove original quaternion repair behavior. Consequence: later engine/lifecycle adapters can consume the row-stored derived placement while retaining immutable raw state and an explicit zero/parent choice.

Finite output is guaranteed only for the builder's placement fields. Arbitrary later binary32 point/parent composition can overflow; the unchanged operations are not wrapped/clamped here. Metadata normalized quaternion is rounded separately from double-derived rows; exact unit length after binary32 publication is not promised. The AppliedNormalization flag reports nonzero computed input norm differing from exactly 1, not original exact-unit semantics.

## Validation and reproduction

Task APPDATA under TEMP, accepted private NuGet cache, sources-cleared NuGet.Config; NuGetAudit=false, UseSharedCompilation=false and UseAppHost=false; PythonUTF8=1 / explicit UTF8 text. No new dependencies/toolchains. Exact offline commands are in the contract.

- Own offline restore/build: pass, zero warnings/errors.
- `dotnet poc/round6-tests/Assets/bin/Debug/net8.0/Assets.dll`: all new native cases pass.
- `python tools/python/verify_scene_create_placement.py local-private/round6/native.txt`: independent basis/point/composition/reflection/raw-bits/metadata oracle passes.
- Shared smoke project offline restore/build with warnings as errors: pass, zero warnings/errors; shared runner not modified or executed.
- Three fixed incorrect native exports (transposed basis, XZY translation, altered raw bits) reject in the independent auditor.
- Workspace validator: 1,053 Markdown files, zero broken/private links; 2,602 names / zero duplicates and 345 packet rows unchanged.
- Baseline-relative audit: all 1,855 baseline file hashes unchanged; exactly seven scoped additions, unchanged HEAD and empty staging index. Private/build outputs ignored; focused whitespace checks pass. Private allowed-path/hash manifest and baseline-relative new-file diff support lead integration.

Initial oracle exact comparisons treated binary32 shortest JSON decimals as exact binary64 values at maximum-float and q/-q exports. Corrected export reconstruction to binary32, preserving all numerical tolerances and production math. Negative-zero JSON handling is explicit. No original behavioral claim follows from those export corrections.

Private exports/audit/hash/diff records remain ignored. No shared runner modification, Godot/headless/GPU run, original execution, network contact, staging/commit/push/publication.

## Handoff and remaining questions

Seven new allowed files: Core Session builder/result/policy, smoke scenario, Python oracle, durable contract, this checkpoint, own console project and Program. Shared runner may register `SceneCreatePlacementScenarios.Run()`; lead owns registration and canonical living documentation.

Original zero/nonunit quaternion repair, world/container transform ownership, root motion and runtime/renderer application remain open. Suggested next action: lead review/acceptance/integration; later bind this placement to accepted owned creature resources under an explicit zero/parent policy. Stop here until another assignment.
