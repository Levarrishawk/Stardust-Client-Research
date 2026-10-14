# Slope-filter current-height normal producer

The Pre-CU producer `00B65730` writes the exact three-component grid consumed by `FSLP` query `00B78A90`. It constructs current-height positions, accumulates two unnormalized triangle normals per square, then normalizes each accumulated vector. Descriptor identity, axes, triangle split, finite-grid edges and dirty timing are **Confirmed** from the target. Effective x87 controls and bit-exact original numeric results remain **Unknown**. No Core or Godot behavior changes accompany this closure.

This extends the [slope filter endpoint/query contract](TERRAIN_FILTER_SLOPE_CONTRACT.md), the [coordinate producer contract](TERRAIN_SAMPLE_COORDINATE_PRODUCER_CONTRACT.md), and the [current-height filter contract](TERRAIN_FILTER_HEIGHT_CONTRACT.md). The [bounded checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-13_SLOPE_NORMAL_PRODUCER.md) records reproduction and scope.

## Input identity and evidence boundary

All VAs refer to executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, checked before VA interpretation. Producer body `[00B65730,00B65AEA)` is 954 bytes, SHA-256 `081AA87C019BA78CD777215C8B8F76AE0B9DFFE798AA66CE2F3C57DFCEEAF4CB`. The previous coordinate-only boundary at `00B657F9` precedes the normal calculation and is not the full body boundary.

The [static verifier](../tools/python/verify_terrain_slope_normal_producer.py) checks 13 body identities, 71 instruction anchors, literal words, PE file/mapped-image agreement, three wrong-input rejections and three separately labeled synthetic body mutations. It never executes the original. These checks preserve the observed instruction evidence; they do not independently prove all arithmetic interpretation or numeric parity.

## Borrowed descriptors and component axes

| State field | Direct observation | Meaning / consequence |
| --- | --- | --- |
| `+0`, `+4` | Loaded at `00B65744/47`. | Signed grid side N and F32 sample spacing; loops use N, not descriptor dimensions. |
| `+0C/+10/+14` | Copied at `00B65750..6E`; used at `00B65790..E9`. | F32 X/Y/Z origin words. |
| `+18` | Loaded at `00B65739`; stride at descriptor +0 and data at +8; four-byte cell load at `00B657BC`. | Current height grid, identical state field read by FHGT and changed by AHCN. |
| `+38` | Loaded at `00B6573C`; twelve-byte cell stores at `00B657D2/E3/E9`. | Current XYZ positions. Y is current height plus Y origin. |
| `+3C` | Loaded at `00B65740`; zeroed at `00B657F9..00B65840`; added and normalized below. | XYZ normal grid. FSLP loads this descriptor at `00B78A99` and its Y word at `00B78AB7`. |

For column c and row r, the height address is `data + 4*(r*stride+c)` and each vector address is `data + 12*(r*stride+c)`. Vector getter `00B66580` uses precisely this latter calculation. Positions are stored as X = column*spacing + originX, Y = currentHeight + originY, Z = row*spacing + originZ. Each coordinate is F32-stored after the corresponding retained x87 arithmetic. Positive spacing gives increasing X across columns and increasing Z across rows.

Builder `0084A530` constructs the state at stack `[ebp-614]`. It obtains bufferBase from context `[ebp-90]+4` (local `[ebp-8C]`), then binds state +18 to bufferBase at `0084A735`, +38 to bufferBase+60 at `0084A729/3B`, and +3C to bufferBase+6C at `0084A732/52`. The context receives the address of the caller appearance's +70 member at `0084A61D..27`; wrapper `00849040` forwards it to `00B5CF20`, which stores it at context+4 (`00B5CF28`).

**Ownership:** the state and producer borrow these caller-supplied descriptors. The producer performs no allocation, freeing or pointer replacement. State constructor `00B63250` clears pointer fields; destructor `00B632D0` clears them, including +38/+3C at `00B632EA/ED`, without a freeing call. Ultimate buffer allocation/destruction, allocation failures and concurrent use are outside this bounded trace.

## Triangle accumulation and finite-grid edges

Let Q00=P(c,r), Q10=P(c+1,r), Q01=P(c,r+1), Q11=P(c+1,r+1). Each square uses diagonal **Q10–Q01**. The position differences are F32-stored before cross products; each cross-product component is also F32-stored before accumulation.

1. A = Q11−Q01 and B = Q01−Q10. Upper triangle normal is B×A. Difference construction is at `00B65891..00B658E7`; cross-product construction is at `00B658EC..00B65927`.
2. C = Q00−Q10 and D = Q10−Q01. Lower triangle normal is D×C. Differences are at `00B65941..00B65988`; cross product is at `00B6598D..00B659C8`.
3. Add the upper vector to Q01, Q11, Q10, then add the lower vector to Q10, Q00, Q01 (`00B659CD..00B65A97`). These are unnormalized cross products, so accumulation preserves triangle area weighting. Each XYZ addition is FADD followed by F32 store, either inline or through `00507820`.

For a cross product E×F, each component retains its two products until subtraction and then spills F32. For example, X is the F32 store of retained `(Ey*Fz − Ez*Fy)`; this is not a multiply-rounded-to-F32 policy. Helper `00413760` only copies three supplied words and introduces no arithmetic.

The square loop is row-major, columns inside rows, with c,r in 0..N−2. The normal grid first receives zero bytes over `12*descriptor[0]*descriptor[4]` bytes; there is no up-vector initialization. Edges and corners receive only faces within this finite N×N domain. There are no outside samples, central differences, extrapolation, per-face normalization or edge repair in this body. N=1 has no squares and leaves zero vectors after normalization's short-vector path. The body does not validate descriptor consistency or modern malformed-input limits; modern adapters must supply their own validation rather than interpreting unchecked accesses as acceptance.

An independently authored geometric witness is a unit 2×2 grid with heights Q00=Q10=Q01=0 and Q11=1. Its unnormalized upper face is (−1,1,−1), lower face is (0,1,0), giving accumulated Q00=(0,1,0), Q11=(−1,1,−1), Q10=Q01=(−1,2,−1). Real-arithmetic normalized vectors distinguish this topology from a central-difference normal policy. This is a synthetic explanatory fixture, not captured original output or a bit-parity oracle.

## Normalization operation graph

Every N×N vector is passed to `00AB5C40` at `00B65AD4`; the Boolean result is ignored. The magnitude computation uses retained x87 products and sums in order **Z²+X², then +Y²**, followed by `FSQRT` at `00AB5C58`. No F32 magnitude spill occurs before comparison.

The comparison at `00AB5C60` uses F32 literal `015EAAF8`, bits `3727C5AC` (9.999999747378752e−6). A finite magnitude strictly below that literal returns AL=0 without changing XYZ. Equality takes normalization. Normalization divides literal 1 (`015DC6F8`, bits `3F800000`) by the retained magnitude, multiplies the retained reciprocal into each component and F32-stores X/Y/Z at `00AB5C7E/85/8B`; it returns AL=1. There is no zero-vector-to-up fallback.

If an unordered comparison completes, the observed status-mask branch also takes the normalization path. This does not establish exception-mask, NaN-payload or nonfinite parity. Neither producer nor normalizer contains a control-word load/change, so effective PC/RC/exception settings are not closed here. Endpoint `FSIN` remains a separate numeric boundary. The graph is implementation-ready under a declared modern arithmetic policy; original numeric parity remains false.

## Query-time cache and dirty timing

Top driver `00B65280` sets state dirty byte +570 at `00B65324` before root traversal. Layer driver `00B63D20` scans locally enabled filters' virtual +18 demand predicates; FSLP's `00B79020` returns true. When a demanding layer encounters dirty state, `00B63E98/99` passes the same state to the producer and `00B63EA1` clears +570. This happens before the layer's per-cell filter/affector loop. The same state is passed to filter query at `00B640A6/00B640B8`.

An affector whose virtual +14 returns true marks +570 at `00B641A1`. This does not rebuild midway through that layer's cells: the next demand occurs at a subsequent layer entry. The top driver additionally regenerates at `00B65381` if dirty after root traversal, then clears +570 at `00B65389`.

The ordered integration follow-up rehashed the target and checked four admitted
height-writer vtable +14 slots. AHCN vtable `015ED678` selects `00B76640`, AHFR
`015ED698` selects `00B76980`, AHTR `015ED6BC` selects `00B771A0`, and AROA
`015EDD70` selects `00B87DF0`. Each body is exactly `B0 01 C3` (`mov al,1; ret`).
These demand-invalidating return values are **Confirmed**. Mark dirty after the
enabled writer is processed even if its amount gate leaves the height unchanged.
The static verifier now additionally checks these four slots/bodies and rejects
eight synthetic slot/body mutations, without changing the historical 13-body/
71-anchor producer inventory. Ultimate controls and original numerical parity
remain unknown.

**Implementation consequence:** a slope-shader adapter needs a separate query-time cache over the ordered current-height state. It must not use final heights or the [modern render-normal policy](TERRAIN_MODERN_HEIGHT_NORMALS_CONTRACT.md). A later height affector can change final render normals after a slope branch has already queried its earlier cache. Full traversal, every affector's demand/dirty behavior, and original buffer padding/window conventions beyond the current coordinate contract are not asserted by this trace.

NGE `TerrainGenerator.cpp::generate` independently corroborates the diagonal, accumulation recipients and final normalization. It is supporting reference only; the target instructions above establish these findings. The exact next implementation step is a declared modern numeric filter-normal cache and ordered shader evaluation adapter with explicit admitted branches. Preserve original parity as false until effective arithmetic controls, FSQRT/F32 results and endpoint FSIN output are independently justified.
