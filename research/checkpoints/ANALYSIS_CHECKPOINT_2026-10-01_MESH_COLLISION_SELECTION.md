# October 1: static-mesh collision eligibility

## Question and stopping condition

Which runtime static-mesh collision path selects primitives, and does that explain collision geometry in the existing three-cell Mustafar route? Stop at direct runtime dispatch, shader predicates, receiver construction/ownership and original candidate reconciliation. No broader POB survey.

## Inputs and method

Rehashed baseline executable: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Read-only PE32 disassembly/pointer/RTTI probes and read-only, no-analysis Ghidra address decompilation. Bundled Java required approved access to its JARs; writes were limited to ignored cache and evidence. The original executable was not run. Original unchanged winning assets were loaded through configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`,48 descending TRE mounts, using the same three [pinned helper-audit meshes](IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_HELPER_GEOMETRY.md).

## Evidence and findings

The [durable selection specification](../../docs/STATIC_MESH_COLLISION_SELECTION.md) records exact addresses, offsets, mode/selector distinction, selected implementation getters, version handling and receiver lifecycle. Direct instruction evidence confirms detailed queries require a receiver and selected shader eligibility, while mode2 uses only the appearance extent. IMPL/0004 derives flags from the first pass; later versions use DATA byte6 for selector1. Zero-pass helpers can be admitted. Names were used as correlation only.

| Original owner | Primitives / triangles | Selector0 | Selector1 |
|---|---:|---:|---:|
| Room5 | 19 / 20,501 | 18 / 20,453 | 19 / 20,501 |
| Hall6 | 6 / 1,984 | 6 / 1,984 | 6 / 1,984 |
| Room7 | 19 / 20,501 | 18 / 20,453 | 19 / 20,501 |

Native and independent raw IFF parsing agree on164 candidate records,26 pinned sources. Every candidate's predicates agree within its primitive, so GPU selection does not change these sample counts. **Confirmed:** visible hall triangles qualify under both recovered selectors. **Unknown:** which selector original actor movement uses and how its volume solver composes floor, appearance and door collision.

## Implementation and validation

Added engine-neutral `MeshPrimitiveCollisionPolicy`, with explicitly selected implementation input and raw selector semantics. Native truth-table, zero-pass, first-pass-only, older-derived and negative gates pass. The full existing native smoke suite passes. The original three-cell `--portal-materials-defaults` probe and `verify_selected_mesh_collision_predicates.py` pass. Detailed evidence remains ignored; only authored rules and compact aggregates are curated. No canonical address names changed;2,599 names,345 packets and research scores89%/94% remain unchanged.

Reproduce: run the native probe with pinned POB hash `79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818`, cells `5,6,7`, explicit archive root/configuration, and save its output under ignored evidence. Supply that output and the existing selected-appearance source manifest to the Python verifier with the configuration hash above. Rebuild the source manifest with the preceding scene-material verifier if absent; retain all private asset-derived inputs locally.

## Compatibility consequence and next action

Hidden-helper-only obstacles are incomplete for this route. Hall6 is a viable original triangle slice within the current2,048 limit. Rooms5/7 require acceleration or a separately documented spatial extraction policy; silently truncating triangles is unacceptable. Next milestone: integrate triangle contact with transformed one-portal floor paths and supported source/target rollback on synthetic fixtures, then exercise the pinned original hall. Original actor selector remains a precise later static probe.
