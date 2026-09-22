# Analysis checkpoint: PASS to D3D9 compiler

Date: 2026-09-12

## Scope

This continuation analyzed `Stardust.exe` and its configured Direct3D 9 implementation module `s207_r.dll` to identify how anonymous PASS fields become actual graphics behavior. It did not begin replacement-client implementation.

## Binary identity

- `Stardust.exe`: SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
- `s207_r.dll`: SHA-256 `D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D`, x86 image base `0x62A00000`

## Confirmed findings

1. The engine/raster boundary is a shader compiler as well as a draw API. Main-executable graphics slots `+0xA0/+0xA4` select an implementation and apply a pass. Backend functions `0x62A0F3C0` and `0x62A0E9D0` compile each logical PASS into a cached 0xA4-byte D3D9 record. `0x62A0F4B0` and `0x62A0EF30` apply the selected record.
2. Implementation-level state is stored as `(D3DRS,value)` pairs and applied by `0x62A0B9B0`, followed by pixel-shader selection. Per-texture sampler state is stored as `(D3DSAMP,value)` pairs and applied by `0x62A0E910` after binding the texture.
3. Named `StaticShader` exports prove PASS offsets: `+0x7C` material tag; `+0x40/+0x44` texture-factor tags; `+0x48` scroll tag; `+0x30/+0x34` alpha-test enable/reference; `+0x4C/+0x50` stencil-reference enable/tag.
4. Alpha-reference sentinels are `A255`, `A128`, `A001`, and `A000`; other tags are looked up in the StaticShader parameter set, defaulting to 1 on lookup failure.
5. PASS `+0x14` becomes a three-way compiled fog override. Application writes D3DRS 34 (`FOGENABLE`). Its broad role is strong; exact historical naming/value-2 policy remains open.
6. Fixed-function texture-stage dispatcher `0x00762C20` supports 0000/0001. Version 0000 reads 24 one-byte fields plus two U32 values; version 0001 reads the common first 15 one-byte fields, the same two U32 values, and the final one-byte field. The engine object remains a stable 0x60-byte record.

## Architecture consequence

The modern client should parse the versioned asset description into a GPU-neutral logical pass, resolve material parameters against each StaticShader instance, and compile/cache modern pipeline plus binding descriptors. It should not preserve either the legacy 0x80-byte engine object or 0xA4-byte D3D9 record layouts.

## Unknowns retained

- labels for remaining PASS 0009 booleans/enums and their exact D3DRS mapping;
- fixed-function texture-stage color/alpha operation and argument labels;
- reachability/overlay status of PASS 0010 and IMPL 0008/0009 assets;
- complete modern compatibility-shader generation rules.

## Progress estimate

- Expanded comprehensive whole-client ledger: **49%**.
- Replacement-relevant implementation research: **65%**.
- Asset/resource workstream: remains **63%**; this block advanced renderer/shader coverage rather than broadening formats.

These are conservative planning estimates, not function-count percentages.

## Exact resume point

Trace construction of the implementation-level D3DRS pair vector feeding `0x62A0B9B0`, then trace the source texture-stage record into the D3DSAMP pairs consumed by `0x62A0E910`. After state labels close, resolve live overlay reachability for PASS 0010 / IMPL 0008-0009.
