# October 1, 2026 — Skeletal shader deformation

## Question and stopping condition

Can owned original MGN shader maps and complete PIDX influences produce correct positions/normals/tangents under explicit rigid diagnostic palettes? Stop at independent original-byte agreement and unequal-map/negative fixtures. No animated original pose or GPU deformation claim.

## Findings and implementation

Added engine-neutral SkeletalShaderDeformation; returns owned shader positions, optional raw weighted normals and tangent vectors with unchanged W. Reuses validated full-weight position path; direction groups follow position index while authored direction values follow their separate maps. No influence cap reduction or sum/vector normalization. Input counts/indices/nonfinite components/palette rigidity reject.

Native distinct position/normal/tangent map fixture,25%/75% rotation/translation, raw nonunit output, absent maps and malformed map gates pass with full suite. Original12 sources/329,296 bytes once,53 joints/52 names and4,362 prior position checks remain passing. Independent pinned original-byte oracle checks5,370 shader vertices across bind/common rigid/split-rotation poses and all position/normal/tangent components. Maximum error4.284464e-7 below2e-5 tolerance. Original data unchanged.

Confirmed bounded operation and executable-backed influence association; original animation/matrix producer remains open. No registry/research/function/protocol metric changes, original execution, server contact, commits or publication. [Durable contract](../../docs/SKELETAL_SHADER_DEFORMATION_CONTRACT.md).

## Next action

Publish diagnostic posed geometry on the main thread, verify upload and bind reset, then inspect GPU capture before integrating any animation source.
