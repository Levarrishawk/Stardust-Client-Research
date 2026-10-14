# Ordered slope texturing and seven-family DX12 rendering

Question: can the replacement apply the six scoped original slope-shader branches
at the correct point in height traversal and visibly render their materials?
The user renewed authorization after the earlier 21:00 stop. No original
executable execution, server contact, commit, push or publication was authorized.

## Inputs and direct evidence

- Pre-CU executable SHA-256:
  `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
- Original Tatooine TRN SHA-256:
  `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
- TGEN SHA-256:
  `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
- prototype host 4.7.2 .NET x64, actual D3D12/Forward+ on RTX 4070 SUPER.
  Existing private .NET 8 toolchain/cache and original effective installed overlay.

The [endpoint contract](../../docs/TERRAIN_FILTER_SLOPE_CONTRACT.md) establishes
P*s*Q conversion, the F32 argument spill, literal half-pi clamp, complementary
sine and F32 endpoint-cache stores. The [normal producer](../../docs/TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md)
establishes current-height positions, Q10–Q01 triangles, row-major accumulation,
normalization and normal Y consumption at the demanding layer entry. Static
replays pass 119 endpoint anchors/18 finite cases and 13 normal-producer body
identities/71 anchors. The follow-up additionally confirms four dirty predicates
and rejects eleven synthetic body/slot mutations plus three wrong identities.

**Confirmed:** operation graph, source widths, finite filter/ASCN selection and
ordering boundaries. **Modern choice:** binary64/library sine and square root,
required F32 stores, first family texture and vertex blending. Original x87
controls/transcendental result bits remain unknown; original numeric parity is false.
NGE is corroboration only. This slice changes no server/protocol boundary.

## Implementation consequence and bounded scope

The [ordered Core policy](../../docs/TERRAIN_ORDERED_SLOPE_PREVIEW_CONTRACT.md)
joins current heights and partial shader evaluation. It admits branches
79/80/81/88/89/133, including FHGT89, for fourteen shader layers and seven families
1/3/4/5/6/8/9. Forty ASCN layers remain deferred. Query snapshots are immutable;
later writers cannot change an earlier slope decision. Shader influences retain
the established boundary/FHGT query arithmetic, while the existing geometry
weight path is preserved. Zero-product height culling remains an explicit modern
geometry-preservation choice. Compose rejects mixed old/new policy receipts.

The [seven-family adapter](../../docs/TERRAIN_SEVEN_FAMILY_GPU_BLEND_CONTRACT.md)
uses CUSTOM0 for 1/3/4/5 and CUSTOM1 for 6/8, with 9 as the complement. Fine and
far meshes share one owned binding and borrow original uploaded textures. Existing
five-family defaults remain intact. Public far layout selection alone does not
prove classification semantics; the host source policy and required original-byte
query traces establish that association.

## Verification completed before package acceptance

Three original probes independently replay 1764 labels/normals and nine cache
snapshots, covering all six branches. They show 1707 family changes against the
independent old five-family classifier. Native comparisons preserve default vs
ordered height words and report zero cache/geometry coordinate mismatches.
Tusken changes175/196; Junland changes751/784, including family1 gravel;
Arch Valley changes781/784, including family4 cracked cliff and family1 gravel.
These are supplied-current-height normal/filter/ASCN oracles, not independent
verification of all earlier height generation or original generated-map parity.

Core seven-family tests pass6227 assertions/219 independent Fraction samples.
The actual DX12 fixture passes912 checks/10 expected rejections. The parent
the separate implementation record
confirms24 pixels,11952 uploaded custom components and12 tamper rejections.
Old five-family and far GPU fixtures also pass. All measured resources balance.

The first joined Tusken developer proof failed the unchanged three-successor
publication gate, with only generations1/4 published. Its geometry/query replay
passes, but it is retained as a failed overall proof. The ordered-only automated
motion schedule now runs through28s and captures at0.6/18.6/29.8s; minimum three
successors/twelve reused chunks and all resource/motion checks remain unchanged.
Gameplay input is unchanged. The Arch Valley developer proof at(19,109) passes
with five successors,4318 updates,161.33m motion and max grounding error7.63e-6m.
This developer run precedes the final zero-product geometry safeguard; the frozen
package is the final-source acceptance gate.

## Package gate and exact continuation

**Final acceptance:** the corrected self-contained `slope-v2` x64/DX12 package
passes all21 frozen checks in `sv2-qa`; package hashes remain unchanged. Its
Arch Valley8×8 start at(19,109) retains320m fine terrain, stitched640m view and
original pond support. The final run checks5133 movement updates/161.4242m,
five successors and max grounding error7.6276e-6m. Independent six-window replay
checks94080 labels/normals,480 cache snapshots and76833 changed family labels;
all seven original shader/MAIN bindings and fine/far weight channels pass.

Main cleanup balances1776 fine visuals, six far visuals, one shared terrain
binding and one water binding. The default start is dry; original pond rollback
is separately verified in the same frozen binary. Rollback/cancel/cancel-stage/
reset/retarget-stage/water-rollback/far-rollback all pass. Final family patch
counts are478 gravel1/94 base3/452 cracked-cliff4. Largest measured wall interval
is715.32ms, including startup/capture; no production frame-pacing claim follows.

Full40 offline suites/104 commands pass with zero omitted defaults. Subsequent
Python-only far-floor/upload-accounting corrections have nine/sixteen focused
tests and actual old/new original replays; they follow the runner source snapshot.
Workspace validation passes1964 Markdown files, zero broken/private links and
zero duplicate provisional addresses. No score, protocol or registry changes.

The paragraphs below preserve failed attempts and gate progression. **Exact next
implementation:** choose the next bounded deferred material/filter branch,
starting with road/square appearance or deterministic family-child choice;
inspect its Pre-CU contract before admission. Full terrain appearance, flora,
view distance, frame pacing and live authority remain separate gaps.

Fresh private package: `ordered-slope-stream-export`, initial8×8(19,109), with
ordered slope, seven-family blending, original local water and stitched640m far
view. Previous accepted `far-water-stream-export-v2` remains preserved. Frozen
verification is in progress; the full40-suite regression replay passes all40
with104 commands and zero omitted defaults. Twelve report-validator tests pass.
The subsequent verifier-only exact far-floor check passes nine focused synthetic
tests plus old/new original proof replays after the runner snapshot. The new
package is **not yet accepted**. Pond rollback uses an explicit original pond
window in the same frozen binary, since the default slope start is dry.

The first frozen package passes its main GPU/query/surface/far verification but
fails `stream-rollback` on the fixture timeout. Injection required a published
successor, even though initial adopted generation0 already owns a visible window;
the first successor arrived after automated movement stopped. The correction
targets that fixture eligibility and preserves exact fault allocations, previous
window/motion/visibility, cleanup, timeout and normal publication gates. Retain
the failed package/receipts. A fresh v2 package must pass the complete gate before
acceptance; no normal gameplay or additional branch change is assigned.

All three developer rollback checks now pass from the actual adopted generation0
window. They preserve exact four-fine/one-water/one-far fault points, all previous
window/motion/visibility assertions and balanced cleanup. The source delta is
limited to fixture eligibility and explicit previous-generation receipt fields.
The first long-name v2 build attempt failed on a 260-character runtime-cache copy
destination; source input was present. The shorter `slope-v2` build succeeds and
its complete frozen verification is running in `slope-v2-verify`. Retain both
failed attempts. No implementation change is underway during this gate.

Frozen original surface/query/far, movement, reset/rollback/cancel/retarget,
cleanup and exported visual inspection are complete for the final package.
Original numeric/LOD/blend parity, full terrain appearance, flora/scene breadth,
frame pacing and live interoperability remain open. No coverage score changes.
