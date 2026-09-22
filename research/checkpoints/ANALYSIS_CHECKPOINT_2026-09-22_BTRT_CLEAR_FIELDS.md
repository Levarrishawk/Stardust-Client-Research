# BTRT CFBC clear-field semantics

Date: 2026-09-22

## Result

`CFBC` (`ClearFrameBufferRenderCommand`) contains three ordered enable/value
pairs: clear color plus packed ARGB; clear depth plus float32 depth bit pattern;
and clear stencil plus unsigned stencil value.

Loader `0x0086B560` reads six U32s, normalizing each enable member to Boolean.
Executor `0x0086B530` forwards them unchanged and in order to Graphics wrapper
`0x007558C0`. The previously traced backend slot is the D3D9 Clear path, where
the flags select target, Z-buffer and stencil clearing and the values populate
Color, Z and Stencil respectively.

No effective face BTRT uses CFBC, but the general executable-supported command
is no longer anonymous.

## Replacement-client requirement

Expose a renderer-neutral clear command
`{color?: ARGB8, depth?: float32, stencil?: uint32}`. A modern backend should
translate this to attachment load/clear operations rather than emulate an
immediate D3D9 call literally.

## Confidence

- Loader widths/order and unchanged graphics forwarding: **confirmed by code**.
- Color/depth/stencil mapping: **confirmed by the established D3D9 Clear
  backend signature**.

