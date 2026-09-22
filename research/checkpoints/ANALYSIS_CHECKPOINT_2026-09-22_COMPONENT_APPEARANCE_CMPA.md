# Component Appearance CMPA Closure

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

`.cmp`/`CMPA` is a transform hierarchy for composing multiple appearance resources into one runtime appearance. It is not a mesh format and should map naturally to an engine scene/resource composition layer, provided the original child transforms, bounds and lifecycle fan-out are retained.

## File contract

`component_appearance_template_load_VA_00798E40` accepts versions `0001` through `0005` and rejects versions outside that range. All deployed effective records are versions `0003`, `0004` or `0005`.

The modern common layout is:

```text
FORM CMPA
  FORM 0003|0004|0005
    FORM APPR ...
    [FORM RADR / INFO U32]
    PART string + 12 little-endian F32
    ...
```

Each `PART` names another appearance resource and supplies its local 3x4 transform. Executable loaders store each resolved appearance-template pointer plus the 48-byte transform in a 0x34-byte record. Versions 0001/0002 use older part/base paths; they are supported by the executable but absent from the effective corpus.

Version 0004 additionally derives/caches a box extent from the base APPR extent when compatible. Version 0005 reads `RADR/INFO` and conditionally creates an auxiliary 16-byte runtime object; the exact historical RADR label remains unknown and is preserved as a raw policy flag rather than guessed.

## Effective corpus

`tools/python/cmpa_corpus_inventory.py` applies configured overlay priority and parses every effective component appearance:

| Measure | Result |
|---|---:|
| live effective CMPA | 165 |
| `0003` / `0004` / `0005` | 13 / 6 / 146 |
| total parts | 779 |
| parse failures | 0 |
| raw archive-copy count previously reported | 253 |

Child references are 640 `.lod`, 87 `.prt`, 26 `.msh`, 16 `.spr`, and 10 nested `.cmp`. Deployed part counts range from 1 to 52. Nested CMP therefore requires cycle-safe shared-resource resolution even though the observed resources are finite.

After applying the appearance-relative namespace rule (`lod/...` resolves as `appearance/lod/...`), all but eight child references resolve to effective live overlay records and the ten nested CMP edges contain **zero cycles**. The eight misses are legacy laser-bolt/bluebolt `.msh` children; those CMPA records also carry sprite children, and the template/runtime path tolerates a failed child fetch. A replacement must therefore skip an unresolved part locally rather than reject the entire component, while logging the missing dependency.

## Runtime contract

`component_appearance_construct_VA_007A0320` creates one child object/appearance per part, assigns the resolved child appearance template, stores children in an owned collection and builds a parent render/visibility object. It attaches each child's visibility object beneath the component parent.

The component runtime fans readiness, collision/query, alter and presentation operations over its children. Before child presentation/update it composes the component/world transform with that part's authored local transform. Destruction releases the parent visibility object and owned child collection. This is a composition owner, not a passive filename list.

## Replacement contract

- Decode fixed-width little-endian 3x4 transforms inside the big-endian IFF envelope.
- Resolve each part through the common appearance factory, including nested CMPA.
- Instantiate child nodes under one component root and compose transforms in the same parent/local order.
- Aggregate readiness, bounds, collision and render visibility across children.
- Preserve base APPR extent behavior and retain the v0005 RADR flag until its narrower semantics are proven.
- Detect recursive resource cycles and share immutable decoded templates; do not share mutable scene instances.
- Treat unresolved child references as part-local failures; the deployed overlay contains eight such legacy mesh references.
- Do not retain original x86 object sizes, container layouts or D3D/DPVS implementation classes.

## Confidence

- Version dispatch, PART schema, transform width/order and corpus distributions: **Confirmed**.
- Child instantiation, ownership, transform composition and visibility hierarchy: **Confirmed**.
- Exact v0005 RADR semantic label: **Unknown**, bounded to a conditional auxiliary runtime policy object.
