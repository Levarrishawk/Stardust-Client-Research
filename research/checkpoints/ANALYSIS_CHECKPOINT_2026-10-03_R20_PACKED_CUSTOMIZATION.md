# Round20 packed customization - October3,2026

## Bounded question

Which concrete ranged-int/palette setter and getter targets consume the version1 record reader's1/2-byte values, and can an owned ordered raw codec preserve the packed boundary under explicit strict atomic policy? Stop at static typed conversion/range evidence, raw codec/independent fixtures and isolated handoff. No CIDM implementation dependency, live field4 state or original player values in this milestone.

## Inputs and identity

Round20 manifest2,374 files, SHA-256 `52d1c308ffd18481ff6a30ccc334a59c98f9dd700b201838e1bb83dcc3fee167`; ZIP `5f379b875a8c3af1c19b8fe1607851c4a252c395c893b322a795bb82d47048e5`. Entire exact unique member set, per-entry hashes/sizes, path containment and archive/destination symlink absence were preflighted before overlay; every written member verified afterward. Snapshot HEAD1fc55dd89cd23ce3af49e3fe471210bdebe9735c remains separate from preserved checkout HEAD65de8c88141071744dc75594be126b3bab3696a5. Unlisted/private bytes and empty index preserved.

Executable C:/Stardust-DEV/Stardust.exe was SHA-256 checked as `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` before address reads. Orientation includes START/latest accepted19, prior field4 ingress/manager/shared consumer contracts, checkpoints, status/ledger/UNKNOWN, curated rows and existing static tools. Server source pin/hashes remain the inherited [Round19 evidence](ANALYSIS_CHECKPOINT_2026-10-02_R19_CUSTOMIZATION_INGRESS.md); no new original asset extraction or values were used. Bundled Python, existing ignored pefile/capstone packages and cached offline .NET8.0.300 only.

## Exact observations and confidence

RTTI tools identify offset0 BasicRangedIntCustomizationVariable vtable015EC8C4/type018B75E4/COL01625524 and PaletteColorCustomizationVariable vtable015EC920/type0187C6C4/COL016255A0. Their slots+10 both point00B49FD0, slots+1C to00B4ADE0/00B4B8D0 and+20 to00B4ADF0/00B4B8E0. This ties the concrete classes to the inherited00B3418C dispatch without naming new canonical addresses.

00B49FD0 bounds-checks signed offset and width. Width1 reads U8 via movzx at00B4A07D. Width2 reads low byte and high byte at00B4A03F..43, sign-extendsCX at00B4A047 and ORs low byte at00B4A04A: exact little-endian S16-to-int32. Both call concrete virtual+20 and return true on successful structural byte read. Thus out-of-range semantic attempts can be structurally successful.

Basic00B4ADF0 stores object+10 only when signed min(+14)<=value<exclusiveMax(+18), then invokes owning-data notification helper00B4A300; getter00B4ADE0 reads+10. Palette00B4B8E0 stores+14 only for0<=index<paletteCount, using00AB7B00 count; getter00B4B8D0 reads+14. Neither setter clamps invalid attempted values into its local slot. Both pass the attempted value to00B4A270, which forwards via linked object+8 virtual+20 when nonnull. Palette color-fetch00B4B920 separately clamps lookup for a nonempty palette and has null-resource fallback; that is not the stored integer setter.

Confidence: **Confirmed** RTTI/slots/instructions for these two concrete types and structural reader. Interpretation: the typed byte conversion and local range-rejection question is closed for them; linked notification graphs/other variable classes/context remain separate. Compatibility consequence: a raw codec must not manufacture clamped values or treat tag high bit as factoryFlag. An adapter requires declared ranges/live variable context and explicit atomic-versus-original sequential policy.

## Implemented boundary

[Durable contract](../../docs/PACKED_CUSTOMIZATION_CONTRACT.md) specifies public types, limits, ownership, strict grammar and original differences. PackedCustomizationRecord owns ID1..127/value bytes of width1/2; PackedCustomizationSnapshot owns bounded read-only ordered records and distinguishes outer-empty from version1/count0. Duplicates/order/raw bytes remain intact. Decode retains exact owned encoded bytes after full validation; Encode(decoded) returns lossless copies, while Encode(constructed) canonically escapes00/FF and appendsFF03.

Strict final EOS/unknown-escape/trailing/budget rejection and atomic snapshot publication are explicit modern policy. No sorting, integer application, CIDM resolution, generation state, callback, queue or original execution. Default limits1536 encoded/255 records/767 decoded; version/count/ID/value bytes charge decoded budget, escape expansion/EOS do not. Valid maximum canonical bytes1535; noncanonical permitted raw zeros retain original spelling. Every rejected input publishes nothing and mutates no caller-owned buffer.

## Reproduce static evidence

Keep generated disassembly private/ignored; inspect only bounded ranges. Existing tool invocation:

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONPATH = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages'
Get-FileHash C:/Stardust-DEV/Stardust.exe -Algorithm SHA256
& $python tools/python/find_vtables_by_rtti.py C:/Stardust-DEV/Stardust.exe 'BasicRangedIntCustomizationVariable|PaletteColorCustomizationVariable'
& $python tools/python/inspect_msvc_vtable.py C:/Stardust-DEV/Stardust.exe 0x015EC8C4 0x015EC920
& $python tools/python/read_pe_ptrs.py C:/Stardust-DEV/Stardust.exe 0x015EC8C4 10
& $python tools/python/read_pe_ptrs.py C:/Stardust-DEV/Stardust.exe 0x015EC920 10
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B49FD0 0x00B4A08F
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B4ADE0 0x00B4AE35
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B4B8D0 0x00B4B97B
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B4A270 0x00B4A285
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B4A300 0x00B4A310
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00AB7B00 0x00AB7B0C
```

## Executed gates and handoff

Synthetic producer and independent byte/grammar oracle import no production code.773 exact fixtures cover all256 low/U8 values, all256 high bytes for S16-width records,127 IDs at both widths, signed boundaries, ordered duplicates, count255 and outer-empty/count0/noncanonical-zero forms.11,995 negative probes cover nonempty truncations, trailing/unknown/premature escapes/EOS, unsupported version/ID/constructor/options and byte/count/decoded budgets.6,699 checks prove raw/input/list/output ownership and exact order/bytes. There are no external resources requiring teardown. Independent oracle agrees; shared smoke project zero warnings/errors and default offline baseline regression suite passes. Standalone commands and callable PackedCustomizationScenarios.Run(peerPath,exportPath) are in the durable contract.

Workspace/link/whitespace and baseline-relative scope gates pass. Exactly seven new allowed paths; no changes among2,374 baseline bytes. HEAD/index remain preserved. Shared Program/project/frozen interfaces/registries/navigation/ledgers are lead-owned and untouched. No Git mutation, original execution, live contact, capture, download or publication. Metrics89%/94%,2602 unique names and345 matrix rows unchanged.

Changed paths:

1. `poc/Stardust.Core/Protocol/PackedCustomizationCodec.cs`
2. `poc/Stardust.Core.SmokeTests/PackedCustomizationScenarios.cs`
3. `docs/PACKED_CUSTOMIZATION_CONTRACT.md`
4. `research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R20_PACKED_CUSTOMIZATION.md`
5. `poc/round20-tests/Networking/Networking.csproj`
6. `poc/round20-tests/Networking/Program.cs`
7. `poc/round20-tests/Networking/peer.py`

Exact next action: lead registers the scenario and reviews raw codec against independent CIDM/declared range context before any current-incarnation application/presentation join. Empty/default and duplicate application must be chosen explicitly. No same-round worker dependency was used; original player values and broader customization mount/linked callback semantics remain Unknown. Worker stops after this one milestone.
