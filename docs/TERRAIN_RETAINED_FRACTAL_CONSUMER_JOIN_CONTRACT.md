# Supplied retained fractal sample composition

The Round47 join extends the [supplied-F32 composition](TERRAIN_SUPPLIED_FRACTAL_CONSUMER_JOIN_CONTRACT.md) to the bounded [retained sample API](TERRAIN_RETAINED_FRACTAL_SAMPLE_CONTRACT.md). It supplies exact signed binary significands/exponents and separately reloads their F32 cache projections. It generates neither noise nor original work requests and does not execute an original cache.

The independent [peer](../../SWG-Godot-Client/poc/round47-tests/Integration/retained_fractal_join_peer.py) composes root AHFR, scaled FFRA query, retained layer shape/minimum, F32 local/product storage and child AHFR. It uses Fraction arithmetic, literal synthetic IFF inputs and explicit24/53/64 profiles. The same sample travels through the native supplied-wide and cache-reloaded paths. Old F32 callers remain a separate regression gate.

786 fixtures include768 cases derived from at-most48-bit products and18 wider midpoint cases. The first set alone cannot show unit-scale FFRA double rounding at binary53. The added midpoint cases can: multiplying a wider supplied value by1 at53 bits can first round to an F32 tie, while a direct cache F32 projection can resolve the other way. This is a conditional supplied-input witness, not an observed original family sample. General-scale and AHFR height/result differences are also compared.

Native comparison passed all786 cases:100 AHFR,67 FFRA and124 final-result miss/hit differences, including nine unit-scale double-rounding witnesses. Actual Core/reference builds have zero warnings/errors. No expected native data is used to generate fixtures. The first independent script referenced a nonexistent shape helper; it was corrected to the existing retained layer oracle. The layer minimum's mandatory F32 accumulator store was preserved before product/child execution.

**Compatibility consequence:** maintain the supplied sample's retained value until each confirmed store, and distinguish supplied-wide provenance from cache reload. **Remaining Unknown:** effective original precision/rounding/exception state, full sampler/optional postprocessing, pointer reload quiescence and original generated-sample reachability. **Next:** join an independently closed conditional seed/noise element to the octave/cache graph without promoting it to original parity.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.
