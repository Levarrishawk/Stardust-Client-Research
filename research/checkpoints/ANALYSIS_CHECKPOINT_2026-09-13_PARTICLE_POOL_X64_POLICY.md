# Block 6 — particle allocation policy and x64 boundary

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Legacy allocator

Particle pool installation at `0x007B5870` derives configured global limits, creates a pooled allocator using 0xA8-byte records and 0x200-entry block granularity, and registers shutdown cleanup. Allocation wrapper `0x007B5960` and release wrapper `0x007B5970` isolate the pool from construction/destruction. Admission helper `0x007B5990` applies normal versus reserve pressure thresholds selected by EMTR/0014 `+0x229`.

The observable contract is that spawning may be rejected under global pressure, priority emitters may consume reserve capacity, rejected spawns do not create partial particles, lifetime destruction returns storage, and exhaustion is graceful degradation rather than a parse/render failure.

## Native x64 design

Replace the global 32-bit fixed-layout allocator with typed 64-bit-safe arenas or structure-of-arrays storage. Keep stable handles across compaction, separate quad and mesh payloads where useful, and make normal/reserve budgets configurable. Preserve deterministic admission ordering and priority distinction without inheriting the 0xA8 layout, 512-entry blocks or pointer assumptions.

Confidence: **Confirmed** for legacy wrappers, record/block sizes and two-threshold policy; **Recommended design** for the replacement.
