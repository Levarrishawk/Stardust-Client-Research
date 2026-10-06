# Round72 camera pivot host

Date: October6,2026. Question: can an explicit supplied engine-world pivot update reuse
the accepted orbit host without changing orbit/input state or committing before actual
camera effects complete? Stopping condition: independent pose/ray agreement, original
orbit regression, bounded failure/ownership proof and sealed scoped handoff.

## Input identity and tooling

Accepted71 frozen input:4335 public files. Manifest SHA-256
`a42f6a8d17554a82c5eb07ad09693484febd48eadf30698458115f8d9cd96426`; archive
`e4f5fd7d111d4006ee09ace0db755353274dbf00bfa5ee848df66322672a79f5`.
Trusted sync completed before edits and preserved private material/HEAD/index. Original
executable identity was verified as
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`;
it was only hashed, never executed. Cached Python, .NET8, Godot4.7.2 .NET x64 and offline
cached packages execute only the replacement scenes. No original asset reads are needed.

## Findings versus interpretation

Observation: [SetPivot](../../poc/Stardust.Godot/TerrainCameraOrbitHost.cs) accepts a
bounded candidate, calculates position before effects, performs actual GlobalPosition
and LookAt with guards, and commits Pivot after both return. Interpretation: it supports
an explicit modern following adapter without owning an actor or simulation clock.
Confidence: Confirmed for bounded replacement fixtures. Compatibility consequence:
caller converts/supplies engine-world pivot and chooses update cadence; no Runtime72 API
or original camera parity is required.

Observation:12 updates across three angle/distance settings and four pivots preserve
yaw/pitch/distance, accepted/handled counters and disabled state, while attempts/revision
advance once per success. Equal updates have no effects. Independent pre-native Python
positions, look basis and perspective rays agree at2e-5/3e-5 tolerances. Confidence:
Confirmed bounded arithmetic/presentation boundary. Compatibility consequence: explicit
following remains available during disabled input; caller must disable following
separately if its UI policy requires that.

Observation: a retained precision experiment used a large pivot and0.25 distance. Its
rounded candidate differed from target, yet Godot logged "same position" during LookAt.
Interpretation: exact binary32 distinction is insufficient for Godot's approximate
origin/target admission. Confidence: Confirmed engine diagnostic; the general policy is
bounded by direct IsEqualApprox admission. Consequence: new SetPivot rejects this case
before effects. Existing Configure/Dispatch behavior is deliberately preserved. Final
replay separately rejects exact collapse at0.001 and approximate equality at0.25 with
pivot components1e6, preserving healthy pose/scalars/diagnostic/counters. LookAt's void
return remains a limitation; no universal engine-error interception is claimed.

Observation: caught reentry, post-position token cancellation and changed camera
projection/parent prevent pivot/revision commit, fault the host and retain honest
position-completed diagnostics. Freed/queued/unconfigured cases reject before effects.
Confidence: Confirmed. Consequence: caller may see a changed actual camera with the old
committed Pivot after failure; no rollback or continuing healthy operation is promised.
The [durable contract](../../docs/TERRAIN_CAMERA_PIVOT_HOST_CONTRACT.md) defines ownership
and preflight-versus-mutation failure behavior.

## Validation and retained limits

[Independent producer/verifier](../../poc/round72-tests/Rendering/verify_picker.py)
creates both new and existing-orbit expectations before native execution. The frozen
regression source is verified against the incoming archive and staged privately. Final
standalone build has zero warnings/errors. New scene:98 headless/100 GPU checks,
12 poses/rays each and private GPU cyan-target capture. Existing regression:118 headless
checks,47 poses/rays,24 accepted inputs. Seven effect/lifetime cases plus numeric,
cancellation, equal, precision, off-thread, closed/faulted and borrowed-survival tests pass.

Focused audit verifies4334 frozen files unchanged and only the allowed existing host
changed, plus nine new public files. Eight runner rejection cases, PowerShell syntax,
Python/XML/Markdown checks, workspace validator, diff whitespace, HEAD/index and private
ignore gates pass. Initial failing precision experiment and its corrected replay remain
ignored for audit. The offline Godot root-certificate-store warning remains with exit0;
final runs contain no LookAt failure or resource leak diagnostics.

No main scene, registry, ledgers or unfinished sibling source changed. No root motion,
actor following adapter, gameplay authority, original camera/input parity, arbitrary
parent transform accuracy, exhaustive precision domain or counter-overflow execution
claim is made.

## Exact next action

Lead joins SetPivot with the supplied engine-world actor point in the existing scene,
checking point/manual input coexistence, disabled GUI policy and camera-relative motion
without introducing a cyclic actor/camera ownership dependency. Define smoothing,
dynamic pivot offset, camera collision/occlusion or failure recovery separately if needed.
