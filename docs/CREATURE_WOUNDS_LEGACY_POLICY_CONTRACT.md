# Recovered bounded integer-vector policy

`CreatureWoundsApplicationPolicy.LegacyBounded` is an explicit opt-in to `CreatureView3WoundsDeltaCodec.Decode(message, limits, policy)`, including the envelope overload. The returned immutable plan retains its policy; `Apply` follows it and `Encode` preserves raw counts. `StrictInterval` remains the default with all accepted [Round9 behavior](CREATURE_VIEW3_WOUNDS_DELTA_CONTRACT.md) intact. The policy is selected out of band; it does not add a wire discriminator. Encoded legacy assignments must be decoded with the same selected policy.

The recovered rules below describe the pinned original generic integer-vector decoder and its mutation helpers before downstream callback effects. They are not inferred from the replacement implementation or server source. Original SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static export covers the exact `0043B1A0-0043B59D` decoder extent and bounded helpers/context, totaling 4,351 bytes. No original execution or new semantic names were introduced.

## Exact counter arithmetic

Let N be the raw U32 operation-loop bound, C the incoming U32 counter, and L the local U32 counter at the start of a field update. Instructions `0043B1DD-0043B1F6` load these values, compute `R = (N - C + L) mod 2^32`, then set the skip threshold to unsigned `min(R, N)`. `2B D9` at `0043B1E8` subtracts C; `03 DA` at `0043B1EA` adds L; `76 05` at `0043B1F1` is unsigned JBE, not a signed branch. Underflow wraps before comparison. There is no signed-negative normalization or impossible-interval rejection.

Both skip and apply paths maintain a separate U32 operation cursor. For ordinary commands it advances once. For assign it advances once per S32 value plus once for the command: `FF 45 EC` at both `0043B233` (skipped values) and `0043B528` (applied values), with the common increment at `0043B252` or `0043B3AD`. Thus N is not always the number of U8 command records. An assign of k values contributes k+1 cursor steps. The decoder continues until cursor reaches or exceeds N, including overshoot by a final assign. A skipped assign consumes its complete counted body and may cross the skip threshold; it is not partially applied. This changes parsing, not only arithmetic.

There is no final `L = max(L,C)` or forced incoming-counter publication in this specialization: `0043B3B9-0043B3C9` directly restores registers and returns. The local counter instead changes inside actual mutation paths. Wholly old/future-gap packets commonly clamp to skip-all and preserve L. A zero-count update leaves L unchanged regardless of C. Modulo arithmetic can make extreme apparently old relations execute a suffix; unsigned behavior is reproduced rather than reinterpreted as serial-number ordering.

## Mutation rules and evidence

| Command | Recovered state effect | Local counter effect | Evidence |
|---|---|---|---|
| remove 0 | Remove if unsigned U16 index is below size; otherwise ignore | +1 only for valid remove | `0043B2AB-0043B2E8`, size getter `0043B5C0`, erase `00446A60-00446A8C` |
| insert 1 | If size is below index, resize to index with zero fill; insert at index | +1 once, including gap growth | `0043B34A-0043B37F`, resize `00448620`, insert `004468F0` |
| replace 2 | Equal existing value is a no-op; otherwise grow to index+1 with zero fill and replace | +1 only on changed/new value | equal branch `0043B3FB-0043B404`, counter `0043B41D`, grow `0043B430-0043B43D` |
| assign 3 | Assign complete counted sequence, including equal or empty sequences | +(k+1), modulo U32 | decoder `0043B474-0043B579`, helper `0043AD50`, increments `0043ADA9` and `0043AEEE` |
| clear 4 | Clear only when nonempty; empty is a no-op | +1 only when nonempty | helper entry branch `0043A7FF` and increment `0043A83D` |

Resize receives an explicit zero scalar from `0043B367` or `0043B436`. Helper `00448620-0044882C` grows/fills using that supplied value. Insert shifts existing elements and erase shifts the suffix; bounded copying helper `0131D240-0131D574` selects forward/backward overlap handling (`0131D254-0131D25A`, forward `rep movsd` at `0131D273`). Counter operations are fixed 32-bit additions/increments; S32 values preserve bits and duplicates. Pointer differences use arithmetic shift only to compute four-byte element counts; wire indices are zero-extended U16 and their comparisons use unsigned branches.

Assign adds one counter step before assignment and one per assigned element after it, even for an identical vector. Empty assign advances once; empty clear does not. A later field reconciles against the resulting actual mutation counter, which can differ from the previous incoming C because ignored/equal operations do not advance it.

## Bounds and deliberate modern changes

LegacyBounded reproduces these arithmetic, count and mutation branches while retaining modern payload/field/command/value/element budgets, immutable ownership, object-ID matching and whole-plan atomic application. Gap growth is checked before adding zeros, including maximum U16 index requiring 65,536 elements. A later bounds failure leaves the input untouched. The original mutates in place and can fire callbacks before a later archive failure; that unsafe behavior is not copied.

The command budget bounds the sum of raw loop bounds; decoded-value budget independently bounds assignments, including final cursor overshoot. Unknown commands reject, as do unsupported mixed fields. The original active switch consumes unknown commands as a byte-only no-op (`0043B277-0043B27A`); its stale path instead consumes a U16 and four bytes for an unknown nonclear/nonremove/nonassign command (`0043B210-0043B297`). This discrepancy is documented rather than exposed as a permissive modern codec.

`0118AA00-0118AA5A` is the common caller: it ignores the declared field count, consumes fields until payload exhaustion, and dispatches the field delta virtual at `0118AA4E`. `00439910` routes checked reads through `0118B030`; the end check at `0118B065` reaches the archive exception path. There is no version branch in the integer-vector decoder. The decoded snapshot initializes the local counter through the same +0x18 member; baseline `0043B050-0043B194` reads count/counter then elements. Existing registration evidence maps this specialization to CREO:3 index17.

Callback adapters `0044C6E0`, `0044C700`, `0044C730`, `0044C760` test nullable callback slots and dispatch member callbacks; no direct counter mutation occurs in those adapters. Downstream callback behavior, registration-specific notification effects, allocation failure recovery and outer exception handling remain outside this milestone. This is a recovered vector kernel, not proof of every original runtime effect.

## Confidence and verification

Observation: exact hash-gated instruction bytes establish unsigned arithmetic, branches, cursor increments, counter writes, helper arguments and return path. Interpretation: the structured legacy policy models that kernel with safe transaction and allocation bounds. Confidence: Confirmed for these decoder/helper branches; downstream callback effects remain Unknown. Implementation consequence: preserve raw operation bounds separately from parsed command count and select the grammar before parsing assignments.

The independent Python oracle verifies 29 exact instruction anchors and the five-entry jump table, models register arithmetic directly, hand-packs fixtures, and compares native encoded bytes/states for 73 cases. These include old/overlap/future/wrap/zero counters, high-bit unsigned discriminators, equal replace/invalid remove, assign+following-command/end/overshoot, skip-threshold crossing, growth and atomic failure. Native scenarios also cover maximum U16 assignment/index, repeated fields, truncation, budgets and ownership. Unchanged Round9 native fixtures and independent oracle verify the default policy.

Next probe: integrate a chosen policy into stable-incarnation wounds/scalar storage with one atomic lifecycle transaction, then investigate concrete wound notification callbacks if presentation requires them. Original live interoperability is not claimed. Lead owns reconciliation of older shared vector prose that incorrectly generalized final counter advancement or described N as command records.
