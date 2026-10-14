# BC6H material bundles — 2026-09-30

## Bounded question

Can separately verified BC6H half images survive actual static material loading, aggregate caches and prototype host MAIN samplers without8-bit/gamma conversion? Closed for typed2D image/material inspection; viewport tone mapping is separate.

## Inputs and observations

The standards-only40-fixture BC6H corpus and dependency pins from [the HDR decoder checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_HDR.md) are reused. A new independent Python writer creates small synthetic MESH/SSHT/EFCT/CMPA material inputs. No original executable or live server is run. Fixture identities are printed to private manifests; original assets are not needed for this proof.

**Confirmed:** core material bundles retain HDR images in a separate dictionary and charge6 bytes/pixel alongside RGBA's4 bytes. Duplicate encoded HDR content shares one immutable description; mixed96+64-byte budgets pass exactly160 and fail159. Typed format detection still invokes the full HDR parser; typeless94 remains rejected. Counts and native half-hash output now include the HDR family explicitly.

**Confirmed:** independent prototype host bcdec agrees on all 264 mips. Forty fixtures feed80 material templates; paired templates share one actual RGBH texture and a linear MAIN sampler. Explicit sampler-object readback matches every half byte and material code contains no source_color conversion. Values include29,832 negative and 45,349 above-one components. Actual GPU and distinct headless paths pass; gpu=False is never called actual GPU verification.

The first added sampler probe assumed mesh nodes were direct component children and failed with InvalidCastException. It was corrected to the observed component child/mesh child structure and rerun successfully. The failure remains in private evidence; successful binding-gpu/headless outputs are the closure gates.

## Validation and consequences

Native ownership, single lookup, mixed image budgets and existing malformed decoder scenarios pass. Zero-warning/error prototype host build passes. Bounded GPU and headless material verification pass 40 fixtures/264 mips/80 templates. Original YT-1300 authored texture source/pixel/effect agreement still passes87 sources/321 mips/39 records. Nonfatal certificate-store startup noise persists. Workspace and diff validation follow closure.

Canonical [material contract](../../docs/BC6H_MATERIAL_BUNDLE_CONTRACT.md). The modern extension does not prove legacy client DX10 acceptance. No tone mapping, full lighting or negative-infinity guarantee is claimed. Metrics/registry remain unchanged. Next exact probe: palette resource bytes, packed color ordering and authored factor selection under bounded static corpus agreement.
