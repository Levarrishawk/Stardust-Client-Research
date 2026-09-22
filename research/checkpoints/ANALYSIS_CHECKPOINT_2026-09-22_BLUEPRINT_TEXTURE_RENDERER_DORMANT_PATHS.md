# Blueprint texture renderer dormant paths

Date: 2026-09-22

## Result

The executable-supported BTRT command graph is now closed beyond the smaller
live face-template subset. These paths do not occur in the eight effective
TRT resources, but their parsing and runtime behavior are directly recoverable.

## Conditional prepare commands

A PCMD may contain an optional `COND`, followed by a counted `TOPS` list and a
counted `FOPS` list. The condition dispatcher `0x0086D0B0` accepts:

- `TRUE` -> always true;
- `FALS` -> always false;
- `OR  ` -> INFO U32 child count plus recursively parsed conditions;
- `AND ` -> INFO U32 child count plus recursively parsed conditions;
- `CVTP` -> accepted through the same always-true construction path as TRUE.

RTTI confirms `TrueAlwaysCondition`, `FalseAlwaysCondition`, `OrCondition` and
`AndCondition`. Evaluators `0x0086CD10` and `0x0086D070` implement ordinary
short-circuit OR and AND respectively. Empty OR is false; empty AND is true.
No separate CVTP runtime class or payload is constructed. It is therefore a
legacy compatibility alias/no-op in this executable; a more specific historical
meaning must not be invented.

## Dormant prepare operations

The three operations absent from the effective face corpus behave as follows:

- `SST2` (`SetShaderTexture2dOperation`) reads a texture-table destination/tag,
  a first source texture, two customization variable names/indices and two
  dimension counts. Runtime clamps row and column independently and selects
  `first + row * columnCount + column`.
- `STF ` (`SetShaderTextureFactorOperation`) reads a destination/tag and one
  customization variable. Runtime binds that U32 value directly as the shader
  texture factor.
- `STFA` (`SetShaderTextureFactorAlphaOperation`) reads destination/tag, three
  constant RGB bytes and one customization variable. Runtime replaces the
  factor alpha with the low byte of that variable while retaining the constant
  RGB bytes.
- `NOP ` has no payload or effect.

## Dormant render commands

- `CFBC` (`ClearFrameBufferRenderCommand`) stores color/packed-ARGB,
  depth/float32 and stencil/U32 enable-value pairs and passes them to the
  graphics clear call. The established D3D9 backend signature confirms the
  order. See `ANALYSIS_CHECKPOINT_2026-09-22_BTRT_CLEAR_FIELDS.md`.
- `RTLI` (`TriListIndexedRenderCommand`) stores six U32 fields. Runtime resolves
  the first as vertex-buffer index and second as index-buffer index, binds both,
  then forwards the remaining four fields to the indexed triangle-list draw.

## Replacement requirement

The engine-neutral BTRT IR can represent all paths with a small Boolean AST,
true/false operation lists, seven operation variants and four render-command
variants. The live executor may initially implement only the corpus-used
subset, but the parser should preserve these dormant variants and reject
unsupported field values explicitly rather than silently discarding them.

## Confidence

- Tags, byte grammar and runtime behavior: **Confirmed by code**.
- Class meanings: **Confirmed by RTTI**, except CVTP.
- CVTP as accepted always-true compatibility path: **Confirmed behavior;
  historical intent unknown**.
- Clear pair labels: **Strongly inferred, not asserted**.
