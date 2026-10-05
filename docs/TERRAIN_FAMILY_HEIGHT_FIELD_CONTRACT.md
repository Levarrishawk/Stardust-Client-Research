# Per-cell family height field contract

The engine-neutral `TerrainFamilyHeightField.Build` owns a complete rectangular prior-height and influence snapshot. It prepares one enabled supported AHFR rule and its exact family, then evaluates only positive-influence cells through one fresh supported-mode sampler. This is an explicit modern field policy, not a recovered original traversal or cache lifetime.

## Inputs and immutable result

[Implementation](../poc/Stardust.Core/Geometry/TerrainFamilyHeightField.cs) accepts width/rows, binary32 origin and positive spacing, complete separate row-major prior/influence arrays, encoded rule, immutable decoded family set, and table/construction/query/consumer precisions independently selected from 24/53/64. Dimensions are 2..128, with default 256 and hard 1,024 samples. Each array length must equal width times rows. All owned prior/influence words must be finite. Arrays may alias on input; each is copied independently.

Result axes use the unchanged frozen patch axis body: finite strictly increasing binary32 coordinates. The only old-file edit makes that helper internal. Rule bytes, axes, prior/influence/output arrays and cells have inaccessible owned backing storage, read-only views and detached copy methods. Family and rule source hashes are retained. Cells retain their original row-major index, full rectangle column/row and coordinates. Bounds use resulting heights, numerically first-tie selection and the existing finite absolute-height bound of 1,000,000.

## Strict preparation and selection

Preparation is exactly the frozen [family route](TERRAIN_FAMILY_HEIGHT_FRACTAL_CONTRACT.md) `Prepare`: complete supported enabled AHFR decoding, operation/scale eligibility, exact family resolution and raw supported-family grammar. This happens even when every influence is nonpositive. Unsupported/disabled/missing/malformed rule or family inputs fail the entire field.

Finite +0, -0 and negative influence select `SkippedNonPositiveInfluence`. The prior height word is preserved exactly, including signed zero. `Applied` is null: there is no invented sample, noise, affector, zero-influence receipt or consumer computation. Resulting skipped heights still pass the display bound.

For positive influence, one fresh sampler owns the full rectangle, with requests at unique original indices. It computes one selected-cell batch. Frozen `Complete` receives the actual retained result, actual prior height and actual influence under `SharedPreparationCell` accounting. Each Applied receipt preserves sample/affector identity and shared decoded rule/family/raw identity. No standalone zero-weight consumer path is substituted.

When there are no positive cells, no sampler is constructed or queried, no normalization/gradient/query eligibility is evaluated and no consumer runs. Structurally valid raw inputs that would fail normalization or query evaluation can therefore produce an entirely skipped field. This distinction is deliberate and covered by tests. Preparation still occurs once.

## Reservations before copying

Let L be encoded rule bytes, W/R the dimensions, N=W*R and M the positive count in owned inputs. All five complete all-positive reservations below must fit before any input copies or owned-value inspection. The caller cannot bypass a cap by changing borrowed influence values between preflight and copying. Only owned snapshots determine M and all later arithmetic.

| Reservation | Logical bytes/work |
| --- | --- |
| Copy/output | L + 4*(W+R) + 100*N + 256 |
| Route | 8*L + 65536 + 9512 + 136 + 32*N + 1696*N |
| Work | L + 32*(W+R) + 24*N + L + 20000 + 256 + N + (8288+16)*N |
| Query bytes | 16*N + (160 + 16*80)*N |
| Query work | N + (16*512+64)*N |

Hard caps are respectively 4 MiB, 256 MiB, 64 Mi work units, 2 MiB and 16 Mi work units; default copy/route/work caps are 1 MiB/128 MiB/16 Mi. Encoded rule cap is 65,536. Invalid cap values fail. These are conservative logical policy reservations, not measurements of managed heap or runtime. All-zero fields must also fit the all-positive reservations.

Accepted actual shared preparation accounts for `8*L+65536` bytes and L work, plus `9512+136+32*N` bytes and `20000+256+N` work only when M>0. Actual route bytes add 1696*M. Actual work adds the outer `L+32*(W+R)+24*N` and `(8288+16)*M`. Per applied cell receipts show 1696 bytes/8288 work, separated from shared cost. Query reservations remain the reported worst case. Counts are one rule decode/lookup; sampler construction/batch/disposal are each one iff M>0; computed cells/applications each equal M; kernel octave queries equal M times raw octave count.

## Atomicity and evidence

The result is published only after all queries, consumers, display checks and cancellation checks succeed and the local sampler has been disposed. Failure returns no partial field or counters. Previously returned objects and caller inputs remain unchanged. Owned input checks reject mutations captured during copying; a coherent simultaneous multi-array snapshot is not promised. Existing sampler query failure/disposal contracts supply local cleanup.

The independent Fraction oracle and [native scenarios](../poc/Stardust.Core.SmokeTests/TerrainFamilyHeightFieldScenarios.cs) verify 156 cases: 44 rejects; 1,987 accepted cells, 1,851 applied and 136 skipped; seven noise-free fields; 165 ownership/atomicity assertions. Eighty-three uniform-positive fields match both frozen fresh-per-cell and single-sampler patch policies. Mixed fields cover all six modes and three arithmetic profiles, signed skips, near-neighbor coordinates, aliased inputs, exact/one-below caps, maximum shapes, mutation, read-only views, independent repeats/foreign thread, late query/consumer/display rejection and pre-cancellation.

Confirmed: these bounded implementation/test behaviors. Strongly inferred: compatibility of the component arithmetic inherited from executable-backed frozen specifications. Unknown: original field traversal, inter-affector composition, frame cache reuse and active precision policy. Timing-dependent mid-call cancellation and concurrent mutation are not claimed as executed tests. No original executable execution, Godot/GPU integration or sibling-round dependency is involved.

Replay with [Invoke-Field.ps1](../poc/round54-tests/Rendering/Invoke-Field.ps1), supplying explicit Python, original executable, asset/config roots, cached packages and dotnet paths. A fresh private output root is required. The producer checks executable SHA-256 before using original inputs; original-derived fixture bytes stay private. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R54_PER_CELL_FAMILY_HEIGHT.md) for input identity and next probe.
