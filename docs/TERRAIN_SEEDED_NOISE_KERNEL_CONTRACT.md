# Conditional owned seeded noise element

Date: 2026-10-04. The engine-neutral [kernel](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainSeededNoiseKernel.cs) implements only the immediate two-coordinate element established by the [Round47 seed/noise evidence](TERRAIN_FRACTAL_SEED_NOISE_CONTRACT.md). It does not implement a complete fractal sampler, octaves, coordinate scaling, modes, cache hits, family loading or terrain generation. Original arithmetic-control parity remains Unknown.

## Evidence and controls

Observation: the SHA-gated executable and Round47 instruction/asset evidence establish the 322-word shuffled RNG, 514-entry permutation/scalar/2D-gradient tables, exact draw and interpolation ordering, and asymmetric fraction storage. Confidence: Confirmed for the finite graph under stated controls. Interpretation: an independently authored explicit rebuild can reproduce that conditional element without the legacy object's layout or constructor. Consequence: callers must explicitly supply a signed seed and table/query precision; no installed default is inferred.

`Create(signedSeed, tablePrecision, tableByteBudget, workBudget, cancellation)` accepts table precision24/53/64. `Query(coordinates, queryPrecision, byteBudget, workBudget, includeInspection, cancellation)` separately accepts query precision24/53/64. All arithmetic uses software nearest-even with bounded finite operands and a wide exponent; it does not read or modify host FPU controls. Those controls are a conditional modern choice, not a recovered original setting. Coordinates are explicit finite F32 bit patterns.

The RNG uses the recovered Schrage recurrence,330 initialization advances and322 shuffled entries. Index selection truncates the **rounded floating division** by6669205. Initialization consumes three draws per gradient in scalar/X/Z order. Normalization retains the square root/reciprocal before the component F32 stores.255 swaps select the other index from the full256 range;258 copies yield514 entries. Ordinary successful rebuilds consume1023 draws/1353 recurrence advances. Zero/negative signed inputs, including signed minimum, normalize to1 according to the reset graph.

Modern policy: raw signed maximum/modulus seed is rejected, rather than adopting its original zero-output/reinitialization edge. A zero normalization vector rejects construction; no fallback direction or partial kernel is published. Constructor shared-global RNG consumption is deliberately excluded from this explicit rebuild. Earlier default construction and global-stream integration remain separate.

## Query and result boundary

Query adds4096 under query precision. Finite signed64 truncation followed by negative noninteger floor correction is restricted to signed32 lattice eligibility; invalid coordinates reject. The first fraction is stored F32, the second retained. The permutation lookup, gradient dot, cubic fade and horizontal/vertical interpolation preserve the Round47 graph. The final value is retained; no unconditional F32 spill is introduced.

Each immutable `Sample` returns unsigned64 `Magnitude`, `Negative` sign (including negative zero), and binary `Exponent`: signed magnitude times2 raised to exponent. Nonzero significands are reduced to odd magnitude, preserving all retained bits. Zero has exponent0. `InspectionBits` is nullable and computed only when explicitly requested; it is an inspection projection, not cache provenance or a full-sampler return.35 independently compared results require a magnitude above signed64 maximum. No sibling Round48 sample factory or MFRC API is used.

## Ownership, allocation and failure policy

The creating managed thread owns the kernel. Query, table-copy and disposal from a foreign thread reject before use. Tables are privately held immutable arrays; there is no mutable query cache, external callback, input enumerator, I/O or shared RNG. Input is a bounded span copied into owned storage. This API cannot synchronously reenter through user code during an operation; an active-operation guard additionally rejects an active query/copy/dispose. Reentry exclusion is structural, not an observed legacy thread rule.

Construction requires logical table/scratch payload budget9512 bytes and fixed build-work allowance20000; accepted upper limits are1048576. Counts are logical payload/work accounting, independent of CLR object overhead or processor instruction counts. Checks precede table arrays and arithmetic. Fixed loops bound build work; arithmetic operands derive only from fixed-width seeds and finite F32 values. The reused software square-root routine has a32-iteration bound.

At most4096 query coordinates are accepted. The aggregate budget checks reserve40 bytes per point (8-byte copied coordinate plus32-byte retained-result allowance) and256 logical work units per query before copying or arithmetic. Byte/work maxima are163840/1048576. No arbitrary exponent/significand input is accepted. Arithmetic intermediates have exponents bounded by finite F32 coordinates and the fixed graph; invalid lattice domains reject before interpolation.

Batch output is published only after every query and the final cancellation check succeed. A late invalid coordinate yields no partial returned collection and changes no tables. Cancellation is checked before allocation, throughout table generation/shuffle and before each query, then again before publication. Failure clears the active-operation flag and leaves an existing kernel usable. Factory failure never returns a kernel. Owner disposal is idempotent and releases its table reference; queries/table-copy after disposal reject. Already returned samples and copied snapshots survive disposal independently. Managed memory is not promised to be wiped deterministically.

`CopyTables` requires8224 logical payload bytes before cloning, capped at1048576, and returns independent read-only collections. This diagnostic boundary lets tests export complete private hashes without publishing a derived table corpus or mutable table view.

## Verification and limits

The [scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainSeededNoiseKernelScenarios.cs) and [runner](../../SWG-Godot-Client/poc/round48-tests/Rendering/Invoke-Kernel.ps1) compare1155 native cases to the frozen Round47 Fraction/modular-integer oracle;186 deliberately reject. Full table hashes and exact retained rationals/projections compare across independent table/query profiles. Synthetic additions cover negative/zero seeds, signed minimum, large positive seeds, subnormals, signed zero, negative lattice boundaries, invalid domains, modulus rejection and a bounded discovered synthetic zero-gradient seed.24 API negative checks cover precision, seed, budget, cancellation, late failure, foreign-thread use, immutable copy mutation and disposal. Snapshot survival, optional inspection and owner recovery pass.

The actual Core/reference build, default smoke, frozen Round47 seed1004/index/recurrence/static/identity gates, sampler2466, AHFR151 raw/8836 consumers, FFRA152 raw/2326 queries, carrier6354 and filtered1141 regressions pass. Original executable execution count remains zero. Selected original parameters are SHA-gated and derived dynamically into private fixtures only; public sources retain no original seed/payload/table corpus.

The existing [arithmetic runtime policy](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md) already closes the CreateThread/CRT/terrain-worker route, baseline no-op hook0131DB40, absence of a local worker install in the inspected bodies, and mutable foundation precision-setter ordering. Those observations do not establish the active worker control word; this milestone does not repeat that startup investigation.

Unknown: effective original table/query controls and exception masks, original zero-vector/invalid-input recovery, full miss graph/octaves/modes, sine/log/helper curves, cache/family lifecycle, constructor shared-global integration, original generated terrain and live compatibility. Next action: bounded curves-disabled mode0 octave/miss/cache composition using accepted frozen pieces, with explicitly supplied table/query controls and preserved miss-return/cache-store boundaries. Original generated parity remains deferred. Lead owns canonical reconciliation and acceptance.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.
