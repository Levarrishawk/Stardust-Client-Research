# Component template and neutral geometry checkpoint — 2026-09-30

## Question and stopping condition

Can the owned native core preserve deployed CMPA parts/transforms and instantiate a bounded static component graph in prototype host, with unsupported children explicitly reported? Stop after independent corpus agreement, ownership/negative fixtures and one real component GPU inspection. Full material and particle behavior is outside this checkpoint.

## Inputs and evidence

- Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rechecked during this overnight run. Static observations from the [CMPA investigation](ANALYSIS_CHECKPOINT_2026-09-22_COMPONENT_APPEARANCE_CMPA.md), loader VA 00798E40 and runtime VA 007A0320 establish the schema and composition owner.
- Configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 TREE0005 mounts; existing independent CMPA inventory and overlay parser.
- Native .NET 8 core/smoke project, independent Python readers and prototype host 4.7.2 .NET x64 with host OpenGL GPU. No original executable or live socket is run.
- Component `appearance/component/frn_tato_vent_pillar_l0.cmp`, priority 45, SHA-256 `252323583140AF29D647CD757B449ABB61BB5658F38C11AAF8C6D626E273F4E3`; four-band child `appearance/lod/frn_tato_vent_pillar_l0_c0.lod`, priority 45, SHA-256 `A58D8E2615F3912203F48828E6E343A170D82FD84A399447AFAF901A42A2C507`.
- Real garage component `appearance/component/mun_all_garage_s01_u01.cmp`, priority 30, SHA-256 `1FE0DF11A1A1B6506AA18B1672F87A03F6C892D48C91A72D63E4D3B170D64B12`, has RADR INFO=1 plus IDTL geometry: four vertices/six U32 indices. This disproved the initial implementation assumption that CMPA RADR only contains INFO; corrected parser preserves optional geometry.

## Findings and confidence

**Confirmed:** all 165 inventory templates (13 v0003, six v0004, 146 v0005) decode. Independent Python/native comparisons match 779 ordered paths and all 9,348 transform float bit patterns. Source identities, priorities, versions and part counts also match. This does not establish every APPR semantic or every child implementation.

**Confirmed implementation policy:** immutable template sharing, separate placement transforms, cycle diagnosis, strict root identity, owned geometry and fatal aggregate limits pass deterministic synthetic tests. Missing/unsupported children are reported locally. Asymmetric transforms prove local-before-parent composition rather than roundtrip-only agreement.

**Confirmed:** the vent component loads seven sources and four mesh instances, reporting one unsupported particle child. All child geometry hashes match independently:

| Level | Vertices | Triangles | Geometry SHA-256 |
|---|---:|---:|---|
| L3 | 28 | 16 | `8E37A5CBE54B8F79C041C38EB3D1FB9CA43D70C0B1E4553A231ED2DD7299962B` |
| L2 | 118 | 107 | `ACD6C810963D0B2535291E22BF918019258A871F33782C6EC752A64DF7EB4C5B` |
| L1 | 240 | 236 | `B5F11FAD992375AE1020C42E711CC614A4CE0BD9B2C9A754908389F059B7D58C` |
| L0 | 423 | 428 | `1C637399809AA7ED1DD6982098CD3D6FD4E7044AF505E94C2CDC8B8F6A339312` |

Prototype host headless and hidden GPU runs pass. Geometry readback, midpoint switches and the existing GPU UV/front-face fixture pass. The captured neutral inspector was visually inspected. The authored nonidentity transform belongs to the omitted particle, so this real sample does not verify a transformed visible mesh; the asymmetric synthetic boundary fixture supplies that evidence.

**Unknown/deferred:** RADR label, complete APPR extents, original materials/particle presentation, mirrored/singular culling, live camera/PIVT distance, readiness/collision/visibility fan-out and lazy runtime lifecycle.

## Validation and limitations

Native smoke suite and prototype host build pass. One build attempt collided with the running corpus verifier's DLL lock; retry after verifier completion passes with zero warnings/errors. Wrong-root identity is rejected. A garage graph probe correctly reports unsupported mesh parts rather than claiming a rendered assembly: its mesh children include sorted-index records and additional coordinate declarations. The existing prototype host root-certificate warning remains unrelated to the successful offline process exit.

Durable contract: [component appearance](../../docs/COMPONENT_APPEARANCE_CONTRACT.md). Research scores and address/protocol registries are unchanged.

## Next probe

Inspect the garage's sorted-index grammar and observed declarations 0x5205/0x35305. Preserve raw auxiliary coordinates and sorted lists with independent byte/hash fixtures, then retry the nested garage graph. Keep neutral geometry inspection separate from faithful material rendering.
