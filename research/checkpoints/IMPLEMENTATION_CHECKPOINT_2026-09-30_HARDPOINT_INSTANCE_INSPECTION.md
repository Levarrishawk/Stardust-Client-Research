# Hardpoint instance placement and inspection — 2026-09-30

## Bounded question

Can owned APPR hardpoint records be composed through loaded static appearance instances without merging duplicate names or losing transforms? Close on asymmetric synthetic composition, two independent original graphs and prototype host reflected marker/visibility gates. This is static inspection, not recovered runtime virtual attachment lookup.

## Evidence and inputs

Unchanged baseline executable SHA `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 mounts. Uses [owned hardpoints](IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_HARDPOINTS.md), [component transforms](IMPLEMENTATION_CHECKPOINT_2026-09-30_COMPONENT_GEOMETRY.md) and [cell graph](IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_CELL_SCENE.md). Relevant existing labels/consumer constraints were reviewed. No original execution, live contact or registry rename.

YT-1300 `appearance/yt1300.pob`, SHA `8B4B59335CF34BA91DAA5CCB092AF60C9A996849DB29E77EB154FD4DF202DF4A`: 14 sources, 13 loaded instances, 14 placements and 168 composed float fields.

Garage `appearance/component/mun_all_garage_s01_u01.cmp`, SHA `1FE0DF11A1A1B6506AA18B1672F87A03F6C892D48C91A72D63E4D3B170D64B12`: 14 sources, 14 instances, 12 placements and 144 fields. Both independent graphs have zero local omissions. Maximum Python-double/native-float absolute discrepancies are below 4.2e-7; the verifier uses relative 2e-6 / absolute 2e-5 tolerances, not a bit-exact composition claim.

## Findings and compatibility consequence

**Confirmed implementation:** `AppearanceHardpointSceneQuery.Collect` performs deterministic preorder over all loaded instances, including inactive LOD branches. It preserves instance ordinal, owned authored child-index chain, owner path, original record identity and local-before-parent composed transform. Optional root transform stays engine neutral. Shared templates instantiate separate occurrences. FindAll/TryFindUnique require an instance ordinal and exact name; duplicate ambiguity is never resolved automatically.

Modern policies: 4,096 instances, 32,768 placements and depth 32; invalid limits, cycles, nonfinite composed transforms and aggregate exhaustion fail. No original runtime scale, appearance virtual override or parent-object attachment behavior is inferred.

**Confirmed host boundary:** optional prototype host component/interior markers use the same reflected transforms; all 14/12 marker world origins and basis axes match core placement. Axes share one owned mesh; markers inherit appearance/cell/LOD visibility and have a separate visibility toggle. Inspection annotations are excluded from geometry camera bounds. One selected readable label avoids overlapping whole-model text. UI lists the first 512 records; all bounded markers remain in the model. Labels include authored indices, so duplicate names remain distinct.

## Validation and reproduction

Full native smoke suite passes asymmetric parent/child transforms, shared templates, owned paths, name ambiguity/case, cycles, depth/resource limits and nonfinite inputs. `verify_hardpoint_scene.py ROOT CONFIG MEMBER SHA CORE_DLL PRIVATE_JSON` independently parses raw HPTS/CMPA/DTLA/APT/PRTO records, reconstructs transforms and verifies ordered identities plus all source hashes. Run on the two pinned roots above. Detailed positions and names remain private.

Prototype host build succeeds without warnings. Garage headless marker gates and YT-1300 GPU marker/host-coordinate capture pass. Final original capture was visually inspected; initial tiny-label and oversized-all-label captures remain private, followed by selectable single-label correction. The current full textured/floor scene still loads 87 sources, 33 textures/321 mips and shows 85 textured/three neutral surfaces. Existing certificate-store startup error remains nonfatal.

Use `Invoke-StardustAssetPreview.ps1 -InspectHardpoints` with `-Component` or `-Interior` and the pinned root. No skeletal/equipment/effect runtime attachment is claimed. Durable conclusion: [hardpoint contract](../../docs/APPEARANCE_HARDPOINT_CONTRACT.md). Research scores, packet and address counts are unchanged.

## Next action

Connect floor surface selection to same-cell graph-node route inspection while preserving coordinate and cell ownership; original legal movement/portal transitions remain separate.

Final selector gate: axes-only uses an explicit sentinel rather than prototype host's automatic negative item ID. Actual widget signals verify zero labels for axes-only and one label for a selected record; final garage GPU and build gates pass.
