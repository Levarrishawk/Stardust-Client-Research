# Round46 terrain fractal family lookup checkpoint

Date: 2026-10-04. Question: how do 00B75B70/00B75DE0 to 00B757A0 create, find, edit and destroy family entries, and what invalidates AHFR/FFRA borrowed key/pointer caches? Stop after a reproducible bounded ownership/lookup contract and one selected original family; no noise implementation or full map prototype.

## Orientation and scope

Read START_HERE and its latest accepted Round45 report, living status/ledger/unknown-function terminology, relevant AHFR/FFRA and height-order contracts/checkpoints, curated references, existing resolver and static tools. Worker HEAD remains 65de8c88141071744dc75594be126b3bab3696a5; the frozen accepted baseline identifies main HEAD 5493338d9cd4255cf58f25de6c4cd15f6507b818.

The Round46 frozen overlay contains 3374 files. Manifest SHA-256 is a31daab4cf81f243120d95933afa8e79135025d58381139d6846c8c2910b2074; ZIP SHA-256 is f0af95216baeb5a8d60658c034ce74ebf08c969914825445ddad78b57b8f7732. All eight prior own Round45 file hashes were checked against the prior scope receipt before overwrite. Archive membership, sizes/hashes, canonical/casefold paths, containment and reparse-point ancestors were checked before writes; retained verified bytes were written once and postflight verified. Overlay was not repeated. This worker adds only its allowed verifier, contract, checkpoint and own research runner. Shared living/nav/registry reconciliation belongs to the lead.

## Input identity and method

The original executable is read-only and SHA-gated before every static probe: E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150. All addresses are VAs. Tools: bundled Python, pefile and Capstone 32-bit disassembly, rg discovery, existing Round33 archive resolver, independently authored verifier. No executable invocation, live server, capture, fuzzing, asset mutation or noise generation occurred.

Config SHA-256: 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. Priority-42 terrain/tatooine.trn: 1188446 bytes, SHA-256 6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427. TGEN starts at source offset 210, length 139608, SHA-256 B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32. Only the first MFAM key 1 was selected: offset 6626 in TGEN, length 132, SHA-256 90CE52CED775F08BF61AE50A702C61576BB53E3982A46A699D90C94C94192DF8. MGRP starts at 6602, length 5633, version 0000. The selected family's DATA length is 28, with 23 name bytes plus terminator after its signed four-byte key; FORM MFRC remains opaque. One family visited before stopping.

A scan of executable sections found relative E8/E9 candidates to 13 selected map entries. Each of 28 candidates was then decoded from its enclosing function entry through the branch. Function inventory was used only for boundary locations, not semantic authority; inventory SHA-256 7576AB635CC40D60B17F4440A7CC57BB729E25C8A1E61F7BCE27EEAD829D9FC5. This does not close indirect callers or global observer coverage. Detailed derived disassembly remains ignored/private; curated output contains independent descriptions and compact instruction anchors.

## Findings, confidence and consequences

The [durable contract](../../docs/TERRAIN_FRACTAL_FAMILY_LOOKUP_CONTRACT.md) is canonical for this bounded conclusion.

- Confirmed: allocated entries are 0x2588 bytes, with owned name +0, ID +4 and inline sampler +8. Tree nodes separately hold signed index key +10 and entry pointer +14. Clear destroys/frees entries and their sampler caches before freeing/resetting nodes.
- Confirmed: signed exact lookup returns zero when absent. Its unchecked inline getter yields address 8, whereas a fresh cached FFFFFFFF key can bypass lookup and retain null. Neither is evidence of safe malformed-input behavior.
- Confirmed: AHFR/FFRA cache borrowed inline sampler pointers and compare numeric keys, without a map-generation comparison in these bodies. Parameter updates clear numerical caches while preserving sampler addresses. No consumer notification appears in the examined map/edit/clear bodies; an external observer remains Unknown.
- Confirmed: generator clear frees map entries at 00B650E4 before deleting layers at 00B6510E. Consumer destructors clear their pointers without dereferencing them. Query exclusion and concurrency safety are Unknown.
- Confirmed: map load clears first; TGEN loads the map before constructing/appending new layers. Existing-generator reload safety is not established. No complete replacement operation is proved. Duplicate insertion, node-only erase and ID-without-reindex have unresolved caller/ownership policy; no direct relative caller of selected edit/remove methods was found in this bounded scan.
- Confirmed occurrence: selected family key 1 uses MFAM DATA followed by MFRC. No corpus-wide format or noise semantic claim follows.

Compatibility consequence: preserve wire/asset lookup boundaries while permitting safe internal handles. Do not claim cached same-key lifetime, safe duplicate/removal/reload behavior or a noise implementation from these findings. The sampler's total extent is Strongly inferred as 0x2580; its numerical values and complete reused-assignment allocation/failure behavior remain Unknown.

## Reproduction and validation

From repository root, with the bundled Python executable and static-analysis packages available:

```powershell
python .\poc\round46-tests\Assets\verify.py C:/Stardust-DEV/Stardust.exe C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
python .\tools\python\validate_organized_workspace.py .
```

The own runner passes: 180 exact instruction anchors; 28 direct candidates and 28 entry-boundary-confirmed branches; eight wrong-executable identity rejections; 32 config/source/TGEN/family identity rejections; 29 synthetic signed lookup/sentinel/missing/lifetime checks across 13 lookup cases. The identity negatives exercise the same SHA gate before parsing; they are not malformed MFRC codec tests. Frozen static prerequisites pass: AHFR 96 anchors and FFRA 93. Original execution and noise decoding/generation are false. No production files changed and no native build is claimed.

Corrections retained for reproducibility: an initial rg used the wrong resolver path (no evidence supplied); discovery found the existing Round33 resolver. Initial occurrence code sliced a generator and raised TypeError, fixed by explicit list materialization. Inventory end ranges initially truncated two exception-funclet branch instructions; decoding entry through branch+15 confirmed them. Initial anchor text split REP prefixes incorrectly; the verifier failed at 013E6A99, then normalization to Capstone compound mnemonics fixed all three REP anchors. A PowerShell inline Python quoting attempt failed with SyntaxError before editing; a literal here-string performed the correction. A Windows wildcard rg path failed, then directory/glob discovery was used. None of these failed probes supports a behavior claim.

Final validation passed: both new Python files parse; organized-workspace validator reports 1466 Markdown files, zero broken links, zero links into private paths, 2626 provisional rows with zero duplicate addresses and 345 packet rows. Exact baseline-relative scope review verifies all 3374 frozen files unchanged, zero existing modifications and exactly four allowed additions. Per-file git diff --no-index --check passes; the focused four-file diff was inspected. HEAD is unchanged, index empty, private evidence ignored. Native compilation was unnecessary for this research-only change and was not run. The private scope receipt carries final file hashes; it is intentionally not linked from curated content.

## Precise resume point

Trace the caller-level exclusion/rebuild/observer mechanism around 00B650E4 to 00B6510E and map-load clear 00B75927 to new-layer construction 00B65D77. Test whether reused-generator reload recreates all consumers or invalidates cached same-key borrowed pointers. Stop before original execution or dynamic testing without explicit authorization and a bounded safety plan. Do not repeat the established entry layout, single-family occurrence or signed lookup investigation.
