# Round44 slope clamp initialization

Date: 2026-10-03 assignment, completed after midnight UTC October4. Question: what initializes live FSLP clamp word `01947228`, and does helper `00B77670` run before construction? Bounded result: actual table registration, helper invocation and ordering before ordinary application entry are Confirmed; universal ordering before constructors inside preceding callbacks remains Unknown. No production implementation, original process, asset sweep, live contact, downloads, stage/commit/push/publication or sibling dependency.

The [initialization contract](../../docs/TERRAIN_FILTER_SLOPE_INITIALIZATION_CONTRACT.md) is the durable conclusion; the accepted [raw contract](../../docs/TERRAIN_FILTER_SLOPE_CONTRACT.md) remains unchanged. No x87 sine or influence parity is claimed.

## Orientation, identity and overlay

Read AGENTS, START/latest accepted43, living status/ledger/UNKNOWN and relevant curated searches, slope raw contract/checkpoint, world terrain architecture, prior layer preparation and existing call/immediate/PE readers. Existing function inventory was discovered by rg --files; no enclosing entry for the thunk was found, and no function identity was inferred from that absence. Its private inventory SHA256 is `7576AB635CC40D60B17F4440A7CC57BB729E25C8A1E61F7BCE27EEAD829D9FC5`; executable instructions supply the proof.

One Round44 overlay: 3,310 public files; frozen manifest SHA256 `e6ca60d60eb62dae240572af25fe868df6caddad0a15eebe3ac9f4ce3800eb47`; ZIP `ba824bc7f0e442c2a59e0e1d33a2982ecfcabfc13b70dac12464ed689223caf9`. All members/set/size/hash/casefold/canonical paths and lexical checkout/all ancestors/targets symlink/junction/reparse/containment checks precede any copy. Main's seven final Assets43 integration hashes exactly match the independent local final review. Current destinations match only prior pinned43 baseline, incoming44 or those reviewed own43 hashes. Retained verified archive bytes are written once, then all3,310 hashes pass postflight. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`, frozen main HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`, empty index and private/unrelated work preserved. No repeat overlay.

Original `C:/Stardust-DEV/Stardust.exe` SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` is rehashed before every current PE/Capstone VA probe and final verifier. No original raw occurrence is decoded this round; the accepted43 single FSLP record remains the corpus observation. Detailed disassembly, discovery outputs and receipts are ignored private material.

## Evidence and interpretation

Direct relative JMP `00B77660 -> 00B77670` resolves the missing literal-helper-pointer lead. Data word `018560AC` contains the thunk. Startup entry `0131DC7A` calls initializer driver `01324772` at `0131DD1F`, which passes second range `0184D000..01866B90` to walker `01324896` at `01324796`. That walker skips zero, calls nonzero slots, advances four and stops at unsigned current>=end. There are26,340 words/26,339 nonzero; thunk occurs once at zero-based9,259, between neighboring scalar initializer thunks. Application entry `00401810` is called afterward at `0131DD55`.

**Confirmed:** successful returning startup invokes the clamp helper before ordinary application entry. Its F32 load/multiply/F32 store computes exact half of normal binary32 `40490FDB`, independently modeled by decrementing its exponent: `3FC90FDB`. Mapped-file zero is not assumed live after this write. Helper has no allocation, calls or cleanup registration. The two neighboring initializers also return and have explicit local ordering.

**Confirmed bounded inventory:** full executable-section linear decode finds one direct write and ten direct reads of `01947228`; only direct helper/thunk branch is the thunk JMP. Whole-section literal search yields exactly the table thunk pointer and no helper pointer. Selected local instruction anchors validate positive identities independently. **Unknown:** indirect stores, synthesized addresses, table modification, unusual earlier-callback construction or paths where prior callbacks fail to return. Therefore no universal pre-constructor claim follows from the table alone.

Accepted FSLP constructor invokes both endpoint setters; destructor reaches common filter cleanup. The scalar helper does not register a destructor. Separate exit table ranges are anchored. Active arithmetic controls, x87 FSIN result bits, normal generation/units/axis semantics and actual cache parity remain Unknown. Early hook `0131DB29` includes FNCLEX after three calls; no active-control claim is inferred from exception clearing.

## Reproduction and verification

Cached Python: `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`; UTF8/nobytecode enabled. Explicit static packages path below; no download or native/Core build is needed because no production API or C# changes occur.

```powershell
python poc/round44-tests/Assets/verify.py C:/Stardust-DEV/Stardust.exe C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages
python tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Executed verifier passes 62 instruction anchors, 26,340 table words/26,339 nonzero, eleven direct clamp references/one direct write, the unique thunk pointer and helper branch, eight wrong-identity negatives before VA interpretation and 66 synthetic mapped-image structural negatives. Linear cross-check decodes 7,228,061 instructions and skips 263 synthetic data records; this does not establish whole-image reachability. Workspace validator passes 1,445 Markdown files, zero broken/private links, 2,626 provisional rows/zero duplicate addresses and 345 packet rows. AST/LF/noBOM, focused per-file whitespace and git diff --check pass. Final scope review confirms all 3,310 baseline files unchanged, exactly four additions, unchanged HEAD and empty index, with private receipts ignored. Exact final hashes and focused baseline-relative diff are retained in the private handoff inventory. No raw-to-cache arithmetic, FSIN model or original execution is hidden in the runner.

Corrected discovery attempt: first whole-section Capstone scan requested operand details for a synthetic skip-data record and stopped with CS_ERR_SKIPDATA. The scanner now skips instruction.id==0 before operand access; the failed attempt supplied no evidence. Subsequent selected identity probes and full scan succeeded. No guessed production kernel was attempted.

## Scope and exact resume

Four additions only: verifier, research-only runner, durable contract and this checkpoint. Existing raw/API/shared entrypoints, registries and living documents remain frozen; lead owns reconciliation. No new provisional name or coverage denominator. Focused baseline-relative diff and per-file SHA256 receipt are retained privately.

Next exact question: what effective control writes and numerical constraints establish a defensible x87 FSIN/F32 cache-result contract at `00B78F9F`/`00B7900F`? If bit-exact original execution is needed, request a separate bounded authorization. Do not equate initializer ordering with completed slope influence. Return handoff and stop idle; no self-dispatch or sibling messages.
