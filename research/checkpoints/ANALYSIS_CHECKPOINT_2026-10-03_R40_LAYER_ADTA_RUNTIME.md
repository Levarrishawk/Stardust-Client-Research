# Round40: serialized layer inversion flags

Date2026-10-03. Question: which raw LAYR ADTA words feed the two inversion
branches established in the general per-sample layer loop? Stop after exact
loader/setter/constructor mapping and one pinned original raw occurrence.
Byte+68 semantics, byte+52 writers/cache lifecycle and full generation remain
separate probes.

## Identity and tools

Executable SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Read-only PE mapping/Capstone and the reusable
`tools/python/verify_terrain_layer_adta.py`, which gates identity before package
imports or VA decoding. Original executable execution/live contact did not
occur. The original-byte reader reuses frozen33 archive precedence/IFF framing,
pins configuration/source/TGEN and visits one first layer. It does not repeat
the earlier257-layer/rule inventory.

## Observation versus interpretation

[The durable mapping](../../docs/TERRAIN_LAYER_ADTA_RUNTIME_CONTRACT.md)
is canonical. **Confirmed:** all supported versions map the first two raw
words through nonzero normalization to bytes+50/+51, used for boundary and
combined-filter inversions. Versions2/3 map word2 to byte+68. Version4 reads
word2 into a temporary overwritten by word3 before normalization/setter use;
word3 supplies byte+68. Constructor zeros all three mapped bytes and the
separate bounding-path byte+52. A fresh version1 load therefore retains+68
zero; its loader does not explicitly reset an existing instance's+68.

**Unknown:** byte+68 meaning, ignored version4 word2's original purpose,
reloaded-instance equivalence, all+52 writers/culling/cache activation and
complete generator semantics. These are not renamed from server analogy or
memory proximity. Original raw words remain unchanged.

## Evidence and validation

The executed verifier passes52 exact instruction anchors, all2/3/3/4 four-byte
loader read counts, setter/member offsets, version4 overwrite sequence and
the general loop's two inversion uses. The same pinned terrain source is
1,188,446bytes/SHA
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`,
priority42; TGEN is139,608bytes at210/SHA
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
One original LAYR0003 occurrence at TGEN offset12,247 has raw mapped false
flags; exact words and section hash remain in the private receipt. This is
an input occurrence, not original runtime flag/influence capture.

Discovery corrections: an initial contextual disassembly began inside the
preceding function at00B63000; its undecodable prefix was discarded and no
claim relies on it. Final verifier ranges start at verified functions. One
PowerShell wildcard rg search returned OS error123; an exact file replaced
it. A guessed prior receipt filename and guessed earlier helper filename were
absent; discovery found the canonical tools and the final occurrence was read
fresh from the pinned archive instead. No static assertion failed.

Round40 final link/scope/whitespace gates belong to its acceptance report;
this completed static probe does not accept the other four milestones.

## Compatibility and next action

Supported fresh layer inputs can supply the two proved inversion booleans
while retaining raw words and neutral+68 provenance. Version4 must not treat
word2 as the same flag used by versions2/3. Fresh version1 defaults cannot be
silently generalized to object reuse.

Next: trace byte+68 reads/callers and independently trace+52 writers/cache
activation before implementing a complete runtime layer descriptor/traversal.
