# Round 34 terrain vertex color

2026-10-03. Question: what exact source-byte, baked-mask, normalization and packing operations produce terrain diffuse ARGB, and can a bounded engine-neutral scalar reproduce them? Stop condition: recovered kernel with explicit unresolved producers, independent finite-domain oracle, native checks and unchanged baseline audit.

## Inputs and method

Baseline SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` was freshly checked before PE loading. Static tools: cached pefile and Capstone; native tools: cached .NET 8 SDK 8.0.300; independent oracle: Python struct/Fraction/hashlib. No original process execution, server contact, assets copied into curated output, GPU sweep, staging or publication occurred.

Orientation included START_HERE and latest named session, current status/ledger/unknown terminology, WORLD_TERRAIN_ARCHITECTURE, the 2026-09-21 vertex/blend checkpoint, lighting-mask correction, curated color-function rows, existing PackedVertexColor, and accepted topology/geometry/assembly contracts. Baseline manifest source HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818` supplied 2,868 verified files. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5` with empty index. Canonical ledger/status reconciliation is the lead's responsibility; this worker owns only the new bounded artifacts.

## Evidence and findings

The [durable contract](../../docs/TERRAIN_VERTEX_COLOR_CONTRACT.md) contains exact addresses, observations, confidence and consequences. Builder `008618D0`, its color span `00861DFE..00861ED7`, helpers `00AB87B0`, `00AB88B0`, `00ABA1C0`, integer helper `0131E27C`, enable accessor `0084A4D0` and black/white initializer bodies establish:

- R,G,B source bytes, alpha 1, rounded F32 reciprocal bits `3B808081`, intermediate F32 A,R,G,B, scale 255 and truncating low-byte ARGB packing: Confirmed.
- Enabled modifier: zero mask selects endpoint `01945AA4`; any nonzero selects `01945A88`; factor bits `3F19999A`: Confirmed.
- Signed delta multiplication is truncated before the original channel byte is added: Confirmed. No interpolation of the completed float color and no mask-magnitude weighting.
- Endpoint initializer bodies write black and white: Confirmed. Startup registration, complete write/lifetime audit and enable setter producer chain: Unknown; public endpoints and enable flag remain explicit supplied inputs.
- Binary32 versus wider product/pack equivalence for the complete bounded byte domain under round-to-nearest: Confirmed by independent enumeration. Actual terrain callback control word: Unknown; prior PC24/nearest installation evidence is correlated with the ordinary frame route only.

Compatibility impact: standalone scalar [TerrainVertexColor](../../poc/Stardust.Core/Geometry/TerrainVertexColor.cs) supplies an immutable modified RGB, normalized RGBA vector and ARGB word. It preserves recovered operation order and has no engine dependency, retained source buffer or callback. Byte-only inputs admit no NaN/infinity and require no clamp. Source RGB/mask generation and effective configuration are still caller boundaries. The existing packed-color utility and production/Godot code are unchanged.

## Verification

[Standalone runner](../../poc/round34-tests/Rendering/Invoke-Color.ps1), [static/rational oracle](../../poc/round34-tests/Rendering/verify_color.py), and [native scenarios](../../poc/Stardust.Core.SmokeTests/TerrainVertexColorScenarios.cs) reproduce the result without proprietary fixtures in Git.

29 static instruction anchors, two constant words, 511 signed-delta precision cases and 256 normalized-pack cases pass. Native build: zero warnings/errors. 720,965 assertions pass, with all 65,536 ordered channel pairs independently covered in R,G,B, representative zero/nonzero masks, all 256 masks with distinct endpoints, disabled source/mask matrix, eight RGB extrema, value ownership, normalized bit patterns and signed truncation boundaries. Independent output matches 4,980,992 bytes and SHA-256 `dc3e8238521660ad0e5a346ea04215ccb7e1b810cf51538ade3941ba62104cef`.

Runtime invalid-finite negative cases: zero, because public inputs contain no floats. Two malformed executable identity cases fail before any address decode. This does not assert the original generic float packer's malformed-input policy. Workspace validator, focused links/whitespace and whole-baseline preservation audit are recorded in the handoff; no existing or unrelated files are part of the change.

## Precise resume point

Trace startup references to `00AB8600/00AB8610` and `00AB8750/00AB8760`; enumerate instruction writes to `01945AA4..AA6` and `01945A88..A8A`, then callers of `0084A4C0`. Close effective configuration before integrating the color stage with patch assembly. Independently audit the thread/control-word route into `008618D0`; do not infer it from another callback. No coverage percentage or readiness denominator changes are justified here.
