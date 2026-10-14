# FSLP0002 endpoint operation graph

Date: 2026-10-13. Bounded question: can serialized endpoints be converted to the target's cached slope thresholds from direct static evidence? Result: exact load/clamp/subtract/sine/store graph is Confirmed; original numerical cache parity remains Unknown. No original process, runtime/publication, network, historical implementation path (omitted) or registry changes. The accepted far/water executable is unchanged.

Read the slope and initialization contracts, R43/R44 checkpoints, arithmetic policy, layer-order contract and existing curated/static tools. The durable conclusion is in [the slope contract](../../docs/TERRAIN_FILTER_SLOPE_CONTRACT.md). This follow-up does not replace historical corpus counts or investigate the normal producer.

## Inputs and direct evidence

Target SHA256, checked before VA use: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Existing PE/Capstone readers operate on its mapped image; original execution is false. Original Tatooine SHA256: `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`; TGEN offset210/length139608, SHA256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`. Proprietary bytes and detailed receipts remain private. Third-party labels are corroboration only; no conclusion depends on them.

| Half-open mapped VA range | SHA256 |
| --- | --- |
| `00B78D70..00B78E80` 0002 loader | `9B5FA7C3A83A57BC7376CBD0B751A43F6E39ACFD2BFE58E4DF260629E33F5BDF` |
| `00B78E80..00B78F40` writer/padding | `34DAB944F736491E2AD56D4007BB0C5A97F3B494D85DF1569FD92DC281B88606` |
| `00B78F40..00B79020` setters/padding | `44AA0D84C42641128D9B343B8A1A10D770BA61E1D0400779BBE065962DB659CC` |

Four-byte reads at `00B78DAA/DD1`, P/raw/Q multiplication and F32 argument spills at `00B78DC1/DE8` precede setter calls. P/Q/B literals are `40490FDB/3BB60B61/43340000`; successful startup initializes C=`3FC90FDB`. Setter finite clamps are0/C, then a nonpop F32 angle store, retained C-angle subtraction, FSIN and F32 cache store. First endpoint angle/cache offsets+20/+28; second+1C/+24. The direct graph, source widths and six sine sites are Confirmed. Degrees/radians are Strongly inferred from both loader and writer conversions, clamp and actual finite0..90 endpoint domain. Raw decoder names/acceptance stay unchanged.

Both setter bodies have no call/control-word instruction. Their status tests route unordered comparisons to reload the argument if comparisons complete; they do not explicitly sanitize NaNs. This closes branch structure, not exception/control/NaN-result behavior. The exact FSIN implementation and effective controls at loader calls remain the numerical blocker.

Independent Fraction nearest-even basic arithmetic plus 140-digit Decimal Taylor sine/cosine gives a synthetic30 substitution witness: literal-C complementary sine F32=`3F5DB3D8`; cosine of the converted angle and exact mathematical cos30 F32=`3F5DB3D7`. These are mathematical reference results, never target execution or FSIN parity. Three basic-operation significand widths 24/53/64 at 0/5/20/30/60/90 were bounded cross-checks. A future binary64/library-sine policy must be explicitly modern and preserve conversion/spill order; it cannot claim original cached bits.

Existing TGEN metadata/raw-frame readers find24 original records, all 0002/local-enabled 1 and finite ordered endpoints 0..90. Enabled ancestry/reachability is not inferred. Static FSLP verifier passes 119 instruction anchors, 18 finite-status cases, RTTI/vtable and six FSIN sites. Reproduction uses existing tools, not a new implementation.

## Reproduction and stopping boundary

```powershell
python -c "import importlib.util; s=importlib.util.spec_from_file_location('s','poc/round43-tests/Assets/verify_slope_input.py'); m=importlib.util.module_from_spec(s); s.loader.exec_module(m); print(m.static('C:/Stardust-DEV/Stardust.exe','local-private/toolchain/python-static-analysis-packages'))"
python poc/round44-tests/Assets/verify.py C:/Stardust-DEV/Stardust.exe local-private/toolchain/python-static-analysis-packages
python tools/python/validate_organized_workspace.py .
git diff --check
```

For the independent 30 witness, decode F32 P/Q/C exactly as rationals; round each multiplication to the selected nearest-even significand width, round the result to F32, subtract that exact F32 from literal C, then evaluate mathematical sine using 140-digit Decimal/100 Taylor terms and round to F32 nearest-even. Independently evaluate cosine at that converted F32 and sqrt(3)/2. This reproduces the substitution distinction without executing target code.

Executed startup verifier passes 62 anchors, eight wrong-identity negatives and 66 structural negatives; it confirms the unique direct clamp write and returning-startup order, without claiming universal pre-constructor order. Workspace validation passes 1,953 Markdown files, zero broken/private links and zero duplicate provisional addresses. Focused diff/whitespace checks pass. Shared unrelated work remains untouched; exactly this checkpoint and the slope contract changed in this probe.

Next numerical action: justify FSIN/F32 and effective controls, or explicitly adopt a modern trig policy with original parity false. Separate next normal-cache entry already identified: `00B65730`, called at `00B63E99` in `00B63D20` on demand/dirty+570; query consumes state+3C. Its producer math, normalization, ownership and invalidation remain unexamined. Stop here; no rendering-normal substitution or influence implementation follows from this checkpoint.
