# Terrain fractal family lookup and ownership contract

Round46, 2026-10-04. This bounded static investigation covers the family map used by AHFR and FFRA. It does not establish a complete editing API, thread safety, or replacement noise generator. Evidence and reproduction are in [the checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R46_FRACTAL_FAMILY_LOOKUP.md); the consumer boundaries remain in [AHFR](TERRAIN_HEIGHT_FRACTAL_CONTRACT.md) and [FFRA](TERRAIN_FILTER_FRACTAL_CONTRACT.md).

All addresses below are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Confirmed means directly observed instructions or selected asset structure; interpretation and incomplete closure are stated separately.

## Entry and index structure

| Object | Observed fields | Evidence / confidence |
| --- | --- | --- |
| Allocated entry, 0x2588 bytes | +0 owned name pointer; +4 signed 32-bit family ID; +8 inline sampler | Allocation 00B75BCE; constructor 00B75680 writes name/id and constructs sampler at 00B75695. Confirmed. |
| Map owner | +0 tree-wrapper pointer | Constructor 00B757C0. Confirmed. |
| Tree wrapper, 12 bytes | +0 head; +4 count; +8 allocator/control byte, semantics unknown | Allocations 00B757DE and 00B757FA. Confirmed structure. |
| Node, 24 bytes | +0 control byte; +4 parent; +8 left; +C right; +10 signed key; +14 entry pointer | Insert/copy 00B76460/00B76490 and lookup 00B76420. Confirmed structure; full balancing semantics deferred. |
| Head | +4 root (initially zero); +8 minimum and +C maximum (initially self) | 00B75814, 00B75819, 00B75821. Confirmed. |

The sampler extent is strongly inferred as 0x2580 from the entry allocation and final sampler cache field at +257C. The map owns allocated entries. Names are copied into separately allocated storage by 00B75720, which releases the old name. Entry destruction 00B756B0 releases that name and invokes sampler destruction 013E6930 at entry+8; the latter releases its numerical cache at sampler+257C.

## Exact lookup and borrowed consumers

00B75DE0 calls signed lower-bound search 00B76420 and returns the entry pointer stored at node+14, or zero for the head/missing result. Signed comparisons include 00B76435 and 00B7644E. Mutable lookup 00B75E10 follows the same exact-key rule. Getter 00B757A0 computes entry+8 with LEA, without a null check. Consequently wrapper 00B75B70 produces address `00000008` when lookup returns zero. 00B75B90/00B757B0 have the same unchecked boundary. This is confirmed arithmetic, not an assertion that a malformed family is safely handled downstream.

AHFR stores a borrowed sampler pointer at +14, cached key +18 and desired key +1C. FFRA uses +1C, +20 and +24 respectively. On a key mismatch, each writes the new cached key before calling 00B75B70 and storing its result (AHFR 00B768C2/CC/D1; FFRA 00B77BEE/F8/FD). On a match, neither checks map identity or generation. Constructors initialize cached keys to FFFFFFFF and pointers to zero. A fresh desired key FFFFFFFF therefore preserves null without lookup; this sentinel collision differs from missing lookup producing address 8. Whether such an ID is legal in original content remains unknown. AHFR's nonpositive finite influence exits before lookup, as established by its separate contract.

**Compatibility consequence:** the pointer is borrowed from map-owned storage, not an independently retained sampler. A stable numeric key alone cannot establish lifetime. A replacement may use safe handles internally, but must settle lookup failure and invalidation at the compatibility boundary before claiming equivalent reload/edit behavior.

## Creation, editing, removal and destruction

Insertion 00B75BB0 allocates and constructs an entry, copies its name, inserts a key/pointer pair, rebalances and increments the count. The observed equal-key branch returns at 00B75CF6 without overwriting the old entry or destroying the newly allocated entry in that body. Duplicate ownership/failure behavior is not closed; this is not evidence of an automatic replacement operation.

Name edit 00B75B50 changes owned name storage and preserves the entry address. ID edit 00B75F20 invokes 00B75790 to change entry+4, without changing node+10 or reindexing in the observed body. Key erase 00B75D10 erases/frees a node and decrements count but does not destroy/free its entry there. No direct relative caller of these three methods, or 00B75B90, appeared in the bounded scan; indirect reachability, caller ownership and missing-key behavior remain unknown.

Clear 00B75A50 iterates entry pointers, invokes entry destruction at 00B75A69, releases entry storage at 00B75A6F, frees nodes, and resets head links/count. Map destruction 00B75850 first clears the entries, then destroys and frees the tree wrapper. These are confirmed destructive lifetime boundaries.

Sampler assignment 013E6A00/013E6A20 preserves the inline object address, copies parameters, resets numerical-cache pointer/dimensions and calls resize 013E6AD0. Full reused-destination allocation/failure policy remains open. Sampled setters 013E6B80, 013E6BB0, 013E6BD0, 013E6BF0 and 013E6C10 call cache zeroing 013E6B50. Resize releases/reallocates a growing cache and clears it. This is **numerical-cache invalidation**, distinct from invalidating AHFR/FFRA borrowed pointers. No consumer notification is observed in these examined map/clear/edit bodies; global observer absence is not proved.

## Load, save and concrete occurrence

Load 00B75920 clears the map first at 00B75927, accepts MGRP/0000 and dispatches 00B75F40. Each MFAM DATA contains a four-byte key followed by a NUL-terminated name; the loader inserts the entry, releases the temporary name, gets its inline sampler and calls MFRC loader 013E8410. Save 00B75970 iterates nodes in ascending index order and writes entry ID/name followed by MFRC through 013E82B0. The distinction between node key and editable entry ID matters to serialization.

Legacy conversion 00B76040 compares existing samplers through 013E80A0; a match returns its ID. Otherwise it selects an unused positive ID starting at 1, generates a unique name, inserts an entry and assigns its inline sampler. This establishes bounded conversion behavior, not a complete replacement API.

One original effective-overlay occurrence was selected: priority-42 terrain/tatooine.trn, TGEN MGRP at offset 6602, version 0000, first MFAM key 1 at offset 6626, length 132, SHA-256 `90CE52CED775F08BF61AE50A702C61576BB53E3982A46A699D90C94C94192DF8`. DATA is 28 bytes: four-byte key plus 23 name bytes and terminator. Its second child is FORM MFRC, retained opaque. No original name, noise parameters or generated noise values are promoted. This confirms one occurrence only.

## Owner order and remaining question

Generator constructor 00B64F70 constructs the map member at +40. Generator clear 00B650C0 calls map clear at 00B650E4 **before** deleting layer objects at 00B6510E. Consumer destructors clear their pointer without dereferencing it, but the examined bodies do not establish exclusion of queries while entries have been freed. Generator destructor invokes clear at 00B65027 and later map destruction at 00B65077.

TGEN loader 00B65CC0 invokes map load at 00B65D17 before new-layer construction/loading at 00B65D77 and append at 00B65D7D. The observed loader body does not first invoke generator clear. Fresh construction/load and reload into an existing generator must therefore remain separate claims.

The verifier confirms 28 direct relative CALL/JMP candidates to 13 selected map entries by decoding from each enclosing function entry. This is bounded direct-call coverage, not whole-program or indirect observer coverage.

**Next probe:** prove caller quiescence, layer rebuilding/recreation, or an external invalidation observer around map-clear 00B650E4 to layer-delete 00B6510E, and map-load clear 00B75927 to new-layer construction 00B65D77. Until then concurrent queries, reused-object reload and cached same-key pointer validity are Unknown. No production map or noise implementation is justified by this result.

## Round47 compatibility increment

The selected MFRC/0001 child now has an independently authored [13-word schema and conditional noise element](TERRAIN_FRACTAL_SEED_NOISE_CONTRACT.md). Raw original words remain in private fixtures; no additional family corpus is surveyed. This does not resolve map reload quiescence or same-key borrowed-pointer lifetime.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.
