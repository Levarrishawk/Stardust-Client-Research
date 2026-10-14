# Appearance extent and detail distance contract

## Primary APPR extent

`AppearanceExtentDecoder.DecodeBase` accepts APPR/0003 and reads its first extent record. Empty primary data or an empty FORM NULL supplies no extent. EXSP/0001 contains SPHR: three little-endian F32 center coordinates followed by F32 radius. EXBX/0001 contains that EXSP and BOX: three F32 maximum coordinates, then three F32 minimum coordinates. The authored sphere center is retained independently of the box midpoint. Other APPR records, including collision, hardpoints and floor, remain separate metadata.

The implemented valid slice rejects nonfinite values, negative radius, reversed bounds, malformed nesting and other extent families/versions. These are explicit replacement validation policies. The original reversed-box path resets its sphere before retaining bounds; its negative-radius diagnostic failure policy is not closed. Do not describe the replacement rejection as recovered original behavior.

## Distance origin

`DetailAppearanceDistance.Measure` uses world translation when PIVT=1. For PIVT=0 or absent, it transforms the primary extent's authored sphere center through the appearance world 3x4 transform; a NULL extent contributes zero local offset. The camera-to-origin Euclidean distance feeds the existing detail selector. It uses wider double intermediates for distance; this does not promise original x87 rounding. Camera/origin must be finite. Invalid pivot values are rejected.

Direct baseline evidence: DTLA constructor 00787400 installs vtable 015E3D84. Slot +8 targets 00B2C2F0, which returns extent+8 or zero vector 01945AB8. Slot +0x14 targets 00788300, fetching template+0x0C. APPR loaders 00B2CCA0/00B2CCF0 store the ExtentFactory result there. EXBX loader 0125F690 calls EXSP loader 01271570; SPHR reader 01271450 stores center at +8 and radius at +0x14. BOX reader 0125F520 reads maximum then minimum without recomputing the valid sphere. Selector 00787710 chooses this virtual offset for false PIVT and translation for true PIVT. Template constructor 007993F0 initializes absent PIVT to false. NULL callback 012703F0 returns zero; initializer 00AB8990 zeros the fallback vector. These are confirmed instruction/data observations for the documented executable SHA.

## Prototype host boundary and reproduction

The adapter converts transforms using S R S and translation S t, with S=diag(1,1,-1); camera coordinates receive the same reflection. Component detail instances use their own global transform. `-CameraLod` enables automatic selection initially; the inspector checkbox can restore the manual distance slider. Distances beyond float range clamp to the selector's far endpoint as replacement policy. This proves distance origin and selection, not original DPVS, lazy loading, visibility hysteresis or lighting.

`verify_detail_extents.py ROOT CONFIG EXISTING_DTLA_CSV CORE_DLL PRIVATE_OUTPUT` independently compares original primary extent float bits, pivot bytes, effective priority and SHA with a one-process native inventory. The supplied inventory is not independently discovered by this verifier. All 6,198 current entries agree: 6,048 EXBX/0001 and 150 NULL. Of the box extents, 6,001 centers are nonzero; 3,146 differ exactly from midpoint, mostly by rounding. One differs by more than 1e-4, with maximum coordinate difference about 0.46737. No proprietary position corpus belongs in curated output.

See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_PIVT_CAMERA_LOD.md) for input identity, validation and the next bounded probe.

## Authored secondary extent — October 1

`DecodeCollisionBase` reads the second APPR/0003 extent independently of primary bounds. Missing secondary data rejects; empty FORM NULL returns null. `DecodeExtent` shares the owned NULL/EXSP/EXBX grammar above. Unsupported composite/mesh families reject explicitly. Primary distance behavior is unchanged.

Confirmed baseline `00B2CCA0` calls `00B2CCF0` then `00B2CD10`; primary stores template `+0C`, secondary stores `+10`, both through factory `012707C0`. The nonnull secondary receives virtual `+40`. This source metadata does not establish runtime actor fallback or participation.

The [bounded six-source probe](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_AUTHORED_COLLISION_EXTENTS.md) independently agrees on original primary F32 values and six NULL secondaries: three selected rooms, door DTLA and both leaf meshes. Synthetic primitive/NULL/malformed/unsupported gates and prototype host build pass. Actor collision extent source policy remains in the [separate participation trace](GROUNDED_ACTOR_COLLISION_PARTICIPATION.md).
