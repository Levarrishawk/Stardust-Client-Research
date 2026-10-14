# Bounded delta-time texture cursor

Round28 Runtime, 2026-10-03. DeltaTimeTextureCursor (external historical reference) implements the local selector shape established by the accepted [SWTS runtime contract](SWITCH_TEXTURE_RUNTIME_CONTRACT.md): zero initial index/accumulator, advance on an exact boundary, repeated subtraction and modulo wrap, retained residual. Numeric safety, counters, ownership and work limits below are modern implementation policy. Original x87 precision/control-word parity and outer clock units remain unverified.

## API and receipts

Namespace `Stardust.Core.Runtime`:

```csharp
DeltaTimeTextureCursor(int frameCount, float interval,
    int maximumCatchUpSteps = 4096,
    ulong revisionSeed = 0, ulong cumulativeAdvancesSeed = 0)
DeltaTimeTextureCursorSnapshot Advance(float delta)
DeltaTimeTextureCursorSnapshot Reset()
DeltaTimeTextureCursorSnapshot Snapshot { get; }
bool IsCurrent(DeltaTimeTextureCursorSnapshot snapshot)
bool IsClosed { get; }
void Close() // Dispose calls Close
```

Frame count is any positive int; no frame array is allocated. Interval must be positive finite float32. Delta must be finite nonnegative float32; negative zero is accepted. Interval and delta are in explicitly matching abstract input units. The cursor has no scheduler, seconds conversion, engine, archives, callback or dependency beyond the existing Core runtime.

Receipts are immutable, owner-bound objects exposing FrameCount, Interval, Index, Residual, Revision, CumulativeAdvances, LastAdvanceSteps and MaximumCatchUpSteps. They explicitly expose `NumericPolicy = binary64-intermediate-float32-persistent`, `InputUnits = abstract-matching-units`, and `OriginalX87Parity = false`. An exact receipt is current only while its owner is open and the same receipt remains issued; equivalent state from another cursor is not a token.

Initial index/residual/last steps are zero, with caller-supplied checked-counter seeds. Every successful Advance, including zero/noop, increments revision once. Reset increments revision, zeros index/residual/last steps, and preserves cumulative advances. Close is idempotent, invalidates current receipts, preserves the last snapshot and counters, and does not increment revision. Snapshot reads remain available after close; writes throw. All operations require the creating managed thread. No external callback can cause reachable same-thread reentry; the busy gate is inspected rather than dynamically exercised.

## Exact modern numerical policy

An update converts the persisted residual and input float to binary64, adds them, and repeatedly subtracts the exact float32 interval promoted to binary64 while remainder is at least interval. Each subtraction advances one frame; index wraps to zero at FrameCount. The final double remainder is cast once to persistent float32. This does not use integer elapsed-frame division, clamp to a single step, or silently discard catch-up work.

The stored residual is not required to be strictly below interval: float32 rounding can produce equality although the temporary double remainder is below it. Equality is preserved without clamp/reset, so a subsequent zero-delta call may advance. For interval bits `3DCCCCCD`, inputs `32700000` then `3E4CCCCC` produce one step and stored residual `3DCCCCCD`; zero then produces another step and positive-zero residual.

Partitioned updates need not have bit-identical residuals. Ten float32 0.0125 inputs versus one float32 0.125 input produce residual bits `3CCCCCD0` and `3CCCCCCC` respectively for the original interval. This is an observed modern fixture result, not a claim about original x87 output or mathematical equivalence of all float partitions. Terminal interval `3DCCCCCD` corresponds to about 0.10000000149011612 abstract units, not established seconds or 10 Hz.

## Bounded admission and atomic publication

Work budget is 1 through 1,048,576 steps per Advance. A constant-time conservative admission check rejects clearly excessive totals before looping. For total T, budget B and promoted interval I, use E = B * (nextDouble(T) - T); reject if T >= (B+1)*I + E. At this work ceiling the float32 interval/product fits binary64 exactly; E conservatively covers subtraction rounding across at most B steps. Near-boundary cases still use the actual repeated-subtraction loop to establish the exact result. The loop checks budget before each subtraction and executes at most B subtractions. Extremely large delta/minimal interval ratios, including subtraction that cannot change the double total, cannot cause unbounded work.

Invalid delta, exhausted work, checked revision/cumulative overflow and reset overflow publish no candidate. Inputs, counters, all bounded arithmetic and receipt allocation precede the sole current-reference assignment; rejection leaves exact prior receipt/index/residual/counters unchanged. There are no callbacks or commit-time checked operations. Cumulative advances count all successful frame advances, including full wraps and count-one updates.

## Evidence and integration limits

Native scenarios (external historical reference) pass 870 checks across 36 cases. The independent Python reference (external historical reference) checks 180 operations over synthetic bit fixtures (external historical reference), with exact neighbor/boundary results, count1/8, multiple wraps, extreme/subnormal/maximum floats, full-work acceptance/rejection, rounded residual equality followed by zero, repeated drift/partition differences, invalid constructor/delta inputs, overflow, reset, owner, closed and immutable receipts. Four updates use their entire work budget, including 1,048,576 steps. The oracle performs a bounded reference loop and does not reuse the production admission formula.

Confirmed: modern implementation and deterministic fixture outputs. Correlation only: terminal frame count8/interval bits from accepted static research. Unknown: outer scheduler units, pause/culling/thread and first-visible-frame behavior, original floating-point control word and original numerical parity. No original executable or assets run/load. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R28_DELTA_TIME_TEXTURE_CURSOR.md).

Next: an engine adapter can supply explicitly documented matching units and own a cursor per runtime wrapper. It must separately ensure FrameCount does not exceed available authored variants. Do not infer a universal original count/list rejection rule, seconds, or rendering parity from this cursor.

## Round28 outer-clock reconciliation

The concurrent [clock investigation](SWITCH_TEXTURE_CLOCK_CONTRACT.md) closes seconds and scheduler quantization for the examined ordinary GroundScene route. This cursor still accepts supplied abstract matching units and does not implement that scheduler. Reachable PC00/RC00 is documented; actual callback control word and original numerical parity remain unclaimed.
