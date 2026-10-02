# Original CELL light contract

`PortalCellLightsDecoder` owns CELL/0004 and /0005 LGHT descriptions. A chunk has S32LE count followed by count93-byte records: S8 type, first ARGB four F32LE, second ARGB four F32LE, row-major3x4 transform twelve F32LE, then ordered three F32LE attenuation coefficients. No padding follows the type byte. Colors remain HDR F32 values; no alpha/color clamping or source-transform rewrite occurs.

## Executable evidence

Verified Stardust.exe SHA256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150. CELL dispatcher0x00B440B0 selects v4 loader0x00B44C80 or v5 loader0x00B451E0. V5 LGHT entry0x00B453F5/0x00B45409 reads signed count;0x00B4556F..0x00B4561B reads one signed byte, two colors via0x00A95860, transform via0x00A95080 and three four-byte scalars. Runtime records are96 bytes because the type expands to S32.0x00B45630 calls existing local-Y rotation0x00AB8E10 with F32 pi at0x015EC824 (3.1415927410125732). V4 follows the same record sequence/correction. Confidence **Confirmed** for raw layout and local post-rotation.

Record factory0x006E5AA0 maps serialized0→runtime0 ambient,1→runtime1 directional,2→runtime3 point through0x0076D500. Directional/point copy transform at record+0x24 into the light pose. Point copies record+0x54/+0x58/+0x5C to Light+0x100/+0x104/+0x108. Ambient ignores stored pose/second color/coefficients; directional ignores coefficients. Unsupported types return null in this factory; the typed replacement rejects before creation. Light's constructor range defaults to5000, not a serialized LGHT range. [Existing attenuation consumer](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_LIGHT_ATTENUATION_CONSUMER.md).

Consumer0x006E5D60 gets the cell-template vector via0x00B45A40 (template+0x14), iterates96-byte records and creates a primary light with participation byte+0x11D disabled. If second RGB is nonzero, it creates another light, zeros its first color and disables participation byte+0x11C. This separates primary/diffuse and secondary/specular paths. Creation/split and raw channels are **Confirmed**; historical C++ field spellings are unavailable. The first/second semantic labels are supported by the typed light-to-shader constant consumer, rather than color appearance alone.

Verified s207_r.dll SHA256 D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D. Existing constant builder0x62A0A3C0 reads directional world K and negates it for the toward-light vector. Consequently the decoder retains both authored transform and runtime transform=authored×Y(pi); `TowardLight` is negative runtime K. No normalization is inserted into this IR. Caller owner composition remains separate from the source loader correction.

## Owned implementation and limits

Accept at most one LGHT chunk per typed cell, exact record width, count0..64 by default (caller limit0..1024), types0/1/2, finite scalar absolute values<=one million and cell versions4/5. Missing LGHT remains empty. Strict counts/types/float bounds are replacement policy, not recovered malformed-asset behavior. Preserve both F32 colors including alpha/HDR, all source affine entries and coefficient signs/order. Do not infer falloff safety, light selection, global ambient, shadows or environment/daylight from these records.

The description is engine-neutral and independent of archives. Runtime-transform derivation preserves translation and applies the loader's F32 pi yaw. Physical point-light evaluation and rendering are separate consumers; the record's second color does not itself implement the original duplicate-light selection/filter flags.

## Original fixture and validation

Existing selected scene `appearance/thm_must_mining_outpost.pob`, priority45/41,269 bytes/SHA256 79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818: cells5/6/7 have7/5/7 records, respectively. Total19 lights: nine directional/ten point, five nonzero secondary RGB records, implying24 objects on the observed split-creation path. Selected point coefficients are(0,0,1); directional records retain(1,0,0) even though their factory ignores them. HDR values above1 are present. No extra source reads are needed; full scene closure remains148/16,612,055 bytes.

`--original-interior-chase-character` additionally emits19 original light rows. Independent `verify_cell_lights.py`, called by `verify_original_interior_sphere.py`, checks exact F32 fields and original-loader yaw against source bytes: maximum transform/direction error1.91276e-8 at2e-6 tolerance. Synthetic all-three-type/ARGB/HDR/pose/yaw/coefficients, absence, every truncation, invalid type/float/count/width/budget fixtures and full native/build pass. Renderer integration is not claimed by this checkpoint. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PORTAL_CELL_LIGHTS.md).

Next: an explicitly selected original-cell light adapter, preserving original source semantics while documenting bounded replacement selection/ambient/attenuation policy.

The [bounded legacy light-selection contract](LEGACY_LIGHT_SELECTION_CONTRACT.md) now closes material participation masks, slot capacities, ranking and channel roles. Live visibility/order and vertex LIT remain open; the prior explicit modern policy is not original slot selection.

The [original positive attenuation contract](LEGACY_POINT_ATTENUATION_CONTRACT.md) now closes coefficient order and uncapped reciprocal behavior. Recovered-slot preview uses that equation for positive finite inputs, with explicit modern singular omission; historical capped preview remains a separate policy. Original high-power vertex LIT behavior remains unresolved.
