# Data Storage POI container provider and reorder boundary

Date: 2026-09-24. Scope A09/W10+W05. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Function names are provisional and decompilation is reconstructed pseudocode.

This is a **sub-boundary refinement** of the already documented [Data Storage UI](ANALYSIS_CHECKPOINT_2026-09-22_DATA_STORAGE_UI.md), not a newly discovered screen or a new A09 breadth slice. `0x00D414C0` already has the stable name `swg_cui_data_storage_construct_VA_00D414C0`.

## Confirmed by code

`SwgCuiContainerProviderPoi` RTTI identifies primary vtable `0x015F454C`. Constructor `0x00E9F870` initializes a base container-provider, an owned vector-like collection of eight-byte object-reference records, and a second vector-like state field. Its direct constructor caller is `SwgCuiDataStorage` constructor `0x00D414C0`, which stores this provider at offset `+0xE0` (`param_1[0x38]`). The same Data Storage constructor binds the UI `containerPoi` page, creates its container mediator via `0x00E7EFB0(..., 5)`, then connects that provider to the mediator through `0x00E80F20`. Adjacent Data Storage pages include waypoints, data and draft; the POI provider is a local child of this screen, not a top-level network listener.

The provider's virtual `0x00E9FF80` accepts an index vector and checks that its size matches the provider's current eight-byte reference-record count. On mismatch it emits `bad vector sizes` and returns. Otherwise it bounds-checks each supplied index, constructs a reordered reference collection, replaces the prior collection, then invokes `0x00E9DCB0(1)` (provider-change notification) and `0x0058FDC0`. The destination records' precise C++ ownership semantics remain unknown, but their reference construction/destruction is evident in the copy/replacement loops. Destructor `0x00E9F930` releases the vector records and base provider. No direct network send or packet parser occurs in these inspected methods.

This establishes an original UI behavior contract: the POI view is backed by a separately ordered local list of existing client object references. It does **not** establish how POI objects are initially populated, their persistent source, or whether reordering persists across login. Those remain open and should not be guessed from the RTTI name.

## Confidence and evidence

- **Confirmed:** RTTI/vtable, Data Storage constructor call and page/provider wiring, count/range checks, reorder/notification path, no direct send in these methods.
- **Strongly inferred:** `0x00E9FF80` is a presentation-order update, given it rearranges extant references and emits the provider notification.
- **Unknown:** POI data origin, user action triggering the permutation, persistence/replication policy and object ownership detail.

Evidence: read-only PE vtable and direct-call reference scans; Ghidra decompilation of `0x00D414C0`, `0x00E9F870`, `0x00E9F930`, `0x00E9FA30`, and `0x00E9FF80`. Transient decompilation is ignored under `local-private/scratch/poi_provider/`.
