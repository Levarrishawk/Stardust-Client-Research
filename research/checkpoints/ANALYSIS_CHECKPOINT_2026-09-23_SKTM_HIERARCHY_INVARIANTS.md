# Analysis checkpoint — effective SKTM hierarchy invariants

This follows the exact SKTM/0002 disk-layout block and audits semantic invariants across all 739 effective standalone/inline skeleton records. The raw assets were read through configured TRE overlay priority; no files were changed.

- The corpus has 14,138 total transforms. Every nonempty skeleton has exactly one `PRNT = -1` root; two zero-transform hand skeletons have none.
- Every non-root parent index is in range and **less than the child's index**. No cycle or forward parent reference appears in deployed content. This means one forward pass is sufficient to build hierarchical matrices for this corpus, although a general importer should still detect invalid future graphs.
- All 14,138 entries in each of `RPRE`, `RPST` and `BPRO` have finite components and squared norm within `0.0001` of 1.0. This strongly validates their quaternion role. Their frequent `(1,0,0,0)` identity record is consistent with scalar-first storage; exact multiplication/coordinate convention is not yet proven by this audit.
- All 14,138 `JROR` U32 values are zero in effective content. The parser must retain the field for fidelity, but a first importer of this exact installed corpus will not encounter a nonzero case. Its historical enum role remains unknown.

These are **confirmed effective-asset facts**; they do not prove every future SWG skeleton follows the same restrictions. A replacement should parse fixed-width disk values into x64-native arrays, validate parent ordering and unit norms, and keep `JROR` rather than dropping it. Binding the rotations/translations into final pose matrices still needs code-side evaluation and known-pose fixtures; no multiplication rule is invented here.

Evidence: ignored `local-private/session-2026-09-23-marathon/audit_sktm_corpus.py`, with all 739 records passing the exact loader-derived size checks.
