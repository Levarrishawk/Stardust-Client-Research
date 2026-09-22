# STFP variable-factory Boolean

Date: 2026-09-22

## Result

The final STFP Boolean is not a shader, color, alpha, palette-clamp or render-
state switch. It is an identity discriminator on the blueprint texture
renderer template's customization `VariableFactory`.

Loader `0x0086DDE0` reads palette path, customization-variable name and the
Boolean, then calls factory lookup/create `0x0086E4F0`. Lookup `0x00870510`
matches an existing factory only when both its name bytes and stored Boolean at
factory `+0x10` match. When absent, `0x0086E4F0` creates a
`PaletteColorVariableFactory` (RTTI vtable `0x015E7BA0`) through constructor
`0x0086C760`; base `VariableFactory` constructor `0x0086C5A0` stores the name
and Boolean.

The STFP operation retains only the resulting factory/vector index. Runtime
`0x0086DF70` reads the instantiated integer customization value, clamps it to
the palette and binds the selected ARGB color. The Boolean is not read on this
render path. `PaletteColorVariableFactory` creates the same palette-backed
customization-variable type regardless of the flag.

## Effective corpus

All 30 effective STFP records use the discriminator, with both false and true
values represented. Therefore it cannot be discarded even though it has no
direct shader effect: factory identity is the observable behavior. The exact
historical source/editor label for the two domains remains unknown.

## Replacement-client requirement

Key blueprint customization-variable factories by `(name, discriminator)`,
preserve the Boolean from disk, and permit two otherwise same-named factories
to remain distinct. Do not use it to alter palette color, alpha, clamping or
material state unless later evidence outside this executable establishes such
a policy.

## Confidence

- Load flow, RTTI class, pairwise lookup identity, operation index, identical
  instantiated variable type and lack of runtime render use: **confirmed by
  code**.
- Original human-readable meaning of false versus true: **unknown naming
  detail**, with no remaining runtime-behavior ambiguity.

