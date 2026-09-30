# Portal-layout reader checkpoint — 2026-09-30

## Question and stop

Can deployed PRTO layouts expose owned named cells, portal geometry and adjacency without losing optional bounds, door fields or auxiliary records? Stop after client byte-reader checks, independent full effective corpus comparison, deterministic positive/negative fixtures and a precise scene-composition resume point.

## Inputs and method

Verified executable baseline `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; configuration `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 mounts. Existing PRTO/FLOR corpus reports, portal/floor research and private reference probes were inspected before extending native readers. Sorted current portal member-list SHA `B76BD8546F0BA91DBFC326D2EA333FF92D81D31A84B95B827F649CF81E77368E`.

Static Capstone/pefile traces corroborate version dispatch, exact cell/reference widths, optional extent handling and indexed geometry loader. Current server `PortalLayout::parse` and `CellPortal::readObject` are correlation, not proof of client control flow. The [durable contract](../../docs/PORTAL_LAYOUT_DECODER_CONTRACT.md) records exact addresses and boundaries. Original execution and network contact remain unused.

## Findings and confidence

**Confirmed owned implementation:** PRTO v1/v3 polygon vertices and v4 indexed triangles; CELL v4/v5 names/paths/raw flags; PRTL version chunks v2/v4/v5, raw geometry/target fields, door names and transforms. Transform bytes are present even with a false transform flag. Bounds can be absent in CELL v5: initial corpus attempt requiring them failed on a real layout; the optional client factory confirms the corrected behavior. Bounds, lights and building path graph retain owned metadata without invented semantics.

**Confirmed corpus agreement:** all 291 live layouts/3,470 cells/3,821 geometries/7,642 references agree with an independent reader on source SHA/priority, float bits and indices, names/raw fields/transforms and auxiliary hashes. Every geometry has exactly two sides; every stored target agrees with the opposite zero-based cell ordinal. One POB is a true zero-size mask. Door counts are 2,448 nonempty names/2,288 enabled transform flags. Detailed proprietary output remains private.

**Confirmed correction:** 00B43E80 only toggles the second portal-reference byte, contradicting the older broad attribution of neighbor resolution to that helper. The current PRTL v5 loader reads but does not retain its target index along the observed path. The portable structure preserves raw targets and derives adjacency from geometry references; full original post-load linking and flag semantics remain open. Raw target labels are corroborated by actual paired records and server symbols.

## Validation and compatibility consequences

All eighteen positive version combinations, owned-buffer independence, optional bounds, false-flag retained transforms and topology fixtures pass. Every truncated prefix, extra bytes, count mismatch, invalid geometry/target indices and nonfinite coordinates reject. Inconsistent valid-range targets become explicit diagnostics. Core smoke suite and independent producer/verifier pass; organized workspace/link/registry and diff checks pass. Scores, 2,590-address registry and 345-row packet boundary do not change.

The loader does not create a playable building. Polygon fan triangulation, allocation caps and diagnostic policy are deliberate replacement choices. Floor collision/navigation, door objects, lighting, DPVS and visibility/lazy lifecycle remain separate. Next: load one pinned layout and its cell appearances through a shared bounded geometry collection; build a named-cell inspector that makes omissions explicit.
