# Block 1 — EMTR/0014 particle-pool priority

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The final byte introduced by `EMTR/0014`, stored at description `+0x229`, is no longer dormant or behaviorally unknown. Spawn routine `0x007A4350` reads it at `0x007A4373` and passes it to `particle_pool_reject_spawn_at_selected_limit_VA_007B5990` before allocating a 0xA8-byte particle record.

The admission helper first asks the global pool whether allocation is unavailable. Under pressure it compares live allocation count against one of two global limits: false selects the normal limit and true selects an expanded/reserve limit. Reaching the selected limit causes the spawn path to return without creating a particle. The conservative semantic name is **reserve-pool admission priority**; the original editor label is not recovered.

The effective corpus contains 3,682 EMTR/0014 emitters: 3,127 false and 555 true. This use distribution independently rejects the earlier dormant-metadata hypothesis.

## Replacement rule

Preserve this Boolean and use it when overload policy decides whether a particle spawn may consume reserve capacity. Do not encode the legacy allocator's addresses, 0xA8 record layout or block implementation into the portable asset model.

Confidence: **Confirmed** for the branch and threshold selection; **Strong** for “priority/reserve” terminology.
