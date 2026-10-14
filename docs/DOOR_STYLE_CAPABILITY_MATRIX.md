# Installed door capabilities and rigid detail leaves

## Reproducible installed inventory

[The capability producer/verifier](../../SWG-Godot-Client/tools/python/inventory_door_capabilities.py) independently decodes the effective table and POB references, then reconciles the native [bounded loader probe](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/DoorCapabilityScenarios.cs). Input root is `C:\Stardust-DEV`; configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 descending-priority mounts. Effective table SHA-256 `C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7`, 124 rows. The POB inventory has 291 effective layouts and one masked layout; first winning archive entry controls each path.

| Current leaf asset/controller result | Style rows | Named POB references |
|---|---:|---:|
| Accepted normal primary leaf with authored box | 91 | 1,497 |
| Secondary leaf outside this slice | 18 | 825 |
| Unsupported vertex declaration in primary graph | 10 | 80 |
| Primary missing/masked in effective overlay | 3 | 0 |
| Force-field variant | 1 | 0 |
| Flip flag without a secondary leaf | 1 | 46 |
| Total | 124 | 2,448 |

All 2,448 named occurrences have eligible raw flags in this corpus. Table presence, asset/controller acceptance, authored occurrence and usable paired floor instance are distinct measures. The 91 accepted rows do **not** imply complete style functionality: frame appearance, effects/audio and legacy narrow phase remain separate. Two table rows declare frame assets; this slice renders only the primary leaf. No named occurrence falls back to a missing style in this installed inventory, while the instance loader still rejects fallback explicitly.

The former direct-mesh boundary accepted 53 rows / 713 named references. `--direct-only` reproduces that boundary. Normal candidates have 101 present primary assets: 58 terminate in MESH and 43 in DTLA. Of the DTLA candidates, 38 load all bands successfully; five encounter unsupported mesh declarations. Five direct MESH candidates also encounter unsupported declarations.

## Rigid detail-leaf extension

[SingleLeafDoorAssetReader](../../SWG-Godot-Client/poc/Stardust.Core/Assets/SingleLeafDoorAsset.cs) now accepts a validated direct MESH or a DTLA with direct static-mesh bands. The collider uses the root APPR primary EXBX, independent of visual LOD selection. It rejects missing/null/non-box bounds, incomplete graphs, graph/band diagnostics, transformed/nested bands and band geometry outside root bounds. Every band must fit the authored box within 1e-4 numeric tolerance; at most one million band vertices are inspected. This is an owned conservative envelope policy, not recovered original narrow phase.

Observation: all 38 newly accepted installed DTLA rows have EXBX roots and 3,556 independently checked source vertices inside those envelopes. Confidence: Confirmed byte/geometry acceptance for this corpus. Consequence: visual band changes must not resize the physical door or change actor clearance. No extent is synthesized from the currently selected mesh.

Godot's existing geometry adapter selects visual bands from current camera/leaf pose. Standalone, crossing and persistent door inspectors now invoke that selector. Physical runtime state retains the same root box. [Synthetic gates](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/DetailDoorLeafScenarios.cs) cover stable bounds under changed band geometry, malformed/incomplete graphs, nonidentity band transforms, oversized geometry and missing dependencies.

## Verified original detail leaf

`door_tato_filler_s01` primary `appearance/door_tato_filler_s01.apt`, SHA-256 `65661A0EF2BDA1865A988E81D79F75322548DFE4C895239909A840ACE96ABFE4`, resolves to a two-band DTLA. Its authored root box is minimum (-1.1,0,-0.1), maximum (1.1,3.1,0.1). Root LOD SHA-256 is `F362DEFFADCD248A5B848939621812A9B9E18016014B7CD9DDFC903C2F60A0B7`. The independent inventory validates child identities and root bounds.

Headless/GPU standalone controls pass open/close, permission, pose, clearance and camera band-switch checks; private capture inspected. The cantina persistent actor regression also passes. There are 782 directed DTLA reference candidates with matching reverse pose/style and floors declared on both sides; this is a **candidate count**, not floor-pairing or movement coverage.

## Next probe

Validate a selected DTLA POB pair end to end: exact floor-edge pairing, authored transform, controller clearance, persistent bidirectional actor trajectory and Godot controls. The yacht provides matching authored candidates. Secondary-leaf/flip semantics and the ten mesh declaration failures remain later bounded questions. Research scores and executable/protocol counts are unchanged.
