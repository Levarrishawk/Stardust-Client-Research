# Analysis checkpoint — CKAT quaternion quantizer selectors

Target: verified `Stardust.exe`. This refines the packed-quaternion checkpoint by resolving the three QCHN/SROT header bytes through instruction-level call arguments.

At `0x00AC0D90`, each header byte selects an eight-byte parameter record based at `0x01945BB8 + selector*8`. The first and second selected records feed the 11-bit component decoder `0x00AC0B30`; the third feeds the 10-bit decoder `0x00AC0AD0`. Each decoder forms a component from a selector-specific base float plus or minus an integer magnitude times a selector-specific scale. For the 11-bit components the high bit `0x400` is the sign and low ten bits the magnitude; for the 10-bit component `0x200` is the sign and low nine bits the magnitude. The fourth quaternion component is reconstructed as the nonnegative `sqrt(1 - x² - y² - z²)`. The SROT seven-byte record and the AROT QCHN both use this decoder, so they share quantizer semantics.

This identifies the three formerly unnamed U8 values as **quantizer selectors**, not quaternion components themselves. The selector table and scale lookup at `0x018AF650/0x018AF658` were subsequently resolved in the quantizer-table formula checkpoint. Numerical/clamp behavior for malformed packed inputs remains unverified. A compatible importer must carry selectors and packed words intact; naïvely interpreting the selector bytes as normalized axes would be wrong.

Evidence: instruction-level disassembly `0x00AC0D90`–`0x00AC0E12`, pseudocode `0x00AC0B30`, `0x00AC0AD0`, `0x00AC1380`, SROT route `0x00825430/60`, AROT route `0x00824F00`. Confidence: confirmed for selector addressing, bit partition and reconstruction; table contents resolved later, invalid-input behavior still open.
