# Legacy UI Style, Font, and Atlas Pipeline

The effective UI presentation-resource layer is now inventoried as 44,674
style/font rows with source provenance (`ui_style_rows.tsv`). Original UI
compatibility requires atlas-backed declarative styles, not only a widget tree.

## Effective records

- 33,695 `FontCharacter` rows
- 10,669 `ImageStyle` rows
- 200 `ImageFrame` rows
- 78 `Palette` rows
- 32 `TextStyle` rows

Text styles cover Aurabesh, Verdana/verdana-bold, Lucida Console, StarWars logo
and Japanese SWG DFGothic profiles at explicit sizes. Glyphs supply Unicode/code
value, name, integer advance, atlas source, and pixel rectangle. There are 110
distinct font-atlas sources; Japanese profiles span many numbered pages.

Image styles provide a named slice, source identity, and pixel `SourceRect`.
There are 191 distinct source identities. Sources include ordinary atlas names
and qualified identities such as `shader_overlay:ui_rebel_final` and
`ui_grayscale:<atlas>`, so the source is a resolver expression, not always a
direct DDS path.

`ImageFrame` and direction names (`n/ne/e/se/s/sw/w/nw/c`) establish frame or
nine-slice composition. Palette rows are open-ended named color maps with
multiple themes and legacy misspellings.

## Replacement architecture

```text
TRE overlay -> UI parser -> style/palette resolver
 -> image source resolver -> atlas slices/nine-slice + glyph metrics/pages
 -> engine-neutral draw commands -> renderer adapter
```

Legacy metrics/atlases remain authoritative even when uploaded into modern
texture arrays. Host fonts are not substitutes because their metrics and
Aurabesh/logo coverage change layout. Derived caches must be disposable and
keyed by original record identity plus decoder version.

Preserve code, advance, leading, source rectangle/page and original spelling.
Resolve qualified sources explicitly, validate rectangles against atlas sizes,
and late-bind palette lookup. The exact rectangle edge convention and every
rare source qualifier remain bounded fixture tests rather than guessed rules.

Counts and field/source data are **Confirmed by complete effective-overlay
extraction**. Nine-slice interpretation is **Strongly inferred**. Rectangle
edge convention is **Unknown**, bounded by fixtures.

