# Blueprint texture renderer command set

Date: 2026-09-22

## Result

The command polymorphism inside the eight live BTRT/0001 face templates is now
bounded and named from executable dispatch plus RTTI. The installed face corpus
uses a much smaller subset than the general dispatcher supports.

## Render-command dispatch

`blueprint_texture_renderer_render_command_dispatch_VA_0086BC20` accepts:

- `CFBC` -> `ClearFrameBufferRenderCommand`
- `SRSC` -> `ShaderRenderSetCommand`

An SRSC record contains `INFO { U32 shaderTemplateIndex, U32 commandCount }`
and dispatches nested primitive render commands through `0x0086BCF0`:

- `RTFC` -> `TriFanRenderCommand`
- `RTLI` -> `TriListIndexedRenderCommand`

All eight effective templates contain 37 SRSC records and 37 nested RTFC
records. None uses CFBC or RTLI. The live face renderer therefore needs only
shader selection plus non-indexed triangle-fan draws for its first fidelity
implementation, while the decoder should retain the other supported types.

## Prepare-command and operation dispatch

`0x0086BDD0` parses each `PCMD`. It optionally loads a `COND` expression and
then optional `TOPS` (true operations) and `FOPS` (false operations), each with
an INFO count. Individual operations are dispatched by `0x0086DFC0`:

| Tag | RTTI-backed meaning |
|---|---|
| `NOP ` | `NoOperation` |
| `SSTC` | `SetShaderConstantTextureOperation` |
| `SST1` | `SetShaderTexture1dOperation` |
| `SST2` | `SetShaderTexture2dOperation` |
| `STF ` | `SetShaderTextureFactorOperation` |
| `STFA` | `SetShaderTextureFactorAlphaOperation` |
| `STFP` | `SetShaderTextureFactorFromPaletteOperation` |

The effective corpus has 67 PCMD/TOPS records and no COND/FOPS branch. Its 67
operations are exactly 20 SSTC, 17 SST1 and 30 STFP. Thus the live face subset
requires constant texture selection, one-dimensional texture selection and
palette-derived texture-factor setup before drawing.

Direct loader shapes are also preserved: SSTC reads three U32 values; SST1
reads three U32 values, a cstring and a final U32 before resolving a variable
factory; STFP reads U16, U32, a long resource/variable string, a second cstring
and a Boolean before resolving its palette/variable binding. Friendly labels
for each numeric field require runtime-consumer correlation and remain open.

## Live runtime semantics

The three live operation vtables close the important operands:

- SSTC stores `textureTableIndex, shaderTextureTag, sourceTextureIndex`.
  `0x0086D3F0` fetches that texture and binds it to the shader tag.
- SST1 stores `textureTableIndex, shaderTextureTag, firstSourceTextureIndex,
  customizationVariableIndex, sourceTextureCount`. `0x0086D5E0` reads and
  clamps the integer customization value, selects
  `firstSourceTextureIndex + value`, and binds the resulting texture.
- STFP stores `textureTableIndex(U16), shaderTextureTag, palette,
  customizationVariableIndex` plus its load-time flag. `0x0086DF70` clamps the
  customization value to the palette, obtains the selected color and binds it
  as the shader texture factor.

`0x0086B720` resolves SRSC's shader index, iterates that shader's passes and
executes the nested render commands for each pass. `0x0086BBA0` resolves RTFC's
single U32 as a vertex-buffer index, binds it and issues the triangle-fan draw.
These are **Confirmed by runtime code** and remove the prior generic operand
uncertainty for the complete live subset.

## Replacement implication

The first compatible BTRT executor can be deliberately narrow and data-driven:

1. evaluate the unconditional TOPS list;
2. implement SSTC, SST1 and STFP over a renderer-neutral material instance;
3. select the indexed shader template named by SRSC;
4. issue RTFC triangle-fan draws into the 256x256 destination;
5. expose the resulting texture to the MGN `MAIN` binding.

This is substantially smaller than emulating arbitrary D3D9 rendering, while
remaining faithful to every effective face template.

## Confidence

- Dispatch tags and class meanings: **Confirmed by code and RTTI**.
- Live command/operation counts and absence of conditional branches:
  **Confirmed by all eight effective assets**.
- Operand roles for live SSTC/SST1/STFP/SRSC/RTFC: **Confirmed by runtime code**.
- Dormant command operands are closed separately. The STFP Boolean is now
  confirmed as part of customization `VariableFactory` identity: lookup keys
  on `(name, Boolean)`, while the operation retains only the resulting index
  and the render path never reads the flag. Its historic domain label remains
  unknown. See `ANALYSIS_CHECKPOINT_2026-09-22_STFP_FACTORY_FLAG.md`.
