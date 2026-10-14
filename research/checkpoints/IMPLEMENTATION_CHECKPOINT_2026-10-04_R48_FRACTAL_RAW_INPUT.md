# Round48 MFRC raw input and full-U64 supplied samples

Date:2026-10-04. Question: can a bounded MFRC/0001 decoder preserve all thirteen raw words with owned snapshots, while a supplied retained sample admits a full64-bit magnitude without signed narrowing? Stop at raw input/full-U64 APIs, one pinned original occurrence and independent/frozen gates. No production noise, full MFAM map, sampler controls or original sample parity.

## Orientation, inputs and preservation

Read main START_HERE, accepted47 session, applicable AGENTS, status/ledger/unknown terminology, relevant curated names, seed/schema/sample/map/consumer contracts and prior checkpoints. Reused frozen47 inspector and archive resolver, accepted raw encoders and Fraction arithmetic. Only root AGENTS applies. Worker HEAD65de8c88141071744dc75594be126b3bab3696a5; frozen main HEAD5493338d9cd4255cf58f25de6c4cd15f6507b818.

ONE complete overlay passed3458 files. Manifest SHA256a6c0f6bfaa352444843e812baf93a169b42645bd4c75d018e6854c7f87e3093a; ZIP5fb375b0aac53e8c5f0c6bc00403b913e7bd002128943ad3f40fac3e0937aa4f. Every own47 changed row was checked against the independently reviewed main47 Assets integration receipt before overwrite. All archive/member/canonical/casefold/size/hash/root+ancestor+target reparse/containment and incoming/prior/own destination checks occurred before writes; retained verified source bytes were written once and all3458 hashes checked after. HEAD/index and unrelated/private evidence were preserved. No reset/delete/staging/commit/download/publication.

Original EXE SHA256E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 is checked before static disassembly. Original never executed. Read-only original config/source/TGEN/family/MFRC identities are checked by the fresh accepted47 inspector. No live contact/capture/fuzzing/external mutation.

## Evidence and implementation

Canonical [raw contract](../../docs/TERRAIN_FRACTAL_RAW_INPUT_CONTRACT.md), [supplied inspection extension](../../docs/TERRAIN_RETAINED_FRACTAL_SAMPLE_CONTRACT.md); sources [raw decoder](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainFractalRawInput.cs), [sample](../../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainSuppliedFractalSample.cs), [native scenarios](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainFractalRawInputScenarios.cs), [independent peer](../../../SWG-Godot-Client/poc/round48-tests/Assets/peer.py), [replay](../../../SWG-Godot-Client/poc/round48-tests/Assets/Invoke-FractalRaw.ps1).

Observation: MFRC/0001 FORM/FORM/DATA is84 bytes with52-byte DATA, thirteen little-endian words in loader/writer order. Static fresh289 anchors agree. Confidence: Confirmed structure, field destinations and signed seed/octave roles. Implementation preserves raw uint words and signed/flag/finite inspection without running setters, normalizing state or admitting runtime eligibility. Nonfinite raw parameter bits are accepted structurally.0000 remains explicitly unsupported, not guessed or made opaque; all unknown versions reject.

Decode checks policy, count/bytes/copy cost and exact envelope before snapshots. DecodeBatch preflights all borrowed envelopes and aggregate count/input/136-byte-per-input logical buffer copy budget before arrays/copies; copies are revalidated before words are interpreted. Returned input/word copies are independently owned. Typed record overhead is not included in logical buffer-copy bytes and no CLR heap-size claim is made. Concurrent caller mutation is outside the borrowed-input contract; malformed changes reject before publication.

FromWideMagnitude(ulong magnitude,bool negative,int exponent) constructs an exact unsigned magnitude then applies sign. Exponent remains[-512,512]. Primitive exact F32 overflow comparison still precedes BigInteger. Old FromWide/FromF32/FromCacheF32 calls, provenance, finite/unused gates, arithmetic receipts and32-byte logical accounting stay correct. All sample properties remain getters. Magnitude/IsNegative are honest full-width inspection. Significand is now nullable Int64: exact old values, negative2^63 as Int64.MinValue, otherwise null outside signed range. The public C# property changes from long to long?, so direct long assignments may require source adaptation; universal source compatibility is not claimed. It never wraps positive U64 or throws from a JSON getter. No other frozen geometry, arithmetic/project/smoke dispatch or engine executor was edited.

## One original occurrence

Priority42 terrain/tatooine.trn,1188446 bytes, source SHA6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427; config SHA22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524; TGEN offset210/139608 bytes/SHA B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32. First family key1 at TGEN6626,132 bytes, SHA90CE52CED775F08BF61AE50A702C61576BB53E3982A46A699D90C94C94192DF8. Its MFRC at6674,84 bytes, SHA85A84EFBDF4EE507C0011C9E4D4ED130EBF4719735A521FD491ABD286EBD723E.

Fresh inspector dynamically derives original words/seed only into ignored original-inspector/fixture/native receipts. Its13 words equal the independently structured peer and actual native decoder. Public sources contain no selected original payload array, seed or parameter literals. Synthetic arbitrary-word vectors are independently authored and explicitly separate. No additional family/corpus census or generated original sample occurred.

## Verification and reproduction

```powershell
.\poc\round48-tests\Assets\Invoke-FractalRaw.ps1 -Python C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe -CachedPackages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages -Exe C:/Stardust-DEV/Stardust.exe -Packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages -AssetRoot C:/Stardust-DEV -Config C:/Stardust-DEV/swgemu_live.cfg
python .\tools\python\validate_organized_workspace.py .
```

Independent fixtures are generated before native execution. Full comparison passes330 raw cases/122 rejects and2592 U64 cases/1338 rejects, including468 accepted cases above long.MaxValue. Coverage includes both signs, signed zero, signed-minimum compatibility, U64 maxima, exponent extremes, finite/overflow midpoint boundaries, precision24/53/64 and actual AHFR arithmetic. Native JSON serialization exercises every sample getter. Raw cases cover arbitrary/nonfinite words, every truncation, malformed tags/sizes,0000/unknown versions, trailing bytes, individual caps and invalid policies.10 ownership and5 exact/late aggregate budget checks pass; earlier results and supplied arrays remain unchanged after rejection.

Frozen47 sample7762 cases/143 rejects,13 ownership21 budgets18 factory negatives pass. Frozen45 AHFR151 raw8836 supplied, FFRA152 raw2326 queries,6354 carrier and1141 filtered cases pass; frozen46 sampler2466 research cases/230 anchors and903 F32 join receipts pass. Frozen47 retained join786 receipts preserve100 AHFR67 FFRA124 final andnine unit-scale witnesses. This proves enumerated conditional supplied arithmetic only. No original controls/noise/terrain parity follows.

Final own replay/default smoke and all native reference builds pass with zero warnings/errors; default smoke retains18 journal events/zero failures. Two separate wrong-EXE/wrong-config processes reject before asset resolution or output, in addition to40 dynamically checked selected-input identity negatives and frozen identity regressions. Organized-workspace validation reports1491 Markdown files, zero broken/private links,2626 provisional rows/zero duplicates and345 packet rows. Python AST, project/config XML and PowerShell parser checks pass. Baseline-relative per-file git diff --no-index --check passes; the two existing-file diffs and all additions were inspected. Exact scope:3458 frozen files,3456 unchanged, two owned edits and nine additions; no deletion/unrelated edits. Original existing newline/BOM/trailing styles were retained. HEAD remains65de8c88141071744dc75594be126b3bab3696a5, index empty, private/generated/build outputs ignored. Full final source hashes/diff/status are in the ignored scope receipt, deliberately not linked from curated content.

## Corrections and remaining question

No native/oracle arithmetic mismatch required changing expected results in this round. The signed inspection return-type change was selected explicitly after checking existing source consumers and was documented before handoff. Exact envelope checks use a small fixed-size decoder because generic IFF snapshot parsing would copy before the required strict/batch preflight; existing archive resolution and research inspector were reused.

Lead owns living status/ledger/navigation reconciliation and any combined use. Next: independently review the raw/U64 APIs and admit them through explicit bounded runtime eligibility and evidenced fresh/optional/control provenance. Full sampler generation, original controls, reused-state behavior and family-map lifetime remain Unknown.
