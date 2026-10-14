# Distance selector and repeated pixel bindings — 2026-09-29

## Question, evidence and scope

Can authored DTLA bands select the same detail under ordinary distance/previous-index conditions, and can the richer L0/L1 dependency graph be decoded without executing original programs? Input baseline is the [DTLA checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-29_DTLA_READER.md), including the rechecked executable SHA and original parent identity.

Observation: baseline runtime `0x00787710` measures Euclidean camera distance from a PIVT-controlled reference, searches from previous index (negative starts at zero), compares against globally scaled lower/upper bands, extends the upper band under a separate option, and retains an index on inclusive boundary ties. With coarse-to-fine order it moves toward lower indices above upper bounds and higher indices below lower bounds; a direction reversal terminates search. If selected child is unavailable, adjacent already-loaded children are examined at increasing offset, coarser before finer at equal offset. Confidence: Confirmed ordinary control flow from baseline static evidence; no original executable run.

`DetailAppearanceSelector` implements the distance-only boundary with explicit previous index, positive scale, upper extension and availability. It does not compute PIVT origin, load children, apply skipL0, forced-detail controls or reproduce hidden global state. Sorting is stable by descending upper bound; tied original sorting behavior remains unverified. Wider intermediate arithmetic is explicit replacement policy for extreme inputs, not a bit-identical x87 claim. Tests cover inclusive ties, near/far clamping, scale/extension, no available child, neighbor fallback and nonfinite/invalid inputs.

## Richer material observation

`effect/a_2blend_dirt.eft` resolves at priority 40 from `mtg_patch_011_files_01.tre`, 1,317 bytes, SHA-256 `18C56F2C07E88FB8C3B69C60EFB0E8911B6E3184636B2FB7AFCB1C926041AA6E`. Its EFCT/0001 has four IMPL/0005 alternatives. Each of the first two PPSH/0001 records has four distinct FORM PTXM/chunk 0002 records, each five bytes (U8 stage and LE U32 tag). The initial implementation incorrectly required one PTXM form. `StaticEffectDecoder` now requires the declared number of individual records, unique stage IDs below eight, exact record size and supported version. Synthetic repeated/duplicate/out-of-range tests pass.

The two fixed fallback alternatives have two STAG records then one MAIN/MAIN STAG, respectively. Program metadata remains opaque; shader programs are not translated. L0 and L1 each resolve 8 materials, 15 unique textures, 2 effects, 26 source identities. Independent Pillow/native comparison passes all 134 mip levels for each, plus geometry hashes. These are two mesh-bundle comparisons sharing textures, not 268 unique texture mip levels.

## Consequence and next action

Preserve richer bindings for later material translation. An explicit inspector may use the authored opaque one-stage MAIN fallback without asserting original capability selection or dirt-blend fidelity. Next: validate all four selected detail resources in prototype host, then close the appearance redirect boundary and precise reference-origin semantics.
