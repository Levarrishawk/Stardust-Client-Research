# One MFRC seed, tables and finite noise graph

Date: 2026-10-04. This closes the seeded table construction at `013E64D0 -> 00B59EF0` and the finite two-coordinate interpolation at `013E66F0 -> 0131E27C` for one pinned family under explicit arithmetic profiles. It is research evidence and a test harness, not a production sampler or original generated-terrain parity claim. Original floating controls and optional sine/log/curve behavior remain Unknown.

The [sampler cache contract](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md) supplies the caller-side stores and retained miss/F32-hit distinction. The [family-map contract](TERRAIN_FRACTAL_FAMILY_LOOKUP_CONTRACT.md) supplies ownership and lookup. The [existing RNG closure](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_STATIC_FLORA_RNG_CLOSURE.md) and canonical `legacy_shuffled_park_miller_next_VA_00B59EF0` name are reused; no registry rename follows.

All VAs refer to EXE SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The [verifier](../tools/python/verify_terrain_fractal_seed_noise.py) gates EXE/config before decoder imports, checks289 selected instruction anchors,13 loader/writer words and exact constants, and compares independent Fraction results to the native research harness. The original executable is never run.

## Selected family identity and MFRC/0001 schema

Reuse the first selected family from the accepted map investigation: effective priority42 `terrain/tatooine.trn`,1,188,446 bytes, SHA256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. Config SHA256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. TGEN starts at source offset210, length139608, SHA256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.

Family key1, MFAM at TGEN offset6626,132 bytes, SHA256 `90CE52CED775F08BF61AE50A702C61576BB53E3982A46A699D90C94C94192DF8`. Its MFRC begins at TGEN6674,84 bytes, SHA256 `85A84EFBDF4EE507C0011C9E4D4ED130EBF4719735A521FD491ABD286EBD723E`. Only this family is interpreted; there is no corpus census or search for preferable noise controls. Names remain uninterpreted private bytes.

The selected envelope is FORM MFRC / FORM 0001 / DATA, with52 payload bytes. IFF extents are big-endian; the13 words are little-endian. Dispatcher `013E8410` recognizes0000/0001;0001 invokes `013E8640`. That loader reads13 four-byte values and setter order matches writer `013E82B0`'s13 writes. Version0000 and unsupported-version diagnostics are outside the typed research inspector.

| Word | Observed runtime destination / setter | Interpretation |
| --- | --- | --- |
| 0 | +0 /013E6B80 | signed seed |
| 1 | byte+24 /013E6C50 | first curve enable, nonzero normalized to true |
| 2 | +28 /013E6C50 | first curve parameter |
| 3 | byte+2C /013E6C70 | second curve enable, nonzero normalized to true |
| 4 | +30 /013E6C70 | second curve parameter |
| 5 | +14 /013E6BF0 | signed octave count |
| 6 | +18 /013E6C10 | frequency multiplier |
| 7 | +1C /013E6C30 | amplitude multiplier |
| 8 | +4 /013E6BB0 | first coordinate scale |
| 9 | +8 /013E6BB0 | second coordinate scale |
| 10 | +0C /013E6BD0 | first offset |
| 11 | +10 /013E6BD0 | second offset |
| 12 | +38 /013E6CB0 | mode discriminator |

Every serialized field above is four bytes. Selected raw parameter words are derived from the SHA-gated original asset only into private receipts and fixtures; public sources contain no selected payload array. Aggregate branch observations: two octaves, mode0, and disabled curve flags.

Observation: exact widths/order/destinations, Confirmed. Seed/scale/octave roles are Confirmed by their consumers. Bias/gain naming for the two optional curves is only an interpretation; this specification uses their observed fields. Consequence: preserve raw words separately from normalized runtime flags and derived normalization+20. The amplitude setter recomputes normalization after the loaded octave count/frequency have been assigned.

Neither this loader nor writer serializes sine byte+34. Fresh constructor `013E6880` sets it to zero. Thus a **fresh** instance of this selected family has both optional curves and sine disabled. Loading into an already mutated sampler does not establish the same sine state. No reused-object or concurrent-load equivalence is claimed.

## RNG state, initialization and exact recurrence

The embedded noise object owns RNG words at+0 (seed),+4 (last shuffled value), and+8 through+50C (322 shuffle entries). `013E64D0` stores wrapped signed negative input seed and zero last value before drawing; the next RNG call initializes all322 entries. Prior shuffle contents therefore do not affect this explicit rebuild.

At `00B59EF0`, seed<=0 or last==0 enters initialization. Wrapped negation is clamped to a minimum of1. Consequently this noise wrapper's zero/negative raw seeds, including signed-minimum wrap, normalize to1; a positive raw seed initializes from that seed. The signed-maximum seed has a separately tested initial zero-output/reinitialization edge and is not silently clamped to a smaller maximum.

For positive state, instruction multiplication/division implements:

`q = trunc(seed / 127773)`

`next = 16807 * (seed - q * 127773) - 2836 * q`

`if next < 0: next += 2147483647`

The observed magic high-product/shift quotient uses `834E0B5F`;520 independent fixed-width arithmetic cases compare it to the mathematical quotient and modular recurrence, including quotient boundaries and the modulus. Confidence: Confirmed instruction arithmetic, with the existing Park–Miller semantic label preserved.

Initialization iterates j=329 down to0:330 advances. For j<322 it fills shuffle[j], then last=shuffle[0]. Each returned draw first advances the state, selects an index, returns the old shuffle entry, and replaces it with the new state.

**The index is not unconditional integer division.** `00B59F9A/9D` performs signed integer load of last followed by FDIV using F32 `4ACB872A` (6669205), under the active arithmetic controls. `0131E27C` then truncates that rounded result. The research graph is `trunc(R(last / 6669205))`. Eight synthetic cases demonstrate a different index from `last // 6669205` under the24-bit profile. Higher profiles are independently tested, not selected as original controls.

Noise constructor `013E6470` invokes RNG constructor `00B59EC0`, which consumes one shared-global RNG draw before explicit table initialization. The later selected seed reset fully determines the tested rebuild tables, but this does not erase the constructor's global-stream side effect or count earlier default table builds. Reported selected rebuild draw/step counts exclude those earlier constructor operations.

## Table construction, normalization and permutation

| Region relative to embedded noise object | Extent | Observation |
| --- | --- | --- |
| +510 | 514 U32 permutation entries | first256 initialized to0..255; later duplicated |
| +D18 | 514 pairs of F32 gradient components | first256 generated and normalized; later duplicated |
| +1D28 | 514 F32 scalar gradients | first256 generated; later duplicated |

For each i=0..255, consume exactly three RNG draws: scalar gradient, first2D component, second2D component. Each starts as `(draw % 512 - 256) * 1/256`, with F32 store; `1/256` is bits `3B800000`. For the ordinary nonnegative draws of this rebuilt state, the observed signed-remainder instruction sequence agrees with this remainder. No host random service is used.

The2D pair is normalized through the retained graph: square second component, square first, add, FSQRT, take reciprocal, multiply each original F32 component by that retained reciprocal, and store each component F32. There is no intervening F32 store of the square root or reciprocal. A zero pair has no demonstrated fallback branch; original exception masks/NaN propagation remain Unknown. The research normalizer rejects it explicitly.

Next, for i=255 down to1, draw j=draw%256 and swap permutation[i]/permutation[j]. This is **not** the usual shuffle restricted to j<=i. It consumes255 draws. Finally,258 iterations append source[i] to destination[256+i] for all three tables. Sources256/257 are the values just appended from0/1, producing514 entries. The explicit seed rebuild consumes1023 returned RNG draws and, for the selected positive seed,1353 recurrence advances including330 initialization steps.

Confidence: Confirmed table extents, call order, normalization stores, swap range and duplication. Consequence: preserve those exact operations rather than a library permutation/gradient initializer. The native and Fraction models compare full private table hashes for three seeds and three table-construction profiles; no generated table corpus is published.

## Conversion helper and lattice arithmetic

`0131E27C` saves the control word, sets rounding-control bits10/11 to11 (toward zero), loads that word, executes FISTP signed64, restores the saved control word, and returns low32 in EAX/high32 in EDX. It pops the converted ST0. For finite in-range values this closes truncation independently of the caller's prior rounding mode. A trapped conversion may not reach the restore; exception recovery is not established. Nonfinite/out-of-range masked results are not adopted as a safe-input contract.

Noise `013E66F0` first adds exact F324096 (`45800000`) to each coordinate under query arithmetic. It passes a duplicate to the helper, retaining the biased value. In the finite signed32 lattice domain, a negative noninteger biased value decrements the truncated low word, implementing floor; a negative exact integer is unchanged. The low word is masked to255, and its next neighbor is independently masked. Inputs whose converted lattice integer lies outside signed32 are rejected by the research model; original low-word wrap behavior is not silently repaired or labeled safe.

**Asymmetric storage:** first fractional coordinate is stored F32 at `013E674E`. Second fraction is retained after FISUB at `013E67A3`, without an analogous F32 spill. The retained second fraction also feeds the second fade. Six synthetic comparisons demonstrate changed F32 results if that fraction is prematurely stored.

## Exact finite interpolation graph

Let R denote explicit nearest-even query precision24/53/64. Let x be the first fraction after its F32 store, z the retained second fraction, dx=R(x-1), dz=R(z-1). The four gradient indices are:

`P[P[ix & 255] + (iz & 255)]`, `P[P[(ix+1) & 255] + (iz & 255)]`,

`P[P[ix & 255] + ((iz+1) & 255)]`, `P[P[(ix+1) & 255] + ((iz+1) & 255)]`.

Permutation duplication supplies the unmasked intermediate sums. For a corner with component words gx/gy, compute its dot as `R(R(zz*gy) + R(xx*gx))`, with the four pairs (x,z),(dx,z),(x,dz),(dx,dz).

Define `fade(t)=R(R(R(3-R(t+t))*t)*t)` and `lerp(t,a,b)=R(R(R(b-a)*t)+a)`. Horizontal interpolation uses the first fade separately for the lower and upper rows. Final result is `lerp(fade(z), lowerRow, upperRow)`.

Observation: `013E67C9..E5` establishes first cubic fade, `013E6802..31` lower row, `013E683D..50` upper row, and `013E6852..67` final second-coordinate fade/interpolation. Stack cleanup `013E6869..71` leaves the computed result; `013E6876` returns without a final F32 spill. Confidence: Confirmed finite graph and operand/store order. The research receipt retains the dyadic result as well as its optional F32 inspection projection. No raw-noise range/distribution or world-unit default is inferred.

The [sampler contract](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md) still owns coordinate scaling/offset/octet stores, mode contributions, normalization, cache FST and caller-specific AHFR/FFRA scaling. This milestone tests the immediate seeded noise element, not complete terrain recursion or an original work request. Its selected MFRC controls explain which mode/optional paths a fresh instance requests; they do not reveal effective arithmetic controls or an original generated sample.

## Validation, modern policies and remaining boundary

1,004 native receipts match the independent Python Fraction/integer oracle:94 framing cases,102 shuffle-index cases,18 sequences of1024 draws,9 complete table builds,21 normalization cases,13 conversion cases and747 finite-noise/profile cases. There are135 explicit modern rejections, eight floating-index witnesses and six premature-second-fraction witnesses. These are bounded deterministic synthetic inputs; selected-family seed/controls are original metadata, while coordinates and expected generated values are conditional research vectors.

The harness rejects nonfinite used values, zero normalization length, conversions outside signed64, lattice integers outside signed32, nonfinite inspection outputs, malformed selected84-byte framing and unsupported precision profiles. The strict selected-frame inspector supports only MFRC/0001 with52 DATA bytes and rejects other framing; this is not a general original version-error policy. Tables and case receipts are managed test objects, with no production cache/ownership or deterministic-wiping promise.

Static gates pass289 instruction anchors,520 recurrence comparisons, eight wrong EXE identities and40 wrong config/source/TGEN/family/MFRC identities. Four external EXE/config mutations/truncations reject before imports/decoder/archive/output, using absent decoder/asset paths. Frozen regressions pass: sampler2466; AHFR151 raw/8836 consumers; FFRA152 raw/2326 queries; carrier6354; filtered1141, with existing ownership/budget checks. The actual Core/reference build has zero warnings/errors. Production baseline files are unchanged; default smoke was not rerun for this research-only milestone.

Remaining Unknown: original table/query precision and rounding, exception masks, zero-vector/invalid-coordinate failure recovery, sine byte after later mutation or reused loading, FSIN/log/exponent-helper bit behavior, other MFRC versions, borrowed-cache lifetime/quiescence, and original generated-sample reachability. No heap allocation appears in the examined RNG/table/interpolation bodies; they write embedded storage progressively and restore normal stack/register state on successful returns. Their failure/thread cleanup is not established by that observation.

Exact next probe: recover effective FPU-control provenance for fresh family construction and the selected sampler callers, including helper restoration and any caller/callee changes; then join this conditional seed/noise element to the recovered two-octave miss graph while preserving stores and fresh optional-state provenance. Keep production noise and original terrain admission deferred until that relevant chain is justified.

## Round48 compatibility increment

The independently reviewed [actual conditional Core kernel](TERRAIN_SEEDED_NOISE_KERNEL_CONTRACT.md) passes1155 oracle cases; [864 computed consumer joins](TERRAIN_COMPUTED_NOISE_CONSUMER_JOIN_CONTRACT.md) preserve unsigned64 and retained/reload boundaries. This is immediate conditional implementation, not full original sampling. Existing [worker startup evidence](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md) bounds the unresolved controls; next viable work is explicit mode0 octave/cache composition.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.
