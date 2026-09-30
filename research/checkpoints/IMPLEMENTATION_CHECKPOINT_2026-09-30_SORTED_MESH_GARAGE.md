# Sorted-index mesh and nested garage checkpoint — 2026-09-30

## Question and stop

Do owned SIDX variants and the two observed coordinate layouts unblock the previously rejected nested garage graph, with independent byte-level agreement? Stop after native grammar/selection negatives, all original geometry and sorted hashes, and neutral Godot inspection.

## Identity and evidence

Baseline `Stardust.exe` SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 configured TREE0005 mounts. Static original reads only.

Root `appearance/component/mun_all_garage_s01_u01.cmp`, priority 30, SHA-256 `1FE0DF11A1A1B6506AA18B1672F87A03F6C892D48C91A72D63E4D3B170D64B12`. Previous [component checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_COMPONENT_GEOMETRY.md) reported three omitted parts for this graph. Direct child mesh records show optional SIDX and declarations 0x5205/0x35305.

Primitive readers VA 00770220/007704E0 and index readers VA 007700A0/00770160 establish version-selected width inside SIDX. Preparation VA 007707D0 computes local displacement dot products and updates the selected entry only on strict less-than; first tie wins. The [existing topology/SIDX investigation](ANALYSIS_CHECKPOINT_2026-09-12_TOPOLOGY_SHADER_ENVELOPE.md) and durable asset spec provided the implementation contract, with targeted static inspection confirming tie behavior.

## Result and confidence

**Confirmed:** the native graph now contains 14 sources, nine mesh instances and zero omitted parts. Independent Python reconstructs all source identities, geometry hashes and 400 sorted-index variants across all nine meshes. Declared auxiliary floats and sorted direction/index values are retained, not flattened into UV0 or ordinary indices.

Two illustrative original comparisons:

- `appearance/mesh/mun_all_garage_s01_u01_l0_c1_l0.msh`: 1,729 vertices/1,071 triangles, 90 sorted variants; geometry `F396315471CE36923764224643277AF2F53A67A4EDB2DD8CDC3A7FF45BD8C89E`, sorted `864E68AB21E5D8A343C86018B99C99698D17D8B989A215A13D3DBE31BD40E924`.
- `appearance/mesh/mun_all_garage_s01_u01_c1_l0.msh`: 1,734 vertices/1,071 triangles, 90 variants; geometry `B3FE426A56F861D7F5D44F3FDF528C720AC984FB9A8F004140BFCF4E75FF3EDF`, sorted `E5F69DC5D05FB919AC7735A57B157F3704E0046900F1322EF90CD0CCC1F28088`.

**Confirmed implementation:** synthetic primitive versions 0000/0001 exercise exact variant order, minimum-dot selection, first ties, ordinary-index fallback, every truncated file prefix, nonfinite directions, out-of-range references and trailing bytes. New coordinate-layout tests preserve two 2D sets and a third 4D set. Existing mesh geometry hashes remain stable because sorted ordering has its own hash.

Godot 4.7.2 .NET x64 builds with zero warnings/errors. Hidden headless and real OpenGL GPU runs pass geometry readback and every LOD midpoint switch. The neutral garage capture was visually inspected. It is not a faithful textured/translucent rendering: SIDX selection is implemented in core but this neutral adapter still draws ordinary INDX. Baseline readiness, culling and original lighting remain separate.

## Compatibility and next action

Canonical details and reproduction: [sorted-index contract](../../docs/STATIC_MESH_SORTED_INDEX_CONTRACT.md). Three-million aggregate index and 4,096 variant limits, zero-variant fallback and double intermediates are explicit modern policies. No research coverage score, provisional address count or protocol registry changes.

Next: connect shared material/texture bundles to component mesh resources with explicit supported/unsupported material diagnostics, or close the APPR/PIVT distance origin before automatic camera-driven selection. Preserve SIDX for a later real material path; do not infer translucent correctness from neutral geometry inspection.
