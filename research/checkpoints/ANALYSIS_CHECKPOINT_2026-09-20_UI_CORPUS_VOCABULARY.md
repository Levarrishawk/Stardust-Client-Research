# Effective UI corpus vocabulary and include closure

Date: 2026-09-20

## Scope

This block inventories the effective `.ui`/`.inc` records selected by `swgemu_live.cfg` archive precedence. It is a resource-corpus result, not a claim that every mediator behavior has been reconstructed.

The reproducible output is `ui_corpus_inventory.md`; the read-only scanner is `ui_corpus_inventory.py`.

## Confirmed corpus

- 251 effective UI records (`.ui` and `.inc`).
- 65 case-normalized element spellings seen by the tolerant scanner.
- 130 distinct include targets.
- All 130 include targets resolve in the effective configured archive overlay.
- Zero payload decompression/decoding failures.
- Only 15 tolerant-parser residues, all attributable to malformed/unescaped legacy content or unusual list syntax; they are retained as test fixtures rather than silently discarded.

The element count is dominated by font glyph records, image styles and pages. The practical widget/style vocabulary includes:

- hierarchy/data: `Page`, `Namespace`, `Data`, `DataSource`, `DataSourceContainer`, `Composite`, `Template`;
- ordinary controls: `Text`, `Image`, `Button`, `Checkbox`, `Scrollbar`, `Sliderbar`, `Textbox`, `ComboBox`, `List`, `Table`, `TreeView`, `TabbedPane`, `PopupMenu`, `RadialMenu`, `Progressbar`;
- shapes/layout: `Ellipse`, `Pie`, `VolumePage`, `Runner`;
- style resources: image, rectangle, button, text, textbox, slider, checkbox, list, tree, tab, popup, radial, tooltip, cursor and grid styles;
- animation/effects: opacity, color, rotation, location and size effectors plus rotate/wave deformers;
- SWG-specific controls: `CuiWidget3dObjectListViewer`, `CuiWidget3dObjectViewer`, `CuiWidget3dPaperdoll`, and `CuiWidgetGroundRadar`.

The two apparent `Imperial` elements are malformed text containing an unescaped `<Imperial Butter Makers...` phrase, not a real widget type.

## Event/expression surface

The effective corpus contains 20 event-like property spellings. The heavily used core is `OnShow`, `OnHide`, `OnEnable`, `OnDisable`, their effector variants, `OnPress`, `OnSet`, `OnUnset`, `OnActivate`, `OnDeactivate`, hover events, `OnRunScript`, `OnSliderChange`, and `OnSelectionChanged`.

Attributes containing dotted paths (for example `button.visible`, `tabs.activetab`, or nested color components) show that the format is not merely XML property initialization. It also embeds path-targeted assignments and event expressions. A generic XML-to-Godot scene converter would lose behavior unless it preserves this expression/path layer.

`Data` and `Namespace` intentionally accept open-ended game-specific keys. Across the whole corpus there are 3,661 attribute spellings, but that number must not be interpreted as 3,661 engine properties. Excluding `Data`/`Namespace` still leaves 1,016 structural/path spellings, many of which are dotted target assignments or style aliases rather than primitive widget fields.

## Compatibility strategy

The original assets can be consumed without cloning the original renderer if the UI adapter has four separable layers:

1. tolerant legacy text parser preserving source spelling, order and malformed-but-used escapes;
2. include resolver over the TRE overlay with cycle detection and diagnostic provenance;
3. engine-neutral widget/style/effect AST with named-path lookup and open-ended data properties;
4. Godot adapter implementing an initially small widget core and explicit custom controls.

Do not require complete visual support before accepting a file. Unknown elements/properties should remain in the AST, produce structured diagnostics, and be round-trippable. This permits incremental fidelity while retaining the original asset as the authority.

## Minimal UI compatibility slice

After the networking-only proof of concept, the first UI asset proof should parse `ui/ui_root.ui`, resolve all includes without instantiating every screen, and instantiate one login/selection include using:

- `Page`, `Namespace`, `Data`, `DataSource`;
- `Text`, `Image`, `Button`, `Textbox`;
- image/text/button styles and cursor references;
- named path lookup;
- `OnPress`, visibility/enabled changes, and show/hide effectors.

3D viewers, radar, paperdoll, tables/trees, deformers and the remaining style vocabulary can be explicit later milestones, but their nodes and properties must be preserved from day one.

## Remaining bounded unknowns

- Exact runtime error/fallback policy for malformed properties, missing style targets and include cycles.
- Full expression grammar and ordering when multiple dotted assignments/effectors fire.
- Mediator activation/deactivation and lifetime contracts for each screen.
- Rendering fidelity rules for the four SWG-specific widget classes.

## Confidence

- Record/tag/include/event counts: **Confirmed by read-only extraction of the effective configured TRE overlay**.
- Four-layer replacement strategy: **Architectural recommendation**.
- Meaning of every open-ended property: **Unknown and not inferred from spelling alone**.

