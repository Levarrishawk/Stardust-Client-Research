# October 1, 2026 — Original sampled skeleton poses

## Question and stopping condition

How do sampled channel values compose with authored skeleton rotations/translations, and can that produce independently verified attached poses and full-weight deformation? Stop before rendered/timed playback.

## Observation and interpretation

Verified executable identity remains E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150. Existing private decompilation led to direct aligned instruction windows from007E67E0 and0082B8D0; curated [contract](../../docs/SKELETON_ANIMATION_POSE_CONTRACT.md) records exact sites and observed order. Getter offsets tie the operands to RPRE/RPST/BPTR/BPRO. Quaternion call sequence yields RPST*(sample*BPRO*RPRE); sampled translation adds to BPTR. Confidence: Confirmed for bounded composition. Original palette segment/cache ownership and animation modifiers remain outside this result.

Constructor00822FC0 queries clip records for each requested skeleton name, explaining why the unused `rifle` need not become a body joint. Original sample has37 matches and one missing body name `hold_l`. The replacement defaults to rejection for missing channels; this preview explicitly requests bind fallback and records missing/unused names. No original fallback identity claim is required.

## Implementation and verification

Added engine-neutral local composition and application to one selected skeleton of the existing attached binding. Native noncommuting hand vectors and malformed sample/name gates pass. Original15-source/726,134-byte closure unchanged. Five queried frames yield265 joint poses and8,950 shader vertices; independent source/double oracle maximum matrix9.958999709347438e-7 and vertex/normal1.3300641358293674e-6, below2e-5.

No score/canonical-name change, original execution, server contact, publication or commit. Exact x87 math, runtime modifiers, complete animation graph and root motion remain unsupported.

## Resume

Upload explicit original idle poses through the existing main-thread deformation adapter, compare complete GPU readback and inspect a captured frame. Preserve neutral materials and explicit missing/unused diagnostics.
