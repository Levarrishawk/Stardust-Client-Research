# Analysis checkpoint — quaternion quantizer table installation

Target: verified `Stardust.exe`; Ghidra output is reconstructed pseudocode. This locates the initialization of the table used by the CKAT SROT/AROT decoder.

The shared-math installer `0x00AB5940` invokes `0x00AC0E20`, which loops over static quantization-range records at `0x018AF64C..`, computes per-range float scales, and populates the selector records based at `0x01945BB8` through `0x00AC0930`. It also sets a completion flag at `0x01945BB0`. The decoder `0x00AC0D90` indexes those selector records with the three bytes stored in each SROT or QCHN. The scale values used by `0x00AC0B30`/`0x00AC0AD0` are therefore initialized math tables, not fields embedded in the clip.

The static range records and initializer arithmetic were resolved in the later quantizer-table formula checkpoint. Remaining work is numerical fixture comparison, especially rare component-sum overshoot and malformed-input behavior. A replacement importer can meanwhile preserve original packed values and selectors losslessly.

Evidence: xref `0x00AB597E` -> `0x00AC0E20`, pseudocode `0x00AB5940`, `0x00AC0E20`, `0x00AC0930`, `0x00AC0BA0`, and read/write xrefs to `0x01945BB8`, `0x018AF650`, `0x018AF658`. Confidence: confirmed for initialization route and table architecture; incomplete on individual values.
