# Slope filter input, endpoint operation graph and cache parity boundary

[TerrainFilterSlopeInput](../../SWG-Godot-Client/poc/Stardust.Core/Assets/TerrainFilterSlopeInput.cs) preserves bounded owned `FSLP` serialized records. Direct factory, RTTI, loaders, setters and query identify this as FilterSlope. **No original-parity influence implementation is supplied:** endpoint preprocessing executes x87 `FSIN`, whose result/control contract is not established by the existing exact arithmetic primitive. The [Round 43 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R43_SLOPE_FILTER.md) records evidence, reproduction and the exact next probe.

Subsequent implementation: the [ordered slope policy](TERRAIN_ORDERED_SLOPE_PREVIEW_CONTRACT.md)
supplies an explicitly modern binary64/library-sine evaluator for six unfeathered
original branches. The statement above describes the original-parity raw-input
boundary; no original bit-exact influence implementation is claimed. The modern
adapter preserves the recovered graph and reports original parity false.

The raw decoder performs no endpoint conversion, transcendental calculation, normal generation, layer adaptation or original query execution. The conditional query equations below describe observed instructions given actual cached words; they are not a claim that raw serialized endpoints can yet produce those words reproducibly.

## Direct identity and evidence

All VAs refer to executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed before each current static PE/Capstone/RTTI read.

| Observation | Interpretation / confidence | Implementation consequence |
| --- | --- | --- |
| Factory compares `46534C50` at `00B7A181`, allocates 0x2C at `00B7A198`, constructs through `00B789E0` at `00B7A1B2`, loads through +8 at `00B7A1C9`. Constructor installs vtable `015ED72C`; COL `016263F0` names type descriptor `018B8AB0`, `.?AVFilterSlope@@`. | Actual FSLP identity, Confirmed. | Do not use the adjacent FSHD factory/class at `00B79460`. |
| Constructor passes kind 2/FSLP to `00B633E0`, clears +1C/+20/+24/+28, then invokes both endpoint setters with raw zero at `00B78A26/2E`. Common base clears selector/amount +14/+18. | Fresh scalar defaults and derived-cache construction, Confirmed. | Absent serialized selector/amount resolves to runtime +0; derived cache values require setter semantics, not assumed literal ones. |
| Vtable +8/+C/+10/+14 resolve to `00B78B50`, `00B78E80`, `00B65FE0`, `00B78A90`; +18 is `00B79020` returning true, +1C is `00B63430` returning false. | Loader/writer/amount/query and predicates, Confirmed. | Predicate instruction results are known; complete producer/cache meaning is not invented. |
| Dispatcher subtracts 0000 then decrements twice, selecting `00B78C10`, `00B78CC0`, `00B78D70`. Their four-byte read counts are 2/3/4. | Three layouts, Confirmed. | Preserve exact raw words and version-specific absent fields. |
| Version 0001 first read at `00B78CFA` is discarded before endpoint reads `00B78D07/2E`. Both endpoint reads in all versions multiply two constants and spill F32 before setter calls. | Discarded prefix and endpoint conversion graph, Confirmed; prefix meaning Unknown. | Keep raw prefix/endpoints; do not expose converted fields as recovered raw data. |
| Setter `00B78F40` stores its selected parameter at +20 and `FSIN` result at +28; `00B78FB0` stores +1C and result at +24. Six finite/exceptional branch sites contain `FSIN`. | Transcendental cached-bound preprocessing, Confirmed; bit-exact outcomes Unknown. | A Fraction-only or library-sine substitution cannot establish compatibility. No production influence API is added. |
| Query reads fifth argument +3C, descriptor stride +0/data +8, row*stride+column, twelve-byte cells and F32 component +4 at `00B78A96..BE`. | Current-height normal Y, Confirmed by the subsequent producer trace. | Use the separate query-time triangle accumulation cache; see the [producer contract](TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md). |
| Query compares against cached +24/+28 with strict endpoints, retains feather span, and uses lower then upper ramps. It contains zero calls. | Finite conditional graph, Confirmed. | Separate it from unclosed raw-to-cache conversion. |
| Destructor `00B78A80` tails to common filter/header teardown `00B63410`; deleting wrapper `00B78A50` optionally releases the object. | Scalar/cache words plus owned IHDR lifetime, Confirmed. | Modern input uses immutable managed storage; no external handles or deterministic legacy destruction timing. |

## Exact raw layouts

One `FORM FSLP` contains one version form. Recognized versions require ordered `FORM IHDR`, then leaf `DATA`. Container lengths use accepted big-endian IFF framing; DATA words are little-endian U32 bits with observed F32/runtime uses.

| Version | DATA order | Exact bytes |
| --- | --- | --- |
| 0000 | First raw endpoint; second raw endpoint. | 8 |
| 0001 | Discarded legacy raw U32; first raw endpoint; second raw endpoint. | 12 |
| 0002 | First raw endpoint; second raw endpoint; selector U32; amount raw F32. | 16 |

`FirstThresholdBits` and `SecondThresholdBits` preserve serialization order without labeling axes or endpoint units. Nullable prefix/selector/amount distinguish absent serialized words from runtime defaults. `CopyDataWordBits()` returns every DATA word in order; `CopyEncodedBytes()` returns the whole independent source. All F32 categories are accepted structurally, including signed zeros, subnormals, infinities and NaNs; `HasFiniteThresholds` and nullable amount-finite inspection do not change acceptance.

Known IHDR 0001 preserves enabled U32 and a NUL-terminated raw name; IHDR 0000 adds three subsequent raw bytes, using the accepted [TGEN header convention](TERRAIN_GENERATOR_INPUT_CONTRACT.md). Names have no inferred encoding. Header and field objects remain immutable and array accessors copy. Nonzero enabled indicates local enablement under that accepted convention.

Unsupported FSLP or IHDR versions retain the bounded whole source opaquely with null typed fields/header and no interpreted DATA words. That inspection policy does not assert safe original unknown-version acceptance. Known malformed tag/order/extent, missing/duplicate children, trailing DATA bytes, unterminated names and exceeded caps reject.

## Proved conversion graph and remaining blocker

Observed literal words are:

| VA | Raw F32 bits | Observation |
| --- | --- | --- |
| `015ED6D8` | `40490FDB` | First loader multiplier. |
| `015DC5F0` | `3BB60B61` | Second loader multiplier. |
| `015DEF20` | `43340000` | Writer numerator multiplier. |
| `01947228` | `00000000` in the mapped file | Live setter clamp/subtraction bound; a helper writes it. |

For each serialized endpoint `s`, loaders retain the first product, then store `a = F32(R(R(P*s)*Q))` to the setter argument, where P/Q are the first two literal values. Setter 1 consumes the first endpoint; setter 2 consumes the second. The October 13 follow-up below establishes the nominal degree/radian interpretation from both conversion directions and real endpoints; the raw API names remain neutral.

Initializer helper `00B77670` loads P, multiplies literal 0.5 and stores F32 to `01947228`. The [startup initialization investigation](TERRAIN_FILTER_SLOPE_INITIALIZATION_CONTRACT.md) closes its registration: table slot018560AC contains thunk00B77660, which jumps to the helper. On the returning startup path it stores exact3FC90FDB before ordinary application entry00401810. The earlier literal helper-pointer search missed the thunk. Universal ordering before constructors inside earlier callbacks and later indirect writes remain Unknown; mapped-file zero is not the successful post-helper runtime bound.

Given the live bound C, each setter selects 0 when argument <0, C when argument >C, otherwise the original argument (equal signed-zero selection preserved for finite comparisons). It stores that selected argument without popping, subtracts it from C, executes `FSIN`, and stores the result to the associated cached word. The exact `FSIN` numerical contract and effective rounding/precision/exception controls remain Unknown. No production library sine, synthetic normal or guessed cache is substituted.

Version-2 selector setter `00B65FD0` is raw. Loader `00B78E18..55` clamps finite serialized amount to [0,1] before raw amount setter +10; equal negative zero survives. Older versions retain constructor selector/amount defaults. The writer emits 0002: first writes `F32(R(R(B/P)*stored+20))`, second similarly from +1C, then selector/amount. This is a runtime conversion/writer path, not a guarantee of raw byte round-trip after loading.

## October 13 bounded endpoint closure

**Confirmed operation graph:** the 0002 loader reads four-byte endpoints at `00B78DAA` and `00B78DD1`, multiplies P then Q, spills F32 at `00B78DC1/DE8`, and calls `00B78F40/FB0` at `00B78DC4/DEB`. P is rounded binary32 pi (`40490FDB`); Q is rounded binary32 1/180 (`3BB60B61`); writer B is exactly 180 (`43340000`). The initialized C is the literal `3FC90FDB`, exactly 13176795/8388608, rather than mathematical pi/2. Nominal serialized degrees and internal radians are **Strongly inferred** from these paired operations, the C clamp and original endpoint range; operation order and widths are **Confirmed**. No NGE source is needed to establish the graph.

The finite setter graph, with R still unspecified, is:

```text
alpha = F32(R(R(P*s)*Q))
a = 0 if alpha < 0, C if alpha > C, otherwise alpha
store F32 a
beta = R(C-a)                 // retained; no F32 spill before FSIN
store F32 FSIN_original(beta)
```

First endpoint stores angle+20/cache+28; second stores angle+1C/cache+24. In-range subtraction/sine/store sites are `00B78F99/F9F/FA1` and `00B79009/900F/9011`; the lower/upper clamp branches execute the same retained subtraction/sine/store graph at their respective six sine sites. Neither setter body contains a call or `FLDCW`, `FNSTCW` or `FSTCW`. `FNSTSW` reads status, not the control word. The [arithmetic policy](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md) does not prove controls at every asset-loader invocation.

**Confirmed conditional branches, not exception parity:** lower comparison tests AH & 5 with parity; upper comparison tests AH & 0x41 for nonzero. If unordered comparisons complete, status 0x45 routes through both tests to reload the original argument and `FSIN`; there is no explicit NaN sanitizer. Finite equal zero/C keep the supplied argument. Exception masking, signaling-NaN behavior, NaN payload propagation and actual transcendental results remain **Unknown**.

An unimplemented modern option is explicitly rounded binary64 basic operations in the recovered order, the existing F32 argument spill/clamp, binary64 literal-C subtraction, library sine and nearest-even F32 result. That would be a modern policy, not original x87 parity. It must not silently become cosine: an independent Fraction/140-digit Decimal reference for synthetic serialized 30 gives ideal `sin(C-alpha)` rounded F32 `3F5DB3D8`, while ideal `cos(alpha)` and mathematical cos30 round `3F5DB3D7`. This one-ULP substitution witness models mathematical functions; it is **not an observed original FSIN result**. Nearest-even24/53/64-significand basic-operation models were also checked at synthetic 0/5/20/30/60/90, without establishing active target controls.

The rehashed original Tatooine TRN (`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`) contains 24 FSLP records: all 0002, all locally enabled, all finite with `0 <= first < second <= 90`. Local enablement does not assert enabled ancestry or reachable query execution. Existing static verification again passes 119 anchors, 18 finite status cases and all six sine sites. The [bounded checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-13_SLOPE_ENDPOINT_GRAPH.md) records body identity and reproduction. **Result:** endpoint graph closed; original cached result bits remain blocked by the effective-control/transcendental contract. No Core or runtime behavior changes.

## Conditional finite query, without an implementation claim

Let L/U be actual cached F32 words +24/+28, n the selected current cell component, f the runtime amount, and R the unspecified original arithmetic rounding operation. Query returns positive zero if `n <= L` or `n >= U`. Otherwise its retained graph is:

```text
span = R(R(R(U-L)*f)*0.5)
if R(L+span) > n: q = R(R(n-L)/span)
else if R(U-span) < n: q = R(R(U-n)/span)
else: q = 1
```

Equality at feather thresholds selects interior one; equal/reversed cached bounds have no finite interior. The query reloads its selected F32 component but does not spill span or final influence. Generic layer filter shaping applies unsigned selector 0 identity, 1 square, 2 square root, 3 ordered cubic; other selectors return positive zero after query, with shaped F32 store at `00B640E9`. These conditional instructions are established directly, but supplied cached bounds or a raw-input influence result are not exposed by this round's production API.

The input-source structure is state +3C, stride/data descriptor and second word of each twelve-byte cell. Query arguments supply column/row; the first two coordinate arguments are not read on this path. Caller state is the existing filter state passed at `00B640A6`/query +14 at `00B640B8`. The subsequent [producer contract](TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md) confirms normal Y, current-height identity, triangle accumulation/normalization, borrowed descriptors and dirty timing. Ultimate buffer ownership and original arithmetic/control parity remain open.

## Modern raw limits and verification

Defaults: 65,536 source bytes, 64 IFF nodes, depth 8, 4,096 name bytes excluding terminator, and 65,536 logical decoded-copy bytes. Hard maxima are 4,194,304 source/copy bytes, 65,536 nodes/name bytes and depth 64; minimum source cap is 24, nodes/depth minimum 1, name/copy caps may be zero. `DecodedCopyBytes = 4*wordCount + nameBytes + legacyBytes`; source/terminator/temporary snapshots/fixed overhead are excluded. Source caps precede source/section copies; name/decoded-copy preflight precedes typed allocations. These are logical payload limits, not measured heap usage.

Null section arguments throw `ArgumentNullException`; invalid option ranges throw `ArgumentOutOfRangeException`; malformed/over-budget source throws `InvalidDataException`. Failed late decoding cannot alter earlier results. Managed temporary lifetime does not promise deterministic wiping.

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainFilterSlopeScenarios.cs) and the [independent raw-byte oracle](../../SWG-Godot-Client/poc/round43-tests/Assets/verify_slope_input.py) agree on 375 cases: 38 accepted, 337 rejected; 19 ownership/budget checks and one null negative pass. All versions, complete-record/DATA truncations, raw F32 categories, unknown versions, malformed child order/extents, exact/one-less source/name/copy budgets, input mutation and late failure are covered. Static verification checks 119 selected instruction anchors, 18 finite status-mask cases, exact RTTI/vtable, read counts and all six `FSIN` sites. One pinned original raw occurrence is compared; no query/arithmetic/transcendental parity is claimed. The separate runner and actual Core/default smoke builds and runs pass with zero warnings/errors.

Exact next numerical probe: establish a justified x87 `FSIN`/F32 cache-result contract at `00B78F9F` and `00B7900F`, including effective controls, or explicitly choose a modern policy with original parity false. Startup registration/application-entry order is independently verified; universal earlier-callback constructor ordering is not. Any original execution needs a separately authorized bounded plan. The separate producer trace now closes `00B65730` to the queried state+3C descriptor. The next implementation prerequisite is a bounded modern query-time normal cache followed by an ordered shader adapter; final render normals cannot supply that cache.
