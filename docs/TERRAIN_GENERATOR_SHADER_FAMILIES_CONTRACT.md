# Terrain generator shader-family raw contract

Round61, 2026-10-05 Eastern. TerrainGeneratorShaderFamilies (external historical reference) owns the observed SGRP0006/SFAM byte structure. It adds a separate decoder to the intentionally opaque shader group of [TerrainGeneratorInput](TERRAIN_GENERATOR_INPUT_CONTRACT.md). It does not resolve names, construct materials/shaders, select duplicates, evaluate blends or infer GPU behavior.

## Executable and installed-byte evidence

SHA-gated x86 baseline `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`: dispatcher `0x00B69C90` attempts optional SGRP at `0x00B69CAA`; absent group branches to return at `0x00B69CB1`. It calls `0x00B698F0` before that attempt; this callee's effects were not expanded. Version dispatch bounds the normalized tag at6 and uses a seven-entry table at `0x00B69D78`. Its0006 entry reaches `0x00B69D61`, calling selected body `0x00B6AAF0` at `0x00B69D64`. Bodies0000..0005 were not investigated.

The selected body enters0006, obtains immediate record count, and loops over required SFAM **chunks**, reading the family layout inline. There is no additional nested family version or separate deserializer in this path. Constructor `0x00B69180` stores the first raw word at object+0. Entry append `0x00B694C0` copies the temporary NUL string and appends the carried family word/string/raw-word triple; no uniqueness check occurs in the inspected append path. The group appends each family pointer in serialized order at `0x00B6AD33`. This proves ordered insertion here, not later duplicate lookup behavior.

Exactly one already pinned effective Tatooine generator is used: TGEN SHA `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`, SGRP at TGEN offset24,1977 encoded bytes, SHA `C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`. Its0006 version contains15 SFAM records and35 entries. Independent byte decoding and native Decode/Bind agree exactly. Original names/words/encoded content remain private; no corpus survey or original execution occurred.

## Confirmed layout; unknown field meaning

```text
FORM SGRP                    BE size, no alignment padding
  FORM 0006                  exactly one version in this modern profile
    SFAM chunk
      raw32 family word      LE; API label FamilyKeyBits
      first NUL byte string
      second NUL byte string
      three individual bytes
      raw32 word0            LE
      raw32 word1            LE
      raw32 entry count      LE; client signed loop gate
      entry count times:
        NUL byte string
        raw32 entry word     LE
    ...
```

The minimum SFAM payload is21 bytes. After the first word and two strings, the fixed suffix is15 bytes:3 individual bytes plus two words and a count. Each entry needs at least5 bytes. No padding is inserted between strings, words, entries or chunks; odd chunk extents are present in the pinned bytes and chunk exit advances by declared size+8.

**Observation / Confirmed:** selected-body read-width sites are4,1,1,1,4,4,4,4; the last word site is inside the entry loop. There are three static string-call sites, with the third repeated per entry. **Unknown:** meanings of both family strings, the three bytes, both raw words, entry string/word and later key/duplicate lookup. `FamilyKeyBits` is a structural label for the first word, not a claim about signed IDs or lookup precedence. Raw words including nonfinite-looking patterns are accepted without float conversion or normalization. Strings preserve arbitrary bytes without text transcoding/path normalization.

**Legacy difference / Confirmed:** `0x00A951B0` reads until NUL and does not inspect its second stack argument, despite the caller passing1000. It does not enforce the modern string bound. `0x00A94DA0` advances the parent position by chunk size+8 without comparing consumed length. The client signed count gate skips entries for count<=0. The new decoder deliberately rejects negative counts, unterminated/overlong strings and trailing SFAM payload. Those are modern strict-profile rejections, not recovered original rejection behavior. Empty groups and zero-entry families are structurally supported. Missing SGRP is a legacy dispatcher branch, not an accepted standalone Decode input.

## API, offsets and ownership

`Decode(encodedSgrp, limits, token)` accepts only exact FORM SGRP/FORM0006 framing, immediate SFAM chunks and exact payload consumption. Other group versions, extra version forms, nested FORM SFAM, wrong tags and trailing bytes reject. Versions0000..0005 remain outside this typed decoder even though the dispatcher recognizes their tags.

`Bind(actualGenerator, limits, token)` retains the actual immutable generator and its actual first Shader group, checks section role/tag/version/extent/record count, then exports that group's bytes through the frozen section API. It does not accept a caller source hash or substituted group. Full string/entry inspection is necessarily **after one charged group export**, because the frozen section offers a copying export rather than a borrowed span. Known encoded/count/minimum copy/array/work limits precede that export; all complete grammar/resource checks precede the decoder's retained snapshot and record arrays. Bind is a documented two-stage preflight, not a claim that all rejection precedes the first export.

All family/entry offsets are relative to the SGRP beginning. Family extents include the8-byte SFAM header; entry extents include the NUL and raw word, without a chunk header. Bound `GeneratorOffset` supplies the origin for TGEN-relative positions. Full source SHA/version, ordered family/entry indices, raw count/words and exact byte exports are preserved. Families and entries share one inaccessible owned encoded snapshot. Read-only views prevent replacement; all copying exports are detached. Duplicate keys and duplicate strings remain distinct ordered records. No dictionary/resolver is built.

## Bounded accounting and publication

Limits default/hard: encoded4/8MiB, families4096/65536, entries per family1024/65536, total entries65536/262144, individual string999/999 bytes, total strings1/8MiB, copy8/16MiB, arrays8/32MiB, work16/64Mi units. All options are nonnegative; resource limits may constrain a shape below its individual count cap. Individual strings exclude the terminator from their bound; aggregate string bytes include every NUL.

Let E be encoded bytes, F family records, C total entries, K=1 for Decode or2 for Bind:

| Logical charge | Formula |
|---|---:|
| Encoded copies |KE|
| Array/record allowance |KE+256+128F+64C|
| Conservative work |(K+4)E+64F+32C|

ArrayBytes includes the retained encoded byte array and, for Bind, the temporary exported section array as an aggregate route allowance. Strings/triple bytes are views of the retained snapshot and are not secretly cloned. Copy budget is separate from the array budget. Fixed/per-record allowances and work are deterministic modern policy charges, not measured managed heap sizes/timings or original allocation sizes. Borrowed generator storage and future caller exports are excluded.

Unsigned sizes are checked against remaining extents before narrowing, count arithmetic is checked, and minimum entry extents precede loops/record arrays. First span-only preflight computes complete counts; the owned snapshot is revalidated before records are constructed. Cancellation is checked during scans/entry loops, before snapshots, after records, and **after result construction/SHA hashing before return**. HashData itself is synchronous/uninterruptible. No partially built result escapes; an earlier completed result is preserved after failed/canceled calls. Mid-copy concurrent mutation and cancellation timed inside hashing were not executed tests.

Native scenarios (external historical reference), independent byte verifier (external historical reference) and offline replay (external historical reference) cover synthetic truncations/counts/string/raw-bit/duplicates/framing/caps/cancellation, detached exports/read-only/repeat/parallel ownership, the one pinned original group,50 static anchors and four wrong-identity gates. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-05_R61_SHADER_FAMILIES.md) records final counts, six-lead budget, elapsed phases and scope. A next bounded string-setter/consumer trace is required before mapping these bytes to a resolver or material behavior.

## Round62 first-string consumer evidence

The [selected setter/getter/consumer trace](TERRAIN_SHADER_FAMILY_CONSUMER_CONTRACT.md) establishes copy ownership and forwarding into one zero-result predicate. The other argument type and comparison/resource/material semantics remain Unknown. Raw fields and structural decoder acceptance are unchanged; no resolver is implemented from this predicate.
