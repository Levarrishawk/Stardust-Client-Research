# Owned PGRF reader — 2026-09-30

## Question and stopping condition

What are the deployed graph fields and range invariants, and can the native owned decoder agree on every deployed floor/layout graph? Closed with direct client reads, independent byte hashes and malformed fixtures.

## Evidence

Baseline SHA `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed before this session's static boundary work; config SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 mounts. Inputs are the pinned effective floor/portal inventories. New member-manifest SHA `9912418C2A43057F9C2D3A2185580BE2AFD650986733221F375472FF1E03DB10`.

Direct client deployed reader **012E2120**, node callback **012E3B70**, edge callback **012E39A0**, array helpers **012E2730/012E2880/012E29F0** establish field order. 012E21D0 calls IFF write operations and must not be mistaken for the reader. Server sources provide semantic leads only. Exact field order and policy are in the [durable decoder contract](../../docs/PATH_GRAPH_DECODER_CONTRACT.md).

## Findings and confidence

**Confirmed corpus structure:** 3,914 PGRF0001 graphs (3,626 floors and 288 layouts), 39,647 nodes, 86,964 directed edges. Every graph raw type is 3. Raw node types: 0 ×15,057; 1 ×17,332; 3 ×584; 4 ×3,453; 5 ×3,221. All authored IDs equal their array ordinals, but the model preserves them separately and tests nonsequential IDs. ECNT/ESTR counts equal node count; ranges cover every edge exactly once and agree with its source ordinal. **1,471 zero-count entries use start -1**. Radius and lane widths have observed minimum zero. The server's radius substitution is not confirmed client behavior and is not applied.

**Strong interpretation:** the third node field is a global association key and the final edge floats are lane widths. Type labels/right-left semantics remain correlation. Raw serialized position is consumed as a client Vector; no server variable-name axis swap is inferred.

## Validation and next probe

`verify_path_graph.py` independently reconstructs raw metadata/node/edge/range hashes and compares a one-process native pinned-manifest probe: all 3,914 agree. Owned synthetic asymmetric fields, directed edges, zero/-1 ranges, every truncated prefix and malformed endpoints/counts/ranges/nonfinite/negative widths pass. Private outputs remain ignored. No coverage-score or registry count change.

Next: explicitly modern deterministic directed route inspection, then recover cross-cell association rules against paired portal evidence before connecting floor graphs. Original live navigation and permission remain separate.
