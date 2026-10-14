# Inline face effect compatibility — 2026-10-01

## Bounded question and inputs

Can the engine-neutral reader retain both original face shader effect candidates without fabricating an external resource path or newer-version defaults? Closed for the two selected SHTM names; generated-face execution remains open. Original BTRT identity and object-root provenance are recorded in the [face description checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_BLUEPRINT_FACE_DESCRIPTION.md).

Reverified executable SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` and s207_r.dll SHA256 `D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D`, DLL image base62A00000. Used configured48-archive descending-priority overlay, existing raw TRE/IFF readers, static disassembler and prior private decompiler evidence. No original binary execution.

| Original member | Priority | Bytes | SHA256 |
| --- | ---: | ---: | --- |
| shader/texren_copy_c1a1.sht |38|639|99D9FCD4B64F48A7EE9104245418600E90B55A3BB82A70529EB99301F32A347F|
| shader/texren_alphablend_c1a1.sht |38|639|B496AF38BB609305F374B57783FF2DDEC65919DCB9D6E102CA6943CA3E69DF6B|

## Observations and interpretation

**Confirmed:** both SSHT0000 shaders contain one inline EFCT0001, two IMPL0003 candidates, each one PASS0007. First programmable PVSH0000/PPSH0001/PTXM0000; second fixed PFFP0001/STAG0000. No external effect NAME or SCAP. MAIN texture SourceFlag1 has no asset NAME: the binding is supplied at runtime. TCSS maps MAIN to0; TFNS contains MAIN/FFFFFFFF. Both programmable candidates use the same texren_copy_c1a1 vertex and pixel program names. These observations establish envelopes, not program semantics.

**Confirmed loader structure:** executable0075D490 reads IMPL0003 DATA5 before optional OPTN;00760DC0 reads PASS0007 DATA47, omitting logical+48 as well as +4D/+64/+68/+6C/+70.00763D10 dispatches PTXM versions;00763D90 consumes0000 U8 stage,U32 texture tag,U32 coordinate tag,sixU8 sampler bytes,finalU8. The coordinate word is consumed without a retained destination in this loader. Replacement retains all16 bytes independently of interpreted stage/tag.

**Confirmed backend correlation:**62A0B340 copies logicalPASS+20 into a cached blend byte;+28/+2C select table62B5145C for D3DRS19/20. Original raw4/5 select numeric5/6 (source-alpha/inverse-source-alpha);+24 selects operation table62B51488, raw0→1 (add). Logical+3C converts legacy A/R/G/B bit positions to Direct3D write bits.62A0B9B0 applies the pair list and forwards cached blend/write bytes through62A04EC0/62A04EE0.62A05620 normal draw branch at62A05765..85 sends cached blend to state27;62A05726..4B sends write mask to state168. Original shaders differ at logical+20 (copy0,alpha1), retain full write mask15, and differ in sampler metadata. Numeric enum labels come from [Microsoft render-state enumeration](https://learn.microsoft.com/en-us/windows/win32/direct3d9/d3drenderstatetype) and [blend enumeration](https://learn.microsoft.com/en-us/windows/win32/direct3d9/d3dblend).

**Unresolved:** alternate draw branch62A056AB..5721 adjusts alpha writes/blend according to global state. Need trace blueprint render-target setup before claiming its effective destination-alpha policy. Pixel/fixed combiner multiplication, addressing/filtering, destination formats and camera/UV raster mapping are not closed here.

## Implementation consequence

StaticShaderReferences default external-only API still rejects inline effects. Explicit DecodeIncludingInlineEffect owns exactly one external or inline effect; inline has empty EffectPath/non-null InlineEffect. Mixed/duplicate references reject. StaticEffectDecoder accepts validated IMPL0003/PASS0007/PTXM0000, retaining absent fields and raw metadata; no capability selection or implicit customization occurs. DecodeInlineEffect consumes the owned description. Existing static mesh renderer policy is unchanged.

## Validation

Full64-bit portable smoke suite passes, including asymmetric legacy field positions, post-DATA options, absent capability/flags/scroll, truncated prefixes, malformed15-byte PTXM and duplicate stages, opt-in/default/mixed-reference behavior. Both pinned original shaders pass native decoding, every truncated prefix and cleared-input ownership. Independent verify_inline_face_effect.py agrees on every25-field PASS, phase/versions, both candidates, program paths and raw texture/stage metadata. Prototype host project build succeeds with zero warnings/errors. Private extracted assets and raw evidence remain ignored.

## Next probe

Follow original BTRT shader programs and fixed stage compiler; derive texture×factor output and sampler semantics, then resolve render-target alpha-write mode. Load dependencies through the existing owned source cache, and prepare explicit supplied variable/factory identities before five-layer composition. Scores and canonical name/packet counts unchanged.
