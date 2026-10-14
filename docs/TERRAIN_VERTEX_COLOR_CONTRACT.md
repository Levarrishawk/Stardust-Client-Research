# Terrain vertex color contract

[TerrainVertexColor](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainVertexColor.cs) converts one supplied three-byte RGB terrain sample into an immutable modified RGB, normalized RGBA vector and diffuse ARGB word. The caller supplies the baked-mask byte, explicit modifier enable flag and both endpoint RGB values. This engine-neutral scalar stage can precede [patch assembly](TERRAIN_PATCH_ASSEMBLY_CONTRACT.md); this milestone does not connect those stages.

## Evidence and recovered order

Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All addresses are VAs. The [bounded checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R34_TERRAIN_COLOR.md) records reproduction and uncertainties.

| Observation | Interpretation and confidence | Compatibility consequence |
| --- | --- | --- |
| Builder `008618D0` reads three consecutive bytes from source RGB data at `source+14`, with row width at `source+0C`; `00861DFE` supplies alpha bits `3F800000`. | Source order R,G,B; alpha 1. Confirmed with helper stores. | Preserve byte order and opaque alpha. |
| `00AB87B0..00AB87F7` multiplies unsigned R,G,B by F32 constant `015EAB04`, bits `3B808081`, and stores F32 A,R,G,B. | Normalization uses the rounded reciprocal, not division by a mathematically exact 255. Confirmed. | Modern receipt projects R,G,B,A into `Vector4`; preserve normalized component bits. |
| `00ABA1C0..00ABA224` multiplies each normalized channel by F32 255 at `015DD8CC`, invokes `0131E27C`, then packs low bytes A,R,G,B. | Truncation toward zero, low-byte assembly, no clamp in this helper. Confirmed. | Returned word is `AARRGGBB`; bounded generated channels preserve original bytes. |
| `00861E25` calls enable getter `0084A4D0`, reading `01924B80`. Disabled branch retains the initial packed word. | Modifier is explicitly conditional. Confirmed. | Caller resolves enable state; no default enable policy. |
| `00861E4A..00861EAF` reads mask row stride `source+84` and bytes `source+8C`, compares against zero, and selects `01945AA4` for zero or `01945A88` for any nonzero byte. It supplies F32 bits `3F19999A`. | Binary endpoint selection with fixed 0.6f interpolation. Confirmed. | Mask magnitude contributes no weight. Mask is a baked lighting/shading input, not passability or ALP blend weights. |
| `00AB88B0..00AB8938` computes signed endpoint minus source, multiplies by the supplied factor, calls the integer helper, and only then adds the source byte. | Per-channel `source + trunc((endpoint-source)*0.6f)`. Confirmed. | Preserve truncation before addition; descending colors differ from truncating the completed sum. |
| `00861EB2..00861ECA` normalizes and packs the modified RGB bytes again. | Modification occurs in the three-byte domain. Confirmed. | Preserve the intermediate RGB, then normalize and pack. |
| `00AB8610..00AB8621` writes zero to `01945AA4..AA6`; `00AB8760..00AB8771` writes FF to `01945A88..A8A`. | Initializer bodies establish black and white respectively. Confirmed; effective lifecycle remains open. | Endpoints remain explicit inputs until initializer registration and subsequent writes are closed. |

For example, source 5 toward endpoint 0 produces `5 + trunc(-5*0.6f) = 2`, rather than 1 from truncating the completed sum. Black toward white yields 153; white toward black yields 102. No clamp is necessary over the exposed byte domain. Neither endpoint alpha nor a caller-supplied interpolation weight exists in this API.

## Arithmetic and ownership boundary

The implementation uses F32 normalization and an exact double representation of the product of a bounded signed integer and the recovered F32 interpolation constant. It truncates that product, adds the original byte, then normalizes the resulting byte. Packing multiplies the stored F32 by double 255 before truncation. These choices are **Confirmed equivalent within the tested byte domain under round-to-nearest**: an independent oracle compares all 511 signed differences under binary32 and exact rational arithmetic, and all 256 normalized bytes under binary32 versus exact wider packing. There are no discrepancies. The relevant magnitudes have no exponent-range, overflow or subnormal ambiguity.

[The previously recovered control-word policy](SWITCH_TEXTURE_CLOCK_CONTRACT.md) configures PC24/round-to-nearest on the ordinary frame route. The actual control word and intervening overrides at this terrain callback remain **Unknown**. This milestone establishes finite-domain arithmetic equivalence under the stated policy, not execution-observed callback state or unrestricted float-color conversion parity.

All inputs and receipts are value types. There are no borrowed buffers, allocations of native/GPU objects, retained callbacks or external mutations. RGB byte inputs exclude invalid finite values and out-of-range channel values by construction. The public API intentionally exposes no general float-color packer; malformed NaN/infinity/clamping behavior outside the terrain byte domain is not claimed. The original generic pack helper's low-byte wrap is therefore not generalized into a replacement API.

## Reproduction and checks

The standalone [runner](../../SWG-Godot-Client/poc/round34-tests/Rendering/Invoke-Color.ps1) accepts explicit executable, cached package directory, Python and .NET paths:

```powershell
.\poc\round34-tests\Rendering\Invoke-Color.ps1 -Exe C:\Stardust-DEV\Stardust.exe -Packages <cached-static-analysis-packages> -Python <python.exe> -Dotnet <dotnet.exe>
```

The runner hash-gates input before address consumption, checks 29 instruction anchors and two constant words, builds a linked-source net8 project with cleared package sources, and compares native output against an independently written rational/struct Python oracle. Both projects keep generated receipts and build output in ignored private storage. It does not execute the original executable.

Results: 720,965 native assertions; every ordered byte pair in each RGB channel, zero and representative nonzero masks, all 256 mask values with distinct black/white endpoints, disabled behavior for all source/mask pairs, all eight RGB corners with both enable states, normalization bits, truncation boundaries, opacity and value ownership. The 4,980,992-byte output digest is `dc3e8238521660ad0e5a346ea04215ccb7e1b810cf51538ade3941ba62104cef`. Two wrong-identity inputs are rejected before static decoding. Invalid-finite runtime test count is zero because float inputs are absent; this is a type boundary, not evidence of generic legacy float rejection.

## Remaining question and next probe

Close registration and lifetime of the black/white initializer functions, enumerate writes to all six endpoint bytes, and trace callers of enable setter `0084A4C0`. Then confirm the control-word/thread route reaching the terrain builder. Until that bounded investigation closes, the caller must explicitly resolve enable state and endpoint values. Height/rule evaluation, source RGB generation, lighting-mask production, material binding and patch integration remain outside this converter.

## Round35 bounded follow-up

Round35 closes CRT startup-table registration for black/white initializer bodies and inventories six literal endpoint writes. Endpoint addresses passed through helper calls are classified separately; literal references cannot prove absence of computed aliases. Enable storage is initially zero-filled, with no direct setter call or exported setter established. Effective runtime enable/endpoint values and callback arithmetic state remain explicit. See [runtime policy](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md).
