# Block 3 — deployed legacy EMTR/0000 and /0006

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Corpus boundary

Only nine effective emitters predate modern version 0009: one EMTR/0000 and eight EMTR/0006. The 0000 record is in `appearance/unshaded_geyser_test.prt`; the 0006 records occur in `pt_transport_thruster[02].prt` and `pt_bls_1_red_hit_water_01.prt`.

## Loader comparison

`particle_emitter_description_load_v0000_VA_007B9D70` loads sixteen WVFM records but no PTIM wrapper. Its scalar chunk supplies common emission/shape/capacity controls, legacy Boolean orientation encoding, and a quad particle description allocated unconditionally. Several obsolete bytes/one word are read and discarded or converted to modern defaults.

`particle_emitter_description_load_v0006_VA_007BAEF0` adds PTIM, an author-label string, explicit U32 orientation mode (normalizing serialized 3 to runtime 2), particle-description discriminator, optional sound path and its companion Boolean. It is the deployed transition to configurable quad-or-mesh/audio structure, while predating terrain and pool-policy extensions.

Both deployed versions use `WVFM/0001`; neither introduces an unsupported waveform format. Decode them into the normalized emitter IR and supply version defaults rather than preserving legacy object memory.

Confidence: **Confirmed** for loader order, conversions and corpus reachability. Exact names of discarded 0000 fields remain historical metadata unknowns with no demonstrated runtime consequence.
