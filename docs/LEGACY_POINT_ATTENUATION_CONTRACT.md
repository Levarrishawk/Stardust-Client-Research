# Original positive point attenuation contract

## Source and backend observation

Original vertex_program/include/functions.inc (priority40,12318 bytes,SHA256CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15) computes inverse distance from lengthSquared(delta). Shared_program/functions.inc (priority40,2398 bytes,SHA256A9E9C7A2E6D41545BDBD0F08A57F14FD4D5C39899788F7FB94B510B20B85B09A) defines that helper as dot(delta,delta). Both point functions form factors(1,r,r²,wFactor) and use reciprocal dot with no denominator cap. Diffuse-only wFactor=1/r; specular-capable wFactor=1. The configured backend sets the fourth coefficient to0 in both cases, so positive finite behavior reduces to1/(a0+a1*r+a2*r²).

s207_r.dll0x62A0A3C0 clears28 float4 records at0x62A0A462. Specular-point coefficients load object+0x100/+0x104/+0x108 at0x62A0A76D/779/787;0x62A0A78D writes fourth coefficient0. Diffuse-point loads at0x62A0A82D/836/83F;0x62A0A845 writes fourth coefficient0. Empty points receive constant coefficient1 at0x62A0A7C8/851, with zero colors from the clearing pass. Upload0x62A0A86A/873/875 uses vertex constants c16 through c43 (28 vectors). Original constants include(priority40,3339 bytes,SHA256C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9) confirms matching slot counts/record ordering and material c11/light c16 roots.

**Confidence: Confirmed** source equation, coefficient order and fourth-zero packing. Source labels constant/linear/quadratic now have direct client source plus instruction evidence. Exact RSQ/device precision and source singular behavior remain Unknown.

## Owned boundary and tests

LegacyPointAttenuation accepts positive finite squared distance, nonnegative finite coefficients and positive finite denominator/result. It implements the reciprocal without the earlier0.01 preview cap. Malformed/singular inputs reject; this is explicit replacement policy. Sixty original coefficient/distance cases and48 synthetic cases independently agree within relative error5.884e-8. Hand cases isolate constant/linear/quadratic order and a near quadratic response10000 that distinguishes the old capped100. The full original157-source union is unchanged; helper/constant source inspection is a separate static audit, not an added runtime compiler dependency.

verify_vertex_light_source_static.py hash-checks configured input, baseline DLL and all three source files, then verifies20 instruction anchors. Existing660 tangent cases/576 selected lighting cases, original route and native negative gates pass. [Source checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_POINT_ATTENUATION.md).

## Other vertex-light source findings and limits

The combined helper starts diffuse at material emissive and specular at0, conditionally includes the directional specular-capable slot when dot3=false, always includes two diffuse directionals and the specular-capable point, then four diffuse-only points. The selected normal VSH adds ambient and multiplies summed vertex specular by material specular exactly once. The source calls HLSL lit for directional/point specular with material power. It does not itself establish generated instruction choice or high-power clamp precision. Microsoft's [HLSL intrinsic description](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-lit) and [vertex LIT instruction description](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/lit---vs) differ in documented exponent/boundary details; these are correlation evidence only. Owned literal-power behavior remains explicitly modern until the selected original compiler output is recovered.

## Retained recovered-slot adapter

FromLegacyCell now enables the closed uncapped positive-distance equation. The older authored-first helper retains its explicit cap. CPU/GPU flags and arrays are verified at publication. Coincident points (distance<=1e-6) and nonpositive denominators contribute0 in the retained adapter; this finite omission policy is explicitly modern and does not claim original singular behavior. No basis, light selection, source, or resource ownership is changed by this equation.

Sixty-four recovered GPU cases cover near/far/coincident/zero-denominator points, both owner poses, facing directions, diffuse/specular readout and all8 selected slots. Maximum display error0.003189; old cap policy differs by RGB-vector0.148170 in the fixture. Previous32 modern capped cases continue to pass. Full original route/camera/live customization/retained owner publication/headless/cancellation and capture inspection pass. [Adapter checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_POINT_ATTENUATION_PREVIEW.md).
