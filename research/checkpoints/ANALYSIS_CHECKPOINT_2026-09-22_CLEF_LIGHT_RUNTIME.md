# CLEF `CLGT` runtime field mapping

Date: 2026-09-22. Target: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Decompiled output is reconstructed pseudocode, not original source.

## Question and evidence chain

Question: Which `CLGT` fields control runtime light appearance and lifetime? Stop at the light constructor, tracking record, and range/visibility-model update. The three versioned CLEF readers consume a **23-byte on-disk** `CLGT` leaf: three U8 bytes immediately followed by five F32 values, with no file padding (`0x00698560`, `0x00698B70`, `0x00699100`). The parser expands that into a **24-byte in-memory** record with one alignment byte after the three colors. This on-disk/runtime distinction is essential for a 64-bit parser. The effective overlay contains 189 `CLGT` leaves in 189 distinct live CEF files; none has multiple `CLGT` leaves. All 189 have the 23-byte shape. This is a corpus observation, not a format limit.

| File offset → runtime offset | Reader shape | Observed consumer | Confidence |
|---|---|---|---|
| `+0..+2 → +0..+2` | Three U8 | `0x00AB87B0` converts ordered R, G, B bytes to floats using `1/255` and adds alpha `1.0`; passed to light construction | Confirmed |
| no file byte → runtime `+3` | Alignment padding in 24-byte runtime record | No semantic value | Confirmed layout distinction |
| `+3..+6 → +4` | F32 | Passed to `0x005230E0` managed-light tracking and stored as remaining lifetime; `0x005235A0` subtracts frame delta and retires at expiration | Confirmed |
| `+7..+18 → +8,+12,+16` | Three F32 | Copied into newly created light object's fields `+0x100,+0x104,+0x108` | Confirmed copy; exact rendering roles **unknown** |
| `+19..+22 → +20` | F32 | Passed to `0x0076D810`, stored at light `+0xFC`, and used to create/update a DPVS sphere test model via `0x00766560` | Confirmed visibility/range bound; exact illumination falloff role unconfirmed |

Runtime effect methods `0x005F05B0` and `0x005F0B70` create a `0x128`-byte light object, invoke `0x0076D500` with light type `3` and the converted color, assign the scalar trio and range, position/attach the light, then register it with `0x005230E0`. The managed-light update `0x005235A0` moves it with its attachment, decrements lifetime and releases expired/invalid instances. This closes creation-to-retirement for installed `CLGT` assets. **Confirmed by code**, with Ghidra type recovery checked against disassembly around the assignments.

`0x00766560` clamps negative sphere radius to `0.1` before calling `DPVS::SphereModel::create`; this establishes a visibility-bound fallback, not necessarily a lighting-intensity fallback. A full-overlay scan finds 189/189 valid 23-byte leaves and finite values, with lifetime `0..8` and range `0..1024`. One common scalar trio is `(0, 0.5, 1)`, but 25 ship-hit effects use `(512, 0.2, 233)`. These values do not independently prove position, attenuation, or color-channel semantics. A prior CDF point-light checkpoint had labeled the same `Light +0x100..+0x108` slots as local position; that interpretation is withdrawn because LOBJ/HLOB separately apply placement. The precise three-field meaning needs the final render-device consumer before GPU mapping. A modern renderer can use a different internal light object but must preserve authored RGB, lifetime, spatial tracking, and validated range behavior.

New stable names: `0x0076D500`, `0x0076D810`, `0x00766560`. The already named color converter `0x00AB87B0` and managed-light tracker `0x005230E0` were reused. `tools/python/cef_clgt_corpus.py` reproduces the on-disk audit. No protocol mapping changes. Planning estimates remain 96% implementation-grade / 90% comprehensive.

Follow-up (2026-09-22 evening): `0x0076D500` initializes Light `+0x100..+0x108` to `(1,0,0)` before CLEF/CDF overrides. This resembles constant/linear/quadratic attenuation defaults in the installed Direct3D 9 `D3DLIGHT9` declaration, but the surrounding Light object does not simply share the SDK structure's offset map, and no final device consumer of these three slots was confirmed. Treat attenuation as a **tentative hypothesis**, not a field mapping. The exact labels remain unknown.
